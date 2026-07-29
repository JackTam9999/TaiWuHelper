using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record PlayerCombatSnapshot
{
    public PlayerCombatSnapshot(
        int characterId,
        SnapshotValue<string> displayName,
        IEnumerable<CombatSkillSnapshot> learnedSkills,
        CombatLoadoutSnapshot equippedSkills,
        IEnumerable<EquipmentSnapshot> equipment,
        SlotBudgetSet slotBudgets,
        GenericSlotAllocation genericSlotAllocation,
        IEnumerable<LegendaryBookModifier> legendaryBookModifiers)
    {
        if (characterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "Character ID must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(learnedSkills);
        ArgumentNullException.ThrowIfNull(equippedSkills);
        ArgumentNullException.ThrowIfNull(equipment);
        ArgumentNullException.ThrowIfNull(slotBudgets);
        ArgumentNullException.ThrowIfNull(genericSlotAllocation);
        ArgumentNullException.ThrowIfNull(legendaryBookModifiers);

        CharacterId = characterId;
        DisplayName = displayName;
        LearnedSkills = CopyUniqueSkills(learnedSkills);
        EquippedSkills = equippedSkills;
        Equipment = CopyUniqueEquipment(equipment);
        SlotBudgets = slotBudgets;
        GenericSlotAllocation = genericSlotAllocation;
        LegendaryBookModifiers = CopyLegendaryBookModifiers(
            legendaryBookModifiers,
            LearnedSkills);
    }

    public int CharacterId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public ImmutableArray<CombatSkillSnapshot> LearnedSkills { get; }

    public CombatLoadoutSnapshot EquippedSkills { get; }

    public ImmutableArray<EquipmentSnapshot> Equipment { get; }

    public SlotBudgetSet SlotBudgets { get; }

    public GenericSlotAllocation GenericSlotAllocation { get; }

    public ImmutableArray<LegendaryBookModifier> LegendaryBookModifiers { get; }

    private static ImmutableArray<CombatSkillSnapshot> CopyUniqueSkills(
        IEnumerable<CombatSkillSnapshot> skills)
    {
        var values = skills.ToImmutableArray();
        if (values.Any(skill => skill is null))
        {
            throw new ArgumentException(
                "Learned skills cannot contain null entries.",
                nameof(skills));
        }

        var duplicate = values
            .GroupBy(skill => skill.SkillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate learned skill {duplicate.Key}.",
                nameof(skills));
        }

        return values;
    }

    private static ImmutableArray<EquipmentSnapshot> CopyUniqueEquipment(
        IEnumerable<EquipmentSnapshot> equipment)
    {
        var values = equipment.ToImmutableArray();
        if (values.Any(item => item is null))
        {
            throw new ArgumentException(
                "Equipment cannot contain null entries.",
                nameof(equipment));
        }

        var duplicate = values
            .GroupBy(item => item.SlotIndex)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate equipment slot {duplicate.Key}.",
                nameof(equipment));
        }

        return values;
    }

    private static ImmutableArray<LegendaryBookModifier>
        CopyLegendaryBookModifiers(
            IEnumerable<LegendaryBookModifier> modifiers,
            ImmutableArray<CombatSkillSnapshot> learnedSkills)
    {
        var values = modifiers.ToImmutableArray();
        if (values.Any(modifier => modifier is null))
        {
            throw new ArgumentException(
                "Legendary-book modifiers cannot contain null entries.",
                nameof(modifiers));
        }

        var learnedById =
            learnedSkills.ToImmutableDictionary(skill => skill.SkillId);
        foreach (var modifier in values)
        {
            if (!learnedById.TryGetValue(modifier.SkillId, out var skill))
            {
                throw new ArgumentException(
                    $"Legendary-book modifier references unlearned skill "
                    + $"{modifier.SkillId}.",
                    nameof(modifiers));
            }

            if (modifier.Category != skill.Category)
            {
                throw new ArgumentException(
                    $"Legendary-book modifier for skill {modifier.SkillId} "
                    + $"uses {modifier.Category}, not {skill.Category}.",
                    nameof(modifiers));
            }
        }

        var duplicateSkill = values
            .GroupBy(modifier => modifier.SkillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSkill is not null)
        {
            throw new ArgumentException(
                $"Skill {duplicateSkill.Key} has more than one "
                + "legendary-book fixed-cost modifier.",
                nameof(modifiers));
        }

        return values;
    }
}
