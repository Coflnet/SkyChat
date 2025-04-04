using System;
using System.IO;
using System.Net;
using System.Reflection;
using Coflnet.Kafka;
using Coflnet.Sky.Chat.Models;
using Coflnet.Sky.Chat.Services;
using Coflnet.Sky.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Prometheus;

namespace Coflnet.Sky.Chat
{
    public partial class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        Prometheus.Counter errorCount = Prometheus.Metrics.CreateCounter("sky_api_error", "Counts the amount of error responses handed out");

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddJsonOptions(o =>
            {
                // always serialize to UTC
                o.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
            });
            // add api token authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiToken", policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        var token = context.User.FindFirst("ApiToken")?.Value;
                        return token == Configuration["API_TOKEN"];
                    });
                });
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SkyChat", Version = "v1" });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                // document auth
                c.AddSecurityDefinition("ApiToken", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiToken"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // Replace with your server version and type.
            // Use 'MariaDbServerVersion' for MariaDB.
            // Alternatively, use 'ServerVersion.AutoDetect(connectionString)'.
            // For common usages, see pull request #1233.
            var serverVersion = new MariaDbServerVersion(new Version(Configuration["MARIADB_VERSION"]));

            // Replace 'YourDbContext' with the name of your own DbContext derived class.
            services.AddDbContext<ChatDbContext>(
                dbContextOptions => dbContextOptions
                    .UseMySql(Configuration["DB_CONNECTION"], serverVersion, mySqlOptions => mySqlOptions
                        .EnableRetryOnFailure(5).CommandTimeout(5))
                    .EnableSensitiveDataLogging() // <-- These two calls are optional but help
                    .EnableDetailedErrors()       // <-- with debugging (remove for production).
            );
            services.AddSingleton<ChatBackgroundService>();
            services.AddHostedService<ChatBackgroundService>(di => di.GetRequiredService<ChatBackgroundService>());
            services.AddJaeger(Configuration, 1);
            services.AddTransient<ChatService>();
            services.AddTransient<MuteService>();
            services.AddTransient<IMuteList, MuteService>(di => di.GetRequiredService<MuteService>());
            services.AddTransient<IMuteService, MuteService>(di => di.GetRequiredService<MuteService>());
            services.AddSingleton<IMuteService, MuteProducer>();
            services.AddSingleton<EmojiService>();
            services.AddSingleton<Kafka.KafkaCreator>();
            services.AddSingleton<PlayerName.Client.Api.IPlayerNameApi>(sp =>
            {
                return new PlayerName.Client.Api.PlayerNameApi(Configuration["PLAYERNAME_BASE_URL"]);
            });
            services.AddSingleton<StackExchange.Redis.ConnectionMultiplexer>((config) =>
            {
                return StackExchange.Redis.ConnectionMultiplexer.Connect(Configuration["REDIS_HOST"]);
            });
            services.AddResponseCompression();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyChat v1");
                c.RoutePrefix = "api";
            });

            app.UseResponseCaching();
            app.UseResponseCompression();

            app.UseRouting();

            app.UseAuthorization();

            app.UseExceptionHandler(errorApp =>
            {
                ErrorHandler.Add(errorApp, "chat");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapMetrics();
                endpoints.MapControllers();
            });
        }
    }
}
