namespace PulseWatch.Core.Probes;

public sealed record ProbeJob(Guid ProbeId, string Url, string Method, int TimeoutSeconds, IReadOnlyList<Guid> AssertionIds);
