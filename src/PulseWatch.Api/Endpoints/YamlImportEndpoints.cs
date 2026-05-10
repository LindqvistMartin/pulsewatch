using Microsoft.EntityFrameworkCore;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PulseWatch.Api.Endpoints;

public static class YamlImportEndpoints
{
    public static IEndpointRouteBuilder MapYamlImportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/yaml-import", Import).WithTags("Import");
        return app;
    }

    static async Task<IResult> Import(HttpContext context, PulseDbContext db,
        bool prune = false, CancellationToken ct = default)
    {
        string yaml;
        using (var reader = new StreamReader(context.Request.Body))
            yaml = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(yaml))
            return Results.Problem(detail: "request body is required", statusCode: 400);

        YamlConfig config;
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            config = deserializer.Deserialize<YamlConfig>(yaml)
                ?? throw new InvalidOperationException("empty YAML");
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: $"YAML parse error: {ex.Message}", statusCode: 400);
        }

        var validationError = Validate(config);
        if (validationError is not null)
            return Results.Problem(detail: validationError, statusCode: 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Find existing project by slug; if not found, create org+project
        var project = await db.Projects
            .Include(p => p.Organization)
            .FirstOrDefaultAsync(p => p.Slug == config.Project!.Slug, ct);

        if (project is null)
        {
            var org = new Organization(config.Project!.Name, config.Project.Slug);
            db.Organizations.Add(org);
            await db.SaveChangesAsync(ct);

            project = new Project(org.Id, config.Project!.Name, config.Project.Slug);
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
        }

        // Upsert probes
        var yamlProbeNames = new HashSet<string>(config.Probes?.Select(p => p.Name ?? "") ?? []);
        var existingProbes = await db.Probes
            .Where(p => p.ProjectId == project.Id)
            .ToListAsync(ct);
        var probeByName = existingProbes.ToDictionary(p => p.Name);

        foreach (var yp in config.Probes ?? [])
        {
            if (string.IsNullOrWhiteSpace(yp.Name) || string.IsNullOrWhiteSpace(yp.Url)) continue;
            var intervalSec = ParseDuration(yp.Interval, fallback: 60);

            if (!probeByName.TryGetValue(yp.Name, out var probe))
            {
                probe = new Probe(project.Id, yp.Name, yp.Url, intervalSec);
                db.Probes.Add(probe);
                await db.SaveChangesAsync(ct);
                probeByName[yp.Name] = probe;

                // Add assertions
                foreach (var assertion in yp.Assertions ?? [])
                {
                    var pa = BuildAssertion(probe.Id, assertion);
                    if (pa is not null) db.ProbeAssertions.Add(pa);
                }
                await db.SaveChangesAsync(ct);
            }
        }

        // Upsert SLOs
        foreach (var ys in config.Slos ?? [])
        {
            if (!probeByName.TryGetValue(ys.Probe ?? "", out var probe)) continue;
            var windowDays = ParseWindowDays(ys.Window, fallback: 30);

            var existing = await db.SloDefinitions.FirstOrDefaultAsync(
                s => s.ProbeId == probe.Id && s.WindowDays == windowDays, ct);
            if (existing is null)
            {
                db.SloDefinitions.Add(new SloDefinition(probe.Id, ys.TargetAvailability, windowDays));
                await db.SaveChangesAsync(ct);
            }
        }

        // Upsert status pages
        foreach (var ysp in config.StatusPages ?? [])
        {
            if (string.IsNullOrWhiteSpace(ysp.Slug) || string.IsNullOrWhiteSpace(ysp.Title)) continue;
            var probeIds = (ysp.Probes ?? [])
                .Select(name => probeByName.GetValueOrDefault(name))
                .Where(p => p is not null)
                .Select(p => p!.Id)
                .ToList();

            var existing = await db.StatusPages.FirstOrDefaultAsync(sp => sp.Slug == ysp.Slug, ct);
            if (existing is null)
            {
                db.StatusPages.Add(new StatusPage(project.Id, ysp.Slug, ysp.Title, ysp.Description ?? "", probeIds));
                await db.SaveChangesAsync(ct);
            }
        }

        // Prune: delete probes not in YAML
        if (prune)
        {
            var toDelete = existingProbes.Where(p => !yamlProbeNames.Contains(p.Name)).Select(p => p.Id).ToList();
            if (toDelete.Count > 0)
                await db.Probes.Where(p => toDelete.Contains(p.Id)).ExecuteDeleteAsync(ct);
        }

        await tx.CommitAsync(ct);

        return Results.Ok(new { message = "import successful", projectId = project.Id });
    }

    static string? Validate(YamlConfig? config)
    {
        if (config is null) return "version is required";
        if (config.Version != 1) return "version must be 1";
        if (config.Project is null) return "project is required";
        if (string.IsNullOrWhiteSpace(config.Project.Name)) return "project.name is required";
        if (string.IsNullOrWhiteSpace(config.Project.Slug)) return "project.slug is required";
        return null;
    }

    static int ParseDuration(string? s, int fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        if (s.EndsWith('s') && int.TryParse(s[..^1], out var sec)) return Math.Max(sec, 15);
        if (s.EndsWith('m') && int.TryParse(s[..^1], out var min)) return min * 60;
        if (int.TryParse(s, out var direct)) return Math.Max(direct, 15);
        return fallback;
    }

    static int ParseWindowDays(string? s, int fallback)
    {
        if (string.IsNullOrWhiteSpace(s)) return fallback;
        if (s.EndsWith('d') && int.TryParse(s[..^1], out var days)) return days;
        if (int.TryParse(s, out var direct)) return direct;
        return fallback;
    }

    static ProbeAssertion? BuildAssertion(Guid probeId, YamlAssertion a)
    {
        if (a.Status.HasValue)
            return new ProbeAssertion(probeId, AssertionType.StatusCode, AssertionOperator.Equals,
                a.Status.Value.ToString());
        if (a.LatencyP95Ms.HasValue)
            return new ProbeAssertion(probeId, AssertionType.LatencyMs, AssertionOperator.LessThan,
                a.LatencyP95Ms.Value.ToString());
        if (!string.IsNullOrWhiteSpace(a.BodyJsonpath) && !string.IsNullOrWhiteSpace(a.EqualsValue))
            return new ProbeAssertion(probeId, AssertionType.JsonPath, AssertionOperator.Equals,
                a.EqualsValue, a.BodyJsonpath);
        return null;
    }
}

