using TaiWu.Application.Localization;

namespace TaiWuAPI.Contracts.CombatRecommendations;

internal static class TargetStrategyText
{
    internal static string Archetype(
        TaiwuLanguage language,
        string code) => Get(language, code, Archetypes);

    internal static string Goal(
        TaiwuLanguage language,
        string code) => Get(language, code, Goals);

    internal static string Gap(
        TaiwuLanguage language,
        string resourceKey) => Get(language, resourceKey, Gaps);

    internal static string AdjustmentReason(
        TaiwuLanguage language,
        string code) => Get(language, code, AdjustmentReasons);

    private static string Get(
        TaiwuLanguage language,
        string code,
        IReadOnlyDictionary<string, BilingualText> values)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        return values.TryGetValue(code, out var value)
            ? language == TaiwuLanguage.Chinese
                ? value.Chinese
                : value.English
            : code;
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
