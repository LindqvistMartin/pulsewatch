using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Infrastructure.Slo;

internal sealed class RollupRefresher(
    IServiceScopeFactory scopeFactory,
    ILogger<RollupRefresher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
            await RefreshOnceAsync(db, ct);
            logger.LogDebug("Materialized views refreshed");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to refresh materialized views");
        }
    }

    internal static async Task RefreshOnceAsync(PulseDbContext db, CancellationToken ct = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY health_check_1m", ct);
        await db.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY health_check_1h", ct);
        await db.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY health_check_1d", ct);
    }
}
