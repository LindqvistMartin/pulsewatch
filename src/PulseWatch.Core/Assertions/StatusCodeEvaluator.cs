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

        if (assertion.Operator is not (AssertionOperator.Equals or AssertionOperator.LessThan))
            return EvaluationResult.Fail($"Operator '{assertion.Operator}' is not supported for StatusCode assertions");

        var passed = assertion.Operator == AssertionOperator.Equals
            ? context.StatusCode.Value == expected
            : context.StatusCode.Value < expected;

        return passed
            ? EvaluationResult.Pass()
            : EvaluationResult.Fail($"StatusCode {context.StatusCode} is {OperatorPhrase(assertion.Operator)} {expected}");
    }

    private static string OperatorPhrase(AssertionOperator op) => op switch
    {
        AssertionOperator.Equals   => "not equal to",
        AssertionOperator.LessThan => "not less than",
        _                          => op.ToString()
    };
}
