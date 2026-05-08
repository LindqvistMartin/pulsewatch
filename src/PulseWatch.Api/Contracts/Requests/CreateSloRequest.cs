namespace PulseWatch.Api.Contracts.Requests;

public sealed record CreateSloRequest(
    double TargetAvailabilityPct,
    int WindowDays,
    int? TargetLatencyP95Ms = null);
