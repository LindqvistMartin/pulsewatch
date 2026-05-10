using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Api.Endpoints;

public static class OrganizationsEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organizations").WithTags("Organizations");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapGet("/{id:guid}", GetById);
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(IOrganizationRepository repo, CancellationToken ct)
    {
        var orgs = await repo.GetAllAsync(ct);
        return Results.Ok(orgs.Select(o => new OrganizationResponse(o.Id, o.Name, o.Slug, o.CreatedAt)));
    }

    static async Task<IResult> Create(CreateOrganizationRequest req, IOrganizationRepository repo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.Problem(detail: "name is required", statusCode: 400);
        if (string.IsNullOrWhiteSpace(req.Slug))
            return Results.Problem(detail: "slug is required", statusCode: 400);

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

    static async Task<IResult> Delete(Guid id, PulseDbContext db, CancellationToken ct)
    {
        var deleted = await db.Organizations.Where(o => o.Id == id).ExecuteDeleteAsync(ct);
        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }
}
