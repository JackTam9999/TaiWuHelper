using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWuAPI.Localization;

internal static class TargetStrategyUiText
{
    internal static string Archetype(
        TaiwuLanguage language,
        string code) => Get(
        language,
        code,
        Archetypes,
        "Verified target pattern",
        "已驗證的目標類型");

    internal static string Goal(
        TaiwuLanguage language,
        string code) => Get(
        language,
        code,
        Goals,
        "Verified response goal",
        "已驗證的應對目標");

    internal static string Facet(
        TaiwuLanguage language,
        string code) => Get(
        language,
        code,
        Facets,
        "Verified profile fact",
        "已驗證的目標特徵");

    internal static string Gap(
        TaiwuLanguage language,
        string resourceKey) => Get(
        language,
        resourceKey,
        Gaps,
        "A verified playbook gap remains unresolved.",
        "仍有一項已驗證的策略缺口尚未解決。");

    internal static string AdjustmentReason(
        TaiwuLanguage language,
        string code) => Get(
        language,
        code,
        AdjustmentReasons,
        "Exact-target evidence changed this response.",
        "目標精確證據改變了此應對方式。");

    internal static string Dimension(
        TaiwuLanguage language,
        TargetProfileDimension dimension) => dimension switch
        {
            TargetProfileDimension.AttackFamily => Bilingual(
                language,
                "Attack-family context",
                "攻擊類型背景"),
            TargetProfileDimension.Pressure => Bilingual(
                language,
                "Pressure",
                "攻勢壓力"),
            TargetProfileDimension.Resilience => Bilingual(
                language,
                "Resilience",
                "防禦韌性"),
            TargetProfileDimension.Control => Bilingual(
                language,
                "Control",
                "控制"),
            TargetProfileDimension.Tempo => Bilingual(
                language,
                "Tempo",
                "節奏"),
            _ => throw new ArgumentOutOfRangeException(nameof(dimension))
        };

