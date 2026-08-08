using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public sealed record LoadoutComparisonResponse(
    string Reference,
    string SnapshotReference,
    string TargetReference,
    IReadOnlyList<LoadoutComparisonColumnResponse> Columns,
    IReadOnlyList<LoadoutComparisonProvenanceResponse> BaselineProvenance);

public sealed record LoadoutComparisonColumnResponse(
    LoadoutComparisonColumnKind Kind,
    LoadoutComparisonColumnStatus Status,
    RecommendationPolicy? Policy,
    LoadoutComparisonLoadoutResponse? Loadout,
    LoadoutComparisonTacticalSummaryResponse? TacticalSummary,
    LoadoutComparisonDiagnosticResponse? Diagnostic);

public sealed record LoadoutComparisonLoadoutResponse(
    IReadOnlyList<LoadoutComparisonCategoryResponse> Categories,
    LoadoutComparisonGenericSlotValueResponse GenericSlotAllocation);

public sealed record LoadoutComparisonCategoryResponse(
    SkillCategory Category,
    LoadoutComparisonCapacityResponse Capacity,
    IReadOnlyList<LoadoutComparisonSkillResponse> Skills);

public sealed record LoadoutComparisonSkillResponse(
    LoadoutComparisonSkillIdentityResponse Identity,
    LoadoutComparisonStringValueResponse Name,
    LoadoutComparisonPracticeDirectionValueResponse CurrentDirection,
    LoadoutComparisonMembershipValueResponse Membership,
    LoadoutComparisonIntValueResponse EffectiveCost,
    IReadOnlyList<LoadoutComparisonSkillActionResponse> Actions);

public sealed record LoadoutComparisonSkillIdentityResponse(
    SkillCategory Category,
    int SkillId);

public sealed record LoadoutComparisonSkillActionResponse(
    LoadoutComparisonSkillActionKind Kind,
    PracticeDirection RequiredDirection,
    LoadoutComparisonReasonResponse Reason);

public sealed record LoadoutComparisonReasonResponse(
    string Code,
    string Summary,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<string> ThreatReferences);

public sealed record LoadoutComparisonCapacityResponse(
    LoadoutComparisonIntValueResponse Used,
    LoadoutComparisonIntValueResponse Capacity,
    LoadoutComparisonIntValueResponse Remaining,
    LoadoutComparisonIntValueResponse CategoryContribution,
    LoadoutComparisonIntValueResponse GenericContribution);

public sealed record LoadoutComparisonTacticalSummaryResponse(
    LoadoutComparisonIntValueResponse ManualActionCount,
    LoadoutComparisonSkillIdentityValueResponse ActiveDefense,
    LoadoutComparisonSkillIdentityValueResponse ActiveAgility,
    IReadOnlyList<LoadoutComparisonThreatResponse> CoveredThreats,
    IReadOnlyList<LoadoutComparisonThreatResponse> UnresolvedThreats,
    IReadOnlyList<string> ConditionReferences,
    IReadOnlyList<string> CaveatReferences,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<LoadoutComparisonScoreResponse> Scores,
    string ScoreScopeNotice);

public sealed record LoadoutComparisonThreatResponse(
    string Reference,
    string Code,
    string? Title);

public sealed record LoadoutComparisonScoreResponse(
    RecommendationScoreComponentKind Component,
    int Weight,
    LoadoutComparisonDecimalValueResponse Score,
    string Explanation,
    string EvidenceReference);

public sealed record LoadoutComparisonDiagnosticResponse(
    string Code,
    string Summary,
    IReadOnlyList<string> EvidenceReferences);

public sealed record LoadoutComparisonProvenanceResponse(
    LoadoutComparisonBaselineField Field,
    SnapshotDataSource Source,
    DateTimeOffset CapturedAtUtc,
    string EvidenceReference);

public sealed record LoadoutComparisonIntValueResponse(
    bool IsAvailable,
    int? Value,
    string? UnavailableReason);

public sealed record LoadoutComparisonDecimalValueResponse(
    bool IsAvailable,
    decimal? Value,
    string? UnavailableReason);

public sealed record LoadoutComparisonStringValueResponse(
    bool IsAvailable,
    string? Value,
    string? UnavailableReason);

public sealed record LoadoutComparisonPracticeDirectionValueResponse(
    bool IsAvailable,
    PracticeDirection? Value,
    string? UnavailableReason);

public sealed record LoadoutComparisonMembershipValueResponse(
    bool IsAvailable,
    LoadoutComparisonMembership? Value,
    string? UnavailableReason);

public sealed record LoadoutComparisonSkillIdentityValueResponse(
    bool IsAvailable,
    LoadoutComparisonSkillIdentityResponse? Value,
    string? UnavailableReason);

public sealed record LoadoutComparisonGenericSlotValueResponse(
    bool IsAvailable,
    GenericSlotPlanResponse? Value,
    string? UnavailableReason);
