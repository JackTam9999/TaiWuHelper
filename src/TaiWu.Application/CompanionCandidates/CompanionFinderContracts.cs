using System.Security.Cryptography;
using System.Text;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWu.Application.CompanionCandidates;

public enum CompanionFinderStatus
{
    Complete = 0,
    Partial = 1,
    Empty = 2,
    InvalidRequest = 3,
    UnknownRole = 4,
    UnsupportedRoleVersion = 5,
    SaveUnavailable = 6,
    UnsupportedSourceVersion = 7,
    ChangedRevision = 8,
    ReadFailed = 9,
    InvalidComparison = 10,
    Failed = 11
}

public interface IFindCompanionCandidates
{
    Task<CompanionFinderResult> ExecuteAsync(
        CompanionFinderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CompanionFinderRequest
{
    public CompanionFinderRequest(
        string roleIdentity,
        string roleVersion,
        CandidateDisciplineDomain disciplineDomain,
        short disciplineType,
        CompanionRoleShortlistFilter filter = CompanionRoleShortlistFilter.All,
        int? firstComparisonCharacterId = null,
        int? secondComparisonCharacterId = null)
    {
        RoleIdentity = roleIdentity;
        RoleVersion = roleVersion;
        DisciplineDomain = disciplineDomain;
        DisciplineType = disciplineType;
        Filter = filter;
        FirstComparisonCharacterId = firstComparisonCharacterId;
        SecondComparisonCharacterId = secondComparisonCharacterId;
    }

    public string RoleIdentity { get; }

    public string RoleVersion { get; }

    public CandidateDisciplineDomain DisciplineDomain { get; }

    public short DisciplineType { get; }

    public CompanionRoleShortlistFilter Filter { get; }

    public int? FirstComparisonCharacterId { get; }

    public int? SecondComparisonCharacterId { get; }
}

public sealed class CompanionFinderSourceIdentity
{
    internal CompanionFinderSourceIdentity(
        CompanionCandidateSnapshot snapshot,
        CompanionCandidateEnrichmentResult enrichment,
        CompanionRoleDefinition definition,
        CandidateDisciplineIdentity discipline)
    {
        SnapshotCapturedAtUtc = snapshot.CapturedAtUtc;
        CandidateSourceVersions = snapshot.SourceVersions;
        CatalogueStatus = enrichment.CatalogueStatus;
        CatalogueSource = enrichment.CatalogueSource;
        RoleIdentity = definition.Identity;
        RoleVersion = definition.RoleVersion;
        EvaluationRuleVersion = definition.EvaluationRuleVersion;
        Discipline = discipline;
    }

    public DateTimeOffset SnapshotCapturedAtUtc { get; }

    public CandidateProfileSourceVersions CandidateSourceVersions { get; }

    public CombatSkillCatalogueStatus CatalogueStatus { get; }

    public CombatSkillCatalogueSourceIdentity? CatalogueSource { get; }

    public CompanionRoleIdentity RoleIdentity { get; }

    public string RoleVersion { get; }

    public string EvaluationRuleVersion { get; }

    public CandidateDisciplineIdentity Discipline { get; }
}

public sealed class CompanionFinderResult
{
    private CompanionFinderResult(
        CompanionFinderStatus status,
        CompanionCandidateSnapshotReadStatus? snapshotReadStatus,
        CompanionCandidateSnapshot? snapshot,
        CompanionCandidateEnrichmentResult? enrichment,
        CompanionRoleRanking? ranking,
        CompanionRoleShortlist? shortlist,
        CompanionRoleShortlistView? view,
        CompanionRoleComparison? comparison,
        string? failureIdentity)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown companion-finder status.");
        }

        var hasAuthoritativeResult = snapshot is not null;
        if (hasAuthoritativeResult != (enrichment is not null)
            || hasAuthoritativeResult != (ranking is not null)
            || hasAuthoritativeResult != (shortlist is not null)
            || hasAuthoritativeResult != (view is not null))
        {
            throw new ArgumentException("Finder source and authoritative result payloads are incompatible.");
        }

        if (hasAuthoritativeResult)
        {
            if (snapshotReadStatus is not (CompanionCandidateSnapshotReadStatus.Complete
                    or CompanionCandidateSnapshotReadStatus.Partial)
                || !ReferenceEquals(enrichment!.Snapshot, snapshot)
                || !ReferenceEquals(shortlist!.Ranking, ranking)
                || !ReferenceEquals(view!.Source, shortlist)
                || comparison is not null
                    && !ReferenceEquals(comparison.Shortlist, shortlist))
            {
                throw new ArgumentException("Finder result payloads do not share one immutable source chain.");
            }

            if (status == CompanionFinderStatus.Empty && shortlist.Counts.Total != 0
                || status == CompanionFinderStatus.Complete && shortlist.Counts.Total == 0)
            {
                throw new ArgumentException("Finder completion state and candidate count are incompatible.");
            }

            if (status == CompanionFinderStatus.InvalidComparison)
            {
                if (failureIdentity is null || comparison is not null)
                {
                    throw new ArgumentException("An invalid comparison requires one typed failure and no comparison.");
                }
            }
            else if (status is not (CompanionFinderStatus.Complete
                         or CompanionFinderStatus.Partial
                         or CompanionFinderStatus.Empty)
                     || failureIdentity is not null)
            {
                throw new ArgumentException("An authoritative finder result has an incompatible status.");
            }
        }
        else if (status is CompanionFinderStatus.Complete
                     or CompanionFinderStatus.Partial
                     or CompanionFinderStatus.Empty
                     or CompanionFinderStatus.InvalidComparison
                 || snapshotReadStatus is CompanionCandidateSnapshotReadStatus.Complete
                     or CompanionCandidateSnapshotReadStatus.Partial
                 || comparison is not null
                 || failureIdentity is null)
        {
            throw new ArgumentException("A failed finder result has an incompatible payload.");
        }

        Status = status;
        SnapshotReadStatus = snapshotReadStatus;
        Snapshot = snapshot;
        Enrichment = enrichment;
        Ranking = ranking;
        Shortlist = shortlist;
        View = view;
        Comparison = comparison;
        FailureIdentity = failureIdentity;
        if (hasAuthoritativeResult)
        {
            SourceIdentity = new CompanionFinderSourceIdentity(
                snapshot!,
                enrichment!,
                ranking!.Definition,
                ranking.Discipline);
            Fingerprint = CreateFingerprint();
        }
    }

