using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWuAPI.Presentation;

public sealed record CombatRecommendationViewModel(
    string SnapshotReference,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset? SaveLastWriteTimeUtc,
    string? GameDataVersion,
    RecommendationPolicy RequestedStyle,
    string InitiallySelectedStyleReference,
    string InformationOnlyNotice,
    IReadOnlyList<ThreatViewModel> Threats,
    IReadOnlyList<RecommendationStyleViewModel> Styles,
    IReadOnlyList<RecommendationWarningViewModel> Warnings,
    InnerPowerStateViewModel? InnerPowerState = null);

public sealed record InnerPowerStateViewModel(
    string Name,
    string? EffectDescription,
    CombatSkillElement? BacklashOnUseElement);

public sealed record ThreatViewModel(
    string Reference,
    string Code,
    string Title,
    string Explanation,
    TargetThreatKind Kind,
    TargetThreatSeverity Severity,
    TargetThreatActivationTiming ActivationTiming,
    IReadOnlyList<string> EvidenceReferences);

public sealed record RecommendationStyleViewModel(
    string Reference,
    string SnapshotReference,
    RecommendationPolicy Style,
    bool IsInitiallySelected,
    bool HasRecommendation,
    string? CandidateReference,
    decimal? TotalScore,
    IReadOnlyList<RecommendationScoreViewModel> Scores,
    IReadOnlyList<LoadoutCategoryViewModel> Categories,
    IReadOnlyList<ManualLoadoutChangeViewModel> ManualChanges,
    IReadOnlyList<BattlePlanStepViewModel> OpeningActions,
    IReadOnlyList<BattlePlanStepViewModel> SwitchingConditions,
    IReadOnlyList<RecommendationCaveatViewModel> Caveats,
    string? Diagnostic);

public sealed record RecommendationScoreViewModel(
    string Reference,
    RecommendationScoreComponentKind Component,
    int Weight,
    decimal? Score,
    decimal? WeightedPoints,
    string Explanation,
    string EvidenceReference);

public sealed record LoadoutCategoryViewModel(
    string Reference,
    SkillCategory Category,
    string DisplayName,
    int? UsedSlots,
    string? UsedSlotsUnavailableReason,
    int Capacity,
    int? RemainingSlots,
    string? RemainingSlotsUnavailableReason,
    int GenericSlots,
    IReadOnlyList<RecommendedSkillViewModel> Skills,
    int CurrentGenericSlots = 0);

public sealed record RecommendedSkillViewModel(
    string Reference,
    int SkillId,
    string? Name,
    SkillCategory Category,
    PracticeDirection? CurrentDirection,
    PracticeDirection? RequiredDirection,
    bool RequiresManualDirectionChange,
    SkillCostViewModel Cost,
    SkillCounterViewModel Counter,
    IReadOnlyList<string> ThreatReferences,
    IReadOnlyList<SkillConditionViewModel> Conditions,
    IReadOnlyList<RecommendationReasonViewModel> Reasons,
    bool RequiresBreakthrough = false);

public sealed record SkillCostViewModel(
    int? ActualCost,
    string? ActualCostUnavailableReason,
    int? EffectiveCost,
    string? EffectiveCostUnavailableReason,
    int? MasteryReduction,
    int? LegendaryBookReduction,
    IReadOnlyList<string> EvidenceReferences);

public sealed record SkillCounterViewModel(
    bool IsAvailable,
    CombatCounterStrength? Strength,
    CombatCounterActivationTiming? ActivationTiming,
    string? EvidenceReference,
    string? UnavailableReason);

public sealed record SkillConditionViewModel(
    string Reference,
    RecommendationConditionKind Kind,
    CombatRequirementCriticality Criticality,
    CombatRequirementStatus Status,
    string Evaluation,
    string EvidenceReference);

public sealed record RecommendationReasonViewModel(
    string Reference,
    string Code,
    string Summary,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> ThreatReferences);

public sealed record ManualLoadoutChangeViewModel(
    string Reference,
    ManualLoadoutChangeKind Kind,
    SkillCategory Category,
    int SkillId,
    string SkillName,
    PracticeDirection? RequiredDirection,
    RecommendationReasonViewModel Reason);

public sealed record BattlePlanStepViewModel(
    string Reference,
    BattlePlanInstructionKind Kind,
    int SkillId,
    string SkillName,
    int? AlternativeSkillId,
    string? AlternativeSkillName,
    string Condition,
    RecommendationReasonViewModel Reason);

public sealed record RecommendationCaveatViewModel(
    string Reference,
    RecommendationCaveatKind Kind,
    string Code,
    string Explanation,
    int? SkillId,
    IReadOnlyList<string> EvidenceReferences);

public enum PresentationWarningKind
{
    StaleData,
    ObservationDifference,
    UnavailableValue,
    UnverifiedMechanic,
    CandidateSearch,
    General
}

public sealed record RecommendationWarningViewModel(
    string Reference,
    string Source,
    string Code,
    PresentationWarningKind Kind,
    bool IsCritical,
    int Occurrences,
    string Message,
    string EffectOnRecommendation,
    IReadOnlyList<string> EvidenceReferences);
