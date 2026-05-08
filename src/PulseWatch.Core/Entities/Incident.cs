namespace PulseWatch.Core.Entities;

public sealed class Incident
{
    public Guid Id { get; private set; }
    public Guid ProbeId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string Reason { get; private set; } = default!;
    public bool AutoDetected { get; private set; }

    public Probe Probe { get; private set; } = default!;
    public ICollection<IncidentUpdate> Updates { get; private set; } = [];

    public bool IsOpen => ClosedAt is null;

    private Incident() { }

    public Incident(Guid probeId, string reason, bool autoDetected)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Id = Guid.NewGuid();
        ProbeId = probeId;
        Reason = reason;
        AutoDetected = autoDetected;
        OpenedAt = DateTime.UtcNow;
    }

    public void Close() => ClosedAt = DateTime.UtcNow;
}
