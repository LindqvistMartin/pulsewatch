namespace PulseWatch.Api.Contracts.Responses;

public sealed record IncidentResponse(
    Guid Id,
    Guid ProbeId,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string Reason,
    bool AutoDetected,
    IReadOnlyList<IncidentUpdateResponse> Updates);

public sealed record IncidentUpdateResponse(
    Guid Id,
    string Status,
    string Message,
    DateTime CreatedAt);
