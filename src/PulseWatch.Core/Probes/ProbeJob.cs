using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Probes;

public sealed record ProbeJob(
    Guid ProbeId,
    Guid ProjectId,
    string Url,
    string Method,
    int TimeoutSeconds,
    IReadOnlyList<ProbeAssertion> Assertions);
