using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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
        Assert.Contains("Comparable", text);
        Assert.Contains("type=\"radio\"", html);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.Contains("scope=\"col\"", html);
        Assert.Contains("scope=\"row\"", html);
        Assert.Contains("aria-live=\"polite\"", html);
        Assert.Equal(
            new VillageWorkforceInteractionState()
                .VisibleCandidates(model).Count + 1,
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
        Assert.Contains("可比較", text);
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
        Assert.Equal(2, state.VisibleCandidates(model).Count);
        state.SetNameQuery("Alternative worker 2");
        Assert.Single(state.VisibleCandidates(model));
        state.SetNameQuery(null);
        state.ToggleComparison(
            model.Current.CharacterId,
            model.Current.CharacterId);
        state.ToggleComparison(
            model.Candidates[0].CharacterId,
            model.Current.CharacterId);
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
        Assert.Contains("Requirements", text);
        Assert.Contains("Components", text);
        Assert.Contains("Provenance", text);
        var comparisonTable = Regex.Match(
            html,
            "<table class=\"workforce-comparison-table\".*?</table>",
            RegexOptions.Singleline).Value;
        Assert.Equal(
            10,
            Regex.Matches(comparisonTable, "<td data-label=").Count);
        Assert.Equal(
            state.VisibleCandidates(model).Count + 3,
            Regex.Matches(
                html,
                "<details class=\"workforce-candidate-evidence\"").Count);
        Assert.Matches(
            $"<input[^>]+id=\"workforce-compare-{model.Candidates[0].DisplayOrdinal}\"[^>]+checked",
            html);
        Assert.Contains("disabled", html);
    }

    [Fact]
    public async Task Initial_shortlist_is_bounded_and_full_results_are_paged()
    {
        var candidates = Enumerable.Range(1, 314)
            .Select(ordinal => Candidate(
                ordinal,
                current: ordinal == 1,
                ordinal == 1
                    ? VillageWorkforceApiEvaluationState.CurrentOnly
                    : VillageWorkforceApiEvaluationState.Ranked,
                ordinal == 1 ? "Current assignment only" : "Ranked",
                ordinal == 1 ? "needs-review" : "comparable",
                rank: ordinal == 1 ? null : ordinal - 1,
                total: ordinal == 1 ? null : 1000 - ordinal))
            .ToArray();
        var model = Model(candidates);
        var state = new VillageWorkforceInteractionState();

        var initial = state.VisibleCandidates(model);

        Assert.Equal(10, initial.Count);
        Assert.DoesNotContain(initial, candidate => candidate.IsCurrent);
        Assert.True(state.HasMoreCompactCandidates(model));

        var compactHtml = await RenderResultsAsync(
            model,
            state,
            comparison: null,
            TaiwuLanguage.English);
        Assert.Equal(
            VillageWorkforceInteractionState.DefaultAlternativeLimit + 1,
            Regex.Matches(
                compactHtml,
                "<details class=\"workforce-candidate-evidence\"").Count);
        Assert.Contains("Show all matching workers", compactHtml);

        state.ShowAllMatches();
        var firstPage = state.VisibleCandidates(model);
        Assert.Equal(VillageWorkforceInteractionState.PageSize, firstPage.Count);
        Assert.Equal(13, state.PageCount(model));
        state.NextPage(model);
        Assert.Equal(1, state.PageIndex);
        Assert.NotEqual(
            firstPage[0].CharacterId,
            state.VisibleCandidates(model)[0].CharacterId);

        state.ToggleComparison(candidates[20].CharacterId, candidates[0].CharacterId);
        Assert.Equal(
            [candidates[0].CharacterId, candidates[20].CharacterId],
            state.SelectedCharacterIds);
        state.ToggleComparison(candidates[20].CharacterId, candidates[0].CharacterId);
        Assert.Empty(state.SelectedCharacterIds);
    }

    [Fact]
    public async Task Display_enrichment_drives_labels_search_and_descriptive_context()
    {
        var snapshot = VillageWorkforcePresentationTestData.Snapshot();
        var displays = snapshot.Workers.Select((worker, index) =>
            new VillageWorkerDisplay(
                worker.Identity,
                $"範例人員{index + 1}",
                $"Synthetic Person {index + 1}",
                "太吾村",
                "Taiwu Village",
                new VillageWorkerCapabilityDisplay(
                    worker.Identity,
                    Enumerable.Repeat<short>(checked((short)(50 + index)), 6),
                    Enumerable.Repeat<short>(checked((short)(60 + index)), 14),
                    Enumerable.Repeat<short>(checked((short)(70 + index)), 16))))
            .ToArray();
        var read = VillageWorkforceSnapshotReadResult.Complete(
            snapshot,
            displays,
            [new VillageWorkforceTargetDisplay(
                snapshot.Targets[0].Identity,
                "茶館",
                "Tea house",
                "太吾村",
                "Taiwu Village",
                "品鑑",
                "Appraisal")]);
        var result = new BuildVillageWorkforce().Execute(
            read,
            new VillageWorkforceFinderRequest(
                snapshot.Targets[0].Identity,
                new WorkforceObjectiveIdentity(
                    WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
                    VerifiedVillageWorkforceRules.ObjectiveVersion)),
            TestContext.Current.CancellationToken);
        var model = VillageWorkforceViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            targetOrdinal: 1);
        var state = new VillageWorkforceInteractionState();

        Assert.Contains("Tea house", model.TargetLabel);
        Assert.All(model.Candidates, candidate =>
        {
            Assert.StartsWith("Synthetic Person", candidate.Label);
            Assert.Equal("Taiwu Village", candidate.LocationLabel);
            Assert.NotNull(candidate.CapabilitySummary);
            Assert.Equal("6/6", candidate.CapabilitySummary!
                .MainAttributes.CoverageLabel);
            Assert.Equal("14/14", candidate.CapabilitySummary
                .MartialDisciplines.CoverageLabel);
            Assert.Equal("16/16", candidate.CapabilitySummary
                .LifeSkillDisciplines.CoverageLabel);
        });
        state.SetNameQuery("Person 2");
        Assert.Equal("Synthetic Person 2", Assert.Single(
            state.VisibleCandidates(model)).Label);

        var html = await RenderResultsAsync(
            model,
            new VillageWorkforceInteractionState(),
            comparison: null,
            TaiwuLanguage.English);
        var text = VisibleText(html);
        Assert.Contains("Six attributes average", text);
        Assert.Contains("Martial aptitudes average", text);
        Assert.Contains("Life-skill aptitudes average", text);
        Assert.Single(Regex.Matches(
            text,
            "descriptive saved context only",
            RegexOptions.IgnoreCase));
    }

    [Fact]
    public async Task Initial_page_discovers_targets_without_evaluating_workers()
    {
        var snapshot = VillageWorkforcePresentationTestData.Snapshot();
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        var discoveryToken = CancellationToken.None;
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                discoveryToken = call.ArgAt<CancellationToken>(1);
                return VillageWorkforceSnapshotReadResult.Complete(
                    snapshot,
                    workerDisplays: [new VillageWorkerDisplay(
                        snapshot.CurrentAssignments[0].Worker,
                        "目前掌櫃",
                        "Current steward",
                        "太吾村",
                        "Taiwu Village")],
                    targetDisplays: [new VillageWorkforceTargetDisplay(
                        snapshot.Targets[0].Identity,
                        "茶館",
                        "Tea house",
                        "太吾村",
                        "Taiwu Village",
                        "品鑑",
                        "Appraisal")]);
            });
        var finder = Substitute.For<IFindVillageWorkforce>();

        var html = await RenderPageAsync(
            finder,
            reader,
            TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Village workforce planner", text);
        Assert.Contains("Shop manager base aptitude", text);
        Assert.Contains("Tea house", text);
        Assert.Contains("Current steward", text);
        Assert.Contains("Inspect position", text);
        Assert.Equal(2, Regex.Matches(html, "<select").Count);
        Assert.DoesNotContain("41001", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Shop area", html, StringComparison.Ordinal);
        Assert.Empty(finder.ReceivedCalls());
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
        Assert.True(discoveryToken.CanBeCanceled);
        Assert.True(discoveryToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Discovery_failure_remaps_when_language_changes()
    {
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(VillageWorkforceSnapshotReadResult.Failed(
                VillageWorkforceSnapshotReadStatus.SaveUnavailable,
                "CONFIGURED_SAVE_UNAVAILABLE",
                "Synthetic internal message"));
        var finder = Substitute.For<IFindVillageWorkforce>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(finder);
        services.AddSingleton(reader);
        services.AddSingleton<BuildVillageWorkforce>();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        var languageState = new LanguageHostState();
        services.AddSingleton(languageState);
        using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<LanguageHost>();
            var english = VisibleText(root.ToHtmlString());

            await languageState.SetLanguageAsync(TaiwuLanguage.Chinese);
            var chinese = VisibleText(root.ToHtmlString());
            var expectedEnglish = VillageWorkforceViewModelMapper
                .MapDiscoveryFailure(
                    VillageWorkforceSnapshotReadStatus.SaveUnavailable,
                    TaiwuLanguage.English);
            var expectedChinese = VillageWorkforceViewModelMapper
                .MapDiscoveryFailure(
                    VillageWorkforceSnapshotReadStatus.SaveUnavailable,
                    TaiwuLanguage.Chinese);

            Assert.Contains(expectedEnglish.Title, english);
            Assert.DoesNotContain(expectedEnglish.Title, chinese);
            Assert.Contains(expectedChinese.Title, chinese);
        });
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

        var state = new VillageWorkforceInteractionState();
        state.SetFilter(WorkforceShortlistFilter.All);
        var html = await RenderResultsAsync(
            model,
            state,
            comparison: null,
            TaiwuLanguage.English,
            isPrevious: true);
        var text = VisibleText(html);

        foreach (var stateLabel in new[]
                 {
                     "Ranked", "Tied", "Current assignment only",
                     "Ineligible", "Incomplete", "Unsupported", "Conflicting"
                 })
        {
            Assert.Contains(stateLabel, text);
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
        services.AddSingleton<BuildVillageWorkforce>();
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
            "Synthetic location",
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

    private static VillageWorkforceViewModel Model(
        IReadOnlyList<VillageWorkforceCandidateViewModel> candidates) => new(
            VillageWorkforceApiStatus.Complete,
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
            IsPartial: false,
            "Shop manager base aptitude",
            "Synthetic objective",
            "1.0.0",
            "Synthetic shop",
            new VillageWorkforceCountsResponse(
                Total: candidates.Count,
                Comparable: candidates.Count(item => item.State is
                    VillageWorkforceApiEvaluationState.Ranked
                    or VillageWorkforceApiEvaluationState.Tied),
                Ranked: candidates.Count(item => item.State ==
                    VillageWorkforceApiEvaluationState.Ranked),
                Tied: 0,
                CurrentOnly: candidates.Count(item => item.State ==
                    VillageWorkforceApiEvaluationState.CurrentOnly),
                Ineligible: 0,
                Incomplete: 0,
                Unsupported: 0,
                Conflicting: 0,
                Visible: candidates.Count),
            candidates.Single(item => item.IsCurrent),
            candidates,
            []);

    public sealed class LanguageHostState
    {
        public TaiwuLanguage Language { get; private set; } =
            TaiwuLanguage.English;

        public Func<Task>? Changed { get; set; }

        public async Task SetLanguageAsync(TaiwuLanguage language)
        {
            Language = language;
            if (Changed is not null)
            {
                await Changed();
            }
        }
    }

    public sealed class LanguageHost : ComponentBase, IDisposable
    {
        [Inject]
        public LanguageHostState State { get; set; } = null!;

        protected override void OnInitialized() =>
            State.Changed = () => InvokeAsync(StateHasChanged);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<TaiwuLanguage>>(0);
            builder.AddAttribute(1, "Value", State.Language);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(content =>
            {
                content.OpenComponent<VillageWorkforce>(0);
                content.CloseComponent();
            }));
            builder.CloseComponent();
        }

        public void Dispose() => State.Changed = null;
    }
}
