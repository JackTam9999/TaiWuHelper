using TaiWu.Application.Localization;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using TaiWuAPI.Presentation;

namespace TaiWuAPI.Localization;

public enum TacticalCombatUiTextKey
{
    TacticalPlan,
    ExactTarget,
    InformationOnlyNotice,
    PreviousResult,
    Loading,
    Cancelled,
    ObservationReplaced,
    Failure,
    PlanAvailable,
    PartialEvidence,
    UnsupportedRules,
    NoCandidate,
    SearchTruncated,
    SourceFailure,
    CalculationFailure,
    EvidenceFreshness,
    SaveRead,
    Observation,
    NoObservation,
    FinishStatus,
    SearchComplete,
    SearchBounded,
    ResultWithinBounds,
    NeedsConfirmation,
    CriticalConditions,
    NoCriticalConditions,
    SearchAndSelection,
    Considered,
    Admitted,
    Rejected,
    Unsupported,
    Irrelevant,
    Dominated,
    Explored,
    Feasible,
    Retained,
    LimitingBound,
    WhyThisPlan,
    CandidateConsideration,
    DetailedEvidence,
    Condition,
    ConditionState,
    ManualAction,
    ExpectedPurpose,
    Limitation,
    StepEvidence,
    Requirements,
    EvidenceSources,
    NotIncluded,
    BaseWeight,
    AppliedWeight,
    NormalizedValue,
    Contribution,
    ShowMore,
    Showing,
    Of,
    NoActionSent,
    RuleVersion,
    ResultIdentity,
    Unavailable
}

