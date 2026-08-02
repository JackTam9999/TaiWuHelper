using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Components.Pages;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class SkillCatalogueRenderingTests
{
    [Fact]
    public async Task Skill_detail_separates_static_and_current_state_with_accessible_study_map()
    {
        var definition = DetailedDefinition(includeEnglishName: true);
        var progress = DetailedProgress();
        var (source, repository) = CurrentDetail(definition);
        var reader = DetailedProgressReader(progress);

        var html = await RenderDetailPageAsync(
            source,
            repository,
            reader,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Black Blood Gu", text);
        Assert.DoesNotContain("黑血蠱降", text);
        Assert.DoesNotContain("Chinese and English names", text);
        Assert.Contains("Faction Ranshan Sect Available", text);
        Assert.Contains("Static definition", text);
        Assert.Contains("Current Taiwu state", text);
        Assert.Contains("Base grid cost 3 Available", text);
        Assert.Contains("Current effective cost 2 Available", text);
        Assert.Contains("Current proficiency 70 Available", text);
        Assert.Contains("Maximum proficiency 100 Available", text);
        Assert.Contains("Proficiency percentage 70% Available", text);
        Assert.Contains("Breakthrough completed Yes Available", text);
        Assert.Contains("Active direction Reverse practice Available", text);
        Assert.Contains("Attainment mastery Yes Available", text);
        Assert.Contains("Exact verified details not studied", text);
        Assert.Contains("Direct detail 3", text);
        Assert.Contains("Common", text);
        Assert.Contains("Direct", text);
        Assert.Contains("Reverse", text);
        Assert.Contains("Studied", text);
        Assert.Contains("Not studied", text);
        Assert.Contains("Unavailable", text);
        Assert.Contains("Display-only raw text", text);
        Assert.Contains("Raw effect description.", text);
        Assert.DoesNotContain("原始效果描述", text);
        Assert.Contains("Source and availability", text);
        Assert.DoesNotContain("Opened from a combat recommendation", text);
        Assert.Contains("data-study-status=\"studied\"", html);
        Assert.Contains("data-study-status=\"not-studied\"", html);
        Assert.Contains("data-study-status=\"unavailable\"", html);
        Assert.Contains("role=\"list\"", html);
        Assert.DoesNotContain("<img", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<svg", html, StringComparison.OrdinalIgnoreCase);
        await reader.Received(1).ReadAsync(
            Arg.Is<CharacterCombatSkillProgressReadRequest>(request =>
                request != null
                && request.CharacterId == null
                && request.PreferredLanguage == CatalogueLanguage.English),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recommendation_context_remains_visible_when_catalogue_is_missing()
    {
        var definition = DetailedDefinition(includeEnglishName: true);
        var source = Source([definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Missing,
                sourceIdentity: null,
                definitionCount: 0,
                builtAtUtc: null));
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();

        var html = await RenderDetailPageAsync(
            source,
            repository,
            reader,
            TaiwuLanguage.English,
            context: "recommendation");
        var text = VisibleText(html);

        Assert.Contains("Opened from a combat recommendation", text);
        Assert.Contains("Back to recommendations", text);
        Assert.Contains(
            "Catalogue availability and raw descriptions do not change recommendation feasibility",
            text);
        Assert.Contains(
            "Skill detail is unavailable until the local catalogue is current",
            text);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task Recommendation_context_remains_visible_when_catalogue_is_stale()
    {
        var definition = DetailedDefinition(includeEnglishName: true);
        var source = Source([definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        var staleIdentity = new CombatSkillCatalogueSourceIdentity(
            "1.0.0-test",
            1,
            new string('F', 64),
            new string('A', 64),
            new string('B', 64));
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                staleIdentity,
                definitionCount: 1,
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")));
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();

        var text = VisibleText(await RenderDetailPageAsync(
            source,
            repository,
            reader,
            TaiwuLanguage.English,
            context: "recommendation"));

        Assert.Contains("Opened from a combat recommendation", text);
        Assert.Contains(
            "Skill detail is unavailable until the local catalogue is current",
            text);
        Assert.Contains("Stale", text);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task Recommendation_context_handles_a_missing_static_definition()
    {
        var definition = DetailedDefinition(includeEnglishName: true);
        var source = Source([definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                CurrentIdentity,
                definitionCount: 1,
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")));
        repository.GetAsync(456, Arg.Any<CancellationToken>())
            .Returns((CombatSkillDefinition?)null);
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.SaveMissing(
                "test save missing"));

        var text = VisibleText(await RenderDetailPageAsync(
            source,
            repository,
            reader,
            TaiwuLanguage.English,
            context: "recommendation"));

        Assert.Contains("Opened from a combat recommendation", text);
        Assert.Contains("No static combat-skill definition matches this ID", text);
        Assert.Contains("Back to recommendations", text);
    }

    [Fact]
    public async Task Skill_detail_labels_name_fallback_explicitly()
    {
        var definition = DetailedDefinition(includeEnglishName: false);
        var progress = DetailedProgress();
        var (source, repository) = CurrentDetail(definition);

        var text = VisibleText(await RenderDetailPageAsync(
            source,
            repository,
            DetailedProgressReader(progress),
            TaiwuLanguage.English));

        Assert.Contains("黑血蠱降", text);
        Assert.DoesNotContain("Traditional Chinese name", text);
        Assert.DoesNotContain("English name", text);
        Assert.DoesNotContain("Chinese and English names", text);
        Assert.Contains("Partial or fallback data", text);
    }

    [Fact]
    public async Task Unsupported_progress_keeps_static_detail_and_translates_state()
    {
        var definition = DetailedDefinition(includeEnglishName: true);
        var (source, repository) = CurrentDetail(definition);
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.UnsupportedVersion(
                "unsupported test save"));

        var text = VisibleText(await RenderDetailPageAsync(
            source,
            repository,
            reader,
            TaiwuLanguage.Chinese));

        Assert.Contains("靜態定義", text);
        Assert.Contains("不支援人物進度對應", text);
        Assert.Contains("此存檔版本尚無經驗證的進度對應", text);
        Assert.Contains("尚未讀取研讀進度", text);
        Assert.DoesNotContain("目前造詣", text);
    }

    private static async Task<string> RenderDetailPageAsync(
        ICombatSkillDefinitionSource source,
        ICombatSkillCatalogueRepository repository,
        ICharacterCombatSkillProgressReader progressReader,
        TaiwuLanguage language,
        string? context = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(source);
        services.AddSingleton(repository);
        services.AddSingleton(progressReader);
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
                                builder.OpenComponent<SkillDetail>(0);
                                builder.AddAttribute(
                                    1,
                                    nameof(SkillDetail.SkillId),
                                    456);
                                builder.AddAttribute(
                                    2,
                                    nameof(SkillDetail.Context),
                                    context);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static (
        ICombatSkillDefinitionSource Source,
        ICombatSkillCatalogueRepository Repository) CurrentDetail(
            CombatSkillDefinition definition)
    {
        var source = Source([definition]);
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>())
            .Returns(new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                CurrentIdentity,
                definitionCount: 1,
                DateTimeOffset.Parse("2026-08-02T12:00:00Z")));
        repository.GetAsync(definition.SkillId, Arg.Any<CancellationToken>())
            .Returns(definition);
        return (source, repository);
    }

    private static ICharacterCombatSkillProgressReader DetailedProgressReader(
        CharacterCombatSkillProgress progress)
    {
        var reader = Substitute.For<ICharacterCombatSkillProgressReader>();
        reader.ReadAsync(
                Arg.Any<CharacterCombatSkillProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CharacterCombatSkillProgressReadResult.Available(
                new CharacterCombatSkillProgressMetadata(
                    progress.SaveSnapshot,
                    "1.0.0-test",
                    [
                        new CharacterCombatSkillProgressWarning(
                            "TEST_PARTIAL",
                            "One verified study detail is unavailable.")
                    ]),
                [progress]));
        return reader;
    }

    private static CombatSkillDefinition DetailedDefinition(
        bool includeEnglishName)
    {
        var gameSource = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            "combat-skill:456");
        var chineseSource = new CatalogueSourceReference(
            CatalogueSourceKind.TraditionalChineseLanguageResource,
            "language-cnh:test",
            "combat-skill-name:456");
        var englishSource = new CatalogueSourceReference(
            CatalogueSourceKind.EnglishLanguageResource,
            "language-en:test",
            "combat-skill-name:456");
        var names = new List<LocalizedCombatSkillName>
        {
            new(
                CatalogueLanguage.TraditionalChinese,
                "黑血蠱降",
                chineseSource)
        };
        if (includeEnglishName)
        {
            names.Add(new LocalizedCombatSkillName(
                CatalogueLanguage.English,
                "Black Blood Gu",
                englishSource));
        }

        return new CombatSkillDefinition(
            456,
            new CombatSkillLocalizedNames(names),
            CatalogueField<CombatSkillDiscipline>.Available(
                CombatSkillDiscipline.Finger,
                gameSource),
            CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(3),
                gameSource),
            CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(7),
                gameSource),
            CatalogueField<CombatSkillElement>.Available(
                CombatSkillElement.Wood,
                gameSource),
            CatalogueField<CombatSkillEquipmentType>.Available(
                CombatSkillEquipmentType.Attack,
                gameSource),
            CatalogueField<CombatSkillGridCost>.Available(
                new CombatSkillGridCost(3),
                gameSource),
            CatalogueField<SkillSlotContribution>.Available(
                new SkillSlotContribution(1, 0, 0, 0, 1),
                gameSource),
            [
                new CombatSkillRequirementDefinition(
                    new CombatSkillRequirementId("minimum-attainment"),
                    CatalogueField<int>.Available(30, gameSource),
                    gameSource)
            ],
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Available(100, gameSource),
                CatalogueField<int>.Available(20, gameSource),
                CatalogueField<int>.Available(80, gameSource)),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(1001),
                    gameSource),
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(1002),
                    gameSource),
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "No neutral effect is defined.",
                    gameSource)),
            [
                new RawCombatSkillDescription(
                    RawCombatSkillDescriptionKind.Effect,
                    CatalogueLanguage.TraditionalChinese,
                    "原始效果描述",
                    chineseSource),
                new RawCombatSkillDescription(
                    RawCombatSkillDescriptionKind.Effect,
                    CatalogueLanguage.English,
                    "Raw effect description.",
                    englishSource)
            ],
            gameSource);
    }

    private static CharacterCombatSkillProgress DetailedProgress()
    {
        var save = new SaveSnapshotIdentity(
            new string('D', 64),
            DateTimeOffset.Parse("2026-08-02T13:00:00Z"));
        var source = new SkillProgressSource(
            SkillProgressSourceKind.SaveSnapshot,
            $"save:{save.Sha256}",
            "combat-skill:456");
        var labelSource = new CatalogueSourceReference(
            CatalogueSourceKind.EnglishLanguageResource,
            "language-en:test",
            "study-detail:456");
        var details = Enumerable.Range(0, 15)
            .Select(index =>
            {
                var group = index < 5
                    ? CombatSkillStudyDetailGroup.Outline
                    : index < 10
                        ? CombatSkillStudyDetailGroup.Direct
                        : CombatSkillStudyDetailGroup.Reverse;
                var groupIndex = index % 5 + 1;
                var groupName = group == CombatSkillStudyDetailGroup.Outline
                    ? "Common"
                    : group.ToString();
                var readState = index == 12
                    ? SkillProgressField<CombatSkillStudyState>.Unavailable(
                        "The verified bit could not be decoded.",
                        source)
                    : SkillProgressField<CombatSkillStudyState>.Available(
                        index == 7
                            ? CombatSkillStudyState.NotRead
                            : CombatSkillStudyState.Read,
                        source);
                return new CombatSkillStudyDetailProgress(
                    $"{group.ToString().ToLowerInvariant()}-{groupIndex}",
                    index,
                    group,
                    CatalogueField<string>.Available(
                        $"{groupName} detail {groupIndex}",
                        labelSource),
                    readState,
                    SkillProgressField<bool>.Available(index == 6, source));
            })
            .ToArray();

        return new CharacterCombatSkillProgress(
            42,
            save,
            456,
            SkillProgressField<bool>.Available(true, source),
            new CombatSkillProficiencyProgress(
                SkillProgressField<int>.Available(70, source),
                SkillProgressField<int>.Available(100, source),
                SkillProgressField<decimal>.Available(70m, source)),
            details,
            SkillProgressField<BreakthroughDirectionAvailability>.Available(
                new BreakthroughDirectionAvailability(true, false, []),
                source),
            SkillProgressField<PracticeDirection>.Available(
                PracticeDirection.Reverse,
                source),
            SkillProgressField<bool>.Available(true, source),
            SkillProgressField<bool>.Available(true, source),
            SkillProgressField<bool>.Available(true, source),
            SkillProgressField<bool>.Available(true, source));
    }
}
