namespace PulseWatch.Core.Entities;

public sealed class SloDefinition
{
    public Guid Id { get; private set; }
    public Guid ProbeId { get; private set; }
    public double TargetAvailabilityPct { get; private set; }
    public int? TargetLatencyP95Ms { get; private set; }
    public int WindowDays { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Probe Probe { get; private set; } = default!;
    public ICollection<SloMeasurement> Measurements { get; private set; } = [];

    private SloDefinition() { }

    public SloDefinition(Guid probeId, double targetAvailabilityPct, int windowDays, int? targetLatencyP95Ms = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(targetAvailabilityPct, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(targetAvailabilityPct, 100);
        ArgumentOutOfRangeException.ThrowIfLessThan(windowDays, 1);
        Id = Guid.NewGuid();
        ProbeId = probeId;
        TargetAvailabilityPct = targetAvailabilityPct;
        TargetLatencyP95Ms = targetLatencyP95Ms;
        WindowDays = windowDays;
        CreatedAt = DateTime.UtcNow;
    }
}
