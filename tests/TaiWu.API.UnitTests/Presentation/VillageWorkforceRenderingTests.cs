using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using System.Net;
using System.Text.RegularExpressions;
using TaiWu.Application.Localization;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using TaiWuAPI.Contracts.VillageWorkforce;
using TaiWuAPI.Components.Pages;
using TaiWuAPI.Components.VillageWorkforce;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class VillageWorkforceRenderingTests
{
    [Fact]
    public async Task English_result_is_compact_accessible_and_hides_raw_ids()
    {
        var result = await VillageWorkforcePresentationTestData.ResultAsync();
        var model = VillageWorkforceViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            targetOrdinal: 1);

        var html = await RenderResultsAsync(
            model,
            new VillageWorkforceInteractionState(),
            comparison: null,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Workforce result", text);
        Assert.Contains("Shop manager position 1", text);
        Assert.Contains("Current worker", text);
        Assert.Contains("Alternative worker", text);
        Assert.Contains("Saved base life-skill qualification", text);
        Assert.Contains("64", text);
        Assert.Contains("72", text);
        Assert.Contains("Tied", text);
        Assert.Contains("Incomplete", text);
        Assert.Contains("type=\"radio\"", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("scope=\"col\"", html);
        Assert.Contains("scope=\"row\"", html);
        Assert.Contains("aria-live=\"polite\"", html);
        Assert.Equal(
            model.Candidates.Count,
            Regex.Matches(
                html,
                "<details class=\"workforce-candidate-evidence\"").Count);
        Assert.DoesNotContain(
            "<details class=\"workforce-candidate-evidence\" open",
            html,
            StringComparison.Ordinal);
        foreach (var rawId in new[]
                 {
                     "41001", "41002", "41003", "41004",
                     "Shop area", "building 33", "life-skill type 6"
                 })
        {
            Assert.DoesNotContain(rawId, html, StringComparison.Ordinal);
        }

        Assert.Single(Regex.Matches(
            text,
            "Saved base life-skill qualification is the only ordering component",
            RegexOptions.IgnoreCase));
    }

    [Fact]
    public async Task Chinese_result_preserves_the_same_facts_and_states()
    {
        var result = await VillageWorkforcePresentationTestData.ResultAsync();
        var model = VillageWorkforceViewModelMapper.Map(
            result,
            TaiwuLanguage.Chinese,
            targetOrdinal: 1);

        var html = await RenderResultsAsync(
            model,
            new VillageWorkforceInteractionState(),
            comparison: null,
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("商鋪管理位置 1", text);
        Assert.Contains("目前人員", text);
        Assert.Contains("替代人員", text);
        Assert.Contains("存檔基礎技藝資質", text);
        Assert.Contains("資質點數", text);
        Assert.Contains("並列", text);
        Assert.Contains("資料不完整", text);
        Assert.DoesNotContain("41001", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Local_filter_and_comparison_reuse_the_authoritative_result()
    {
        var result = await VillageWorkforcePresentationTestData.ResultAsync();
        var model = VillageWorkforceViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            targetOrdinal: 1);
        var state = new VillageWorkforceInteractionState();
        state.SetFilter(WorkforceShortlistFilter.Comparable);
        Assert.Equal(3, state.VisibleCandidates(model).Count);
        state.SetNameQuery("Alternative worker 2");
        Assert.Single(state.VisibleCandidates(model));
        state.SetNameQuery(null);
        state.ToggleComparison(model.Current.CharacterId);
        state.ToggleComparison(model.Candidates[0].CharacterId);
        Assert.True(state.ComparisonReady);
        Assert.True(state.IsSelectionDisabled(model.Candidates[^1].CharacterId));

        var comparison = VillageWorkforceViewModelMapper.MapComparison(
            result,
            model,
            state.SelectedCharacterIds[0],
            state.SelectedCharacterIds[1],
            TaiwuLanguage.English);
        var html = await RenderResultsAsync(
            model,
            state,
            comparison,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Comparison ready", text);
        Assert.Contains("Worker comparison", text);
        Assert.Contains("Manual checklist", text);
        Assert.Contains("No action was sent to the game", text);
        Assert.Contains("disabled", html);
    }

    [Fact]
    public async Task Initial_page_discovers_targets_without_evaluating_workers()
    {
        var snapshot = VillageWorkforcePresentationTestData.Snapshot();
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var finder = Substitute.For<IFindVillageWorkforce>();

        var html = await RenderPageAsync(
            finder,
            reader,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Village workforce planner", text);
        Assert.Contains("Shop manager base aptitude", text);
        Assert.Contains("Shop manager position 1", text);
        Assert.Contains("Inspect position", text);
        Assert.Contains("<select", html);
        Assert.DoesNotContain("41001", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Shop area", html, StringComparison.Ordinal);
        Assert.Empty(finder.ReceivedCalls());
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Every_worker_state_and_previous_result_have_visible_text_cues()
    {
        var candidates = new[]
        {
            Candidate(1, true, VillageWorkforceApiEvaluationState.Ranked, "Ranked", "comparable", 1, 60),
            Candidate(2, false, VillageWorkforceApiEvaluationState.Tied, "Tied", "comparable", 1, 60),
            Candidate(3, false, VillageWorkforceApiEvaluationState.CurrentOnly, "Current assignment only", "needs-review"),
            Candidate(4, false, VillageWorkforceApiEvaluationState.Ineligible, "Ineligible", "ineligible"),
            Candidate(5, false, VillageWorkforceApiEvaluationState.Incomplete, "Incomplete", "needs-review"),
            Candidate(6, false, VillageWorkforceApiEvaluationState.Unsupported, "Unsupported", "needs-review"),
            Candidate(7, false, VillageWorkforceApiEvaluationState.Conflicting, "Conflicting", "needs-review")
        };
        var model = new VillageWorkforceViewModel(
            VillageWorkforceApiStatus.Partial,
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
            IsPartial: true,
            "Shop manager base aptitude",
            "Synthetic objective",
            "1.0.0",
            "Shop manager position 1",
            new VillageWorkforceCountsResponse(
                Total: 7,
                Comparable: 2,
                Ranked: 1,
                Tied: 1,
                CurrentOnly: 1,
                Ineligible: 1,
                Incomplete: 1,
                Unsupported: 1,
                Conflicting: 1,
                Visible: 7),
            candidates[0],
            candidates,
            ["Synthetic shared limitation"]);

        var html = await RenderResultsAsync(
            model,
            new VillageWorkforceInteractionState(),
            comparison: null,
            TaiwuLanguage.English,
            isPrevious: true);
        var text = VisibleText(html);

        foreach (var state in new[]
                 {
                     "Ranked", "Tied", "Current assignment only",
                     "Ineligible", "Incomplete", "Unsupported", "Conflicting"
                 })
        {
            Assert.Contains(state, text);
        }

        Assert.Contains("Previous result", text);
        Assert.Contains("inert", html);
        Assert.Contains("disabled", html);
    }

    [Theory]
    [InlineData(VillageWorkforceFinderStatus.SaveUnavailable)]
    [InlineData(VillageWorkforceFinderStatus.UnsupportedSourceVersion)]
    [InlineData(VillageWorkforceFinderStatus.ConflictingSources)]
    [InlineData(VillageWorkforceFinderStatus.ChangedRevision)]
    [InlineData(VillageWorkforceFinderStatus.TargetNotFound)]
    [InlineData(VillageWorkforceFinderStatus.ReadFailed)]
    public void Failure_states_have_safe_bilingual_guidance(
        VillageWorkforceFinderStatus status)
    {
        var english = VillageWorkforceViewModelMapper.MapFailure(
            status,
            TaiwuLanguage.English);
        var chinese = VillageWorkforceViewModelMapper.MapFailure(
            status,
            TaiwuLanguage.Chinese);

        Assert.False(string.IsNullOrWhiteSpace(english.Title));
        Assert.False(string.IsNullOrWhiteSpace(english.Message));
        Assert.False(string.IsNullOrWhiteSpace(chinese.Title));
        Assert.False(string.IsNullOrWhiteSpace(chinese.Message));
        Assert.NotEqual(english.Title, chinese.Title);
    }

    private static async Task<string> RenderResultsAsync(
        VillageWorkforceViewModel model,
        VillageWorkforceInteractionState state,
        VillageWorkforceComparisonViewModel? comparison,
        TaiwuLanguage language,
        bool isPrevious = false)
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
                                builder.OpenComponent<
                                    VillageWorkforceResults>(0);
                                builder.AddAttribute(
                                    1,
                                    nameof(VillageWorkforceResults.Model),
                                    model);
                                builder.AddAttribute(
                                    2,
                                    nameof(VillageWorkforceResults.State),
                                    state);
                                builder.AddAttribute(
                                    3,
                                    nameof(VillageWorkforceResults.Comparison),
                                    comparison);
                                builder.AddAttribute(
                                    4,
                                    nameof(VillageWorkforceResults.IsPrevious),
                                    isPrevious);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static async Task<string> RenderPageAsync(
        IFindVillageWorkforce finder,
        IVillageWorkforceSnapshotReader reader,
        TaiwuLanguage language)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(finder);
        services.AddSingleton(reader);
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
                                builder.OpenComponent<VillageWorkforce>(0);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static string VisibleText(string html)
    {
        var withoutTags = HtmlTagPattern().Replace(html, " ");
        return Regex.Replace(
            WebUtility.HtmlDecode(withoutTags),
            @"\s+",
            " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    private static VillageWorkforceCandidateViewModel Candidate(
        int ordinal,
        bool current,
        VillageWorkforceApiEvaluationState state,
        string stateLabel,
        string cssClass,
        int? rank = null,
        decimal? total = null) => new(
            42000 + ordinal,
            ordinal,
            $"Synthetic worker {ordinal}",
            current,
            state,
            stateLabel,
            cssClass,
            rank,
            total,
            "qualification points",
            stateLabel,
            PassedRequirements: 0,
            Requirements: [],
            Components: []);
}
