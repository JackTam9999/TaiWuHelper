using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record SkillCostExplanation
{
    internal SkillCostExplanation(
        CombatSkillCostBreakdown breakdown,
        SlotBudget categoryBudget,
        IEnumerable<string> evidenceReferences)
    {
        BaseCost = breakdown.BaseCost;
        Mastered = breakdown.Mastered;
        MasteryReduction = breakdown.MasteryReduction;
        LegendaryBookReduction = breakdown.LegendaryBookReduction;
        EffectiveCost = breakdown.EffectiveCost;
        CategoryBudget = categoryBudget;
        EvidenceReferences = [.. evidenceReferences];
    }

    public SnapshotValue<int> BaseCost { get; }

    public SnapshotValue<bool> Mastered { get; }

    public SnapshotValue<int> MasteryReduction { get; }

    public SnapshotValue<int> LegendaryBookReduction { get; }

    public SnapshotValue<int> EffectiveCost { get; }

    public SlotBudget CategoryBudget { get; }

    public ImmutableArray<string> EvidenceReferences { get; }
}
