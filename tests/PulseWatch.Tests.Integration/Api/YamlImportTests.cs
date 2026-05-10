using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class YamlImportTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly string ValidYaml = """
        version: 1
        project:
          name: Import Test
          slug: import-test
        probes:
          - name: API
            url: https://example.com/health
            interval: 30s
        """;

    private static HttpContent YamlContent(string yaml) =>
        new StringContent(yaml, System.Text.Encoding.UTF8, "text/yaml");

    [Fact]
    public async Task YamlImport_CalledTwiceWithSameConfig_DoesNotCreateDuplicates()
    {
        await _client.PostAsync("/api/v1/yaml-import", YamlContent(ValidYaml));
        var response = await _client.PostAsync("/api/v1/yaml-import", YamlContent(ValidYaml));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var org = await db.Organizations.FirstOrDefaultAsync(o => o.Slug == "import-test");
        org.Should().NotBeNull();

        var probeCount = await db.Probes
            .Where(p => p.Project.Organization.Slug == "import-test" && p.Name == "API")
            .CountAsync();
        probeCount.Should().Be(1);
    }

    [Fact]
    public async Task YamlImport_WithPruneTrue_RemovesProbesNotInYaml()
    {
        // Create a probe via API first
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Prune Org", "prune-org")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();
        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("Prune Proj", "prune-proj")))
            .Content.ReadFromJsonAsync<ProjectResponse>();
        await _client.PostAsJsonAsync($"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest("ToDelete", "https://example.com/todelete", 30));

        // Import YAML (for a different project) that does NOT contain "ToDelete"
        var yamlWithoutProbe = """
            version: 1
            project:
              name: Prune Proj
              slug: prune-proj
            probes:
              - name: API
                url: https://example.com/health
                interval: 30s
            """;

        var response = await _client.PostAsync("/api/v1/yaml-import?prune=true", YamlContent(yamlWithoutProbe));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        var remaining = await db.Probes.Where(p => p.ProjectId == project.Id).ToListAsync();
        remaining.Should().NotContain(p => p.Name == "ToDelete");
        remaining.Should().Contain(p => p.Name == "API");
    }

    [Fact]
    public async Task YamlImport_InvalidYaml_Returns400WithValidationDetail()
    {
        var invalidYaml = """
            version: 2
            probes:
              - name: API
            """;

        var response = await _client.PostAsync("/api/v1/yaml-import", YamlContent(invalidYaml));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("version");
    }
}
