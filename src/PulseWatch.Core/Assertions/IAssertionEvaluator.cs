using PulseWatch.Core.Entities;

namespace PulseWatch.Core.Assertions;

public interface IAssertionEvaluator
{
    EvaluationResult Evaluate(ProbeAssertion assertion, AssertionContext context);
}
