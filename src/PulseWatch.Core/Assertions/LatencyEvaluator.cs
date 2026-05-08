using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class LatencyEvaluator : IAssertionEvaluator
{
    public EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context)
    {
        if (!long.TryParse(assertion.ExpectedValue, out var expected))
            return EvaluationResult.Fail($"Invalid expected value '{assertion.ExpectedValue}'");

        if (assertion.Operator is not (AssertionOperator.Equals or AssertionOperator.LessThan))
            return EvaluationResult.Fail($"Operator '{assertion.Operator}' is not supported for LatencyMs assertions");

        var passed = assertion.Operator == AssertionOperator.Equals
            ? context.ResponseTimeMs == expected
            : context.ResponseTimeMs < expected;

        return passed
            ? EvaluationResult.Pass()
            : EvaluationResult.Fail($"Latency {context.ResponseTimeMs}ms is {OperatorPhrase(assertion.Operator)} {expected}ms");
    }

    private static string OperatorPhrase(AssertionOperator op) => op switch
    {
        AssertionOperator.Equals   => "not equal to",
        AssertionOperator.LessThan => "not less than",
        _                          => op.ToString()
    };
}
