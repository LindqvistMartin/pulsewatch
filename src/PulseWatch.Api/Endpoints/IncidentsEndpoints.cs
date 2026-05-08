using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Abstractions;

namespace PulseWatch.Api.Endpoints;

public static class IncidentsEndpoints
{
    public static IEndpointRouteBuilder MapIncidentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/projects/{projectId:guid}/probes/{probeId:guid}/incidents")
            .WithTags("Incidents");

        group.MapGet("/", GetAll);

        return app;
    }

    static async Task<IResult> GetAll(
        Guid projectId, Guid probeId,
        IProbeRepository probeRepo, IIncidentRepository incidentRepo,
        CancellationToken ct)
    {
        var probe = await probeRepo.GetByIdAsync(probeId, ct);
        if (probe is null || probe.ProjectId != projectId)
            return Results.NotFound();

        var incidents = await incidentRepo.GetByProbeAsync(probeId, ct);
        return Results.Ok(incidents.Select(i => new IncidentResponse(
            i.Id, i.ProbeId, i.OpenedAt, i.ClosedAt, i.Reason, i.AutoDetected,
            i.Updates
                .OrderBy(u => u.CreatedAt)
                .Select(u => new IncidentUpdateResponse(u.Id, u.Status.ToString(), u.Message, u.CreatedAt))
                .ToList())));
    }
}
