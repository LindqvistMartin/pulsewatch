namespace PulseWatch.Api.Contracts.Responses;

public sealed record HealthCheckResponse(Guid Id, int? StatusCode, long ResponseTimeMs, bool IsSuccess, string? FailureReason, DateTime CheckedAt);
