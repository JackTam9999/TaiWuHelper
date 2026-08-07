using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class CombatSkillDefinitionMapperTests
{
    [Fact]
    public void Maps_complete_bilingual_static_definition_with_provenance()
    {
        var definition = CombatSkillDefinitionMapper.Map(
            Record(),
            Catalog(
                ("Name_456", "黑血蠱降"),
                ("Desc_456", "繁體效果")),
            Catalog(
                ("Name_456", "Corruptive Gu Infection"),
                ("Desc_456", "English effect")),
            Catalog(
                ("Desc_331_0", "繁體正練效果"),
                ("Desc_1057_0", "繁體逆練效果")),
            Catalog(
                ("Desc_331_0", "English direct effect"),
                ("Desc_1057_0", "English reverse effect")),
            Sources());

        Assert.Equal(456, definition.SkillId);
        Assert.Equal(
            "黑血蠱降",
            definition.Names.Get(CatalogueLanguage.TraditionalChinese)
                .Value.Text);
        Assert.Equal(
            "Corruptive Gu Infection",
            definition.Names.Get(CatalogueLanguage.English).Value.Text);
        Assert.Equal(CombatSkillDiscipline.Finger, definition.Category.Value);
        Assert.Equal(5, definition.Grade.Value.Value);
        Assert.Equal(15, definition.Faction.Value.Value);
        Assert.Equal(CombatSkillElement.Wood, definition.Element.Value);
        Assert.Equal(
            CombatSkillEquipmentType.Attack,
            definition.EquipmentType.Value);
        Assert.Equal(3, definition.BaseGridCost.Value.Value);
        Assert.Equal(2, definition.SlotContribution.Value.Attack);
        Assert.Equal(1, definition.SlotContribution.Value.Generic);
        Assert.Equal(39000, definition.Timing.PreparationProgress.Value);
        Assert.Equal(100, definition.Timing.BreathStanceCost.Value);
        Assert.Equal(25, definition.Timing.CastSpeed.Value);
        Assert.Equal(331, definition.Effects.Direct.Value.Value);
        Assert.Equal(1057, definition.Effects.Reverse.Value.Value);
        Assert.False(definition.Effects.Neutral.IsAvailable);

        var requirement = Assert.Single(definition.Requirements);
        Assert.Equal("character-property:17:slot:0", requirement.RequirementId.Value);
        Assert.Equal(60, requirement.RequiredValue.Value);
        Assert.Equal(CatalogueSourceKind.GameData, requirement.Source.Kind);

        Assert.Collection(
            definition.RawDescriptions,
            traditionalChinese =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.Effect,
                    traditionalChinese.Kind);
                Assert.Equal(
                    CatalogueLanguage.TraditionalChinese,
                    traditionalChinese.Language);
                Assert.Equal("繁體效果", traditionalChinese.Text);
                Assert.False(traditionalChinese.IsVerifiedMechanic);
            },
            english =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.Effect,
                    english.Kind);
                Assert.Equal(CatalogueLanguage.English, english.Language);
                Assert.Equal("English effect", english.Text);
                Assert.False(english.IsVerifiedMechanic);
            },
            traditionalChineseDirect =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.DirectEffect,
                    traditionalChineseDirect.Kind);
                Assert.Equal(
                    CatalogueLanguage.TraditionalChinese,
                    traditionalChineseDirect.Language);
                Assert.Equal("繁體正練效果", traditionalChineseDirect.Text);
                Assert.Equal(
                    "special-effect-description:331",
                    traditionalChineseDirect.Source.RecordIdentity);
            },
            englishDirect =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.DirectEffect,
                    englishDirect.Kind);
                Assert.Equal(
                    CatalogueLanguage.English,
                    englishDirect.Language);
                Assert.Equal("English direct effect", englishDirect.Text);
            },
            traditionalChineseReverse =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.ReverseEffect,
                    traditionalChineseReverse.Kind);
                Assert.Equal(
                    CatalogueLanguage.TraditionalChinese,
                    traditionalChineseReverse.Language);
                Assert.Equal("繁體逆練效果", traditionalChineseReverse.Text);
                Assert.Equal(
                    "special-effect-description:1057",
                    traditionalChineseReverse.Source.RecordIdentity);
            },
            englishReverse =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.ReverseEffect,
                    englishReverse.Kind);
                Assert.Equal(
                    CatalogueLanguage.English,
                    englishReverse.Language);
                Assert.Equal("English reverse effect", englishReverse.Text);
            });
        Assert.Equal(
            "gamedata:test",
            definition.SourceRecord.SourceIdentity);
    }

    [Fact]
    public void Missing_language_is_independent_and_domain_fallback_remains_deterministic()
    {
        var definition = CombatSkillDefinitionMapper.Map(
            Record(),
            Catalog(("Name_456", "黑血蠱降")),
            Catalog(),
            Catalog(),
            Catalog(),
            Sources());

        Assert.False(
            definition.Names.Get(CatalogueLanguage.English).IsAvailable);
        var fallback = definition.Names.Resolve(CatalogueLanguage.English);
        Assert.True(fallback.IsAvailable);
        Assert.Equal(CatalogueLanguage.TraditionalChinese, fallback.Value.Language);
        Assert.Equal(
            CatalogueSourceKind.TraditionalChineseLanguageResource,
            fallback.Source!.Kind);
    }

    [Fact]
    public void Malformed_fields_are_imported_as_explicit_nonavailable_values()
    {
        var malformed = Record() with
        {
            Category = 99,
            Grade = 99,
            Faction = -1,
            Element = 99,
            EquipmentType = 99,
            BaseGridCost = 0,
            SpecificGrids = [1, -1],
            GenericGrid = -1,
            PreparationProgress = -1,
            BreathStanceCost = -2,
            CastSpeed = -3,
            DirectEffectId = 0,
            ReverseEffectId = -1,
            Requirements =
            [
                new CombatSkillRequirementSourceValue(17, -10, 0)
            ]
        };

        var definition = CombatSkillDefinitionMapper.Map(
            malformed,
            Catalog(),
            Catalog(),
            Catalog(),
            Catalog(),
            Sources());

        Assert.Equal(CatalogueFieldStatus.Unsupported, definition.Category.Status);
        Assert.Equal(CatalogueFieldStatus.Unsupported, definition.Grade.Status);
        Assert.Equal(CatalogueFieldStatus.Unsupported, definition.Faction.Status);
        Assert.Equal(CatalogueFieldStatus.Unsupported, definition.Element.Status);
        Assert.Equal(
            CatalogueFieldStatus.Unsupported,
            definition.EquipmentType.Status);
        Assert.Equal(
            CatalogueFieldStatus.Unavailable,
            definition.BaseGridCost.Status);
        Assert.Equal(
            CatalogueFieldStatus.Unsupported,
            definition.SlotContribution.Status);
        Assert.Equal(
            CatalogueFieldStatus.Unsupported,
            definition.Timing.PreparationProgress.Status);
        Assert.False(definition.Effects.Direct.IsAvailable);
        Assert.Equal(
            CatalogueFieldStatus.Unsupported,
            Assert.Single(definition.Requirements).RequiredValue.Status);
        Assert.Empty(definition.Names.Values);
        Assert.Empty(definition.RawDescriptions);
    }

    [Fact]
    public void Source_record_uses_immutable_copies_of_collection_values()
    {
        var specific = new[] { 2, 0, 0, 0 };
        var requirements = new[]
        {
            new CombatSkillRequirementSourceValue(17, 60, 0)
        };
        var record = Record() with
        {
            SpecificGrids = specific.ToImmutableArray(),
            Requirements = requirements.ToImmutableArray()
        };

        specific[0] = 99;
        requirements[0] = new CombatSkillRequirementSourceValue(99, 99, 0);

        Assert.Equal(2, record.SpecificGrids[0]);
        Assert.Equal(17, record.Requirements[0].PropertyId);
    }

    private static CombatSkillSourceRecord Record() => new(
        SkillId: 456,
        NameKey: "Name_456",
        DescriptionKey: "Desc_456",
        Category: (int)CombatSkillDiscipline.Finger,
        Grade: 5,
        Faction: 15,
        Element: (int)CombatSkillElement.Wood,
        EquipmentType: (int)CombatSkillEquipmentType.Attack,
        BaseGridCost: 3,
        SpecificGrids: [2, 0, 0, 0],
        GenericGrid: 1,
        PreparationProgress: 39000,
        BreathStanceCost: 100,
        CastSpeed: 25,
        DirectEffectId: 331,
        ReverseEffectId: 1057,
        Requirements:
        [
            new CombatSkillRequirementSourceValue(17, 60, 0)
        ]);

    private static TaiwuLanguageCatalog Catalog(
        params (string Key, string Value)[] values) => new(
            values.ToDictionary(
                value => value.Key,
                value => value.Value,
                StringComparer.Ordinal));

    private static CombatSkillCatalogueMappingSources Sources() => new(
        "gamedata:test",
        "language-cnh:test",
        "language-en:test",
        "special-effect-language-cnh:test",
        "special-effect-language-en:test");
}
