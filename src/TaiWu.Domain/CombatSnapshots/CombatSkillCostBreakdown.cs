using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSkillCostBreakdown
{
    internal CombatSkillCostBreakdown(
        CombatSkillSnapshot skill,
        IEnumerable<LegendaryBookCostAssignment> appliedAssignments,
        SnapshotValue<int> masteryReduction,
        SnapshotValue<int> legendaryBookReduction,
        SnapshotValue<int> effectiveCost)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(appliedAssignments);
        ArgumentNullException.ThrowIfNull(masteryReduction);
        ArgumentNullException.ThrowIfNull(legendaryBookReduction);
        ArgumentNullException.ThrowIfNull(effectiveCost);

        SkillId = skill.SkillId;
        Category = skill.Category;
        BaseCost = skill.GridCost;
        Mastered = skill.Mastered;
        MasteryReduction = masteryReduction;
        AppliedLegendaryBookCostAssignments = [.. appliedAssignments];
        LegendaryBookReduction = legendaryBookReduction;
        EffectiveCost = effectiveCost;
    }

    public int SkillId { get; }

    public SkillCategory Category { get; }

    public SnapshotValue<int> BaseCost { get; }

    public SnapshotValue<bool> Mastered { get; }

    public SnapshotValue<int> MasteryReduction { get; }

    public SnapshotValue<int> LegendaryBookReduction { get; }

    public ImmutableArray<LegendaryBookCostAssignment>
        AppliedLegendaryBookCostAssignments
    {
        get;
    }

    public SnapshotValue<int> EffectiveCost { get; }
}
