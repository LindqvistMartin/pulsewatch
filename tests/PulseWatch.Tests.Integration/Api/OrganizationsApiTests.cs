using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PulseWatch.Api.Contracts.Requests;
using PulseWatch.Api.Contracts.Responses;
using PulseWatch.Tests.Integration.Infrastructure;

namespace PulseWatch.Tests.Integration.Api;

[Collection("Api")]
public class OrganizationsApiTests(ApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.CleanAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_CreatesOrganizationAndReturns201()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Acme Corp", "acme"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<OrganizationResponse>();
        body!.Id.Should().NotBeEmpty();
        body.Name.Should().Be("Acme Corp");
        body.Slug.Should().Be("acme");
        response.Headers.Location!.ToString().Should().Contain(body.Id.ToString());
    }

    [Fact]
    public async Task Get_AfterCreate_ReturnsOrganization()
    {
        await _client.PostAsJsonAsync("/api/v1/organizations", new CreateOrganizationRequest("Widgets Inc", "widgets"));

        var response = await _client.GetAsync("/api/v1/organizations");
        var orgs = await response.Content.ReadFromJsonAsync<List<OrganizationResponse>>();

        orgs.Should().HaveCount(1);
        orgs![0].Slug.Should().Be("widgets");
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/organizations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithBlankName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("", "valid-slug"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithBlankSlug_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/organizations",
            new CreateOrganizationRequest("Valid Name", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
