using TaiWu.Application.Localization;
using TaiWuAPI.Contracts.VillageWorkforce;

namespace TaiWuAPI.Localization;

public enum VillageWorkforceUiTextKey
{
    PageTitle,
    Eyebrow,
    HeroCopy,
    InformationOnly,
    InformationNotice,
    CandidateBoundary,
    Objective,
    ObjectiveLabel,
    ObjectiveDescription,
    AssignmentTarget,
    ChooseTarget,
    InspectPosition,
    InspectingPosition,
    TargetHelp,
    NoTargetsTitle,
    NoTargetsMessage,
    RetryTargets,
    Result,
    PreviousResult,
    PreviousResultMessage,
    SnapshotCurrent,
    SnapshotPartial,
    SnapshotCaptured,
    VerifiedRule,
    SharedScope,
    QualificationMeaning,
    NamesUnavailable,
    CurrentAssignment,
    CurrentWorker,
    AlternativeWorker,
    WorkerNameUnavailable,
    SavedBaseQualification,
    QualificationPoints,
    Unavailable,
    ResultSummary,
    Total,
    Comparable,
    NeedsReview,
    Ineligible,
    Ranked,
    Tied,
    Incomplete,
    Unsupported,
    Conflicting,
    CurrentOnly,
    Filters,
    ShowWorkers,
    All,
    FilterNames,
    VisibleWorkers,
    Rank,
    Worker,
    State,
    Compare,
    SelectForComparison,
    SelectedForComparison,
    ComparisonLimit,
    ComparisonReady,
    CandidateComparison,
    ClearComparison,
    FirstWorker,
    SecondWorker,
    RelativeResult,
    EvidenceDetails,
    RequirementsPassed,
    VerifiedComponents,
    Requirements,
    Components,
    Provenance,
    SourceConfiguredSave,
    SourceInstalledGameData,
    SourceDerivedRule,
    NoVisibleWorkers,
    ManualChecklist,
    NoActionSent,
    EvidenceAndScope,
    EvidenceAndScopeMessage,
    ReadingTitle,
    ReadingMessage,
    CancelledTitle,
    CancelledMessage,
    RetryRead,
    SaveUnavailableTitle,
    SaveUnavailableMessage,
    UnsupportedSourceTitle,
    UnsupportedSourceMessage,
    ConflictingSourcesTitle,
    ConflictingSourcesMessage,
    ChangedRevisionTitle,
    ChangedRevisionMessage,
    TargetMissingTitle,
    TargetMissingMessage,
    ReadFailedTitle,
    ReadFailedMessage,
    PartialMessage,
    ResultLimitation,
    LoadingTargets
}

