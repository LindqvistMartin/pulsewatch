using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class IncidentTests
{
    [Fact]
    public void Constructor_SetsOpenedAtAndIsOpen()
    {
        var probeId = Guid.NewGuid();

        var incident = new Incident(probeId, "Availability dropped", autoDetected: true);

        incident.Id.Should().NotBeEmpty();
        incident.ProbeId.Should().Be(probeId);
        incident.Reason.Should().Be("Availability dropped");
        incident.AutoDetected.Should().BeTrue();
        incident.OpenedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        incident.ClosedAt.Should().BeNull();
        incident.IsOpen.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsIfReasonIsWhitespace(string reason)
    {
        var act = () => new Incident(Guid.NewGuid(), reason, autoDetected: true);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Close_SetsClosedAtAndIsOpenFalse()
    {
        var incident = new Incident(Guid.NewGuid(), "Test reason", autoDetected: false);

        incident.Close();

        incident.ClosedAt.Should().NotBeNull();
        incident.IsOpen.Should().BeFalse();
        incident.ClosedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
