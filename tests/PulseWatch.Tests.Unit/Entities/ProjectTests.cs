using FluentAssertions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var orgId = Guid.NewGuid();
        var project = new Project(orgId, "Backend Services", "backend");

        project.Id.Should().NotBeEmpty();
        project.OrganizationId.Should().Be(orgId);
        project.Name.Should().Be("Backend Services");
        project.Slug.Should().Be("backend");
        project.Probes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "slug")]
    [InlineData("  ", "slug")]
    [InlineData("Name", "")]
    [InlineData("Name", "  ")]
    public void Constructor_WithBlankFields_Throws(string name, string slug)
    {
        var act = () => new Project(Guid.NewGuid(), name, slug);
        act.Should().Throw<ArgumentException>();
    }
}
