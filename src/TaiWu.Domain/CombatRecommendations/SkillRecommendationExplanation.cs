using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record SkillRecommendationExplanation
{
    internal SkillRecommendationExplanation(
        CombatSkillSnapshot skill,
        IEnumerable<RecommendationReason> reasons,
        IEnumerable<SkillThreatExplanation> threats,
        SkillCounterExplanation counter,
        SkillDirectionExplanation direction,
        SkillCostExplanation cost,
        IEnumerable<SkillConditionExplanation> conditions)
    {
        SkillId = skill.SkillId;
        DisplayName = skill.DisplayName;
        Category = skill.Category;
        Reasons = [.. reasons];
        Threats = [.. threats];
        Counter = counter;
        Direction = direction;
        Cost = cost;
        Conditions = [.. conditions];
    }

    public int SkillId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public SkillCategory Category { get; }

    public ImmutableArray<RecommendationReason> Reasons { get; }

    public ImmutableArray<SkillThreatExplanation> Threats { get; }

    public SkillCounterExplanation Counter { get; }

    public SkillDirectionExplanation Direction { get; }

    public SkillCostExplanation Cost { get; }

    public ImmutableArray<SkillConditionExplanation> Conditions { get; }
}
