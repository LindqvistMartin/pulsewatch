using Microsoft.EntityFrameworkCore;
using Npgsql;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Abstractions;
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
        group.MapDelete("/{id:guid}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(Guid orgId, IProjectRepository repo, CancellationToken ct)
    {
        var projects = await repo.GetByOrganizationAsync(orgId, ct);
        return Results.Ok(projects.Select(p => new ProjectResponse(p.Id, p.OrganizationId, p.Name, p.Slug, p.CreatedAt)));
    }

    static async Task<IResult> Create(Guid orgId, CreateProjectRequest req, IProjectRepository repo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.Problem(detail: "name is required", statusCode: 400);
        if (string.IsNullOrWhiteSpace(req.Slug))
            return Results.Problem(detail: "slug is required", statusCode: 400);

        var project = new Project(orgId, req.Name, req.Slug);
        try
        {
            await repo.AddAsync(project, ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Results.Problem(detail: "slug already in use", statusCode: 409);
        }
        return Results.Created($"/api/v1/organizations/{orgId}/projects/{project.Id}",
            new ProjectResponse(project.Id, project.OrganizationId, project.Name, project.Slug, project.CreatedAt));
    }

    static async Task<IResult> GetById(Guid orgId, Guid id, IProjectRepository repo, CancellationToken ct)
    {
        var project = await repo.GetByIdAsync(orgId, id, ct);
        if (project is null) return Results.NotFound();
        return Results.Ok(new ProjectResponse(project.Id, project.OrganizationId, project.Name, project.Slug, project.CreatedAt));
    }

    static async Task<IResult> Delete(Guid orgId, Guid id, PulseDbContext db, CancellationToken ct)
    {
        var deleted = await db.Projects.Where(p => p.Id == id && p.OrganizationId == orgId).ExecuteDeleteAsync(ct);
        return deleted > 0 ? Results.NoContent() : Results.NotFound();
    }
}
