using FluentAssertions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class OutboxMessageTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var msg = new OutboxMessage("HealthCheckRecorded", """{"probeId":"abc","isSuccess":true}""");

        msg.Id.Should().NotBeEmpty();
        msg.Type.Should().Be("HealthCheckRecorded");
        msg.Payload.Should().Contain("probeId");
        msg.ProcessedAt.Should().BeNull();
        msg.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", """{}""")]
    [InlineData("  ", """{}""")]
    [InlineData("Type", "")]
    [InlineData("Type", "   ")]
    public void Constructor_WithBlankFields_Throws(string type, string payload)
    {
        var act = () => new OutboxMessage(type, payload);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessedAt_CanBeSetAfterConstruction()
    {
        var msg = new OutboxMessage("Test", "{}");
        var now = DateTime.UtcNow;

        msg.ProcessedAt = now;

        msg.ProcessedAt.Should().Be(now);
    }
}
