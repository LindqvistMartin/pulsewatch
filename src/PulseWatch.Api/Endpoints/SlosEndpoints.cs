using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Api.Endpoints;

public static class SlosEndpoints
{
    public static IEndpointRouteBuilder MapSlosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/projects/{projectId:guid}/probes/{probeId:guid}/slos")
            .WithTags("SLOs");

        group.MapPost("/", Create);
        group.MapGet("/", GetAll);

        return app;
    }

    static async Task<IResult> Create(
        Guid projectId, Guid probeId,
        CreateSloRequest req,
        IProbeRepository probeRepo, ISloRepository sloRepo,
        CancellationToken ct)
    {
        var probe = await probeRepo.GetByIdAsync(probeId, ct);
        if (probe is null || probe.ProjectId != projectId)
            return Results.NotFound();

        if (req.TargetAvailabilityPct is < 0 or > 100)
            return Results.Problem(detail: "targetAvailabilityPct must be between 0 and 100", statusCode: 400);
        if (req.WindowDays < 1)
            return Results.Problem(detail: "windowDays must be >= 1", statusCode: 400);

        var def = new SloDefinition(probeId, req.TargetAvailabilityPct, req.WindowDays, req.TargetLatencyP95Ms);
        await sloRepo.AddAsync(def, ct);
        return Results.Created(
            $"/api/v1/projects/{projectId}/probes/{probeId}/slos/{def.Id}",
            ToDefinitionResponse(def, null));
    }

    static async Task<IResult> GetAll(
        Guid projectId, Guid probeId,
        IProbeRepository probeRepo, ISloRepository sloRepo,
        CancellationToken ct)
    {
        var probe = await probeRepo.GetByIdAsync(probeId, ct);
        if (probe is null || probe.ProjectId != projectId)
            return Results.NotFound();

        var definitions = await sloRepo.GetByProbeAsync(probeId, ct);
        var responses = new List<SloDefinitionResponse>();
        foreach (var def in definitions)
        {
            var latest = await sloRepo.GetLatestMeasurementAsync(def.Id, ct);
            responses.Add(ToDefinitionResponse(def, latest));
        }
        return Results.Ok(responses);
    }

    static SloDefinitionResponse ToDefinitionResponse(SloDefinition def, SloMeasurement? latest) =>
        new(def.Id, def.ProbeId, def.TargetAvailabilityPct, def.WindowDays, def.TargetLatencyP95Ms,
            def.CreatedAt, latest is null ? null : ToMeasurementResponse(latest));

    static SloMeasurementResponse ToMeasurementResponse(SloMeasurement m) =>
        new(m.Id, m.ComputedAt, m.AvailabilityPct, m.P95LatencyMs,
            m.ErrorBudgetTotalSeconds, m.ErrorBudgetConsumedSeconds, m.BurnRate, m.ProjectedExhaustionAt);
}
