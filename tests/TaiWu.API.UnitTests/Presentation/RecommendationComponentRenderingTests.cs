using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using System.Net;
using System.Text.RegularExpressions;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Targets;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWuAPI.Components.Layout;
using TaiWuAPI.Components.Recommendations;
using TaiWuAPI.Localization;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class RecommendationComponentRenderingTests
{
    [Fact]
    public async Task Target_observation_starts_disabled_with_bilingual_guidance()
    {
        var state = new TargetObservationEditorState();
        var target = new TargetLookupEntry(
            16317,
            "霍劍嬋",
            age: 52,
            areaId: 1,
            blockId: 2);

        var english = await RenderAsync<TargetObservationForm>(
            new Dictionary<string, object?>
            {
                [nameof(TargetObservationForm.State)] = state,
                [nameof(TargetObservationForm.Target)] = target
            });
        var chinese = await RenderAsync<TargetObservationForm>(
            new Dictionary<string, object?>
            {
                [nameof(TargetObservationForm.State)] =
                    new TargetObservationEditorState(),
                [nameof(TargetObservationForm.Target)] = target,
                [nameof(TargetObservationForm.Language)] =
                    TaiwuLanguage.Chinese
            },
            TaiwuLanguage.Chinese);

        Assert.Contains("Report a visible sparring loadout", VisibleText(english));
        Assert.Contains("disabled", english);
        Assert.Contains("Get a save-only recommendation first", VisibleText(english));
        Assert.DoesNotContain("id=\"target-skill-query\"", english);
        Assert.Contains("回報可見的切磋運功配置", VisibleText(chinese));
        Assert.Contains("敵對及劇情人物不會顯示此畫面", VisibleText(chinese));
    }

    [Theory]
    [InlineData(TargetObservationContext.Hostile)]
    [InlineData(TargetObservationContext.Story)]
    public async Task Hidden_encounter_renders_unavailable_without_skill_input(
        TargetObservationContext context)
    {
        var state = new TargetObservationEditorState();
        state.SetEnabled(enabled: true, hasInitialRecommendation: true);
        state.SetContext(context);

        var html = await RenderAsync<TargetObservationForm>(
            TargetObservationParameters(state));
        var text = VisibleText(html);

        Assert.Contains("Opponent loadout unavailable", text);
        Assert.Contains("No hidden loadout input will be requested", text);
        Assert.Contains("role=\"status\"", html);
        Assert.DoesNotContain("id=\"target-skill-query\"", html);
        Assert.DoesNotContain("Use observation for recommendation", text);
    }

    [Fact]
    public async Task Sparring_editor_has_semantic_keyboard_controls_and_status_text()
    {
        var state = new TargetObservationEditorState();
        state.SetEnabled(enabled: true, hasInitialRecommendation: true);
        state.SetContext(TargetObservationContext.Sparring);

        var html = await RenderAsync<TargetObservationForm>(
            TargetObservationParameters(state));
        var text = VisibleText(html);

        Assert.Contains("霍劍嬋", text);
        Assert.Contains("Save timestamp available", text);
        Assert.Contains("Partial loadout", text);
        Assert.Contains("Complete current loadout", text);
        Assert.Contains("Category is verified from the catalogue", text);
        Assert.Contains("Editing a session-only target observation", text);
        Assert.Contains("<fieldset", html);
        Assert.Contains("type=\"radio\"", html);
        Assert.Contains("id=\"target-skill-query\"", html);
        Assert.Contains("aria-describedby=", html);
        Assert.Contains("role=\"status\"", html);
        Assert.DoesNotContain(">Apply<", html);
        Assert.DoesNotContain("screenshot", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Target_observation_impact_separates_changes_unknowns_and_conflicts()
    {
        var html = await RenderAsync<TargetObservationImpactPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetObservationImpactPanel.Impact)] =
                    TargetImpact()
            });
        var text = VisibleText(html);

        Assert.Contains("Save-only compared with observed", text);
        Assert.Contains("Unlisted target skills remain possible", text);
        Assert.Contains("Added threat", text);
        Assert.Contains("Confirmed threat", text);
        Assert.Contains("Demoted to learned-unconfirmed", text);
        Assert.Contains("Removed typed threat", text);
        Assert.Contains("Unchanged threat", text);
        Assert.Contains("Feasibility changes", text);
        Assert.Contains("Reverse Qilun Life Support", text);
        Assert.Contains("Evidence chain", text);
        Assert.Contains("Repeatable defeat-mark reset", text);
        Assert.Contains("Scoring changes", text);
        Assert.Contains("Still unsupported", text);
        Assert.Contains("No severity or score was assigned", text);
        Assert.Contains("Source conflicts and precedence", text);
        Assert.Contains("Save snapshot", text);
        Assert.Contains("Current screen observation", text);
        Assert.Contains("not a win probability", text);
        Assert.Contains("role=\"status\"", html);
        Assert.Contains("role=\"note\"", html);
        Assert.DoesNotContain("UNRECOGNIZED_TARGET_EFFECT", text);
        Assert.DoesNotContain("ui:target-observation", text);
        Assert.DoesNotContain("SAVE_SCREEN_CONFLICT", text);
    }

    [Fact]
    public async Task Target_observation_impact_renders_critical_chinese_status()
    {
        var html = await RenderAsync<TargetObservationImpactPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetObservationImpactPanel.Impact)] =
                    TargetImpact(),
                [nameof(TargetObservationImpactPanel.Language)] =
                    TaiwuLanguage.Chinese
            },
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("只用存檔與觀察後結果比較", text);
        Assert.Contains("部分觀察", text);
        Assert.Contains("威脅變更", text);
        Assert.Contains("新增威脅", text);
        Assert.Contains("已確認威脅", text);
        Assert.Contains("降為已學但未確認裝備", text);
        Assert.Contains("移除已定型威脅", text);
        Assert.Contains("未變威脅", text);
        Assert.Contains("可行性變更", text);
        Assert.Contains("證據鏈", text);
        Assert.Contains("仍未支援", text);
        Assert.Contains("來源衝突與優先順序", text);
        Assert.Contains("並非勝率", text);
    }

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

        Assert.Contains("Recommended capacity 1 /3 slots", text);
        Assert.Contains("Recommended 萬用 allocation: 1", text);
        Assert.Contains("金貌玉魄", text);
        Assert.Contains("逆練 · Reverse", text);
        Assert.Contains("Actual cost 2", text);
        Assert.Contains("Effective cost 1", text);
        Assert.Contains("Active attack", text);
        Assert.Contains("Weapon Satisfied Required weapon is equipped.", text);
        Assert.Contains("Add · change direction", text);
        Assert.Contains("Evidence and linked threats", text);
        Assert.Contains("Counter-effect evidence", text);
        Assert.Contains("View catalogue detail", text);
        Assert.Contains(
            "href=\"/skills/604?context=recommendation\"",
            html);
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
        Assert.Contains("lang=\"zh-Hant\"", html);
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
        serviceCollection.AddSingleton(
            Substitute.For<IResolveTargetSkillSelection>());
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

    private static TargetObservationImpactViewModel TargetImpact() => new(
        [
            new TargetThreatImpactViewModel(
                "DEFEAT_MARK_RESET_LOOP",
                "Repeatable defeat-mark reset",
                TargetThreatImpactKind.Added,
                TargetThreatSeverity.Critical,
                [TargetThreatSourceKind.ObservedEquipped],
                ["docs:verified-rule", "ui:target-observation"]),
            new TargetThreatImpactViewModel(
                "MIND_RESONANCE_CASCADE",
                "Mind-resonance cascade",
                TargetThreatImpactKind.Demoted,
                TargetThreatSeverity.Critical,
                [TargetThreatSourceKind.LearnedUnconfirmed],
                ["docs:verified-rule"]),
            new TargetThreatImpactViewModel(
                "MAGIC_SOUND_MIND_DAMAGE",
                "Positive-practice magic-sound mind damage",
                TargetThreatImpactKind.Confirmed,
                TargetThreatSeverity.High,
                [TargetThreatSourceKind.ObservedEquipped],
                ["docs:verified-rule"]),
            new TargetThreatImpactViewModel(
                "DIRECT_PRESSURE",
                "Direct pressure",
                TargetThreatImpactKind.Removed,
                TargetThreatSeverity.Moderate,
                [TargetThreatSourceKind.SaveEquipped],
                ["save:test"]),
            new TargetThreatImpactViewModel(
                "KNOWN_BASELINE",
                "Known baseline",
                TargetThreatImpactKind.Unchanged,
                TargetThreatSeverity.Informational,
                [TargetThreatSourceKind.LearnedUnconfirmed],
                ["docs:verified-rule"])
        ],
        [
            new TargetRecommendationImpactViewModel(
                RecommendationPolicy.Safe,
                TargetRecommendationImpactKind.Added,
                TargetRecommendationChangeCause.Feasibility,
                291,
                "Reverse Qilun Life Support",
                SkillCategory.Assistance,
                PracticeDirection.Reverse,
                ["DEFEAT_MARK_RESET_LOOP"],
                ["Repeatable defeat-mark reset"],
                ["effect:915"])
        ],
        [
            new TargetRecommendationImpactViewModel(
                RecommendationPolicy.Aggressive,
                TargetRecommendationImpactKind.Removed,
                TargetRecommendationChangeCause.Scoring,
                604,
                "Reverse Jinni Suppression",
                SkillCategory.Attack,
                PracticeDirection.Reverse,
                ["MIND_RESONANCE_CASCADE"],
                ["Mind-resonance cascade"],
                ["effect:1064"])
        ],
        [
            new TargetUnsupportedEvidenceViewModel(
                "UNRECOGNIZED_TARGET_EFFECT",
                WasPresentBefore: false,
                "ui:target-observation",
                SkillId: 719,
                SkillName: "Target Art")
        ],
        PartialCoverageLeavesUnknown: true,
        [
            new TargetObservationConflictViewModel(
                "target.equippedSkills",
                "SAVE_SCREEN_CONFLICT",
                "NEWER_CURRENT_SCREEN_FIELD_PRECEDENCE",
                [
                    new TargetObservationConflictSourceViewModel(
                        SnapshotDataSource.Save,
                        DateTimeOffset.Parse("2026-08-07T20:00:00Z"),
                        "save:test"),
                    new TargetObservationConflictSourceViewModel(
                        SnapshotDataSource.CurrentScreenObservation,
                        DateTimeOffset.Parse("2026-08-07T20:01:00Z"),
                        "ui:target-observation")
                ])
        ],
        "Evidence confidence describes provenance, not a win probability.");

    private static Dictionary<string, object?> TargetObservationParameters(
        TargetObservationEditorState state) => new()
        {
            [nameof(TargetObservationForm.State)] = state,
            [nameof(TargetObservationForm.Target)] = new TargetLookupEntry(
                16317,
                "霍劍嬋",
                age: 52,
                areaId: 1,
                blockId: 2),
            [nameof(TargetObservationForm.Recommendation)] =
                new CombatRecommendationViewModel(
                    "snapshot:test",
                    DateTimeOffset.Parse("2026-08-07T21:00:00Z"),
                    DateTimeOffset.Parse("2026-08-07T20:00:00Z"),
                    "1.0.0-test",
                    RecommendationPolicy.Balanced,
                    "style:balanced",
                    "Information only",
                    Threats: [],
                    Styles: [],
                    Warnings: [])
        };

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
