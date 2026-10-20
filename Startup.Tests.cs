using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using NUnit.Framework;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Coflnet.Sky.Chat;

public class StartupSwaggerTests
{
    [Test]
    public void GlobalSecurityRequirementReferencesApiToken()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(new FakeEnv());
        services.AddLogging();
        services.AddControllers().AddApplicationPart(typeof(Startup).Assembly);
        services.AddSwaggerGen(Startup.ConfigureSwagger);
        using var provider = services.BuildServiceProvider();

        var document = provider.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        Assert.That(document.Security, Is.Not.Null.And.Not.Empty);
        var requirement = document.Security.Single();
        Assert.That(requirement, Is.Not.Empty, "an empty requirement makes generated clients skip the Authorization header");
        Assert.That(requirement.Keys.Select(k => k.Reference.Id), Does.Contain("ApiToken"));

        // the serialized document is what the client generator consumes
        using var writer = new StringWriter();
        document.SerializeAsV3(new OpenApiJsonWriter(writer));
        Assert.That(writer.ToString().Replace(" ", "").Replace("\n", "").Replace("\r", ""), Does.Contain("\"security\":[{\"ApiToken\":[]}]"));
    }

    private class FakeEnv : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "SkyChat";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Production";
    }
}
