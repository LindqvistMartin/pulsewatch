using PulseWatch.Core.Services;

namespace PulseWatch.Tests.Unit.Slo;

public class SloMathTests
{
    [Fact]
    public void Availability_99Checks_1Failure_Is99pct()
    {
        var result = SloMath.ComputeAvailability(total: 100, success: 99);
        result.Should().BeApproximately(99.0, 0.001);
    }

    [Fact]
    public void Availability_ZeroChecks_Returns100()
    {
        var result = SloMath.ComputeAvailability(total: 0, success: 0);
        result.Should().Be(100.0);
    }

    [Fact]
    public void BurnRate_AtTarget_Is1()
    {
        // 99.9% target, 30d window: budget = 0.001 * 2592000 = 2592s
        // 100 checks, 0 failures → consumed = 0 → burn = 0
        // For burn=1: consumed == total
        // consumed = (1 - 99.9/100) * windowSeconds = 2592
        // total  = same as budget for burn = 1
        var windowSeconds = 30 * 86400.0;
        var budgetTotal = SloMath.ComputeErrorBudgetTotal(99.9, windowSeconds);
        var burnRate = SloMath.ComputeBurnRate(consumed: budgetTotal, total: budgetTotal);
        burnRate.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void BurnRate_TwiceExhaustion_Is2()
    {
        var windowSeconds = 30 * 86400.0;
        var budgetTotal = SloMath.ComputeErrorBudgetTotal(99.9, windowSeconds);
        var burnRate = SloMath.ComputeBurnRate(consumed: budgetTotal * 2, total: budgetTotal);
        burnRate.Should().BeApproximately(2.0, 0.001);
    }

    [Fact]
    public void ProjectedExhaustion_HalfConsumed_IsHalfWindow()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var windowSeconds = 30 * 86400.0;
        var budgetTotal = SloMath.ComputeErrorBudgetTotal(99.9, windowSeconds);

        // Half consumed → half the window remaining before exhaustion
        var result = SloMath.ComputeProjectedExhaustion(now, consumed: budgetTotal / 2, total: budgetTotal, windowSeconds);

        result.Should().NotBeNull();
        var expectedHours = windowSeconds / 2 / 3600;
        result!.Value.Should().BeCloseTo(now.AddSeconds(windowSeconds / 2), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ProjectedExhaustion_ZeroBurnRate_ReturnsNull()
    {
        var result = SloMath.ComputeProjectedExhaustion(
            DateTime.UtcNow, consumed: 0, total: 100, windowSeconds: 86400);
        result.Should().BeNull();
    }

    [Fact]
    public void ComputeErrorBudgetTotal_99_9pctTarget_30dWindow_CorrectSeconds()
    {
        var windowSeconds = 30 * 86400.0; // 2,592,000s
        var result = SloMath.ComputeErrorBudgetTotal(99.9, windowSeconds);
        // budget = (1 - 0.999) * 2592000 = 2592s
        result.Should().BeApproximately(2592.0, 0.1);
    }

    [Fact]
    public void ComputeErrorBudgetConsumed_ZeroFailures_ReturnsZero()
    {
        var result = SloMath.ComputeErrorBudgetConsumed(total: 100, success: 100, windowSeconds: 86400);
        result.Should().Be(0.0);
    }
}
