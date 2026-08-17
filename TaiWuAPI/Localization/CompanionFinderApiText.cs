using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWuAPI.Localization;

public static class CompanionFinderApiText
{
    public static string RolePurpose(
        TaiwuLanguage language,
        CompanionRoleIdentity identity) => identity.Value switch
        {
            "MARTIAL_DISCIPLINE_APTITUDE" => Localized(
                language,
                "Compare current companions by the exact saved base aptitude in one selected martial discipline.",
                "依所選武學類別的存檔基礎資質精確值，比較目前同道。"),
            "LIFE_SKILL_DISCIPLINE_APTITUDE" => Localized(
                language,
                "Compare current companions by the exact saved base aptitude in one selected life-skill discipline.",
                "依所選技藝類別的存檔基礎資質精確值，比較目前同道。"),
            "COMPREHENSIVE_BASE_CAPABILITY" => Localized(
                language,
                "Compare current companions by a transparent equal-weight breadth index across six base attributes, 14 martial aptitudes, and 16 life-skill aptitudes.",
                "以透明的等權重廣度指數，比較目前同道的六項基礎主要屬性、十四項武學資質與十六項技藝資質。"),
            _ => Localized(
                language,
                "Compare candidates for this verified role.",
                "依此已驗證角色比較候選同道。")
        };

    public static string ScoreLimitation(TaiwuLanguage language) => Localized(
        language,
        "The score is role-local evidence, not a universal ranking, success probability, or action recommendation.",
        "此分數僅代表該角色的證據，不是通用排名、成功機率或行動建議。");

    public static string ScoreLimitation(
        TaiwuLanguage language,
        CompanionRoleIdentity identity) => identity.Value
            == "COMPREHENSIVE_BASE_CAPABILITY"
        ? Localized(
            language,
            "The breadth index equally averages three complete saved-base category averages. It ranks this descriptive objective only; it is not future potential, universal suitability, success probability, or an action recommendation.",
            "廣度指數等權平均三個完整的存檔基礎類別平均值，只依此描述目標排名；不代表未來潛力、通用適合度、成功機率或行動建議。")
        : ScoreLimitation(language);

    public static string RankingState(
        TaiwuLanguage language,
        CompanionRoleCandidateRankingState state) => state switch
        {
            CompanionRoleCandidateRankingState.Ranked => Localized(language, "Ranked", "已排名"),
            CompanionRoleCandidateRankingState.Tied => Localized(language, "Tied", "同分"),
            CompanionRoleCandidateRankingState.Ineligible => Localized(language, "Ineligible", "不符合資格"),
            CompanionRoleCandidateRankingState.Incomplete => Localized(language, "Incomplete evidence", "證據不完整"),
            CompanionRoleCandidateRankingState.Unsupported => Localized(language, "Unsupported", "不支援"),
            CompanionRoleCandidateRankingState.Conflicting => Localized(language, "Conflicting evidence", "證據衝突"),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown candidate-ranking state.")
        };

    public static string EvaluationState(
        TaiwuLanguage language,
        CompanionRoleEvaluationState state) => state switch
        {
            CompanionRoleEvaluationState.Rankable => Localized(language, "Rankable", "可排名"),
            CompanionRoleEvaluationState.Ineligible => Localized(language, "Ineligible", "不符合資格"),
            CompanionRoleEvaluationState.Incomplete => Localized(language, "Incomplete evidence", "證據不完整"),
            CompanionRoleEvaluationState.Unsupported => Localized(language, "Unsupported", "不支援"),
            CompanionRoleEvaluationState.Conflicting => Localized(language, "Conflicting evidence", "證據衝突"),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown role-evaluation state.")
        };

