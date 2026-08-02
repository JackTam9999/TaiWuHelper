using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSkills;

public sealed class CombatSkillDefinitionTests
{
    [Fact]
    public void Definition_exposes_typed_static_fields()
    {
        var definition = CreateDefinition(skillId: 456);

        Assert.Equal(456, definition.SkillId);
        Assert.Equal(
            CombatSkillDiscipline.Finger,
            definition.Category.Value);
        Assert.Equal(5, definition.Grade.Value.Value);
        Assert.Equal(15, definition.Faction.Value.Value);
        Assert.Equal(CombatSkillElement.Wood, definition.Element.Value);
        Assert.Equal(
            CombatSkillEquipmentType.Attack,
            definition.EquipmentType.Value);
        Assert.Equal(3, definition.BaseGridCost.Value.Value);
        Assert.Equal(2, definition.SlotContribution.Value.Attack);
        Assert.Equal(1, definition.SlotContribution.Value.Generic);
        Assert.Equal(331, definition.Effects.Direct.Value.Value);
        Assert.Equal(1057, definition.Effects.Reverse.Value.Value);
    }

    [Fact]
    public void Stable_skill_id_is_definition_identity()
    {
        var first = CreateDefinition(
            skillId: 456,
            names: Names((CatalogueLanguage.English, "First")));
        var second = CreateDefinition(
            skillId: 456,
            names: Names((CatalogueLanguage.English, "Second")));
        var other = CreateDefinition(skillId: 498);

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
        Assert.True(first != other);
    }

    [Fact]
    public void Localized_names_resolve_preferred_then_other_language()
    {
        var names = Names(
            (CatalogueLanguage.TraditionalChinese, "黑血蠱降"),
            (CatalogueLanguage.English, "Corruptive Gu Infection"));

        var chinese = names.Resolve(CatalogueLanguage.TraditionalChinese);
        var english = names.Resolve(CatalogueLanguage.English);

        Assert.Equal("黑血蠱降", chinese.Value.Text);
        Assert.Equal(
            CatalogueLanguage.TraditionalChinese,
            chinese.Value.Language);
        Assert.Equal(
            CatalogueSourceKind.TraditionalChineseLanguageResource,
            chinese.Value.Source.Kind);
        Assert.Equal("Corruptive Gu Infection", english.Value.Text);
        Assert.Equal(CatalogueLanguage.English, english.Value.Language);
        Assert.Equal(
            CatalogueSourceKind.EnglishLanguageResource,
            english.Value.Source.Kind);
    }

    [Fact]
    public void Missing_preferred_name_uses_deterministic_fallback()
    {
        var names = Names(
            (CatalogueLanguage.TraditionalChinese, "黑血蠱降"));

        var resolved = names.Resolve(CatalogueLanguage.English);

        Assert.True(resolved.IsAvailable);
        Assert.Equal(
            CatalogueLanguage.TraditionalChinese,
            resolved.Value.Language);
        Assert.Equal("黑血蠱降", resolved.Value.Text);
    }

    [Fact]
    public void No_localized_names_is_explicitly_unavailable()
    {
        var names = new CombatSkillLocalizedNames();

        var resolved = names.Resolve(CatalogueLanguage.English);

        Assert.Equal(CatalogueFieldStatus.Unavailable, resolved.Status);
        Assert.Contains("No Traditional Chinese", resolved.Reason);
        Assert.Throws<InvalidOperationException>(() => resolved.Value);
    }

    [Fact]
    public void Duplicate_language_names_are_rejected()
    {
        var source = EnglishSource();

        Assert.Throws<ArgumentException>(
            () => new CombatSkillLocalizedNames(
            [
                new LocalizedCombatSkillName(
                    CatalogueLanguage.English,
                    "First",
                    source),
                new LocalizedCombatSkillName(
                    CatalogueLanguage.English,
                    "Second",
                    source)
            ]));
    }

