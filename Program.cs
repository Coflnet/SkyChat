using Coflnet.Security.OpenBao;
using Coflnet.Sky.Chat.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Coflnet.Sky.Chat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (PublicClientCertificateInfo.TryRun(args, System.Console.Out))
                return;
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) => config.AddOpenBaoFromEnvironment())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
