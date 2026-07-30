using System.Collections.Immutable;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatRecommendationExplanation
{
    internal CombatRecommendationExplanation(
        string candidateStableKey,
        IEnumerable<SkillRecommendationExplanation> skills,
        IEnumerable<RecommendationCaveat> caveats)
    {
        CandidateStableKey = candidateStableKey;
        Skills = [.. skills];
        Caveats = [.. caveats];
    }

    public string CandidateStableKey { get; }

    public ImmutableArray<SkillRecommendationExplanation> Skills { get; }

    public ImmutableArray<RecommendationCaveat> Caveats { get; }

    public ImmutableArray<RecommendationCaveat> Assumptions =>
        [.. Caveats.Where(caveat =>
            caveat.Kind == RecommendationCaveatKind.Assumption)];

    public ImmutableArray<RecommendationCaveat> UnavailableData =>
        [.. Caveats.Where(caveat =>
            caveat.Kind == RecommendationCaveatKind.UnavailableData)];
}
