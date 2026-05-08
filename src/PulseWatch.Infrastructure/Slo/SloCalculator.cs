using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;
using PulseWatch.Core.Services;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Infrastructure.Slo;

internal sealed class SloCalculator(
    IServiceScopeFactory scopeFactory,
    ILogger<SloCalculator> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Brief startup delay so RollupRefresher has a chance to populate views first
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ComputeAllAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    internal async Task ComputeAllAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
            var sloRepo = scope.ServiceProvider.GetRequiredService<ISloRepository>();
            var incidentRepo = scope.ServiceProvider.GetRequiredService<IIncidentRepository>();

            var definitions = await sloRepo.GetAllActiveAsync(ct);
            foreach (var def in definitions)
                await ComputeForDefinitionAsync(db, sloRepo, incidentRepo, def, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "SloCalculator error");
        }
    }

    private async Task ComputeForDefinitionAsync(
        PulseDbContext db, ISloRepository sloRepo, IIncidentRepository incidentRepo,
        SloDefinition def, CancellationToken ct)
    {
        var windowSeconds = def.WindowDays * 86400.0;
        var from = DateTime.UtcNow.AddDays(-def.WindowDays);

        // View names are constants, not user input — safe to embed in SQL string
        var rows = def.WindowDays <= 30
            ? await db.Database.SqlQuery<RollupRow>($"""
                SELECT probe_id, SUM(total) AS total, SUM(success) AS success, MAX(p95_ms) AS p95_ms
                FROM health_check_1h
                WHERE probe_id = {def.ProbeId} AND bucket >= {from}
                GROUP BY probe_id
                """).ToListAsync(ct)
            : await db.Database.SqlQuery<RollupRow>($"""
                SELECT probe_id, SUM(total) AS total, SUM(success) AS success, MAX(p95_ms) AS p95_ms
                FROM health_check_1d
                WHERE probe_id = {def.ProbeId} AND bucket >= {from}
                GROUP BY probe_id
                """).ToListAsync(ct);

        var row = rows.FirstOrDefault();
        long total = row?.total ?? 0;
        long success = row?.success ?? 0;
        int? p95 = row?.p95_ms;

        var availability = SloMath.ComputeAvailability(total, success);
        var budgetTotal = SloMath.ComputeErrorBudgetTotal(def.TargetAvailabilityPct, windowSeconds);
        var budgetConsumed = SloMath.ComputeErrorBudgetConsumed(total, success, windowSeconds);
        var burnRate = SloMath.ComputeBurnRate(budgetConsumed, budgetTotal);
        var projectedExhaustion = SloMath.ComputeProjectedExhaustion(
            DateTime.UtcNow, budgetConsumed, budgetTotal, windowSeconds);

        var measurement = new SloMeasurement(
            def.Id, availability, p95, budgetTotal, budgetConsumed, burnRate, projectedExhaustion);
        await sloRepo.AddMeasurementAsync(measurement, ct);

        await HandleIncidentAsync(incidentRepo, def, availability, total, ct);
    }

    private static async Task HandleIncidentAsync(
        IIncidentRepository incidentRepo, SloDefinition def,
        double availability, long total, CancellationToken ct)
    {
        var openIncident = await incidentRepo.GetOpenByProbeAsync(def.ProbeId, ct);
        bool isBreach = availability < def.TargetAvailabilityPct && total > 0;

        if (isBreach && openIncident is null)
        {
            var incident = new Incident(def.ProbeId, "Availability dropped below target", autoDetected: true);
            await incidentRepo.AddAsync(incident, ct);
            await incidentRepo.AddUpdateAsync(
                new IncidentUpdate(incident.Id, IncidentStatus.Investigating,
                    $"Availability {availability:F2}% below target {def.TargetAvailabilityPct:F2}%"),
                ct);
        }
        else if (!isBreach && openIncident is not null)
        {
            openIncident.Close();
            await incidentRepo.SaveChangesAsync(ct);
            await incidentRepo.AddUpdateAsync(
                new IncidentUpdate(openIncident.Id, IncidentStatus.Resolved,
                    $"Availability restored to {availability:F2}%"),
                ct);
        }
    }

    private sealed class RollupRow
    {
        public Guid probe_id { get; init; }
        public long total { get; init; }
        public long success { get; init; }
        public int? p95_ms { get; init; }
    }
}
