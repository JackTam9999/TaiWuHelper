using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatSkills;

public enum CombatSkillDiscipline
{
    Neigong = 0,
    Agility = 1,
    SpecialTechnique = 2,
    FistAndPalm = 3,
    Finger = 4,
    Leg = 5,
    HiddenWeapon = 6,
    Sword = 7,
    Blade = 8,
    LongWeapon = 9,
    ExoticWeapon = 10,
    FlexibleWeapon = 11,
    Archery = 12,
    Music = 13
}

public enum CombatSkillEquipmentType
{
    Neigong = 0,
    Attack = 1,
    Agility = 2,
    Defense = 3,
    Assistance = 4
}

public readonly record struct CombatSkillGrade
{
    public const int Minimum = 0;
    public const int Maximum = 8;

    public CombatSkillGrade(int value)
    {
        if (value is < Minimum or > Maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Combat-skill grade must be {Minimum}..{Maximum}.");
        }

        Value = value;
    }

    public int Value { get; }
}

public readonly record struct CombatSkillFactionId
{
    public CombatSkillFactionId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "A faction ID cannot be negative.");
        }

        Value = value;
    }

    public int Value { get; }
}

public readonly record struct CombatSkillGridCost
{
    public CombatSkillGridCost(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "A base combat-skill grid cost must be positive.");
        }

        Value = value;
    }

    public int Value { get; }
}

public readonly record struct CombatSkillEffectId
{
    public CombatSkillEffectId(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "A combat-skill effect ID cannot be negative.");
        }

        Value = value;
    }

    public int Value { get; }
}

public readonly record struct CombatSkillRequirementId
{
    public CombatSkillRequirementId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A combat-skill requirement ID cannot be blank.",
                nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public sealed record CombatSkillRequirementDefinition
{
    public CombatSkillRequirementDefinition(
        CombatSkillRequirementId requirementId,
        CatalogueField<int> requiredValue,
        CatalogueSourceReference source)
    {
        if (string.IsNullOrWhiteSpace(requirementId.Value))
        {
            throw new ArgumentException(
                "A default combat-skill requirement ID is invalid.",
                nameof(requirementId));
        }

        ArgumentNullException.ThrowIfNull(requiredValue);
        ArgumentNullException.ThrowIfNull(source);
        if (requiredValue.IsAvailable && requiredValue.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredValue),
                "An available requirement value cannot be negative.");
        }

        RequirementId = requirementId;
        RequiredValue = requiredValue;
        Source = source;
    }

    public CombatSkillRequirementId RequirementId { get; }

    public CatalogueField<int> RequiredValue { get; }

    public CatalogueSourceReference Source { get; }
}

public sealed record CombatSkillTimingDefinition
{
    public CombatSkillTimingDefinition(
        CatalogueField<int> preparationProgress,
        CatalogueField<int> breathStanceCost,
        CatalogueField<int> castSpeed)
    {
        PreparationProgress = ValidateNonNegative(
            preparationProgress,
            nameof(preparationProgress));
        BreathStanceCost = ValidateNonNegative(
            breathStanceCost,
            nameof(breathStanceCost));
        CastSpeed = ValidateNonNegative(castSpeed, nameof(castSpeed));
    }

    public CatalogueField<int> PreparationProgress { get; }

    public CatalogueField<int> BreathStanceCost { get; }

    public CatalogueField<int> CastSpeed { get; }

    private static CatalogueField<int> ValidateNonNegative(
        CatalogueField<int> field,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(field, parameterName);
        if (field.IsAvailable && field.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "An available timing value cannot be negative.");
        }

        return field;
    }
}

public sealed record CombatSkillEffectReferences
{
    public CombatSkillEffectReferences(
        CatalogueField<CombatSkillEffectId> direct,
        CatalogueField<CombatSkillEffectId> reverse,
        CatalogueField<CombatSkillEffectId> neutral)
    {
        ArgumentNullException.ThrowIfNull(direct);
        ArgumentNullException.ThrowIfNull(reverse);
        ArgumentNullException.ThrowIfNull(neutral);
        Direct = direct;
        Reverse = reverse;
        Neutral = neutral;
    }

    public CatalogueField<CombatSkillEffectId> Direct { get; }

    public CatalogueField<CombatSkillEffectId> Reverse { get; }

    public CatalogueField<CombatSkillEffectId> Neutral { get; }
}

public enum RawCombatSkillDescriptionKind
{
    Effect = 0,
    Requirement = 1,
    Other = 2,
    DirectEffect = 3,
    ReverseEffect = 4
}

