using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.RegularExpressions;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Components.Pages;
using TaiWuAPI.Components.Skills;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class SkillCatalogueRenderingTests
{
    [Fact]
    public async Task Current_atlas_renders_search_filters_freshness_and_skill_cards()
    {
        var definition = Definition(456, "Black Blood Gu");
        var progress = Progress(
            42,
            456,
            new BreakthroughDirectionAvailability(true, false, []),
            PracticeDirection.Direct,
            mastered: true,
            simplified: true,
            activated: true,
            equipped: true);
        var (source, repository) = Current([definition]);
        var reader = ProgressReader([progress]);

        var html = await RenderPageAsync(source, repository, reader);
        var decodedHtml = WebUtility.HtmlDecode(html);
        var text = VisibleText(html);

        Assert.Contains("Your martial arts, mapped.", text);
        Assert.Contains("Catalogue Current", text);
        Assert.Contains("Find a combat skill", text);
        Assert.Contains("Search in this language", html);
        Assert.Contains("Faction", text);
        Assert.Contains("All factions", text);
        Assert.Contains("data-faction-id=\"1\"", html);
        Assert.Contains("element-metal alignment-kind", html);
        Assert.Contains(
            "aria-label=\"Shaolin Sect · Metal Qi · Benevolent\"",
            decodedHtml);
        Assert.Contains("S Shaolin Sect Metal Qi · Benevolent", text);
        Assert.DoesNotContain("id=\"atlas-faction\"", html);
        Assert.Contains("Category All categories", text);
        Assert.Contains("Grade All grades", text);
        Assert.Contains("More catalogue and progress filters", text);
        Assert.Contains("Learned state", text);
        Assert.Contains("Breakthrough ready", text);
        Assert.Contains("Breakthrough completed", text);
        Assert.Contains("Attainment mastery", text);
        Assert.Contains("Equipped", text);
        Assert.Contains("Finger 1 skills", text);
        Assert.Contains("Shaolin Sect", text);
        Assert.Contains("Black Blood Gu", text);
        Assert.Contains("class=\"skill-glyph\"", html);
        Assert.Contains("<span>B</span>", decodedHtml);
        Assert.Contains("class=\"skill-atlas-card grade-5\"", html);
        Assert.Contains("data-grade=\"5\"", html);
        Assert.DoesNotContain("黑血蠱降", text);
        Assert.Contains("Learned", text);
        Assert.Contains("Broken through", text);
        Assert.Contains("Direct practice", text);
        Assert.Contains("Mastered", text);
        Assert.DoesNotContain("Mastered · Direct practice", text);
        Assert.Contains(
            "class=\"practice-marker direct\" data-practice-state=\"active\"",
            html);
        Assert.Contains("正 Black Blood Gu", text);
        Assert.Contains("<details", html);
        Assert.Contains("<summary", html);
        Assert.Contains("aria-busy=\"false\"", html);
        await reader.Received(1).ReadAsync(
            Arg.Is<CharacterCombatSkillProgressReadRequest>(request =>
                request != null
                && request.CharacterId == null
                && request.PreferredLanguage == CatalogueLanguage.English),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Atlas_groups_by_category_and_orders_each_group_by_grade()
    {
        var definitions = new[]
        {
            Definition(
                701,
                "Lower Finger Skill",
                CombatSkillDiscipline.Finger,
                grade: 2),
            Definition(
                702,
                "Higher Finger Skill",
                CombatSkillDiscipline.Finger,
                grade: 8),
            Definition(
                703,
                "Agility Skill",
                CombatSkillDiscipline.Agility,
                grade: 4)
        };
        var progress = definitions
            .Select(definition => Progress(
                42,
                definition.SkillId,
                new BreakthroughDirectionAvailability(true, false, []),
                PracticeDirection.Direct,
                mastered: false,
                simplified: false,
                activated: false,
                equipped: false))
            .ToArray();
        var (source, repository) = Current(definitions);

        var html = WebUtility.HtmlDecode(await RenderPageAsync(
            source,
            repository,
            ProgressReader(progress)));

        var agilityHeader = html.IndexOf(
            "id=\"skill-category-Agility\"",
            StringComparison.Ordinal);
        var fingerHeader = html.IndexOf(
            "id=\"skill-category-Finger\"",
            StringComparison.Ordinal);
        var higherSkill = html.IndexOf(
            "Higher Finger Skill",
            StringComparison.Ordinal);
        var lowerSkill = html.IndexOf(
            "Lower Finger Skill",
            StringComparison.Ordinal);

        Assert.True(agilityHeader >= 0);
        Assert.True(fingerHeader > agilityHeader);
        Assert.True(lowerSkill > fingerHeader);
        Assert.True(higherSkill > lowerSkill);
        Assert.Contains("class=\"skill-atlas-card grade-8\"", html);
        Assert.Contains("class=\"skill-atlas-card grade-2\"", html);
    }

    [Fact]
    public async Task Faction_picker_uses_active_language_initial_and_profile_labels()
    {
        var definition = Definition(456, "Black Blood Gu");
        var progress = Progress(
            42,
            456,
            new BreakthroughDirectionAvailability(true, false, []),
            PracticeDirection.Direct,
            mastered: true,
            simplified: true,
            activated: true,
            equipped: true);
        var (source, repository) = Current([definition]);

        var html = await RenderPageAsync(
            source,
            repository,
            ProgressReader([progress]),
            TaiwuLanguage.Chinese);
        var decodedHtml = WebUtility.HtmlDecode(html);
        var text = VisibleText(html);

        Assert.Contains("少 少林派 金剛 · 仁善", text);
        Assert.Contains("element-metal alignment-kind", html);
        Assert.Contains(
            "aria-label=\"少林派 · 金剛 · 仁善\"",
            decodedHtml);
        Assert.DoesNotContain("Shaolin Sect", text);
        Assert.DoesNotContain("Metal Qi", text);
        Assert.DoesNotContain("Benevolent", text);
    }

    [Fact]
    public async Task Missing_catalogue_renders_explicit_helper_cache_action()
    {
        var definition = Definition(456, "Black Blood Gu");
        var source = Source([definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Missing,
                sourceIdentity: null,
                definitionCount: 0,
                builtAtUtc: null));
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();

        var text = VisibleText(await RenderPageAsync(source, repository, reader));

        Assert.Contains("Local catalogue not built", text);
        Assert.Contains("Build local catalogue", text);
        Assert.DoesNotContain("Find a combat skill", text);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task Unsupported_sources_render_translated_nonrebuild_state()
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>())
            .Returns(CombatSkillDefinitionSourceResult.UnsupportedVersion(
                "Unsupported test source."));
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();

        var text = VisibleText(await RenderPageAsync(
            source,
            repository,
            reader,
            TaiwuLanguage.Chinese));

        Assert.Contains("不支援已安裝的版本", text);
        Assert.Contains("此已安裝的 GameData 版本尚無經驗證的匯入器", text);
        Assert.DoesNotContain("建立本機功法目錄", text);
    }

    [Fact]
    public async Task Skill_card_shows_only_supported_positive_progress_badges()
    {
        var definition = Definition(686, "Cloud Formula");
        var progress = Progress(
            42,
            686,
            new BreakthroughDirectionAvailability(
                false,
                true,
                [PracticeDirection.Reverse]),
            activeDirection: null,
            mastered: false,
            simplified: false,
            activated: false,
            equipped: false);
        var entry = Entry(definition, progress);

        var html = await RenderCardAsync(entry, TaiwuLanguage.Chinese);
        var decodedHtml = WebUtility.HtmlDecode(html);
        var text = VisibleText(html);

        Assert.Contains("class=\"skill-glyph\"", html);
        Assert.Contains("<span>雲</span>", decodedHtml);
        Assert.Contains("雲術", text);
        Assert.DoesNotContain("少林派", text);
        Assert.DoesNotContain("品級 5", text);
        Assert.Contains("已取得", text);
        Assert.Contains("可突破", text);
        Assert.Contains("突破 逆 雲術", text);
        Assert.Contains(
            "class=\"practice-marker reverse\" data-practice-state=\"available\"",
            html);
        Assert.DoesNotContain(
            "class=\"practice-marker direct\" data-practice-state=\"available\"",
            html);
        Assert.DoesNotContain("已突破", text);
        Assert.DoesNotContain("已大成", text);
        Assert.DoesNotContain("已裝備", text);
        Assert.Contains("15 / 15 已研讀", text);
        Assert.Contains("class=\"skill-study-strip\"", html);
        Assert.Equal(
            15,
            Regex.Matches(html, "data-study-status=\"studied\"").Count);
        Assert.Contains("href=\"/skills/686\"", html);
        Assert.Contains("開啟完整功法詳情", text);
        Assert.Contains("role", html);
    }

    [Fact]
    public async Task Breakthrough_marker_orders_both_available_directions_before_name()
    {
        var definition = Definition(686, "Cloud Formula");
        var progress = Progress(
            42,
            686,
            new BreakthroughDirectionAvailability(
                false,
                true,
                [PracticeDirection.Reverse, PracticeDirection.Direct]),
            activeDirection: null,
            mastered: false,
            simplified: false,
            activated: false,
            equipped: false);

        var html = await RenderCardAsync(
            Entry(definition, progress),
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("突破 正 逆 雲術", text);
        Assert.Contains(
            "class=\"practice-marker direct\" data-practice-state=\"available\"",
            html);
        Assert.Contains(
            "class=\"practice-marker reverse\" data-practice-state=\"available\"",
            html);
    }

    [Fact]
    public async Task Skill_card_colours_the_red_and_blue_study_paths()
    {
        var definition = Definition(686, "Cloud Formula");
        string[] labels =
        [
            "承", "合", "解", "異", "獨",
            "修", "思", "源", "參", "藏",
            "用", "奇", "巧", "化", "絕"
        ];
        var progress = Progress(
            42,
            686,
            new BreakthroughDirectionAvailability(false, false, []),
            activeDirection: null,
            mastered: false,
            simplified: false,
            activated: false,
            equipped: false,
            studyLabels: labels);

        var html = await RenderCardAsync(
            Entry(definition, progress),
            TaiwuLanguage.Chinese);

        Assert.Equal(5, Regex.Matches(html, "study-path-red").Count);
        Assert.Equal(5, Regex.Matches(html, "study-path-blue").Count);
    }

    [Fact]
    public async Task Completed_breakthrough_uses_direction_without_repeated_status()
    {
        var definition = Definition(686, "Cloud Formula");
        var progress = Progress(
            42,
            686,
            new BreakthroughDirectionAvailability(true, false, []),
            PracticeDirection.Reverse,
            mastered: false,
            simplified: false,
            activated: false,
            equipped: false);

        var html = await RenderCardAsync(
            Entry(definition, progress),
            TaiwuLanguage.Chinese);

        Assert.Contains(
            "class=\"practice-marker reverse\" data-practice-state=\"active\"",
            html);
        Assert.DoesNotContain("class=\"skill-card-status broken\"", html);
        Assert.Contains("class=\"progress-badge broken\"", html);
    }

    [Fact]
    public async Task Atlas_includes_catalogue_skills_without_save_progress()
    {
        var learned = Definition(456, "Learned Skill");
        var unlearned = Definition(457, "Unlearned Skill");
        var progress = Progress(
            42,
            learned.SkillId,
            new BreakthroughDirectionAvailability(false, false, []),
            activeDirection: null,
            mastered: false,
            simplified: false,
            activated: false,
            equipped: false);
        var (source, repository) = Current([learned, unlearned]);

        var html = await RenderPageAsync(
            source,
            repository,
            ProgressReader([progress]));
        var text = VisibleText(html);

        Assert.Contains("Unlearned Skill", text);
        Assert.Contains("Not learned", text);
        Assert.Contains("class=\"skill-card-status neutral\"", html);
    }

    [Fact]
    public async Task Skill_card_keeps_an_unavailable_status_distinct_from_not_learned()
    {
        var definition = Definition(456, "Black Blood Gu");
        var entry = new CharacterCombatSkillAtlasEntry(
            definition.SkillId,
            progress: null,
            definition,
            new CombatSkillDisplayName(
                CatalogueLanguage.TraditionalChinese,
                definition.Names.Resolve(
                    CatalogueLanguage.TraditionalChinese),
                UsedFallback: false),
            SkillProgressField<bool>.Unavailable("test status unavailable"),
            SkillProgressField<int>.Unavailable("test cost unavailable"));

        var text = VisibleText(await RenderCardAsync(
            entry,
            TaiwuLanguage.Chinese));

        Assert.Contains("狀態不可用", text);
        Assert.DoesNotContain("未取得", text);
    }

    private static async Task<string> RenderPageAsync(
        ICombatSkillDefinitionSource source,
        ICombatSkillCatalogueRepository repository,
        ICharacterCombatSkillProgressReader progressReader,
        TaiwuLanguage language = TaiwuLanguage.English)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(source);
        services.AddSingleton(repository);
        services.AddSingleton(progressReader);
        var factionProfiles = Substitute.For<
            ICombatSkillFactionProfileSource>();
        factionProfiles.ReadAsync(Arg.Any<CancellationToken>())
            .Returns(
            [
                new CombatSkillFactionProfile(
                    new CombatSkillFactionId(1),
                    CombatSkillElement.Metal,
                    CombatSkillFactionAlignment.Kind)
            ]);
        services.AddSingleton(factionProfiles);
        using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<
                CascadingValue<TaiwuLanguage>>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        [nameof(CascadingValue<TaiwuLanguage>.Value)] = language,
                        [nameof(CascadingValue<TaiwuLanguage>.ChildContent)] =
                            (RenderFragment)(builder =>
                            {
                                builder.OpenComponent<SkillCatalogue>(0);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static async Task<string> RenderCardAsync(
        CharacterCombatSkillAtlasEntry entry,
        TaiwuLanguage language)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<
                CascadingValue<TaiwuLanguage>>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        [nameof(CascadingValue<TaiwuLanguage>.Value)] = language,
                        [nameof(CascadingValue<TaiwuLanguage>.ChildContent)] =
                            (RenderFragment)(builder =>
                            {
                                builder.OpenComponent<SkillAtlasCard>(0);
                                builder.AddAttribute(
                                    1,
                                    nameof(SkillAtlasCard.Entry),
                                    entry);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static CharacterCombatSkillAtlasEntry Entry(
        CombatSkillDefinition definition,
        CharacterCombatSkillProgress progress) => new(
            definition.SkillId,
            progress,
            definition,
            new CombatSkillDisplayName(
                CatalogueLanguage.TraditionalChinese,
                definition.Names.Resolve(
                    CatalogueLanguage.TraditionalChinese),
                UsedFallback: false),
            progress.Learned,
            SkillProgressField<int>.Available(
                2,
                new SkillProgressSource(
                    SkillProgressSourceKind.VerifiedRule,
                    "verified-rule:test",
                    "effective-grid-cost")));

    private static (
        ICombatSkillDefinitionSource Source,
        ICombatSkillCatalogueRepository Repository) Current(
            IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                CurrentIdentity,
                definitions.Count,
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(definitions);
        return (Source(definitions), repository);
    }

    private static ICombatSkillDefinitionSource Source(
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>())
            .Returns(CombatSkillDefinitionSourceResult.Available(
                CurrentIdentity,
                definitions));
        return source;
    }

    private static ICharacterCombatSkillProgressReader ProgressReader(
        IReadOnlyList<CharacterCombatSkillProgress> progress)
    {
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.Available(
                new CharacterCombatSkillProgressMetadata(
                    progress[0].SaveSnapshot,
                    "1.0.0-test"),
                progress));
        return reader;
    }

    private static CombatSkillDefinition Definition(
        int skillId,
        string english,
        CombatSkillDiscipline category = CombatSkillDiscipline.Finger,
        int grade = 5)
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(
                [
                    new LocalizedCombatSkillName(
                        CatalogueLanguage.TraditionalChinese,
                        skillId == 686 ? "雲術" : "黑血蠱降",
                        new CatalogueSourceReference(
                            CatalogueSourceKind.TraditionalChineseLanguageResource,
                            "language-cnh:test",
                            $"combat-skill-name:{skillId}")),
                    new LocalizedCombatSkillName(
                        CatalogueLanguage.English,
                        english,
                        new CatalogueSourceReference(
                            CatalogueSourceKind.EnglishLanguageResource,
                            "language-en:test",
                            $"combat-skill-name:{skillId}"))
                ]),
            CatalogueField<CombatSkillDiscipline>.Available(
                category,
                source),
            CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(grade),
                source),
            CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(1),
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
            CatalogueField<SkillSlotContribution>.Unavailable("test"),
            requirements: null,
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Unavailable("test"),
                CatalogueField<int>.Unavailable("test"),
                CatalogueField<int>.Unavailable("test")),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Unavailable("test"),
                CatalogueField<CombatSkillEffectId>.Unavailable("test"),
                CatalogueField<CombatSkillEffectId>.Unavailable("test")),
            rawDescriptions: null,
            source);
    }

    private static CharacterCombatSkillProgress Progress(
        int characterId,
        int skillId,
        BreakthroughDirectionAvailability breakthrough,
        PracticeDirection? activeDirection,
        bool mastered,
        bool simplified,
        bool activated,
        bool equipped,
        IReadOnlyList<string>? studyLabels = null)
    {
        var source = new SkillProgressSource(
            SkillProgressSourceKind.SaveSnapshot,
            $"save:{new string('E', 64)}",
            "test");
        var labels = studyLabels
            ?? Enumerable.Range(1, 15)
                .Select(index => $"Detail {index}")
                .ToArray();
        var details = labels
            .Select((label, index) => new CombatSkillStudyDetailProgress(
                $"outline-{index}",
                index,
                CombatSkillStudyDetailGroup.Outline,
                CatalogueField<string>.Available(
                    label,
                    new CatalogueSourceReference(
                        CatalogueSourceKind.EnglishLanguageResource,
                        "language-en:test",
                        $"detail:{index}")),
                SkillProgressField<CombatSkillStudyState>.Available(
                    CombatSkillStudyState.Read,
                    source),
                SkillProgressField<bool>.Available(activated, source)))
            .ToArray();
        return new CharacterCombatSkillProgress(
            characterId,
            new SaveSnapshotIdentity(
                new string('E', 64),
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")),
            skillId,
            SkillProgressField<bool>.Available(true, source),
            new CombatSkillProficiencyProgress(
                SkillProgressField<int>.Available(50, source),
                SkillProgressField<int>.Available(100, source),
                SkillProgressField<decimal>.Available(50m, source)),
            details,
            SkillProgressField<BreakthroughDirectionAvailability>.Available(
                breakthrough,
                source),
            activeDirection.HasValue
                ? SkillProgressField<PracticeDirection>.Available(
                    activeDirection.Value,
                    source)
                : SkillProgressField<PracticeDirection>.Unavailable("test"),
            SkillProgressField<bool>.Available(mastered, source),
            SkillProgressField<bool>.Available(simplified, source),
            SkillProgressField<bool>.Available(activated, source),
            SkillProgressField<bool>.Available(equipped, source));
    }

    private static string VisibleText(string html)
    {
        var withoutTags = HtmlTagPattern().Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespacePattern().Replace(decoded, " ").Trim();
    }

    private static CombatSkillCatalogueSourceIdentity CurrentIdentity { get; } =
        new(
            "1.0.0-test",
            1,
            new string('0', 64),
            new string('A', 64),
            new string('B', 64));

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();
}
