using System.Collections.Immutable;

namespace TaiWu.Domain.CombatCounters;

public sealed record CombatCounterAccessReport
{
    internal CombatCounterAccessReport(
        IEnumerable<CombatCounterAccessEvaluation> evaluations)
    {
        Evaluations = [.. evaluations];
    }

    public ImmutableArray<CombatCounterAccessEvaluation> Evaluations { get; }

    public IEnumerable<CombatCounterAccessEvaluation> AccessibleCounters =>
        Evaluations.Where(evaluation => evaluation.IsAccessible);

    public IEnumerable<CombatCounterAccessEvaluation> MissingAccess =>
        Evaluations.Where(evaluation => !evaluation.IsAccessible);
}
