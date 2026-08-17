using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWuAPI.Localization;

public enum CompanionFinderUiTextKey
{
    PageTitle = 0,
    Eyebrow = 1,
    HeroCopy = 2,
    InformationOnly = 3,
    InformationNotice = 4,
    CandidateBoundary = 5,
    SelectObjective = 6,
    RoleFamily = 7,
    MartialRole = 8,
    LifeSkillRole = 9,
    Discipline = 10,
    ChooseDiscipline = 11,
    FindCandidates = 12,
    FindingCandidates = 13,
    SelectRoleAndDiscipline = 14,
    DisciplineSourceUnavailable = 15,
    DisciplineSourceUnavailableMessage = 16,
    RetryLabels = 17,
    Result = 18,
    PreviousResult = 19,
    PreviousResultMessage = 20,
    SnapshotCurrent = 21,
    PartialResult = 22,
    SnapshotCaptured = 23,
    ScoreHeading = 24,
    ScoreLimitation = 25,
    Considered = 26,
    Eligible = 27,
    Ranked = 28,
    Tied = 29,
    NeedsReview = 30,
    Ineligible = 31,
    Incomplete = 32,
    Unsupported = 33,
    Conflicting = 34,
    Filters = 35,
    ShowCandidates = 36,
    All = 37,
    FilterVisibleNames = 38,
    Candidate = 39,
    SavedBaseQualification = 40,
    State = 41,
    Evidence = 42,
    Location = 43,
    Compare = 44,
    RankedCandidates = 45,
    NeedsReviewCandidates = 46,
    IneligibleCandidates = 47,
    NoVisibleCandidates = 48,
    Unavailable = 49,
    UnnamedCandidate = 50,
    LocationUnavailable = 51,
    SelectForComparison = 52,
    SelectedForComparison = 53,
    ComparisonLimit = 54,
    ComparisonReady = 55,
    Comparison = 56,
    ClearComparison = 57,
    EvaluationState = 58,
    HardGates = 59,
    RoleLocalScore = 60,
    CompetitionRank = 61,
    RelativeResult = 62,
    ReviewManually = 63,
    EvidenceAndScope = 64,
    EvidenceAndScopeMessage = 65,
    ReadingTitle = 66,
    ReadingMessage = 67,
    CancelledTitle = 68,
    CancelledMessage = 69,
    RetryRead = 70,
    SaveUnavailableTitle = 71,
    SaveUnavailableMessage = 72,
    UnsupportedSourceTitle = 73,
    UnsupportedSourceMessage = 74,
    ChangedRevisionTitle = 75,
    ChangedRevisionMessage = 76,
    ReadFailedTitle = 77,
    ReadFailedMessage = 78,
    EmptyTitle = 79,
    EmptyMessage = 80,
    PartialMessage = 81,
    ConfirmedEvidence = 82,
    MissingEvidence = 83,
    IncompleteEvidence = 84,
    UnsupportedEvidence = 85,
    StaleEvidence = 86,
    ConflictingEvidence = 87,
    Rank = 88,
    NotRanked = 89,
    ResultSummary = 90,
    DecisiveStrengths = 91,
    MaterialLimitations = 92,
    RequirementEvidence = 93,
    EnrichmentCurrentTitle = 94,
    EnrichmentCurrentMessage = 95,
    CandidateEvidencePartialTitle = 96,
    CandidateEvidencePartialMessage = 97,
    CatalogueMissingTitle = 98,
    CatalogueMissingMessage = 99,
    CatalogueSourcesMissingTitle = 100,
    CatalogueSourcesMissingMessage = 101,
    CatalogueStaleTitle = 102,
    CatalogueStaleMessage = 103,
    CatalogueRebuildingTitle = 104,
    CatalogueRebuildingMessage = 105,
    CatalogueUnsupportedTitle = 106,
    CatalogueUnsupportedMessage = 107,
    CatalogueSourceReadFailedTitle = 108,
    CatalogueSourceReadFailedMessage = 109,
    CatalogueRepositoryFailedTitle = 110,
    CatalogueRepositoryFailedMessage = 111,
    CatalogueCorruptTitle = 112,
    CatalogueCorruptMessage = 113
}