public sealed record RawCombatSkillDescription
{
    public RawCombatSkillDescription(
        RawCombatSkillDescriptionKind kind,
        CatalogueLanguage language,
        string text,
        CatalogueSourceReference source)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown raw description kind.");
        }

        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "A raw combat-skill description cannot be blank.",
                nameof(text));
        }

        ArgumentNullException.ThrowIfNull(source);
        Kind = kind;
        Language = language;
        Text = text.Trim();
        Source = source;
    }

    public RawCombatSkillDescriptionKind Kind { get; }

    public CatalogueLanguage Language { get; }

    public string Text { get; }

    public CatalogueSourceReference Source { get; }

    public bool IsVerifiedMechanic => false;
}

public sealed class CombatSkillDefinition : IEquatable<CombatSkillDefinition>
{
    public CombatSkillDefinition(
        int skillId,
        CombatSkillLocalizedNames names,
        CatalogueField<CombatSkillDiscipline> category,
        CatalogueField<CombatSkillGrade> grade,
        CatalogueField<CombatSkillFactionId> faction,
        CatalogueField<CombatSkillElement> element,
        CatalogueField<CombatSkillEquipmentType> equipmentType,
        CatalogueField<CombatSkillGridCost> baseGridCost,
        CatalogueField<SkillSlotContribution> slotContribution,
        IEnumerable<CombatSkillRequirementDefinition>? requirements,
        CombatSkillTimingDefinition timing,
        CombatSkillEffectReferences effects,
        IEnumerable<RawCombatSkillDescription>? rawDescriptions,
        CatalogueSourceReference sourceRecord)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A combat-skill ID cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(names);
        ValidateEnumField(category, nameof(category));
        ArgumentNullException.ThrowIfNull(grade);
        ArgumentNullException.ThrowIfNull(faction);
        ValidateEnumField(element, nameof(element));
        ValidateEnumField(equipmentType, nameof(equipmentType));
        ArgumentNullException.ThrowIfNull(baseGridCost);
        ArgumentNullException.ThrowIfNull(slotContribution);
        ArgumentNullException.ThrowIfNull(timing);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(sourceRecord);

        var requirementValues = (requirements ?? []).ToImmutableArray();
        if (requirementValues.Any(requirement => requirement is null))
        {
            throw new ArgumentException(
                "Requirements cannot contain null.",
                nameof(requirements));
        }

        var duplicateRequirement = requirementValues
            .GroupBy(requirement => requirement.RequirementId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateRequirement is not null)
        {
            throw new ArgumentException(
                $"Duplicate requirement {duplicateRequirement.Key}.",
                nameof(requirements));
        }

        var descriptionValues = (rawDescriptions ?? []).ToImmutableArray();
        if (descriptionValues.Any(description => description is null))
        {
            throw new ArgumentException(
                "Raw descriptions cannot contain null.",
                nameof(rawDescriptions));
        }

        SkillId = skillId;
        Names = names;
        Category = category;
        Grade = grade;
        Faction = faction;
        Element = element;
        EquipmentType = equipmentType;
        BaseGridCost = baseGridCost;
        SlotContribution = slotContribution;
        Requirements = requirementValues;
        Timing = timing;
        Effects = effects;
        RawDescriptions = descriptionValues;
        SourceRecord = sourceRecord;
    }

    public int SkillId { get; }

    public CombatSkillLocalizedNames Names { get; }

    public CatalogueField<CombatSkillDiscipline> Category { get; }

    public CatalogueField<CombatSkillGrade> Grade { get; }

    public CatalogueField<CombatSkillFactionId> Faction { get; }

    public CatalogueField<CombatSkillElement> Element { get; }

    public CatalogueField<CombatSkillEquipmentType> EquipmentType { get; }

    public CatalogueField<CombatSkillGridCost> BaseGridCost { get; }

    public CatalogueField<SkillSlotContribution> SlotContribution { get; }

    public ImmutableArray<CombatSkillRequirementDefinition> Requirements
    { get; }

    public CombatSkillTimingDefinition Timing { get; }

    public CombatSkillEffectReferences Effects { get; }

    public ImmutableArray<RawCombatSkillDescription> RawDescriptions { get; }

    public CatalogueSourceReference SourceRecord { get; }

    public bool Equals(CombatSkillDefinition? other) =>
        other is not null && SkillId == other.SkillId;

    public override bool Equals(object? obj) =>
        obj is CombatSkillDefinition other && Equals(other);

    public override int GetHashCode() => SkillId;

    public static bool operator ==(
        CombatSkillDefinition? left,
        CombatSkillDefinition? right) => object.Equals(left, right);

    public static bool operator !=(
        CombatSkillDefinition? left,
        CombatSkillDefinition? right) => !object.Equals(left, right);

    private static void ValidateEnumField<TEnum>(
        CatalogueField<TEnum> field,
        string parameterName)
        where TEnum : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(field, parameterName);
        if (field.IsAvailable && !Enum.IsDefined(field.Value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                field.Value,
                "An unknown enum value must be represented as unsupported.");
        }
    }
}
