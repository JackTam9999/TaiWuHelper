using System.ComponentModel.DataAnnotations;

namespace TaiWuAPI.Contracts.VillageWorkforce;

public enum VillageWorkforceApiLanguage
{
    English = 0,
    TraditionalChinese = 1
}

public enum VillageWorkforceApiStatus
{
    Complete = 0,
    Partial = 1,
    InvalidRequest = 2,
    SaveUnavailable = 3,
    UnsupportedSourceVersion = 4,
    ConflictingSources = 5,
    ChangedRevision = 6,
    ReadFailed = 7,
    TargetNotFound = 8,
    UnsupportedRule = 9,
    InvalidComparison = 10,
    InvalidProposal = 11
}

public enum VillageWorkforceApiSnapshotStatus
{
    Complete = 0,
    Partial = 1,
    SaveUnavailable = 2,
    UnsupportedVersion = 3,
    ConflictingSources = 4,
    ChangedRevision = 5,
    ReadFailed = 6
}

public enum VillageWorkforceApiEvaluationState
{
    Ranked = 0,
    Tied = 1,
    CurrentOnly = 2,
    Ineligible = 3,
    Incomplete = 4,
    Unsupported = 5,
    Conflicting = 6
}

public enum VillageWorkforceApiWorkerState
{
    Eligible = 0,
    CurrentOnly = 1,
    Ineligible = 2,
    Incomplete = 3,
    Unsupported = 4,
    Conflicting = 5
}

public enum VillageWorkforceApiRequirementKind
{
    SupportedSourceVersion = 0,
    SupportedShopTarget = 1,
    AlternativeWorkCandidate = 2,
    CharacterProfileAvailable = 3,
    QualificationProvenanceMatch = 4
}

public enum VillageWorkforceApiRequirementOutcome
{
    Passed = 0,
    Failed = 1,
    Incomplete = 2,
    Unsupported = 3,
    Conflicting = 4
}

public enum VillageWorkforceApiEvidenceSource
{
    ConfiguredSave = 0,
    InstalledGameData = 1,
    DerivedRule = 2
}

public enum VillageWorkforceApiValueKind
{
    Boolean = 0,
    Int16 = 1,
    Int32 = 2
}

public enum VillageWorkforceApiComparisonOutcome
{
    Higher = 0,
    Lower = 1,
    Equal = 2,
    Unavailable = 3,
    Incompatible = 4,
    NotComparable = 5
}

public enum VillageWorkforceApiVacancyState
{
    NoExplicitVacancy = 0
}

public enum VillageWorkforceApiChecklistCategory
{
    Prerequisite = 0,
    FactToVerify = 1,
    Caution = 2
}

public enum VillageWorkforceApiChecklistItemKind
{
    TargetIdentityMustMatch = 0,
    ReassignmentAvailabilityMustBeVerified = 1,
    QualificationAndEvidenceMustBeReviewed = 2,
    EfficiencyWasNotCalculated = 3,
    NoActionWasSentToGame = 4
}

public enum VillageWorkforceApiDiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}

public sealed record VillageWorkforceApiQuery
{
    [Range(0, short.MaxValue)]
    public short AreaId { get; init; }

    [Range(0, short.MaxValue)]
    public short BlockId { get; init; }

    [Range(0, short.MaxValue)]
    public short BuildingBlockIndex { get; init; }

    [Range(0, sbyte.MaxValue)]
    public int ManagerSlotIndex { get; init; }

    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string Objective { get; init; } =
        VillageWorkforceApiTokens.Objective;

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string ObjectiveVersion { get; init; } =
        VillageWorkforceApiTokens.ObjectiveVersion;

    [Required]
    [StringLength(30, MinimumLength = 1)]
    public string Filter { get; init; } = VillageWorkforceApiTokens.FilterAll;

    [Range(1, int.MaxValue)]
    public int? FirstComparisonCharacterId { get; init; }

    [Range(1, int.MaxValue)]
    public int? SecondComparisonCharacterId { get; init; }

    [Range(1, int.MaxValue)]
    public int? ProposedCharacterId { get; init; }

    [Required]
    [StringLength(10, MinimumLength = 2)]
    public string Language { get; init; } = VillageWorkforceApiTokens.English;
}

public sealed record VillageWorkforceDiscoveryResponse(
    VillageWorkforceApiStatus Status,
    VillageWorkforceFailureResponse? Failure,
    IReadOnlyList<VillageWorkforceObjectiveResponse> Objectives,
    IReadOnlyList<VillageWorkforceTargetResponse> Targets);

public sealed record VillageWorkforceResultResponse(
    VillageWorkforceApiStatus Status,
    VillageWorkforceFailureResponse? Failure,
    string? Fingerprint,
    VillageWorkforceSourceResponse? Source,
    VillageWorkforceObjectiveResponse? Objective,
    VillageWorkforceTargetResponse? Target,
    VillageWorkforceCurrentAssignmentResponse? CurrentAssignment,
    VillageWorkforceCountsResponse? Counts,
    IReadOnlyList<VillageWorkforceCandidateResponse> Candidates,
    IReadOnlyList<string> VisibleCandidateReferences,
    IReadOnlyList<VillageWorkforceLimitationResponse> Limitations,
    VillageWorkforceComparisonResponse? Comparison,
    VillageWorkforceManualPlanResponse? ManualPlan,
    IReadOnlyList<VillageWorkforceDiagnosticResponse> Diagnostics);

