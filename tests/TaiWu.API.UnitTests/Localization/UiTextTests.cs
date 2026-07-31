using TaiWu.Application.Localization;
using TaiWuAPI.Localization;
using Xunit;

namespace TaiWu.API.UnitTests.Localization;

public sealed class UiTextTests
{
    public static TheoryData<string, string> ChineseExamples =>
        new()
        {
            {
                "Positive-practice magic-sound mind damage",
                "正練魔音造成的失神傷害"
            },
            {
                "Retain the skill because the selected loadout preserves "
                + "this current selection.",
                "保留此功法，因為所選運功方案保留了目前配置。"
            },
            {
                "Target 16317's active loadout is not present in the current "
                + "disk save. GameData may select NPC combat skills during "
                + "combat preparation; recommendations use known skills and "
                + "verified mechanics instead.",
                "目前磁碟存檔中沒有目標 16317 的實際運功配置。"
                + "GameData 可能在準備戰鬥時才替 NPC 選擇功法；"
                + "推薦將改用已知功法及已驗證機制。"
            },
            {
                "Target equipped skills are unavailable: The current save "
                + "contains no equipped target skills.",
                "無法取得目標已裝備功法：目前存檔不含目標已裝備的功法。"
            },
            {
                "Used slots cannot exceed capacity. (Parameter 'used') "
                + "Occurred in 22 explored combinations.",
                "已使用格數不能超過容量。"
                + " 此情況出現在 22 個已探索組合中。"
            },
            {
                "Skill 604 is Neutral and cannot activate its Reverse "
                + "effect.",
                "功法 604 目前為中性，無法啟動其逆練效果。"
            },
            {
                "Skill 267 is Reverse, not Direct.",
                "功法 267 為逆練，並非正練。"
            },
            {
                "Skill 686 satisfies EquippedPassive.",
                "功法 686 已滿足裝備後被動生效條件。"
            },
            {
                "Remove skill 604 manually.",
                "手動移除功法 604。"
            },
            {
                "Add skill 624 to Attack manually.",
                "手動將功法 624 加入摧破欄。"
            },
            {
                "Keep skill 0 in Neigong.",
                "將功法 0 保留在內功欄。"
            },
            {
                "Allocate 4 萬用 slot(s) to 摧破.",
                "將 4 個萬用欄位分配至摧破欄。"
            },
            {
                "Use 伏龙刀法 when its listed conditions and linked threat "
                + "timing are present.",
                "施展伏龙刀法；僅在其列出的條件及關聯威脅時機都符合時操作。"
            },
            {
                "31 possible targets were found. Select one using its age, "
                + "character ID, and location.",
                "找到 31 個可能的目標。請依年齡、人物 ID 與所在地點選擇。"
            },
            {
                "葛贵婵 is selected for the next read-only analysis.",
                "已選擇葛贵婵，將用於下一次唯讀分析。"
            },
            {
                "The archive reached the expected standalone event-runtime "
                + "boundary: Void InitRuntimeEnvironment()",
                "存檔已讀取至預期的獨立事件執行環境邊界："
                + "Void InitRuntimeEnvironment()"
            }
        };

    [Theory]
    [MemberData(nameof(ChineseExamples))]
    public void Chinese_mode_localizes_generated_recommendation_text(
        string english,
        string expected)
    {
        var actual = UiText.Get(TaiwuLanguage.Chinese, english);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ChineseExamples))]
    public void English_mode_preserves_source_text(
        string english,
        string _)
    {
        var actual = UiText.Get(TaiwuLanguage.English, english);

        Assert.Equal(english, actual);
    }

    [Theory]
    [InlineData("TARGET_LOADOUT_NOT_PERSISTED")]
    [InlineData("snapshot:target:16317:equipped-skills")]
    [InlineData("GameData")]
    public void Chinese_mode_preserves_technical_identifiers(string value)
    {
        var actual = UiText.Get(TaiwuLanguage.Chinese, value);

        Assert.Equal(value, actual);
    }
}
