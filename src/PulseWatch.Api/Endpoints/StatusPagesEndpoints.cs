using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Api.Endpoints;

public static class StatusPagesEndpoints
{
    public static IEndpointRouteBuilder MapStatusPagesEndpoints(this IEndpointRouteBuilder app)
    {
        var mgmt = app.MapGroup("/api/v1/projects/{projectId:guid}/status-pages").WithTags("StatusPages");
        mgmt.MapPost("/", Create);
        mgmt.MapGet("/", GetAll);
        mgmt.MapDelete("/{id:guid}", Delete);

        app.MapGet("/public/status/{slug}", GetSnapshot).WithTags("Public");

        return app;
    }

    static async Task<IResult> Create(Guid projectId, CreateStatusPageRequest req,
        PulseDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Slug))
            return Results.Problem(detail: "slug is required", statusCode: 400);
        if (string.IsNullOrWhiteSpace(req.Title))
            return Results.Problem(detail: "title is required", statusCode: 400);
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
            return Results.NotFound();

        var page = new StatusPage(projectId, req.Slug, req.Title, req.Description ?? string.Empty, req.ProbeIds ?? []);
        db.StatusPages.Add(page);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.NpgsqlException { SqlState: "23505" })
        {
            return Results.Problem(detail: "slug already in use", statusCode: 409);
        }
        return Results.Created($"/api/v1/projects/{projectId}/status-pages/{page.Id}", ToResponse(page));
    }

    static async Task<IResult> GetAll(Guid projectId, PulseDbContext db, CancellationToken ct)
    {
        var pages = await db.StatusPages.Where(sp => sp.ProjectId == projectId).ToListAsync(ct);
        return Results.Ok(pages.Select(ToResponse));
    }

    static async Task<IResult> Delete(Guid projectId, Guid id, PulseDbContext db, CancellationToken ct)
    {
        var deleted = await db.StatusPages.Where(sp => sp.Id == id && sp.ProjectId == projectId)
            .ExecuteDeleteAsync(ct);
        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }

    static async Task<IResult> GetSnapshot(string slug, PulseDbContext db, IMemoryCache cache, CancellationToken ct)
    {
        var cacheKey = $"status-page:{slug}";
        if (cache.TryGetValue(cacheKey, out StatusPageSnapshot? cached))
            return Results.Ok(cached);

        var page = await db.StatusPages.FirstOrDefaultAsync(sp => sp.Slug == slug, ct);
        if (page is null) return Results.NotFound();

        var cutoff90 = DateTime.UtcNow.AddDays(-89);
        var now = DateTime.UtcNow;
        var probeIds = page.ProbeIds.ToList();

        // Current status per probe (last health check)
        var lastChecks = await db.HealthChecks
            .Where(c => probeIds.Contains(c.ProbeId))
            .GroupBy(c => c.ProbeId)
            .Select(g => new { ProbeId = g.Key, IsSuccess = g.OrderByDescending(c => c.CheckedAt).Select(c => c.IsSuccess).First() })
            .ToDictionaryAsync(x => x.ProbeId, x => x.IsSuccess, ct);

        // Daily bars from materialized view (last 90 days)
        var dailyRows = await db.Database.SqlQuery<DailyRollupRow>($"""
            SELECT probe_id AS "ProbeId", bucket AS "Bucket", total AS "Total", success AS "Success"
            FROM health_check_1d
            WHERE probe_id = ANY({probeIds.ToArray()}) AND bucket >= {cutoff90}
            """).ToListAsync(ct);

        var dailyByProbe = dailyRows
            .GroupBy(r => r.ProbeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Probe names
        var probes = await db.Probes.Where(p => probeIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        // Active incidents
        var incidents = await db.Incidents
            .Where(i => probeIds.Contains(i.ProbeId) && i.ClosedAt == null)
            .OrderByDescending(i => i.OpenedAt)
            .ToListAsync(ct);

        var probeSnapshots = probeIds.Select(pid =>
        {
            var lastOk = lastChecks.GetValueOrDefault(pid, true);
            var status = lastOk ? "Healthy" : "Down";
            var dailyBars = BuildDailyBars(dailyByProbe.GetValueOrDefault(pid) ?? [], cutoff90, now);
            return new ProbeSnapshot(pid, probes.GetValueOrDefault(pid, "Unknown"), status, dailyBars);
        }).ToList();

        var overallStatus = probeSnapshots.All(p => p.Status == "Healthy") ? "Operational"
            : probeSnapshots.Any(p => p.Status == "Down") ? "Outage"
            : "Degraded";

        var snapshot = new StatusPageSnapshot(
            page.Title,
            page.Description,
            overallStatus,
            probeSnapshots,
            incidents.Select(i => new ActiveIncidentResponse(i.Id, i.OpenedAt, i.Reason)).ToList());

        cache.Set(cacheKey, snapshot, TimeSpan.FromSeconds(30));
        return Results.Ok(snapshot);
    }

    static IReadOnlyList<DailyBar> BuildDailyBars(
        List<DailyRollupRow> rows, DateTime from, DateTime to)
    {
        var byDate = rows.ToDictionary(r => DateOnly.FromDateTime(r.Bucket));
        var bars = new List<DailyBar>();
        var current = DateOnly.FromDateTime(from.Date);
        var end = DateOnly.FromDateTime(to.Date);
        while (current <= end)
        {
            if (byDate.TryGetValue(current, out var row))
            {
                var pct = row.Total > 0 ? row.Success * 100.0 / row.Total : 100.0;
                bars.Add(new DailyBar(current, Math.Round(pct, 2), row.Total));
            }
            else
            {
                bars.Add(new DailyBar(current, 100.0, 0));
            }
            current = current.AddDays(1);
        }
        return bars;
    }

    static StatusPageResponse ToResponse(StatusPage p) =>
        new(p.Id, p.ProjectId, p.Slug, p.Title, p.Description, p.ProbeIds, p.CreatedAt);
}

internal sealed record DailyRollupRow(Guid ProbeId, DateTime Bucket, int Total, int Success);
