namespace PulseWatch.Api.Contracts.Responses;

public sealed record SloDefinitionResponse(
    Guid Id,
    Guid ProbeId,
    double TargetAvailabilityPct,
    int WindowDays,
    int? TargetLatencyP95Ms,
    DateTime CreatedAt,
    SloMeasurementResponse? LatestMeasurement);

public sealed record SloMeasurementResponse(
    Guid Id,
    DateTime ComputedAt,
    double AvailabilityPct,
    int? P95LatencyMs,
    double ErrorBudgetTotalSeconds,
    double ErrorBudgetConsumedSeconds,
    double BurnRate,
    DateTime? ProjectedExhaustionAt);
