namespace PulseWatch.Core.Services;

public static class SloMath
{
    public static double ComputeAvailability(long total, long success)
    {
        if (total == 0) return 100.0;
        return (double)success / total * 100.0;
    }

    public static double ComputeErrorBudgetTotal(double targetAvailabilityPct, double windowSeconds)
        => (1.0 - targetAvailabilityPct / 100.0) * windowSeconds;

    public static double ComputeErrorBudgetConsumed(long total, long success, double windowSeconds)
    {
        if (total == 0) return 0.0;
        return (double)(total - success) / total * windowSeconds;
    }

    public static double ComputeBurnRate(double consumed, double total)
    {
        if (total == 0) return 0.0;
        return consumed / total;
    }

    public static DateTime? ComputeProjectedExhaustion(
        DateTime now, double consumed, double total, double windowSeconds)
    {
        var burnRate = ComputeBurnRate(consumed, total);
        if (burnRate <= 0) return null;
        return now.AddSeconds((1.0 - burnRate) * windowSeconds);
    }
}
