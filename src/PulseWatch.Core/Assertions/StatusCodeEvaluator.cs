using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class StatusCodeEvaluator : IAssertionEvaluator
{
    public EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context)
    {
        if (context.StatusCode is null)
            return EvaluationResult.Fail("No status code (connection failed)");

        if (!int.TryParse(assertion.ExpectedValue, out var expected))
            return EvaluationResult.Fail($"Invalid expected value '{assertion.ExpectedValue}'");

        var passed = assertion.Operator switch
        {
            AssertionOperator.Equals   => context.StatusCode.Value == expected,
            AssertionOperator.LessThan => context.StatusCode.Value < expected,
            _                          => false
        };

        return passed
            ? EvaluationResult.Pass()
            : EvaluationResult.Fail($"StatusCode {context.StatusCode} {assertion.Operator} {expected} failed");
    }
}
