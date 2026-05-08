using System.Text.RegularExpressions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class BodyRegexEvaluator : IAssertionEvaluator
{
    public EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context)
    {
        if (context.Body is null)
            return EvaluationResult.Fail("Response body is null");

        try
        {
            var isMatch = Regex.IsMatch(context.Body, assertion.ExpectedValue,
                RegexOptions.None, TimeSpan.FromSeconds(1));

            return isMatch
                ? EvaluationResult.Pass()
                : EvaluationResult.Fail($"Body did not match pattern '{assertion.ExpectedValue}'");
        }
        catch (RegexMatchTimeoutException)
        {
            return EvaluationResult.Fail("Regex evaluation timed out");
        }
    }
}
