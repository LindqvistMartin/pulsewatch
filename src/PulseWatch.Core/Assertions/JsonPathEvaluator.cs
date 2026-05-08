using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class JsonPathEvaluator : IAssertionEvaluator
{
    public EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context)
    {
        if (context.Body is null)
            return EvaluationResult.Fail("Response body is null");

        if (assertion.JsonPathExpression is null)
            return EvaluationResult.Fail("JsonPath expression is not configured");

        if (assertion.Operator is not (AssertionOperator.Equals or AssertionOperator.Contains))
            return EvaluationResult.Fail($"Operator '{assertion.Operator}' is not supported for JsonPath assertions");

        try
        {
            var node = JsonNode.Parse(context.Body);
            if (node is null)
                return EvaluationResult.Fail("Response body is JSON null");

            var path = JsonPath.Parse(assertion.JsonPathExpression);
            var result = path.Evaluate(node);

            if (!result.Matches.Any())
                return EvaluationResult.Fail($"JsonPath '{assertion.JsonPathExpression}' matched nothing");

            // Extract string value correctly for all JSON types:
            // JSON string "ok"  → "ok"  (without quotes)
            // JSON number 1     → "1"
            // JSON bool true    → "true"
            var matchedNode = result.Matches[0].Value;
            string? actual = matchedNode is JsonValue jv && jv.TryGetValue<string>(out var s)
                ? s
                : matchedNode?.ToJsonString();

            var passed = assertion.Operator == AssertionOperator.Equals
                ? actual == assertion.ExpectedValue
                : actual?.Contains(assertion.ExpectedValue) == true;

            return passed
                ? EvaluationResult.Pass()
                : EvaluationResult.Fail($"JsonPath value '{actual}' is not {assertion.Operator.ToString().ToLower()} '{assertion.ExpectedValue}'");
        }
        catch (Exception ex) when (ex is JsonException or PathParseException)
        {
            return EvaluationResult.Fail($"JsonPath evaluation error: {ex.Message}");
        }
    }
}
