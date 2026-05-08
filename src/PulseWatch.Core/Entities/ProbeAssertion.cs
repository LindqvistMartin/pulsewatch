namespace PulseWatch.Core.Entities;

public enum AssertionType { StatusCode, LatencyMs, BodyRegex, JsonPath }
public enum AssertionOperator { Equals, LessThan, Contains }

public sealed class ProbeAssertion
{
    public Guid Id { get; private set; }
    public Guid ProbeId { get; private set; }
    public AssertionType Type { get; private set; }
    public AssertionOperator Operator { get; private set; }
    public string ExpectedValue { get; private set; } = default!;
    public string? JsonPathExpression { get; private set; }

    public Probe Probe { get; private set; } = default!;

    private ProbeAssertion() { }

    public ProbeAssertion(Guid probeId, AssertionType type, AssertionOperator op, string expectedValue, string? jsonPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedValue);
        if (type == AssertionType.JsonPath)
            ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath, nameof(jsonPath));
        Id = Guid.NewGuid();
        ProbeId = probeId;
        Type = type;
        Operator = op;
        ExpectedValue = expectedValue;
        JsonPathExpression = jsonPath;
    }
}
