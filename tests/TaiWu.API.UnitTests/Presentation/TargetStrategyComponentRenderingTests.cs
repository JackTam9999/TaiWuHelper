using System.Text.RegularExpressions;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.Localization;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetProfiles;
using TaiWuAPI.Components.Recommendations;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class RecommendationComponentRenderingTests
{
    [Fact]
    public async Task Target_strategy_groups_multi_match_and_shared_details()
    {
        var html = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = Strategy()
            });
        var text = VisibleText(html);

        Assert.Contains("Reusable response strategy", text);
        Assert.Contains("2 verified target patterns combine", text);
        Assert.Contains("Attack-family context", text);
        Assert.Contains("Verified combat mechanics", text);
        Assert.True(
            text.IndexOf("Mind resonance and defeat-reset chain",
                StringComparison.Ordinal)
            < text.IndexOf("Configured poison application",
                StringComparison.Ordinal));
        Assert.Contains("Reusable response goals", text);
        Assert.Contains("Control distraction marks", text);
        Assert.Contains("Magic-sound mind damage", text);
        Assert.Contains("Gold Lion Demon-Suppressing Blade", text);
        Assert.Contains("Not accessible", text);
        Assert.Contains("Exact verified counter is not currently accessible", text);
        Assert.Contains("href=\"#target-threats-heading\"", html);
        Assert.Contains("href=\"#target-counter-1\"", html);
        Assert.Contains("href=\"/skills/604?context=recommendation\"", html);
        Assert.Contains("<details", html);
        Assert.Contains("role=\"status\"", html);
        Assert.Contains("role=\"note\"", html);
        Assert.Equal(
            2,
            Regex.Matches(html, "target-counter-card state-").Count);
        Assert.DoesNotContain("Loadout comparison", text);
        Assert.DoesNotContain("Recommended capacity", text);
        Assert.DoesNotContain("Show detailed skill cards", text);
        Assert.DoesNotContain("MIND_RESONANCE_BASELINE", text);
        Assert.DoesNotContain("REVERSE_JINNI_SUPPRESSION", text);
    }

    [Fact]
    public async Task Target_strategy_renders_complete_traditional_chinese_copy()
    {
        var html = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = Strategy(
                    TaiwuLanguage.Chinese),
                [nameof(TargetStrategyPanel.Language)] =
                    TaiwuLanguage.Chinese
            },
            TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("可重用的應對策略", text);
        Assert.Contains("2 個已驗證目標類型合併", text);
        Assert.Contains("攻擊類型背景", text);
        Assert.Contains("已驗證的戰鬥機制", text);
        Assert.Contains("已檢查的目標類型", text);
        Assert.Contains("可重用的應對目標", text);
        Assert.Contains("關聯威脅", text);
        Assert.Contains("已驗證應對功法", text);
        Assert.Contains("目前無法取得", text);
        Assert.Contains("裝備為被動功法", text);
        Assert.Contains("精確目標調整", text);
        Assert.Contains("角色可行性", text);
        Assert.Contains("保留", text);
        Assert.Contains("提高", text);
        Assert.Contains("降低", text);
        Assert.Contains("加入", text);
        Assert.Contains("取代", text);
        Assert.Contains("未解決", text);
        Assert.Contains("精確相反證據", text);
        Assert.Contains("這仍是未解決缺口", text);
        Assert.Contains("本區只說明可重用的目標策略", text);
        Assert.DoesNotContain("Reusable response strategy", text);
        Assert.DoesNotContain("Availability unresolved", text);
    }

    [Fact]
    public async Task Target_adjustments_separate_exact_evidence_from_feasibility()
    {
        var html = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = Strategy()
            });
        var text = VisibleText(html);

        Assert.Contains("Exact-target customization", text);
        Assert.Contains("Player feasibility", text);
        Assert.True(
            text.IndexOf("Exact-target customization", StringComparison.Ordinal)
            < text.IndexOf("Player feasibility", StringComparison.Ordinal));
        foreach (var action in Enum.GetValues<TargetPlaybookAdjustmentAction>())
        {
            Assert.Contains(
                $"target-adjustment state-{action.ToString().ToLowerInvariant()}",
                html);
        }

        Assert.Contains("href=\"#target-goal-CONTROL_DISTRACTION_MARKS\"", html);
        Assert.Contains("href=\"#target-threats-heading\"", html);
        Assert.Contains("href=\"#target-counter-1\"", html);
        Assert.Contains("Contrary exact evidence", text);
        Assert.Contains(
            "Reduced priority does not remove exact evidence or source conflicts.",
            text);
        Assert.Contains(
            "This remains a gap; it is not completed mitigation.",
            text);
        Assert.Contains(
            "The current player has not learned this skill.",
            text);
        Assert.DoesNotContain("Show detailed skill cards", text);
        Assert.DoesNotContain("Manual configuration", text);
        Assert.DoesNotContain("Loadout comparison", text);
    }

    [Fact]
    public async Task Target_adjustment_state_replaces_and_clears_atomically()
    {
        var saveOnly = Strategy();
        var observed = saveOnly with
        {
            Adjustments =
            [
                saveOnly.Adjustments[0] with
                {
                    Action = TargetPlaybookAdjustmentAction.Elevated,
                    ActionLabel = "Elevated by current observation",
                    Summary = "Observed exact target response"
                }
            ]
        };

        var before = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = saveOnly
            });
        var applied = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = observed
            });
        var cleared = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = saveOnly
            });

        Assert.DoesNotContain("Observed exact target response", before);
        Assert.Contains("Observed exact target response", applied);
        Assert.Equal(before, cleared);
    }

    [Fact]
    public async Task Target_feasibility_identifies_an_unchanged_current_loadout()
    {
        var strategy = Strategy() with
        {
            Feasibility = new TargetStrategyFeasibilityViewModel(
                "The final recommendation is unchanged because the current "
                    + "loadout already satisfies the composed response.",
                CurrentLoadoutAlreadySatisfies: true,
                FeasibleCounterCount: 2,
                UnavailableCounterCount: 0)
        };

        var html = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = strategy
            });
        var text = VisibleText(html);

        Assert.Contains("final recommendation is unchanged", text);
        Assert.Contains("Current loadout already satisfies this strategy.", text);
        Assert.Contains("role=\"status\"", html);
    }

    [Theory]
    [InlineData(TargetStrategyStatus.Partial, "Partial profile")]
    [InlineData(TargetStrategyStatus.Unsupported, "Unsupported version")]
    [InlineData(TargetStrategyStatus.Conflicting, "Evidence conflict")]
    [InlineData(TargetStrategyStatus.NoMatch, "No verified match")]
    public async Task Target_strategy_renders_non_playbook_states(
        TargetStrategyStatus status,
        string expectedLabel)
    {
        var strategy = Strategy() with
        {
            Status = status,
            StatusLabel = expectedLabel,
            Summary = "No confirmed playbook can be claimed.",
            MatchedArchetypeCount = 0,
            Goals = [],
            Counters = [],
            StandaloneGaps = [],
            Adjustments = [],
            Feasibility = new TargetStrategyFeasibilityViewModel(
                "No verified counter reached player feasibility filtering.",
                CurrentLoadoutAlreadySatisfies: false,
                FeasibleCounterCount: 0,
                UnavailableCounterCount: 0)
        };

        var html = await RenderAsync<TargetStrategyPanel>(
            new Dictionary<string, object?>
            {
                [nameof(TargetStrategyPanel.Strategy)] = strategy
            });
        var text = VisibleText(html);

        Assert.Contains($"data-status=\"{status}\"", html);
        Assert.Contains(expectedLabel, text);
        Assert.Contains(
            "No verified playbook goal is available for this profile state.",
            text);
        Assert.DoesNotContain("Verified counter options", text);
    }

    private static TargetStrategyViewModel Strategy(
        TaiwuLanguage language = TaiwuLanguage.English)
    {
        var chinese = language == TaiwuLanguage.Chinese;
        var counters = new[]
        {
            new TargetCounterSummaryViewModel(
                "REVERSE_JINNI_SUPPRESSION",
                "target-counter-1",
                604,
                chinese ? "金猊鎮魔刀" : "Gold Lion Demon-Suppressing Blade",
                "/skills/604?context=recommendation",
                chinese ? "逆練" : "Reverse practice",
                TargetPlaybookCounterAvailabilityState.Feasible,
                chinese ? "目前可用" : "Available now",
                chinese
                    ? "已通過角色取得條件及運功可行性檢查。"
                    : "Passed player access and loadout feasibility checks.",
                [],
                Gap: null),
            new TargetCounterSummaryViewModel(
                "REVERSE_LAOJUN_MARK_CLEAR",
                "target-counter-2",
                686,
                chinese ? "老君拂塵功" : "Laojun's Whisk Style",
                "/skills/686?context=recommendation",
                chinese ? "逆練" : "Reverse practice",
                TargetPlaybookCounterAvailabilityState.Inaccessible,
                chinese ? "目前無法取得" : "Not accessible",
                chinese
                    ? "目前角色尚未習得此功法。"
                    : "The current player has not learned this skill.",
                [chinese ? "裝備為被動功法" : "Equip as a passive"],
                new TargetStrategyGapViewModel(
                    "PLAYER_COUNTER_INACCESSIBLE",
                    chinese
                        ? "目前無法取得此精確驗證功法。"
                        : "Exact verified counter is not currently accessible."))
        };
        var jinni = new TargetStrategyCounterLinkViewModel(
            counters[0].Anchor,
            counters[0].SkillName);
        var laojun = new TargetStrategyCounterLinkViewModel(
            counters[1].Anchor,
            counters[1].SkillName);

        return new TargetStrategyViewModel(
            TargetStrategyStatus.Available,
            chinese ? "已有可用策略" : "Playbook available",
            chinese
                ? "2 個已驗證目標類型合併為一套可重用的應對策略。"
                : "2 verified target patterns combine into one reusable "
                    + "response strategy.",
            DateTimeOffset.Parse("2026-08-10T12:00:00Z"),
            "E5.PROFILE.1",
            EvidenceSourceCount: 5,
            MatchedArchetypeCount: 2,
            [
                new TargetProfileGroupViewModel(
                    TargetProfileGroupKind.Context,
                    chinese ? "攻擊類型背景" : "Attack-family context",
                    [Facet(
                        TargetProfileDimension.AttackFamily,
                        chinese ? "刀法背景" : "Blade-family context",
                        chinese)]),
                new TargetProfileGroupViewModel(
                    TargetProfileGroupKind.Mechanics,
                    chinese
                        ? "已驗證的戰鬥機制"
                        : "Verified combat mechanics",
                    [Facet(
                        TargetProfileDimension.Control,
                        chinese ? "失神標記控制" : "Distraction-mark control",
                        chinese)])
            ],
            [
                Archetype(
                    "MIND_RESONANCE_BASELINE",
                    chinese
                        ? "心神共鳴與敗北標記重置連鎖"
                        : "Mind resonance and defeat-reset chain",
                    TargetArchetypeMatchState.Matched,
                    chinese),
                Archetype(
                    "OUTER_DAMAGE_CONFIGURED",
                    chinese ? "已配置外傷壓力" : "Configured outer-damage pressure",
                    TargetArchetypeMatchState.Matched,
                    chinese),
                Archetype(
                    "POISON_APPLICATION_CONFIGURED",
                    chinese ? "已配置毒素施加" : "Configured poison application",
                    TargetArchetypeMatchState.Partial,
                    chinese)
            ],
            [
                new TargetResponseGoalViewModel(
                    "CONTROL_DISTRACTION_MARKS",
                    chinese ? "控制失神標記" : "Control distraction marks",
                    chinese ? "關鍵應對" : "Critical response",
                    chinese ? "戰鬥開始時生效" : "Ready at combat start",
                    IsEligible: true,
                    [new TargetStrategyThreatLinkViewModel(
                        "threat:magic-sound",
                        chinese ? "魔音心神傷害" : "Magic-sound mind damage")],
                    [jinni, laojun],
                    []),
                new TargetResponseGoalViewModel(
                    "BREAK_MIND_RESONANCE_CASCADE",
                    chinese ? "阻斷心神共鳴連鎖" : "Break the resonance cascade",
                    chinese ? "關鍵應對" : "Critical response",
                    chinese ? "戰鬥開始時生效" : "Ready at combat start",
                    IsEligible: true,
                    [],
                    [jinni],
                    [])
            ],
            counters,
            [],
            Adjustments(chinese, counters[0]),
            new TargetStrategyFeasibilityViewModel(
                chinese
                    ? "2 項已驗證應對功法中，有 1 項通過目前角色的可行性檢查。"
                    : "1 of 2 verified counter options pass the current "
                        + "player's feasibility checks.",
                CurrentLoadoutAlreadySatisfies: false,
                FeasibleCounterCount: 1,
                UnavailableCounterCount: 1));
    }

    private static TargetAdjustmentExplanationViewModel[] Adjustments(
        bool chinese,
        TargetCounterSummaryViewModel counter)
    {
        var goal = new TargetAdjustmentReferenceViewModel(
            chinese ? "控制失神標記" : "Control distraction marks",
            "#target-goal-CONTROL_DISTRACTION_MARKS");
        var threat = new TargetAdjustmentReferenceViewModel(
            chinese ? "魔音心神傷害" : "Magic-sound mind damage",
            "#target-threats-heading",
            "threat:magic-sound");
        var counterReference = new TargetAdjustmentReferenceViewModel(
            counter.SkillName,
            $"#{counter.Anchor}");
        var confirmed = Evidence(
            TargetPlaybookAdjustmentEvidenceState.Confirmed,
            chinese,
            chinese ? "失神標記控制" : "Distraction-mark control",
            "#profile-facet:3:DISTRACTION_MARK_CONTROL");
        var contrary = Evidence(
            TargetPlaybookAdjustmentEvidenceState.Contrary,
            chinese,
            chinese ? "相反的目標特徵" : "Contrary target fact",
            "#profile-facet:1:OUTER_DAMAGE_CONFIGURED");
        var incomplete = Evidence(
            TargetPlaybookAdjustmentEvidenceState.Incomplete,
            chinese,
            chinese ? "缺少的精確證據" : "Missing exact evidence",
            Href: null);

        return
        [
            Adjustment(
                TargetPlaybookAdjustmentAction.Retained,
                chinese ? "保留" : "Retained",
                chinese ? "保留此應對。" : "Keep this response.",
                goal,
                Result: null,
                confirmed),
            Adjustment(
                TargetPlaybookAdjustmentAction.Elevated,
                chinese ? "提高" : "Elevated",
                chinese ? "提高此應對的優先度。" : "Raise this response.",
                goal,
                Result: null,
                confirmed),
            Adjustment(
                TargetPlaybookAdjustmentAction.Reduced,
                chinese ? "降低" : "Reduced",
                chinese ? "降低此廣泛應對的優先度。" : "Reduce this broad response.",
                goal,
                Result: null,
                contrary),
            Adjustment(
                TargetPlaybookAdjustmentAction.Added,
                chinese ? "加入" : "Added",
                chinese ? "加入精確目標威脅。" : "Add an exact-target threat.",
                Original: null,
                threat,
                confirmed),
            Adjustment(
                TargetPlaybookAdjustmentAction.Replaced,
                chinese ? "取代" : "Replaced",
                chinese ? "以可行功法取代原有應對。" : "Replace with a feasible counter.",
                goal,
                counterReference,
                confirmed),
            Adjustment(
                TargetPlaybookAdjustmentAction.Unresolved,
                chinese ? "未解決" : "Unresolved",
                chinese ? "此緩解仍未完成。" : "This mitigation remains unresolved.",
                goal,
                Result: null,
                incomplete)
        ];
    }

    private static TargetAdjustmentExplanationViewModel Adjustment(
        TargetPlaybookAdjustmentAction action,
        string label,
        string summary,
        TargetAdjustmentReferenceViewModel? Original,
        TargetAdjustmentReferenceViewModel? Result,
        TargetAdjustmentEvidenceViewModel evidence) => new(
        action,
        label,
        summary,
        summary,
        Original,
        Result,
        [evidence]);

    private static TargetAdjustmentEvidenceViewModel Evidence(
        TargetPlaybookAdjustmentEvidenceState state,
        bool chinese,
        string title,
        string? Href) => new(
        TargetPlaybookAdjustmentEvidenceKind.ProfileFacet,
        state,
        state switch
        {
            TargetPlaybookAdjustmentEvidenceState.Confirmed =>
                chinese ? "已確認的精確證據" : "Confirmed exact evidence",
            TargetPlaybookAdjustmentEvidenceState.Contrary =>
                chinese ? "精確相反證據" : "Contrary exact evidence",
            TargetPlaybookAdjustmentEvidenceState.Incomplete =>
                chinese ? "缺少或不完整的證據" : "Missing or incomplete evidence",
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        },
        title,
        Href,
        ThreatReference: null,
        SourceCount: 1);

    private static TargetProfileFacetSummaryViewModel Facet(
        TargetProfileDimension dimension,
        string title,
        bool chinese) => new(
        $"facet:{dimension}",
        dimension,
        chinese ? "類別" : "Dimension",
        title,
        TargetProfileEvidenceState.Confirmed,
        chinese ? "已驗證" : "Verified",
        ValueSummary: null,
        EvidenceSourceCount: 1,
        chinese ? "目前安裝資料" : "Installed rules");

    private static TargetArchetypeSummaryViewModel Archetype(
        string code,
        string title,
        TargetArchetypeMatchState state,
        bool chinese) => new(
        code,
        title,
        state,
        state == TargetArchetypeMatchState.Matched
            ? chinese ? "已匹配" : "Matched"
            : chinese ? "部分匹配" : "Partial",
        chinese ? "類型規則 1.0.0" : "Archetype 1.0.0",
        EvidenceSourceCount: 2,
        chinese ? "2 項證據來源" : "2 evidence sources",
        [chinese ? "支持特徵" : "Supporting fact"],
        state == TargetArchetypeMatchState.Partial
            ? [chinese ? "缺少特徵" : "Missing fact"]
            : [],
        ExcludingFacts: [],
        ConflictingFacts: []);
}
