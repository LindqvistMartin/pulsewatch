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
        new StatusCodeEvaluator().Evaluate(assertion, new AssertionContext(null, 0, null)).Passed.Should().BeFalse();
    }

    [Fact]
    public void StatusCode_UnsupportedOperator_ReturnsFailWithMessage()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.StatusCode, AssertionOperator.Contains, "200");
        var result = new StatusCodeEvaluator().Evaluate(assertion, new AssertionContext(200, 0, null));
        result.Passed.Should().BeFalse();
        result.FailureMessage.Should().Contain("not supported");
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
        new LatencyEvaluator().Evaluate(assertion, new AssertionContext(200, actualMs, null)).Passed.Should().Be(pass);
    }

    [Fact]
    public void Latency_UnsupportedOperator_ReturnsFailWithMessage()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.LatencyMs, AssertionOperator.Contains, "100");
        var result = new LatencyEvaluator().Evaluate(assertion, new AssertionContext(200, 50, null));
        result.Passed.Should().BeFalse();
        result.FailureMessage.Should().Contain("not supported");
    }

    // ── BodyRegex evaluator ──────────────────────────────────────────────
    [Theory]
    [InlineData("""{"status":"ok"}""", "\"status\":\"ok\"", AssertionOperator.Contains, true)]
    [InlineData("Hello World", "Goodbye",                   AssertionOperator.Contains, false)]
    [InlineData("abc 123",     @"\d+",                      AssertionOperator.Contains, true)]
    public void BodyRegex_Contains_PartialMatch(string body, string pattern, AssertionOperator op, bool pass)
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.BodyRegex, op, pattern);
        new BodyRegexEvaluator().Evaluate(assertion, new AssertionContext(200, 50, body)).Passed.Should().Be(pass);
    }

    [Theory]
    [InlineData("ok",             "ok", AssertionOperator.Equals,   true)]   // pattern matches entire body
    [InlineData("ok extra stuff", "ok", AssertionOperator.Equals,   false)]  // partial match but Equals requires whole body
    [InlineData("ok extra stuff", "ok", AssertionOperator.Contains, true)]   // Contains: partial match is fine
    [InlineData("ok\n",           "ok", AssertionOperator.Equals,   false)]  // trailing \n: $ would pass, \z must not
    public void BodyRegex_Equals_RequiresFullMatch(string body, string pattern, AssertionOperator op, bool pass)
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.BodyRegex, op, pattern);
        new BodyRegexEvaluator().Evaluate(assertion, new AssertionContext(200, 50, body)).Passed.Should().Be(pass);
    }

    [Fact]
    public void BodyRegex_NullBody_Fails()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.BodyRegex, AssertionOperator.Contains, "ok");
        new BodyRegexEvaluator().Evaluate(assertion, new AssertionContext(200, 50, null)).Passed.Should().BeFalse();
    }

    [Fact]
    public void BodyRegex_InvalidPattern_ReturnsFail()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.BodyRegex, AssertionOperator.Contains, "[invalid");
        var result = new BodyRegexEvaluator().Evaluate(assertion, new AssertionContext(200, 50, "body"));
        result.Passed.Should().BeFalse();
        result.FailureMessage.Should().Contain("Invalid regex");
    }

    // ── JsonPath evaluator ───────────────────────────────────────────────
    [Fact]
    public void JsonPath_Equals_StringValue_Match()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.Equals, "ok", jsonPath: "$.status");
        var ctx = new AssertionContext(200, 50, """{"status":"ok"}""");
        new JsonPathEvaluator().Evaluate(assertion, ctx).Passed.Should().BeTrue();
    }

    [Fact]
    public void JsonPath_Equals_NumericValue_ComparesAsString()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.Equals, "42", jsonPath: "$.count");
        var ctx = new AssertionContext(200, 50, """{"count":42}""");
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
    public void JsonPath_Equals_BooleanValue_ComparesAsLowercaseString()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.Equals, "true", jsonPath: "$.active");
        var ctx = new AssertionContext(200, 50, """{"active":true}""");
        new JsonPathEvaluator().Evaluate(assertion, ctx).Passed.Should().BeTrue();
    }

    [Fact]
    public void JsonPath_NullBody_Fails()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.Equals, "ok", jsonPath: "$.status");
        new JsonPathEvaluator().Evaluate(assertion, new AssertionContext(200, 50, null)).Passed.Should().BeFalse();
    }

    [Fact]
    public void JsonPath_UnsupportedOperator_ReturnsFailWithMessage()
    {
        var assertion = new ProbeAssertion(Guid.NewGuid(), AssertionType.JsonPath,
            AssertionOperator.LessThan, "ok", jsonPath: "$.status");
        var result = new JsonPathEvaluator().Evaluate(assertion, new AssertionContext(200, 50, """{"status":"ok"}"""));
        result.Passed.Should().BeFalse();
        result.FailureMessage.Should().Contain("not supported");
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
            new(probeId, AssertionType.StatusCode, AssertionOperator.Equals,   "200"),
            new(probeId, AssertionType.LatencyMs,  AssertionOperator.LessThan, "500"),
        };
        var ctx = new AssertionContext(200, 100, null);
        var factory = new AssertionEvaluatorFactory();

        assertions.Select(a => factory.Get(a.Type).Evaluate(a, ctx))
            .Should().AllSatisfy(r => r.Passed.Should().BeTrue());
    }
}
