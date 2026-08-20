using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public sealed record TacticalCombatResponse(
    TacticalCombatRecommendationStatus Status,
    string ReasonIdentity,
    bool HasTacticalPlan,
    TacticalRecommendationIdentityResponse? Identity,
    TacticalSnapshotSummaryResponse? Snapshot,
    TacticalTargetChainResponse? TargetChain,
    TacticalExecutionContextResponse? ExecutionContext,
    TacticalCandidateDiscoveryResponse? CandidateDiscovery,
    TacticalSearchResponse? Search,
    TacticalScoringResponse? Scoring,
    TacticalSelectedLoadoutResponse? SelectedLoadout,
    TacticalPlanResponse? Plan,
    TacticalDiagnosticsResponse Diagnostics);

public sealed record TacticalRecommendationIdentityResponse(
    string SnapshotFingerprint,
    string ObservationFingerprint,
    string TargetChainFingerprint,
    string RuleFingerprint,
    string? CandidateFingerprint,
    string BoundFingerprint,
    string PolicyFingerprint,
    string? SelectedLoadoutFingerprint,
    string? PlanFingerprint,
    string SemanticFingerprint);

public sealed record TacticalSnapshotSummaryResponse(
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset? LatestObservationAtUtc,
    string SourceRevisionFingerprint,
    string ObservationRevisionFingerprint,
    string? GameDataVersion);

public sealed record TacticalTargetChainResponse(
    string GameDataVersion,
    string RuleSetFingerprint,
    TacticalRuleSetResolutionStatus Status,
    IReadOnlyList<TacticalTransitionRuleResponse> Transitions,
    IReadOnlyList<TacticalRoleRuleResponse> Roles);

