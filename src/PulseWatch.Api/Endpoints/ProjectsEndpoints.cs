using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Abstractions;
using PulseWatch.Core.Entities;

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
        await repo.AddAsync(project, ct);
        return Results.Created($"/api/v1/organizations/{orgId}/projects/{project.Id}",
            new ProjectResponse(project.Id, project.OrganizationId, project.Name, project.Slug, project.CreatedAt));
    }

    static async Task<IResult> GetById(Guid orgId, Guid id, IProjectRepository repo, CancellationToken ct)
    {
        var project = await repo.GetByIdAsync(orgId, id, ct);
        if (project is null) return Results.NotFound();
        return Results.Ok(new ProjectResponse(project.Id, project.OrganizationId, project.Name, project.Slug, project.CreatedAt));
    }
}
