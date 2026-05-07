namespace PulseWatch.Api.Contracts.Responses;

public sealed record OrganizationResponse(Guid Id, string Name, string Slug, DateTime CreatedAt);