public static class CompanionFinderUiText
{
    public static string RequirementSummary(
        TaiwuLanguage language,
        int passed,
        int total)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(passed);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);
        if (passed > total)
        {
            throw new ArgumentOutOfRangeException(
                nameof(passed),
                passed,
                "Passed requirements cannot exceed total requirements.");
        }

        var needsReview = total - passed;
        return language switch
        {
            TaiwuLanguage.English when needsReview == 0 =>
                $"All {total} requirements passed",
            TaiwuLanguage.English when needsReview == 1 =>
                $"1 of {total} requirements needs review",
            TaiwuLanguage.English =>
                $"{needsReview} of {total} requirements need review",
            TaiwuLanguage.Chinese when needsReview == 0 =>
                $"已通過全部 {total} 項條件",
            TaiwuLanguage.Chinese =>
                $"{total} 項條件中有 {needsReview} 項需檢查",
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown UI language.")
        };
    }

    public static string Get(
        TaiwuLanguage language,
        CompanionFinderUiTextKey key)
    {
        var (english, chinese) = key switch
        {
            CompanionFinderUiTextKey.PageTitle =>
                ("Companion finder", "同道人選比較"),
            CompanionFinderUiTextKey.Eyebrow =>
                ("Current group · one discipline", "目前隊伍 · 單一類別"),
            CompanionFinderUiTextKey.HeroCopy =>
                ("Choose one verified aptitude role, then review an evidence-aware shortlist from one stable configured-save snapshot.",
                 "選擇一項已驗證的資質目標，再查看由單一穩定存檔快照產生、保留證據狀態的人選清單。"),
            CompanionFinderUiTextKey.InformationOnly =>
                ("Information only", "僅供參考"),
            CompanionFinderUiTextKey.InformationNotice =>
                ("TaiWu Helper reads the configured save. It cannot recruit, train, move, equip, assign, or otherwise change anyone or the game.",
                 "太吾助手只會讀取已設定的存檔；不能招募、訓練、移動、裝備、指派或以其他方式改變人物或遊戲。"),
            CompanionFinderUiTextKey.CandidateBoundary =>
                ("Candidate boundary: the current saved Taiwu group roster excluding the Taiwu player. Membership and living-state evidence determine eligibility.",
                 "人選範圍：目前存檔中的太吾隊伍名冊（不含太吾本人）；隊伍成員與在世狀態的證據決定是否符合資格。"),
            CompanionFinderUiTextKey.SelectObjective =>
                ("Select the comparison objective", "選擇比較目標"),
            CompanionFinderUiTextKey.RoleFamily =>
                ("Role family", "目標類型"),
            CompanionFinderUiTextKey.MartialRole =>
                ("Martial discipline aptitude", "武學資質"),
            CompanionFinderUiTextKey.LifeSkillRole =>
                ("Life-skill discipline aptitude", "技藝資質"),
            CompanionFinderUiTextKey.Discipline =>
                ("Discipline", "類別"),
            CompanionFinderUiTextKey.ChooseDiscipline =>
                ("Choose a discipline", "選擇類別"),
            CompanionFinderUiTextKey.FindCandidates =>
                ("Find candidates", "查找人選"),
            CompanionFinderUiTextKey.FindingCandidates =>
                ("Finding candidates…", "正在查找人選……"),
            CompanionFinderUiTextKey.SelectRoleAndDiscipline =>
                ("Select a role family and discipline to enable the read-only search.",
                 "請選擇目標類型與類別，以啟用唯讀搜尋。"),
            CompanionFinderUiTextKey.DisciplineSourceUnavailable =>
                ("Discipline names are unavailable", "無法取得類別名稱"),
            CompanionFinderUiTextKey.DisciplineSourceUnavailableMessage =>
                ("The installed bilingual language resources could not be read. Check the trusted game installation and retry.",
                 "無法讀取已安裝的雙語資源。請檢查受信任的遊戲安裝後重試。"),
            CompanionFinderUiTextKey.RetryLabels =>
                ("Retry labels", "重新讀取名稱"),
            CompanionFinderUiTextKey.Result =>
                ("Result", "結果"),
            CompanionFinderUiTextKey.PreviousResult =>
                ("Previous result", "上一次結果"),
            CompanionFinderUiTextKey.PreviousResultMessage =>
                ("Draft controls changed. This result remains visible for context but is inactive until a new search succeeds.",
                 "草稿選項已變更。此結果仍保留供參考，但在新搜尋成功前不再提供互動。"),
            CompanionFinderUiTextKey.SnapshotCurrent =>
                ("Snapshot current", "快照目前有效"),
            CompanionFinderUiTextKey.PartialResult =>
                ("Partial result", "部分結果"),
            CompanionFinderUiTextKey.SnapshotCaptured =>
                ("Snapshot captured", "快照時間"),
            CompanionFinderUiTextKey.ScoreHeading =>
                ("Score meaning", "分數意義"),
            CompanionFinderUiTextKey.ScoreLimitation =>
                ("Scores compare saved base qualification within this selected discipline only. They are not current attainment, success probability, or universal companion quality.",
                 "分數只比較所選類別的存檔基礎資質，並非目前造詣、成功機率或人物的整體價值。"),
            CompanionFinderUiTextKey.Considered =>
                ("Considered", "共計"),
            CompanionFinderUiTextKey.Eligible =>
                ("Eligible", "符合資格"),
            CompanionFinderUiTextKey.Ranked =>
                ("Ranked", "已排序"),
            CompanionFinderUiTextKey.Tied =>
                ("Tied", "並列"),
            CompanionFinderUiTextKey.NeedsReview =>
                ("Needs review", "需檢查"),
            CompanionFinderUiTextKey.Ineligible =>
                ("Ineligible", "不符合資格"),
            CompanionFinderUiTextKey.Incomplete =>
                ("Incomplete", "資料不完整"),
            CompanionFinderUiTextKey.Unsupported =>
                ("Unsupported", "目前不支援"),
            CompanionFinderUiTextKey.Conflicting =>
                ("Conflicting", "資料衝突"),
            CompanionFinderUiTextKey.Filters =>
                ("Filter this immutable result", "篩選此固定結果"),
            CompanionFinderUiTextKey.ShowCandidates =>
                ("Show candidates", "顯示人選"),
            CompanionFinderUiTextKey.All =>
                ("All", "全部"),
            CompanionFinderUiTextKey.FilterVisibleNames =>
                ("Filter visible names", "篩選顯示姓名"),
            CompanionFinderUiTextKey.Candidate =>
                ("Candidate", "人選"),
            CompanionFinderUiTextKey.SavedBaseQualification =>
                ("Saved base qualification", "存檔基礎資質"),
            CompanionFinderUiTextKey.State =>
                ("State", "狀態"),
            CompanionFinderUiTextKey.Evidence =>
                ("Evidence", "證據"),
            CompanionFinderUiTextKey.Location =>
                ("Location", "位置"),
            CompanionFinderUiTextKey.Compare =>
                ("Compare", "比較"),
            CompanionFinderUiTextKey.RankedCandidates =>
                ("Ranked candidates", "已排序人選"),
            CompanionFinderUiTextKey.NeedsReviewCandidates =>
                ("Candidates needing review", "需檢查的人選"),
            CompanionFinderUiTextKey.IneligibleCandidates =>
                ("Ineligible candidates", "不符合資格的人選"),
            CompanionFinderUiTextKey.NoVisibleCandidates =>
                ("No candidates match the current display filter. The unfiltered result is unchanged.",
                 "目前顯示篩選沒有相符人選；未篩選的結果維持不變。"),
            CompanionFinderUiTextKey.Unavailable =>
                ("Unavailable", "無法取得"),
            CompanionFinderUiTextKey.UnnamedCandidate =>
                ("Unnamed candidate", "未命名同道"),
            CompanionFinderUiTextKey.LocationUnavailable =>
                ("Verified location unavailable", "無法取得已驗證位置"),
            CompanionFinderUiTextKey.SelectForComparison =>
                ("Select for comparison", "選取作比較"),
            CompanionFinderUiTextKey.SelectedForComparison =>
                ("Selected for comparison", "已選取作比較"),
            CompanionFinderUiTextKey.ComparisonLimit =>
                ("Two candidates are selected. Clear or unselect one before choosing another.",
                 "已選取兩位人選；請先清除或取消其中一位，再選擇其他人選。"),
            CompanionFinderUiTextKey.ComparisonReady =>
                ("Comparison ready", "比較已就緒"),
            CompanionFinderUiTextKey.Comparison =>
                ("Candidate comparison", "人選比較"),
            CompanionFinderUiTextKey.ClearComparison =>
                ("Clear comparison", "清除比較"),
            CompanionFinderUiTextKey.EvaluationState =>
                ("Evaluation state", "評估狀態"),
            CompanionFinderUiTextKey.HardGates =>
                ("Hard gates", "必要條件"),
            CompanionFinderUiTextKey.RoleLocalScore =>
                ("Role-local score", "角色限定分數"),
            CompanionFinderUiTextKey.CompetitionRank =>
                ("Competition rank", "共同名次"),
            CompanionFinderUiTextKey.RelativeResult =>
                ("Relative result", "相對結果"),
            CompanionFinderUiTextKey.ReviewManually =>
                ("Review the evidence and make any choice manually in the game.",
                 "請檢查證據，並自行在遊戲中作出選擇。"),
            CompanionFinderUiTextKey.EvidenceAndScope =>
                ("Evidence and scope limitations", "證據與範圍限制"),
            CompanionFinderUiTextKey.EvidenceAndScopeMessage =>
                ("Unknown values remain unavailable. Names and locations are display context only; they never change eligibility, score, rank, or tie state.",
                 "未知數值會維持無法取得。姓名與位置只供顯示，不會改變資格、分數、名次或並列狀態。"),
            CompanionFinderUiTextKey.ReadingTitle =>
                ("Reading one stable snapshot", "正在讀取單一穩定快照"),
            CompanionFinderUiTextKey.ReadingMessage =>
                ("The configured save is being read without changing it. Old and new results are not mixed.",
                 "正在唯讀存取已設定的存檔；舊結果與新結果不會混合。"),
            CompanionFinderUiTextKey.CancelledTitle =>
                ("Read cancelled", "讀取已取消"),
            CompanionFinderUiTextKey.CancelledMessage =>
                ("No new result was installed. Choose the objective and retry when ready.",
                 "未載入任何新結果。請確認目標後再重試。"),
            CompanionFinderUiTextKey.RetryRead =>
                ("Retry read", "重新讀取"),
            CompanionFinderUiTextKey.SaveUnavailableTitle =>
                ("Configured save unavailable", "無法取得已設定的存檔"),
            CompanionFinderUiTextKey.SaveUnavailableMessage =>
                ("Configure one trusted absolute .sav path, restart TaiWu Helper, and retry.",
                 "請設定一個受信任的 .sav 絕對路徑，重新啟動太吾助手後再試。"),
            CompanionFinderUiTextKey.UnsupportedSourceTitle =>
                ("Unsupported game version", "不支援的遊戲版本"),
            CompanionFinderUiTextKey.UnsupportedSourceMessage =>
                ("The installed source is outside the verified companion mapping. No estimate was produced.",
                 "已安裝的來源不在已驗證同道對應範圍內，因此未產生估算。"),
            CompanionFinderUiTextKey.ChangedRevisionTitle =>
                ("Save changed during the read", "讀取期間存檔已變更"),
            CompanionFinderUiTextKey.ChangedRevisionMessage =>
                ("The mixed revision was discarded. Wait for the save to stabilize, then retry.",
                 "混合版本的資料已捨棄。請等待存檔穩定後重試。"),
            CompanionFinderUiTextKey.ReadFailedTitle =>
                ("Could not complete the candidate read", "無法完成人選讀取"),
            CompanionFinderUiTextKey.ReadFailedMessage =>
                ("No unsafe fallback or old result was installed. Check the trusted configuration and retry.",
                 "未載入不安全的替代值或舊結果。請檢查受信任的設定後重試。"),
            CompanionFinderUiTextKey.EmptyTitle =>
                ("No current-group candidates", "目前隊伍沒有人選"),
            CompanionFinderUiTextKey.EmptyMessage =>
                ("This stable snapshot contained no non-Taiwu current-group profiles to evaluate.",
                 "此穩定快照中沒有可評估的非太吾隊伍成員。"),
            CompanionFinderUiTextKey.PartialMessage =>
                ("Some candidate fields could not be read from this stable snapshot. Ranked facts remain exact; affected candidates stay visibly unranked.",
                 "此穩定快照中的部分人選欄位無法讀取。已排名數值仍為精確值；受影響的人選會明確維持未排名。"),
            CompanionFinderUiTextKey.ConfirmedEvidence =>
                ("Saved base value confirmed", "已確認存檔基礎值"),
            CompanionFinderUiTextKey.MissingEvidence =>
                ("Required evidence missing", "缺少必要證據"),
            CompanionFinderUiTextKey.IncompleteEvidence =>
                ("Evidence incomplete", "證據不完整"),
            CompanionFinderUiTextKey.UnsupportedEvidence =>
                ("Evidence unsupported", "證據目前不支援"),
            CompanionFinderUiTextKey.StaleEvidence =>
                ("Evidence no longer current", "證據已非最新"),
            CompanionFinderUiTextKey.ConflictingEvidence =>
                ("Evidence conflicts", "證據互相衝突"),
            CompanionFinderUiTextKey.Rank =>
                ("Rank", "第"),
            CompanionFinderUiTextKey.NotRanked =>
                ("Not ranked", "未排名"),
            CompanionFinderUiTextKey.ResultSummary =>
                ("Result summary", "結果摘要"),
            CompanionFinderUiTextKey.DecisiveStrengths =>
                ("Decisive strengths", "主要優勢"),
            CompanionFinderUiTextKey.MaterialLimitations =>
                ("Material limitations", "重要限制"),
            CompanionFinderUiTextKey.RequirementEvidence =>
                ("Requirement evidence", "條件證據"),
            CompanionFinderUiTextKey.EnrichmentCurrentTitle =>
                ("Catalogue evidence current", "目錄證據目前有效"),
            CompanionFinderUiTextKey.EnrichmentCurrentMessage =>
                ("The installed catalogue and candidate evidence match this stable snapshot.",
                 "已安裝目錄與人選證據皆符合此穩定快照。"),
            CompanionFinderUiTextKey.CandidateEvidencePartialTitle =>
                ("Some candidate evidence is incomplete", "部分人選證據不完整"),
            CompanionFinderUiTextKey.CandidateEvidencePartialMessage =>
                ("Review candidates marked as needing review. Unavailable evidence was not treated as a negative or a zero.",
                 "請檢查標示為需檢查的人選；無法取得的證據不會被當作否定或零值。"),
            CompanionFinderUiTextKey.CatalogueMissingTitle =>
                ("Local catalogue missing", "缺少本機目錄"),
            CompanionFinderUiTextKey.CatalogueMissingMessage =>
                ("Rebuild the local catalogue from the trusted installed game sources, then run the search again.",
                 "請從受信任的已安裝遊戲來源重建本機目錄，再重新搜尋。"),
            CompanionFinderUiTextKey.CatalogueSourcesMissingTitle =>
                ("Installed catalogue sources missing", "缺少已安裝的目錄來源"),
            CompanionFinderUiTextKey.CatalogueSourcesMissingMessage =>
                ("Check that the supported game installation and bilingual source files are available, then retry.",
                 "請確認支援的遊戲安裝與雙語來源檔案皆可取得，再重試。"),
            CompanionFinderUiTextKey.CatalogueStaleTitle =>
                ("Local catalogue is stale", "本機目錄已過期"),
            CompanionFinderUiTextKey.CatalogueStaleMessage =>
                ("The installed sources no longer match the local catalogue. Refresh the catalogue and run the search again.",
                 "已安裝來源已不符合本機目錄；請更新目錄後重新搜尋。"),
            CompanionFinderUiTextKey.CatalogueRebuildingTitle =>
                ("Catalogue rebuild in progress", "目錄正在重建"),
            CompanionFinderUiTextKey.CatalogueRebuildingMessage =>
                ("Wait for the catalogue rebuild to finish, then retry this search.",
                 "請等待目錄重建完成，再重試此搜尋。"),
            CompanionFinderUiTextKey.CatalogueUnsupportedTitle =>
                ("Catalogue version unsupported", "不支援此目錄版本"),
            CompanionFinderUiTextKey.CatalogueUnsupportedMessage =>
                ("The installed game data is outside the verified catalogue mapping. Use a supported source version before retrying.",
                 "已安裝的遊戲資料不在已驗證目錄對應範圍內；請改用支援的來源版本後重試。"),
            CompanionFinderUiTextKey.CatalogueSourceReadFailedTitle =>
                ("Could not read catalogue sources", "無法讀取目錄來源"),
            CompanionFinderUiTextKey.CatalogueSourceReadFailedMessage =>
                ("Check access to the trusted installed game sources, then retry without using the incomplete catalogue evidence.",
                 "請檢查受信任遊戲來源的存取狀態，再重試；不要使用不完整的目錄證據。"),
            CompanionFinderUiTextKey.CatalogueRepositoryFailedTitle =>
                ("Local catalogue unavailable", "本機目錄無法使用"),
            CompanionFinderUiTextKey.CatalogueRepositoryFailedMessage =>
                ("Restart the helper and retry. If the state remains, rebuild the local catalogue from trusted installed sources.",
                 "請重新啟動助手後重試；若狀態持續，請從受信任的已安裝來源重建本機目錄。"),
            CompanionFinderUiTextKey.CatalogueCorruptTitle =>
                ("Local catalogue is corrupt", "本機目錄已損壞"),
            CompanionFinderUiTextKey.CatalogueCorruptMessage =>
                ("Rebuild the local catalogue from trusted installed sources before using catalogue-dependent evidence.",
                 "使用依賴目錄的證據前，請先從受信任的已安裝來源重建本機目錄。"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(key),
                key,
                "Unknown companion-finder UI text identity.")
        };

        return language switch
        {
            TaiwuLanguage.English => english,
            TaiwuLanguage.Chinese => chinese,
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown UI language.")
        };
    }

    public static string RoleLabel(
        TaiwuLanguage language,
        CandidateDisciplineDomain domain) => Get(
        language,
        domain switch
        {
            CandidateDisciplineDomain.Martial =>
                CompanionFinderUiTextKey.MartialRole,
            CandidateDisciplineDomain.LifeSkill =>
                CompanionFinderUiTextKey.LifeSkillRole,
            _ => throw new ArgumentOutOfRangeException(
                nameof(domain),
                domain,
                "Unknown discipline domain.")
        });

    public static string FilterLabel(
        TaiwuLanguage language,
        CompanionRoleShortlistFilter filter) => Get(
        language,
        filter switch
        {
            CompanionRoleShortlistFilter.All =>
                CompanionFinderUiTextKey.All,
            CompanionRoleShortlistFilter.Ranked =>
                CompanionFinderUiTextKey.Ranked,
            CompanionRoleShortlistFilter.NeedsReview =>
                CompanionFinderUiTextKey.NeedsReview,
            CompanionRoleShortlistFilter.Ineligible =>
                CompanionFinderUiTextKey.Ineligible,
            _ => throw new ArgumentOutOfRangeException(
                nameof(filter),
                filter,
                "Unknown shortlist filter.")
        });
}
