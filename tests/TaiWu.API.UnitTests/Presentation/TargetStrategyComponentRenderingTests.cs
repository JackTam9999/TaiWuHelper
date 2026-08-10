using System.Text.RegularExpressions;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.Localization;
using TaiWu.Domain.TargetArchetypes;
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
        Assert.DoesNotContain("MIND_RESONANCE_RESET_BASELINE", text);
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
        Assert.Contains("本區只說明可重用的目標策略", text);
        Assert.DoesNotContain("Reusable response strategy", text);
        Assert.DoesNotContain("Availability unresolved", text);
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
            StandaloneGaps = []
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
                    "MIND_RESONANCE_RESET_BASELINE",
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
            []);
    }

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
