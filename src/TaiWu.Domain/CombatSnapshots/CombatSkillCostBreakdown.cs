using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSkillCostBreakdown
{
    internal CombatSkillCostBreakdown(
        CombatSkillSnapshot skill,
        IEnumerable<LegendaryBookModifier> appliedLegendaryBookModifiers,
        SnapshotValue<int> legendaryBookReduction,
        SnapshotValue<int> effectiveCost)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(appliedLegendaryBookModifiers);
        ArgumentNullException.ThrowIfNull(legendaryBookReduction);
        ArgumentNullException.ThrowIfNull(effectiveCost);

        SkillId = skill.SkillId;
        Category = skill.Category;
        BaseCost = skill.GridCost;
        Mastered = skill.Mastered;
        MasteryReduction =
            skill.Mastered.IsAvailable && skill.Mastered.Value ? 1 : 0;
        AppliedLegendaryBookModifiers =
            [.. appliedLegendaryBookModifiers];
        LegendaryBookReduction = legendaryBookReduction;
        EffectiveCost = effectiveCost;
    }

    public int SkillId { get; }

    public SkillCategory Category { get; }

    public SnapshotValue<int> BaseCost { get; }

    public SnapshotValue<bool> Mastered { get; }

    public int MasteryReduction { get; }

    public SnapshotValue<int> LegendaryBookReduction { get; }

    public ImmutableArray<LegendaryBookModifier>
        AppliedLegendaryBookModifiers
    {
        get;
    }

    public SnapshotValue<int> EffectiveCost { get; }
}
