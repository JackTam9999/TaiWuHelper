namespace TaiWu.Domain.CombatSnapshots;

public static class CombatSlotBudgetCalculator
{
    public const int BaseNeigongCapacity = 6;

    public const int BaseOuterCategoryCapacity = 2;

    public static int CalculateConfiguredCapacity(
        SkillCategory category,
        IEnumerable<CombatSkillSnapshot> equippedNeigong,
        GenericSlotAllocation genericSlotAllocation)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown skill category.");
        }

        ArgumentNullException.ThrowIfNull(equippedNeigong);
        ArgumentNullException.ThrowIfNull(genericSlotAllocation);
        var values = equippedNeigong.ToArray();
        if (values.Any(skill => skill is null))
        {
            throw new ArgumentException(
                "Equipped Neigong cannot contain null entries.",
                nameof(equippedNeigong));
        }

        if (values.Any(skill => skill.Category != SkillCategory.Neigong))
        {
            throw new ArgumentException(
                "Only Neigong skills can contribute configured capacity.",
                nameof(equippedNeigong));
        }

        return GetCapacity(category, values, genericSlotAllocation);
    }

    public static SlotBudgetSet Calculate(PlayerCombatSnapshot player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var learnedById = player.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        ValidateEquippedSkills(player.EquippedSkills, learnedById);

        var equippedNeigong = GetEquippedNeigong(
            player.EquippedSkills,
            learnedById);

        return new SlotBudgetSet(
            Enum.GetValues<SkillCategory>().Select(
                category => CalculateCategory(
                    player,
                    player.EquippedSkills,
                    category,
                    CalculateConfiguredCapacity(
                        category,
                        equippedNeigong,
                        player.GenericSlotAllocation))));
    }

    public static SlotBudgetSet CalculateProposed(
        PlayerCombatSnapshot player,
        CombatLoadoutSnapshot proposedLoadout,
        GenericSlotAllocation proposedGenericSlotAllocation,
        IEnumerable<LegendaryBookCostAssignment>?
            proposedLegendaryCostAssignments = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(proposedLoadout);
        ArgumentNullException.ThrowIfNull(proposedGenericSlotAllocation);

        var learnedById = player.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        var legendaryAssignments = proposedLegendaryCostAssignments?.ToArray();
        ValidateEquippedSkills(player.EquippedSkills, learnedById);
        ValidateEquippedSkills(proposedLoadout, learnedById);

        var currentNeigong = GetEquippedNeigong(
            player.EquippedSkills,
            learnedById);
        var proposedNeigong = GetEquippedNeigong(
            proposedLoadout,
            learnedById);

        return new SlotBudgetSet(
            Enum.GetValues<SkillCategory>().Select(category =>
            {
                var configuredCurrentCapacity = CalculateConfiguredCapacity(
                    category,
                    currentNeigong,
                    player.GenericSlotAllocation);
                var observedCapacityAdjustment = checked(
                    player.SlotBudgets[category].Capacity
                    - configuredCurrentCapacity);
                var configuredProposedCapacity = CalculateConfiguredCapacity(
                    category,
                    proposedNeigong,
                    proposedGenericSlotAllocation);
                var proposedCapacity = checked(
                    configuredProposedCapacity
                    + observedCapacityAdjustment);
                return CalculateCategory(
                    player,
                    proposedLoadout,
                    category,
                    proposedCapacity,
                    legendaryAssignments);
            }));
    }

    private static CombatSkillSnapshot[] GetEquippedNeigong(
        CombatLoadoutSnapshot loadout,
        Dictionary<int, CombatSkillSnapshot> learnedById)
    {
        return loadout.NeigongSkillIds
            .Select(skillId => learnedById[skillId])
            .ToArray();
    }

    private static SlotBudget CalculateCategory(
        PlayerCombatSnapshot player,
        CombatLoadoutSnapshot loadout,
        SkillCategory category,
        int capacity,
        LegendaryBookCostAssignment[]? proposedAssignments = null)
    {
        var used = GetUsed(
            player,
            loadout,
            category,
            proposedAssignments);

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
        CombatLoadoutSnapshot loadout,
        SkillCategory category,
        LegendaryBookCostAssignment[]? proposedAssignments)
    {
        var used = 0;
        foreach (var skillId in loadout.Get(category))
        {
            var cost = proposedAssignments is null
                ? CombatSkillCostCalculator.Calculate(player, skillId)
                : CombatSkillCostCalculator.CalculateProposed(
                    player,
                    skillId,
                    proposedAssignments);
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
