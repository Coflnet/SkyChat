using System;
using System.Threading;
using System.Threading.Tasks;
using Coflnet.Sky.Chat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Coflnet.Sky.Chat.Infrastructure;

internal static class DatabaseReadiness
{
    public static async Task CheckAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        try
        {
            await Task.Run(async () =>
            {
                using var scope = services.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
                await database.Database.ExecuteSqlRawAsync("SELECT 1", timeout.Token);
            }, timeout.Token).WaitAsync(TimeSpan.FromSeconds(4), cancellationToken);
        }
        finally { timeout.Cancel(); }
    }
}
