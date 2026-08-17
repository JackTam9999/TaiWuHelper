using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using System.Net;
using System.Text.RegularExpressions;
using TaiWu.API.UnitTests.Controllers;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWuAPI.Components.Companions;
using TaiWuAPI.Components.Pages;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

[Collection(CompanionCandidatesApiCollection.Name)]
public sealed partial class CompanionFinderRenderingTests
{
    [Fact]
    public async Task English_result_uses_semantic_tables_visible_states_and_no_ids()
    {
        var result = await CompanionFinderTestData.ResultAsync();
        var disciplines = CompanionFinderViewModelMapper.MapDisciplines(
            CompanionFinderTestData.Disciplines(),
            TaiwuLanguage.English);
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline",
            disciplines);

        var html = await RenderResultsAsync(
            model,
            new CompanionFinderInteractionState(),
            comparison: null,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Synthetic martial discipline", text);
        Assert.Contains("Scores compare saved base qualification", text);
        Assert.Contains("Considered 9", text);
        Assert.Contains("Eligible 8", text);
        Assert.Contains("Needs review 5", text);
        Assert.Contains("Incomplete 3", text);
        Assert.Contains("Synthetic Person A", text);
        Assert.Contains("Synthetic Place A", text);
        Assert.Contains("Rank 1", text);
        Assert.Contains("Tied at rank 2", text);
        Assert.Contains("Saved base value confirmed", text);
        Assert.Contains("Evidence no longer current", text);
        Assert.Contains("Evidence unsupported", text);
        Assert.Contains("Evidence conflicts", text);
        Assert.Contains("Decisive strengths", text);
        Assert.Contains("Material limitations", text);
        Assert.Contains("Requirement evidence", text);
        Assert.Contains("All 5 requirements passed", text);
        Assert.Equal(
            model.Candidates.Count,
            Regex.Matches(
                html,
                "<details class=\"companion-candidate-evidence\"").Count);
        Assert.DoesNotContain(
            "<details class=\"companion-candidate-evidence\" open",
            html,
            StringComparison.Ordinal);
        Assert.Single(Regex.Matches(
            text,
            "Scores compare saved base qualification within this selected discipline only",
            RegexOptions.IgnoreCase));
        Assert.Contains("Candidate-universe eligibility · Passed", text);
        Assert.Contains(
            "Required saved base martial qualification evidence · Passed",
            text);
        Assert.Contains("data-requirement-order=\"1\"", html);
        Assert.Contains(
            "data-requirement-identity=\"CANDIDATE_UNIVERSE_ELIGIBLE\"",
            html);
        Assert.Contains("data-requirement-kind=\"CandidateUniverseEligible\"", html);
        Assert.Contains("data-requirement-field=\"BaseMartialQualification\"", html);
        Assert.Contains("data-gate-outcome=\"Passed\"", html);
        Assert.Contains(
            "data-reason-identity=\"CANDIDATE_UNIVERSE_ELIGIBLE\"",
            html);
        Assert.Contains("scope=\"col\"", html);
        Assert.Contains("scope=\"row\"", html);
        Assert.Contains("type=\"radio\"", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.DoesNotContain("31001", html, StringComparison.Ordinal);
        Assert.DoesNotContain("31009", html, StringComparison.Ordinal);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
        Assert.DoesNotContain("universal companion rank", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chinese_result_preserves_facts_with_complete_accessible_copy()
    {
        var result = await CompanionFinderTestData.ResultAsync();
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.Chinese,
            "範例武學類別");

        var html = await RenderResultsAsync(
            model,
            new CompanionFinderInteractionState(),
            comparison: null,
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("範例武學類別", text);
        Assert.Contains("分數只比較所選類別的存檔基礎資質", text);
        Assert.Contains("共計 9", text);
        Assert.Contains("符合資格 8", text);
        Assert.Contains("需檢查 5", text);
        Assert.Contains("資料不完整 3", text);
        Assert.Contains("範例人物甲", text);
        Assert.Contains("範例地點甲", text);
        Assert.Contains("第 1 名", text);
        Assert.Contains("並列第 2 名", text);
        Assert.Contains("已確認存檔基礎值", text);
        Assert.Contains("證據已非最新", text);
        Assert.Contains("已通過全部 5 項條件", text);
        Assert.Contains("姓名與位置只供顯示，不會改變資格", text);
        Assert.DoesNotContain("31001", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Comprehensive_objective_shows_breadth_and_all_three_averages_in_each_row()
    {
        var result = await CompanionFinderTestData.ResultAsync(
            comprehensiveObjective: true);
        var disciplines = CompanionFinderViewModelMapper.MapDisciplines(
            CompanionFinderTestData.Disciplines(),
            TaiwuLanguage.English);
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            disciplineName: null,
            disciplines);

        var html = await RenderResultsAsync(
            model,
            new CompanionFinderInteractionState(),
            comparison: null,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Comprehensive base capability", text);
        Assert.Contains("Breadth index", text);
        Assert.Contains("Synthetic Person A", text);
        Assert.Contains(">47.67</strong>", html);
        Assert.Contains("Six base attributes 53.5", text);
        Assert.Contains("14 martial aptitudes 51", text);
        Assert.Contains("16 life-skill aptitudes 38.5", text);
        Assert.Contains("class=\"companion-capability-row-summary\"", html);
        Assert.DoesNotContain(
            "Comprehensive base capability · Comprehensive base capability",
            text);
    }

    [Fact]
    public async Task Every_noncurrent_enrichment_state_renders_its_typed_action()
    {
        var result = await CompanionFinderTestData.ResultAsync();
        var baseModel = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");
        var states = new[]
        {
            (CompanionCandidateEnrichmentStatus.Partial,
                CombatSkillCatalogueStatus.Current),
            (CompanionCandidateEnrichmentStatus.CatalogueMissing,
                CombatSkillCatalogueStatus.Missing),
            (CompanionCandidateEnrichmentStatus.CatalogueMissing,
                CombatSkillCatalogueStatus.MissingSources),
            (CompanionCandidateEnrichmentStatus.CatalogueStale,
                CombatSkillCatalogueStatus.Stale),
            (CompanionCandidateEnrichmentStatus.CatalogueRebuilding,
                CombatSkillCatalogueStatus.Rebuilding),
            (CompanionCandidateEnrichmentStatus.CatalogueUnsupported,
                CombatSkillCatalogueStatus.UnsupportedVersion),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.SourceReadFailed),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.RepositoryFailed),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.Corrupt)
        };

        foreach (var (status, catalogueStatus) in states)
        {
            var enrichment = CompanionFinderViewModelMapper.MapEnrichment(
                status,
                catalogueStatus,
                TaiwuLanguage.English);
            var model = baseModel with
            {
                Enrichment = enrichment,
                IsPartial = true
            };
            var html = await RenderResultsAsync(
                model,
                new CompanionFinderInteractionState(),
                comparison: null,
                TaiwuLanguage.English);
            var text = VisibleText(html);

            Assert.Contains($"data-enrichment-status=\"{status}\"", html);
            Assert.Contains(
                $"data-catalogue-status=\"{catalogueStatus}\"",
                html);
            Assert.Contains(enrichment.Title, text);
            Assert.Contains(enrichment.Message, text);
        }
    }

    [Fact]
    public async Task Partial_snapshot_renders_separate_typed_source_guidance()
    {
        var result = await CompanionFinderTestData.ResultAsync(
            partialSnapshot: true);
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");

        var html = await RenderResultsAsync(
            model,
            new CompanionFinderInteractionState(),
            comparison: null,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("data-snapshot-status=\"Partial\"", html);
        Assert.Contains("Some candidate fields could not be read", text);
        Assert.DoesNotContain("data-enrichment-status", html);
    }

    [Fact]
    public async Task Ready_comparison_shows_same_facts_and_disables_third_selection()
    {
        var result = await CompanionFinderTestData.ResultAsync();
        var disciplines = CompanionFinderViewModelMapper.MapDisciplines(
            CompanionFinderTestData.Disciplines(),
            TaiwuLanguage.English);
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline",
            disciplines);
        var state = new CompanionFinderInteractionState();
        state.ToggleComparison(model, 31002);
        state.ToggleComparison(model, 31003);
        var comparison = CompanionFinderViewModelMapper.MapComparison(
            result,
            model,
            31002,
            31003,
            TaiwuLanguage.English);

        var html = await RenderResultsAsync(
            model,
            state,
            comparison,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Comparison ready", text);
        Assert.Contains("Candidate comparison", text);
        Assert.Contains("Synthetic Person B", text);
        Assert.Contains("Synthetic Person C", text);
        Assert.Contains("Equal confirmed evidence", text);
        Assert.Contains("Capability overview", text);
        Assert.Contains("Breadth index", text);
        Assert.Contains("Six base attributes", text);
        Assert.Contains("14 martial aptitudes", text);
        Assert.Contains("16 life-skill aptitudes", text);
        Assert.Contains("Synthetic Person B 48.29", text);
        Assert.Contains("Synthetic Person C 49.26", text);
        Assert.Contains("Intelligence 57", text);
        Assert.Contains("Martial discipline 1 75", text);
        Assert.Contains("descriptive only", text);
        Assert.Contains("Saved base qualification", text);
        Assert.Contains("Synthetic Person B 75", text);
        Assert.Contains("Synthetic Person C 75", text);
        Assert.Contains("Hard gates", text);
        Assert.Contains("Evaluation state", text);
        Assert.Equal(
            2,
            Regex.Matches(html, @">\s*Rankable\s*<").Count);
        Assert.Contains("Clear comparison", text);
        Assert.Contains("disabled", html);
        Assert.DoesNotContain("31002", html, StringComparison.Ordinal);
        Assert.DoesNotContain("31003", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Previous_result_is_labelled_inert_and_has_disabled_controls()
    {
        var result = await CompanionFinderTestData.ResultAsync();
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");

        var html = await RenderResultsAsync(
            model,
            new CompanionFinderInteractionState(),
            comparison: null,
            TaiwuLanguage.English,
            isPrevious: true);
        var text = VisibleText(html);

        Assert.Contains("Previous result", text);
        Assert.Contains("Draft controls changed", text);
        Assert.Contains("aria-disabled=\"true\"", html);
        Assert.Contains("inert", html);
        Assert.Contains("disabled", html);
    }

    [Fact]
    public async Task Initial_page_has_native_objective_controls_without_reading_save()
    {
        var finder = Substitute.For<IFindCompanionCandidates>();
        var source = Substitute.For<ICompanionDisciplineDisplaySource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            CompanionFinderTestData.Disciplines());

        var html = await RenderPageAsync(
            finder,
            source,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Companion finder", text);
        Assert.Contains(
            "current saved Taiwu group roster excluding the Taiwu player",
            text);
        Assert.Contains(
            "Membership and living-state evidence determine eligibility",
            text);
        Assert.Contains("Martial discipline aptitude", text);
        Assert.Contains("Life-skill discipline aptitude", text);
        Assert.Contains("Comprehensive base capability", text);
        Assert.True(
            text.IndexOf("Comprehensive base capability", StringComparison.Ordinal)
            < text.IndexOf("Martial discipline aptitude", StringComparison.Ordinal));
        Assert.DoesNotContain("Choose a discipline", text);
        Assert.Contains("Find candidates", text);
        Assert.Contains("type=\"radio\"", html);
        Assert.Matches(
            "id=\"companion-role-capability\"[^>]*checked",
            html);
        Assert.DoesNotContain("<select", html);
        var findButton = Assert.Single(
            System.Text.RegularExpressions.Regex.Matches(
                html,
                "<button[^>]*class=\"primary-button\"[^>]*>"));
        Assert.DoesNotContain("disabled", findButton.Value);
        Assert.Empty(finder.ReceivedCalls());
    }

    [Fact]
    public async Task Missing_installed_labels_have_safe_bilingual_recovery()
    {
        var finder = Substitute.For<IFindCompanionCandidates>();
        var source = Substitute.For<ICompanionDisciplineDisplaySource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            new CompanionDisciplineDisplayResult(
                CompanionDisciplineDisplayStatus.Unavailable,
                disciplines: [],
                "DISCIPLINE_LANGUAGE_READ_FAILED"));

        var html = await RenderPageAsync(
            finder,
            source,
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("無法取得類別名稱", text);
        Assert.Contains("請檢查受信任的遊戲安裝後重試", text);
        Assert.Contains("重新讀取名稱", text);
        Assert.DoesNotContain("DISCIPLINE_LANGUAGE_READ_FAILED", text);
        Assert.Empty(finder.ReceivedCalls());
    }

    private static async Task<string> RenderResultsAsync(
        CompanionFinderViewModel model,
        CompanionFinderInteractionState state,
        CompanionComparisonViewModel? comparison,
        TaiwuLanguage language,
        bool isPrevious = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
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
                                builder.OpenComponent<
                                    CompanionCandidateResults>(0);
                                builder.AddAttribute(
                                    1,
                                    nameof(CompanionCandidateResults.Model),
                                    model);
                                builder.AddAttribute(
                                    2,
                                    nameof(CompanionCandidateResults.State),
                                    state);
                                builder.AddAttribute(
                                    3,
                                    nameof(CompanionCandidateResults.Comparison),
                                    comparison);
                                builder.AddAttribute(
                                    4,
                                    nameof(CompanionCandidateResults.IsPrevious),
                                    isPrevious);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static async Task<string> RenderPageAsync(
        IFindCompanionCandidates finder,
        ICompanionDisciplineDisplaySource source,
        TaiwuLanguage language)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(finder);
        services.AddSingleton(source);
        services.AddSingleton(Substitute.For<IJSRuntime>());
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
                                builder.OpenComponent<CompanionFinder>(0);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static string VisibleText(string html)
    {
        var withoutTags = HtmlTagPattern().Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespacePattern().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();
}
