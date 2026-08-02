using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Infrastructure.Catalogue;

internal static class CombatSkillDefinitionMapper
{
    internal static CombatSkillDefinition Map(
        CombatSkillSourceRecord record,
        TaiwuLanguageCatalog traditionalChinese,
        TaiwuLanguageCatalog english,
        CombatSkillCatalogueMappingSources sources)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(traditionalChinese);
        ArgumentNullException.ThrowIfNull(english);
        ArgumentNullException.ThrowIfNull(sources);

        var recordSource = sources.GameDataRecord(record.SkillId);
        return new CombatSkillDefinition(
            record.SkillId,
            MapNames(record, traditionalChinese, english, sources),
            MapEnum<CombatSkillDiscipline>(
                record.Category,
                "category",
                recordSource),
            MapGrade(record.Grade, recordSource),
            MapFaction(record.Faction, recordSource),
            MapEnum<CombatSkillElement>(
                record.Element,
                "element",
                recordSource),
            MapEnum<CombatSkillEquipmentType>(
                record.EquipmentType,
                "equipment type",
                recordSource),
            record.BaseGridCost > 0
                ? CatalogueField<CombatSkillGridCost>.Available(
                    new CombatSkillGridCost(record.BaseGridCost),
                    recordSource)
                : CatalogueField<CombatSkillGridCost>.Unavailable(
                    $"Configured grid cost {record.BaseGridCost} is not positive.",
                    recordSource),
            MapSlotContribution(record, recordSource),
            MapRequirements(record, sources),
            new CombatSkillTimingDefinition(
                MapNonNegative(
                    record.PreparationProgress,
                    "preparation progress",
                    recordSource),
                MapNonNegative(
                    record.BreathStanceCost,
                    "breath/stance cost",
                    recordSource),
                MapNonNegative(
                    record.CastSpeed,
                    "cast speed",
                    recordSource)),
            new CombatSkillEffectReferences(
                MapEffect(
                    record.DirectEffectId,
                    "direct",
                    recordSource),
                MapEffect(
                    record.ReverseEffectId,
                    "reverse",
                    recordSource),
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "No verified neutral effect field exists in this GameData version.",
                    recordSource)),
            MapDescriptions(
                record,
                traditionalChinese,
                english,
                sources),
            recordSource);
    }

    private static CombatSkillLocalizedNames MapNames(
        CombatSkillSourceRecord record,
        TaiwuLanguageCatalog traditionalChinese,
        TaiwuLanguageCatalog english,
        CombatSkillCatalogueMappingSources sources)
    {
        List<LocalizedCombatSkillName> names = [];
        AddName(
            names,
            CatalogueLanguage.TraditionalChinese,
            traditionalChinese.Find(record.NameKey),
            sources.TraditionalChineseRecord(record.SkillId));
        AddName(
            names,
            CatalogueLanguage.English,
            english.Find(record.NameKey),
            sources.EnglishRecord(record.SkillId));
        return new CombatSkillLocalizedNames(names);
    }

    private static void AddName(
        ICollection<LocalizedCombatSkillName> names,
        CatalogueLanguage language,
        string? value,
        CatalogueSourceReference source)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            names.Add(new LocalizedCombatSkillName(language, value, source));
        }
    }

    private static CatalogueField<TEnum> MapEnum<TEnum>(
        int value,
        string field,
        CatalogueSourceReference source)
        where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value)
            ? CatalogueField<TEnum>.Available(
                (TEnum)Enum.ToObject(typeof(TEnum), value),
                source)
            : CatalogueField<TEnum>.Unsupported(
                $"Configured {field} value {value} is unsupported.",
                source);
    }

    private static CatalogueField<CombatSkillGrade> MapGrade(
        int value,
        CatalogueSourceReference source) =>
        value is >= CombatSkillGrade.Minimum and <= CombatSkillGrade.Maximum
            ? CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(value),
                source)
            : CatalogueField<CombatSkillGrade>.Unsupported(
                $"Configured grade {value} is unsupported.",
                source);

    private static CatalogueField<CombatSkillFactionId> MapFaction(
        int value,
        CatalogueSourceReference source) => value >= 0
            ? CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(value),
                source)
            : CatalogueField<CombatSkillFactionId>.Unsupported(
                $"Configured faction value {value} is unsupported.",
                source);

    private static CatalogueField<SkillSlotContribution> MapSlotContribution(
        CombatSkillSourceRecord record,
        CatalogueSourceReference source)
    {
        if (record.SpecificGrids.Length != 4
            || record.SpecificGrids.Any(value => value < 0)
            || record.GenericGrid < 0)
        {
            return CatalogueField<SkillSlotContribution>.Unsupported(
                "Configured slot contributions must contain four non-negative "
                + "specific values and one non-negative generic value.",
                source);
        }

        return CatalogueField<SkillSlotContribution>.Available(
            new SkillSlotContribution(
                record.SpecificGrids[0],
                record.SpecificGrids[1],
                record.SpecificGrids[2],
                record.SpecificGrids[3],
                record.GenericGrid),
            source);
    }

    private static IEnumerable<CombatSkillRequirementDefinition>
        MapRequirements(
            CombatSkillSourceRecord record,
            CombatSkillCatalogueMappingSources sources)
    {
        return record.Requirements.Select(requirement =>
        {
            var source = sources.GameDataRequirement(
                record.SkillId,
                requirement.SourceIndex);
            return new CombatSkillRequirementDefinition(
                new CombatSkillRequirementId(
                    $"character-property:{requirement.PropertyId}:"
                    + $"slot:{requirement.SourceIndex}"),
                requirement.RequiredValue >= 0
                    ? CatalogueField<int>.Available(
                        requirement.RequiredValue,
                        source)
                    : CatalogueField<int>.Unsupported(
                        $"Configured requirement value "
                        + $"{requirement.RequiredValue} is unsupported.",
                        source),
                source);
        });
    }

    private static CatalogueField<int> MapNonNegative(
        int value,
        string field,
        CatalogueSourceReference source) => value >= 0
            ? CatalogueField<int>.Available(value, source)
            : CatalogueField<int>.Unsupported(
                $"Configured {field} value {value} is unsupported.",
                source);

    private static CatalogueField<CombatSkillEffectId> MapEffect(
        int value,
        string direction,
        CatalogueSourceReference source) => value > 0
            ? CatalogueField<CombatSkillEffectId>.Available(
                new CombatSkillEffectId(value),
                source)
            : CatalogueField<CombatSkillEffectId>.Unavailable(
                $"No positive {direction} effect ID is configured.",
                source);

    private static IEnumerable<RawCombatSkillDescription> MapDescriptions(
        CombatSkillSourceRecord record,
        TaiwuLanguageCatalog traditionalChinese,
        TaiwuLanguageCatalog english,
        CombatSkillCatalogueMappingSources sources)
    {
        List<RawCombatSkillDescription> values = [];
        AddDescription(
            values,
            CatalogueLanguage.TraditionalChinese,
            traditionalChinese.Find(record.DescriptionKey),
            sources.TraditionalChineseDescription(record.SkillId));
        AddDescription(
            values,
            CatalogueLanguage.English,
            english.Find(record.DescriptionKey),
            sources.EnglishDescription(record.SkillId));
        return values;
    }

    private static void AddDescription(
        ICollection<RawCombatSkillDescription> descriptions,
        CatalogueLanguage language,
        string? value,
        CatalogueSourceReference source)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            descriptions.Add(new RawCombatSkillDescription(
                RawCombatSkillDescriptionKind.Effect,
                language,
                value,
                source));
        }
    }
}

