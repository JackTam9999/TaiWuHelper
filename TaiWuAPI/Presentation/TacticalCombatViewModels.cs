using TaiWu.Application.TacticalCombat;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWuAPI.Presentation;

public enum TacticalPlanSurfaceState
{
    Unavailable,
    Loading,
    Ready,
    PreviousResult,
    Cancelled,
    ObservationReplaced,
    Failure
}

public enum TacticalConditionPresentationState
{
    Confirmed,
    NeedsConfirmation,
    Unsupported,
    Conflicting,
    Unsatisfied,
    Fallback,
    Unresolved
}

public enum TacticalCandidatePresentationGroup
{
    Selected,
    AdmittedAlternative,
    Rejected,
    Unsupported,
    Irrelevant,
    Dominated
}

public sealed record BilingualText(string English, string Chinese)
{
    public string For(TaiwuLanguage language) =>
        language == TaiwuLanguage.Chinese
            ? Chinese
            : English;
}

public sealed record TacticalCombatViewModel(
    TacticalCombatRecommendationStatus Status,
    string TargetName,
    RecommendationPolicy Policy,
    DateTimeOffset? CapturedAtUtc,
    DateTimeOffset? LatestObservationAtUtc,
    string? GameDataVersion,
    TacticalFinishDisposition? FinishDisposition,
    TacticalSelectedLoadoutViewModel? SelectedLoadout,
    IReadOnlyList<TacticalStageViewModel> Stages,
    IReadOnlyList<TacticalGapViewModel> CriticalGaps,
    TacticalSearchSummaryViewModel? Search,
    IReadOnlyList<TacticalScoreComponentViewModel> ScoreComponents,
    IReadOnlyList<TacticalCandidateGroupViewModel> CandidateGroups,
    IReadOnlyList<TacticalEvidenceSummaryViewModel> Evidence,
    string SemanticFingerprint)
{
    public bool HasPlan => Stages.Count > 0;

    public bool IsPartial => Status ==
        TacticalCombatRecommendationStatus.PartialEvidence;

    public string SemanticFingerprintPrefix =>
        string.IsNullOrWhiteSpace(SemanticFingerprint)
            ? "—"
            : SemanticFingerprint[..Math.Min(12, SemanticFingerprint.Length)];
}

public sealed record TacticalSelectedLoadoutViewModel(
    string Fingerprint,
    decimal TotalScore,
    IReadOnlyList<TacticalLoadoutCategoryViewModel> Categories,
    GenericSlotAllocation UniversalSlots,
    IReadOnlyList<TacticalLoadoutSkillViewModel> Skills,
    IReadOnlyList<TacticalLoadoutSkillViewModel> OptionalAlternatives,
    IReadOnlyList<TacticalLoadoutChangeViewModel> Changes);

public sealed record TacticalLoadoutCategoryViewModel(
    SkillCategory Category,
    int Used,
    int Capacity,
    int UniversalSlotContribution);

public sealed record TacticalLoadoutSkillViewModel(
    int SkillId,
    string Name,
    SkillCategory Category,
    PracticeDirection Direction,
    int EffectiveCost,
    TacticalLoadoutAssignmentKind Assignment,
    int RecoveryCastCount,
    bool IsScoringEligible,
    BilingualText Limitation);

public sealed record TacticalLoadoutChangeViewModel(
    TacticalPreparationCheckKind Kind,
    BilingualText Action);

public sealed record TacticalStageViewModel(
    TacticalPlanStage Stage,
    TacticalPlanStageState State,
    BilingualText Limitation,
    IReadOnlyList<TacticalStepViewModel> Steps);

public sealed record TacticalStepViewModel(
    int Order,
    TacticalStepBranchKind BranchKind,
    TacticalConditionPresentationState ConditionState,
    BilingualText Condition,
    BilingualText ManualAction,
    BilingualText ExpectedPurpose,
    BilingualText Limitation,
    IReadOnlyList<TacticalRequirementViewModel> Requirements,
    IReadOnlyList<TacticalEvidenceSummaryViewModel> Evidence);

public sealed record TacticalRequirementViewModel(
    TacticalRequirementOutcome Outcome,
    BilingualText Description);

public sealed record TacticalGapViewModel(
    TacticalConditionPresentationState State,
    BilingualText Description,
    BilingualText Effect);

public sealed record TacticalSearchSummaryViewModel(
    bool IsComplete,
    int Considered,
    int Admitted,
    int Rejected,
    int Unsupported,
    int Irrelevant,
    int Dominated,
    int Explored,
    int Feasible,
    int Retained,
    TacticalSearchTerminator Terminator,
    int LimitingBound);

public sealed record TacticalScoreComponentViewModel(
    TacticalScoreComponentKind Kind,
    TacticalScoreComponentState State,
    int BaseWeight,
    decimal? AppliedWeight,
    decimal? NormalizedValue,
    decimal? Contribution,
    BilingualText Meaning,
    BilingualText Limitation);

public sealed record TacticalCandidateGroupViewModel(
    TacticalCandidatePresentationGroup Group,
    IReadOnlyList<TacticalCandidateViewModel> Candidates);

public sealed record TacticalCandidateViewModel(
    string Name,
    SkillCategory Category,
    PracticeDirection Direction,
    bool RequiresBreakthrough,
    BilingualText Reason);

public sealed record TacticalEvidenceSummaryViewModel(
    TacticalEvidenceSourceKind Source,
    string GameDataVersion,
    string RuleVersion,
    BilingualText Scope);
