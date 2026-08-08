using TaiWu.Application.Localization;

namespace TaiWuAPI.Localization;

public static class UiText
{
    private static readonly Dictionary<string, string>
        ChineseTranslations = new(
            StringComparer.Ordinal)
        {
            ["Skip to main content"] = "跳至主要內容",
            ["TaiWu Helper home"] = "太吾助手首頁",
            ["Pre-fight briefing"] = "戰前簡報",
            ["Information only"] = "僅供參考",
            ["Language"] = "語言",
            ["English"] = "英文",
            ["Chinese"] = "中文",
            ["Combat recommendation"] = "戰鬥推薦",
            ["Primary navigation"] = "主要導覽",
            ["Recommendations"] = "戰鬥推薦",
            ["Skill atlas"] = "功法圖鑑",
            ["Regional stories"] = "地區故事",
            ["Read-only story progress"] = "唯讀故事進度",
            ["Your regional stories, rewards, and next steps."] =
                "你的地區故事、獎勵與下一步。",
            ["Compare every sect's main-story reward and post-story upgrade with progress from the configured save."] =
                "把十五派的主線獎勵、後傳強化與設定存檔中的進度放在一起比較。",
            ["Save remains read only"] = "存檔維持唯讀",
            ["Statuses are derived from ending dates and active world task chains. No story or save value is changed."] =
                "狀態依結局日期及進行中的世界任務鏈判定；不會改動任何故事或存檔數值。",
            ["Fifteen sect stories"] = "十五派地區故事",
            ["Story progress and bonuses"] = "故事進度與獎勵",
            ["Reading story progress…"] = "正在讀取故事進度……",
            ["Refresh save status"] = "重新讀取存檔狀態",
            ["Save configuration is invalid"] = "存檔設定無效",
            ["Set SaveGame:DefaultSaveFilePath before reading regional-story progress."] =
                "請先設定 SaveGame:DefaultSaveFilePath，才能讀取地區故事進度。",
            ["Story progress could not be read"] = "無法讀取故事進度",
            ["The configured save did not contain readable regional-story data."] =
                "設定的存檔不包含可讀取的地區故事資料。",
            ["An unexpected regional-story read failure occurred."] =
                "讀取地區故事時發生未預期的錯誤。",
            ["Story snapshot metadata"] = "故事快照資訊",
            ["Save modified"] = "存檔修改時間",
            ["Read-only fingerprint"] = "唯讀指紋",
            ["Story status summary"] = "故事狀態摘要",
            ["All stories"] = "全部故事",
            ["Completed endings"] = "已完成結局",
            ["In progress"] = "進行中",
            ["No active progress"] = "目前未進行",
            ["Shared story reward"] = "共通故事獎勵",
            ["Completing a regional story grants its sect treasure and raises the power of all that sect's martial arts. Completing the post-story upgrades the treasure into Xuan equipment and strengthens the unique function shown below."] =
                "完成地區故事可取得門派寶物，提高該門派所有功法威力；完成後傳後，寶物會升級為玄字裝備，並強化下列門派特有功能。",
            ["Some story fields were unavailable"] = "部分故事欄位不可用",
            ["Prosperous ending"] = "昌盛結局",
            ["Failing ending"] = "衰落結局",
            ["Not completed"] = "未完成",
            ["Ending recorded"] = "結局記錄",
            ["Current task"] = "當前任務",
            ["Task title unavailable"] = "任務名稱不可用",
            ["Main-story unlock"] = "主線解鎖",
            ["Post-story upgrade"] = "後傳強化",
            ["No stories match this status filter."] =
                "沒有故事符合這個狀態篩選。",
            ["Bonus summaries are based on the installed Encyclopedia. The status column covers the main regional story; post-story completion is not inferred without a verified dedicated flag."] =
                "獎勵摘要依目前安裝版本的百曉冊整理。狀態欄只判定地區故事主線；未取得經驗證的專用旗標前，不會推測後傳是否完成。",
            ["Version-aware skill catalogue"] = "版本感知功法目錄",
            ["Your martial arts, mapped."] = "你的武學，一覽無遺。",
            ["Search every installed combat skill and compare it with the current Taiwu's learned, studied, breakthrough, mastery and equipped state."] =
                "搜尋所有已安裝的戰鬥功法，並與目前太吾的取得、研讀、突破、大成及裝備狀態逐項比對。",
            ["Read-only atlas"] = "唯讀圖鑑",
            ["The catalogue is a helper-owned cache. Save progress is read only and never copied back into the game."] =
                "目錄是助手自有的快取；存檔進度只會被讀取，絕不會寫回遊戲。",
            ["Catalogue and character overlay"] = "目錄與人物進度疊加",
            ["Combat skill atlas"] = "戰鬥功法圖鑑",
            ["Rebuilding local catalogue…"] = "正在重建本機功法目錄……",
            ["Reading catalogue and save…"] = "正在讀取目錄與存檔……",
            ["Clearing local progress cache…"] = "正在清除本機進度快取……",
            ["Atlas freshness"] = "圖鑑新鮮度",
            ["Catalogue"] = "目錄",
            ["Catalogue built"] = "目錄建立時間",
            ["Save read"] = "存檔讀取時間",
            ["Matching skills"] = "符合的功法",
            ["Derived progress cache"] = "衍生人物進度快取",
            ["Stores only recent helper-owned snapshots. Clearing it never changes the save; the next read rebuilds it."] =
                "只保留近期的助手自有快照；清除不會改動存檔，下次讀取時會重新建立。",
            ["Clear local progress cache"] = "清除本機進度快取",
            ["Local progress cache cleared."] = "已清除本機進度快取。",
            ["Local progress cache was already empty."] = "本機進度快取原本已是空白。",
            ["The local progress cache could not be cleared safely."] =
                "無法安全清除本機進度快取。",
            ["Failure"] = "失敗",
            ["The skill atlas could not be read"] = "無法讀取功法圖鑑",
            ["The configured local sources could not be read safely."] =
                "無法安全讀取已設定的本機來源。",
            ["Build local catalogue"] = "建立本機功法目錄",
            ["Character progress is unavailable"] = "無法取得人物功法進度",
            ["Search and filters"] = "搜尋與篩選",
            ["Find a combat skill"] = "尋找戰鬥功法",
            ["Clear filters"] = "清除篩選",
            ["Skill name"] = "功法名稱",
            ["Traditional Chinese or English name"] = "繁體中文或英文名稱",
            ["Search in this language"] = "搜尋目前語言的功法名稱",
            ["Category"] = "類別",
            ["All categories"] = "所有類別",
            ["Grade"] = "品級",
            ["All grades"] = "所有品級",
            ["Apply filters"] = "套用篩選",
            ["More catalogue and progress filters"] = "更多目錄與進度篩選",
            ["Faction"] = "門派",
            ["All factions"] = "所有門派",
            ["No faction filter"] = "不限制門派",
            ["Faction text uses its inner-power color; the outer ring uses its alignment color."] =
                "門派文字使用主內力屬性顏色；外圈使用主立場顏色。",
            ["Unknown faction"] = "未知門派",
            ["Any"] = "不限",
            ["Element"] = "五行",
            ["Equipment type"] = "運功類型",
            ["Learned state"] = "取得狀態",
            ["Learned"] = "已取得",
            ["Not learned"] = "未取得",
            ["Status unavailable"] = "狀態不可用",
            ["Proficiency available"] = "造詣數值可用",
            ["Available"] = "可用",
            ["All details studied"] = "所有研讀細節完成",
            ["Study complete (15/15)"] = "已完成（15/15 研讀）",
            ["Complete"] = "已完成",
            ["Incomplete"] = "未完成",
            ["Breakthrough ready"] = "可突破",
            ["Ready"] = "已就緒",
            ["Not ready"] = "尚未就緒",
            ["Breakthrough completed"] = "突破完成",
            ["Completed"] = "已完成",
            ["Not completed"] = "未完成",
            ["Practice direction"] = "修習方向",
            ["Direct practice"] = "正練",
            ["Reverse practice"] = "逆練",
            ["Attainment mastery"] = "大成狀態",
            ["Mastered"] = "已大成",
            ["Not mastered"] = "未大成",
            ["Simplified"] = "已化簡",
            ["Activated"] = "已啟用",
            ["Equipped"] = "已裝備",
            ["Yes"] = "是",
            ["No"] = "否",
            ["Partial character data"] = "部分人物資料不可用",
            ["No skills match these filters"] = "沒有功法符合這些篩選",
            ["Clear one or more filters and try again."] =
                "請清除一項或多項篩選後再試。",
            ["Showing"] = "顯示",
            ["of"] = "／",
            ["matching skills"] = "項符合的功法",
            ["Candidate limit reached"] = "已達候選上限",
            ["Skill status legend"] = "功法狀態圖例",
            ["Grade order: low to high"] = "品級順序：由低至高",
            ["Low grade"] = "低品",
            ["High grade"] = "高品",
            ["skills"] = "項功法",
            ["skill"] = "項功法",
            ["Skill catalogue pages"] = "功法目錄分頁",
            ["Previous page"] = "上一頁",
            ["Next page"] = "下一頁",
            ["Page"] = "第",
            ["results"] = "項結果",
            ["Unknown category"] = "未知類別",
            ["Grade unavailable"] = "品級不可用",
            ["Base grid cost"] = "基礎佔格",
            ["Character progress"] = "人物進度",
            ["Ready to break through"] = "可突破",
            ["Not ready to break through"] = "未可突破",
            ["Broken through"] = "已突破",
            ["Study progress"] = "研讀進度",
            ["Proficiency"] = "造詣",
            ["Current effective cost"] = "目前有效佔格",
            ["studied"] = "已研讀",
            ["Some fields are unavailable or use a fallback. Open the detail view for provenance."] =
                "部分欄位不可用或使用了備援資料；可在詳細檢視中查看來源。",
            ["Open full skill detail"] = "開啟完整功法詳情",
            ["View catalogue detail"] = "查看功法圖鑑詳情",
            ["Breadcrumb"] = "麵包屑導覽",
            ["Back to skill atlas"] = "返回功法圖鑑",
            ["Skill detail"] = "功法詳情",
            ["Recommendation context"] = "推薦情境",
            ["Opened from a combat recommendation"] = "從戰鬥推薦開啟",
            ["Catalogue availability and raw descriptions do not change recommendation feasibility, rules, threats, counters, or scores."] =
                "功法目錄是否可用及其原始描述，都不會改變推薦的可行性、規則、威脅、克制或評分。",
            ["Back to recommendations"] = "返回戰鬥推薦",
            ["Loading"] = "載入中",
            ["Reading skill detail and current Taiwu progress…"] =
                "正在讀取功法詳情與目前太吾進度……",
            ["The skill detail could not be read"] = "無法讀取功法詳情",
            ["Skill detail is unavailable until the local catalogue is current"] =
                "本機目錄更新前無法顯示功法詳情",
            ["Return to the atlas to build or refresh the helper-owned catalogue."] =
                "請返回圖鑑建立或更新助手自有的功法目錄。",
            ["Open skill atlas"] = "開啟功法圖鑑",
            ["Not found"] = "找不到",
            ["No static combat-skill definition matches this ID"] =
                "沒有符合此 ID 的靜態戰鬥功法定義",
            ["The skill may have been removed or the local catalogue may need rebuilding."] =
                "此功法可能已被移除，或本機目錄需要重建。",
            ["Static definition and current-Taiwu progress are shown separately. No value is written back to the game."] =
                "靜態定義與目前太吾進度會分開顯示；任何數值都不會寫回遊戲。",
            ["Stable skill ID"] = "穩定功法 ID",
            ["Skill ID"] = "功法 ID",
            ["Explicit language availability"] = "明確語言可用性",
            ["Chinese and English names"] = "中文與英文名稱",
            ["Traditional Chinese name"] = "繁體中文名稱",
            ["English name"] = "英文名稱",
            ["fallback"] = "備援",
            ["Fallback"] = "使用備援",
            ["Partial or fallback data"] = "部分或備援資料",
            ["One or more localized fields use a fallback or are unavailable."] =
                "一個或多個本地化欄位使用備援資料或無法取得。",
            ["One or more study details are unavailable for this save version."] =
                "此存檔版本有一個或多個研讀細節無法取得。",
            ["The current effective cost could not be verified."] =
                "無法驗證目前有效佔格。",
            ["Installed read-only sources"] = "已安裝的唯讀來源",
            ["Static definition"] = "靜態定義",
            ["Slot contribution"] = "格數貢獻",
            ["Preparation progress"] = "準備進度",
            ["Breath stance cost"] = "架勢提氣消耗",
            ["Cast speed"] = "施展速度",
            ["Requirements"] = "需求",
            ["Character property requirement"] = "人物屬性需求",
            ["No typed requirements are available."] = "沒有可用的型別化需求。",
            ["Effect references"] = "效果參照",
            ["Direct effect"] = "正練效果",
            ["Reverse effect"] = "逆練效果",
            ["Neutral effect"] = "中性效果",
            ["Current save · read only"] = "目前存檔 · 唯讀",
            ["Current Taiwu state"] = "目前太吾狀態",
            ["Read"] = "讀取於",
            ["Character progress mapping is unsupported"] = "不支援人物進度對應",
            ["Current save is unavailable"] = "目前存檔無法取得",
            ["Character progress could not be read"] = "無法讀取人物進度",
            ["No character entry for this skill"] = "此功法沒有對應的人物進度",
            ["The definition is installed, but the current save has no matching progress record."] =
                "功法定義已安裝，但目前存檔沒有相符的進度記錄。",
            ["Current proficiency"] = "目前造詣",
            ["Maximum proficiency"] = "最高造詣",
            ["Power"] = "功法威力",
            ["Current power"] = "目前功法威力",
            ["Maximum power"] = "威力上限",
            ["Active direction"] = "目前正逆練方向",
            ["Ordered study details"] = "依序排列的研讀細節",
            ["Study-detail map"] = "研讀細節圖",
            ["Study summary"] = "研讀摘要",
            ["verified details studied"] = "項已驗證細節已研讀",
            ["Study progress has not been read"] = "尚未讀取研讀進度",
            ["The static skill remains available, but no character study overlay could be read."] =
                "靜態功法仍可查看，但無法讀取人物研讀進度。",
            ["Study-detail mapping is unavailable"] = "研讀細節對應無法取得",
            ["This save version does not expose a verified ordered detail set for this skill."] =
                "此存檔版本沒有提供此功法經驗證且有序的細節集合。",
            ["Study status legend"] = "研讀狀態圖例",
            ["Studied"] = "已研讀",
            ["Not studied"] = "未研讀",
            ["Exact verified details not studied"] = "尚未研讀的確切已驗證細節",
            ["Every available verified detail has been studied."] =
                "所有可用且已驗證的細節都已研讀。",
            ["Ordered study-detail groups"] = "依序排列的研讀細節群組",
            ["Common"] = "共通",
            ["details"] = "項細節",
            ["Active detail"] = "目前生效",
            ["Source and availability"] = "來源與可用性",
            ["Status"] = "狀態",
            ["Explanation"] = "說明",
            ["Source kind"] = "來源類型",
            ["Source identity"] = "來源識別碼",
            ["Field identity"] = "欄位識別碼",
            ["Detail ID"] = "細節 ID",
            ["Order"] = "順序",
            ["Read state"] = "研讀狀態",
            ["Progress source"] = "進度來源",
            ["Label source"] = "標籤來源",
            ["Unnamed detail"] = "未命名細節",
            ["Display context, not recommendation logic"] = "僅供顯示，不作推薦規則",
            ["Raw descriptions"] = "原始描述",
            ["Original description"] = "原始描述",
            ["Direct practice description"] = "正練描述",
            ["Reverse practice description"] = "逆練描述",
            ["No localized raw descriptions are available."] = "沒有可用的本地化原始描述。",
            ["Verified mechanic"] = "已驗證機制",
            ["Display-only raw text"] = "僅供顯示的原始文字",
            ["The static definition remains available; no save-derived values are inferred."] =
                "靜態定義仍可查看；不會推測任何存檔衍生數值。",
            ["TraditionalChinese"] = "繁體中文",
            ["Effect"] = "效果",
            ["DirectEffect"] = "正練效果",
            ["ReverseEffect"] = "逆練效果",
            ["Requirement"] = "需求",
            ["Other"] = "其他",
            ["Metal"] = "金",
            ["Wood"] = "木",
            ["Water"] = "水",
            ["Fire"] = "火",
            ["Earth"] = "土",
            ["Mixed"] = "混合",
            ["Generic"] = "萬用",
            ["Unsupported"] = "不支援",
            ["Conflicting"] = "互相衝突",
            ["SaveSnapshot"] = "存檔快照",
            ["CurrentScreenObservation"] = "目前畫面觀察",
            ["VerifiedRule"] = "已驗證規則",
            ["TraditionalChineseLanguageResource"] = "繁體中文語言資源",
            ["EnglishLanguageResource"] = "英文語言資源",
            ["Current"] = "最新",
            ["Missing"] = "未建立",
            ["Stale"] = "已過期",
            ["Rebuilding"] = "重建中",
            ["MissingSources"] = "缺少來源",
            ["UnsupportedVersion"] = "版本不支援",
            ["SourceReadFailed"] = "來源讀取失敗",
            ["RepositoryFailed"] = "目錄讀取失敗",
            ["Corrupt"] = "目錄損壞",
            ["SaveMissing"] = "缺少存檔",
            ["SaveReadFailed"] = "存檔讀取失敗",
            ["NotRead"] = "尚未讀取",
            ["Local catalogue not built"] = "尚未建立本機目錄",
            ["Local catalogue is out of date"] = "本機目錄已過期",
            ["Local catalogue is rebuilding"] = "本機目錄正在重建",
            ["Installed version is unsupported"] = "不支援已安裝的版本",
            ["Installed skill sources are missing"] = "缺少已安裝的功法來源",
            ["Local catalogue is unavailable"] = "本機目錄不可用",
            ["Build the helper-owned cache from the installed read-only sources."] =
                "從已安裝的唯讀來源建立助手自有快取。",
            ["The installed sources changed. Rebuild the helper-owned cache before reading the atlas."] =
                "已安裝的來源已變更；請先重建助手自有快取，再讀取圖鑑。",
            ["Wait for the atomic cache replacement to finish, then retry."] =
                "請等待快取的原子替換完成後再試。",
            ["This installed GameData version does not have a verified importer."] =
                "此已安裝的 GameData 版本尚無經驗證的匯入器。",
            ["The installed game or language resources could not be located."] =
                "找不到已安裝的遊戲或語言資源。",
            ["The helper-owned cache could not be read safely."] =
                "無法安全讀取助手自有快取。",
            ["Configure an existing local save and retry."] =
                "請設定一個現有的本機存檔後再試。",
            ["This save version does not have a verified progress mapping."] =
                "此存檔版本尚無經驗證的進度對應。",
            ["The configured save could not be read safely. Close the game while making a stable golden verification if needed."] =
                "無法安全讀取已設定的存檔；如需穩定的黃金驗證，請先關閉遊戲。",
            ["The local catalogue could not be rebuilt safely."] =
                "無法安全重建本機目錄。",
            ["Read-only combat planning"] = "唯讀戰鬥規劃",
            ["Prepare for the fight before it starts."] = "在戰鬥開始前做好準備。",
            ["Search the configured save, choose a target and compare three evidence-backed loadout styles. Every instruction is performed manually by you in the game."] =
                "搜尋已設定的存檔、選擇目標，並比較三種有證據支持的運功方案。所有操作均由你在遊戲中手動完成。",
            ["TaiWu Helper cannot change the save, equip skills, or control the game."] =
                "太吾助手不能修改存檔、裝備功法或控制遊戲。",
            ["Recommendation controls"] = "推薦設定",
            ["Choose the encounter"] = "選擇對手",
            ["Reading the configured save…"] = "正在讀取已設定的存檔……",
            ["Target name"] = "目標姓名",
            ["Enter the in-game character name"] = "輸入遊戲內的人物姓名",
            ["Search"] = "搜尋",
            ["Target search results"] = "目標搜尋結果",
            ["years"] = "歲",
            ["Location unavailable"] = "無法取得地點名稱",
            ["Area"] = "區域",
            ["Block"] = "地塊",
            ["Preferred style"] = "偏好風格",
            ["Safe prioritizes survival; Aggressive accepts more risk."] =
                "穩健優先確保生存；進取則接受較高風險。",
            ["Preferred weapon (optional)"] = "偏好武器（可選）",
            ["No preference"] = "無偏好",
            ["A visible player preference only; verified rules still decide whether a loadout is feasible."] =
                "這只是顯示用的玩家偏好；方案是否可行仍由已驗證規則決定。",
            ["Optional current-screen observation"] = "可選：目前畫面觀察值",
            ["Analysis input only. These values refine this recommendation and are never sent to or applied in the game."] =
                "僅作分析輸入。這些數值只用來改善推薦，絕不會傳送或套用到遊戲。",
            ["Use selected equipped-skill names"] = "使用所選的已裝備功法名稱",
            ["Get an initial recommendation to load available skill names."] =
                "請先取得一次推薦，以載入可用的功法名稱。",
            ["Optional target observation"] = "可選：目標觀察",
            ["Report a visible sparring loadout"] = "回報可見的切磋運功配置",
            ["Report visible target combat information"] =
                "回報畫面可見的目標戰鬥資訊",
            ["Manual input only"] = "僅限手動輸入",
            ["Use this only after opening the opponent's loadout during a martial-arts spar. Hostile and story characters do not expose this screen."] =
                "僅在切磋武功中已打開對手運功畫面時使用。敵對及劇情人物不會顯示此畫面。",
            ["A sparring loadout may support complete coverage. Hostile and story encounters support only partial skill effects visibly exposed by the combat UI."] =
                "切磋運功畫面可以支援完整涵蓋；敵對及劇情情境只能回報戰鬥介面實際顯示的部分功法效果。",
            ["Add a current sparring-opponent observation"] =
                "加入目前切磋對手的觀察",
            ["Add a current target observation"] = "加入目前目標的觀察",
            ["Get a save-only recommendation first so target identity and save freshness can be reviewed before entry."] =
                "請先取得只使用存檔的推薦，以便輸入前核對目標身分與存檔新鮮度。",
            ["Observed target"] = "觀察目標",
            ["Save snapshot read"] = "存檔快照讀取時間",
            ["Where can you inspect this opponent?"] = "你在哪種情境查看此對手？",
            ["Martial-arts spar"] = "切磋武功",
            ["Hostile encounter"] = "敵對戰鬥",
            ["Story encounter"] = "劇情情境",
            ["Opponent loadout unavailable"] = "無法查看對手運功",
            ["The supported game UI does not expose the opponent's loadout for hostile or story characters. No hidden loadout input will be requested."] =
                "目前支援的遊戲介面不會顯示敵對或劇情人物的運功配置，因此不會要求輸入任何隱藏資料。",
            ["Full opponent loadout unavailable"] = "無法查看完整對手運功",
            ["Hostile and story encounters can expose partial skill-effect panels during combat. Report only names, direction, and power actually visible there; omitted skills remain unknown."] =
                "敵對及劇情戰鬥仍可能顯示部分功法效果面板；只回報當中實際可見的名稱、正逆練及威力，未列出的功法仍屬未知。",
            ["How much of the current loadout did you inspect?"] =
                "你查看了目前運功配置的多少內容？",
            ["Partial loadout"] = "部分運功配置",
            ["Only listed skills are confirmed; omitted skills remain unknown."] =
                "只確認列出的功法；未列出的功法仍屬未知。",
            ["Complete current loadout"] = "完整的目前運功配置",
            ["Every visible category and empty slot on this one displayed preset was inspected."] =
                "已查看這一套目前顯示預設中的所有可見分類及空格。",
            ["Partial battle-visible effects"] = "部分戰鬥可見效果",
            ["This evidence confirms only the listed active effects. It cannot establish equipment slots, omitted skills, or a complete loadout."] =
                "此證據只確認列出的生效效果，不能證明裝備欄位、未列出功法或完整運功配置。",
            ["Visible combat-skill name"] = "畫面可見的功法名稱",
            ["Find skill"] = "尋找功法",
            ["Searching…"] = "搜尋中……",
            ["Search the exact name visible in the active game language. Category is verified from the catalogue, not typed manually."] =
                "請依目前遊戲語言搜尋畫面上的確切名稱；分類由目錄驗證，不需手動填寫。",
            ["Catalogue matches"] = "目錄相符項目",
            ["Verified catalogue match"] = "已驗證目錄相符項目",
            ["base slots"] = "基礎格數",
            ["Exact name"] = "名稱完全相符",
            ["Partial name"] = "名稱部分相符",
            ["Confirmed visible skills"] = "已確認的可見功法",
            ["No target skill has been confirmed yet."] = "尚未確認任何目標功法。",
            ["Verified catalogue identity"] = "已驗證目錄身分",
            ["Visible direction"] = "畫面可見的正逆練",
            ["Visible power percent"] = "畫面可見威力百分比",
            ["Evidence only; visible power does not change legality or scoring."] =
                "僅作證據；畫面威力不會改變可行性或評分。",
            ["Not observed"] = "未觀察",
            ["I confirm this visible screen is newer than the configured save snapshot."] =
                "我確認此可見畫面比已設定的存檔快照更新。",
            ["Review observation"] = "檢查觀察內容",
            ["Clear observation"] = "清除觀察",
            ["Observation review"] = "觀察內容檢查",
            ["Target"] = "目標",
            ["Coverage"] = "涵蓋範圍",
            ["Observed at"] = "觀察時間",
            ["Evidence status"] = "證據狀態",
            ["Only the confirmed fields above will receive current-screen precedence. This does not control or modify the game."] =
                "只有上方已確認欄位會採用目前畫面的優先順序；此操作不會控制或修改遊戲。",
            ["Use observation for recommendation"] = "使用觀察內容建立推薦",
            ["Save timestamp unavailable; explicit precedence confirmation is required."] =
                "無法取得存檔時間戳記；必須明確確認來源優先順序。",
            ["Save timestamp available for freshness comparison."] =
                "可使用存檔時間戳記比較新鮮度。",
            ["Target observation is off."] = "目標觀察目前已關閉。",
            ["Editing a session-only target observation."] =
                "正在編輯只限本次工作階段的目標觀察。",
            ["Editing partial battle-visible target effects."] =
                "正在編輯部分戰鬥可見的目標效果。",
            ["Searching catalogue…"] = "正在搜尋目錄……",
            ["More than one catalogue skill matched. Confirm the correct one."] =
                "有多門目錄功法相符，請確認正確項目。",
            ["Review the confirmed evidence before using it."] =
                "使用前請先檢查已確認的證據。",
            ["Applying observation to a new helper snapshot…"] =
                "正在把觀察套用至新的助手快照……",
            ["Observation applied to the helper snapshot."] =
                "觀察已套用至助手快照。",
            ["Observation is not newer than the configured save and was not applied."] =
                "觀察並不比已設定存檔更新，因此未被套用。",
            ["Observation applied with a saved-value conflict; both sources are retained."] =
                "觀察已套用，但與存檔值衝突；兩個來源均已保留。",
            ["Observation is unsupported for this GameData version and was not applied."] =
                "此 GameData 版本不支援該觀察，因此未被套用。",
            ["Confirm that the visible screen is newer because the save timestamp is unavailable."] =
                "由於無法取得存檔時間戳記，請確認可見畫面較新。",
            ["Opponent loadout observation is unavailable in this encounter context."] =
                "此遭遇情境無法觀察對手運功。",
            ["Observation cleared; the recommendation is save-only."] =
                "觀察已清除；推薦現在只使用存檔。",
            ["Target observation state unavailable."] = "無法取得目標觀察狀態。",
            ["Get a save-only recommendation before entering target evidence."] =
                "輸入目標證據前，請先取得只使用存檔的推薦。",
            ["Confirm a martial-arts spar before searching target skills."] =
                "搜尋目標功法前，請先確認情境為切磋武功。",
            ["Choose where the target information is visible before searching target skills."] =
                "搜尋目標功法前，請先選擇可查看目標資訊的情境。",
            ["Battle-visible active effect"] = "戰鬥可見的生效效果",
            ["Enter the combat-skill name visible in the game."] =
                "請輸入遊戲畫面可見的功法名稱。",
            ["Multiple skills matched; choose the verified catalogue entry."] =
                "有多門功法相符；請選擇已驗證的目錄項目。",
            ["No verified catalogue skill matched that visible name."] =
                "沒有已驗證目錄功法符合該可見名稱。",
            ["The current catalogue cannot resolve this target skill."] =
                "目前目錄無法解析此目標功法。",
            ["A partial observation must confirm at least one visible skill."] =
                "部分觀察必須確認至少一門可見功法。",
            ["The selected skill could not be confirmed from the current catalogue."] =
                "無法從目前目錄確認所選功法。",
            ["The observation request could not be completed. Review the fields and try again."] =
                "無法完成觀察要求；請檢查各欄位後重試。",
            ["The target observation is not ready."] = "目標觀察尚未準備完成。",
            ["Observation impact"] = "觀察影響",
            ["Save-only compared with observed"] = "只用存檔與觀察後結果比較",
            ["Evidence comparison"] = "證據比較",
            ["Evidence confidence describes provenance, not a win probability."] =
                "證據信心只描述來源依據，並非勝率。",
            ["Partial observation"] = "部分觀察",
            ["Unlisted target skills remain possible and were not removed from analysis."] =
                "未列出的目標功法仍可能存在，且未從分析中移除。",
            ["Threat changes"] = "威脅變更",
            ["No typed threat changed."] = "沒有已定型威脅發生變更。",
            ["Feasibility changes"] = "可行性變更",
            ["No recommendation changed because of feasibility."] =
                "沒有推薦因可行性而變更。",
            ["Scoring changes"] = "評分變更",
            ["No recommendation changed only because of scoring."] =
                "沒有推薦只因評分而變更。",
            ["Still unsupported"] = "仍未支援",
            ["No unresolved target evidence remains."] =
                "沒有尚未解析的目標證據。",
            ["Still unsupported from the save-only result"] =
                "從只用存檔的結果起仍未支援",
            ["New unsupported observation evidence"] = "新增的未支援觀察證據",
            ["No severity or score was assigned."] = "未給予嚴重度或分數。",
            ["Source conflicts and precedence"] = "來源衝突與優先順序",
            ["Both values are retained; the newer current screen has field-level precedence."] =
                "兩個值均會保留；較新的目前畫面在該欄位具有優先權。",
            ["Added threat"] = "新增威脅",
            ["Confirmed threat"] = "已確認威脅",
            ["Demoted to learned-unconfirmed"] = "降為已學但未確認裝備",
            ["Removed typed threat"] = "移除已定型威脅",
            ["Unchanged threat"] = "未變威脅",
            ["Added recommendation"] = "新增推薦",
            ["Removed recommendation"] = "移除推薦",
            ["Save-equipped"] = "存檔顯示已裝備",
            ["Observed equipped"] = "觀察確認已裝備",
            ["Learned, not confirmed equipped"] = "已學但未確認裝備",
            ["Unresolved target evidence"] = "尚未解析的目標證據",
            ["unresolved effect"] = "尚未解析的效果",
            ["Equipped loadout"] = "已裝備運功配置",
            ["Save snapshot"] = "存檔快照",
            ["Current screen observation"] = "目前畫面觀察",
            ["Verified rule"] = "已驗證規則",
            ["Game configuration"] = "遊戲設定",
            ["Evidence chain"] = "證據鏈",
            ["Selection feasibility"] = "配置可行性",
            ["total"] = "總數",
            ["Selected"] = "已選擇",
            ["Safe"] = "穩健",
            ["Balanced"] = "均衡",
            ["Aggressive"] = "進取",
            ["Select a target before requesting a recommendation."] =
                "請先選擇目標，再取得推薦。",
            ["Calculating…"] = "計算中……",
            ["Get recommendation"] = "取得推薦",
            ["Snapshot read"] = "存檔快照",
            ["Snapshot metadata"] = "快照資訊",
            ["Source freshness"] = "來源新鮮度",
            ["Unavailable"] = "無法取得",
            ["Weapon preference"] = "武器偏好",
            ["Current inner-power state"] = "目前內力狀態",
            ["Applied to actively cast skills; equipping Neigong alone is not treated as a cast."] =
                "只套用於實際施展的功法；單純裝備內功不視為施展。",
            ["None"] = "無",
            ["Same snapshot · three policies"] = "同一快照 · 三種策略",
            ["Recommendation ready"] = "推薦已完成",
            ["Recommendation styles"] = "推薦風格",
            ["Known-constraint score"] = "已知條件評分",
            ["This is not a win probability."] = "此數值並非勝率。",
            ["Threats identified"] = "已識別威脅",
            ["Manual changes"] = "手動變更",
            ["Conditional caveats"] = "條件式注意事項",
            ["Inner-power compatibility"] = "內力狀態相容性",
            ["No feasible recommendation was produced."] = "未能產生可行的推薦。",
            ["Manual configuration"] = "手動配置",
            ["Recommended loadout"] = "推薦運功配置",
            ["Known constraints validated"] = "已驗證所有已知條件",
            ["Target analysis"] = "目標分析",
            ["Target threats"] = "目標威脅",
            ["Clear focus"] = "清除焦點",
            ["Critical"] = "嚴重",
            ["High"] = "高",
            ["Moderate"] = "中",
            ["At combat start"] = "戰鬥開始時",
            ["Always active"] = "持續生效",
            ["On skill use"] = "施展功法時",
            ["On hit"] = "命中時",
            ["When a mark is applied"] = "施加標記時",
            ["At mark threshold"] = "標記達到門檻時",
            ["Timing unavailable"] = "無法取得生效時機",
            ["evidence source(s)"] = "項證據來源",
            ["Select a threat to highlight the skills and plan steps that address it."] =
                "選擇一項威脅，以標示對應的功法與計劃步驟。",
            ["No skill selected in this category."] = "此類別未選擇任何功法。",
            ["Recommended capacity"] = "推薦容量",
            ["Recommended 萬用 allocation"] = "建議萬用配置",
            ["slots"] = "格",
            ["Usage unavailable"] = "無法取得佔用量",
            ["Practice"] = "修習",
            ["Actual cost"] = "實際佔格",
            ["Effective cost"] = "有效佔格",
            ["Activation"] = "生效方式",
            ["Add · change direction"] = "加入 · 更改正逆練",
            ["Break through · add"] = "突破 · 加入",
            ["Complete breakthrough"] = "完成突破",
            ["Change direction"] = "更改正逆練",
            ["Add manually"] = "手動加入",
            ["Keep"] = "保留",
            ["Recommended"] = "推薦",
            ["Direct"] = "正練",
            ["Reverse"] = "逆練",
            ["Neutral"] = "中性",
            ["Unavailable · see warning"] = "無法取得 · 請查看警告",
            ["Combat-start passive"] = "戰鬥開始時被動生效",
            ["Equipped passive"] = "裝備後被動生效",
            ["Active attack"] = "主動摧破",
            ["Active defense"] = "主動護體",
            ["Active agility"] = "主動輕靈",
            ["No counter timing"] = "無克制生效時機",
            ["Satisfied"] = "已滿足",
            ["Not satisfied"] = "未滿足",
            ["Unknown"] = "未知",
            ["Skill requirements"] = "功法需求",
            ["Evidence and linked threats"] = "證據與關聯威脅",
            ["Unnamed skill"] = "未命名功法",
            ["Target-threat evidence"] = "目標威脅證據",
            ["Slot-cost evidence"] = "佔格證據",
            ["Counter-effect evidence"] = "克制效果證據",
            ["Requirement evidence"] = "需求證據",
            ["Manual setup"] = "手動設定",
            ["Setup checklist"] = "設定檢查表",
            ["Copy checklist"] = "複製檢查表",
            ["Print recommendation"] = "列印推薦",
            ["Instructions only: TaiWu Helper cannot perform these steps."] =
                "僅提供操作說明：太吾助手不能執行這些步驟。",
            ["No manual setup differences were produced."] = "沒有需要手動變更的設定。",
            ["Reason and evidence"] = "原因與證據",
            ["Why this step"] = "為何需要此步驟",
            ["The selected recommendation requires these generic slots in this category."] =
                "所選推薦配置需要將這些萬用欄位分配至此類別。",
            ["This skill has a weapon condition that must be checked manually."] =
                "此功法具有需要玩家手動確認的兵器條件。",
            ["This skill has a resource condition that must be checked manually."] =
                "此功法具有需要玩家手動確認的資源條件。",
            ["Recommendation checklist copied."] = "已複製推薦檢查表。",
            ["Remove"] = "移除",
            ["Add"] = "加入",
            ["Direction"] = "正逆練",
            ["Breakthrough"] = "突破",
            ["Weapon"] = "武器",
            ["Resource"] = "資源",
            ["Trick"] = "式",
            ["Range"] = "距離",
            ["Weapon unlock"] = "兵器解鎖",
            ["Skill activation"] = "功法生效狀態",
            ["Fight reference"] = "戰鬥參考",
            ["Battle plan"] = "戰鬥計劃",
            ["No separate evidence-backed instruction is available for this phase."] =
                "此階段沒有獨立且有證據支持的指示。",
            ["Skill"] = "功法",
            ["Supporting detail"] = "補充資料",
            ["Verify the recommendation"] = "核對推薦",
            ["Alternatives"] = "其他方案",
            ["Score"] = "評分",
            ["manual changes"] = "項手動變更",
            ["caveats"] = "項注意事項",
            ["Assumptions and unavailable data"] = "假設與無法取得的資料",
            ["Assumption"] = "假設",
            ["Unavailable data"] = "無法取得",
            ["No additional assumption or unavailable-data caveat was produced."] =
                "沒有其他假設或資料缺失的注意事項。",
            ["Conditional requirements"] = "條件式需求",
            ["No conditional requirement was selected for this style."] =
                "此風格未選取任何條件式需求。",
            ["Score contributions"] = "評分構成",
            ["Component"] = "項目",
            ["Weight"] = "權重",
            ["Points"] = "分數",
            ["Explanation"] = "說明",
            ["Detailed evidence"] = "詳細證據",
            ["Target threat"] = "目標威脅",
            ["Recommended skill"] = "推薦功法",
            ["Recommendation scoring"] = "推薦評分",
            ["Warnings and caveats"] = "警告與注意事項",
            ["Analysis"] = "分析",
            ["Manual review"] = "手動檢查",
            ["No evidence reference was available."] = "沒有可用的證據參照。",
            ["Review before setup"] = "設定前請檢查",
            ["Warnings and unavailable information"] = "警告與無法取得的資訊",
            ["Critical review required"] = "需要重點檢查",
            ["Aggregated from"] = "彙整自",
            ["evaluated combinations."] = "個已評估組合。",
            ["Effect on recommendation:"] = "對推薦的影響：",
            ["Warning evidence"] = "警告證據",
            ["Target loadout unavailable"] = "無法取得目標運功配置",
            ["Target equipped skills unavailable"] = "無法取得目標已裝備功法",
            ["Unrecognized target mechanic"] = "無法識別的目標機制",
            ["Next step:"] = "下一步：",
            ["Retry read"] = "重新讀取",
            ["Start with a target"] = "先選擇目標",
            ["Search the configured save by character name, then select the intended opponent."] =
                "以人物姓名搜尋已設定的存檔，然後選擇預定對手。",
            ["Searching the configured save"] = "正在搜尋已設定的存檔",
            ["Building the combat recommendation"] = "正在建立戰鬥推薦",
            ["Reading a new snapshot from the configured save. No game data is changed."] =
                "正在從已設定的存檔讀取新快照。遊戲資料不會被更改。",
            ["No matching target"] = "找不到相符目標",
            ["The configured save returned no target for this search."] =
                "已設定的存檔中沒有符合此搜尋的目標。",
            ["Target found"] = "已找到目標",
            ["Select the matching result, review the context, and request the recommendation."] =
                "選擇相符結果、檢查資料，然後取得推薦。",
            ["Target selected"] = "已選擇目標",
            ["Recommendation ready with warnings"] = "推薦已完成，但有警告",
            ["A recommendation was produced, but unavailable or uncertain information requires manual review."] =
                "推薦已產生，但無法取得或不確定的資訊仍需手動檢查。",
            ["Read every warning before following the manual setup."] =
                "依照手動設定操作前，請先閱讀所有警告。",
            ["The recommendation satisfies every known constraint in this snapshot."] =
                "此推薦符合快照中的所有已知條件。",
            ["Save path is not configured"] = "尚未設定存檔路徑",
            ["Could not complete the read"] = "無法完成讀取",
            ["Retry the read. TaiWu Helper did not change the save or game."] =
                "請重新讀取。太吾助手沒有更改存檔或遊戲。",
            ["Before combat"] = "戰鬥前",
            ["Opening"] = "開局",
            ["Normal execution"] = "一般應對",
            ["Trigger-based reactions"] = "觸發式應對",
            ["Switching conditions"] = "切換條件",
            ["Stale data"] = "過期資料",
            ["Observation difference"] = "觀察值差異",
            ["Unavailable value"] = "無法取得的數值",
            ["Unverified mechanic"] = "未驗證機制",
            ["Candidate search"] = "候選方案搜尋",
            ["General"] = "一般",
            ["slot use"] = "格位使用量",
            ["Neigong"] = "內功",
            ["Attack"] = "摧破",
            ["Agility"] = "輕靈",
            ["Defense"] = "護體",
            ["Assistance"] = "奇竅",
            ["EquippedPassive"] = "裝備後被動",
            ["ActiveDefense"] = "啟動中的護體",
            ["ActiveAgility"] = "啟動中的輕靈",
            ["Threat coverage"] = "威脅覆蓋",
            ["Survival"] = "生存能力",
            ["Execution reliability"] = "執行可靠度",
            ["Current-loadout compatibility"] = "現有配置相容度",
            ["Damage potential"] = "傷害潛力",
            ["Opportunity cost"] = "格位機會成本",
            ["Conditional risk"] = "條件風險",
            ["Multiple targets matched"] = "找到多個相符目標",
            ["Check the in-game name and search again."] =
                "請核對遊戲內的姓名後再次搜尋。",
            ["If the intended opponent is still unclear, gather more in-game evidence before requesting a recommendation."] =
                "若仍無法確定預定對手，請先在遊戲中取得更多證據，再要求推薦。",
            ["Review the target context before requesting a recommendation."] =
                "取得推薦前，請先核對目標資料。",
            ["Unsupported GameData version"] = "不支援的 GameData 版本",
            ["Verified mechanic rules do not cover this GameData version, so the helper does not estimate the missing recommendation."] =
                "已驗證的機制規則未涵蓋此 GameData 版本，因此助手不會估算缺少的推薦。",
            ["Use a save from a verified game version or update the helper's evidence-backed rules, then retry the read."] =
                "請使用已驗證遊戲版本的存檔，或更新助手中有證據支持的規則，再重新讀取。",
            ["The helper needs a valid absolute .sav path before it can read a snapshot."] =
                "助手需要有效的 .sav 絕對路徑，才能讀取快照。",
            ["Set SaveGames:DefaultSaveFilePath to an absolute .sav path and restart TaiWu Helper."] =
                "請將 SaveGames:DefaultSaveFilePath 設為 .sav 絕對路徑，然後重新啟動太吾助手。",
            ["An unexpected read or calculation failure occurred."] =
                "讀取或計算時發生未預期的錯誤。",
            ["An unexpected target read failure occurred."] =
                "讀取目標時發生未預期的錯誤。",
            ["An unexpected recommendation read or calculation failure occurred."] =
                "讀取或計算推薦時發生未預期的錯誤。",
            ["The target search could not read valid data from the configured save."] =
                "目標搜尋無法從已設定存檔讀取有效資料。",
            ["The recommendation could not be built from the available save data."] =
                "無法使用存檔中的現有資料建立推薦。",
            ["Source timestamp is newer than the read"] =
                "來源時間戳記晚於本次讀取時間",
            ["Information only — TaiWu Helper cannot apply, equip, or execute this recommendation."] =
                "僅供參考 — 太吾助手不能套用、裝備或執行此推薦。",
            ["Unknown values remain unavailable. TaiWu Helper never replaces them with estimates."] =
                "未知數值會維持無法取得；太吾助手絕不以估算值代替。",
            ["No feasible scored candidate is available for a manual combat plan."] =
                "沒有可用於手動戰鬥計劃的可行評分候選方案。",
            ["No eligible loadout options remain after hard candidate filters."] =
                "經強制候選條件篩選後，沒有符合資格的運功方案。",
            ["The affected value remains unavailable and is not replaced with an estimate; review the related caveat manually."] =
                "受影響的數值仍無法取得，且不會以估算值代替；請手動檢查相關注意事項。",
            ["The target's exact equipped loadout remains unconfirmed. Recommendations use known target skills and verified mechanics; equipped-only conclusions are excluded."] =
                "目標實際裝備的運功配置仍未確認。推薦會使用目標的已知功法及已驗證機制，並排除僅在確認裝備後才能成立的結論。",
            ["The affected mechanic was excluded from verified scoring, so threat coverage may be incomplete."] =
                "受影響的機制已從已驗證評分中排除，因此威脅覆蓋可能不完整。",
            ["The affected option was excluded before scoring; returned candidates still satisfy known constraints."] =
                "受影響的選項已在評分前排除；回傳的候選方案仍符合已知條件。",
            ["No eligible option survived validation; this style cannot provide a feasible recommendation."] =
                "沒有選項通過驗證；此風格無法提供可行的推薦。",
            ["This warning is retained with the recommendation for manual review and does not receive an inferred replacement value."] =
                "此警告會隨推薦保留供手動檢查，且不會以推定值代替。",
            ["The current-screen input was not used as the authoritative value; verify the displayed loadout before following it."] =
                "目前畫面的輸入未被視為權威數值；採用推薦前請核對畫面上的運功配置。",
            ["Snapshot freshness cannot be fully established; reread the save before relying on time-sensitive details."] =
                "無法完全確認快照的新鮮度；依賴時效性資料前請重新讀取存檔。",
            ["Positive-practice magic-sound mind damage"] =
                "正練魔音造成的失神傷害",
            ["Positive-practice magic-sound attacks accumulate mind-loss damage and pressure guarding-mind defense."] =
                "正練魔音攻擊會累積失神傷害，並對守心防禦造成壓力。",
            ["Distraction-mark accumulation"] = "失神標記累積",
            ["Mind-loss damage produces distraction marks that can directly advance the player's defeat condition."] =
                "失神傷害會產生失神標記，並可能直接推進玩家的戰敗條件。",
            ["Mind-resonance cascade"] = "心神共鳴連鎖",
            ["The first distraction mark begins a countdown; when it expires, mind resonance applies repeated mind-loss pressure and can make new marks persistent."] =
                "第一個失神標記會開始倒數；倒數結束後，心神共鳴會反覆施加失神壓力，並可能使新標記持續存在。",
            ["Repeatable defeat-mark reset"] = "可重複清除戰敗標記",
            ["Reverse-practice 九色玉蝉法 consumes 9 Qiqiao true-Qi when the target reaches the defeat condition, clears all injury, hindrance, and critical-injury marks, then raises the next cost by 9 up to 99. Surviving alone cannot win while the target can keep paying this cost."] =
                "逆練九色玉蟬法在目標達到戰敗條件時消耗 9 點奇竅真氣，清除全部傷勢、妨害及重創標記；之後每次消耗增加 9 點，最高為 99 點。只要目標仍能支付，單純生存並不足以取勝。",
            ["The observed reset at 36 defeat marks resembles reverse 九色玉蟬法, but the target's equipped source effect is not confirmed."] =
                "觀察到的 36 個戰敗標記重置現象類似逆練九色玉蟬法，但尚未確認目標所裝備的效果來源。",
            ["The target's active loadout is not present in this disk save; GameData may select NPC combat skills during combat preparation."] =
                "此磁碟存檔不含目標的實際運功配置；GameData 可能在準備戰鬥時才替 NPC 選擇功法。",
            ["The current save contains no equipped target skills."] =
                "目前存檔不含目標已裝備的功法。",
            ["Configured GridCost and confirmed mastery were mapped, but effective used capacity remains unavailable because the standalone-unsafe SpecialEffect calculation was not invoked."] =
                "已讀取設定的功法佔格與已確認的精解狀態，但為避免呼叫不適合獨立讀檔環境的特殊效果計算，有效佔用容量仍無法取得。",
            ["Configured slot capacities were mapped, but runtime capacity modifiers were not invoked. Supply current-screen displayed slot budgets when exact capacities differ."] =
                "已讀取設定的欄位容量，但未呼叫執行階段的容量修正。若實際容量不同，請提供目前遊戲畫面顯示的欄位上限。",
            ["Used slots cannot exceed capacity."] =
                "已使用格數不能超過容量。",
            ["Used slots cannot exceed capacity. (Parameter 'used')"] =
                "已使用格數不能超過容量。",
            ["Retain the skill because the selected loadout preserves this current selection."] =
                "保留此功法，因為所選運功方案保留了目前配置。",
            ["Add the skill because it is part of the highest-ranked feasible loadout."] =
                "加入此功法，因為它屬於評分最高的可行運功方案。",
            ["Remove the skill manually because it is absent from the highest-ranked feasible loadout."] =
                "手動移除此功法，因為評分最高的可行運功方案未包含它。",
            ["The skill is part of the highest-ranked feasible loadout."] =
                "此功法屬於評分最高的可行運功方案。",
            ["Follow this opening step to obtain the verified counter effect represented by the selected candidate."] =
                "依照此開局步驟操作，以取得所選候選方案所代表的已驗證克制效果。",
            ["Use this feasible active-role choice according to its ranked candidate and verified counter evidence."] =
                "依照候選方案排名及已驗證的克制證據，使用這項可行的主動功法選擇。",
            ["Before combat or between attempts, choose the alternative if the primary skill's activation requirements cannot be satisfied."] =
                "若主要功法的生效需求無法滿足，請在戰鬥前或兩次嘗試之間改用替代功法。",
            ["Before combat begins, confirm this passive is equipped."] =
                "戰鬥開始前，確認已裝備此被動功法。",
            ["Keep this passive equipped while its counter is needed."] =
                "需要此克制效果期間，請保持裝備這項被動功法。",
            ["At the opening, select this as the active defense skill; activate it only when its requirements are satisfied."] =
                "開局時將其選為主動護體功法；只有在滿足需求時才運起。",
            ["At the opening, select this as the active agility skill; activate it only when its requirements are satisfied."] =
                "開局時將其選為主動輕靈功法；只有在滿足需求時才施展。",
            ["At the opening, use this attack only when its activation requirements are satisfied."] =
                "開局時，只有在滿足生效需求時才施展此摧破功法。",
            ["Interrupts, clears, and temporarily prevents the target's Direct-practice skills."] =
                "打斷、清除並暫時阻止目標的正練功法。",
            ["Starts with a finite pool that automatically removes the player's hindrance marks after the defeat-mark threshold."] =
                "戰鬥開始時取得有限次數；達到戰敗標記門檻後會自動移除玩家的妨害標記。",
            ["Greatly shortens the player's mind-resonance duration while this agility skill is active."] =
                "此輕靈功法生效期間，大幅縮短玩家的心神共鳴持續時間。",
            ["Greatly shortens the duration of the player's distraction marks."] =
                "大幅縮短玩家失神標記的持續時間。",
            ["Reduces all enemy attack-skill power according to achieved effectiveness for the rest of combat."] =
                "依實際發揮降低所有敵方摧破功法的威力，並持續至戰鬥結束。",
            ["No verified counter mapping is attached; this skill was selected for another stated reason."] =
                "此功法沒有已驗證的克制對應；它是基於另一項已說明的理由而選取。",
            ["Severity-weighted target threats covered by selected counter options."] =
                "所選克制方案覆蓋的目標威脅，按嚴重程度加權。",
            ["Severity-weighted hard-counter and mitigation protection."] =
                "強力克制與緩解保護，按威脅嚴重程度加權。",
            ["Penalizes manual direction changes and active-attack execution steps."] =
                "對手動更改正逆練及主動摧破操作步驟扣分。",
            ["Share of current equipped skills retained in the candidate."] =
                "候選方案保留目前已裝備功法的比例。",
            ["Caller-supplied, evidence-backed damage potential."] =
                "由呼叫端提供且有證據支持的傷害潛力。",
            ["No verified damage evidence is available; this component is excluded from the normalized total."] =
                "沒有可用的已驗證傷害證據；此項不計入正規化總分。",
            ["Share of total feasible slot capacity left unused."] =
                "可行總格位容量中尚未使用的比例。",
            ["Penalizes unsatisfied or unknown conditional requirements."] =
                "對未滿足或未知的條件式需求扣分。",
            ["Compatibility of actively cast skills with the current inner-power state, including power, requirements, and backlash on use."] =
                "評估實際施展功法與目前內力狀態的相容性，包括威力、發揮需求及施展反噬。",
            ["This threat input is observational or hypothetical and may not represent stable game data."] =
                "此威脅輸入來自觀察或假設，可能無法代表穩定的遊戲資料。",
            ["Effective used capacity requires combat-skill cost rules that are not evaluated by the read-only adapter."] =
                "有效佔用容量需要計算唯讀配接器未評估的功法成本規則。",
            ["The save does not contain all four generic-grid allocation values."] =
                "存檔未包含全部四項萬用欄位分配值。",
            ["The current-screen observation was not applied because it is not newer than the disk save."] =
                "目前畫面的觀察值未套用，因為它並不比磁碟存檔更新。",
            ["The current-screen observation was applied using source precedence because the save timestamp is unavailable."] =
                "因無法取得存檔時間戳記，已按來源優先順序套用目前畫面的觀察值。",
            ["TaiWu Helper recommendation — information only"] =
                "太吾助手推薦 — 僅供參考",
            ["TaiWu Helper cannot perform these steps."] =
                "太吾助手不能執行這些步驟。",
            ["Page not found"] = "找不到頁面",
            ["This path is not part of TaiWu Helper."] = "此路徑不屬於太吾助手。",
            ["Return to the recommendation page"] = "返回推薦頁面"
        };

    public static string Get(TaiwuLanguage language, string english)
    {
        ArgumentNullException.ThrowIfNull(english);
        if (language != TaiwuLanguage.Chinese)
        {
            return english;
        }

        return ChineseTranslations.TryGetValue(english, out var translation)
            ? translation
            : DynamicUiText.Get(english);
    }
}