public static class TacticalCombatUiText
{
    public static string Get(
        TaiwuLanguage language,
        TacticalCombatUiTextKey key)
    {
        var value = key switch
        {
            TacticalCombatUiTextKey.TacticalPlan =>
                ("Tactical plan", "戰術計畫"),
            TacticalCombatUiTextKey.ExactTarget =>
                ("Exact target", "精確目標"),
            TacticalCombatUiTextKey.InformationOnlyNotice =>
                ("Information only — carry out every action manually in the game.",
                    "僅供參考，請自行在遊戲中完成每項操作。"),
            TacticalCombatUiTextKey.PreviousResult =>
                ("Previous result — the controls or evidence have changed.",
                    "先前結果 — 控制項或證據已變更。"),
            TacticalCombatUiTextKey.Loading =>
                ("Calculating a complete replacement result…",
                    "正在計算完整的替代結果……"),
            TacticalCombatUiTextKey.Cancelled =>
                ("Tactical calculation was cancelled. No mixed partial plan is active.",
                    "戰術計算已取消；目前沒有混合的部分計畫。"),
            TacticalCombatUiTextKey.ObservationReplaced =>
                ("The target observation replaced the prior result. No tactical plan is active because that observation cannot be converted into verified tactical-rule evidence.",
                    "目標觀察已取代先前結果。該觀察不能轉換成已驗證的戰術規則證據，因此目前沒有啟用戰術計畫。"),
            TacticalCombatUiTextKey.Failure =>
                ("The tactical result could not be calculated. The previous result, if shown, is inactive.",
                    "無法計算戰術結果；若仍顯示先前結果，該結果並未啟用。"),
            TacticalCombatUiTextKey.PlanAvailable =>
                ("Plan available", "計畫可用"),
            TacticalCombatUiTextKey.PartialEvidence =>
                ("Partial evidence", "部分證據"),
            TacticalCombatUiTextKey.UnsupportedRules =>
                ("Tactical rules unsupported", "不支援此戰術規則"),
            TacticalCombatUiTextKey.NoCandidate =>
                ("No tactical candidate", "沒有戰術候選方案"),
            TacticalCombatUiTextKey.SearchTruncated =>
                ("Search bounded", "搜尋受限"),
            TacticalCombatUiTextKey.SourceFailure =>
                ("Tactical source unavailable", "無法取得戰術來源"),
            TacticalCombatUiTextKey.CalculationFailure =>
                ("Tactical calculation unavailable", "無法進行戰術計算"),
            TacticalCombatUiTextKey.EvidenceFreshness =>
                ("Evidence freshness", "證據新鮮度"),
            TacticalCombatUiTextKey.SaveRead =>
                ("Save read", "存檔讀取"),
            TacticalCombatUiTextKey.Observation =>
                ("Latest observation", "最新觀察"),
            TacticalCombatUiTextKey.NoObservation =>
                ("No later observation", "沒有較新的觀察"),
            TacticalCombatUiTextKey.FinishStatus =>
                ("Finish status", "收尾狀態"),
            TacticalCombatUiTextKey.SearchComplete =>
                ("Search complete", "搜尋完整"),
            TacticalCombatUiTextKey.SearchBounded =>
                ("Search bounded", "搜尋受限"),
            TacticalCombatUiTextKey.ResultWithinBounds =>
                ("Highest-ranked result found within the stated bounds",
                    "在所述限制內找到的最高排名結果"),
            TacticalCombatUiTextKey.NeedsConfirmation =>
                ("Needs confirmation", "需要確認"),
            TacticalCombatUiTextKey.CriticalConditions =>
                ("Needs confirmation", "需要確認"),
            TacticalCombatUiTextKey.NoCriticalConditions =>
                ("No critical unresolved condition is present in this result.",
                    "此結果沒有關鍵的未解決條件。"),
            TacticalCombatUiTextKey.SearchAndSelection =>
                ("Search and selection", "搜尋與選擇"),
            TacticalCombatUiTextKey.Considered =>
                ("Considered", "已檢視"),
            TacticalCombatUiTextKey.Admitted =>
                ("Admitted", "已納入"),
            TacticalCombatUiTextKey.Rejected =>
                ("Rejected", "已排除"),
            TacticalCombatUiTextKey.Unsupported =>
                ("Unsupported", "不支援"),
            TacticalCombatUiTextKey.Irrelevant =>
                ("Irrelevant", "不相關"),
            TacticalCombatUiTextKey.Dominated =>
                ("Dominated", "已被優勢方案取代"),
            TacticalCombatUiTextKey.Explored =>
                ("Explored", "已探索"),
            TacticalCombatUiTextKey.Feasible =>
                ("Feasible", "可行"),
            TacticalCombatUiTextKey.Retained =>
                ("Retained", "已保留"),
            TacticalCombatUiTextKey.LimitingBound =>
                ("Limiting bound", "限制界線"),
            TacticalCombatUiTextKey.WhyThisPlan =>
                ("Why this plan", "為何選擇此計畫"),
            TacticalCombatUiTextKey.CandidateConsideration =>
                ("Candidate consideration", "候選檢視"),
            TacticalCombatUiTextKey.DetailedEvidence =>
                ("Detailed evidence", "詳細證據"),
            TacticalCombatUiTextKey.Condition =>
                ("When / condition", "條件／時機"),
            TacticalCombatUiTextKey.ConditionState =>
                ("Condition state", "條件狀態"),
            TacticalCombatUiTextKey.ManualAction =>
                ("Do manually", "手動操作"),
            TacticalCombatUiTextKey.ExpectedPurpose =>
                ("Expected verified purpose", "已驗證預期用途"),
            TacticalCombatUiTextKey.Limitation =>
                ("Limitation", "限制"),
            TacticalCombatUiTextKey.StepEvidence =>
                ("Review step evidence", "查看步驟證據"),
            TacticalCombatUiTextKey.Requirements =>
                ("Requirements", "需求"),
            TacticalCombatUiTextKey.EvidenceSources =>
                ("Evidence sources", "證據來源"),
            TacticalCombatUiTextKey.NotIncluded =>
                ("Not included in this result", "未納入此結果"),
            TacticalCombatUiTextKey.BaseWeight =>
                ("Base weight", "基礎權重"),
            TacticalCombatUiTextKey.AppliedWeight =>
                ("Applied weight", "套用權重"),
            TacticalCombatUiTextKey.NormalizedValue =>
                ("Normalized value", "正規化數值"),
            TacticalCombatUiTextKey.Contribution =>
                ("Contribution", "貢獻"),
            TacticalCombatUiTextKey.ShowMore =>
                ("Show more", "顯示更多"),
            TacticalCombatUiTextKey.Showing =>
                ("Showing", "顯示"),
            TacticalCombatUiTextKey.Of =>
                ("of", "共"),
            TacticalCombatUiTextKey.NoActionSent =>
                ("No action was sent to the game.", "未向遊戲傳送任何操作。"),
            TacticalCombatUiTextKey.RuleVersion =>
                ("Rule version", "規則版本"),
            TacticalCombatUiTextKey.ResultIdentity =>
                ("Result identity", "結果識別"),
            TacticalCombatUiTextKey.Unavailable =>
                ("Unavailable", "無法取得"),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
        return language == TaiwuLanguage.Chinese ? value.Item2 : value.Item1;
    }

    public static string Stage(TaiwuLanguage language, TacticalPlanStage stage)
        => Pick(language, stage switch
        {
            TacticalPlanStage.Preparation => ("Preparation", "戰前準備"),
            TacticalPlanStage.Opening => ("Opening", "開場"),
            TacticalPlanStage.TargetStateResponse =>
                ("Target-state response", "目標狀態應對"),
            TacticalPlanStage.Recovery => ("Recovery", "恢復"),
            TacticalPlanStage.Finish => ("Finish", "收尾"),
            TacticalPlanStage.Fallback => ("Fallback", "後備方案"),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        });

    public static string Status(
        TaiwuLanguage language,
        TacticalCombatRecommendationStatus status) => Get(language,
            status switch
            {
                TacticalCombatRecommendationStatus.Success =>
                    TacticalCombatUiTextKey.PlanAvailable,
                TacticalCombatRecommendationStatus.PartialEvidence =>
                    TacticalCombatUiTextKey.PartialEvidence,
                TacticalCombatRecommendationStatus.UnsupportedChain =>
                    TacticalCombatUiTextKey.UnsupportedRules,
                TacticalCombatRecommendationStatus.NoCandidate =>
                    TacticalCombatUiTextKey.NoCandidate,
                TacticalCombatRecommendationStatus.SearchTruncated =>
                    TacticalCombatUiTextKey.SearchTruncated,
                TacticalCombatRecommendationStatus.SourceFailure =>
                    TacticalCombatUiTextKey.SourceFailure,
                _ => TacticalCombatUiTextKey.CalculationFailure
            });

    public static string Policy(
        TaiwuLanguage language,
        RecommendationPolicy policy) => Pick(language, policy switch
        {
            RecommendationPolicy.Safe => ("Safe", "穩健"),
            RecommendationPolicy.Balanced => ("Balanced", "均衡"),
            RecommendationPolicy.Aggressive => ("Aggressive", "進取"),
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
        });

    public static string Finish(
        TaiwuLanguage language,
        TacticalFinishDisposition? disposition) => Pick(language,
            disposition switch
            {
                TacticalFinishDisposition.Supported =>
                    ("Supported finish", "支援收尾"),
                TacticalFinishDisposition.FallbackOnly =>
                    ("Fallback only", "僅有後備方案"),
                TacticalFinishDisposition.Unsupported =>
                    ("Finish evidence unavailable", "缺少收尾證據"),
                null => ("Unavailable", "無法取得"),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(disposition), disposition, null)
            });

