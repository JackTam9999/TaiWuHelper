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
using TaiWu.Application.Localization;
using TaiWu.Application.Targets;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.LoadoutComparisons;
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

        Assert.Contains("Report visible target combat information", VisibleText(english));
        Assert.Contains("disabled", english);
        Assert.Contains("Get a save-only recommendation first", VisibleText(english));
        Assert.DoesNotContain("id=\"target-skill-query\"", english);
        Assert.Contains("回報畫面可見的目標戰鬥資訊", VisibleText(chinese));
        Assert.Contains("敵對及劇情情境只能回報戰鬥介面實際顯示的部分功法效果", VisibleText(chinese));
    }

    [Theory]
    [InlineData(TargetObservationContext.Hostile)]
    [InlineData(TargetObservationContext.Story)]
    public async Task Battle_encounter_renders_editable_partial_effect_input(
        TargetObservationContext context)
    {
        var state = new TargetObservationEditorState();
        state.SetEnabled(enabled: true, hasInitialRecommendation: true);
        state.SetContext(context);

        var html = await RenderAsync<TargetObservationForm>(
            TargetObservationParameters(state));
        var text = VisibleText(html);
        var chineseParameters = TargetObservationParameters(state);
        chineseParameters[nameof(TargetObservationForm.Language)] =
            TaiwuLanguage.Chinese;
        var chinese = await RenderAsync<TargetObservationForm>(
            chineseParameters,
            TaiwuLanguage.Chinese);
        var chineseText = VisibleText(chinese);

        Assert.Contains("Full opponent loadout unavailable", text);
        Assert.Contains("Report only names, direction, and power actually visible", text);
        Assert.Contains("Partial battle-visible effects", text);
        Assert.Contains("omitted skills remain unknown", text);
        Assert.Contains("role=\"status\"", html);
        Assert.Contains("id=\"target-skill-query\"", html);
        Assert.DoesNotContain("Use observation for recommendation", text);
        Assert.DoesNotContain("Complete current loadout", text);
        Assert.Contains("無法查看完整對手運功", chineseText);
        Assert.Contains("部分戰鬥可見效果", chineseText);
        Assert.Contains("未列出的功法仍屬未知", chineseText);
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
        Assert.Contains("Battle-visible active effect", text);
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
        Assert.Contains("戰鬥可見的生效效果", text);
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

    [Fact]
    public async Task Comparison_matrix_renders_two_policy_facts_and_hides_balanced()
    {
        var html = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] =
                    Comparison()
            });
        var text = VisibleText(html);

        Assert.Contains("Loadout comparison", text);
        Assert.Contains("Current loadout", text);
        Assert.Contains("Safe", text);
        Assert.DoesNotContain("Balanced", text);
        Assert.Contains("Aggressive", text);
        Assert.Contains("Current baseline provenance", text);
        Assert.Contains("Equipped skills Save", text);
        Assert.Contains("內功", text);
        Assert.Contains("摧破", text);
        Assert.Contains("輕靈", text);
        Assert.Contains("護體", text);
        Assert.Contains("奇竅", text);
        Assert.Contains("Unchanged Guard", text);
        Assert.Contains("Changed Blade", text);
        Assert.Contains("Unknown Cost Art", text);
        Assert.Contains("Retained", text);
        Assert.Contains("Added", text);
        Assert.Contains("Removed", text);
        Assert.Contains("⇄ Change to Reverse", text);
        Assert.Contains("Effective cost: Unavailable", text);
        Assert.Contains("3 / 4", text);
        Assert.Contains("Remaining: 1", text);
        Assert.Contains("萬用: 1", text);
        Assert.DoesNotContain("No feasible proposal", text);
        Assert.Contains("Direction change required", text);
        Assert.Contains(
            "TaiWu Helper cannot equip, redirect, or break through skills.",
            text);
        Assert.Contains("href=\"#manual-checklist-heading\"", html);
        Assert.Contains("data-column=\"Current\"", html);
        Assert.DoesNotContain("0 / 0", text);
        Assert.Equal(
            5,
            Regex.Matches(html, "comparison-category-row").Count);
    }

    [Fact]
    public async Task Comparison_difference_filter_keeps_changed_manual_actions()
    {
        var filter = new LoadoutComparisonFilterState();
        filter.ShowDifferences();

        var html = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] =
                    Comparison(),
                [nameof(LoadoutComparisonMatrix.FilterState)] = filter
            });
        var text = VisibleText(html);

        Assert.Contains("Changed Blade", text);
        Assert.Contains("Change to Reverse", text);
        Assert.DoesNotContain("Unchanged Guard", text);
        Assert.DoesNotContain("Unknown Cost Art", text);
        Assert.Contains("1 skill row(s) shown", text);
        Assert.Contains("aria-pressed", html);
        Assert.Contains("class=\"active\"", html);
    }

    [Fact]
    public async Task Comparison_matrix_exposes_bilingual_accessible_narrow_state()
    {
        const string longName =
            "超長測試功法名稱會完整換行而且不會隱藏必要的手動操作與無法取得原因";
        var comparison = Comparison();
        comparison = comparison with
        {
            Categories =
            [
                .. comparison.Categories.Select(category =>
                    category.Category == SkillCategory.Attack
                        ? category with
                        {
                            Skills =
                            [
                                .. category.Skills.Select(skill =>
                                    skill.SkillId == 103
                                        ? skill with { Name = longName }
                                        : skill)
                            ]
                        }
                        : category)
            ]
        };

        var html = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] = comparison,
                [nameof(LoadoutComparisonMatrix.SelectedPolicy)] =
                    RecommendationPolicy.Safe,
                [nameof(LoadoutComparisonMatrix.Language)] =
                    TaiwuLanguage.Chinese
            },
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("運功配置比較", text);
        Assert.DoesNotContain("比較方案", text);
        Assert.Contains("所有列", text);
        Assert.Contains("僅顯示差異", text);
        Assert.Contains("比較類別", text);
        Assert.Contains("跳至比較類別", text);
        Assert.Contains("各策略獨立事實", text);
        Assert.Contains("威脅覆蓋、需求與風險", text);
        Assert.Contains("已覆蓋威脅", text);
        Assert.Contains("未解決風險", text);
        Assert.Contains("策略內排名： 穩健", text);
        Assert.Contains(
            "分數僅用於各策略內排列候選方案，並非獲勝機率。",
            text);
        Assert.Contains("目前運功", text);
        Assert.Contains("保留", text);
        Assert.Contains("移除", text);
        Assert.Contains("無法取得", text);
        Assert.Contains(longName, text);
        Assert.Contains(
            "太吾助手不會裝備、改變正逆練或進行突破。"
            + "請在遊戲中手動按照指示操作。",
            text);
        Assert.Contains("data-selected-policy=\"Safe\"", html);
        Assert.Contains("comparison-selected-safe", html);
        Assert.Contains("comparison-selected-policy", html);
        Assert.Contains("comparison-unselected-policy", html);
        Assert.Contains("scope=\"rowgroup\"", html);
        Assert.Contains("tabindex=\"-1\"", html);
        Assert.Contains("aria-live=\"polite\"", html);
        Assert.Contains("aria-label=\"Changed Blade", html);
        Assert.Contains("comparison-current-column", html);
        Assert.Contains("data-column=\"Aggressive\"", html);
        Assert.DoesNotContain(">Attack<", html);
        Assert.DoesNotContain("Effective cost is unavailable.", text);
        Assert.DoesNotContain(
            "Unrecognized target mechanic remains unsupported.",
            text);
        Assert.DoesNotContain(
            "Weapon state must be confirmed manually.",
            text);
        Assert.DoesNotContain(
            "Critical timing remains a manual decision.",
            text);
        Assert.DoesNotContain(
            "No verified damage evidence is available.",
            text);
        Assert.DoesNotContain(
            "No feasible candidate satisfies known slot constraints.",
            text);
        Assert.DoesNotContain(
            "Reverse practice is required for this counter.",
            text);
    }

    [Fact]
    public async Task Comparison_policy_switch_preserves_difference_filter_state()
    {
        var filter = new LoadoutComparisonFilterState();
        filter.ShowDifferences();

        var safe = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] = Comparison(),
                [nameof(LoadoutComparisonMatrix.SelectedPolicy)] =
                    RecommendationPolicy.Safe,
                [nameof(LoadoutComparisonMatrix.FilterState)] = filter
            });
        var aggressive = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] =
                    Comparison(aggressiveInfeasible: true),
                [nameof(LoadoutComparisonMatrix.SelectedPolicy)] =
                    RecommendationPolicy.Aggressive,
                [nameof(LoadoutComparisonMatrix.FilterState)] = filter
            });

        Assert.True(filter.DifferencesOnly);
        Assert.Contains("comparison-differences-only", safe);
        Assert.Contains("comparison-selected-safe", safe);
        Assert.Contains("Safe: Differences only: 1", VisibleText(safe));
        Assert.Contains("comparison-differences-only", aggressive);
        Assert.Contains("comparison-selected-aggressive", aggressive);
        Assert.Contains(
            "Aggressive: Differences only: 0",
            VisibleText(aggressive));
        Assert.Contains(
            "No feasible candidate satisfies known slot constraints.",
            VisibleText(aggressive));
    }

    [Fact]
    public async Task Comparison_tactics_keep_risks_evidence_and_policy_scores_visible()
    {
        var filter = new LoadoutComparisonFilterState();
        filter.ShowDifferences();

        var html = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] = Comparison(),
                [nameof(LoadoutComparisonMatrix.SelectedPolicy)] =
                    RecommendationPolicy.Safe,
                [nameof(LoadoutComparisonMatrix.FilterState)] = filter
            });
        var text = VisibleText(html);

        Assert.Contains("Threat coverage, requirements, and risks", text);
        Assert.Contains("Unsupported or excluded mechanics", text);
        Assert.Contains(
            "Unrecognized target mechanic remains unsupported.",
            text);
        Assert.Contains("Covered threats", text);
        Assert.Contains("MAGIC_SOUND", text);
        Assert.Contains("Magic-sound mind damage", text);
        Assert.Contains("Unresolved risks", text);
        Assert.Contains("DEFEAT_LOOP", text);
        Assert.Contains("Repeatable defeat-mark reset", text);
        Assert.Contains("Critical · Unresolved", text);
        Assert.Contains("Requirements and caveats", text);
        Assert.Contains("Weapon state must be confirmed manually.", text);
        Assert.Contains("Critical timing remains a manual decision.", text);
        Assert.Contains("Ranking within Safe", text);
        Assert.Contains("Threat coverage 40 0.75", text);
        Assert.Contains("Severity-weighted verified threats covered.", text);
        Assert.Contains("Damage potential 10", text);
        Assert.Contains("No verified damage evidence is available.", text);
        Assert.Contains(
            "Scores rank candidates only inside each policy; "
            + "they are not win odds.",
            text);
        Assert.Contains("comparison-unresolved", html);
        Assert.Contains("class=\"critical\"", html);
        Assert.Contains("type=\"button\"", html);
        Assert.DoesNotContain("evidence:", text);
        Assert.DoesNotContain("Best policy", text);
        Assert.DoesNotContain("win probability", text);
    }

    [Fact]
    public async Task Comparison_tactics_render_identical_and_distinct_coverage()
    {
        var identical = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] = Comparison()
            });
        var comparison = Comparison();
        comparison = comparison with
        {
            Columns =
            [
                .. comparison.Columns.Select(column =>
                    column.Policy == RecommendationPolicy.Aggressive
                        ? column with
                        {
                            Tactical = column.Tactical! with
                            {
                                CoveredThreats =
                                [
                                    new LoadoutComparisonThreatViewModel(
                                        "threat:direct-pressure",
                                        "DIRECT_PRESSURE",
                                        "Verified direct pressure",
                                        TargetThreatSeverity.Moderate,
                                        ["evidence:direct-pressure"])
                                ],
                                UnresolvedThreats = []
                            }
                        }
                        : column)
            ]
        };
        var distinct = await RenderAsync<LoadoutComparisonMatrix>(
            new Dictionary<string, object?>
            {
                [nameof(LoadoutComparisonMatrix.Comparison)] = comparison,
                [nameof(LoadoutComparisonMatrix.SelectedPolicy)] =
                    RecommendationPolicy.Aggressive
            });

        Assert.Single(
            Regex.Matches(VisibleText(identical), "MAGIC_SOUND")
                .Cast<Match>());
        Assert.DoesNotContain("MAGIC_SOUND", VisibleText(distinct));
        Assert.Contains("DIRECT_PRESSURE", VisibleText(distinct));
        Assert.Contains("Verified direct pressure", VisibleText(distinct));
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
    public async Task Empty_battle_plan_renders_one_compact_message()
    {
        var phases = Enum.GetValues<BattlePlanPhaseKind>()
            .Select(kind => new BattlePlanPhaseViewModel(
                kind,
                kind.ToString(),
                []))
            .ToArray();

        var html = await RenderAsync<BattlePlan>(
            new Dictionary<string, object?>
            {
                [nameof(BattlePlan.Phases)] = phases
            });
        var text = VisibleText(html);

        Assert.Contains(
            "No separate evidence-backed battle instruction is available.",
            text);
        Assert.DoesNotContain("BeforeCombat", text);
        Assert.Single(
            Regex.Matches(html, "battle-plan-empty").Cast<Match>());
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
    public async Task Matching_warnings_render_as_one_group()
    {
        var warnings = new[]
        {
            new RecommendationWarningViewModel(
                "warning:capacity:safe",
                "Capacity",
                "CAPACITY_UNAVAILABLE",
                PresentationWarningKind.General,
                IsCritical: false,
                Occurrences: 1,
                "Current capacity is unavailable.",
                "Review the displayed slot limit manually.",
                ["evidence:safe"]),
            new RecommendationWarningViewModel(
                "warning:capacity:aggressive",
                "Capacity",
                "CAPACITY_UNAVAILABLE",
                PresentationWarningKind.General,
                IsCritical: false,
                Occurrences: 1,
                "Effective capacity is unavailable.",
                "Review the displayed slot limit manually.",
                ["evidence:aggressive"])
        };

        var html = await RenderAsync<WarningBanner>(
            new Dictionary<string, object?>
            {
                [nameof(WarningBanner.Warnings)] = warnings
            });
        var text = VisibleText(html);

        Assert.Single(
            Regex.Matches(html, "warning-item")
                .Cast<Match>());
        Assert.Contains("Effective capacity is unavailable.", text);
        Assert.Contains("Current capacity is unavailable.", text);
        Assert.Contains("Combined 2 related warnings.", text);
        Assert.Contains("General · 2 evidence source(s)", text);
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
    public async Task Target_results_distinguish_same_name_story_characters()
    {
        TargetLookupEntry[] matches =
        [
            new(
                101,
                "筠兒",
                age: 24,
                areaId: -1,
                blockId: -1,
                kind: TargetLookupKind.StoryCharacter,
                templateId: 700,
                consummateLevel: 16),
            new(
                202,
                "筠兒",
                age: 24,
                areaId: -1,
                blockId: -1,
                kind: TargetLookupKind.StoryCharacter,
                templateId: 701,
                consummateLevel: 18),
            new(
                303,
                "筠兒",
                age: 24,
                areaId: -1,
                blockId: -1,
                kind: TargetLookupKind.StoryCharacter,
                templateId: 702)
        ];
        var parameters = new Dictionary<string, object?>
        {
            [nameof(TargetSearchResults.Matches)] = matches,
            [nameof(TargetSearchResults.SelectedCharacterId)] = 202
        };

        var english = await RenderAsync<TargetSearchResults>(parameters);
        var chineseParameters = new Dictionary<string, object?>(parameters)
        {
            [nameof(TargetSearchResults.Language)] = TaiwuLanguage.Chinese
        };
        var chinese = await RenderAsync<TargetSearchResults>(
            chineseParameters,
            TaiwuLanguage.Chinese);

        Assert.Contains("Consummate level 16", VisibleText(english));
        Assert.Contains("Selected target", VisibleText(english));
        Assert.Contains("Choose this target", VisibleText(english));
        Assert.Contains("aria-pressed=\"true\"", english);
        Assert.Contains("精純 16", VisibleText(chinese));
        Assert.Contains("精純 18", VisibleText(chinese));
        Assert.Contains("精純 無法取得", VisibleText(chinese));
        Assert.Contains("已選目標", VisibleText(chinese));
        Assert.Contains("選擇此目標", VisibleText(chinese));
        Assert.DoesNotContain("#101", VisibleText(english));
        Assert.DoesNotContain("#202", VisibleText(chinese));
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
        serviceCollection.AddSingleton<NavigationManager>(
            new TestNavigationManager());
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

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
        }
    }

    private static LoadoutComparisonViewModel Comparison(
        bool aggressiveInfeasible = false)
    {
        var current = ComparisonColumn(
            LoadoutComparisonColumnKind.Current,
            policy: null,
            manualActions: 0);
        var safe = ComparisonColumn(
            LoadoutComparisonColumnKind.Safe,
            RecommendationPolicy.Safe,
            manualActions: 1,
            tactical: Tactical(RecommendationPolicy.Safe));
        var balanced = ComparisonColumn(
            LoadoutComparisonColumnKind.Balanced,
            RecommendationPolicy.Balanced,
            manualActions: 2,
            allocationChanged: true,
            tactical: Tactical(RecommendationPolicy.Balanced));
        var aggressive = aggressiveInfeasible
            ? new LoadoutComparisonColumnViewModel(
                LoadoutComparisonColumnKind.Aggressive,
                LoadoutComparisonColumnStatus.Infeasible,
                RecommendationPolicy.Aggressive,
                "style:snapshot:test:aggressive",
                GenericSlots: null,
                GenericSlotsChanged: false,
                ManualActionCount: null,
                ManualActionCountUnavailableReason:
                    "No feasible candidate satisfies known slot constraints.",
                Diagnostic:
                    "No feasible candidate satisfies known slot constraints.")
            : ComparisonColumn(
                LoadoutComparisonColumnKind.Aggressive,
                RecommendationPolicy.Aggressive,
                manualActions: 2,
                allocationChanged: true,
                tactical: Tactical(RecommendationPolicy.Aggressive));
        var columns = new[] { current, safe, balanced, aggressive };
        var comparisonSkills = ComparisonSkills();
        if (aggressiveInfeasible)
        {
            comparisonSkills =
            [
                .. comparisonSkills.Select(skill => skill with
                {
                    Cells =
                    [
                        .. skill.Cells.Where(cell => cell.Column
                            != LoadoutComparisonColumnKind.Aggressive)
                    ]
                })
            ];
        }

        var categories = Enum.GetValues<SkillCategory>()
            .Select(category => new LoadoutComparisonCategoryViewModel(
                category,
                CategoryName(category),
                [..
                    new[]
                    {
                    ComparisonCapacity(
                        LoadoutComparisonColumnKind.Current,
                        category),
                    ComparisonCapacity(
                        LoadoutComparisonColumnKind.Safe,
                        category),
                    ComparisonCapacity(
                        LoadoutComparisonColumnKind.Balanced,
                        category),
                    ComparisonCapacity(
                        LoadoutComparisonColumnKind.Aggressive,
                        category)
                    }.Where(capacity => !aggressiveInfeasible
                        || capacity.Column
                            != LoadoutComparisonColumnKind.Aggressive)],
                category == SkillCategory.Attack
                    ? comparisonSkills
                    : []))
            .ToArray();

        return new LoadoutComparisonViewModel(
            "comparison:test",
            "snapshot:test",
            columns,
            categories,
            [
                new LoadoutComparisonProvenanceViewModel(
                    LoadoutComparisonBaselineField.EquippedSkills,
                    SnapshotDataSource.Save,
                    DateTimeOffset.Parse("2026-08-08T12:00:00Z")),
                new LoadoutComparisonProvenanceViewModel(
                    LoadoutComparisonBaselineField.GenericSlotAllocation,
                    SnapshotDataSource.CurrentScreenObservation,
                    DateTimeOffset.Parse("2026-08-08T12:01:00Z")),
                new LoadoutComparisonProvenanceViewModel(
                    LoadoutComparisonBaselineField.SlotBudgets,
                    SnapshotDataSource.GameConfiguration,
                    DateTimeOffset.Parse("2026-08-08T12:00:00Z")),
                new LoadoutComparisonProvenanceViewModel(
                    LoadoutComparisonBaselineField
                        .LegendaryBookCostAssignments,
                    SnapshotDataSource.VerifiedRule,
                    DateTimeOffset.Parse("2026-08-08T12:00:00Z"))
            ],
            "TaiWu Helper cannot equip, redirect, or break through skills. "
            + "Follow these instructions manually in the game.",
            [
                new LoadoutComparisonUnsupportedViewModel(
                    IsCritical: true,
                    "Unrecognized target mechanic remains unsupported.",
                    "The affected mechanic was excluded from verified "
                    + "scoring, so threat coverage may be incomplete.",
                    ["evidence:unsupported"])
            ]);
    }

    private static LoadoutComparisonColumnViewModel ComparisonColumn(
        LoadoutComparisonColumnKind kind,
        RecommendationPolicy? policy,
        int manualActions,
        bool allocationChanged = false,
        LoadoutComparisonTacticalViewModel? tactical = null) => new(
            kind,
            LoadoutComparisonColumnStatus.Available,
            policy,
            policy.HasValue
                ? $"style:snapshot:test:{policy.Value.ToString().ToLowerInvariant()}"
                : null,
            new LoadoutComparisonGenericSlotsViewModel(
                Total: 4,
                Attack: allocationChanged ? 2 : 1,
                Agility: 1,
                Defense: allocationChanged ? 0 : 1,
                Assistance: 1),
            allocationChanged,
            manualActions,
            ManualActionCountUnavailableReason: null,
            Diagnostic: null,
            tactical);

    private static LoadoutComparisonTacticalViewModel Tactical(
        RecommendationPolicy policy) => new(
            policy,
            new LoadoutComparisonRoleViewModel(
                "Synthetic Defense",
                UnavailableReason: null),
            new LoadoutComparisonRoleViewModel(
                SkillName: null,
                "No active agility is selected."),
            [
                new LoadoutComparisonThreatViewModel(
                    "threat:magic-sound",
                    "MAGIC_SOUND",
                    "Magic-sound mind damage",
                    TargetThreatSeverity.High,
                    ["evidence:magic-sound"])
            ],
            [
                new LoadoutComparisonThreatViewModel(
                    "threat:defeat-loop",
                    "DEFEAT_LOOP",
                    "Repeatable defeat-mark reset",
                    TargetThreatSeverity.Critical,
                    ["evidence:defeat-loop"])
            ],
            [
                new LoadoutComparisonConditionSummaryViewModel(
                    "Changed Blade",
                    RecommendationConditionKind.Weapon,
                    CombatRequirementCriticality.Conditional,
                    CombatRequirementStatus.Unknown,
                    "Weapon state must be confirmed manually.",
                    "evidence:weapon")
            ],
            [
                new LoadoutComparisonCaveatSummaryViewModel(
                    RecommendationCaveatKind.KnownRisk,
                    "Critical timing remains a manual decision.",
                    "Changed Blade",
                    ["evidence:timing"])
            ],
            [
                new LoadoutComparisonScoreSummaryViewModel(
                    RecommendationScoreComponentKind.ThreatCoverage,
                    Weight: 40,
                    Score: 0.75m,
                    ScoreUnavailableReason: null,
                    "Severity-weighted verified threats covered.",
                    "evidence:score:coverage"),
                new LoadoutComparisonScoreSummaryViewModel(
                    RecommendationScoreComponentKind.DamagePotential,
                    Weight: 10,
                    Score: null,
                    "No verified damage evidence is available.",
                    "No verified damage evidence is available.",
                    "evidence:score:damage")
            ],
            [
                "evidence:magic-sound",
                "evidence:defeat-loop",
                "evidence:weapon",
                "evidence:timing",
                "evidence:score:coverage",
                "evidence:score:damage"
            ]);

    private static LoadoutComparisonCapacityCellViewModel ComparisonCapacity(
        LoadoutComparisonColumnKind column,
        SkillCategory category)
    {
        var used = category == SkillCategory.Attack ? 3 : 0;
        return new LoadoutComparisonCapacityCellViewModel(
            column,
            used,
            UsedUnavailableReason: null,
            Capacity: 4,
            CapacityUnavailableReason: null,
            Remaining: 4 - used,
            RemainingUnavailableReason: null,
            CategoryContribution: Math.Max(0, used - 1),
            CategoryContributionUnavailableReason: null,
            GenericContribution: category == SkillCategory.Attack ? 1 : 0,
            GenericContributionUnavailableReason: null);
    }

    private static LoadoutComparisonSkillRowViewModel[] ComparisonSkills() =>
    [
        new(
            SkillCategory.Attack,
            101,
            "Unchanged Guard",
            NameUnavailableReason: null,
            [
                ComparisonCell(
                    LoadoutComparisonColumnKind.Current,
                    LoadoutComparisonMembership.Present,
                    cost: 1),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Safe,
                    LoadoutComparisonMembership.Retained,
                    cost: 1),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Balanced,
                    LoadoutComparisonMembership.Retained,
                    cost: 1),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Aggressive,
                    LoadoutComparisonMembership.Retained,
                    cost: 1)
            ]),
        new(
            SkillCategory.Attack,
            102,
            "Changed Blade",
            NameUnavailableReason: null,
            [
                ComparisonCell(
                    LoadoutComparisonColumnKind.Current,
                    LoadoutComparisonMembership.Present,
                    cost: 1),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Safe,
                    LoadoutComparisonMembership.Removed,
                    cost: 1),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Balanced,
                    LoadoutComparisonMembership.Added,
                    cost: 1,
                    [
                        new LoadoutComparisonSkillActionViewModel(
                            LoadoutComparisonSkillActionKind
                                .DirectionChangeRequired,
                            PracticeDirection.Reverse,
                            "Reverse practice is required for this counter.")
                    ]),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Aggressive,
                    LoadoutComparisonMembership.Added,
                    cost: 1,
                    [
                        new LoadoutComparisonSkillActionViewModel(
                            LoadoutComparisonSkillActionKind
                                .DirectionChangeRequired,
                            PracticeDirection.Reverse,
                            "Reverse practice is required for this counter.")
                    ])
            ]),
        new(
            SkillCategory.Attack,
            103,
            "Unknown Cost Art",
            NameUnavailableReason: null,
            [
                ComparisonCell(
                    LoadoutComparisonColumnKind.Current,
                    LoadoutComparisonMembership.Present,
                    cost: null),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Safe,
                    LoadoutComparisonMembership.Retained,
                    cost: null),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Balanced,
                    LoadoutComparisonMembership.Retained,
                    cost: null),
                ComparisonCell(
                    LoadoutComparisonColumnKind.Aggressive,
                    LoadoutComparisonMembership.Retained,
                    cost: null)
            ])
    ];

    private static LoadoutComparisonSkillCellViewModel ComparisonCell(
        LoadoutComparisonColumnKind column,
        LoadoutComparisonMembership membership,
        int? cost,
        IReadOnlyList<LoadoutComparisonSkillActionViewModel>? actions = null) =>
        new(
            column,
            membership,
            MembershipUnavailableReason: null,
            PracticeDirection.Direct,
            CurrentDirectionUnavailableReason: null,
            cost,
            cost.HasValue ? null : "Effective cost is unavailable.",
            actions ?? []);

    private static string CategoryName(SkillCategory category) =>
        category switch
        {
            SkillCategory.Neigong => "內功",
            SkillCategory.Attack => "摧破",
            SkillCategory.Agility => "輕靈",
            SkillCategory.Defense => "護體",
            SkillCategory.Assistance => "奇竅",
            _ => category.ToString()
        };

    private static TargetObservationImpactViewModel TargetImpact() => new(
        [
            new TargetThreatImpactViewModel(
                "DEFEAT_MARK_RESET_LOOP",
                "Repeatable defeat-mark reset",
                TargetThreatImpactKind.Added,
                TargetThreatSeverity.Critical,
                [TargetThreatSourceKind.ObservedActiveEffect],
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
