namespace PulseWatch.Core.Assertions;

public sealed record AssertionContext(int? StatusCode, long ResponseTimeMs, string? Body);
