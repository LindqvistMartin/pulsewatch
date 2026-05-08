using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Assertions;
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
    private static readonly AssertionEvaluatorFactory _factory = new();

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
            // using var response ensures disposal on all paths including mid-stream read exceptions
            using var response = await client.SendAsync(request, ct);
            sw.Stop();
            statusCode = (int)response.StatusCode;

            string? body = null;
            if (job.Assertions.Any(a => a.Type is AssertionType.BodyRegex or AssertionType.JsonPath))
                body = await response.Content.ReadAsStringAsync(ct);

            if (job.Assertions.Count > 0)
            {
                var ctx = new AssertionContext(statusCode, sw.ElapsedMilliseconds, body);
                var results = job.Assertions.Select(a => _factory.Get(a.Type).Evaluate(a, ctx)).ToList();
                isSuccess = results.All(r => r.Passed);
                if (!isSuccess)
                    failureReason = string.Join("; ", results.Where(r => !r.Passed).Select(r => r.FailureMessage));
            }
            else
            {
                isSuccess = statusCode is >= 200 and < 300;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            sw.Stop();
            failureReason = ex.Message;
            logger.LogWarning("Probe {ProbeId} failed after {Ms}ms: {Reason}",
                job.ProbeId, sw.ElapsedMilliseconds, ex.Message);
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
            var probeRepo = scope.ServiceProvider.GetRequiredService<IProbeRepository>();

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var check = new HealthCheck(job.ProbeId, statusCode, sw.ElapsedMilliseconds, isSuccess, failureReason);
            db.HealthChecks.Add(check);
            db.OutboxMessages.Add(new OutboxMessage(
                "HealthCheckRecorded",
                JsonSerializer.Serialize(new
                {
                    check.ProbeId,
                    ProjectId = job.ProjectId,
                    check.IsSuccess,
                    check.StatusCode,
                    check.ResponseTimeMs,
                    check.CheckedAt,
                    FailureReason = check.FailureReason
                })));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // MarkCheckedAsync is outside the transaction by design: accepted at-least-once semantics.
            // If the process crashes between CommitAsync and here, the probe fires again on the next
            // scheduler tick and writes a duplicate HealthCheck row. This is benign for a monitoring tool.
            await probeRepo.MarkCheckedAsync(job.ProbeId, ct);

            logger.LogInformation("Probe {ProbeId} {Result} in {Ms}ms",
                job.ProbeId, isSuccess ? "OK" : "FAIL", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to persist health check for probe {ProbeId}", job.ProbeId);
        }
    }
}