public sealed record TacticalTransitionRuleResponse(
    string Identity,
    TacticalRulePurpose Purpose,
    TacticalTransitionTiming Timing,
    TacticalRuleApplicability Applicability,
    IReadOnlyList<string> TriggerFacts,
    IReadOnlyList<string> ResultingFacts,
    IReadOnlyList<string> TargetGoalCodes,
    IReadOnlyList<string> UnmetEvidence,
    string LimitationIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalRoleRuleResponse(
    string Identity,
    TacticalRulePurpose Purpose,
    TacticalTransitionTiming Timing,
    TacticalRuleApplicability Applicability,
    int SkillId,
    PracticeDirection Direction,
    int RawEffectId,
    IReadOnlyList<CombatEffectMechanic> RequiredMechanics,
    IReadOnlyList<string> TargetGoalCodes,
    IReadOnlyList<string> Transitions,
    IReadOnlyList<string> UnmetEvidence,
    string LimitationIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalExecutionContextResponse(
    string SemanticFingerprint,
    TacticalRuleSetResolutionStatus RuleResolutionStatus,
    IReadOnlyList<TacticalResolvedRuleResponse> ResolvedRules,
    IReadOnlyList<TacticalContextFactResponse> Current,
    IReadOnlyList<TacticalContextFactResponse> Proposed);

public sealed record TacticalResolvedRuleResponse(
    TacticalResolvedRuleKind Kind,
    string RuleIdentity,
    TacticalRuleApplicability Applicability,
    IReadOnlyList<string> UnmetEvidence);

public sealed record TacticalContextFactResponse(
    string Identity,
    TacticalContextFactState State,
    TacticalContextOrigin Origin,
    TacticalContextAvailability Availability,
    string ReasonIdentity,
    IReadOnlyList<string> EvidenceIdentities,
    TacticalContextValueResponse? Value);

public sealed record TacticalContextValueResponse(
    string Kind,
    int? Integer = null,
    IReadOnlyList<int>? Integers = null,
    IReadOnlyList<TacticalResourceResponse>? Resources = null,
    IReadOnlyList<TacticalSlotBudgetResponse>? SlotBudgets = null,
    GenericSlotPlanResponse? UniversalSlots = null,
    TacticalInnerPowerResponse? InnerPower = null,
    IReadOnlyList<string>? SlotReferences = null,
    IReadOnlyList<TacticalLegendaryAssignmentResponse>? Assignments = null);

public sealed record TacticalResourceResponse(
    CombatResourceKind Resource,
    bool IsAvailable,
    int? Amount,
    string? UnavailableReason);

public sealed record TacticalSlotBudgetResponse(
    SkillCategory Category,
    bool UsedIsAvailable,
    int? Used,
    int Capacity,
    string? UnavailableReason);

public sealed record TacticalInnerPowerResponse(
    int StateId,
    CombatSkillElement? BacklashOnUseElement);

public sealed record TacticalLegendaryAssignmentResponse(
    string SlotReference,
    int SkillId,
    SkillCategory Category,
    LegendaryBookAssignmentOrigin Origin,
    string EvidenceIdentity);

public sealed record TacticalCandidateDiscoveryResponse(
    string SemanticFingerprint,
    int LearnedSkillCount,
    int SupportedRoleCount,
    int ConsideredVerifiedRoleCount,
    int AdmittedVerifiedRoleCount,
    int UnsupportedCount,
    IReadOnlyList<TacticalCandidateResponse> Candidates,
    IReadOnlyList<TacticalCandidateCountResponse> AdmissionCounts,
    IReadOnlyList<TacticalRejectionSummaryResponse> RejectionSummaries);

public sealed record TacticalCandidateResponse(
    string Identity,
    int SkillId,
    SkillCategory Category,
    PracticeDirection Direction,
    bool RequiresBreakthrough,
    bool IsCurrentlyEquipped,
    TacticalCandidateSupportState SupportState,
    TacticalCandidateAdmissionState AdmissionState,
    TacticalCandidateDecision Decision,
    TacticalIntegerFactResponse ObservedRawEffectId,
    TacticalIntegerFactResponse EffectiveCost,
    TacticalCandidateRoleResponse? Role,
    IReadOnlyList<TacticalCandidateGateResponse> Gates);

public sealed record TacticalIntegerFactResponse(
    TacticalContextFactState State,
    int? Value,
    string ReasonIdentity,
    IReadOnlyList<string> EvidenceIdentities);

public sealed record TacticalCandidateRoleResponse(
    string Identity,
    TacticalRulePurpose Purpose,
    TacticalTransitionTiming Timing,
    int RawEffectId,
    IReadOnlyList<string> Transitions,
    string LimitationIdentity,
    IReadOnlyList<string> EvidenceIdentities);

public sealed record TacticalCandidateGateResponse(
    TacticalCandidateGateKind Kind,
    TacticalCandidateGateState State,
    string ReasonIdentity,
    IReadOnlyList<string> EvidenceIdentities);

public sealed record TacticalCandidateCountResponse(
    TacticalCandidateAdmissionState State,
    int Count);

public sealed record TacticalRejectionSummaryResponse(
    string ReasonIdentity,
    int Count,
    IReadOnlyList<string> ExampleCandidateIdentities);

public sealed record TacticalSearchResponse(
    string SemanticFingerprint,
    bool IsComplete,
    bool IsOptimal,
    TacticalSearchCoverageResponse Coverage,
    IReadOnlyList<TacticalSearchCandidateResponse> CandidateDecisions,
    IReadOnlyList<TacticalPrunedCandidateResponse> PrunedCandidates,
    IReadOnlyList<TacticalFeasibleResultResponse> FeasibleResults);

public sealed record TacticalSearchCoverageResponse(
    TacticalSearchBoundsResponse Bounds,
    int CandidateUniverseCount,
    int RoleSupportedCount,
    int AdmittedCount,
    int RejectedCount,
    int UnsupportedCount,
    int IrrelevantCount,
    int DominatedCount,
    int SearchedOptionCount,
    int ExploredCombinationCount,
    int FeasibleResultCount,
    int RetainedResultCount,
    TacticalSearchTerminator FirstTerminator,
    long ElapsedMilliseconds,
    string Fingerprint,
    IReadOnlyList<TacticalCacheDiagnosticResponse> Caches);

public sealed record TacticalSearchBoundsResponse(
    int MaximumOptions,
    int MaximumExploredCombinations,
    int MaximumElapsedMilliseconds,
    int MaximumResults);

public sealed record TacticalCacheDiagnosticResponse(
    string CacheIdentity,
    int HitCount,
    int MissCount);

public sealed record TacticalSearchCandidateResponse(
    string Identity,
    TacticalCandidateDecision Decision,
    IReadOnlyList<string> Roles,
    IReadOnlyList<TacticalRequirementEvaluationResponse> Requirements,
    string ReasonIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence,
    string? DominatedBy);

public sealed record TacticalPrunedCandidateResponse(
    string Candidate,
    TacticalPruningRuleKind Rule,
    string ReasonIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence,
    string? Dominator);

public sealed record TacticalFeasibleResultResponse(
    string StableKey,
    IReadOnlyList<string> SelectedCandidates);

public sealed record TacticalScoringResponse(
    string SemanticFingerprint,
    string ScoringVersion,
    RecommendationPolicy Policy,
    string PolicyLimitationIdentity,
    IReadOnlyList<TacticalScoredLoadoutResponse> RankedCandidates);

public sealed record TacticalScoredLoadoutResponse(
    string CandidateIdentity,
    decimal TotalScore,
    IReadOnlyList<TacticalScoreComponentResponse> Components,
    IReadOnlyList<TacticalUnusedCapacityResponse> UnusedCapacity);

public sealed record TacticalScoreComponentResponse(
    TacticalScoreComponentKind Kind,
    TacticalScoreComponentState State,
    string NormalizationIdentity,
    int BaseWeight,
    decimal? AppliedWeight,
    decimal? NormalizedValue,
    decimal? Contribution,
    IReadOnlyList<TacticalScoreInputResponse> RawInputs,
    IReadOnlyList<TacticalEvidenceResponse> Evidence,
    IReadOnlyList<string> Limitations);

public sealed record TacticalScoreInputResponse(
    TacticalScoreInputKind Kind,
    string Identity,
    TacticalEvidenceState State,
    TacticalFactValueResponse? Value,
    string ReasonIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalUnusedCapacityResponse(
    SkillCategory Category,
    int Remaining,
    int Capacity);

public sealed record TacticalSelectedLoadoutResponse(
    string Fingerprint,
    string CandidateIdentity,
    decimal TotalScore,
    IReadOnlyList<string> SelectedCandidates,
    IReadOnlyList<TacticalLoadoutCategoryResponse> Categories,
    GenericSlotPlanResponse UniversalSlots);

public sealed record TacticalLoadoutCategoryResponse(
    SkillCategory Category,
    IReadOnlyList<int> SkillIds,
    TacticalSlotBudgetResponse SlotBudget);

public sealed record TacticalPlanResponse(
    string SemanticFingerprint,
    string GameDataVersion,
    string RuleVersion,
    TacticalFinishDisposition FinishDisposition,
    IReadOnlyList<TacticalStateFactResponse> Facts,
    IReadOnlyList<TacticalRequirementDefinitionResponse> Requirements,
    IReadOnlyList<TacticalPlanTransitionResponse> Transitions,
    IReadOnlyList<TacticalPlanRoleResponse> Roles,
    IReadOnlyList<TacticalPlanStageResponse> Stages,
    IReadOnlyList<TacticalPreparationCheckResponse> PreparationChecks,
    IReadOnlyList<TacticalEvidenceResponse> SharedEvidence);

public sealed record TacticalStateFactResponse(
    string Identity,
    TacticalEvidenceState State,
    TacticalFactValueResponse? Value,
    string ReasonIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence,
    IReadOnlyList<TacticalConflictResponse> Conflicts);

public sealed record TacticalFactValueResponse(
    TacticalFactValueKind Kind,
    string CanonicalValue);

public sealed record TacticalConflictResponse(
    TacticalFactValueResponse Value,
    TacticalEvidenceResponse Evidence);

public sealed record TacticalRequirementDefinitionResponse(
    string Identity,
    string Fact,
    TacticalRequirementOperator Operator,
    TacticalFactValueResponse? ExpectedValue);

public sealed record TacticalRequirementEvaluationResponse(
    string Requirement,
    TacticalRequirementOutcome Outcome,
    string ReasonIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalPlanTransitionResponse(
    string Identity,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> ResultingFacts,
    TacticalTransitionTiming Timing,
    string ExpectedPurposeIdentity,
    string LimitationIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalPlanRoleResponse(
    string Identity,
    int SkillId,
    PracticeDirection Direction,
    int EffectId,
    TacticalTransitionTiming Timing,
    IReadOnlyList<string> Transitions,
    IReadOnlyList<string> Requirements,
    string LimitationIdentity,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalPlanStageResponse(
    TacticalPlanStage Stage,
    TacticalPlanStageState State,
    string LimitationIdentity,
    IReadOnlyList<TacticalPlanStepResponse> Steps,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalPlanStepResponse(
    string Identity,
    int Order,
    TacticalStepBranchKind BranchKind,
    IReadOnlyList<string> ObservedFacts,
    IReadOnlyList<TacticalRequirementEvaluationResponse> Requirements,
    IReadOnlyList<string> Transitions,
    string ManualActionIdentity,
    string ExpectedPurposeIdentity,
    string LimitationIdentity,
    IReadOnlyList<TacticalPlanBranchResponse> Branches,
    IReadOnlyList<TacticalEvidenceResponse> Evidence);

public sealed record TacticalPlanBranchResponse(
    string ConditionIdentity,
    TacticalBranchOutcome Outcome,
    string? TargetStep);

public sealed record TacticalPreparationCheckResponse(
    string Identity,
    TacticalPreparationCheckKind Kind,
    string ManualActionIdentity,
    SkillCategory? Category,
    int? SkillId,
    PracticeDirection? Direction);

public sealed record TacticalEvidenceResponse(
    TacticalEvidenceSourceKind Source,
    string EvidenceIdentity,
    string GameDataVersion,
    string RuleVersion,
    string ScopeIdentity);

public sealed record TacticalDiagnosticsResponse(
    TacticalRecommendationWorkCountsResponse WorkCounts,
    long? SearchElapsedMilliseconds,
    DateTimeOffset? CapturedAtUtc,
    DateTimeOffset? LatestObservationAtUtc);

public sealed record TacticalRecommendationWorkCountsResponse(
    int SnapshotReads,
    int LegacyRecommendationBuilds,
    int ComparisonBuilds,
    int RuleResolutions,
    int ContextProjections,
    int CandidateDiscoveries,
    int Searches,
    int Scores,
    int PlanCompilations);
