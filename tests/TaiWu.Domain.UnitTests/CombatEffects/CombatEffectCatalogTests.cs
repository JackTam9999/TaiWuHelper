using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatEffects;

public sealed class CombatEffectCatalogTests
{
    private const string Version = "1.0.0+test";

    [Fact]
    public void Catalog_records_exact_GameData_version()
    {
        var catalog = VerifiedCombatEffectCatalogs.GoldenAntiMagic;

        Assert.Equal(
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion,
            catalog.GameDataVersion);
        Assert.Equal(12, catalog.Entries.Length);
    }

    [Fact]
    public void Direct_and_reverse_effects_remain_distinct()
    {
        var catalog = VerifiedCombatEffectCatalogs.GoldenAntiMagic;

        var direct = catalog.Resolve(
            catalog.GameDataVersion,
            skillId: 604,
            PracticeDirection.Direct,
            rawEffectId: 338);
        var reverse = catalog.Resolve(
            catalog.GameDataVersion,
            skillId: 604,
            PracticeDirection.Reverse,
            rawEffectId: 1064);

        Assert.True(direct.IsRecognized);
        Assert.True(reverse.IsRecognized);
        Assert.NotEqual(direct.RawEffectId, reverse.RawEffectId);
        Assert.Contains(
            CombatEffectMechanic.SuppressEnemyReversePractice,
            direct.CatalogEntry!.Mechanics);
        Assert.Contains(
            CombatEffectMechanic.SuppressEnemyDirectPractice,
            reverse.CatalogEntry!.Mechanics);
    }

    [Fact]
    public void Raw_id_text_and_source_reference_are_preserved()
    {
        var catalog = VerifiedCombatEffectCatalogs.GoldenAntiMagic;

        var result = catalog.Resolve(
            catalog.GameDataVersion,
            skillId: 686,
            PracticeDirection.Reverse,
            rawEffectId: 1422);

        var entry = Assert.IsType<CombatEffectCatalogEntry>(
            result.CatalogEntry);
        Assert.Equal(1422, entry.RawEffectId);
        Assert.Contains("消除妨害标记", entry.RawSourceText);
        Assert.Equal(
            "local-config:Language_CN/SpecialEffect_language.txt"
            + "#Desc_1422_0",
            entry.SourceReference);
    }

    [Fact]
    public void Unknown_effect_id_remains_visible_and_is_not_guessed()
    {
        var catalog = CreateCatalog([]);

        var result = catalog.Resolve(
            Version,
            skillId: 999,
            PracticeDirection.Reverse,
            rawEffectId: 4321);

        Assert.False(result.IsRecognized);
        Assert.Equal(
            CombatEffectResolutionStatus.Unrecognized,
            result.Status);
        Assert.Equal(4321, result.RawEffectId);
        Assert.Equal(999, result.SkillId);
        Assert.Equal(PracticeDirection.Reverse, result.Direction);
        Assert.Null(result.CatalogEntry);
    }

    [Fact]
    public void Unmapped_source_entry_remains_visible_without_mechanics()
    {
        var entry = CreateEntry(mechanics: []);
        var result = CreateCatalog([entry]).Resolve(
            Version,
            entry.SkillId,
            entry.Direction,
            entry.RawEffectId);

        Assert.Equal(
            CombatEffectResolutionStatus.Unrecognized,
            result.Status);
        Assert.Same(entry, result.CatalogEntry);
        Assert.Equal("Verified source text.", entry.RawSourceText);
        Assert.Empty(entry.Mechanics);
    }

    [Fact]
    public void Version_mismatch_invalidates_otherwise_known_effect()
    {
        var entry = CreateEntry();

        var result = CreateCatalog([entry]).Resolve(
            observedGameDataVersion: "1.0.0+new",
            entry.SkillId,
            entry.Direction,
            entry.RawEffectId);

        Assert.Equal(
            CombatEffectResolutionStatus.VersionMismatch,
            result.Status);
        Assert.False(result.IsRecognized);
        Assert.Null(result.CatalogEntry);
        Assert.Equal("1.0.0+new", result.ObservedGameDataVersion);
    }

    [Fact]
    public void Changed_effect_id_is_visible_but_not_treated_as_match()
    {
        var entry = CreateEntry();

        var result = CreateCatalog([entry]).Resolve(
            Version,
            entry.SkillId,
            entry.Direction,
            rawEffectId: 2000);

        Assert.Equal(
            CombatEffectResolutionStatus.EffectIdMismatch,
            result.Status);
        Assert.Equal(2000, result.RawEffectId);
        Assert.Same(entry, result.CatalogEntry);
        Assert.False(result.IsRecognized);
    }

    [Fact]
    public void Duplicate_skill_direction_entries_are_rejected()
    {
        var first = CreateEntry();
        var second = CreateEntry(rawEffectId: 1001);

        var exception = Assert.Throws<ArgumentException>(
            () => CreateCatalog([first, second]));

        Assert.Contains("Duplicate", exception.Message);
    }

    [Fact]
    public void Entry_rejects_neutral_or_duplicate_mechanics()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateEntry(direction: PracticeDirection.Neutral));
        Assert.Throws<ArgumentException>(
            () => CreateEntry(
                mechanics:
                [
                    CombatEffectMechanic.RemoveOwnHindranceMarks,
                    CombatEffectMechanic.RemoveOwnHindranceMarks
                ]));
    }

    [Fact]
    public void Resolution_rejects_invalid_observation_identity()
    {
        var catalog = CreateCatalog([]);

        Assert.Throws<ArgumentException>(
            () => catalog.Resolve(
                " ",
                skillId: 1,
                PracticeDirection.Direct,
                rawEffectId: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => catalog.Resolve(
                Version,
                skillId: -1,
                PracticeDirection.Direct,
                rawEffectId: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => catalog.Resolve(
                Version,
                skillId: 1,
                PracticeDirection.Neutral,
                rawEffectId: 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => catalog.Resolve(
                Version,
                skillId: 1,
                PracticeDirection.Direct,
                rawEffectId: -1));
    }

    private static CombatEffectCatalog CreateCatalog(
        CombatEffectCatalogEntry[] entries)
    {
        return new CombatEffectCatalog(Version, entries);
    }

    private static CombatEffectCatalogEntry CreateEntry(
        int rawEffectId = 1000,
        PracticeDirection direction = PracticeDirection.Reverse,
        CombatEffectMechanic[]? mechanics = null)
    {
        return new CombatEffectCatalogEntry(
            skillId: 604,
            skillName: "Skill",
            direction,
            rawEffectId,
            rawSourceText: "Verified source text.",
            sourceReference: "local-config:effect-1000",
            mechanics
                ??
                [
                    CombatEffectMechanic.RemoveOwnHindranceMarks
                ]);
    }
}
