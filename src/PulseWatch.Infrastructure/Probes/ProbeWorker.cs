using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseWatch.Core.Entities;
using PulseWatch.Core.Probes;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Infrastructure.Probes;

internal sealed class ProbeWorker(
    Channel<ProbeJob> channel,
    IHttpClientFactory httpFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<ProbeWorker> logger) : BackgroundService
{
    private static readonly ActivitySource Source = new("PulseWatch.Probes");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(ProbeJob job, CancellationToken ct)
    {
        using var activity = Source.StartActivity("probe.execute");
        activity?.SetTag("probe.id", job.ProbeId.ToString());

        var client = httpFactory.CreateClient("probe");
        var sw = Stopwatch.StartNew();
        int? statusCode = null;
        bool isSuccess = false;
        string? failureReason = null;

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(job.Method), job.Url);
            using var response = await client.SendAsync(request, ct);
            sw.Stop();
            statusCode = (int)response.StatusCode;
            isSuccess = response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            failureReason = ex.Message;
            logger.LogWarning("Probe {ProbeId} failed after {Ms}ms: {Reason}", job.ProbeId, sw.ElapsedMilliseconds, ex.Message);
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var check = new HealthCheck(job.ProbeId, statusCode, sw.ElapsedMilliseconds, isSuccess, failureReason);
            db.HealthChecks.Add(check);
            db.OutboxMessages.Add(new OutboxMessage(
                "HealthCheckRecorded",
                JsonDocument.Parse(JsonSerializer.Serialize(new
                {
                    check.ProbeId,
                    check.IsSuccess,
                    check.ResponseTimeMs,
                    check.CheckedAt
                }))));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            logger.LogInformation("Probe {ProbeId} {Result} in {Ms}ms", job.ProbeId, isSuccess ? "OK" : "FAIL", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to persist health check for probe {ProbeId}", job.ProbeId);
        }
    }
}
