namespace PulseWatch.Core.Entities;

public sealed class HealthCheck
{
    public Guid Id { get; private set; }
    public Guid ProbeId { get; private set; }
    public int? StatusCode { get; private set; }
    public long ResponseTimeMs { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CheckedAt { get; private set; }

    public Probe Probe { get; private set; } = default!;

    private HealthCheck() { }

    public HealthCheck(Guid probeId, int? statusCode, long responseTimeMs, bool isSuccess, string? failureReason = null)
    {
        Id = Guid.NewGuid();
        ProbeId = probeId;
        StatusCode = statusCode;
        ResponseTimeMs = responseTimeMs;
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        CheckedAt = DateTime.UtcNow;
    }
}
