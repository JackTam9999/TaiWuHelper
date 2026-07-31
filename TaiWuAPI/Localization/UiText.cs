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
