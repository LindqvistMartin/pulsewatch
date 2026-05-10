namespace PulseWatch.Api.Contracts.Responses;

public sealed record StatusPageResponse(
    Guid Id,
    Guid ProjectId,
    string Slug,
    string Title,
    string Description,
    IReadOnlyList<Guid> ProbeIds,
    DateTime CreatedAt);

public sealed record StatusPageSnapshot(
    string Title,
    string Description,
    string OverallStatus,
    IReadOnlyList<ProbeSnapshot> Probes,
    IReadOnlyList<ActiveIncidentResponse> ActiveIncidents);

public sealed record ProbeSnapshot(
    Guid Id,
    string Name,
    string Status,
    IReadOnlyList<DailyBar> DailyBars);

public sealed record DailyBar(DateOnly Date, double AvailabilityPct, int TotalChecks);

public sealed record ActiveIncidentResponse(
    Guid Id,
    DateTime OpenedAt,
    string Reason);
