using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWuAPI.Presentation;

public enum CompanionCandidateSection
{
    Ranked = 0,
    NeedsReview = 1,
    Ineligible = 2
}

public sealed record CompanionFinderRoleOptionViewModel(
    string Identity,
    string Version,
    CandidateDisciplineDomain Domain,
    bool RequiresDisciplineSelection,
    string Label,
    string Purpose,
    string ScoreLimitation);

public sealed record CompanionDisciplineOptionViewModel(
    CandidateDisciplineDomain Domain,
    short Type,
    string DisplayName,
    bool NameAvailable);

public sealed record CompanionFinderCountsViewModel(
    int Total,
    int Eligible,
    int Ranked,
    int Tied,
    int NeedsReview,
    int Ineligible,
    int Incomplete,
    int Unsupported,
    int Conflicting);

public sealed record CompanionFinderEnrichmentViewModel(
    CompanionCandidateEnrichmentStatus Status,
    CombatSkillCatalogueStatus CatalogueStatus,
    string Title,
    string Message,
    bool NeedsAttention);

public sealed record CompanionCandidateGateViewModel(
    int Order,
    string RequirementIdentity,
    CompanionRoleRequirementKind Kind,
    CandidateProfileField? Field,
    string RequirementLabel,
    CompanionRoleGateOutcome Outcome,
    string OutcomeLabel,
    string ReasonIdentity,
    string Explanation,
    bool Passed);

public sealed record CompanionCapabilityTopValueViewModel(
    string Label,
    short Value);

public sealed record CompanionCapabilityCategoryViewModel(
    CompanionCapabilityCategory Category,
    CompanionCapabilitySummaryState State,
    string Label,
    string ScoreLabel,
    string CoverageLabel,
    IReadOnlyList<CompanionCapabilityTopValueViewModel> TopValues);

public sealed record CompanionCapabilitySummaryViewModel(
    CompanionCapabilitySummaryState State,
    string RuleVersion,
    CompanionCapabilitySummaryFormula Formula,
    string BreadthIndexLabel,
    CompanionCapabilityCategoryViewModel MainAttributes,
    CompanionCapabilityCategoryViewModel MartialDisciplines,
    CompanionCapabilityCategoryViewModel LifeSkillDisciplines);

public sealed record CompanionCandidateViewModel(
    int CharacterId,
    string DisplayName,
    string LocationName,
    CompanionCandidateSection Section,
    CompanionRoleCandidateRankingState RankingState,
    string RankingStateLabel,
    CompanionRoleEvaluationState EvaluationState,
    string EvaluationStateLabel,
    int? CompetitionRank,
    string RankLabel,
    decimal? RoleLocalScore,
    string ScoreLabel,
    string EvidenceLabel,
    CompanionCapabilitySummaryViewModel CapabilitySummary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<CompanionCandidateGateViewModel> Gates);

public sealed record CompanionFinderViewModel(
    CompanionFinderStatus Status,
    string DisciplineName,
    string RoleLabel,
    bool RequiresDisciplineSelection,
    string ScoreColumnLabel,
    string RolePurpose,
    string ScoreLimitation,
    DateTimeOffset SnapshotCapturedAtUtc,
    CompanionCandidateSnapshotReadStatus SnapshotReadStatus,
    CompanionFinderEnrichmentViewModel Enrichment,
    CompanionFinderCountsViewModel Counts,
    IReadOnlyList<CompanionCandidateViewModel> Candidates,
    bool IsPartial,
    bool IsEmpty);

public sealed record CompanionComparisonFactViewModel(
    string Label,
    string FirstValue,
    string SecondValue);

public sealed record CompanionCapabilityComparisonFactViewModel(
    string Label,
    string FirstValue,
    string FirstDetail,
    string SecondValue,
    string SecondDetail);

public sealed record CompanionCapabilityComparisonViewModel(
    string Title,
    string Limitation,
    IReadOnlyList<CompanionCapabilityComparisonFactViewModel> Facts);

public sealed record CompanionComparisonViewModel(
    string FirstCandidateName,
    string SecondCandidateName,
    string Outcome,
    CompanionCapabilityComparisonViewModel Capability,
    IReadOnlyList<CompanionComparisonFactViewModel> Facts);

public enum CompanionFinderNoticeStatus
{
    Initial = 0,
    Loading = 1,
    Cancelled = 2,
    Failure = 3
}

public sealed record CompanionFinderNoticeViewModel(
    CompanionFinderNoticeStatus Status,
    string Title,
    string Message,
    bool CanRetry);
