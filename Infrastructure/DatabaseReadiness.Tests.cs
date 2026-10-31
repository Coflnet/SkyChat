using System;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Coflnet.Sky.Chat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Coflnet.Sky.Chat.Infrastructure;

public class DatabaseReadinessTests
{
    [Test]
    public async Task ReadinessQueriesSqlEveryTime()
    {
        var sql = new SqlProbe();
        using var services = Services(sql);
        await DatabaseReadiness.CheckAsync(services);
        await DatabaseReadiness.CheckAsync(services);
        Assert.That(sql.Calls, Is.EqualTo(2));
        Assert.That(sql.CancellationCanBeRequested, Is.True);
    }

    [Test]
    public void DatabaseFailureFailsTheReadinessCheck()
    {
        using var services = Services(new SqlProbe { Fail = true });
        Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseReadiness.CheckAsync(services));
    }

    [Test]
    public async Task StalledSqlIsCancelledWithinTheOverallReadinessDeadline()
    {
        var sql = new SqlProbe { Stall = true };
        using var services = Services(sql);
        var elapsed = Stopwatch.StartNew();
        Assert.CatchAsync<Exception>(() => DatabaseReadiness.CheckAsync(services));
        Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds(6)));
        Assert.That(sql.Calls, Is.EqualTo(1));
        await sql.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task CallerCancellationCancelsPendingSql()
    {
        var sql = new SqlProbe { Stall = true };
        using var services = Services(sql);
        using var cancellation = new CancellationTokenSource();
        var check = DatabaseReadiness.CheckAsync(services, cancellation.Token);
        await sql.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        cancellation.Cancel();
        Assert.CatchAsync<OperationCanceledException>(() => check);
        await sql.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    private static ServiceProvider Services(SqlProbe sql) => new ServiceCollection()
        .AddDbContext<ChatDbContext>(options => options.UseMySql(
            "Server=synthetic.invalid;User ID=synthetic;Password=synthetic-only;ConnectionTimeout=3600",
            new MariaDbServerVersion(new Version(10, 11)), options => options.EnableRetryOnFailure(5).CommandTimeout(5))
            .AddInterceptors(new SuppressOpen(), sql)).BuildServiceProvider();

    private sealed class SuppressOpen : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => new(InterceptionResult.Suppress());
    }

    private sealed class SqlProbe : DbCommandInterceptor
    {
        public bool Stall, Fail, CancellationCanBeRequested;
        public int Calls;
        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Assert.That(command.CommandText, Is.EqualTo("SELECT 1"));
            Calls++;
            Started.TrySetResult(true);
            CancellationCanBeRequested = cancellationToken.CanBeCanceled;
            if (Fail) throw new InvalidOperationException("synthetic SQL failure");
            if (Stall)
            {
                try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
                finally { if (cancellationToken.IsCancellationRequested) Cancelled.TrySetResult(true); }
            }
            return InterceptionResult<int>.SuppressWithResult(1);
        }
    }
}
