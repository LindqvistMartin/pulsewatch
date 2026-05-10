namespace PulseWatch.Core.Entities;

public sealed class StatusPage
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Slug { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public List<Guid> ProbeIds { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }

    public Project Project { get; private set; } = default!;

    private StatusPage() { }

    public StatusPage(Guid projectId, string slug, string title, string description, IReadOnlyList<Guid> probeIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(probeIds);
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Slug = slug;
        Title = title;
        Description = description ?? string.Empty;
        ProbeIds = probeIds.ToList();
        CreatedAt = DateTime.UtcNow;
    }
}
