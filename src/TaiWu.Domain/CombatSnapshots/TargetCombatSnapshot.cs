using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record TargetCombatSnapshot
{
    public TargetCombatSnapshot(
        int characterId,
        SnapshotValue<string> displayName,
        SnapshotValue<int> age,
        IEnumerable<CombatSkillSnapshot> learnedSkills,
        SnapshotValue<CombatLoadoutSnapshot> equippedSkills,
        IEnumerable<EquipmentSnapshot> equipment)
    {
        if (characterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "Character ID must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(age);
        ArgumentNullException.ThrowIfNull(learnedSkills);
        ArgumentNullException.ThrowIfNull(equippedSkills);
        ArgumentNullException.ThrowIfNull(equipment);

        if (age.IsAvailable && age.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(age),
                "An available age cannot be negative.");
        }

        var skillValues = learnedSkills.ToImmutableArray();
        if (skillValues.Any(skill => skill is null))
        {
            throw new ArgumentException(
                "Learned skills cannot contain null entries.",
                nameof(learnedSkills));
        }

        var duplicateSkill = skillValues
            .GroupBy(skill => skill.SkillId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSkill is not null)
        {
            throw new ArgumentException(
                $"Duplicate learned skill {duplicateSkill.Key}.",
                nameof(learnedSkills));
        }

        var equipmentValues = equipment.ToImmutableArray();
        if (equipmentValues.Any(item => item is null))
        {
            throw new ArgumentException(
                "Equipment cannot contain null entries.",
                nameof(equipment));
        }

        var duplicateEquipment = equipmentValues
            .GroupBy(item => item.SlotIndex)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateEquipment is not null)
        {
            throw new ArgumentException(
                $"Duplicate equipment slot {duplicateEquipment.Key}.",
                nameof(equipment));
        }

        CharacterId = characterId;
        DisplayName = displayName;
        Age = age;
        LearnedSkills = skillValues;
        EquippedSkills = equippedSkills;
        Equipment = equipmentValues;
    }

    public int CharacterId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public SnapshotValue<int> Age { get; }

    public ImmutableArray<CombatSkillSnapshot> LearnedSkills { get; }

    public SnapshotValue<CombatLoadoutSnapshot> EquippedSkills { get; }

    public ImmutableArray<EquipmentSnapshot> Equipment { get; }
}
