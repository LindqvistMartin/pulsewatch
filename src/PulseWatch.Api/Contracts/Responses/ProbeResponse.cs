namespace PulseWatch.Api.Contracts.Responses;

public sealed record ProbeResponse(Guid Id, Guid ProjectId, string Name, string Url, string Method, int IntervalSeconds, bool IsActive, DateTime CreatedAt, DateTime? LastCheckedAt, bool? LastCheckSuccess);
