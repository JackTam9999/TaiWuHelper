namespace TaiWu.Domain.CombatSnapshots;

public static class CombatSkillCostCalculator
{
    public const int MinimumEffectiveCost = 1;

    public static CombatSkillCostBreakdown Calculate(
        PlayerCombatSnapshot player,
        int skillId)
    {
        ArgumentNullException.ThrowIfNull(player);

        var skill = player.LearnedSkills.FirstOrDefault(
            candidate => candidate.SkillId == skillId)
            ?? throw new KeyNotFoundException(
                $"Player has not learned combat skill {skillId}.");

        return Calculate(skill, player.LegendaryBookModifiers);
    }

    public static CombatSkillCostBreakdown Calculate(
        CombatSkillSnapshot skill,
        IEnumerable<LegendaryBookModifier> legendaryBookModifiers)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentNullException.ThrowIfNull(legendaryBookModifiers);

        var modifiers = legendaryBookModifiers.ToArray();
        if (modifiers.Any(modifier => modifier is null))
        {
            throw new ArgumentException(
                "Legendary-book modifiers cannot contain null entries.",
                nameof(legendaryBookModifiers));
        }

        var appliedModifiers = modifiers
            .Where(modifier => modifier.SkillId == skill.SkillId)
            .ToArray();

        var categoryMismatch = appliedModifiers.FirstOrDefault(
            modifier => modifier.Category != skill.Category);
        if (categoryMismatch is not null)
        {
            throw new ArgumentException(
                $"Legendary-book modifier for skill {skill.SkillId} "
                + $"uses {categoryMismatch.Category}, not {skill.Category}.",
                nameof(legendaryBookModifiers));
        }

        if (appliedModifiers.Length > 1)
        {
            throw new ArgumentException(
                $"Skill {skill.SkillId} has more than one confirmed "
                + "legendary-book fixed-cost modifier.",
                nameof(legendaryBookModifiers));
        }

        SnapshotValue<int> legendaryBookReduction;
        SnapshotValue<int> effectiveCost;
        if (!skill.GridCost.IsAvailable)
        {
            legendaryBookReduction = CreateUnavailableLegendaryBookReduction(
                appliedModifiers,
                "GridCost is unavailable");
            effectiveCost = SnapshotValue<int>.Unavailable(
                "Effective cost is unavailable because GridCost is "
                + $"unavailable: {skill.GridCost.UnavailableReason}");
        }
        else if (!skill.Mastered.IsAvailable)
        {
            legendaryBookReduction = CreateUnavailableLegendaryBookReduction(
                appliedModifiers,
                "mastery is unavailable");
            effectiveCost = SnapshotValue<int>.Unavailable(
                "Effective cost is unavailable because mastery is "
                + $"unavailable: {skill.Mastered.UnavailableReason}");
        }
        else
        {
            var masteryReduction = skill.Mastered.Value ? 1 : 0;
            var masteryAdjustedCost = Math.Max(
                MinimumEffectiveCost,
                skill.GridCost.Value - masteryReduction);
            var calculatedCost = appliedModifiers.Length == 0
                ? masteryAdjustedCost
                : Math.Min(
                    masteryAdjustedCost,
                    appliedModifiers[0].FixedCost);

            calculatedCost = Math.Max(
                MinimumEffectiveCost,
                calculatedCost);
            legendaryBookReduction = SnapshotValue<int>.Available(
                masteryAdjustedCost - calculatedCost);
            effectiveCost = SnapshotValue<int>.Available(calculatedCost);
        }

        return new CombatSkillCostBreakdown(
            skill,
            appliedModifiers,
            legendaryBookReduction,
            effectiveCost);
    }

    private static SnapshotValue<int> CreateUnavailableLegendaryBookReduction(
        LegendaryBookModifier[] appliedModifiers,
        string reason)
    {
        return appliedModifiers.Length == 0
            ? SnapshotValue<int>.Available(0)
            : SnapshotValue<int>.Unavailable(
                "Legendary-book reduction is unavailable because "
                + $"{reason}.");
    }
}
