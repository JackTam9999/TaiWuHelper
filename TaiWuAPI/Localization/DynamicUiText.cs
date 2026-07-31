using System.Text.RegularExpressions;
using TaiWu.Application.Localization;

namespace TaiWuAPI.Localization;

internal static partial class DynamicUiText
{
    public static string Get(string english)
    {
        ArgumentNullException.ThrowIfNull(english);

        var match = PossibleTargetsPattern().Match(english);
        if (match.Success)
        {
            return $"找到 {match.Groups["count"].Value} 個可能的目標。"
                + "請依年齡、人物 ID 與所在地點選擇。";
        }

        match = SelectedTargetPattern().Match(english);
        if (match.Success)
        {
            return $"已選擇{match.Groups["name"].Value}，"
                + "將用於下一次唯讀分析。";
        }

        match = TargetWithoutSkillsPattern().Match(english);
        if (match.Success)
        {
            return $"目前磁碟存檔中，目標 {match.Groups["id"].Value} "
                + "沒有已裝備功法；目前遊戲畫面的證據可能較新。";
        }

        match = TargetSkillsUnavailablePattern().Match(english);
        if (match.Success)
        {
            return "無法取得目標已裝備功法："
                + UiText.Get(
                    TaiwuLanguage.Chinese,
                    match.Groups["reason"].Value);
        }

        match = UnrecognizedTargetMechanicPattern().Match(english);
        if (match.Success)
        {
            return "無法識別的目標機制："
                + UiText.Get(
                    TaiwuLanguage.Chinese,
                    match.Groups["mechanic"].Value);
        }

        match = StandaloneBoundaryPattern().Match(english);
        if (match.Success)
        {
            return "存檔已讀取至預期的獨立事件執行環境邊界："
                + match.Groups["detail"].Value;
        }

        match = OccurrencePattern().Match(english);
        if (match.Success)
        {
            return UiText.Get(
                    TaiwuLanguage.Chinese,
                    match.Groups["reason"].Value)
                + $" 此情況出現在 {match.Groups["count"].Value} "
                + "個已探索組合中。";
        }

        match = SkillCannotActivatePattern().Match(english);
        if (match.Success)
        {
            return $"功法 {match.Groups["id"].Value} 目前為"
                + $"{Direction(match.Groups["current"].Value)}，"
                + $"無法啟動其{Direction(match.Groups["required"].Value)}效果。";
        }

        match = SkillDirectionMismatchPattern().Match(english);
        if (match.Success)
        {
            return $"功法 {match.Groups["id"].Value} 為"
                + $"{Direction(match.Groups["current"].Value)}，"
                + $"並非{Direction(match.Groups["required"].Value)}。";
        }

        match = RemoveSkillPattern().Match(english);
        if (match.Success)
        {
            return $"手動移除功法 {match.Groups["id"].Value}。";
        }

        match = AddSkillPattern().Match(english);
        if (match.Success)
        {
            return $"手動將功法 {match.Groups["id"].Value} 加入"
                + $"{Category(match.Groups["category"].Value)}欄。";
        }

        match = KeepSkillPattern().Match(english);
        if (match.Success)
        {
            return $"將功法 {match.Groups["id"].Value} 保留在"
                + $"{Category(match.Groups["category"].Value)}欄。";
        }

        match = ChangeDirectionPattern().Match(english);
        if (match.Success)
        {
            return $"將功法 {match.Groups["id"].Value} 改為"
                + $"{Direction(match.Groups["direction"].Value)}。";
        }

        match = AllocateGenericSlotsPattern().Match(english);
        if (match.Success)
        {
            return $"將 {match.Groups["count"].Value} 個萬用欄位分配至"
                + $"{Category(match.Groups["category"].Value)}欄。";
        }

        match = ConfirmRequirementPattern().Match(english);
        if (match.Success)
        {
            return $"確認{match.Groups["skill"].Value}："
                + UiText.Get(
                    TaiwuLanguage.Chinese,
                    match.Groups["evaluation"].Value);
        }

        match = UseSkillPattern().Match(english);
        if (match.Success)
        {
            var action = match.Groups["action"].Value == "Use"
                ? "施展"
                : "運起";
            return $"{action}{match.Groups["skill"].Value}；"
                + "僅在其列出的條件及關聯威脅時機都符合時操作。";
        }

        match = SkillActivationPattern().Match(english);
        if (match.Success)
        {
            var status = match.Groups["negative"].Success
                ? "未滿足"
                : "已滿足";
            return $"功法 {match.Groups["id"].Value} {status}"
                + $"{ActivationState(match.Groups["state"].Value)}條件。";
        }

        match = WeaponStatePattern().Match(english);
        if (match.Success)
        {
            var status = match.Groups["negative"].Success ? "未裝備" : "已裝備";
            return $"{status}兵器類型 {match.Groups["id"].Value}。";
        }

        match = WeaponUnlockPattern().Match(english);
        if (match.Success)
        {
            var status = match.Groups["negative"].Success ? "尚未解鎖" : "已解鎖";
            return $"兵器類型 {match.Groups["id"].Value}{status}。";
        }

        match = TrickCountPattern().Match(english);
        if (match.Success)
        {
            return $"式類型 {match.Groups["id"].Value} 目前有 "
                + $"{match.Groups["actual"].Value} 個；"
                + $"需要 {match.Groups["required"].Value} 個。";
        }

        match = DistanceStatePattern().Match(english);
        if (match.Success)
        {
            var state = match.Groups["state"].Value == "within"
                ? "位於"
                : "不在";
            return $"戰鬥距離 {match.Groups["distance"].Value} "
                + $"{state}需求範圍內。";
        }

        match = ResourceCountPattern().Match(english);
        if (match.Success)
        {
            return $"{Resource(match.Groups["resource"].Value)}目前有 "
                + $"{match.Groups["actual"].Value}；"
                + $"需要 {match.Groups["required"].Value}。";
        }

        match = ResourceUnavailablePattern().Match(english);
        if (match.Success)
        {
            return $"{Resource(match.Groups["resource"].Value)}無法取得："
                + match.Groups["reason"].Value;
        }

        match = ResourceNotReportedPattern().Match(english);
        if (match.Success)
        {
            return $"未回報{Resource(match.Groups["resource"].Value)}。";
        }

        match = ExplorationLimitPattern().Match(english);
        if (match.Success)
        {
            return $"組合探索已在設定上限 "
                + $"{match.Groups["count"].Value} 停止。";
        }

        match = ResultLimitPattern().Match(english);
        if (match.Success)
        {
            return $"可行結果已限制為 {match.Groups["shown"].Value} 個，"
                + $"原有 {match.Groups["total"].Value} 個。";
        }

        match = SkillEffectMismatchPattern().Match(english);
        if (match.Success)
        {
            return $"功法 {match.Groups["id"].Value} 與預期效果 "
                + $"{match.Groups["effect"].Value} 不符。";
        }

        match = SkillCategoryUnsupportedPattern().Match(english);
        if (match.Success)
        {
            return $"功法 {match.Groups["id"].Value} 的裝備類型 "
                + $"{match.Groups["type"].Value} 不受支援，因此已略過。";
        }

        match = SkillGridBonusPattern().Match(english);
        if (match.Success)
        {
            return $"功法 {match.Groups["id"].Value} 的欄位加成無效："
                + match.Groups["reason"].Value;
        }

        match = SkillMissingValuePattern().Match(english);
        if (match.Success)
        {
            return $"功法 {match.Groups["id"].Value} "
                + MissingValue(match.Groups["value"].Value);
        }

        match = GenericSlotMismatchPattern().Match(english);
        if (match.Success)
        {
            return $"存檔分配了 {match.Groups["assigned"].Value} 個萬用欄位，"
                + $"但已讀取的功法與特性設定只能解釋 "
                + $"{match.Groups["configured"].Value} 個；"
                + "已保留存檔中的分配總數。";
        }

        match = ObservedSkillUnlearnedPattern().Match(english);
        if (match.Success)
        {
            return $"畫面觀察到的功法 {match.Groups["id"].Value} "
                + "尚未由玩家習得。";
        }

        match = ObservedSkillCategoryPattern().Match(english);
        if (match.Success)
        {
            return $"畫面觀察到的功法 {match.Groups["id"].Value} "
                + $"屬於{Category(match.Groups["actual"].Value)}，"
                + $"而非{Category(match.Groups["expected"].Value)}。";
        }

        return english;
    }

