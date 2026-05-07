namespace PulseWatch.Core.Entities;

public sealed class Project
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public Organization Organization { get; private set; } = default!;
    public ICollection<Probe> Probes { get; private set; } = [];

    private Project() { }

    public Project(Guid organizationId, string name, string slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = name;
        Slug = slug;
        CreatedAt = DateTime.UtcNow;
    }
}
