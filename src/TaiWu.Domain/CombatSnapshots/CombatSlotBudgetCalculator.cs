namespace TaiWu.Domain.CombatSnapshots;

public static class CombatSlotBudgetCalculator
{
    public const int BaseNeigongCapacity = 6;

    public const int BaseOuterCategoryCapacity = 2;

    public static SlotBudgetSet Calculate(PlayerCombatSnapshot player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var learnedById = player.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        ValidateEquippedSkills(player.EquippedSkills, learnedById);

        var equippedNeigong = player.EquippedSkills.NeigongSkillIds
            .Select(skillId => learnedById[skillId])
            .ToArray();

        return new SlotBudgetSet(
            Enum.GetValues<SkillCategory>().Select(
                category => CalculateCategory(
                    player,
                    category,
                    equippedNeigong)));
    }

    private static SlotBudget CalculateCategory(
        PlayerCombatSnapshot player,
        SkillCategory category,
        CombatSkillSnapshot[] equippedNeigong)
    {
        var capacity = GetCapacity(
            category,
            equippedNeigong,
            player.GenericSlotAllocation);
        var used = GetUsed(player, category);

        return new SlotBudget(category, used, capacity);
    }

    private static int GetCapacity(
        SkillCategory category,
        CombatSkillSnapshot[] equippedNeigong,
        GenericSlotAllocation genericSlotAllocation)
    {
        if (category == SkillCategory.Neigong)
        {
            return BaseNeigongCapacity;
        }

        var specificContribution = equippedNeigong.Sum(
            skill => skill.SlotContribution.GetSpecific(category));
        return checked(
            BaseOuterCategoryCapacity
            + specificContribution
            + genericSlotAllocation.Get(category));
    }

    private static SnapshotValue<int> GetUsed(
        PlayerCombatSnapshot player,
        SkillCategory category)
    {
        var used = 0;
        foreach (var skillId in player.EquippedSkills.Get(category))
        {
            var cost = CombatSkillCostCalculator.Calculate(
                player,
                skillId);
            if (!cost.EffectiveCost.IsAvailable)
            {
                return SnapshotValue<int>.Unavailable(
                    $"Used {category} slots are unavailable because "
                    + $"skill {skillId} has no available effective cost: "
                    + cost.EffectiveCost.UnavailableReason);
            }

            used = checked(used + cost.EffectiveCost.Value);
        }

        return SnapshotValue<int>.Available(used);
    }

    private static void ValidateEquippedSkills(
        CombatLoadoutSnapshot loadout,
        Dictionary<int, CombatSkillSnapshot> learnedById)
    {
        foreach (var category in Enum.GetValues<SkillCategory>())
        {
            foreach (var skillId in loadout.Get(category))
            {
                if (!learnedById.TryGetValue(skillId, out var skill))
                {
                    throw new ArgumentException(
                        $"Equipped skill {skillId} is not learned.",
                        nameof(loadout));
                }

                if (skill.Category != category)
                {
                    throw new ArgumentException(
                        $"Equipped skill {skillId} belongs to "
                        + $"{skill.Category}, not {category}.",
                        nameof(loadout));
                }
            }
        }
    }
}