    public static string Condition(
        TaiwuLanguage language,
        TacticalConditionPresentationState state) => Pick(language,
            state switch
            {
                TacticalConditionPresentationState.Confirmed =>
                    ("Confirmed", "已確認"),
                TacticalConditionPresentationState.NeedsConfirmation =>
                    ("Needs confirmation", "需要確認"),
                TacticalConditionPresentationState.Unsupported =>
                    ("Unsupported", "不支援"),
                TacticalConditionPresentationState.Conflicting =>
                    ("Conflicting", "資料衝突"),
                TacticalConditionPresentationState.Unsatisfied =>
                    ("Unsatisfied", "未符合"),
                TacticalConditionPresentationState.Fallback =>
                    ("Fallback", "後備方案"),
                TacticalConditionPresentationState.Unresolved =>
                    ("Unresolved", "未解決"),
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            });

    public static string CandidateGroup(
        TaiwuLanguage language,
        TacticalCandidatePresentationGroup group) => Pick(language,
            group switch
            {
                TacticalCandidatePresentationGroup.Selected =>
                    ("Selected options", "所選方案"),
                TacticalCandidatePresentationGroup.AdmittedAlternative =>
                    ("Admitted alternatives", "已納入的替代方案"),
                TacticalCandidatePresentationGroup.Rejected =>
                    ("Rejected by feasibility", "因可行性而排除"),
                TacticalCandidatePresentationGroup.Unsupported =>
                    ("Unsupported role or effect", "不支援的角色或效果"),
                TacticalCandidatePresentationGroup.Irrelevant =>
                    ("Irrelevant to this chain", "與此因果鏈不相關"),
                TacticalCandidatePresentationGroup.Dominated =>
                    ("Dominated in this context", "在此情境由優勢方案取代"),
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
            });

