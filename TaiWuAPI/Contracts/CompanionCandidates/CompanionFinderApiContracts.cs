using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWuAPI.Contracts.CompanionCandidates;

public enum CompanionRolePresetStatus
{
    Supported = 0
}

public enum CompanionApiDiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}

public enum CompanionFactEvidenceState
{
    Confirmed = 0,
    Missing = 1,
    Incomplete = 2,
    Unsupported = 3,
    Stale = 4,
    Conflicting = 5
}

public sealed record CompanionRoleDiscoveryResponse(
    TaiwuLanguage Language,
    IReadOnlyList<CompanionRolePresetResponse> Roles);

public sealed record CompanionRolePresetResponse(
    string Reference,
    string Identity,
    string RoleVersion,
    string EvaluationRuleVersion,
    CompanionRolePresetStatus Status,
    CandidateDisciplineDomain DisciplineDomain,
    short MinimumDisciplineType,
    short MaximumDisciplineType,
    string Purpose,
    [property: Description("Role-local evidence only; never a universal ranking, probability, or action recommendation.")]
    string ScoreLimitation);

public sealed class CompanionFinderApiRequest
{
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string RoleIdentity { get; init; } = string.Empty;

    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string RoleVersion { get; init; } = string.Empty;

    public CandidateDisciplineDomain DisciplineDomain { get; init; }

    [Range(0, short.MaxValue)]
    public short DisciplineType { get; init; }

    public CompanionRoleShortlistFilter Filter { get; init; } =
        CompanionRoleShortlistFilter.All;

    [Range(1, int.MaxValue)]
    public int? FirstComparisonCharacterId { get; init; }

    [Range(1, int.MaxValue)]
    public int? SecondComparisonCharacterId { get; init; }

    public TaiwuLanguage Language { get; init; } = TaiwuLanguage.English;

    internal CompanionFinderRequest ToApplication() => new(
        RoleIdentity,
        RoleVersion,
        DisciplineDomain,
        DisciplineType,
        Filter,
        FirstComparisonCharacterId,
        SecondComparisonCharacterId);
}

public sealed record CompanionFinderResponse(
    CompanionFinderStatus Status,
    CompanionFinderFailureResponse? Failure,
    string? Fingerprint,
    CompanionFinderSourceResponse? Source,
    CompanionRoleContextResponse? Role,
    CompanionEnrichmentSummaryResponse? Enrichment,
    CompanionShortlistCountsResponse? Counts,
    IReadOnlyList<CompanionCandidateResponse> Candidates,
    IReadOnlyList<string> VisibleCandidateReferences,
    CompanionComparisonResponse? Comparison,
    IReadOnlyList<CompanionApiDiagnosticResponse> Diagnostics);

public sealed record CompanionFinderFailureResponse(
    string Identity,
    string Message);

public sealed record CompanionFinderSourceResponse(
    DateTimeOffset SnapshotCapturedAtUtc,
    string SaveFingerprint,
    string GameDataVersion,
    string ProfileMappingVersion,
    string DisciplineCatalogueVersion,
    string FingerprintSchemaVersion,
    CombatSkillCatalogueStatus CatalogueStatus,
    CompanionCatalogueSourceResponse? CatalogueSource);

public sealed record CompanionCatalogueSourceResponse(
    string GameDataVersion,
    int ImporterVersion,
    string GameDataFingerprint,
    string TraditionalChineseFingerprint,
    string EnglishFingerprint,
    string TraditionalChineseSpecialEffectFingerprint,
    string EnglishSpecialEffectFingerprint);

public sealed record CompanionRoleContextResponse(
    string Reference,
    string Identity,
    string RoleVersion,
    string EvaluationRuleVersion,
    CandidateDisciplineDomain DisciplineDomain,
    short DisciplineType,
    string Purpose,
    [property: Description("Role-local evidence only; never a universal ranking, probability, or action recommendation.")]
    string ScoreLimitation);

public sealed record CompanionEnrichmentSummaryResponse(
    CompanionCandidateEnrichmentStatus Status,
    CombatSkillCatalogueStatus CatalogueStatus);

public sealed record CompanionShortlistCountsResponse(
    int Total,
    int Ranked,
    int Tied,
    int Ineligible,
    int Incomplete,
    int Unsupported,
    int Conflicting,
    int Visible);

public sealed record CompanionCandidateResponse(
    string Reference,
    int CharacterId,
    string? DisplayName,
    string? LocationName,
    CompanionRoleCandidateRankingState RankingState,
    string RankingStateLabel,
    int? CompetitionRank,
    CompanionRoleEvaluationState EvaluationState,
    [property: Description("Nullable role-local total from verified components; not a universal ranking or success probability.")]
    decimal? TotalScore,
    IReadOnlyList<CompanionGateResponse> Gates,
    IReadOnlyList<CompanionScoreComponentResponse> Components,
    IReadOnlyList<CompanionExplanationResponse> Explanations,
    IReadOnlyList<CompanionRoleFactResponse> ScoreFacts,
    IReadOnlyList<CompanionRoleFactResponse> LocationEvidence,
    IReadOnlyList<CompanionRoleFactResponse> AvailableLocationFacts,
    CompanionCandidateEnrichmentResponse Enrichment,
    IReadOnlyList<CompanionApiDiagnosticResponse> Diagnostics);