public static class VillageWorkforceUiText
{
    public static string Get(
        TaiwuLanguage language,
        VillageWorkforceUiTextKey key)
    {
        var (english, chinese) = key switch
        {
            VillageWorkforceUiTextKey.PageTitle =>
                ("Village workforce planner", "村莊人力規劃"),
            VillageWorkforceUiTextKey.Eyebrow =>
                ("Village work · occupied shop position", "村莊工作 · 已佔用商鋪位置"),
            VillageWorkforceUiTextKey.HeroCopy =>
                ("Compare one current shop assignment with verified alternatives from one stable saved snapshot.",
                 "以單一穩定存檔快照，比較目前商鋪指派與有證據支持的替代人員。"),
            VillageWorkforceUiTextKey.InformationOnly =>
                ("Information only", "僅供參考"),
            VillageWorkforceUiTextKey.InformationNotice =>
                ("TaiWu Helper reads the configured save. It cannot assign workers, change buildings, collect resources, or control the game.",
                 "太吾助手只會讀取已設定的存檔；不能指派人員、改變建築、收集資源或控制遊戲。"),
            VillageWorkforceUiTextKey.CandidateBoundary =>
                ("Candidate boundary: verified alternatives for an occupied shop-manager position. Aptitude alone does not prove productivity.",
                 "人選範圍：已佔用商鋪管理位置的已驗證替代人員。資質本身不能證明生產力。"),
            VillageWorkforceUiTextKey.Objective =>
                ("Objective", "目標"),
            VillageWorkforceUiTextKey.ObjectiveLabel =>
                ("Shop manager base aptitude", "商鋪管理基礎資質"),
            VillageWorkforceUiTextKey.ObjectiveDescription =>
                ("Order candidates by the exact saved base life-skill qualification required by the selected shop.",
                 "依所選商鋪需要的存檔基礎技藝資質精確值排列人選。"),
            VillageWorkforceUiTextKey.AssignmentTarget =>
                ("Shop manager position", "商鋪管理位置"),
            VillageWorkforceUiTextKey.ChooseTarget =>
                ("Choose a position", "選擇位置"),
            VillageWorkforceUiTextKey.InspectPosition =>
                ("Inspect position", "檢查位置"),
            VillageWorkforceUiTextKey.InspectingPosition =>
                ("Inspecting position…", "正在檢查位置……"),
            VillageWorkforceUiTextKey.TargetHelp =>
                ("Select a supported occupied position, then inspect it explicitly.",
                 "請選擇受支援的已佔用位置，再明確執行檢查。"),
            VillageWorkforceUiTextKey.NoTargetsTitle =>
                ("No supported occupied shop position", "沒有受支援的已佔用商鋪位置"),
            VillageWorkforceUiTextKey.NoTargetsMessage =>
                ("The current stable save has no occupied shop-manager position covered by this verified rule.",
                 "目前穩定存檔沒有此已驗證規則涵蓋的已佔用商鋪管理位置。"),
            VillageWorkforceUiTextKey.RetryTargets =>
                ("Reload positions", "重新讀取位置"),
            VillageWorkforceUiTextKey.Result =>
                ("Workforce result", "人力結果"),
            VillageWorkforceUiTextKey.PreviousResult =>
                ("Previous result", "上一次結果"),
            VillageWorkforceUiTextKey.PreviousResultMessage =>
                ("The draft position changed. Inspect it to replace this inert previous result.",
                 "草擬位置已變更。請檢查新位置，以取代這項停用的上一次結果。"),
            VillageWorkforceUiTextKey.SnapshotCurrent =>
                ("Current stable snapshot", "目前穩定快照"),
            VillageWorkforceUiTextKey.SnapshotPartial =>
                ("Partial snapshot", "部分快照"),
            VillageWorkforceUiTextKey.SnapshotCaptured =>
                ("Snapshot captured", "快照擷取時間"),
            VillageWorkforceUiTextKey.VerifiedRule =>
                ("Verified rule", "已驗證規則"),
            VillageWorkforceUiTextKey.SharedScope =>
                ("This result applies only to the selected assignment and verified rule version. It is not universal character quality, future potential, or a game action.",
                 "此結果只適用於所選指派與已驗證規則版本，並非人物的整體價值、未來潛力或遊戲操作。"),
            VillageWorkforceUiTextKey.QualificationMeaning =>
                ("Saved base life-skill qualification is the only ordering component. It is not current attainment, efficiency, output, revenue, or a percentage.",
                 "存檔基礎技藝資質是唯一排序項目；不代表目前造詣、效率、產出、收益或百分比。"),
            VillageWorkforceUiTextKey.NamesUnavailable =>
                ("Verified worker and building names are unavailable at this source boundary, so localized ordinal labels are used and raw IDs stay hidden.",
                 "此資料來源邊界沒有已驗證的人員與建築名稱，因此使用本地化順序標籤並隱藏原始 ID。"),
            VillageWorkforceUiTextKey.CurrentAssignment =>
                ("Current assignment", "目前指派"),
            VillageWorkforceUiTextKey.CurrentWorker =>
                ("Current worker", "目前人員"),
            VillageWorkforceUiTextKey.AlternativeWorker =>
                ("Alternative worker", "替代人員"),
            VillageWorkforceUiTextKey.WorkerNameUnavailable =>
                ("name unavailable", "名稱無法取得"),
            VillageWorkforceUiTextKey.SavedBaseQualification =>
                ("Saved base life-skill qualification", "存檔基礎技藝資質"),
            VillageWorkforceUiTextKey.QualificationPoints =>
                ("qualification points", "資質點數"),
            VillageWorkforceUiTextKey.Unavailable =>
                ("Unavailable", "無法取得"),
            VillageWorkforceUiTextKey.ResultSummary =>
                ("Result summary", "結果摘要"),
            VillageWorkforceUiTextKey.Total =>
                ("Total", "共計"),
            VillageWorkforceUiTextKey.Comparable =>
                ("Comparable", "可比較"),
            VillageWorkforceUiTextKey.NeedsReview =>
                ("Needs review", "需檢查"),
            VillageWorkforceUiTextKey.Ineligible =>
                ("Ineligible", "不符合資格"),
            VillageWorkforceUiTextKey.Ranked =>
                ("Ranked", "已排序"),
            VillageWorkforceUiTextKey.Tied =>
                ("Tied", "並列"),
            VillageWorkforceUiTextKey.Incomplete =>
                ("Incomplete", "資料不完整"),
            VillageWorkforceUiTextKey.Unsupported =>
                ("Unsupported", "目前不支援"),
            VillageWorkforceUiTextKey.Conflicting =>
                ("Conflicting", "資料衝突"),
            VillageWorkforceUiTextKey.CurrentOnly =>
                ("Current assignment only", "僅屬目前指派"),
            VillageWorkforceUiTextKey.Filters =>
                ("Display filters", "顯示篩選"),
            VillageWorkforceUiTextKey.ShowWorkers =>
                ("Show workers", "顯示人員"),
            VillageWorkforceUiTextKey.All =>
                ("All", "全部"),
            VillageWorkforceUiTextKey.FilterNames =>
                ("Filter displayed labels", "篩選顯示標籤"),
            VillageWorkforceUiTextKey.VisibleWorkers =>
                ("workers visible", "位人員顯示"),
            VillageWorkforceUiTextKey.Rank =>
                ("Rank", "名次"),
            VillageWorkforceUiTextKey.Worker =>
                ("Worker", "人員"),
            VillageWorkforceUiTextKey.State =>
                ("State", "狀態"),
            VillageWorkforceUiTextKey.Compare =>
                ("Compare", "比較"),
            VillageWorkforceUiTextKey.SelectForComparison =>
                ("Select for comparison", "選取作比較"),
            VillageWorkforceUiTextKey.SelectedForComparison =>
                ("Selected for comparison", "已選取作比較"),
            VillageWorkforceUiTextKey.ComparisonLimit =>
                ("Two workers are already selected. Clear or change a selection to compare another worker.",
                 "已選取兩位人員。請清除或變更選取項目，再比較其他人員。"),
            VillageWorkforceUiTextKey.ComparisonReady =>
                ("Comparison ready", "比較已就緒"),
            VillageWorkforceUiTextKey.CandidateComparison =>
                ("Worker comparison", "人員比較"),
            VillageWorkforceUiTextKey.ClearComparison =>
                ("Clear comparison", "清除比較"),
            VillageWorkforceUiTextKey.FirstWorker =>
                ("First worker", "第一位人員"),
            VillageWorkforceUiTextKey.SecondWorker =>
                ("Second worker", "第二位人員"),
            VillageWorkforceUiTextKey.RelativeResult =>
                ("Relative result", "相對結果"),
            VillageWorkforceUiTextKey.EvidenceDetails =>
                ("Worker-specific evidence", "人員專屬證據"),
            VillageWorkforceUiTextKey.RequirementsPassed =>
                ("requirements passed", "項條件通過"),
            VillageWorkforceUiTextKey.VerifiedComponents =>
                ("verified components", "項已驗證組成"),
            VillageWorkforceUiTextKey.Requirements =>
                ("Requirements", "條件"),
            VillageWorkforceUiTextKey.Components =>
                ("Components", "組成"),
            VillageWorkforceUiTextKey.Provenance =>
                ("Provenance", "來源"),
            VillageWorkforceUiTextKey.SourceConfiguredSave =>
                ("Configured save", "已設定存檔"),
            VillageWorkforceUiTextKey.SourceInstalledGameData =>
                ("Installed game data", "已安裝遊戲資料"),
            VillageWorkforceUiTextKey.SourceDerivedRule =>
                ("Verified derived rule", "已驗證推導規則"),
            VillageWorkforceUiTextKey.NoVisibleWorkers =>
                ("No workers match these display filters.", "沒有任何人員符合這些顯示篩選。"),
            VillageWorkforceUiTextKey.ManualChecklist =>
                ("Manual checklist", "手動檢查清單"),
            VillageWorkforceUiTextKey.NoActionSent =>
                ("No action was sent to the game.", "沒有向遊戲送出任何操作。"),
            VillageWorkforceUiTextKey.EvidenceAndScope =>
                ("Evidence, scope, and deferred mechanics", "證據、範圍與延後機制"),
            VillageWorkforceUiTextKey.EvidenceAndScopeMessage =>
                ("Settlement optimization, vacancies, recruitment, construction, resource routing, persistence, and game control remain outside this result.",
                 "聚落最佳化、空缺、招募、建造、資源路線、保存結果與遊戲控制均不在本結果範圍內。"),
            VillageWorkforceUiTextKey.ReadingTitle =>
                ("Reading one stable workforce snapshot", "正在讀取單一穩定人力快照"),
            VillageWorkforceUiTextKey.ReadingMessage =>
                ("The active result will change only after this read finishes safely.",
                 "只有在此次讀取安全完成後，才會更換目前結果。"),
            VillageWorkforceUiTextKey.CancelledTitle =>
                ("Inspection cancelled", "檢查已取消"),
            VillageWorkforceUiTextKey.CancelledMessage =>
                ("No workforce result was produced. You can inspect the position again.",
                 "沒有產生人力結果；你可以再次檢查此位置。"),
            VillageWorkforceUiTextKey.RetryRead =>
                ("Retry inspection", "重試檢查"),
            VillageWorkforceUiTextKey.SaveUnavailableTitle =>
                ("Configured save unavailable", "無法讀取已設定存檔"),
            VillageWorkforceUiTextKey.SaveUnavailableMessage =>
                ("Check the trusted save configuration, then reload positions or retry.",
                 "請檢查受信任的存檔設定，再重新讀取位置或重試。"),
            VillageWorkforceUiTextKey.UnsupportedSourceTitle =>
                ("Source version unsupported", "資料版本目前不支援"),
            VillageWorkforceUiTextKey.UnsupportedSourceMessage =>
                ("No estimate was produced because the installed source is outside the verified rule boundary.",
                 "已安裝資料超出已驗證規則邊界，因此沒有產生估算。"),
            VillageWorkforceUiTextKey.ConflictingSourcesTitle =>
                ("Workforce sources conflict", "人力資料來源互相衝突"),
            VillageWorkforceUiTextKey.ConflictingSourcesMessage =>
                ("Review the source installation and stable save before retrying.",
                 "請檢查資料來源安裝與穩定存檔後再重試。"),
            VillageWorkforceUiTextKey.ChangedRevisionTitle =>
                ("Save changed during inspection", "檢查期間存檔已變更"),
            VillageWorkforceUiTextKey.ChangedRevisionMessage =>
                ("The mixed result was discarded. Wait for the save to stabilize, then retry.",
                 "混合版本的結果已捨棄。請等待存檔穩定後重試。"),
            VillageWorkforceUiTextKey.TargetMissingTitle =>
                ("Selected position no longer exists", "所選位置已不存在"),
            VillageWorkforceUiTextKey.TargetMissingMessage =>
                ("Reload the current positions and select another supported target.",
                 "請重新讀取目前位置，再選擇另一個受支援目標。"),
            VillageWorkforceUiTextKey.ReadFailedTitle =>
                ("Workforce inspection failed safely", "人力檢查已安全失敗"),
            VillageWorkforceUiTextKey.ReadFailedMessage =>
                ("No old or partial result was relabelled. Correct the configuration or retry.",
                 "舊結果或部分結果均未被重新標示。請修正設定或重試。"),
            VillageWorkforceUiTextKey.PartialMessage =>
                ("Some workers require review because required evidence is incomplete, unsupported, or conflicting.",
                 "部分人員因必要證據不完整、不受支援或互相衝突而需要檢查。"),
            VillageWorkforceUiTextKey.ResultLimitation =>
                ("Shared result limitations", "共用結果限制"),
            VillageWorkforceUiTextKey.LoadingTargets =>
                ("Loading supported positions…", "正在讀取受支援位置……"),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
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

    public static string TargetLabel(TaiwuLanguage language, int ordinal) =>
        language switch
        {
            TaiwuLanguage.English => $"Shop manager position {ordinal}",
            TaiwuLanguage.Chinese => $"商鋪管理位置 {ordinal}",
            _ => throw new ArgumentOutOfRangeException(nameof(language))
        };

    public static string WorkerLabel(
        TaiwuLanguage language,
        int ordinal,
        bool isCurrent) =>
        $"{Get(language, isCurrent
            ? VillageWorkforceUiTextKey.CurrentWorker
            : VillageWorkforceUiTextKey.AlternativeWorker)} {ordinal} · "
        + Get(language, VillageWorkforceUiTextKey.WorkerNameUnavailable);

    public static string VisibleCount(
        TaiwuLanguage language,
        int visible,
        int total) => language switch
        {
            TaiwuLanguage.English => $"{visible} of {total} workers visible",
            TaiwuLanguage.Chinese => $"共 {total} 位人員，顯示 {visible} 位",
            _ => throw new ArgumentOutOfRangeException(nameof(language))
        };

    public static string Source(
        TaiwuLanguage language,
        VillageWorkforceApiEvidenceSource source) => Get(language, source switch
        {
            VillageWorkforceApiEvidenceSource.ConfiguredSave =>
                VillageWorkforceUiTextKey.SourceConfiguredSave,
            VillageWorkforceApiEvidenceSource.InstalledGameData =>
                VillageWorkforceUiTextKey.SourceInstalledGameData,
            VillageWorkforceApiEvidenceSource.DerivedRule =>
                VillageWorkforceUiTextKey.SourceDerivedRule,
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        });
}
