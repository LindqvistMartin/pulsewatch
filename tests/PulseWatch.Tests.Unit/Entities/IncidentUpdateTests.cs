using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class IncidentUpdateTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var incidentId = Guid.NewGuid();

        var update = new IncidentUpdate(incidentId, IncidentStatus.Investigating, "Looking into it");

        update.Id.Should().NotBeEmpty();
        update.IncidentId.Should().Be(incidentId);
        update.Status.Should().Be(IncidentStatus.Investigating);
        update.Message.Should().Be("Looking into it");
        update.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsIfMessageIsWhitespace(string message)
    {
        var act = () => new IncidentUpdate(Guid.NewGuid(), IncidentStatus.Resolved, message);
        act.Should().Throw<ArgumentException>();
    }
}
