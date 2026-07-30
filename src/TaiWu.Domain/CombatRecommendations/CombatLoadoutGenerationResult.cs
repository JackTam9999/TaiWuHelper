using System.Collections.Immutable;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatLoadoutGenerationResult
{
    internal CombatLoadoutGenerationResult(
        IEnumerable<GeneratedCombatLoadout> candidates,
        IEnumerable<CombatLoadoutGenerationDiagnostic> diagnostics,
        int exploredCombinations)
    {
        Candidates = [.. candidates];
        Diagnostics = AggregateDiagnostics(diagnostics);
        ExploredCombinations = exploredCombinations;
    }

    public ImmutableArray<GeneratedCombatLoadout> Candidates { get; }

    public ImmutableArray<CombatLoadoutGenerationDiagnostic> Diagnostics
    {
        get;
    }

    public int ExploredCombinations { get; }

    private static ImmutableArray<CombatLoadoutGenerationDiagnostic>
        AggregateDiagnostics(
            IEnumerable<CombatLoadoutGenerationDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        return
        [
            .. diagnostics
                .GroupBy(diagnostic => new
                {
                    diagnostic.Code,
                    diagnostic.Reason,
                    diagnostic.SkillId,
                    Failures = string.Join(
                        "\u001f",
                        diagnostic.FeasibilityFailures.Select(failure =>
                            $"{failure.Code}\u001e{failure.SkillId}\u001e"
                            + failure.Reason))
                })
                .Select(group =>
                {
                    var first = group.First();
                    return first.WithOccurrences(
                        group.Sum(value => value.Occurrences));
                })
        ];
    }
}