    public static string Score(
        TaiwuLanguage language,
        TacticalScoreComponentKind kind) => Pick(language, kind switch
        {
            TacticalScoreComponentKind.CausalValue =>
                ("Causal-chain contribution", "因果鏈貢獻"),
            TacticalScoreComponentKind.LayeredProtection =>
                ("Layered protection", "分層保護"),
            TacticalScoreComponentKind.TimingOpportunity =>
                ("Timing opportunity", "時機機會"),
            TacticalScoreComponentKind.ExecutionReliability =>
                ("Execution reliability", "執行可靠度"),
            TacticalScoreComponentKind.RecoveryCost =>
                ("Recovery cost", "恢復代價"),
            TacticalScoreComponentKind.FinishPath =>
                ("Finish path", "收尾路徑"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        });

    public static string SearchTerminator(
        TaiwuLanguage language,
        TacticalSearchTerminator terminator) => Pick(language,
            terminator switch
            {
                TacticalSearchTerminator.None => ("None", "無"),
                TacticalSearchTerminator.OptionLimit =>
                    ("Option limit", "選項上限"),
                TacticalSearchTerminator.ExplorationLimit =>
                    ("Exploration limit", "探索上限"),
                TacticalSearchTerminator.TimeLimit =>
                    ("Time limit", "時間上限"),
                TacticalSearchTerminator.ResultLimit =>
                    ("Result limit", "結果上限"),
                TacticalSearchTerminator.Cancelled =>
                    ("Cancelled", "已取消"),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(terminator), terminator, null)
            });

    public static string Direction(
        TaiwuLanguage language,
        PracticeDirection direction) => Pick(language, direction switch
        {
            PracticeDirection.Direct => ("Direct", "正練"),
            PracticeDirection.Reverse => ("Reverse", "逆練"),
            PracticeDirection.Neutral => ("Neutral", "中性"),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        });

    public static string Source(
        TaiwuLanguage language,
        TacticalEvidenceSourceKind source) => Pick(language, source switch
        {
            TacticalEvidenceSourceKind.SaveSnapshot =>
                ("Save snapshot", "存檔快照"),
            TacticalEvidenceSourceKind.InstalledConfiguration =>
                ("Installed configuration", "已安裝設定"),
            TacticalEvidenceSourceKind.ConfirmedObservation =>
                ("Confirmed observation", "已確認觀察"),
            TacticalEvidenceSourceKind.VerifiedRule =>
                ("Verified rule", "已驗證規則"),
            TacticalEvidenceSourceKind.PlayerConfirmation =>
                ("Player confirmation", "玩家確認"),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        });

    private static string Pick(
        TaiwuLanguage language,
        (string English, string Chinese) value) =>
        language == TaiwuLanguage.Chinese ? value.Chinese : value.English;
}
