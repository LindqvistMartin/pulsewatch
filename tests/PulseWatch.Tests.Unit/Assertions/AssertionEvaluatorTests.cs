using PulseWatch.Core.Assertions;
using PulseWatch.Core.Entities;

namespace PulseWatch.Tests.Unit.Assertions;

public class AssertionEvaluatorTests
{
    // ── StatusCode evaluator ─────────────────────────────────────────────
    [Theory]
    [InlineData(200, "200", AssertionOperator.Equals, true)]
    [InlineData(201, "200", AssertionOperator.Equals, false)]
    [InlineData(404, "200", AssertionOperator.Equals, false)]
    public void StatusCode_Equals(int actual, string expected, AssertionOperator op, bool pass)
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.StatusCode, op, expected);
        var ctx = new AssertionContext(actual, 100, null);
        new StatusCodeEvaluator().Evaluate(assertion, ctx).Passed.Should().Be(pass);
    }

    [Fact]
    public void StatusCode_NullCode_Fails()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.StatusCode, AssertionOperator.Equals, "200");
        var ctx = new AssertionContext(null, 0, null);
        new StatusCodeEvaluator().Evaluate(assertion, ctx).Passed.Should().BeFalse();
    }

    // ── Latency evaluator ────────────────────────────────────────────────
    [Theory]
    [InlineData(99,  "100", AssertionOperator.LessThan, true)]
    [InlineData(100, "100", AssertionOperator.LessThan, false)]
    [InlineData(100, "100", AssertionOperator.Equals,   true)]
    [InlineData(101, "100", AssertionOperator.Equals,   false)]
    public void Latency_Operators(long actualMs, string expected, AssertionOperator op, bool pass)
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.LatencyMs, op, expected);
        var ctx = new AssertionContext(200, actualMs, null);
        new LatencyEvaluator().Evaluate(assertion, ctx).Passed.Should().Be(pass);
    }

    // ── BodyRegex evaluator ──────────────────────────────────────────────
    [Theory]
    [InlineData("""{"status":"ok"}""", "\"status\":\"ok\"", AssertionOperator.Contains, true)]
    [InlineData("Hello World", "Goodbye",                   AssertionOperator.Contains, false)]
    [InlineData("abc 123",     @"\d+",                      AssertionOperator.Contains, true)]
    public void BodyRegex_Contains(string body, string pattern, AssertionOperator op, bool pass)
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.BodyRegex, op, pattern);
        var ctx = new AssertionContext(200, 50, body);
        new BodyRegexEvaluator().Evaluate(assertion, ctx).Passed.Should().Be(pass);
    }

    [Fact]
    public void BodyRegex_NullBody_Fails()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.BodyRegex, AssertionOperator.Contains, "ok");
        var ctx = new AssertionContext(200, 50, null);
        new BodyRegexEvaluator().Evaluate(assertion, ctx).Passed.Should().BeFalse();
    }

    // ── JsonPath evaluator ───────────────────────────────────────────────
    [Fact]
    public void JsonPath_Equals_Match()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.Equals, "ok", jsonPath: "$.status");
        var ctx = new AssertionContext(200, 50, """{"status":"ok"}""");
        new JsonPathEvaluator().Evaluate(assertion, ctx).Passed.Should().BeTrue();
    }

    [Fact]
    public void JsonPath_Equals_NoMatch()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.Equals, "ok", jsonPath: "$.status");
        var ctx = new AssertionContext(200, 50, """{"status":"error"}""");
        new JsonPathEvaluator().Evaluate(assertion, ctx).Passed.Should().BeFalse();
    }

    [Fact]
    public void JsonPath_NullBody_Fails()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.Equals, "ok", jsonPath: "$.status");
        var ctx = new AssertionContext(200, 50, null);
        new JsonPathEvaluator().Evaluate(assertion, ctx).Passed.Should().BeFalse();
    }

    // ── Factory routing ──────────────────────────────────────────────────
    [Theory]
    [InlineData(AssertionType.StatusCode, typeof(StatusCodeEvaluator))]
    [InlineData(AssertionType.LatencyMs,  typeof(LatencyEvaluator))]
    [InlineData(AssertionType.BodyRegex,  typeof(BodyRegexEvaluator))]
    [InlineData(AssertionType.JsonPath,   typeof(JsonPathEvaluator))]
    public void Factory_ReturnsCorrectEvaluator(AssertionType type, Type expected)
    {
        new AssertionEvaluatorFactory().Get(type).Should().BeOfType(expected);
    }

    // ── All-pass through factory ─────────────────────────────────────────
    [Fact]
    public void AllAssertions_Pass_WhenConditionsMet()
    {
        var probeId = Guid.NewGuid();
        var assertions = new List<ProbeAssertion>
        {
            new(probeId, AssertionType.StatusCode, AssertionOperator.Equals, "200"),
            new(probeId, AssertionType.LatencyMs,  AssertionOperator.LessThan, "500"),
        };
        var ctx = new AssertionContext(200, 100, null);
        var factory = new AssertionEvaluatorFactory();

        assertions.Select(a => factory.Get(a.Type).Evaluate(a, ctx))
            .Should().AllSatisfy(r => r.Passed.Should().BeTrue());
    }
}