    public CompanionFinderStatus Status { get; }

    public CompanionCandidateSnapshotReadStatus? SnapshotReadStatus { get; }

    public CompanionCandidateSnapshot? Snapshot { get; }

    public CompanionCandidateEnrichmentResult? Enrichment { get; }

    public CompanionRoleRanking? Ranking { get; }

    public CompanionRoleShortlist? Shortlist { get; }

    public CompanionRoleShortlistView? View { get; }

    public CompanionRoleComparison? Comparison { get; }

    public CompanionFinderSourceIdentity? SourceIdentity { get; }

    public string? FailureIdentity { get; }

    public string? Fingerprint { get; }

    public bool HasAuthoritativeResult => Snapshot is not null;

    internal static CompanionFinderResult Authoritative(
        CompanionFinderStatus status,
        CompanionCandidateSnapshotReadStatus snapshotReadStatus,
        CompanionCandidateSnapshot snapshot,
        CompanionCandidateEnrichmentResult enrichment,
        CompanionRoleRanking ranking,
        CompanionRoleShortlist shortlist,
        CompanionRoleShortlistView view,
        CompanionRoleComparison? comparison) => new(
            status,
            snapshotReadStatus,
            snapshot,
            enrichment,
            ranking,
            shortlist,
            view,
            comparison,
            failureIdentity: null);

    internal static CompanionFinderResult InvalidComparison(
        CompanionCandidateSnapshotReadStatus snapshotReadStatus,
        CompanionCandidateSnapshot snapshot,
        CompanionCandidateEnrichmentResult enrichment,
        CompanionRoleRanking ranking,
        CompanionRoleShortlist shortlist,
        CompanionRoleShortlistView view,
        string failureIdentity) => new(
            CompanionFinderStatus.InvalidComparison,
            snapshotReadStatus,
            snapshot,
            enrichment,
            ranking,
            shortlist,
            view,
            comparison: null,
            StableFailure(failureIdentity));

    internal static CompanionFinderResult Failed(
        CompanionFinderStatus status,
        string failureIdentity,
        CompanionCandidateSnapshotReadStatus? snapshotReadStatus = null) => new(
            status,
            snapshotReadStatus,
            snapshot: null,
            enrichment: null,
            ranking: null,
            shortlist: null,
            view: null,
            comparison: null,
            StableFailure(failureIdentity));

    private static string StableFailure(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.IndexOfAny(['|', '/', '\\', '\r', '\n']) >= 0)
        {
            throw new ArgumentException("A finder failure requires a stable path-free identity.", nameof(value));
        }

        return value.Trim();
    }

    private string CreateFingerprint()
    {
        var versions = Snapshot!.SourceVersions;
        var canonical = new StringBuilder()
            .Append("COMPANION_FINDER_RESULT_V1\n")
            .Append(versions.SaveSha256).Append('|')
            .Append(versions.GameDataVersion).Append('|')
            .Append(versions.ProfileMappingVersion).Append('|')
            .Append(versions.DisciplineCatalogVersion).Append('|')
            .Append(versions.FingerprintSchemaVersion).Append('\n')
            .Append(Enrichment!.Fingerprint).Append('\n')
            .Append(Shortlist!.Fingerprint).Append('\n');
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
