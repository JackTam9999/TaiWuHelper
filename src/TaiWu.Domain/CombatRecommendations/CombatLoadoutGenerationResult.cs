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
        Diagnostics = [.. diagnostics];
        ExploredCombinations = exploredCombinations;
    }

    public ImmutableArray<GeneratedCombatLoadout> Candidates { get; }

    public ImmutableArray<CombatLoadoutGenerationDiagnostic> Diagnostics
    {
        get;
    }

    public int ExploredCombinations { get; }
}
