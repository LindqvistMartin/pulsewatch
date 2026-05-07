using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;

namespace PulseWatch.Api.Endpoints;

public static class ProjectsEndpoints
{
    public static IEndpointRouteBuilder MapProjectsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organizations/{orgId:guid}/projects").WithTags("Projects");

        group.MapGet("/", GetAll);
        group.MapPost("/", Create);
        group.MapGet("/{id:guid}", GetById);

        return app;
    }

    static async Task<IResult> GetAll(Guid orgId, PulseDbContext db, CancellationToken ct)
    {
        var projects = await db.Projects.AsNoTracking().Where(p => p.OrganizationId == orgId).ToListAsync(ct);
        return Results.Ok(projects.Select(p => new ProjectResponse(p.Id, p.OrganizationId, p.Name, p.Slug, p.CreatedAt)));
    }

    static async Task<IResult> Create(Guid orgId, CreateProjectRequest req, PulseDbContext db, CancellationToken ct)
    {
        var project = new Project(orgId, req.Name, req.Slug);
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/organizations/{orgId}/projects/{project.Id}",
            new ProjectResponse(project.Id, project.OrganizationId, project.Name, project.Slug, project.CreatedAt));
    }

    static async Task<IResult> GetById(Guid orgId, Guid id, PulseDbContext db, CancellationToken ct)
    {
        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId, ct);
        if (project is null) return Results.NotFound();
        return Results.Ok(new ProjectResponse(project.Id, project.OrganizationId, project.Name, project.Slug, project.CreatedAt));
    }
}
