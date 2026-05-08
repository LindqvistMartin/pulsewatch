using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class SloDefinitionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var probeId = Guid.NewGuid();

        var slo = new SloDefinition(probeId, 99.9, 30, targetLatencyP95Ms: 500);

        slo.Id.Should().NotBeEmpty();
        slo.ProbeId.Should().Be(probeId);
        slo.TargetAvailabilityPct.Should().Be(99.9);
        slo.WindowDays.Should().Be(30);
        slo.TargetLatencyP95Ms.Should().Be(500);
        slo.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_ThrowsIfTargetBelow0()
    {
        var act = () => new SloDefinition(Guid.NewGuid(), -0.1, 7);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ThrowsIfTargetAbove100()
    {
        var act = () => new SloDefinition(Guid.NewGuid(), 100.1, 7);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_ThrowsIfWindowDaysLessThan1()
    {
        var act = () => new SloDefinition(Guid.NewGuid(), 99.9, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_AcceptsNullLatencyTarget()
    {
        var act = () => new SloDefinition(Guid.NewGuid(), 99.9, 7, targetLatencyP95Ms: null);
        act.Should().NotThrow();
    }
}
