using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Hubs;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Api.BackgroundServices;

internal sealed class OutboxRelay(
    IServiceScopeFactory scopeFactory,
    IHubContext<PulseHub> hub,
    ILogger<OutboxRelay> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OutboxRelay error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var messages = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAt" IS NULL
                ORDER BY "CreatedAt"
                LIMIT 50
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        if (messages.Count == 0)
        {
            await tx.RollbackAsync(ct);
            return;
        }

        foreach (var msg in messages)
        {
            try
            {
                if (msg.Type == "HealthCheckRecorded")
                {
                    using var doc = JsonDocument.Parse(msg.Payload);
                    var projectId = doc.RootElement.GetProperty("ProjectId").GetGuid();
                    await hub.Clients.Group($"proj:{projectId}")
                        .SendAsync("HealthCheckRecorded", msg.Payload, ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Poison-message: mark processed anyway to prevent infinite retry loop.
                // The SignalR broadcast is best-effort; connected clients re-fetch on reconnect.
                logger.LogError(ex, "Undeliverable outbox message {Id} (type={Type}) — skipping to prevent stuck relay",
                    msg.Id, msg.Type);
            }

            // Mark processed regardless of dispatch outcome. Explicit state assignment
            // guards against EF change tracker not detecting the mutation in edge cases.
            msg.ProcessedAt = DateTime.UtcNow;
            db.Entry(msg).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
