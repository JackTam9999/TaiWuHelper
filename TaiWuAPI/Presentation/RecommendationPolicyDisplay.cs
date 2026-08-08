using System.Collections.Immutable;
using TaiWu.Domain.CombatRecommendations;

namespace TaiWuAPI.Presentation;

public static class RecommendationPolicyDisplay
{
    public static ImmutableArray<RecommendationPolicy> VisiblePolicies { get; } =
    [
        RecommendationPolicy.Safe,
        RecommendationPolicy.Aggressive
    ];

    public static bool IsVisible(RecommendationPolicy policy) =>
        VisiblePolicies.Contains(policy);
}
