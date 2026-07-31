using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using System.Net;
using System.Text.RegularExpressions;
using TaiWu.Application.Targets;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Components.Layout;
using TaiWuAPI.Components.Recommendations;
using TaiWuAPI.Localization;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class RecommendationComponentRenderingTests
{
    [Fact]
    public async Task Loadout_renders_capacity_direction_cost_timing_conditions_and_change()
    {
        var reason = new RecommendationReasonViewModel(
            "reason:604",
            "COUNTERS_TARGET",
            "Counters the documented target threat.",
            ["evidence:reason"],
            ["threat:magic-sound"]);
        var skill = new RecommendedSkillViewModel(
            "skill:604",
            604,
            "金貌玉魄",
            SkillCategory.Attack,
            PracticeDirection.Neutral,
            PracticeDirection.Reverse,
            RequiresManualDirectionChange: true,
            new SkillCostViewModel(
                ActualCost: 2,
                ActualCostUnavailableReason: null,
                EffectiveCost: 1,
                EffectiveCostUnavailableReason: null,
                MasteryReduction: 1,
                LegendaryBookReduction: 0,
                ["evidence:cost"]),
            new SkillCounterViewModel(
                IsAvailable: true,
                CombatCounterStrength.HardCounter,
                CombatCounterActivationTiming.ActiveAttack,
                "evidence:counter",
                UnavailableReason: null),
            ["threat:magic-sound"],
            [
                new SkillConditionViewModel(
                    "condition:604:weapon",
                    RecommendationConditionKind.Weapon,
                    CombatRequirementCriticality.Hard,
                    CombatRequirementStatus.Satisfied,
                    "Required weapon is equipped.",
                    "evidence:weapon")
            ],
            [reason]);
        var category = new LoadoutCategoryViewModel(
            "category:attack",
            SkillCategory.Attack,
            "摧破",
            UsedSlots: 1,
            UsedSlotsUnavailableReason: null,
            Capacity: 3,
            RemainingSlots: 2,
            RemainingSlotsUnavailableReason: null,
            GenericSlots: 1,
            [skill]);
        var changes = new ManualLoadoutChangeViewModel[]
        {
            new(
                "change:add:604",
                ManualLoadoutChangeKind.Add,
                SkillCategory.Attack,
                604,
                "金貌玉魄",
                RequiredDirection: null,
                reason),
            new(
                "change:direction:604",
                ManualLoadoutChangeKind.ChangeDirection,
                SkillCategory.Attack,
                604,
                "金貌玉魄",
                PracticeDirection.Reverse,
                reason)
        };

        var html = await RenderAsync<LoadoutCategory>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutCategory.Category)] = category,
                [nameof(LoadoutCategory.ManualChanges)] = changes,
                [nameof(LoadoutCategory.SelectedThreatReference)] =
                    "threat:magic-sound"
            });
        var text = VisibleText(html);

        Assert.Contains("1 /3 slots", text);
        Assert.Contains("+1 萬用", text);
        Assert.Contains("金貌玉魄", text);
        Assert.Contains("逆練 · Reverse", text);
        Assert.Contains("Actual cost 2", text);
        Assert.Contains("Effective cost 1", text);
        Assert.Contains("Active attack", text);
        Assert.Contains("Weapon Satisfied Required weapon is equipped.", text);
        Assert.Contains("Add · change direction", text);
        Assert.Contains("Evidence and linked threats", text);
        Assert.Contains("Counter-effect evidence", text);
        Assert.DoesNotContain("evidence:", text);
        Assert.DoesNotContain("#604", text);
        Assert.Contains("threat-highlight", html);
    }

    [Theory]
    [InlineData("loading", "Reading configured save", "status", false)]
    [InlineData("warning", "Recommendation ready with warnings", "status", true)]
    [InlineData("empty", "No matching target", "status", false)]
    [InlineData("ambiguous", "Multiple targets matched", "status", false)]
    [InlineData("failure", "Could not complete the read", "alert", true)]
    public async Task Page_states_render_status_and_safe_recovery(
        string scenario,
        string expectedTitle,
        string expectedRole,
        bool expectsRetry)
    {
        var state = State(scenario);

        var html = await RenderAsync<PageStateNotice>(
            new Dictionary<string, object?>
            {
                [nameof(PageStateNotice.State)] = state
            });
        var text = VisibleText(html);

        Assert.Contains(expectedTitle, text);
        Assert.Contains($"role=\"{expectedRole}\"", html);
        Assert.Equal(expectsRetry, text.Contains("Retry read"));
        Assert.DoesNotContain(
            "repair",
            text,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "modify game",
            text,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Checklist_renders_manual_information_only_boundary()
    {
        var item = new ManualChecklistItemViewModel(
            "checklist:add:604",
            ManualChecklistItemKind.AddSkill,
            "金猊鎮魔刀",
            "Add 金猊鎮魔刀 manually.",
            "Add the skill because it is part of the highest-ranked feasible loadout.",
            "reason:add",
            ["evidence:add"]);

        var html = await RenderAsync<ManualChecklist>(
            new Dictionary<string, object?>
            {
                [nameof(ManualChecklist.Items)] =
                    new ManualChecklistItemViewModel[] { item }
            });
        var text = VisibleText(html);

        Assert.Contains(
            "Instructions only: TaiWu Helper cannot perform these steps.",
            text);
        Assert.Contains("Add 金猊鎮魔刀 manually.", text);
        Assert.Contains("Why this step", text);
        Assert.Contains(
            "Add the skill because it is part of the highest-ranked feasible loadout.",
            text);
        Assert.DoesNotContain("evidence source", text);
        Assert.DoesNotContain(
            "skill 604",
            text,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reason:", text);
        Assert.DoesNotContain("evidence:", text);
        Assert.Contains("type=\"checkbox\"", html);
        Assert.DoesNotContain(">Apply<", html);
        Assert.DoesNotContain(">Execute<", html);
    }

    [Fact]
    public async Task Checklist_renders_named_actions_without_skill_ids()
    {
        var item = new ManualChecklistItemViewModel(
            "checklist:add:624",
            ManualChecklistItemKind.AddSkill,
            "伏龍刀法",
            "Add 伏龍刀法 to Attack manually.",
            "Add the skill because it is part of the highest-ranked feasible loadout.",
            "reason:add",
            ["evidence:add"]);

        var html = await RenderAsync<ManualChecklist>(
            new Dictionary<string, object?>
            {
                [nameof(ManualChecklist.Items)] =
                    new ManualChecklistItemViewModel[] { item }
            });
        var text = VisibleText(html);

        Assert.Contains("Add 伏龍刀法 to Attack manually.", text);
        Assert.DoesNotContain("624", text);
    }

    [Fact]
    public async Task Checklist_renders_breakthrough_as_required_manual_step()
    {
        var item = new ManualChecklistItemViewModel(
            "checklist:breakthrough:686",
            ManualChecklistItemKind.CompleteBreakthrough,
            "老君拂塵功",
            "Complete 老君拂塵功's breakthrough as 逆練 (Reverse) before "
            + "combat.",
            "Complete breakthrough manually as Reverse before using this "
            + "recommendation; only then is the verified effect active.",
            "reason:breakthrough",
            ["evidence:breakthrough"]);

        var html = await RenderAsync<ManualChecklist>(
            new Dictionary<string, object?>
            {
                [nameof(ManualChecklist.Items)] =
                    new ManualChecklistItemViewModel[] { item }
            });
        var text = VisibleText(html);

        Assert.Contains("Breakthrough", text);
        Assert.Contains(
            "Complete 老君拂塵功's breakthrough as 逆練 (Reverse)",
            text);
        Assert.DoesNotContain("686", text);
        Assert.DoesNotContain("evidence:", text);
    }

    [Fact]
    public async Task Battle_plan_renders_distinct_named_actions_without_ids()
    {
        var phases = new BattlePlanPhaseViewModel[]
        {
            new(
                BattlePlanPhaseKind.BeforeCombat,
                "Before combat",
                [
                    new BattlePlanItemViewModel(
                        "plan:686",
                        "老君拂塵功",
                        "Before combat, confirm 老君拂塵功 is equipped so its "
                        + "passive can activate.",
                        686,
                        "reason:passive",
                        ["threat:mind"],
                        ["evidence:passive"])
                ]),
            new(
                BattlePlanPhaseKind.Opening,
                "Opening",
                [
                    new BattlePlanItemViewModel(
                        "plan:624",
                        "伏龍刀法",
                        "At the opening, use 伏龍刀法 once its activation "
                        + "requirements are satisfied.",
                        624,
                        "reason:opening",
                        ["threat:mind"],
                        ["evidence:opening"])
                ])
        };

        var html = await RenderAsync<BattlePlan>(
            new Dictionary<string, object?>
            {
                [nameof(BattlePlan.Phases)] = phases
            });
        var text = VisibleText(html);

        Assert.Contains("老君拂塵功", text);
        Assert.Contains("伏龍刀法", text);
        Assert.Contains("Before combat", text);
        Assert.Contains("At the opening", text);
        Assert.DoesNotContain("686", text);
        Assert.DoesNotContain("624", text);
        Assert.DoesNotContain("reason:", text);
        Assert.DoesNotContain("evidence:", text);
    }

    [Fact]
    public async Task Supporting_details_render_named_evidence_not_references()
    {
        var details = new RecommendationSupportingDetailsViewModel(
            Alternatives: [],
            Assumptions: [],
            UnavailableData: [],
            ConditionalRequirements: [],
            Scores: [],
            EvidenceReferences: ["snapshot:skill:604:evidence"],
            EvidenceSummaries:
            [
                new SupportingEvidenceSummaryViewModel(
                    "金猊鎮魔刀",
                    "Recommended skill",
                    SourceCount: 1)
            ],
            UnknownValuePolicy:
                RecommendationSupportingDetailsBuilder.UnknownValuePolicy);

        var html = await RenderAsync<SupportingDetails>(
            new Dictionary<string, object?>
            {
                [nameof(SupportingDetails.Details)] = details
            });
        var text = VisibleText(html);

        Assert.Contains("金猊鎮魔刀", text);
        Assert.Contains("Recommended skill", text);
        Assert.DoesNotContain("snapshot:", text);
        Assert.DoesNotContain("604", text);
    }

    [Fact]
    public async Task Aggregated_warning_renders_one_occurrence_summary()
    {
        var warning = new RecommendationWarningViewModel(
            "warning:generation:CombinationInfeasible:1",
            "CandidateGeneration",
            "CombinationInfeasible",
            PresentationWarningKind.CandidateSearch,
            IsCritical: false,
            Occurrences: 2236,
            "Used slots cannot exceed capacity. Occurred in 2236 explored "
            + "combinations.",
            "The affected options were excluded.",
            []);

        var html = await RenderAsync<WarningBanner>(
            new Dictionary<string, object?>
            {
                [nameof(WarningBanner.Warnings)] =
                    new RecommendationWarningViewModel[] { warning }
            });
        var text = VisibleText(html);

        Assert.Contains("Candidate search", text);
        Assert.DoesNotContain("CombinationInfeasible", text);
        Assert.Contains(
            "Aggregated from 2236 evaluated combinations.",
            text);
    }

    [Fact]
    public async Task Layout_always_renders_information_only_message()
    {
        RenderFragment body = builder =>
            builder.AddContent(0, "Recommendation body");

        var html = await RenderAsync<MainLayout>(
            new Dictionary<string, object?>
            {
                [nameof(MainLayout.Body)] = body
            });
        var text = VisibleText(html);

        Assert.Contains("Information only", text);
        Assert.Contains("Recommendation body", text);
        Assert.Contains("Skip to main content", text);
        Assert.Contains("EN", text);
        Assert.Contains("中", text);
    }

    [Fact]
    public async Task Layout_renders_chinese_when_language_is_selected()
    {
        RenderFragment body = builder =>
            builder.AddContent(0, "推薦內容");

        var html = await RenderAsync<MainLayout>(
            new Dictionary<string, object?>
            {
                [nameof(MainLayout.Body)] = body
            },
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("僅供參考", text);
        Assert.Contains("戰前簡報", text);
        Assert.Contains("跳至主要內容", text);
        Assert.Contains("推薦內容", text);
        Assert.Contains("lang=\"zh-Hans\"", html);
        Assert.Contains("class=\"active\"", html);
    }

    private static RecommendationPageState State(string scenario) =>
        scenario switch
        {
            "loading" => RecommendationPageState.Loading(
                "Reading configured save"),
            "warning" => new(
                RecommendationPageStatus.SuccessWithWarning,
                "Recommendation ready with warnings",
                "Review uncertainty.",
                "Read every warning before following the manual setup.",
                CanRetryRead: true),
            "empty" => RecommendationPageState.ForTargetLookup(
                TargetLookupStatus.NotFound,
                matchCount: 0),
            "ambiguous" => RecommendationPageState.ForTargetLookup(
                TargetLookupStatus.Ambiguous,
                matchCount: 2),
            "failure" => RecommendationPageState.Failure("Read failed."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(scenario),
                scenario,
                "Unknown rendering scenario.")
        };

    private static async Task<string> RenderAsync<TComponent>(
        Dictionary<string, object?> parameters,
        TaiwuLanguage language = TaiwuLanguage.English)
        where TComponent : IComponent
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddSingleton(Substitute.For<IJSRuntime>());
        var languageState = new TaiwuLanguageState();
        languageState.Set(language);
        serviceCollection.AddSingleton(languageState);
        using var services = serviceCollection.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            services,
            services.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<TComponent>(
                ParameterView.FromDictionary(parameters));
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
