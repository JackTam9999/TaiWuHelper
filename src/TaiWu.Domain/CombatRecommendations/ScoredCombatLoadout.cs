using System.Collections.Immutable;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record ScoredCombatLoadout
{
    internal ScoredCombatLoadout(
        GeneratedCombatLoadout candidate,
        RecommendationPolicy policy,
        IEnumerable<RecommendationScoreComponent> components,
        decimal totalScore)
    {
        Candidate = candidate;
        Policy = policy;
        Components = [.. components];
        TotalScore = totalScore;
    }

    public GeneratedCombatLoadout Candidate { get; }

    public RecommendationPolicy Policy { get; }

    public ImmutableArray<RecommendationScoreComponent> Components { get; }

    public decimal TotalScore { get; }

    public RecommendationScoreComponent Get(
        RecommendationScoreComponentKind kind) =>
        Components.Single(component => component.Kind == kind);
}