    internal static string MatchState(
        TaiwuLanguage language,
        TargetArchetypeMatchState state) => state switch
        {
            TargetArchetypeMatchState.Matched => Bilingual(
                language,
                "Matched",
                "已匹配"),
            TargetArchetypeMatchState.Partial => Bilingual(
                language,
                "Partial",
                "部分匹配"),
            TargetArchetypeMatchState.NotMatched => Bilingual(
                language,
                "Not matched",
                "不匹配"),
            TargetArchetypeMatchState.Unsupported => Bilingual(
                language,
                "Unsupported",
                "不支援"),
            TargetArchetypeMatchState.Conflicting => Bilingual(
                language,
                "Conflicting evidence",
                "證據衝突"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    internal static string ProfileState(
        TaiwuLanguage language,
        TargetProfileEvidenceState state) => state switch
        {
            TargetProfileEvidenceState.Confirmed => Bilingual(
                language,
                "Verified",
                "已驗證"),
            TargetProfileEvidenceState.Incomplete => Bilingual(
                language,
                "Incomplete",
                "資料不完整"),
            TargetProfileEvidenceState.Unsupported => Bilingual(
                language,
                "Unsupported",
                "不支援"),
            TargetProfileEvidenceState.Conflicting => Bilingual(
                language,
                "Conflicting",
                "互相衝突"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    internal static string Priority(
        TaiwuLanguage language,
        TargetResponsePriority priority) => priority switch
        {
            TargetResponsePriority.Critical => Bilingual(
                language,
                "Critical response",
                "關鍵應對"),
            TargetResponsePriority.High => Bilingual(
                language,
                "High priority",
                "高優先"),
            TargetResponsePriority.Normal => Bilingual(
                language,
                "Normal priority",
                "一般優先"),
            TargetResponsePriority.Fallback => Bilingual(
                language,
                "Fallback",
                "備用"),
            _ => throw new ArgumentOutOfRangeException(nameof(priority))
        };

    internal static string Timing(
        TaiwuLanguage language,
        CombatCounterActivationTiming timing) => timing switch
        {
            CombatCounterActivationTiming.CombatStartPassive => Bilingual(
                language,
                "Ready at combat start",
                "戰鬥開始時生效"),
            CombatCounterActivationTiming.EquippedPassive => Bilingual(
                language,
                "Equipped passive",
                "裝備後被動生效"),
            CombatCounterActivationTiming.ActiveAttack => Bilingual(
                language,
                "Active attack",
                "主動摧破"),
            CombatCounterActivationTiming.ActiveDefense => Bilingual(
                language,
                "Active defense",
                "主動護體"),
            CombatCounterActivationTiming.ActiveAgility => Bilingual(
                language,
                "Active agility",
                "主動輕靈"),
            _ => throw new ArgumentOutOfRangeException(nameof(timing))
        };

    internal static string Availability(
        TaiwuLanguage language,
        TargetPlaybookCounterAvailabilityState state) => state switch
        {
            TargetPlaybookCounterAvailabilityState.Feasible => Bilingual(
                language,
                "Available now",
                "目前可用"),
            TargetPlaybookCounterAvailabilityState.Inaccessible => Bilingual(
                language,
                "Not accessible",
                "目前無法取得"),
            TargetPlaybookCounterAvailabilityState.Infeasible => Bilingual(
                language,
                "Does not fit this loadout",
                "無法放入目前運功"),
            TargetPlaybookCounterAvailabilityState.Unresolved => Bilingual(
                language,
                "Availability unresolved",
                "可用性未確定"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    internal static string Direction(
        TaiwuLanguage language,
        PracticeDirection direction) => direction switch
        {
            PracticeDirection.Direct => Bilingual(
                language,
                "Direct practice",
                "正練"),
            PracticeDirection.Reverse => Bilingual(
                language,
                "Reverse practice",
                "逆練"),
            PracticeDirection.Neutral => Bilingual(
                language,
                "Direction neutral",
                "正逆未定"),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

    internal static string EvidenceSource(
        TaiwuLanguage language,
        TargetProfileEvidenceSourceKind kind) => kind switch
        {
            TargetProfileEvidenceSourceKind.SavedEquippedMembership =>
                Bilingual(language, "Save equipment", "存檔裝備"),
            TargetProfileEvidenceSourceKind.InstalledConfiguration =>
                Bilingual(language, "Installed rules", "目前安裝資料"),
            TargetProfileEvidenceSourceKind.CurrentScreenObservation =>
                Bilingual(language, "Current observation", "目前畫面觀察"),
            TargetProfileEvidenceSourceKind.SavedBaseCharacter =>
                Bilingual(language, "Save attributes", "存檔人物屬性"),
            TargetProfileEvidenceSourceKind.VerifiedRule =>
                Bilingual(language, "Verified mechanic", "已驗證機制"),
            TargetProfileEvidenceSourceKind.SyntheticFixture =>
                Bilingual(language, "Verification fixture", "驗證案例"),
            TargetProfileEvidenceSourceKind.SavedLoadoutSource =>
                Bilingual(language, "Save loadout", "存檔運功"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    internal static string Bilingual(
        TaiwuLanguage language,
        string english,
        string chinese)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        return language == TaiwuLanguage.Chinese ? chinese : english;
    }

    private static string Get(
        TaiwuLanguage language,
        string code,
        IReadOnlyDictionary<string, BilingualText> values,
        string fallbackEnglish,
        string fallbackChinese)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        return values.TryGetValue(code, out var value)
            ? language == TaiwuLanguage.Chinese
                ? value.Chinese
                : value.English
            : Bilingual(language, fallbackEnglish, fallbackChinese);
    }

    private static IReadOnlyDictionary<string, BilingualText> Archetypes
    { get; } = new Dictionary<string, BilingualText>(
            StringComparer.Ordinal)
    {
        ["MIND_RESONANCE_RESET_BASELINE"] = new(
                "Mind resonance and defeat-reset chain",
                "心神共鳴與敗北標記重置連鎖"),
        ["OUTER_DAMAGE_CONFIGURED"] = new(
                "Configured outer-damage pressure",
                "已配置外傷壓力"),
        ["CHANNEL_RESISTANCE_ASYMMETRY"] = new(
                "Outer/inner resistance asymmetry",
                "內外傷抗性不對稱"),
        ["POISON_APPLICATION_CONFIGURED"] = new(
                "Configured poison application",
                "已配置毒素施加")
    };

    private static IReadOnlyDictionary<string, BilingualText> Goals
    { get; } = new Dictionary<string, BilingualText>(
            StringComparer.Ordinal)
    {
        ["SURVIVE_MIND_DAMAGE_PRESSURE"] = new(
                "Survive mind-damage pressure",
                "承受心神傷害壓力"),
        ["CONTROL_DISTRACTION_MARKS"] = new(
                "Control distraction marks",
                "控制失神標記"),
        ["BREAK_MIND_RESONANCE_CASCADE"] = new(
                "Break the mind-resonance cascade",
                "阻斷心神共鳴連鎖"),
        ["PRESSURE_DEFEAT_MARK_RESET"] = new(
                "Pressure the defeat-mark reset",
                "壓制敗北標記重置"),
        ["PREPARE_FOR_OUTER_DAMAGE"] = new(
                "Prepare for configured outer damage",
                "準備應對已配置外傷"),
        ["EXPLOIT_LESS_RESISTED_CHANNEL"] = new(
                "Exploit the less-resisted damage channel",
                "利用抗性較低的傷害通道"),
        ["MITIGATE_CONFIGURED_POISON_APPLICATION"] = new(
                "Mitigate configured poison application",
                "緩解已配置毒素施加")
    };

    private static IReadOnlyDictionary<string, BilingualText> Facets
    { get; } = new Dictionary<string, BilingualText>(
            StringComparer.Ordinal)
    {
        ["WEAPON_SUBTYPE"] = new("Weapon-family context", "武器類型背景"),
        ["OUTER_DAMAGE_CONFIGURED"] = new(
                "Configured outer-damage pressure",
                "已配置外傷壓力"),
        ["CHANNEL_RESISTANCE_ASYMMETRY"] = new(
                "Outer/inner resistance asymmetry",
                "內外傷抗性不對稱"),
        ["POISON_APPLICATION_CONFIGURED"] = new(
                "Configured poison application",
                "已配置毒素施加"),
        ["MIND_DAMAGE_PRESSURE"] = new(
                "Mind-damage pressure",
                "心神傷害壓力"),
        ["DISTRACTION_MARK_CONTROL"] = new(
                "Distraction-mark control",
                "失神標記控制"),
        ["MIND_RESONANCE_CONTROL"] = new(
                "Mind-resonance cascade",
                "心神共鳴連鎖"),
        ["DEFEAT_MARK_RESET"] = new(
                "Defeat-mark reset",
                "戰敗標記重置")
    };

    private static IReadOnlyDictionary<string, BilingualText> Gaps
    { get; } = new Dictionary<string, BilingualText>(
            StringComparer.Ordinal)
    {
        ["TargetPlaybook.Gap.NoVerifiedOuterDamageCounter"] = new(
                "No verified outer-damage counter is registered yet.",
                "目前尚未登記經驗證的外傷應對功法。"),
        ["TargetPlaybook.Gap.NoVerifiedChannelAccessOption"] = new(
                "No verified option for exploiting the resistance gap is "
                + "registered yet.",
                "目前尚未登記可利用抗性差距的經驗證功法。"),
        ["TargetPlaybook.Gap.NoVerifiedPoisonCounter"] = new(
                "No verified poison counter is registered yet.",
                "目前尚未登記經驗證的毒素應對功法。"),
        ["TargetPlaybook.Gap.NoGuaranteedResetLockout"] = new(
                "No verified option guarantees that the reset cannot recur.",
                "目前沒有經驗證的功法可保證重置不會再次發生。"),
        ["TargetPlaybook.Gap.PlayerCannotAccessVerifiedCounter"] = new(
                "The exact verified counter is inaccessible or infeasible "
                + "for the current player snapshot.",
                "目前角色快照無法使用或無法容納此項精確驗證功法。")
    };

    private static IReadOnlyDictionary<string, BilingualText>
        AdjustmentReasons
    { get; } = new Dictionary<string, BilingualText>(
            StringComparer.Ordinal)
    {
        ["EXACT_TARGET_SUPPORTS_RESPONSE"] = new(
                "Exact target evidence supports this response.",
                "此應對方式獲得目標精確證據支持。"),
        ["CURRENT_OBSERVATION_CONFIRMS_RESPONSE"] = new(
                "The current observation confirms and elevates this "
                + "response.",
                "目前觀察確認此應對方式，並提高其優先度。"),
        ["EXACT_TARGET_EVIDENCE_INCOMPLETE"] = new(
                "Exact target evidence is incomplete.",
                "目標精確證據尚不完整。"),
        ["PLAYBOOK_GAP_REMAINS_UNRESOLVED"] = new(
                "The verified playbook gap remains unresolved.",
                "經驗證的策略缺口仍未解決。"),
        ["EXACT_TARGET_THREAT_OUTSIDE_PLAYBOOK"] = new(
                "The exact target has a verified threat outside the matched "
                + "playbook.",
                "此目標具有已驗證、但不在已匹配策略中的威脅。")
    };

    private sealed record BilingualText(string English, string Chinese);
}
