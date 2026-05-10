using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Api.Endpoints;

public static class ProbesEndpoints
{
    public static IEndpointRouteBuilder MapProbesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/projects/{projectId:guid}/probes").WithTags("Probes");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapGet("/{id:guid}", GetById);
        group.MapDelete("/{id:guid}", Delete);
        group.MapGet("/{id:guid}/checks", GetChecks);

        return app;
    }

    static async Task<IResult> GetAll(Guid projectId, IProbeRepository repo, PulseDbContext db, CancellationToken ct)
    {
        var probes = await repo.GetByProjectAsync(projectId, ct);
        if (probes.Count == 0) return Results.Ok(Array.Empty<ProbeResponse>());

        var ids = probes.Select(p => p.Id).ToList();
        var idsArray = ids.ToArray();
        var from30d = DateTime.UtcNow.AddDays(-30);
        var from24h = DateTime.UtcNow.AddHours(-24);

        var lastChecks = await db.HealthChecks
            .Where(c => ids.Contains(c.ProbeId))
            .GroupBy(c => c.ProbeId)
            .Select(g => new { ProbeId = g.Key, IsSuccess = g.OrderByDescending(c => c.CheckedAt).Select(c => c.IsSuccess).First() })
            .ToDictionaryAsync(x => x.ProbeId, x => (bool?)x.IsSuccess, ct);

        // 30-day uptime % from materialized daily rollup
        var uptimeRows = await db.Database.SqlQuery<UptimeRow>($"""
            SELECT probe_id AS "ProbeId",
                   CASE WHEN SUM(total) = 0 THEN NULL
                        ELSE ROUND((SUM(success)::numeric / SUM(total)) * 100, 3)
                   END AS "UptimePct"
            FROM health_check_1d
            WHERE probe_id = ANY({idsArray}) AND bucket >= {from30d}
            GROUP BY probe_id
            """).ToListAsync(ct);
        var uptime30d = uptimeRows.ToDictionary(x => x.ProbeId, x => x.UptimePct);

        // P95 latency (max p95 bucket over last 24h) from materialized hourly rollup
        var p95Rows = await db.Database.SqlQuery<P95Row>($"""
            SELECT probe_id AS "ProbeId", MAX(p95_ms) AS "P95Ms"
            FROM health_check_1h
            WHERE probe_id = ANY({idsArray}) AND bucket >= {from24h}
            GROUP BY probe_id
            """).ToListAsync(ct);
        var p95map = p95Rows.ToDictionary(x => x.ProbeId, x => x.P95Ms);

        return Results.Ok(probes.Select(p => ToResponse(
            p,
            lastChecks.GetValueOrDefault(p.Id),
            uptime30d.GetValueOrDefault(p.Id),
            p95map.GetValueOrDefault(p.Id))));
    }

    static async Task<IResult> Create(Guid projectId, CreateProbeRequest req,
        PulseDbContext db, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
            return Results.NotFound();

        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.Problem(detail: "name is required", statusCode: 400);
        if (string.IsNullOrWhiteSpace(req.Url))
            return Results.Problem(detail: "url is required", statusCode: 400);
        if (req.IntervalSeconds < 15)
            return Results.Problem(detail: "intervalSeconds must be >= 15", statusCode: 400);

        var probe = new Probe(projectId, req.Name, req.Url, req.IntervalSeconds);
        db.Probes.Add(probe);

        if (req.Assertions is { Count: > 0 })
        {
            foreach (var a in req.Assertions)
            {
                if (!Enum.TryParse<AssertionType>(a.Type, ignoreCase: true, out var type))
                    return Results.Problem(detail: $"Unknown assertion type: {a.Type}", statusCode: 400);
                if (!Enum.TryParse<AssertionOperator>(a.Operator, ignoreCase: true, out var op))
                    return Results.Problem(detail: $"Unknown assertion operator: {a.Operator}", statusCode: 400);
                if (type == AssertionType.JsonPath && string.IsNullOrWhiteSpace(a.JsonPathExpression))
                    return Results.Problem(detail: "JsonPath assertions require a jsonPathExpression", statusCode: 400);
                db.ProbeAssertions.Add(new ProbeAssertion(probe.Id, type, op, a.ExpectedValue, a.JsonPathExpression));
            }
        }

        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/projects/{projectId}/probes/{probe.Id}", ToResponse(probe));
    }

    static async Task<IResult> GetById(Guid projectId, Guid id, IProbeRepository repo, CancellationToken ct)
    {
        var probe = await repo.GetByIdAsync(id, ct);
        if (probe is null || probe.ProjectId != projectId) return Results.NotFound();
        return Results.Ok(ToResponse(probe));
    }

    static async Task<IResult> Delete(Guid projectId, Guid id, IProbeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(projectId, id, ct);
        return Results.NoContent();
    }

    static async Task<IResult> GetChecks(Guid projectId, Guid id, IProbeRepository probeRepo,
        IHealthCheckRepository checksRepo, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var probe = await probeRepo.GetByIdAsync(id, ct);
        if (probe is null || probe.ProjectId != projectId) return Results.NotFound();

        var start = from ?? DateTime.UtcNow.AddHours(-24);
        var end = to ?? DateTime.UtcNow;
        var checks = await checksRepo.GetByProbeAsync(id, start, end, ct);
        return Results.Ok(checks.Select(c => new HealthCheckResponse(
            c.Id, c.StatusCode, c.ResponseTimeMs, c.IsSuccess, c.FailureReason, c.CheckedAt)));
    }

    static ProbeResponse ToResponse(Probe p, bool? lastCheckSuccess = null, double? uptimePct30d = null, long? p95Ms24h = null) =>
        new(p.Id, p.ProjectId, p.Name, p.Url, p.Method, p.IntervalSeconds, p.IsActive, p.CreatedAt, p.LastCheckedAt, lastCheckSuccess, uptimePct30d, p95Ms24h);
}

internal sealed record UptimeRow(Guid ProbeId, double? UptimePct);
internal sealed record P95Row(Guid ProbeId, long? P95Ms);
