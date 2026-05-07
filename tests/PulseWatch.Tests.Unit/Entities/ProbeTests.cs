using FluentAssertions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class ProbeTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var probe = new Probe(Guid.NewGuid(), "API Health", "https://api.example.com/health", 30);

        probe.Id.Should().NotBeEmpty();
        probe.Method.Should().Be("GET");
        probe.TimeoutSeconds.Should().Be(10);
        probe.IsActive.Should().BeTrue();
        probe.LastCheckedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string name)
    {
        var act = () => new Probe(Guid.NewGuid(), name, "https://example.com", 30);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankUrl_Throws(string url)
    {
        var act = () => new Probe(Guid.NewGuid(), "Test", url, 30);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithIntervalBelow15_Throws()
    {
        var act = () => new Probe(Guid.NewGuid(), "Test", "https://example.com", 14);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithExactly15Seconds_DoesNotThrow()
    {
        var act = () => new Probe(Guid.NewGuid(), "Test", "https://example.com", 15);
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordChecked_SetsLastCheckedAt()
    {
        var probe = new Probe(Guid.NewGuid(), "Test", "https://example.com", 30);
        var before = DateTime.UtcNow.AddSeconds(-1);

        probe.RecordChecked();

        probe.LastCheckedAt.Should().NotBeNull()
            .And.BeAfter(before);
    }

    [Fact]
    public void RecordChecked_CalledTwice_UpdatesTimestamp()
    {
        var probe = new Probe(Guid.NewGuid(), "Test", "https://example.com", 30);
        probe.RecordChecked();
        var first = probe.LastCheckedAt!.Value;
        Thread.Sleep(10);

        probe.RecordChecked();

        probe.LastCheckedAt.Should().BeAfter(first);
    }
}
