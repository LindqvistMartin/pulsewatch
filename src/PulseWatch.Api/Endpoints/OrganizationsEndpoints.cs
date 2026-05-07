using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
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

        return app;
    }

    static async Task<IResult> GetAll(PulseDbContext db, CancellationToken ct)
    {
        var orgs = await db.Organizations.AsNoTracking().ToListAsync(ct);
        return Results.Ok(orgs.Select(o => new OrganizationResponse(o.Id, o.Name, o.Slug, o.CreatedAt)));
    }

    static async Task<IResult> Create(CreateOrganizationRequest req, PulseDbContext db, CancellationToken ct)
    {
        var org = new Organization(req.Name, req.Slug);
        db.Organizations.Add(org);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/organizations/{org.Id}",
            new OrganizationResponse(org.Id, org.Name, org.Slug, org.CreatedAt));
    }

    static async Task<IResult> GetById(Guid id, PulseDbContext db, CancellationToken ct)
    {
        var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (org is null) return Results.NotFound();
        return Results.Ok(new OrganizationResponse(org.Id, org.Name, org.Slug, org.CreatedAt));
    }
}