    [Fact]
    public void Unsupported_enum_is_distinct_from_unknown_available_enum()
    {
        var source = GameDataSource();
        var unsupported = CatalogueField<CombatSkillDiscipline>.Unsupported(
            "Installed value 99 is not mapped.",
            source);

        var definition = CreateDefinition(category: unsupported);

        Assert.Equal(
            CatalogueFieldStatus.Unsupported,
            definition.Category.Status);
        Assert.Contains("99", definition.Category.Reason);

        var invalid = CatalogueField<CombatSkillDiscipline>.Available(
            (CombatSkillDiscipline)99,
            source);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateDefinition(category: invalid));
    }

    [Fact]
    public void Unavailable_value_preserves_reason_without_a_value()
    {
        var field = CatalogueField<CombatSkillFactionId>.Unavailable(
            "Faction is absent in this version.");

        Assert.False(field.IsAvailable);
        Assert.Equal(CatalogueFieldStatus.Unavailable, field.Status);
        Assert.Equal("Faction is absent in this version.", field.Reason);
        Assert.Null(field.Source);
        Assert.Throws<InvalidOperationException>(() => field.Value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void Invalid_grade_is_rejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillGrade(value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Invalid_base_grid_cost_is_rejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillGridCost(value));
    }

    [Fact]
    public void Negative_available_timing_is_rejected_but_unavailable_is_valid()
    {
        var source = GameDataSource();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillTimingDefinition(
                CatalogueField<int>.Available(-1, source),
                CatalogueField<int>.Unavailable("Not configured."),
                CatalogueField<int>.Unavailable("Not configured.")));

        var timing = new CombatSkillTimingDefinition(
            CatalogueField<int>.Unavailable("Not configured."),
            CatalogueField<int>.Unavailable("Not configured."),
            CatalogueField<int>.Unavailable("Not configured."));
        Assert.Equal(
            CatalogueFieldStatus.Unavailable,
            timing.PreparationProgress.Status);
    }

    [Fact]
    public void Duplicate_requirement_ids_are_rejected()
    {
        var source = GameDataSource();
        var requirement = new CombatSkillRequirementDefinition(
            new CombatSkillRequirementId("attribute:strength"),
            CatalogueField<int>.Available(80, source),
            source);

        Assert.Throws<ArgumentException>(
            () => CreateDefinition(requirements: [requirement, requirement]));
    }

    [Fact]
    public void Collections_are_immutable_defensive_copies()
    {
        var source = GameDataSource();
        List<CombatSkillRequirementDefinition> requirements =
        [
            new CombatSkillRequirementDefinition(
                new CombatSkillRequirementId("attribute:strength"),
                CatalogueField<int>.Available(80, source),
                source)
        ];
        List<RawCombatSkillDescription> descriptions =
        [
            new RawCombatSkillDescription(
                RawCombatSkillDescriptionKind.Effect,
                CatalogueLanguage.English,
                "Imported display text.",
                EnglishSource())
        ];
        var definition = CreateDefinition(
            requirements: requirements,
            descriptions: descriptions);

        requirements.Clear();
        descriptions.Clear();

        Assert.Single(definition.Requirements);
        Assert.Single(definition.RawDescriptions);
        Assert.False(definition.RawDescriptions[0].IsVerifiedMechanic);
    }

    [Theory]
    [InlineData(@"C:\game\GameData.dll")]
    [InlineData("../GameData.dll")]
    [InlineData("Language_EN/CombatSkill")]
    public void Source_record_identity_cannot_be_a_path(string identity)
    {
        Assert.Throws<ArgumentException>(
            () => new CatalogueSourceReference(
                CatalogueSourceKind.GameData,
                "gamedata:1.0.0",
                identity));
    }

    private static CombatSkillDefinition CreateDefinition(
        int skillId = 456,
        CombatSkillLocalizedNames? names = null,
        CatalogueField<CombatSkillDiscipline>? category = null,
        IEnumerable<CombatSkillRequirementDefinition>? requirements = null,
        IEnumerable<RawCombatSkillDescription>? descriptions = null)
    {
        var source = GameDataSource();
        return new CombatSkillDefinition(
            skillId,
            names ?? Names(
                (CatalogueLanguage.TraditionalChinese, "黑血蠱降"),
                (CatalogueLanguage.English, "Corruptive Gu Infection")),
            category ?? CatalogueField<CombatSkillDiscipline>.Available(
                CombatSkillDiscipline.Finger,
                source),
            CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(5),
                source),
            CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(15),
                source),
            CatalogueField<CombatSkillElement>.Available(
                CombatSkillElement.Wood,
                source),
            CatalogueField<CombatSkillEquipmentType>.Available(
                CombatSkillEquipmentType.Attack,
                source),
            CatalogueField<CombatSkillGridCost>.Available(
                new CombatSkillGridCost(3),
                source),
            CatalogueField<SkillSlotContribution>.Available(
                new SkillSlotContribution(
                    attack: 2,
                    agility: 0,
                    defense: 0,
                    assistance: 0,
                    generic: 1),
                source),
            requirements,
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Available(39000, source),
                CatalogueField<int>.Available(100, source),
                CatalogueField<int>.Available(0, source)),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(331),
                    source),
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(1057),
                    source),
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "No neutral effect is configured.",
                    source)),
            descriptions,
            source);
    }

    private static CombatSkillLocalizedNames Names(
        params (CatalogueLanguage Language, string Text)[] values)
    {
        return new CombatSkillLocalizedNames(
            values.Select(value => new LocalizedCombatSkillName(
                value.Language,
                value.Text,
                value.Language == CatalogueLanguage.English
                    ? EnglishSource()
                    : ChineseSource())));
    }

    private static CatalogueSourceReference GameDataSource() =>
        new(
            CatalogueSourceKind.GameData,
            "gamedata:1.0.0+68032f25",
            "combat-skill:456");

    private static CatalogueSourceReference ChineseSource() =>
        new(
            CatalogueSourceKind.TraditionalChineseLanguageResource,
            "language-cnh:9932b589",
            "combat-skill-name:456");

    private static CatalogueSourceReference EnglishSource() =>
        new(
            CatalogueSourceKind.EnglishLanguageResource,
            "language-en:f89c3b8a",
            "combat-skill-name:456");
}
