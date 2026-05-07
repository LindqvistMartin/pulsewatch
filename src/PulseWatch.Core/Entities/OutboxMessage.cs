using System.Text.Json;

namespace PulseWatch.Core.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = default!;
    public JsonDocument Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, JsonDocument payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }
}
