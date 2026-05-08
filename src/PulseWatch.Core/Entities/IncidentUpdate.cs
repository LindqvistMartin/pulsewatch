namespace PulseWatch.Core.Entities;

public enum IncidentStatus { Investigating, Identified, Monitoring, Resolved }

public sealed class IncidentUpdate
{
    public Guid Id { get; private set; }
    public Guid IncidentId { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string Message { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public Incident Incident { get; private set; } = default!;

    private IncidentUpdate() { }

    public IncidentUpdate(Guid incidentId, IncidentStatus status, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Id = Guid.NewGuid();
        IncidentId = incidentId;
        Status = status;
        Message = message;
        CreatedAt = DateTime.UtcNow;
    }
}
