using FluentAssertions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class OrganizationTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var org = new Organization("Acme Corp", "acme");

        org.Id.Should().NotBeEmpty();
        org.Name.Should().Be("Acme Corp");
        org.Slug.Should().Be("acme");
        org.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        org.Projects.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "acme")]
    [InlineData("  ", "acme")]
    [InlineData("Acme", "")]
    [InlineData("Acme", "  ")]
    public void Constructor_WithBlankFields_Throws(string name, string slug)
    {
        var act = () => new Organization(name, slug);
        act.Should().Throw<ArgumentException>();
    }
}
