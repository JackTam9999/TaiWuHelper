using TaiWuAPI.Contracts.VillageWorkforce;

namespace TaiWuAPI.Localization;

internal static class VillageWorkforceApiText
{
    public static string Failure(
        VillageWorkforceApiLanguage language,
        string identity) => identity switch
        {
            "VILLAGE_WORKFORCE_REQUEST_INVALID" => Pick(
                language,
                "The village-workforce request is invalid.",
                "村莊人力請求無效。"),
            "VILLAGE_WORKFORCE_SAVE_UNAVAILABLE" => Pick(
                language,
                "The configured save is unavailable.",
                "無法讀取已設定的存檔。"),
            "VILLAGE_WORKFORCE_SOURCE_VERSION_UNSUPPORTED" => Pick(
                language,
                "The installed source version is unsupported.",
                "目前安裝的資料版本尚未受支援。"),
            "VILLAGE_WORKFORCE_SOURCES_CONFLICTING" => Pick(
                language,
                "The workforce sources conflict.",
                "村莊人力資料來源互相衝突。"),
            "VILLAGE_WORKFORCE_SAVE_REVISION_CHANGED" => Pick(
                language,
                "The save changed while it was being read. Try again.",
                "讀取期間存檔已變更，請重試。"),
            "VILLAGE_WORKFORCE_SNAPSHOT_READ_FAILED" => Pick(
                language,
                "The workforce snapshot could not be read.",
                "無法讀取村莊人力快照。"),
            "VILLAGE_WORKFORCE_TARGET_NOT_FOUND" => Pick(
                language,
                "The selected shop-manager position no longer exists.",
                "所選商鋪管理位置已不存在。"),
            "VILLAGE_WORKFORCE_COMPARISON_WORKER_NOT_FOUND" => Pick(
                language,
                "A selected comparison worker is not in this result.",
                "所選比較人員不在本次結果中。"),
            "VILLAGE_WORKFORCE_PROPOSAL_INVALID" => Pick(
                language,
                "The proposed worker is not a rankable alternative.",
                "建議人員不是可排序的替代人選。"),
            "WORKFORCE_OBJECTIVE_VERSION_UNSUPPORTED"
                or "WORKFORCE_GAME_DATA_VERSION_UNSUPPORTED"
                or "WORKFORCE_MAPPING_VERSION_UNSUPPORTED"
                or "WORKFORCE_CANDIDATE_UNIVERSE_VERSION_UNSUPPORTED"
                or "WORKFORCE_FINGERPRINT_SCHEMA_VERSION_UNSUPPORTED"
                or "WORKFORCE_TARGET_KIND_UNSUPPORTED" => Pick(
                    language,
                    "No verified rule supports this request and source version.",
                    "沒有已驗證規則支援此請求與資料版本。"),
            _ => Pick(
                language,
                "The village-workforce request could not be completed.",
                "無法完成村莊人力請求。")
        };

    public static string ObjectiveLabel(VillageWorkforceApiLanguage language) =>
        Pick(language, "Shop manager base aptitude", "商鋪管理基礎資質");

    public static string ObjectiveDescription(
        VillageWorkforceApiLanguage language) => Pick(
        language,
        "Compare occupied-shop replacement candidates by the exact saved base life-skill qualification required by that shop.",
        "依商鋪所需技藝的存檔基礎資質精確值，比較已佔用管理位置的替代人選。");

    public static string Unit(VillageWorkforceApiLanguage language) =>
        Pick(language, "Qualification points", "資質點數");

    public static string Target(
        VillageWorkforceApiLanguage language,
        short areaId,
        short blockId,
        short buildingIndex,
        int slotIndex,
        sbyte discipline) => Pick(
        language,
        $"Shop area {areaId}, block {blockId}, building {buildingIndex}, manager position {slotIndex + 1}; life-skill type {discipline}",
        $"商鋪區域 {areaId}、地塊 {blockId}、建築 {buildingIndex}、管理位置 {slotIndex + 1}；技藝類別 {discipline}");

    public static string Worker(
        VillageWorkforceApiLanguage language,
        int characterId) => Pick(
        language,
        $"Worker {characterId}",
        $"人員 {characterId}");

    public static string CurrentAssignment(
        VillageWorkforceApiLanguage language) =>
        Pick(language, "Current saved assignment", "目前存檔指派");

