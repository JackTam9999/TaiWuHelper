using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaiWu.Application.Localization;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using TaiWuAPI.Components.Recommendations;
using TaiWuAPI.Contracts.CombatRecommendations;
using TaiWuAPI.Localization;
using TaiWuAPI.Presentation;
using Xunit;
using TacticalCombatPlanComponent =
    TaiWuAPI.Components.Recommendations.TacticalCombatPlan;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class TacticalCombatRenderingTests
{
    [Fact]
    public async Task Complete_plan_has_one_semantic_stage_list_and_manual_boundary()
    {
        var html = await RenderAsync(Model());
        var text = VisibleText(html);

        Assert.Contains("Tactical plan", text);
        Assert.Contains("霍劍嬋", text);
        Assert.Contains("Balanced", text);
        Assert.Contains("Fallback only", text);
        Assert.Contains("Preparation", text);
        Assert.Contains("Opening", text);
        Assert.Contains("Target-state response", text);
        Assert.Contains("Recovery", text);
        Assert.Contains("Finish evidence unavailable", text);
        Assert.Contains("Fallback", text);
        Assert.Contains("When / condition", text);
        Assert.Contains("Do manually", text);
        Assert.Contains("Expected verified purpose", text);
        Assert.Contains("Review step evidence", text);
        Assert.Contains("No action was sent to the game", text);
        Assert.Single(Regex.Matches(html, "<ol").Cast<Match>());
        Assert.DoesNotContain("type=\"checkbox\"", html);
        Assert.DoesNotContain("win probability", text,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("predicted damage", text,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<details", html);
        Assert.Contains("<summary", html);
    }

    [Fact]
    public async Task Chinese_plan_exposes_equivalent_status_steps_and_evidence()
    {
        var html = await RenderAsync(Model(), TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("戰術計畫", text);
        Assert.Contains("精確目標", text);
        Assert.Contains("均衡", text);
        Assert.Contains("僅有後備方案", text);
        Assert.Contains("戰前準備", text);
        Assert.Contains("開場", text);
        Assert.Contains("目標狀態應對", text);
        Assert.Contains("恢復", text);
        Assert.Contains("缺少收尾證據", text);
        Assert.Contains("後備方案", text);
        Assert.Contains("需要確認", text);
        Assert.Contains("資料衝突", text);
        Assert.Contains("查看步驟證據", text);
        Assert.Contains("未向遊戲傳送任何操作", text);
    }

    [Theory]
    [InlineData(TacticalPlanSurfaceState.Loading,
        "Calculating a complete replacement result")]
    [InlineData(TacticalPlanSurfaceState.PreviousResult, "Previous result")]
    [InlineData(TacticalPlanSurfaceState.Cancelled, "calculation was cancelled")]
    [InlineData(TacticalPlanSurfaceState.ObservationReplaced,
        "target observation replaced the prior result")]
    [InlineData(TacticalPlanSurfaceState.Failure, "could not be calculated")]
    public async Task Transitional_states_are_visible_and_never_mix_active_steps(
        TacticalPlanSurfaceState state,
        string expected)
    {
        var model = state == TacticalPlanSurfaceState.ObservationReplaced
            ? null
            : Model();
        var html = await RenderAsync(model, surfaceState: state);
        var text = VisibleText(html);

        Assert.Contains(expected, text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("role=\"status\"", html);
        Assert.Contains("No action was sent to the game", text);
        if (model is not null)
        {
            Assert.Contains("aria-disabled=\"true\"", html);
        }
    }

    [Theory]
    [InlineData(TacticalSearchTerminator.OptionLimit, 16, "Option limit")]
    [InlineData(TacticalSearchTerminator.ExplorationLimit, 80, "Exploration limit")]
    [InlineData(TacticalSearchTerminator.TimeLimit, 2000, "Time limit")]
    [InlineData(TacticalSearchTerminator.ResultLimit, 25, "Result limit")]
    [InlineData(TacticalSearchTerminator.Cancelled, 0, "Cancelled")]
    public async Task Every_search_bound_is_named_without_optimality_claim(
        TacticalSearchTerminator terminator,
        int bound,
        string expected)
    {
        var model = Model() with
        {
            Status = TacticalCombatRecommendationStatus.SearchTruncated,
            Search = Search(isComplete: false, terminator, bound)
        };
        var text = VisibleText(await RenderAsync(model));

        Assert.Contains(
            "Highest-ranked result found within the stated bounds",
            text);
        Assert.Contains(expected, text);
        Assert.DoesNotContain("optimal", text,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Search complete", text);
    }

    [Fact]
    public async Task Partial_conflicting_and_unsupported_states_use_text_and_cues()
    {
        var model = Model() with
        {
            Status = TacticalCombatRecommendationStatus.PartialEvidence,
            FinishDisposition = TacticalFinishDisposition.Unsupported,
            CriticalGaps =
            [
                Gap(TacticalConditionPresentationState.NeedsConfirmation),
                Gap(TacticalConditionPresentationState.Conflicting),
                Gap(TacticalConditionPresentationState.Unsupported),
                Gap(TacticalConditionPresentationState.Unsatisfied)
            ]
        };
        var text = VisibleText(await RenderAsync(model));

        Assert.Contains("Partial evidence", text);
        Assert.Contains("Needs confirmation", text);
        Assert.Contains("Conflicting", text);
        Assert.Contains("Unsupported", text);
        Assert.Contains("Unsatisfied", text);
        Assert.Contains("Finish evidence unavailable", text);
    }

    [Theory]
    [InlineData(TacticalCombatRecommendationStatus.Success, "Plan available")]
    [InlineData(TacticalCombatRecommendationStatus.PartialEvidence,
        "Partial evidence")]
    [InlineData(TacticalCombatRecommendationStatus.UnsupportedChain,
        "Tactical rules unsupported")]
    [InlineData(TacticalCombatRecommendationStatus.NoCandidate,
        "No tactical candidate")]
    [InlineData(TacticalCombatRecommendationStatus.SearchTruncated,
        "Search bounded")]
    [InlineData(TacticalCombatRecommendationStatus.SourceFailure,
        "Tactical source unavailable")]
    [InlineData(TacticalCombatRecommendationStatus.ContextFailure,
        "Tactical calculation unavailable")]
    public async Task Result_status_matrix_has_distinct_visible_copy(
        TacticalCombatRecommendationStatus status,
        string expected)
    {
        var model = Model() with { Status = status };
        var text = VisibleText(await RenderAsync(model));

        Assert.Contains(expected, text);
    }

    [Fact]
    public async Task Candidate_details_are_grouped_bounded_and_use_names()
    {
        var candidates = Enumerable.Range(1, 30)
            .Select(index => new TacticalCandidateViewModel(
                $"Named option {index}",
                SkillCategory.Attack,
                PracticeDirection.Reverse,
                RequiresBreakthrough: false,
                new BilingualText(
                    "A hard feasibility requirement failed.",
                    "未通過硬性可行性需求。")))
            .ToArray();
        var model = Model() with
        {
            CandidateGroups =
            [
                new TacticalCandidateGroupViewModel(
                    TacticalCandidatePresentationGroup.Rejected,
                    candidates)
            ]
        };
        var html = await RenderAsync(model);
        var text = VisibleText(html);

        Assert.Contains("Showing 25 of 30", text);
        Assert.Contains("Named option 25", text);
        Assert.DoesNotContain("Named option 26", text);
        Assert.Contains("Show more", text);
        Assert.DoesNotContain("skill:", text,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unavailable_score_is_excluded_instead_of_rendered_as_zero()
    {
        var text = VisibleText(await RenderAsync(Model()));

        Assert.Contains("Finish path", text);
        Assert.Contains("Not included in this result", text);
        Assert.Contains("excluded rather than treated as zero", text);
        Assert.DoesNotContain("0%", text);
    }

    [Fact]
    public void Every_fixed_ui_term_has_distinct_chinese_copy()
    {
        foreach (var key in Enum.GetValues<TacticalCombatUiTextKey>())
        {
            Assert.NotEqual(
                TacticalCombatUiText.Get(TaiwuLanguage.English, key),
                TacticalCombatUiText.Get(TaiwuLanguage.Chinese, key));
        }
    }

    [Fact]
    public void Every_typed_ui_value_has_complete_bilingual_copy()
    {
        AssertBilingual<TacticalPlanStage>(TacticalCombatUiText.Stage);
        AssertBilingual<TacticalCombatRecommendationStatus>(
            TacticalCombatUiText.Status);
        AssertBilingual<RecommendationPolicy>(TacticalCombatUiText.Policy);
        AssertBilingual<TacticalFinishDisposition>(
            (language, value) => TacticalCombatUiText.Finish(language, value));
        AssertBilingual<TacticalConditionPresentationState>(
            TacticalCombatUiText.Condition);
        AssertBilingual<TacticalCandidatePresentationGroup>(
            TacticalCombatUiText.CandidateGroup);
        AssertBilingual<TacticalScoreComponentKind>(TacticalCombatUiText.Score);
        AssertBilingual<TacticalSearchTerminator>(
            TacticalCombatUiText.SearchTerminator);
        AssertBilingual<PracticeDirection>(TacticalCombatUiText.Direction);
        AssertBilingual<TacticalEvidenceSourceKind>(TacticalCombatUiText.Source);
    }

    [Fact]
    public void Mapper_preserves_partial_conflict_without_exposing_stable_codes()
    {
        var response = EmptyResponse() with
        {
            Status = TacticalCombatRecommendationStatus.PartialEvidence,
            TargetChain = new TacticalTargetChainResponse(
                "historical-version",
                new string('A', 64),
                TacticalRuleSetResolutionStatus.Resolved,
                [new TacticalTransitionRuleResponse(
                    "RAW_TRANSITION_CODE",
                    TacticalRulePurpose.CastSuppression,
                    TacticalTransitionTiming.DuringCast,
                    TacticalRuleApplicability.Conflicting,
                    [],
                    [],
                    [],
                    ["RAW_EVIDENCE_CODE"],
                    "RAW_LIMITATION_CODE",
                    [])],
                []),
            ExecutionContext = new TacticalExecutionContextResponse(
                new string('B', 64),
                TacticalRuleSetResolutionStatus.Resolved,
                [],
                [new TacticalContextFactResponse(
                    "RAW_CONTEXT_CODE",
                    TacticalContextFactState.Conflicting,
                    TacticalContextOrigin.SaveSnapshot,
                    TacticalContextAvailability.FixedForRequest,
                    "RAW_REASON_CODE",
                    [],
                    Value: null)],
                [])
        };

        var model = TacticalCombatViewModelMapper.Map(
            response,
            "霍劍嬋",
            RecommendationPolicy.Balanced,
            new Dictionary<int, string>());

        Assert.Equal(TacticalCombatRecommendationStatus.PartialEvidence,
            model.Status);
        Assert.Contains(model.CriticalGaps, item =>
            item.State == TacticalConditionPresentationState.Conflicting);
        Assert.DoesNotContain(model.CriticalGaps, item =>
            item.Description.English.Contains("RAW_", StringComparison.Ordinal));
    }

    [Fact]
    public void Mapper_groups_every_candidate_decision_and_uses_skill_names()
    {
        var candidates = Enum.GetValues<TacticalCandidateDecision>()
            .Select((decision, index) => Candidate(
                600 + index,
                decision))
            .Append(Candidate(700, TacticalCandidateDecision.Admitted))
            .ToArray();
        var selectedIdentity = candidates[0].Identity;
        var response = EmptyResponse() with
        {
            CandidateDiscovery = new TacticalCandidateDiscoveryResponse(
                new string('C', 64),
                LearnedSkillCount: candidates.Length,
                SupportedRoleCount: candidates.Length,
                ConsideredVerifiedRoleCount: candidates.Length,
                AdmittedVerifiedRoleCount: 1,
                UnsupportedCount: 1,
                candidates,
                [],
                []),
            Search = new TacticalSearchResponse(
                new string('D', 64),
                IsComplete: true,
                IsOptimal: true,
                SearchCoverage(),
                candidates.Select(item =>
                    new TacticalSearchCandidateResponse(
                        item.Identity,
                        item.Decision,
                        [],
                        [],
                        "RAW_REASON",
                        [],
                        DominatedBy: null)).ToArray(),
                [],
                []),
            SelectedLoadout = new TacticalSelectedLoadoutResponse(
                new string('E', 64),
                "selected-loadout",
                TotalScore: 1m,
                [selectedIdentity],
                [],
                new GenericSlotPlanResponse(0, 0, 0, 0, 0))
        };
        var names = candidates.ToDictionary(
            item => item.SkillId,
            item => $"Named skill {item.SkillId}");

        var model = TacticalCombatViewModelMapper.Map(
            response,
            "霍劍嬋",
            RecommendationPolicy.Balanced,
            names);

        Assert.Equal(Enum.GetValues<TacticalCandidatePresentationGroup>(),
            model.CandidateGroups.Select(item => item.Group));
        Assert.All(
            model.CandidateGroups.SelectMany(item => item.Candidates),
            item => Assert.StartsWith("Named skill", item.Name));
        Assert.DoesNotContain(
            model.CandidateGroups.SelectMany(item => item.Candidates),
            item => item.Reason.English.Contains("RAW_", StringComparison.Ordinal));
    }

    private static TacticalCombatViewModel Model() => new(
        TacticalCombatRecommendationStatus.Success,
        "霍劍嬋",
        RecommendationPolicy.Balanced,
        DateTimeOffset.Parse("2026-08-20T12:00:00Z"),
        DateTimeOffset.Parse("2026-08-20T11:59:00Z"),
        "0.0.0.0-HISTORICAL",
        TacticalFinishDisposition.FallbackOnly,
        Enum.GetValues<TacticalPlanStage>().Select(Stage).ToArray(),
        [Gap(TacticalConditionPresentationState.Conflicting)],
        Search(isComplete: true, TacticalSearchTerminator.None, 0),
        Scores(),
        CandidateGroups(),
        [Evidence()],
        "ABCDEF123456");

    private static TacticalCombatResponse EmptyResponse() => new(
        TacticalCombatRecommendationStatus.Success,
        "RAW_TOP_REASON",
        HasTacticalPlan: false,
        new TacticalRecommendationIdentityResponse(
            new string('A', 64),
            new string('B', 64),
            new string('C', 64),
            new string('D', 64),
            CandidateFingerprint: null,
            new string('E', 64),
            new string('F', 64),
            SelectedLoadoutFingerprint: null,
            PlanFingerprint: null,
            new string('1', 64)),
        new TacticalSnapshotSummaryResponse(
            DateTimeOffset.Parse("2026-08-20T12:00:00Z"),
            LatestObservationAtUtc: null,
            new string('A', 64),
            new string('B', 64),
            "historical-version"),
        TargetChain: null,
        ExecutionContext: null,
        CandidateDiscovery: null,
        Search: null,
        Scoring: null,
        SelectedLoadout: null,
        Plan: null,
        new TacticalDiagnosticsResponse(
            new TacticalRecommendationWorkCountsResponse(
                1, 1, 1, 1, 1, 1, 1, 1, 0),
            SearchElapsedMilliseconds: null,
            CapturedAtUtc: null,
            LatestObservationAtUtc: null));

    private static TacticalCandidateResponse Candidate(
        int skillId,
        TacticalCandidateDecision decision) => new(
            $"{skillId}:REVERSE",
            skillId,
            SkillCategory.Attack,
            PracticeDirection.Reverse,
            RequiresBreakthrough: false,
            IsCurrentlyEquipped: false,
            decision == TacticalCandidateDecision.Unsupported
                ? TacticalCandidateSupportState.UnsupportedEffect
                : TacticalCandidateSupportState.VerifiedRole,
            decision switch
            {
                TacticalCandidateDecision.Admitted =>
                    TacticalCandidateAdmissionState.Admitted,
                TacticalCandidateDecision.Rejected =>
                    TacticalCandidateAdmissionState.Infeasible,
                TacticalCandidateDecision.Unsupported =>
                    TacticalCandidateAdmissionState.Unsupported,
                _ => TacticalCandidateAdmissionState.RetainedOnly
            },
            decision,
            IntegerFact(),
            IntegerFact(),
            Role: null,
            Gates: []);

    private static TacticalIntegerFactResponse IntegerFact() => new(
        TacticalContextFactState.Available,
        Value: 1,
        "VALUE_AVAILABLE",
        []);

    private static TacticalSearchCoverageResponse SearchCoverage() => new(
        new TacticalSearchBoundsResponse(16, 100, 2000, 25),
        CandidateUniverseCount: 5,
        RoleSupportedCount: 4,
        AdmittedCount: 1,
        RejectedCount: 1,
        UnsupportedCount: 1,
        IrrelevantCount: 1,
        DominatedCount: 1,
        SearchedOptionCount: 1,
        ExploredCombinationCount: 1,
        FeasibleResultCount: 1,
        RetainedResultCount: 1,
        TacticalSearchTerminator.None,
        ElapsedMilliseconds: 1,
        new string('A', 64),
        []);

    private static TacticalStageViewModel Stage(TacticalPlanStage stage)
    {
        if (stage == TacticalPlanStage.Finish)
        {
            return new TacticalStageViewModel(
                stage,
                TacticalPlanStageState.Unsupported,
                new BilingualText(
                    "Finish evidence unavailable; no finish action is inferred.",
                    "缺少收尾證據；不推測任何收尾操作。"),
                []);
        }

        var conditionState = stage == TacticalPlanStage.Fallback
            ? TacticalConditionPresentationState.Fallback
            : stage == TacticalPlanStage.TargetStateResponse
                ? TacticalConditionPresentationState.NeedsConfirmation
                : TacticalConditionPresentationState.Confirmed;
        return new TacticalStageViewModel(
            stage,
            TacticalPlanStageState.Supported,
            new BilingualText("Use only as listed.", "只依列示內容使用。"),
            [Step(stage, conditionState)]);
    }

    private static TacticalStepViewModel Step(
        TacticalPlanStage stage,
        TacticalConditionPresentationState state) => new(
            Order: 1,
            stage == TacticalPlanStage.Fallback
                ? TacticalStepBranchKind.Fallback
                : TacticalStepBranchKind.Conditional,
            state,
            new BilingualText(
                "Confirm the listed condition before acting.",
                "操作前請確認列出的條件。"),
            new BilingualText(
                "Use 金貌玉魄 manually.",
                "請手動使用金貌玉魄。"),
            new BilingualText(
                "Use the verified suppression purpose.",
                "使用已驗證的壓制用途。"),
            new BilingualText(
                "No result is guaranteed.",
                "不保證任何結果。"),
            [new TacticalRequirementViewModel(
                state == TacticalConditionPresentationState.NeedsConfirmation
                    ? TacticalRequirementOutcome.Unknown
                    : TacticalRequirementOutcome.Satisfied,
                new BilingualText(
                    "Confirm this typed prerequisite.",
                    "請確認此型別化前置需求。"))],
            [Evidence()]);

    private static TacticalGapViewModel Gap(
        TacticalConditionPresentationState state) => new(
            state,
            new BilingualText(
                "A target condition needs review.",
                "需要檢查一項目標條件。"),
            new BilingualText(
                "Dependent steps remain conditional.",
                "依賴此條件的步驟維持條件式。"));

    private static TacticalSearchSummaryViewModel Search(
        bool isComplete,
        TacticalSearchTerminator terminator,
        int bound) => new(
            isComplete,
            Considered: 84,
            Admitted: 7,
            Rejected: 4,
            Unsupported: 2,
            Irrelevant: 1,
            Dominated: 2,
            Explored: 31,
            Feasible: 9,
            Retained: 9,
            terminator,
            bound);

    private static TacticalScoreComponentViewModel[] Scores() =>
    [
        new(
            TacticalScoreComponentKind.CausalValue,
            TacticalScoreComponentState.Available,
            BaseWeight: 40,
            AppliedWeight: 50m,
            NormalizedValue: 0.75m,
            Contribution: 37.5m,
            new BilingualText(
                "Supported chain contribution.",
                "受支援的因果鏈貢獻。"),
            new BilingualText(
                "Applies only to this result.",
                "只適用於此結果。")),
        new(
            TacticalScoreComponentKind.FinishPath,
            TacticalScoreComponentState.Unavailable,
            BaseWeight: 20,
            AppliedWeight: null,
            NormalizedValue: null,
            Contribution: null,
            new BilingualText(
                "Separately supported finish route.",
                "另行支援的收尾路徑。"),
            new BilingualText(
                "Evidence is unavailable, so this component is excluded rather than treated as zero.",
                "證據無法取得，因此排除此項，而非視為零。"))
    ];

    private static TacticalCandidateGroupViewModel[] CandidateGroups() =>
    [
        new(
            TacticalCandidatePresentationGroup.Selected,
            [new TacticalCandidateViewModel(
                "金貌玉魄",
                SkillCategory.Attack,
                PracticeDirection.Reverse,
                RequiresBreakthrough: false,
                new BilingualText("Selected option.", "所選方案。"))]),
        new(
            TacticalCandidatePresentationGroup.Unsupported,
            [new TacticalCandidateViewModel(
                "未支援功法",
                SkillCategory.Defense,
                PracticeDirection.Direct,
                RequiresBreakthrough: false,
                new BilingualText("Unsupported role.", "不支援的角色。"))])
    ];

    private static TacticalEvidenceSummaryViewModel Evidence() => new(
        TacticalEvidenceSourceKind.VerifiedRule,
        "0.0.0.0-HISTORICAL",
        "TACTICAL_COMBAT_RULES@1.0.0",
        new BilingualText("Exact-target scope", "精確目標範圍"));

    private static async Task<string> RenderAsync(
        TacticalCombatViewModel? model,
        TaiwuLanguage language = TaiwuLanguage.English,
        TacticalPlanSurfaceState surfaceState =
            TacticalPlanSurfaceState.Ready)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        using var services = serviceCollection.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            services,
            services.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<
                TacticalCombatPlanComponent>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(TacticalCombatPlanComponent.Model)] = model,
                    [nameof(TacticalCombatPlanComponent.Language)] = language,
                    [nameof(TacticalCombatPlanComponent.SurfaceState)] =
                        surfaceState
                }));
            return output.ToHtmlString();
        });
    }

    private static string VisibleText(string html)
    {
        var withoutTags = Tags().Replace(html, " ");
        return Whitespace().Replace(WebUtility.HtmlDecode(withoutTags), " ")
            .Trim();
    }

    private static void AssertBilingual<T>(
        Func<TaiwuLanguage, T, string> getText)
        where T : struct, Enum
    {
        foreach (var value in Enum.GetValues<T>())
        {
            var english = getText(TaiwuLanguage.English, value);
            var chinese = getText(TaiwuLanguage.Chinese, value);
            Assert.False(string.IsNullOrWhiteSpace(english));
            Assert.False(string.IsNullOrWhiteSpace(chinese));
            Assert.NotEqual(english, chinese);
        }
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
