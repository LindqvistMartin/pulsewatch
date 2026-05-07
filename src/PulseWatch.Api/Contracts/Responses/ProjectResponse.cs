namespace PulseWatch.Api.Contracts.Responses;

public sealed record ProjectResponse(Guid Id, Guid OrganizationId, string Name, string Slug, DateTime CreatedAt);