public sealed record VillageWorkforceFailureResponse(
    string Identity,
    string Message);

public sealed record VillageWorkforceSourceResponse(
    DateTimeOffset CapturedAtUtc,
    VillageWorkforceApiSnapshotStatus SnapshotStatus,
    string GameDataVersion,
    string MappingVersion,
    string CandidateUniverseVersion,
    string FingerprintSchemaVersion);

public sealed record VillageWorkforceObjectiveResponse(
    string Reference,
    string Identity,
    string ObjectiveVersion,
    string RuleVersion,
    string Label,
    string Description,
    string UnitLabel);

public sealed record VillageWorkforceTargetResponse(
    string Reference,
    short AreaId,
    short BlockId,
    short BuildingBlockIndex,
    int ManagerSlotIndex,
    sbyte RequiredLifeSkillType,
    VillageWorkforceApiVacancyState VacancyState,
    string Label);

public sealed record VillageWorkforceCurrentAssignmentResponse(
    string TargetReference,
    string WorkerReference,
    int CharacterId,
    string Label);

public sealed record VillageWorkforceCountsResponse(
    int Total,
    int Comparable,
    int Ranked,
    int Tied,
    int CurrentOnly,
    int Ineligible,
    int Incomplete,
    int Unsupported,
    int Conflicting,
    int Visible);

public sealed record VillageWorkforceCandidateResponse(
    string Reference,
    int CharacterId,
    string Label,
    string? LocationLabel,
    bool IsCurrent,
    VillageWorkforceApiWorkerState WorkerState,
    VillageWorkforceApiEvaluationState EvaluationState,
    string EvaluationStateLabel,
    int? CompetitionRank,
    decimal? Total,
    string? Unit,
    IReadOnlyList<VillageWorkforceRequirementResponse> Requirements,
    IReadOnlyList<VillageWorkforceComponentResponse> Components,
    IReadOnlyList<VillageWorkforceDiagnosticResponse> Diagnostics);

public sealed record VillageWorkforceRequirementResponse(
    int Order,
    VillageWorkforceApiRequirementKind Kind,
    VillageWorkforceApiRequirementOutcome Outcome,
    string OutcomeLabel,
    string ReasonIdentity,
    string Explanation,
    IReadOnlyList<VillageWorkforceEvidenceResponse> Evidence,
    IReadOnlyList<VillageWorkforceConflictResponse> Conflicts);

public sealed record VillageWorkforceComponentResponse(
    string Identity,
    sbyte RequiredLifeSkillType,
    short RawValue,
    decimal NormalizedValue,
    decimal Weight,
    decimal Contribution,
    string Unit,
    string ExplanationIdentity,
    string Explanation,
    IReadOnlyList<VillageWorkforceEvidenceResponse> Evidence);

public sealed record VillageWorkforceEvidenceResponse(
    string Reference,
    VillageWorkforceApiEvidenceSource Source,
    string SourceVersion);

public sealed record VillageWorkforceConflictResponse(
    VillageWorkforceApiValueKind ValueKind,
    bool? Boolean,
    short? Int16,
    int? Int32,
    VillageWorkforceApiEvidenceSource Source,
    string SourceVersion);

public sealed record VillageWorkforceLimitationResponse(
    string Identity,
    string Message);

public sealed record VillageWorkforceComparisonResponse(
    string FirstWorkerReference,
    string SecondWorkerReference,
    VillageWorkforceApiComparisonOutcome Outcome,
    string OutcomeLabel,
    decimal? FirstValue,
    decimal? SecondValue,
    string? Unit);

public sealed record VillageWorkforceManualPlanResponse(
    string CurrentWorkerReference,
    string ProposedWorkerReference,
    IReadOnlyList<VillageWorkforceChecklistItemResponse> Checklist);

public sealed record VillageWorkforceChecklistItemResponse(
    VillageWorkforceApiChecklistItemKind Kind,
    VillageWorkforceApiChecklistCategory Category,
    string Message);

public sealed record VillageWorkforceDiagnosticResponse(
    string Scope,
    string Identity,
    VillageWorkforceApiDiagnosticSeverity Severity,
    string Message,
    string? WorkerReference);

public static class VillageWorkforceApiTokens
{
    public const string Objective =
        "SHOP_MANAGER_BASE_LIFE_SKILL_QUALIFICATION";
    public const string ObjectiveVersion = "1";
    public const string FilterAll = "ALL";
    public const string FilterComparable = "COMPARABLE";
    public const string FilterNeedsReview = "NEEDS_REVIEW";
    public const string FilterIneligible = "INELIGIBLE";
    public const string English = "en";
    public const string TraditionalChinese = "zh-Hant";
}