    public static string GateRequirement(
        TaiwuLanguage language,
        CompanionRoleRequirementKind kind,
        CandidateProfileField? field) => kind switch
        {
            CompanionRoleRequirementKind.CandidateUniverseEligible =>
                Localized(language, "Candidate-universe eligibility", "候選範圍資格"),
            CompanionRoleRequirementKind.SourceVersionsSupported =>
                Localized(language, "Supported source versions", "受支援的來源版本"),
            CompanionRoleRequirementKind.DisciplineSupported =>
                Localized(language, "Supported discipline", "受支援的類別"),
            CompanionRoleRequirementKind.ObjectiveSupported =>
                Localized(language, "Supported comparison objective", "受支援的比較目標"),
            CompanionRoleRequirementKind.RequiredFactConfirmed => Localized(
                language,
                $"Required {FieldLabel(language, field)} evidence",
                $"必要的{FieldLabel(language, field)}證據"),
            CompanionRoleRequirementKind.FactProvenanceCompatible => Localized(
                language,
                $"{FieldLabel(language, field)} source compatibility",
                $"{FieldLabel(language, field)}來源相容性"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown role-requirement kind.")
        };

    public static string GateOutcome(
        TaiwuLanguage language,
        CompanionRoleGateOutcome outcome) => outcome switch
        {
            CompanionRoleGateOutcome.Passed => Localized(language, "Passed", "通過"),
            CompanionRoleGateOutcome.Failed => Localized(language, "Failed", "未通過"),
            CompanionRoleGateOutcome.Incomplete => Localized(language, "Incomplete", "不完整"),
            CompanionRoleGateOutcome.Unsupported => Localized(language, "Unsupported", "不支援"),
            CompanionRoleGateOutcome.Conflicting => Localized(language, "Conflicting", "衝突"),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown gate outcome.")
        };

    public static string GateReason(TaiwuLanguage language, string identity) =>
        identity switch
        {
            "ROLE_REQUIREMENTS_PASSED" => Localized(
                language,
                "All verified role requirements passed.",
                "所有已驗證的角色條件均已通過。"),
            "CANDIDATE_UNIVERSE_INELIGIBLE" => Localized(
                language,
                "Verified candidate-universe evidence does not meet eligibility.",
                "已驗證的候選範圍證據不符合資格。"),
            "REQUIRED_FACT_MISSING" => Localized(
                language,
                "A required saved fact is missing.",
                "缺少必要的存檔資料。"),
            "REQUIRED_FACT_INCOMPLETE" or "REQUIRED_FACT_STALE" => Localized(
                language,
                "A required saved fact is incomplete or no longer current.",
                "必要的存檔資料不完整或已非最新。"),
            "REQUIRED_FACT_UNSUPPORTED" => Localized(
                language,
                "The current source cannot provide a required fact.",
                "目前來源無法提供必要資料。"),
            "REQUIRED_FACT_CONFLICTING" or "FACT_PROVENANCE_CONFLICTS_WITH_PROFILE" => Localized(
                language,
                "Required evidence conflicts and cannot be resolved safely.",
                "必要證據互相衝突，無法安全判定。"),
            "CAPABILITY_SUMMARY_INCOMPLETE" or "CAPABILITY_SUMMARY_STALE" => Localized(
                language,
                "At least one required capability value is incomplete or stale, so no breadth rank is assigned.",
                "至少一項必要能力值不完整或已過期，因此不會給予廣度名次。"),
            "CAPABILITY_SUMMARY_UNSUPPORTED" => Localized(
                language,
                "At least one required capability value is unsupported, so no breadth rank is assigned.",
                "至少一項必要能力值不受支援，因此不會給予廣度名次。"),
            "CAPABILITY_SUMMARY_CONFLICTING" or
                "CAPABILITY_PROVENANCE_CONFLICTS_WITH_PROFILE" => Localized(
                    language,
                    "Capability evidence conflicts with the profile revision, so no breadth rank is assigned.",
                    "能力證據與人物資料版本衝突，因此不會給予廣度名次。"),
            "SOURCE_VERSIONS_UNSUPPORTED" or "DISCIPLINE_UNSUPPORTED" or
                "OBJECTIVE_UNSUPPORTED" => Localized(
                language,
                "The selected source version, discipline, or comparison objective is unsupported.",
                "所選來源版本、類別或比較目標不受支援。"),
            _ => Localized(
                language,
                "See the typed requirement outcome and supporting evidence.",
                "請查看具類型的條件結果與佐證。")
        };

    public static string Explanation(TaiwuLanguage language, string identity) =>
        identity switch
        {
            "STRONGEST_APPROVED_SCORE_CONTRIBUTION" => Localized(
                language,
                "Strongest contribution among the role's approved components.",
                "此角色已核准計分項目中的最高貢獻。"),
            "ROLE_SCORE_LIMITED_TO_APPROVED_COMPONENTS" => ScoreLimitation(language),
            "CAPABILITY_BREADTH_INDEX_CONTRIBUTION" => Localized(
                language,
                "Equal-weight mean of the complete six-attribute, martial-aptitude, and life-skill-aptitude category averages.",
                "完整的六項基礎主要屬性、武學資質與技藝資質類別平均值之等權平均。"),
            "EXACT_ROLE_TOTAL_TIE" => Localized(
                language,
                "Another candidate has the same exact role-local total and shared rank.",
                "另一位候選同道具有完全相同的角色分數與共同名次。"),
            _ => GateReason(language, identity)
        };

    public static string ComparisonOutcome(
        TaiwuLanguage language,
        CompanionRoleComparisonOutcome outcome) => outcome switch
        {
            CompanionRoleComparisonOutcome.FirstAdvantage => Localized(language, "First candidate advantage", "第一位候選較高"),
            CompanionRoleComparisonOutcome.SecondAdvantage => Localized(language, "Second candidate advantage", "第二位候選較高"),
            CompanionRoleComparisonOutcome.Equal => Localized(language, "Equal confirmed evidence", "已確認證據相同"),
            CompanionRoleComparisonOutcome.Unavailable => Localized(language, "Comparison unavailable", "無法比較"),
            CompanionRoleComparisonOutcome.Conflicting => Localized(language, "Conflicting evidence", "證據衝突"),
            CompanionRoleComparisonOutcome.Tradeoff => Localized(language, "Genuine component tradeoff", "計分項目各有取捨"),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown comparison outcome.")
        };

    public static string Failure(TaiwuLanguage language, string identity) =>
        identity switch
        {
            "ROLE_IDENTITY_UNKNOWN" => Localized(language, "The requested role is unknown.", "找不到所選角色。"),
            "ROLE_VERSION_UNSUPPORTED" => Localized(language, "The requested role version is unsupported.", "不支援所選角色版本。"),
            "CANDIDATE_SAVE_UNAVAILABLE" => Localized(language, "The configured save is unavailable.", "無法讀取已設定的存檔。"),
            "CANDIDATE_SOURCE_VERSION_UNSUPPORTED" => Localized(language, "The candidate source version is unsupported.", "不支援候選資料來源版本。"),
            "CANDIDATE_SAVE_REVISION_CHANGED" => Localized(language, "The save changed during reading; retry to build a new result.", "讀取期間存檔已變更；請重試以建立新結果。"),
            "COMPARISON_CANDIDATE_NOT_FOUND" => Localized(language, "A selected comparison candidate is not in this result.", "所選比較對象不在此結果中。"),
            "COMPANION_FINDER_REQUEST_INVALID" => Localized(language, "The companion-finder request is invalid.", "同道搜尋要求無效。"),
            _ => Localized(language, "The companion finder could not produce a safe result.", "同道搜尋無法產生安全結果。")
        };

    public static string Diagnostic(TaiwuLanguage language, string identity) =>
        identity switch
        {
            "ROLE_SCORE_IS_ROLE_LOCAL" => ScoreLimitation(language),
            "SHORTLIST_IS_INFORMATION_ONLY" => Localized(
                language,
                "This result is information only and does not control the game.",
                "此結果僅供參考，不會控制遊戲。"),
            "SHORTLIST_CONTAINS_UNRANKED_EVIDENCE" => Localized(
                language,
                "Some candidates remain unranked because required evidence is unavailable or conflicting.",
                "部分候選同道因必要證據缺漏或衝突而未排名。"),
            _ => Localized(
                language,
                "Additional typed source evidence affects this result.",
                "其他具類型的來源證據會影響此結果。")
        };

    public static string Unavailable(TaiwuLanguage language, string code) =>
        Localized(
            language,
            $"Evidence is unavailable ({code}).",
            $"證據目前不可用（{code}）。");

    private static string FieldLabel(
        TaiwuLanguage language,
        CandidateProfileField? field) => field switch
        {
            CandidateProfileField.BaseMartialQualification => Localized(
                language,
                "saved base martial qualification",
                "存檔基礎武學資質"),
            CandidateProfileField.BaseLifeSkillQualification => Localized(
                language,
                "saved base life-skill qualification",
                "存檔基礎技藝資質"),
            CandidateProfileField.CapabilityBreadthIndex => Localized(
                language,
                "saved-base capability breadth index",
                "存檔基礎能力廣度指數"),
            null => throw new ArgumentException(
                "This requirement kind needs a candidate-profile field.",
                nameof(field)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "The candidate-profile field has no requirement label.")
        };

    private static string Localized(
        TaiwuLanguage language,
        string english,
        string chinese) => language switch
        {
            TaiwuLanguage.English => english,
            TaiwuLanguage.Chinese => chinese,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown UI language.")
        };
}
