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

    static async Task<IResult> GetAll(Guid projectId, IProbeRepository repo, CancellationToken ct)
    {
        var probes = await repo.GetByProjectAsync(projectId, ct);
        return Results.Ok(probes.Select(ToResponse));
    }

    static async Task<IResult> Create(Guid projectId, CreateProbeRequest req,
        PulseDbContext db, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
            return Results.NotFound();

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

    static ProbeResponse ToResponse(Probe p) =>
        new(p.Id, p.ProjectId, p.Name, p.Url, p.Method, p.IntervalSeconds, p.IsActive, p.CreatedAt);
}
