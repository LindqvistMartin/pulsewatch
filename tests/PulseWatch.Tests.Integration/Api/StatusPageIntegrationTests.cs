using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Core.Entities;
using PulseWatch.Infrastructure.Persistence;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class StatusPageIntegrationTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid projectId, Guid probeId, string slug)> SetupStatusPageAsync(string slug)
    {
        var org = await (await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Status Org", $"status-org-{slug}")))
            .Content.ReadFromJsonAsync<OrganizationResponse>();

        var project = await (await _client.PostAsJsonAsync($"/api/v1/organizations/{org!.Id}/projects",
            new CreateProjectRequest("Status Project", $"status-proj-{slug}")))
            .Content.ReadFromJsonAsync<ProjectResponse>();

        var probe = await (await _client.PostAsJsonAsync($"/api/v1/projects/{project!.Id}/probes",
            new CreateProbeRequest("API Health", "https://example.com/health", 30)))
            .Content.ReadFromJsonAsync<ProbeResponse>();

        await _client.PostAsJsonAsync($"/api/v1/projects/{project.Id}/status-pages",
            new CreateStatusPageRequest(slug, "My Status", "Status description", [probe!.Id]));

        return (project.Id, probe.Id, slug);
    }

    [Fact]
    public async Task StatusPage_UnknownSlug_Returns404()
    {
        var response = await _client.GetAsync("/public/status/does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StatusPage_ReturnsNinetyDailyBars()
    {
        var (_, _, slug) = await SetupStatusPageAsync("ninety-bars");

        var response = await _client.GetAsync($"/public/status/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var probes = doc.RootElement.GetProperty("probes");
        probes.GetArrayLength().Should().Be(1);
        var bars = probes[0].GetProperty("dailyBars");
        bars.GetArrayLength().Should().Be(90);
    }

    [Fact]
    public async Task StatusPage_WhenProbeHasRecentFailure_ReflectsDegradedStatus()
    {
        var (_, probeId, slug) = await SetupStatusPageAsync("failure-status");

        // Insert a failed HealthCheck directly (cache is empty for this slug — fresh test)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PulseDbContext>();
        db.HealthChecks.Add(new HealthCheck(probeId, 500, 100, false, "Internal Server Error"));
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/public/status/{slug}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var probeStatus = doc.RootElement.GetProperty("probes")[0].GetProperty("status").GetString();
        probeStatus.Should().Be("Down");
        doc.RootElement.GetProperty("overallStatus").GetString().Should().NotBe("Operational");
    }
}
