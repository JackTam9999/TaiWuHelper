using TaiWu.Application.CombatRecommendations;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public sealed record TargetStrategyResponse(
    TargetCombatProfileResponse Profile,
    IReadOnlyList<TargetArchetypeMatchResponse> Archetypes,
    TargetPlaybookCompositionResponse Playbook,
    TargetPlaybookAdjustmentSetResponse Adjustments,
    IReadOnlyList<TargetCounterAvailabilityResponse> CounterAvailability);

public sealed record TargetCombatProfileResponse(
    int TargetCharacterId,
    string RuleVersion,
    string Fingerprint,
    IReadOnlyList<TargetProfileFacetResponse> Facets,
    IReadOnlyList<TargetProfileDiagnosticResponse> Diagnostics);

public sealed record TargetProfileFacetResponse(
    TargetProfileDimension Dimension,
    string Code,
    TargetProfileEvidenceState State,
    TargetProfileFacetValueResponse? Value,
    IReadOnlyList<TargetProfileEvidenceResponse> Evidence,
    IReadOnlyList<TargetProfileConflictCandidateResponse> ConflictCandidates,
    TargetProfileUnavailableReasonResponse? UnavailableReason);

public sealed record TargetProfileFacetValueResponse(
    TargetProfileDimension Dimension,
    string Code,
    TargetProfileFacetValueKind Kind,
    IReadOnlyList<TargetProfileMeasurementResponse> Measurements);

public sealed record TargetProfileMeasurementResponse(
    string Code,
    int Value,
    string UnitCode);

public sealed record TargetProfileEvidenceResponse(
    string Reference,
    TargetProfileEvidenceSourceKind SourceKind,
    string SourceIdentity,
    string SourceVersion);

public sealed record TargetProfileConflictCandidateResponse(
    TargetProfileFacetValueResponse Value,
    IReadOnlyList<TargetProfileEvidenceResponse> Evidence);

public sealed record TargetProfileUnavailableReasonResponse(
    string Code,
    string? Detail);

public sealed record TargetProfileDiagnosticResponse(
    string Code,
    TargetProfileDiagnosticSeverity Severity,
    TargetProfileFacetReferenceResponse? Facet,
    IReadOnlyList<string> EvidenceReferences);

public sealed record TargetProfileFacetReferenceResponse(
    TargetProfileDimension Dimension,
    string Code);

public sealed record TargetArchetypeMatchResponse(
    string Code,
    string Version,
    string Title,
    string ApplicableProfileRuleVersion,
    TargetArchetypeMatchState State,
    IReadOnlyList<TargetProfileFacetReferenceResponse> SupportingFacets,
    IReadOnlyList<TargetProfileFacetReferenceResponse> MissingFacets,
    IReadOnlyList<TargetProfileFacetReferenceResponse> ExcludingFacets,
    IReadOnlyList<TargetProfileFacetReferenceResponse> ConflictingFacets,
    IReadOnlyList<TargetArchetypeMatchDiagnosticResponse> Diagnostics,
    IReadOnlyList<string> EvidenceReferences);

public sealed record TargetArchetypeMatchDiagnosticResponse(
    string Code,
    string? PredicateCode,
    TargetProfileFacetReferenceResponse? Facet);

public sealed record TargetPlaybookCompositionResponse(
    string ProfileFingerprint,
    IReadOnlyList<TargetPlaybookIdentityResponse> Sources,
    IReadOnlyList<TargetResponseGoalResponse> Goals,
    IReadOnlyList<TargetPlaybookConflictResponse> Conflicts,
    IReadOnlyList<TargetPlaybookGapResponse> Gaps,
    IReadOnlyList<TargetPlaybookCompositionDiagnosticResponse> Diagnostics);

public sealed record TargetPlaybookIdentityResponse(
    string ArchetypeCode,
    string ArchetypeVersion,
    string PlaybookVersion,
    IReadOnlyList<string> EvidenceReferences);

public sealed record TargetResponseGoalResponse(
    string Code,
    string Title,
    int Sequence,
    TargetResponsePriority Priority,
    CombatCounterActivationTiming ResponseTiming,
    bool IsEligible,
    IReadOnlyList<string> SourcePlaybookReferences,
    IReadOnlyList<TargetProfileFacetReferenceResponse> ProfileFacets,
    IReadOnlyList<string> ThreatReferences,
    IReadOnlyList<TargetCounterOptionResponse> Options,
    IReadOnlyList<string> ConflictGroups,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<TargetPlaybookGapResponse> KnownGaps);

