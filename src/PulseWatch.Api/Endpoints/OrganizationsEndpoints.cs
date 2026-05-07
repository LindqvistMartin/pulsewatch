using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Api.Endpoints;

public static class OrganizationsEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organizations").WithTags("Organizations");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapGet("/{id:guid}", GetById);

        return app;
    }

    static async Task<IResult> GetAll(IOrganizationRepository repo, CancellationToken ct)
    {
        var orgs = await repo.GetAllAsync(ct);
        return Results.Ok(orgs.Select(o => new OrganizationResponse(o.Id, o.Name, o.Slug, o.CreatedAt)));
    }

    static async Task<IResult> Create(CreateOrganizationRequest req, IOrganizationRepository repo, CancellationToken ct)
    {
        var org = new Organization(req.Name, req.Slug);
        await repo.AddAsync(org, ct);
        return Results.Created($"/api/v1/organizations/{org.Id}",
            new OrganizationResponse(org.Id, org.Name, org.Slug, org.CreatedAt));
    }

    static async Task<IResult> GetById(Guid id, IOrganizationRepository repo, CancellationToken ct)
    {
        var org = await repo.GetByIdAsync(id, ct);
        if (org is null) return Results.NotFound();
        return Results.Ok(new OrganizationResponse(org.Id, org.Name, org.Slug, org.CreatedAt));
    }
}
