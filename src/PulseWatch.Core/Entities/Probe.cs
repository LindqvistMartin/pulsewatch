namespace PulseWatch.Core.Entities;

public sealed class Probe
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Url { get; private set; } = default!;
    public string Method { get; private set; } = "GET";
    public int IntervalSeconds { get; private set; }
    public int TimeoutSeconds { get; private set; } = 10;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastCheckedAt { get; private set; }

    public Project Project { get; private set; } = default!;
    public ICollection<ProbeAssertion> Assertions { get; private set; } = [];
    public ICollection<HealthCheck> HealthChecks { get; private set; } = [];
    public ICollection<SloDefinition> SloDefinitions { get; private set; } = [];
    public ICollection<Incident> Incidents { get; private set; } = [];

    private Probe() { }

    public Probe(Guid projectId, string name, string url, int intervalSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentOutOfRangeException.ThrowIfLessThan(intervalSeconds, 15);
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Name = name;
        Url = url;
        IntervalSeconds = intervalSeconds;
        CreatedAt = DateTime.UtcNow;
    }

    public void RecordChecked() => LastCheckedAt = DateTime.UtcNow;
}
