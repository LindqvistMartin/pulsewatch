using System.Text.RegularExpressions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class BodyRegexEvaluator : IAssertionEvaluator
{
    public EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context)
    {
        if (context.Body is null)
            return EvaluationResult.Fail("Response body is null");

        // Equals: pattern must match the entire body; Contains: partial match anywhere.
        // Use \A/\z (absolute start/end) not ^/$ — in .NET, $ matches before a trailing \n,
        // so "ok\n" would pass ^ok$ despite the extra character.
        var pattern = assertion.Operator == AssertionOperator.Equals
            ? $@"\A(?:{assertion.ExpectedValue})\z"
            : assertion.ExpectedValue;

        try
        {
            var isMatch = Regex.IsMatch(context.Body, pattern,
                RegexOptions.None, TimeSpan.FromSeconds(1));

            return isMatch
                ? EvaluationResult.Pass()
                : EvaluationResult.Fail($"Body did not match pattern '{assertion.ExpectedValue}'");
        }
        catch (RegexMatchTimeoutException)
        {
            return EvaluationResult.Fail("Regex evaluation timed out");
        }
        catch (RegexParseException ex)
        {
            return EvaluationResult.Fail($"Invalid regex pattern: {ex.Message}");
        }
    }
}
