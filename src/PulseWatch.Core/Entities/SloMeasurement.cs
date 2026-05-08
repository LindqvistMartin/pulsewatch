namespace PulseWatch.Core.Entities;

public sealed class SloMeasurement
{
    public Guid Id { get; private set; }
    public Guid SloDefinitionId { get; private set; }
    public DateTime ComputedAt { get; private set; }
    public double AvailabilityPct { get; private set; }
    public int? P95LatencyMs { get; private set; }
    public double ErrorBudgetTotalSeconds { get; private set; }
    public double ErrorBudgetConsumedSeconds { get; private set; }
    public double BurnRate { get; private set; }
    public DateTime? ProjectedExhaustionAt { get; private set; }

    public SloDefinition SloDefinition { get; private set; } = default!;

    private SloMeasurement() { }

    public SloMeasurement(
        Guid sloDefinitionId,
        double availabilityPct,
        int? p95LatencyMs,
        double errorBudgetTotalSeconds,
        double errorBudgetConsumedSeconds,
        double burnRate,
        DateTime? projectedExhaustionAt)
    {
        Id = Guid.NewGuid();
        SloDefinitionId = sloDefinitionId;
        ComputedAt = DateTime.UtcNow;
        AvailabilityPct = availabilityPct;
        P95LatencyMs = p95LatencyMs;
        ErrorBudgetTotalSeconds = errorBudgetTotalSeconds;
        ErrorBudgetConsumedSeconds = errorBudgetConsumedSeconds;
        BurnRate = burnRate;
        ProjectedExhaustionAt = projectedExhaustionAt;
    }
}
