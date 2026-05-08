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

        try
        {
            var node = JsonNode.Parse(context.Body);
            var path = JsonPath.Parse(assertion.JsonPathExpression);
            var result = path.Evaluate(node);

            if (!result.Matches.Any())
                return EvaluationResult.Fail($"JsonPath '{assertion.JsonPathExpression}' matched nothing");

            var actual = result.Matches[0].Value?.ToString();
            var passed = assertion.Operator switch
            {
                AssertionOperator.Equals   => actual == assertion.ExpectedValue,
                AssertionOperator.Contains => actual?.Contains(assertion.ExpectedValue) == true,
                _                          => false
            };

            return passed
                ? EvaluationResult.Pass()
                : EvaluationResult.Fail($"JsonPath value '{actual}' {assertion.Operator} '{assertion.ExpectedValue}' failed");
        }
        catch (Exception ex) when (ex is JsonException or PathParseException)
        {
            return EvaluationResult.Fail($"JsonPath evaluation error: {ex.Message}");
        }
    }
}
