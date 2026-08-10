using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record TargetCombatSnapshot
{
    public TargetCombatSnapshot(
        int characterId,
        SnapshotValue<string> displayName,
        SnapshotValue<int> age,
        IEnumerable<CharacterFeatureSnapshot> features,
        IEnumerable<CombatSkillSnapshot> learnedSkills,
        SnapshotValue<CombatLoadoutSnapshot> equippedSkills,
        IEnumerable<EquipmentSnapshot> equipment,
        TargetLoadoutObservation? loadoutObservation = null,
        SnapshotValue<TargetChannelResistanceSnapshot>?
            baseChannelResistance = null)
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
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(learnedSkills);
        ArgumentNullException.ThrowIfNull(equippedSkills);
        ArgumentNullException.ThrowIfNull(equipment);

        if (age.IsAvailable && age.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(age),
                "An available age cannot be negative.");
        }

        var featureValues = features.ToImmutableArray();
        if (featureValues.Any(feature => feature is null))
        {
            throw new ArgumentException(
                "Features cannot contain null entries.",
                nameof(features));
        }

        var duplicateFeature = featureValues
            .GroupBy(feature => feature.FeatureId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFeature is not null)
        {
            throw new ArgumentException(
                $"Duplicate feature {duplicateFeature.Key}.",
                nameof(features));
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
        Features = featureValues;
        LearnedSkills = skillValues;
        EquippedSkills = equippedSkills;
        Equipment = equipmentValues;
        if (loadoutObservation is not null
            && loadoutObservation.TargetCharacterId != characterId)
        {
            throw new ArgumentException(
                "A target loadout observation must identify this target.",
                nameof(loadoutObservation));
        }

        LoadoutObservation = loadoutObservation;
        BaseChannelResistance = baseChannelResistance
            ?? SnapshotValue<TargetChannelResistanceSnapshot>.Unavailable(
                "Base channel resistance was not captured.");
    }

    public int CharacterId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public SnapshotValue<int> Age { get; }

    public ImmutableArray<CharacterFeatureSnapshot> Features { get; }

    public ImmutableArray<CombatSkillSnapshot> LearnedSkills { get; }

    public SnapshotValue<CombatLoadoutSnapshot> EquippedSkills { get; }

    public ImmutableArray<EquipmentSnapshot> Equipment { get; }

    public TargetLoadoutObservation? LoadoutObservation { get; }

    public SnapshotValue<TargetChannelResistanceSnapshot>
        BaseChannelResistance
    { get; }
}