// YAML model POCOs
internal sealed class YamlConfig
{
    public int Version { get; set; }
    public YamlProject? Project { get; set; }
    public List<YamlProbe>? Probes { get; set; }
    public List<YamlSlo>? Slos { get; set; }
    public List<YamlStatusPage>? StatusPages { get; set; }
}

internal sealed class YamlProject
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
}

internal sealed class YamlProbe
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Interval { get; set; }
    public List<YamlAssertion>? Assertions { get; set; }
}

internal sealed class YamlAssertion
{
    public int? Status { get; set; }
    [YamlMember(Alias = "latency_p95_ms")]
    public int? LatencyP95Ms { get; set; }
    [YamlMember(Alias = "body_jsonpath")]
    public string? BodyJsonpath { get; set; }
    [YamlMember(Alias = "equals")]
    public string? EqualsValue { get; set; }
}

internal sealed class YamlSlo
{
    public string? Probe { get; set; }
    [YamlMember(Alias = "target_availability")]
    public double TargetAvailability { get; set; } = 99.9;
    [YamlMember(Alias = "target_latency_p95_ms")]
    public int? TargetLatencyP95Ms { get; set; }
    public string? Window { get; set; }
}

internal sealed class YamlStatusPage
{
    public string? Slug { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? Probes { get; set; }
}
