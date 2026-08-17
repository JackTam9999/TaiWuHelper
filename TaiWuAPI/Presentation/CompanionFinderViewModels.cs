using TaiWu.Application.CompanionCandidates;
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

public sealed record CompanionCandidateGateViewModel(
    string Outcome,
    string Explanation,
    bool Passed);

public sealed record CompanionCandidateViewModel(
    int CharacterId,
    string DisplayName,
    string LocationName,
    CompanionCandidateSection Section,
    CompanionRoleCandidateRankingState RankingState,
    string RankingStateLabel,
    int? CompetitionRank,
    string RankLabel,
    decimal? RoleLocalScore,
    string ScoreLabel,
    string EvidenceLabel,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<CompanionCandidateGateViewModel> Gates);

public sealed record CompanionFinderViewModel(
    CompanionFinderStatus Status,
    string DisciplineName,
    string RoleLabel,
    string RolePurpose,
    string ScoreLimitation,
    DateTimeOffset SnapshotCapturedAtUtc,
    CompanionFinderCountsViewModel Counts,
    IReadOnlyList<CompanionCandidateViewModel> Candidates,
    bool IsPartial,
    bool IsEmpty);

public sealed record CompanionComparisonFactViewModel(
    string Label,
    string FirstValue,
    string SecondValue);

public sealed record CompanionComparisonViewModel(
    string FirstCandidateName,
    string SecondCandidateName,
    string Outcome,
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