    private static string Category(string value) => value switch
    {
        "Neigong" => "內功",
        "Attack" => "摧破",
        "Agility" => "輕靈",
        "Defense" => "護體",
        "Assistance" => "奇竅",
        _ => value
    };

    private static string Direction(string value) => value switch
    {
        "Direct" or "正練 (Direct)" => "正練",
        "Reverse" or "逆練 (Reverse)" => "逆練",
        "Neutral" or "中性 (Neutral)" => "中性",
        "the required practice direction" => "所需的修習方向",
        _ => value
    };

    private static string ActivationState(string value) => value switch
    {
        "EquippedPassive" => "裝備後被動生效",
        "ActiveDefense" => "主動護體生效",
        "ActiveAgility" => "主動輕靈生效",
        _ => value
    };

    private static string Resource(string value) => value switch
    {
        "Stance" => "架勢",
        "Breath" => "提氣",
        _ => value
    };

    private static string MissingValue(string value) => value switch
    {
        "name" => "沒有設定名稱。",
        "positive configured GridCost" => "沒有設定正數的功法佔格。",
        _ => $"缺少{value}。"
    };

    [GeneratedRegex(
        @"^(?<count>\d+) possible targets were found\. Select one using its age, character ID, and location\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex PossibleTargetsPattern();

    [GeneratedRegex(
        @"^(?<name>.+) is selected for the next read-only analysis\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SelectedTargetPattern();

    [GeneratedRegex(
        @"^Target (?<id>\d+) has no equipped skills in the current disk save\. Current-screen evidence may be newer\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TargetWithoutSkillsPattern();

    [GeneratedRegex(
        @"^Target equipped skills are unavailable: (?<reason>.+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TargetSkillsUnavailablePattern();

    [GeneratedRegex(
        @"^Unrecognized target mechanic: (?<mechanic>.+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex UnrecognizedTargetMechanicPattern();

    [GeneratedRegex(
        @"^The archive reached the expected standalone event-runtime boundary: (?<detail>.+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex StandaloneBoundaryPattern();

    [GeneratedRegex(
        @"^(?<reason>.+) Occurred in (?<count>\d+) explored combinations\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex OccurrencePattern();

    [GeneratedRegex(
        @"^Skill (?<id>\d+) is (?<current>Direct|Reverse|Neutral) and cannot activate its (?<required>Direct|Reverse) effect\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SkillCannotActivatePattern();

    [GeneratedRegex(
        @"^Skill (?<id>\d+) is (?<current>Direct|Reverse|Neutral), not (?<required>Direct|Reverse|Neutral)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SkillDirectionMismatchPattern();

    [GeneratedRegex(
        @"^Remove skill (?<id>\d+) manually\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex RemoveSkillPattern();

    [GeneratedRegex(
        @"^Add skill (?<id>\d+) to (?<category>\w+) manually\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex AddSkillPattern();

    [GeneratedRegex(
        @"^Keep skill (?<id>\d+) in (?<category>\w+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex KeepSkillPattern();

    [GeneratedRegex(
        @"^Change skill (?<id>\d+) to (?<direction>.+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ChangeDirectionPattern();

    [GeneratedRegex(
        @"^Allocate (?<count>\d+) 萬用 slot\(s\) to (?<category>.+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex AllocateGenericSlotsPattern();

    [GeneratedRegex(
        @"^Confirm for (?<skill>.+): (?<evaluation>.+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ConfirmRequirementPattern();

    [GeneratedRegex(
        @"^(?<action>Use|Activate) (?<skill>.+) when its listed conditions and linked threat timing are present\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex UseSkillPattern();

    [GeneratedRegex(
        @"^Skill (?<id>\d+) (?<negative>does not )?satisf(?:y|ies) (?<state>EquippedPassive|ActiveDefense|ActiveAgility)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SkillActivationPattern();

    [GeneratedRegex(
        @"^Weapon type (?<id>\d+) is (?<negative>not )?equipped\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex WeaponStatePattern();

    [GeneratedRegex(
        @"^Weapon type (?<id>\d+) is (?<negative>not )?unlocked\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex WeaponUnlockPattern();

    [GeneratedRegex(
        @"^Trick type (?<id>\d+) has (?<actual>\d+) available; (?<required>\d+) required\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TrickCountPattern();

    [GeneratedRegex(
        @"^Combat distance (?<distance>-?\d+) is (?<state>within|outside) the required range\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex DistanceStatePattern();

    [GeneratedRegex(
        @"^(?<resource>\w+) has (?<actual>-?\d+); (?<required>-?\d+) required\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ResourceCountPattern();

    [GeneratedRegex(
        @"^(?<resource>\w+) is unavailable: (?<reason>.+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ResourceUnavailablePattern();

    [GeneratedRegex(
        @"^(?<resource>\w+) was not reported\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ResourceNotReportedPattern();

    [GeneratedRegex(
        @"^Combination exploration stopped at the configured limit of (?<count>\d+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ExplorationLimitPattern();

    [GeneratedRegex(
        @"^Feasible results were limited to (?<shown>\d+) of (?<total>\d+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ResultLimitPattern();

    [GeneratedRegex(
        @"^Skill (?<id>\d+) does not match expected effect (?<effect>\d+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SkillEffectMismatchPattern();

    [GeneratedRegex(
        @"^Skill (?<id>\d+) has unsupported equip type (?<type>.+) and was omitted\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SkillCategoryUnsupportedPattern();

    [GeneratedRegex(
        @"^Skill (?<id>\d+) grid bonuses were invalid: (?<reason>.+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SkillGridBonusPattern();

    [GeneratedRegex(
        @"^Skill (?<id>\d+) has no configured (?<value>name|positive configured GridCost)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SkillMissingValuePattern();

    [GeneratedRegex(
        @"^The save allocates (?<assigned>\d+) generic slots but the mapped skill and feature configuration explains (?<configured>\d+)\. The allocated total was retained\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex GenericSlotMismatchPattern();

    [GeneratedRegex(
        @"^Observed skill (?<id>\d+) is not learned by the player\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ObservedSkillUnlearnedPattern();

    [GeneratedRegex(
        @"^Observed skill (?<id>\d+) belongs to (?<actual>\w+), not (?<expected>\w+)\.$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ObservedSkillCategoryPattern();
}
