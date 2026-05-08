using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Json.Path;
using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class JsonPathEvaluator : IAssertionEvaluator
{
    public EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context)
    {
        // Operator guard first — mirrors BodyRegexEvaluator ordering so misconfigured
        // assertions report the configuration mistake, not a runtime symptom.
        if (assertion.Operator is not (AssertionOperator.Equals or AssertionOperator.Contains))
            return EvaluationResult.Fail($"Operator '{assertion.Operator}' is not supported for JsonPath assertions");

        if (assertion.JsonPathExpression is null)
            return EvaluationResult.Fail("JsonPath expression is not configured");

        if (context.Body is null)
            return EvaluationResult.Fail("Response body is null");

        try
        {
            var node = JsonNode.Parse(context.Body);
            if (node is null)
                return EvaluationResult.Fail("Response body is JSON null");

            var path = JsonPath.Parse(assertion.JsonPathExpression);
            var result = path.Evaluate(node);

            if (!result.Matches.Any())
                return EvaluationResult.Fail($"JsonPath '{assertion.JsonPathExpression}' matched nothing");

            // Reject multi-match paths — silently checking only [0] would mask partial failures.
            if (result.Matches.Count > 1)
                return EvaluationResult.Fail(
                    $"JsonPath '{assertion.JsonPathExpression}' matched {result.Matches.Count} values; use a more specific path");

            // Extract string value correctly for all JSON types:
            // JSON string "ok"  → "ok"  (without quotes)
            // JSON number 1     → "1"
            // JSON bool true    → "true"
            // JSON null         → "null"
            var matchedNode = result.Matches[0].Value;
            string actual = matchedNode is null
                ? "null"
                : matchedNode is JsonValue jv && jv.TryGetValue<string>(out var s)
                    ? s
                    : matchedNode.ToJsonString();

            if (assertion.Operator == AssertionOperator.Equals)
            {
                return actual == assertion.ExpectedValue
                    ? EvaluationResult.Pass()
                    : EvaluationResult.Fail($"JsonPath value '{actual}' is not equal to '{assertion.ExpectedValue}'");
            }

            // Contains: use regex, consistent with BodyRegexEvaluator.Contains behaviour.
            try
            {
                var matched = Regex.IsMatch(actual, assertion.ExpectedValue,
                    RegexOptions.None, TimeSpan.FromSeconds(1));
                return matched
                    ? EvaluationResult.Pass()
                    : EvaluationResult.Fail($"JsonPath value '{actual}' does not match pattern '{assertion.ExpectedValue}'");
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EvaluationResult.Fail($"JsonPath evaluation error: {ex.Message}");
        }
    }
}
