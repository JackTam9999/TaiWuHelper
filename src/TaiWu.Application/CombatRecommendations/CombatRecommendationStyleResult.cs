using TaiWu.Domain.CombatRecommendations;

namespace TaiWu.Application.CombatRecommendations;

public sealed record CombatRecommendationStyleResult
{
    internal CombatRecommendationStyleResult(
        RecommendationPolicy policy,
        CombatRecommendationScoringResult scoring,
        ManualCombatPlanResult manualPlan,
        CombatRecommendationExplanation? explanation)
    {
        Policy = policy;
        Scoring = scoring;
        ManualPlan = manualPlan;
        Explanation = explanation;
    }

    public RecommendationPolicy Policy { get; }

    public CombatRecommendationScoringResult Scoring { get; }

    public ManualCombatPlanResult ManualPlan { get; }

    public CombatRecommendationExplanation? Explanation { get; }
}