    public static string EvaluationState(
        VillageWorkforceApiLanguage language,
        VillageWorkforceApiEvaluationState state) => state switch
        {
            VillageWorkforceApiEvaluationState.Ranked =>
                Pick(language, "Ranked", "已排序"),
            VillageWorkforceApiEvaluationState.Tied =>
                Pick(language, "Exact tie", "精確同分"),
            VillageWorkforceApiEvaluationState.CurrentOnly =>
                Pick(language, "Current assignment only", "僅屬目前指派"),
            VillageWorkforceApiEvaluationState.Ineligible =>
                Pick(language, "Ineligible", "不符合資格"),
            VillageWorkforceApiEvaluationState.Incomplete =>
                Pick(language, "Incomplete evidence", "證據不完整"),
            VillageWorkforceApiEvaluationState.Unsupported =>
                Pick(language, "Unsupported evidence", "證據不受支援"),
            VillageWorkforceApiEvaluationState.Conflicting =>
                Pick(language, "Conflicting evidence", "證據衝突"),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    public static string RequirementOutcome(
        VillageWorkforceApiLanguage language,
        VillageWorkforceApiRequirementOutcome outcome) => outcome switch
        {
            VillageWorkforceApiRequirementOutcome.Passed =>
                Pick(language, "Passed", "通過"),
            VillageWorkforceApiRequirementOutcome.Failed =>
                Pick(language, "Failed", "未通過"),
            VillageWorkforceApiRequirementOutcome.Incomplete =>
                Pick(language, "Incomplete", "不完整"),
            VillageWorkforceApiRequirementOutcome.Unsupported =>
                Pick(language, "Unsupported", "不受支援"),
            VillageWorkforceApiRequirementOutcome.Conflicting =>
                Pick(language, "Conflicting", "互相衝突"),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };

    public static string Requirement(
        VillageWorkforceApiLanguage language,
        VillageWorkforceApiRequirementKind kind) => kind switch
        {
            VillageWorkforceApiRequirementKind.SupportedSourceVersion => Pick(
                language,
                "Source and rule versions must match.",
                "資料來源與規則版本必須相符。"),
            VillageWorkforceApiRequirementKind.SupportedShopTarget => Pick(
                language,
                "The target must be this occupied supported shop position.",
                "目標必須是此已佔用且受支援的商鋪位置。"),
            VillageWorkforceApiRequirementKind.AlternativeWorkCandidate => Pick(
                language,
                "The worker must belong to the verified alternative universe.",
                "人員必須屬於已驗證的替代人選範圍。"),
            VillageWorkforceApiRequirementKind.CharacterProfileAvailable => Pick(
                language,
                "The required saved qualification must be available.",
                "必須能讀取所需的存檔基礎資質。"),
            VillageWorkforceApiRequirementKind
                .QualificationProvenanceMatch => Pick(
                    language,
                    "Qualification evidence must match this save revision.",
                    "資質證據必須符合本次存檔版本。"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public static string Component(VillageWorkforceApiLanguage language) =>
        Pick(
            language,
            "Exact saved base qualification; identity normalization and weight 1.",
            "存檔基礎資質精確值；不轉換並採權重 1。");

    public static string Limitation(
        VillageWorkforceApiLanguage language,
        string identity) => identity switch
        {
            "SAVED_BASE_QUALIFICATION_ONLY" => Pick(
                language,
                "This result uses saved base qualification only, not current attainment.",
                "本結果只使用存檔基礎資質，不代表目前造詣。"),
            "NO_EFFICIENCY_OUTPUT_OR_REVENUE" => Pick(
                language,
                "No efficiency, output, or revenue change was calculated.",
                "沒有計算效率、產出或收益變化。"),
            "OCCUPIED_SHOP_REPLACEMENT_ONLY" => Pick(
                language,
                "This comparison covers replacement of an occupied shop-manager position only.",
                "本比較只涵蓋已佔用商鋪管理位置的替換。"),
            _ => Pick(language, "Result scope is limited.", "結果範圍有限。")
        };

    public static string Comparison(
        VillageWorkforceApiLanguage language,
        VillageWorkforceApiComparisonOutcome outcome) => outcome switch
        {
            VillageWorkforceApiComparisonOutcome.Higher =>
                Pick(language, "First value is higher", "第一項數值較高"),
            VillageWorkforceApiComparisonOutcome.Lower =>
                Pick(language, "First value is lower", "第一項數值較低"),
            VillageWorkforceApiComparisonOutcome.Equal =>
                Pick(language, "Values are exactly equal", "數值完全相同"),
            VillageWorkforceApiComparisonOutcome.Unavailable =>
                Pick(language, "A required value is unavailable", "缺少必要數值"),
            VillageWorkforceApiComparisonOutcome.Incompatible =>
                Pick(language, "Values use incompatible contracts", "數值規格不相容"),
            VillageWorkforceApiComparisonOutcome.NotComparable =>
                Pick(language, "Workers are not comparable", "人員無法比較"),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };

    public static string Checklist(
        VillageWorkforceApiLanguage language,
        VillageWorkforceApiChecklistItemKind kind) => kind switch
        {
            VillageWorkforceApiChecklistItemKind.TargetIdentityMustMatch => Pick(
                language,
                "Verify that the shop and manager position match in the game.",
                "請在遊戲中核對商鋪與管理位置。"),
            VillageWorkforceApiChecklistItemKind
                .ReassignmentAvailabilityMustBeVerified => Pick(
                    language,
                    "Whether reassignment is currently allowed must be verified in the game.",
                    "目前是否允許重新指派，必須在遊戲中確認。"),
            VillageWorkforceApiChecklistItemKind
                .QualificationAndEvidenceMustBeReviewed => Pick(
                    language,
                    "Review the exact qualification and its evidence.",
                    "請檢查資質精確值與相關證據。"),
            VillageWorkforceApiChecklistItemKind.EfficiencyWasNotCalculated => Pick(
                language,
                "Efficiency and output improvement were not calculated.",
                "本工具沒有計算效率或產出改善。"),
            VillageWorkforceApiChecklistItemKind.NoActionWasSentToGame => Pick(
                language,
                "No action was sent to the game.",
                "沒有向遊戲送出任何操作。"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public static string Diagnostic(VillageWorkforceApiLanguage language) =>
        Pick(
            language,
            "Additional source evidence is available for review.",
            "另有資料來源證據可供檢查。");

    private static string Pick(
        VillageWorkforceApiLanguage language,
        string english,
        string traditionalChinese) => language switch
        {
            VillageWorkforceApiLanguage.English => english,
            VillageWorkforceApiLanguage.TraditionalChinese =>
                traditionalChinese,
            _ => throw new ArgumentOutOfRangeException(nameof(language))
        };
}