internal sealed record CombatSkillCatalogueMappingSources(
    string GameDataIdentity,
    string TraditionalChineseIdentity,
    string EnglishIdentity)
{
    internal CatalogueSourceReference GameDataRecord(int skillId) => new(
        CatalogueSourceKind.GameData,
        GameDataIdentity,
        $"combat-skill:{skillId}");

    internal CatalogueSourceReference GameDataRequirement(
        int skillId,
        int index) => new(
            CatalogueSourceKind.GameData,
            GameDataIdentity,
            $"combat-skill:{skillId}:requirement:{index}");

    internal CatalogueSourceReference TraditionalChineseRecord(int skillId) =>
        new(
            CatalogueSourceKind.TraditionalChineseLanguageResource,
            TraditionalChineseIdentity,
            $"combat-skill-name:{skillId}");

    internal CatalogueSourceReference EnglishRecord(int skillId) => new(
        CatalogueSourceKind.EnglishLanguageResource,
        EnglishIdentity,
        $"combat-skill-name:{skillId}");

    internal CatalogueSourceReference TraditionalChineseDescription(
        int skillId) => new(
            CatalogueSourceKind.TraditionalChineseLanguageResource,
            TraditionalChineseIdentity,
            $"combat-skill-description:{skillId}");

    internal CatalogueSourceReference EnglishDescription(int skillId) => new(
        CatalogueSourceKind.EnglishLanguageResource,
        EnglishIdentity,
        $"combat-skill-description:{skillId}");
}
