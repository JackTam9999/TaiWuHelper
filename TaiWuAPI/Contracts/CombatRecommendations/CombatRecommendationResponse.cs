using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public sealed record CombatRecommendationResponse(
    string SnapshotReference,
    DateTimeOffset CapturedAtUtc,
    string? GameDataVersion,
    RecommendationPolicy RequestedStyle,
    IReadOnlyList<CombatThreatResponse> Threats,
    IReadOnlyList<CombatRecommendationStyleResponse> Styles,
    IReadOnlyList<CombatRecommendationWarningResponse> Warnings);

public sealed record CombatThreatResponse(
    string Reference,
    string Code,
    string Title,
    TargetThreatSeverity Severity,
    TargetThreatActivationTiming ActivationTiming,
    IReadOnlyList<string> EvidenceReferences);

public sealed record CombatRecommendationStyleResponse(
    string SnapshotReference,
    RecommendationPolicy Style,
    bool HasRecommendation,
    string? CandidateReference,
    decimal? TotalScore,
    IReadOnlyList<RecommendationScoreResponse> Scores,
    IReadOnlyList<RecommendedSkillResponse> Skills,
    IReadOnlyList<ManualLoadoutChangeResponse> ManualChanges,
    IReadOnlyList<CombatPlanStepResponse> OpeningActions,
    IReadOnlyList<CombatPlanStepResponse> SwitchingConditions,
    IReadOnlyList<RecommendationCaveatResponse> Caveats,
    string? Diagnostic);

public sealed record RecommendationScoreResponse(
    RecommendationScoreComponentKind Component,
    int Weight,
    decimal? Score,
    decimal? WeightedPoints,
    string Explanation,
    string EvidenceReference);

public sealed record RecommendedSkillResponse(
    string Reference,
    int SkillId,
    string? Name,
    SkillCategory Category,
    PracticeDirection? CurrentDirection,
    PracticeDirection? RequiredDirection,
    bool RequiresManualDirectionChange,
    int? EffectiveCost,
    CombatCounterStrength? CounterStrength,
    CombatCounterActivationTiming? ActivationTiming,
    IReadOnlyList<RecommendationReasonResponse> Reasons);

public sealed record RecommendationReasonResponse(
    string Reference,
    string Code,
    string Summary,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> ThreatReferences);

public sealed record ManualLoadoutChangeResponse(
    string Reference,
    ManualLoadoutChangeKind Kind,
    SkillCategory Category,
    int SkillId,
    PracticeDirection? RequiredDirection,
    RecommendationReasonResponse Reason);

public sealed record CombatPlanStepResponse(
    string Reference,
    BattlePlanInstructionKind Kind,
    int SkillId,
    int? AlternativeSkillId,
    string Condition,
    RecommendationReasonResponse Reason);

public sealed record RecommendationCaveatResponse(
    string Reference,
    RecommendationCaveatKind Kind,
    string Code,
    string Explanation,
    int? SkillId,
    IReadOnlyList<string> EvidenceReferences);

public sealed record CombatRecommendationWarningResponse(
    string Reference,
    string Source,
    string Code,
    int Occurrences,
    string Message,
    IReadOnlyList<string> EvidenceReferences);
