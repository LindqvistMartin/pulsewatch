namespace PulseWatch.Api.Contracts.Requests;

public sealed record CreateProbeRequest(string Name, string Url, int IntervalSeconds, string Method = "GET");
