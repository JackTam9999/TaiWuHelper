using System.Collections.Immutable;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatRecommendationScoringResult
{
    internal CombatRecommendationScoringResult(
        RecommendationPolicyWeights weights,
        IEnumerable<ScoredCombatLoadout> rankedCandidates)
    {
        Weights = weights;
        RankedCandidates = [.. rankedCandidates];
    }

    public RecommendationPolicyWeights Weights { get; }

    public ImmutableArray<ScoredCombatLoadout> RankedCandidates { get; }
}
