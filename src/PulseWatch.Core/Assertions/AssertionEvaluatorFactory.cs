using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public sealed class AssertionEvaluatorFactory
{
    private static readonly StatusCodeEvaluator _statusCode = new();
    private static readonly LatencyEvaluator    _latency    = new();
    private static readonly BodyRegexEvaluator  _bodyRegex  = new();
    private static readonly JsonPathEvaluator   _jsonPath   = new();

    public IAssertionEvaluator Get(AssertionType type) => type switch
    {
        AssertionType.StatusCode => _statusCode,
        AssertionType.LatencyMs  => _latency,
        AssertionType.BodyRegex  => _bodyRegex,
        AssertionType.JsonPath   => _jsonPath,
        _                        => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
