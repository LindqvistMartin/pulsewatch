using FluentAssertions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Entities;

public class HealthCheckTests
{
    [Fact]
    public void Constructor_SuccessfulCheck_SetsProperties()
    {
        var probeId = Guid.NewGuid();
        var check = new HealthCheck(probeId, 200, 142, true);

        check.Id.Should().NotBeEmpty();
        check.ProbeId.Should().Be(probeId);
        check.StatusCode.Should().Be(200);
        check.ResponseTimeMs.Should().Be(142);
        check.IsSuccess.Should().BeTrue();
        check.FailureReason.Should().BeNull();
        check.CheckedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_FailedCheck_StoresReason()
    {
        var check = new HealthCheck(Guid.NewGuid(), null, 10_000, false, "Connection refused");

        check.IsSuccess.Should().BeFalse();
        check.StatusCode.Should().BeNull();
        check.FailureReason.Should().Be("Connection refused");
    }

    [Fact]
    public void Constructor_WithStatusCode500_CanBeUnsuccessful()
    {
        var check = new HealthCheck(Guid.NewGuid(), 500, 88, false, "Internal Server Error");

        check.StatusCode.Should().Be(500);
        check.IsSuccess.Should().BeFalse();
    }
}
