using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class LatencyEvaluator : IAssertionEvaluator
{
    public EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context)
    {
        if (!long.TryParse(assertion.ExpectedValue, out var expected))
            return EvaluationResult.Fail($"Invalid expected value '{assertion.ExpectedValue}'");

        var passed = assertion.Operator switch
        {
            AssertionOperator.Equals   => context.ResponseTimeMs == expected,
            AssertionOperator.LessThan => context.ResponseTimeMs < expected,
            _                          => false
        };

        return passed
            ? EvaluationResult.Pass()
            : EvaluationResult.Fail($"Latency {context.ResponseTimeMs}ms {assertion.Operator} {expected}ms failed");
    }
}
