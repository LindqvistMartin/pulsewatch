namespace PulseWatch.Api.Contracts.Requests;

public sealed record CreateAssertionRequest(
    string Type,
    string Operator,
    string ExpectedValue,
    string? JsonPathExpression = null);

public sealed record CreateProbeRequest(
    string Name,
    string Url,
    int IntervalSeconds,
    string Method = "GET",
    IReadOnlyList<CreateAssertionRequest>? Assertions = null);
