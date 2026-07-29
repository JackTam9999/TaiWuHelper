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
        IEnumerable<LegendaryBookCostSlot> legendaryBookCostSlots,
        IEnumerable<LegendaryBookCostAssignment>
            legendaryBookCostAssignments)
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
        ArgumentNullException.ThrowIfNull(legendaryBookCostSlots);
        ArgumentNullException.ThrowIfNull(legendaryBookCostAssignments);

        CharacterId = characterId;
        DisplayName = displayName;
        LearnedSkills = CopyUniqueSkills(learnedSkills);
        EquippedSkills = equippedSkills;
        Equipment = CopyUniqueEquipment(equipment);
        SlotBudgets = slotBudgets;
        GenericSlotAllocation = genericSlotAllocation;
        LegendaryBookCostSlots = CopyLegendaryBookCostSlots(
            legendaryBookCostSlots);
        LegendaryBookCostAssignments = CopyLegendaryBookCostAssignments(
            legendaryBookCostAssignments,
            LegendaryBookCostSlots,
            LearnedSkills);
    }

    public int CharacterId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public ImmutableArray<CombatSkillSnapshot> LearnedSkills { get; }

    public CombatLoadoutSnapshot EquippedSkills { get; }

    public ImmutableArray<EquipmentSnapshot> Equipment { get; }

    public SlotBudgetSet SlotBudgets { get; }

    public GenericSlotAllocation GenericSlotAllocation { get; }

    public ImmutableArray<LegendaryBookCostSlot> LegendaryBookCostSlots
    {
        get;
    }

    public ImmutableArray<LegendaryBookCostAssignment>
        LegendaryBookCostAssignments
    {
        get;
    }

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

    private static ImmutableArray<LegendaryBookCostSlot>
        CopyLegendaryBookCostSlots(
            IEnumerable<LegendaryBookCostSlot> slots)
    {
        var values = slots.ToImmutableArray();
        if (values.Any(slot => slot is null))
        {
            throw new ArgumentException(
                "Legendary-book cost slots cannot contain null entries.",
                nameof(slots));
        }

        var duplicateSlot = values
            .GroupBy(
                slot => slot.SlotReference,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSlot is not null)
        {
            throw new ArgumentException(
                $"Duplicate legendary-book cost slot "
                + $"'{duplicateSlot.Key}'.",
                nameof(slots));
        }

        return values;
    }

    private static ImmutableArray<LegendaryBookCostAssignment>
        CopyLegendaryBookCostAssignments(
            IEnumerable<LegendaryBookCostAssignment> assignments,
            ImmutableArray<LegendaryBookCostSlot> slots,
            ImmutableArray<CombatSkillSnapshot> learnedSkills)
    {
        var values = assignments.ToImmutableArray();
        if (values.Any(assignment => assignment is null))
        {
            throw new ArgumentException(
                "Legendary-book cost assignments cannot contain null entries.",
                nameof(assignments));
        }

        var slotsByReference = slots.ToImmutableDictionary(
            slot => slot.SlotReference,
            StringComparer.Ordinal);
        var learnedById =
            learnedSkills.ToImmutableDictionary(skill => skill.SkillId);
        foreach (var assignment in values)
        {
            if (assignment.Origin == LegendaryBookAssignmentOrigin.Proposed)
            {
                throw new ArgumentException(
                    "A current player snapshot cannot contain proposed "
                    + "legendary-book assignments.",
                    nameof(assignments));
            }

            if (!slotsByReference.TryGetValue(
                    assignment.Slot.SlotReference,
                    out var knownSlot)
                || knownSlot != assignment.Slot)
            {
                throw new ArgumentException(
                    $"Legendary-book assignment references unknown or "
                    + $"mismatched slot '{assignment.Slot.SlotReference}'.",
                    nameof(assignments));
            }

            if (!learnedById.TryGetValue(
                    assignment.SkillId,
                    out var skill))
            {
                throw new ArgumentException(
                    $"Legendary-book assignment references unlearned skill "
                    + $"{assignment.SkillId}.",
                    nameof(assignments));
            }

            if (assignment.Category != skill.Category)
            {
                throw new ArgumentException(
                    $"Legendary-book assignment for skill "
                    + $"{assignment.SkillId} uses {assignment.Category}, "
                    + $"not {skill.Category}.",
                    nameof(assignments));
            }
        }

        var duplicateSlot = values
            .GroupBy(
                assignment => assignment.Slot.SlotReference,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSlot is not null)
        {
            throw new ArgumentException(
                $"Legendary-book cost slot '{duplicateSlot.Key}' has more "
                + "than one current assignment.",
                nameof(assignments));
        }

        var duplicateSkill = values
            .GroupBy(assignment => assignment.SkillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSkill is not null)
        {
            throw new ArgumentException(
                $"Skill {duplicateSkill.Key} has more than one "
                + "legendary-book fixed-cost assignment.",
                nameof(assignments));
        }

        return values;
    }
}
