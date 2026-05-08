namespace PulseWatch.Core.Assertions;

public sealed record EvaluationResult(bool Passed, string? FailureMessage = null)
{
    public static EvaluationResult Pass() => new(true);
    public static EvaluationResult Fail(string message) => new(false, message);
}