public sealed record CompanionGateResponse(
    int Order,
    string RequirementIdentity,
    CompanionRoleRequirementKind Kind,
    CandidateProfileField? Field,
    CompanionRoleGateOutcome Outcome,
    string OutcomeLabel,
    string ReasonIdentity,
    string Explanation,
    IReadOnlyList<CompanionEvidenceResponse> Evidence);

public sealed record CompanionScoreComponentResponse(
    string DimensionIdentity,
    CandidateProfileField Field,
    CandidateDisciplineDomain DisciplineDomain,
    short DisciplineType,
    string Unit,
    CompanionRoleScoreDirection Direction,
    CompanionRoleNormalizationKind Normalization,
    decimal NormalizationMinimum,
    decimal NormalizationMaximum,
    short RawValue,
    decimal NormalizedValue,
    decimal Weight,
    decimal Contribution,
    string ExplanationIdentity,
    string Explanation,
    IReadOnlyList<CompanionEvidenceResponse> Evidence);

public sealed record CompanionExplanationResponse(
    CompanionRoleExplanationKind Kind,
    string Identity,
    string Message,
    IReadOnlyList<string> ComponentIdentities,
    IReadOnlyList<string> GateReasonIdentities);

public sealed record CompanionRoleFactResponse(
    CandidateProfileField Field,
    CandidateDisciplineDomain? DisciplineDomain,
    short? DisciplineType,
    CompanionFactEvidenceState EvidenceState,
    CompanionFactValueResponse? Value,
    CompanionProvenanceResponse? Provenance,
    CompanionUnavailableResponse? Unavailable,
    IReadOnlyList<CompanionConflictValueResponse> Conflicts,
    CompanionConflictDecisionResponse? ConflictDecision,
    IReadOnlyList<CompanionEvidenceResponse> Evidence);

public sealed record CompanionFactValueResponse(
    CandidateFactValueKind Kind,
    bool? Boolean,
    short? Int16,
    int? Int32,
    IReadOnlyList<int> Identities);

public sealed record CompanionProvenanceResponse(
    CandidateEvidenceSourceKind SourceKind,
    string SourceIdentity,
    string SourceVersion,
    string RevisionIdentity);

public sealed record CompanionEvidenceResponse(
    string Reference,
    CompanionProvenanceResponse Provenance);

public sealed record CompanionUnavailableResponse(
    string Code,
    string Message);

public sealed record CompanionConflictValueResponse(
    CompanionFactValueResponse Value,
    CompanionProvenanceResponse Provenance,
    IReadOnlyList<CompanionEvidenceResponse> Evidence);

public sealed record CompanionConflictDecisionResponse(
    CandidateConflictDecisionKind Kind,
    string RationaleIdentity,
    CompanionProvenanceResponse? SelectedProvenance);

public sealed record CompanionCandidateEnrichmentResponse(
    CompanionCandidateEnrichmentState State,
    CompanionMembershipEvidenceState LearnedMartialState,
    CompanionMembershipEvidenceState EquippedMartialState,
    CompanionMembershipEvidenceState LearnedLifeSkillState,
    IReadOnlyList<CompanionSkillEnrichmentResponse> CombatSkills,
    IReadOnlyList<CompanionApiDiagnosticResponse> Diagnostics);

public sealed record CompanionSkillEnrichmentResponse(
    int SkillId,
    CompanionSkillDefinitionState DefinitionState,
    CompanionDetailedProgressState DetailedProgressState,
    CompanionMembershipResponse Learned,
    CompanionMembershipResponse Equipped);

public sealed record CompanionMembershipResponse(
    CompanionMembershipEvidenceState State,
    bool? Value);

public sealed record CompanionComparisonResponse(
    string FirstCandidateReference,
    string SecondCandidateReference,
    CompanionRoleComparisonOutcome Outcome,
    string OutcomeLabel,
    IReadOnlyList<CompanionComparisonRowResponse> Rows);

public sealed record CompanionComparisonRowResponse(
    string DimensionIdentity,
    CandidateProfileField Field,
    CompanionRoleComparisonOutcome Outcome,
    string OutcomeLabel,
    CompanionComparisonValueResponse First,
    CompanionComparisonValueResponse Second);

public sealed record CompanionComparisonValueResponse(
    CompanionRoleComparisonEvidenceState State,
    short? Value,
    CompanionRoleFactResponse? Fact);

public sealed record CompanionApiDiagnosticResponse(
    string Scope,
    string Identity,
    CompanionApiDiagnosticSeverity Severity,
    string Message,
    string? CandidateReference);
