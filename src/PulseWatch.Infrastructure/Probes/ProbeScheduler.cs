using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Probes;

namespace PulseWatch.Infrastructure.Probes;

internal sealed class ProbeScheduler(
    Channel<ProbeJob> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<ProbeScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SchedulePendingProbesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task SchedulePendingProbesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IProbeRepository>();
            var probes = await repo.GetActiveAsync(ct);
            var now = DateTime.UtcNow;

            foreach (var probe in probes)
            {
                var due = probe.LastCheckedAt is null
                    || (now - probe.LastCheckedAt.Value).TotalSeconds >= probe.IntervalSeconds;

                if (!due) continue;

                var job = new ProbeJob(
                    probe.Id,
                    probe.ProjectId,
                    probe.Url,
                    probe.Method,
                    probe.TimeoutSeconds,
                    probe.Assertions.ToList());

                if (!channel.Writer.TryWrite(job))
                    logger.LogWarning("Channel full, dropping probe job {ProbeId}", probe.Id);
                else
                    await repo.MarkCheckedAsync(probe.Id, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error scheduling probes");
        }
    }
}