public sealed record TargetCounterOptionResponse(
    string Code,
    int SkillId,
    string? SkillName,
    PracticeDirection RequiredDirection,
    int RawEffectId,
    CombatCounterStrength Strength,
    CombatCounterActivationTiming ActivationTiming,
    IReadOnlyList<string> ThreatReferences,
    IReadOnlyList<TargetCombatRequirementResponse> Requirements,
    IReadOnlyList<string> SourcePlaybookReferences,
    IReadOnlyList<string> SourceGoalCodes,
    IReadOnlyList<string> ConflictGroups);

public enum TargetCombatRequirementKind
{
    Weapon,
    Trick,
    Range,
    Resource,
    WeaponUnlock,
    SkillActivation
}

public sealed record TargetCombatRequirementResponse(
    TargetCombatRequirementKind Kind,
    CombatRequirementCriticality Criticality,
    string EvidenceReference,
    int? WeaponTypeId = null,
    int? TrickTypeId = null,
    int? MinimumCount = null,
    int? MinimumRangeInclusive = null,
    int? MaximumRangeInclusive = null,
    CombatResourceKind? Resource = null,
    int? MinimumAmount = null,
    int? SkillId = null,
    SkillActivationState? RequiredSkillState = null);

public sealed record TargetPlaybookGapResponse(
    string Code,
    TargetCounterPlaybookGapKind Kind,
    string Message,
    string? RelatedCounterCode,
    IReadOnlyList<string> EvidenceReferences);

public sealed record TargetPlaybookConflictResponse(
    TargetPlaybookCompositionConflictKind Kind,
    string ConflictGroup,
    IReadOnlyList<string> GoalCodes,
    IReadOnlyList<string> OptionCodes);

public sealed record TargetPlaybookCompositionDiagnosticResponse(
    string Code,
    string ArchetypeCode,
    string ArchetypeVersion,
    TargetArchetypeMatchState? MatchState,
    TargetCounterPlaybookResolutionStatus? ResolutionStatus);

public sealed record TargetPlaybookAdjustmentSetResponse(
    string ProfileFingerprint,
    IReadOnlyList<TargetPlaybookAdjustmentResponse> Items,
    IReadOnlyList<TargetPlaybookAdjustmentDiagnosticResponse> Diagnostics);

public sealed record TargetPlaybookAdjustmentResponse(
    string RuleCode,
    TargetPlaybookAdjustmentAction Action,
    TargetPlaybookResponseReferenceResponse? OriginalResponse,
    TargetPlaybookResponseReferenceResponse? ResultResponse,
    string ReasonCode,
    string Reason,
    IReadOnlyList<TargetPlaybookAdjustmentEvidenceResponse> Evidence);

public sealed record TargetPlaybookResponseReferenceResponse(
    TargetPlaybookResponseReferenceKind Kind,
    string Code);

public sealed record TargetPlaybookAdjustmentEvidenceResponse(
    TargetPlaybookAdjustmentEvidenceKind Kind,
    TargetPlaybookAdjustmentEvidenceState State,
    string Identity,
    IReadOnlyList<string> EvidenceReferences);

public sealed record TargetPlaybookAdjustmentDiagnosticResponse(
    string Code,
    string RuleCode,
    IReadOnlyList<string> EvidenceIdentities);

public sealed record TargetCounterAvailabilityResponse(
    string CounterCode,
    TargetPlaybookCounterAvailabilityState State,
    IReadOnlyList<TargetCounterAccessIssueResponse> AccessIssues,
    IReadOnlyList<TargetCounterGenerationDiagnosticResponse>
        GenerationDiagnostics,
    TargetPlaybookGapResponse? Gap);

public sealed record TargetCounterAccessIssueResponse(
    CombatCounterAccessIssueCode Code,
    string Reason);

public sealed record TargetCounterGenerationDiagnosticResponse(
    CombatLoadoutGenerationDiagnosticCode Code,
    int Occurrences,
    string Reason);
