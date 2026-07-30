using System.Threading;
using System.Threading.Tasks;
using Coflnet.Sky.Chat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace Coflnet.Sky.Chat.Services
{

    public class ChatBackgroundService : BackgroundService
    {
        private IServiceScopeFactory scopeFactory;
        private IConfiguration config;
        private ILogger<ChatBackgroundService> logger;
        private Prometheus.Counter consumeCount = Prometheus.Metrics.CreateCounter("sky_chat_conume", "How many messages were consumed");

        private ConcurrentDictionary<string, ModelClient> Clients = new();
        internal const int MaximumModerationQuoteRetentionHours = 96;

        public bool Ready => Clients.Count > 0;

        public ChatBackgroundService(
            IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<ChatBackgroundService> logger)
        {
            this.scopeFactory = scopeFactory;
            this.config = config;
            this.logger = logger;
        }

        internal ModelClient GetClient(string clientToken)
        {
            if (!Clients.TryGetValue(clientToken, out ModelClient client))
                throw new ApiException("invalid_token", "Invalid client Id/unkown client");
            return client;
        }

        /// <summary>
        /// Called by asp.net on startup
        /// </summary>
        /// <param name="stoppingToken">is canceled when the applications stops</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            // make sure all migrations are applied
            // runs syncronously to avoid multiple migrations starting simotaniously 
            context.Database.Migrate();

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var sanctionCutoff = now
                    - TimeSpan.FromDays(MuteService.RepeatSanctionWindowDays);
                var sanctionsDeleted = await context.Mute
                    .Where(ExpiredSanctionOutsideRepeatWindow(sanctionCutoff))
                    .ExecuteDeleteAsync(stoppingToken);
                if (sanctionsDeleted > 0)
                    logger.LogInformation(
                        "Deleted {Count} expired sanctions outside the repeat-sanction window",
                        sanctionsDeleted);

                var quoteRetention = GetModerationQuoteRetention(
                    config["MODERATION_QUOTE_RETENTION_HOURS"]);
                var quoteCutoff = now - quoteRetention;
                var redacted = await context.Mute
                    .Where(QuotedEvidenceExpired(quoteCutoff))
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.Message, (string)null)
                            .SetProperty(m => m.Reason, (string)null),
                        stoppingToken);
                if (redacted > 0)
                    logger.LogInformation(
                        "Redacted quoted moderation evidence from {Count} sanctions",
                        redacted);
                MuteService.RedactCachedEvidence(quoteCutoff);
                Clients = new ConcurrentDictionary<string, ModelClient>(await context.Clients.ToDictionaryAsync(c => c.ApiKey));
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

            }

        }

        internal async Task SendWebhooks(ChatMessage message)
        {
            var webhookClients = Clients
                .Where(c => IsWebhookAllowed(c.Value))
                .ToArray();
            if (webhookClients.Length == 0)
                return;

            using var client = new HttpClient();
            message.Message = Regex.Replace(message.Message, @"§.", "");
            var serialized = JsonConvert.SerializeObject(message);
            await Task.WhenAll(webhookClients.Select(async c =>
            {
                var hook = c.Value;
                var content = new StringContent(serialized, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, hook.WebHook)
                {
                    Content = content
                };
                request.Headers.Add("Authorization", hook.WebhookAuth);

                var response = await client.SendAsync(request);
                if(response.StatusCode == System.Net.HttpStatusCode.BadGateway)
                {
                    Clients.TryRemove(c);
                }
            }));
        }

        internal static TimeSpan GetModerationQuoteRetention(string configuredHours)
        {
            if (!int.TryParse(configuredHours, out var hours) || hours < 1)
                hours = MaximumModerationQuoteRetentionHours;
            return TimeSpan.FromHours(
                Math.Min(hours, MaximumModerationQuoteRetentionHours));
        }

        internal static Expression<Func<Mute, bool>> QuotedEvidenceExpired(
            DateTime cutoff)
        {
            return mute => mute.Timestamp < cutoff
                && (mute.Message != null || mute.Reason != null);
        }

        internal static Expression<Func<Mute, bool>>
            ExpiredSanctionOutsideRepeatWindow(DateTime cutoff)
        {
            return mute => mute.Expires <= cutoff;
        }

        internal static bool IsWebhookAllowed(ModelClient client)
        {
            return client != null
                && !string.IsNullOrWhiteSpace(client.WebHook)
                && !string.IsNullOrWhiteSpace(client.WebhookAuth)
                && Uri.TryCreate(client.WebHook, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps;
        }

        private ChatService GetService()
        {
            return scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ChatService>();
        }
    }
}
