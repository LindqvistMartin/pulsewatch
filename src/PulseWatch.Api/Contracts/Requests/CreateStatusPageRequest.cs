namespace PulseWatch.Api.Contracts.Requests;

public sealed record CreateStatusPageRequest(
    string Slug,
    string Title,
    string Description,
    IReadOnlyList<Guid> ProbeIds);
