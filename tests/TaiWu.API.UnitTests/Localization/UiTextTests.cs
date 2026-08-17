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
                "Repeatable defeat-mark reset",
                "可重複清除戰敗標記"
            },
            {
                "Retain the skill because the selected loadout preserves "
                + "this current selection.",
                "保留此功法，因為所選運功方案保留了目前配置。"
            },
            {
                "the selected target's active loadout is not present in the current "
                + "disk save. GameData may select NPC combat skills during "
                + "combat preparation; recommendations use known skills and "
                + "verified mechanics instead.",
                "目前磁碟存檔中沒有所選目標的實際運功配置。"
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
                "金猊鎮魔刀 is Neutral and cannot activate its Reverse "
                + "effect.",
                "金猊鎮魔刀目前為中性，無法啟動其逆練效果。"
            },
            {
                "伏龍刀法 is Reverse, not Direct.",
                "伏龍刀法為逆練，並非正練。"
            },
            {
                "老君拂塵功 requires Reverse, but its practice direction is "
                + "unavailable: 老君拂塵功 has not completed breakthrough, "
                + "so its practice direction is not active.",
                "老君拂塵功尚未完成突破，因此逆練效果目前不可用。"
            },
            {
                "老君拂塵功 satisfies EquippedPassive.",
                "老君拂塵功已滿足裝備後被動生效條件。"
            },
            {
                "Remove 金猊鎮魔刀 manually.",
                "手動移除金猊鎮魔刀。"
            },
            {
                "Add 伏龍刀法 to Attack manually.",
                "手動將伏龍刀法加入摧破欄。"
            },
            {
                "Keep 沛然訣 in Neigong.",
                "將沛然訣保留在內功欄。"
            },
            {
                "Change 金猊鎮魔刀 to 逆練 (Reverse).",
                "將金猊鎮魔刀改為逆練。"
            },
            {
                "Change direction manually to activate the verified Reverse "
                + "effect used by this recommendation.",
                "調整正逆練，使本次推薦所使用的已驗證逆練效果生效。"
            },
            {
                "Complete 老君拂塵功's breakthrough as 逆練 (Reverse) before "
                + "combat.",
                "先將老君拂塵功完成逆練突破，再開始戰鬥。"
            },
            {
                "Complete breakthrough manually as Reverse before using this "
                + "recommendation; only then is the verified effect active.",
                "先手動完成逆練突破，再使用此推薦；只有完成後，"
                + "本次推薦所使用的已驗證效果才會生效。"
            },
            {
                "老君拂塵功 requires Reverse, but its immediately available "
                + "breakthrough cannot produce Reverse.",
                "老君拂塵功目前可以突破，但無法突破成逆練。"
            },
            {
                "老君拂塵功 requires Reverse, but it has not completed "
                + "breakthrough and cannot break through now.",
                "老君拂塵功尚未完成突破，而且目前仍未滿足突破條件。"
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
                "Before combat, confirm 老君拂塵功 is equipped so its passive "
                + "can activate.",
                "戰鬥開始前，確認已裝備老君拂塵功，使其被動效果能夠生效。"
            },
            {
                "At the opening, use 伏龍刀法 once its activation requirements "
                + "are satisfied.",
                "開局時，在滿足生效需求後施展伏龍刀法。"
            },
            {
                "Before combat or between attempts, use 水火硬氣功 instead of "
                + "曼荼羅真言 if 曼荼羅真言's activation requirements cannot "
                + "be satisfied.",
                "若主要功法的生效需求無法滿足，請在戰鬥前或兩次嘗試之間，"
                + "以水火硬氣功替代曼荼羅真言。"
            },
            {
                "31 possible targets were found. Select one using its name, "
                + "age, and named location.",
                "找到 31 個可能的目標。請依姓名、年齡與地點名稱選擇。"
            },
            {
                "葛贵婵 is selected for the next read-only analysis.",
                "已選擇葛贵婵，將用於下一次唯讀分析。"
            },
            {
                "Report a visible sparring loadout",
                "回報可見的切磋運功配置"
            },
            {
                "Target observation is off.",
                "目標觀察目前已關閉。"
            },
            {
                "Editing a session-only target observation.",
                "正在編輯只限本次工作階段的目標觀察。"
            },
            {
                "Searching catalogue…",
                "正在搜尋目錄……"
            },
            {
                "Page",
                "第"
            },
            {
                "The supported game UI does not expose the opponent's "
                + "loadout for hostile or story characters. No hidden "
                + "loadout input will be requested.",
                "目前支援的遊戲介面不會顯示敵對或劇情人物的運功配置，"
                + "因此不會要求輸入任何隱藏資料。"
            },
            {
                "More than one catalogue skill matched. Confirm the correct one.",
                "有多門目錄功法相符，請確認正確項目。"
            },
            {
                "Review the confirmed evidence before using it.",
                "使用前請先檢查已確認的證據。"
            },
            {
                "Applying observation to a new helper snapshot…",
                "正在把觀察套用至新的助手快照……"
            },
            {
                "Observation applied to the helper snapshot.",
                "觀察已套用至助手快照。"
            },
            {
                "Observation is not newer than the configured save and was "
                + "not applied.",
                "觀察並不比已設定存檔更新，因此未被套用。"
            },
            {
                "Observation applied with a saved-value conflict; both "
                + "sources are retained.",
                "觀察已套用，但與存檔值衝突；兩個來源均已保留。"
            },
            {
                "Observation is unsupported for this GameData version and "
                + "was not applied.",
                "此 GameData 版本不支援該觀察，因此未被套用。"
            },
            {
                "Confirm that the visible screen is newer because the save "
                + "timestamp is unavailable.",
                "由於無法取得存檔時間戳記，請確認可見畫面較新。"
            },
            {
                "Opponent loadout observation is unavailable in this "
                + "encounter context.",
                "此遭遇情境無法觀察對手運功。"
            },
            {
                "Observation cleared; the recommendation is save-only.",
                "觀察已清除；推薦現在只使用存檔。"
            },
            {
                "The Aggressive style result is unavailable in this "
                + "recommendation.",
                "此推薦中無法取得進取方案結果。"
            },
            {
                "No feasible Balanced policy winner is available.",
                "沒有可用的均衡策略可行最佳方案。"
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
