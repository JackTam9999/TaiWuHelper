using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWu.Application.CompanionCandidates;

public sealed class FindCompanionCandidates(
    ICompanionCandidateSnapshotReader snapshotReader,
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository catalogueRepository,
    CombatSkillCatalogueMaintenanceCoordinator? coordinator = null)
    : IFindCompanionCandidates
{
    public async Task<CompanionFinderResult> ExecuteAsync(
        CompanionFinderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var validated = Validate(request);
        if (validated.Failure is not null)
        {
            return validated.Failure;
        }

        var definition = validated.Definition!;
        var discipline = validated.Discipline!;
        var read = await snapshotReader.ReadAsync(
                CompanionCandidateSnapshotReadRequest.Current,
                cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (read.Snapshot is null)
        {
            return MapReadFailure(read.Status);
        }

        var snapshot = read.Snapshot;
        var enrichment = await new EnrichCompanionCandidateProfiles(
                definitionSource,
                catalogueRepository,
                coordinator)
            .ExecuteAsync(snapshot, cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        var ranking = CompanionRoleShortlistBuilder.EvaluateAndRank(
            definition,
            discipline,
            enrichment.Candidates.Select(item => item.Profile),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var shortlist = CompanionRoleShortlistFactory.Create(ranking);
        var view = CompanionRoleShortlistFilterer.CreateView(
            shortlist,
            request.Filter);

        CompanionRoleComparison? comparison = null;
        if (request.FirstComparisonCharacterId.HasValue)
        {
            var firstCharacterId = request.FirstComparisonCharacterId.Value;
            var secondCharacterId = request.SecondComparisonCharacterId!.Value;
            if (!shortlist.Entries.Any(item =>
                    item.Evaluation.Profile.Identity.CharacterId == firstCharacterId)
                || !shortlist.Entries.Any(item =>
                    item.Evaluation.Profile.Identity.CharacterId == secondCharacterId))
            {
                return CompanionFinderResult.InvalidComparison(
                    read.Status,
                    snapshot,
                    enrichment,
                    ranking,
                    shortlist,
                    view,
                    "COMPARISON_CANDIDATE_NOT_FOUND");
            }

            comparison = CompanionRoleComparisonBuilder.Compare(
                shortlist,
                firstCharacterId,
                secondCharacterId);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var status = read.Status == CompanionCandidateSnapshotReadStatus.Partial
            || enrichment.Status != CompanionCandidateEnrichmentStatus.Complete
            ? CompanionFinderStatus.Partial
            : shortlist.Counts.Total == 0
                ? CompanionFinderStatus.Empty
                : CompanionFinderStatus.Complete;
        return CompanionFinderResult.Authoritative(
            status,
            read.Status,
            snapshot,
            enrichment,
            ranking,
            shortlist,
            view,
            comparison);
    }

    private static (
        CompanionRoleDefinition? Definition,
        CandidateDisciplineIdentity? Discipline,
        CompanionFinderResult? Failure) Validate(CompanionFinderRequest request)
    {
        CompanionRoleIdentity identity;
        CandidateDisciplineIdentity discipline;
        try
        {
            identity = new CompanionRoleIdentity(request.RoleIdentity);
            discipline = new CandidateDisciplineIdentity(
                request.DisciplineDomain,
                request.DisciplineType);
            if (!Enum.IsDefined(request.Filter))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Filter,
                    "Unknown shortlist filter.");
            }

            var hasFirst = request.FirstComparisonCharacterId.HasValue;
            var hasSecond = request.SecondComparisonCharacterId.HasValue;
            if (hasFirst != hasSecond
                || request.FirstComparisonCharacterId <= 0
                || request.SecondComparisonCharacterId <= 0
                || hasFirst
                    && request.FirstComparisonCharacterId == request.SecondComparisonCharacterId)
            {
                throw new ArgumentException("Comparison selection is invalid.");
            }
        }
        catch (ArgumentException)
        {
            return (
                null,
                null,
                CompanionFinderResult.Failed(
                    CompanionFinderStatus.InvalidRequest,
                    "COMPANION_FINDER_REQUEST_INVALID"));
        }

        CompanionRoleDefinitionResolution resolution;
        try
        {
            resolution = VerifiedCompanionRoleDefinitions.Resolve(
                identity,
                request.RoleVersion);
        }
        catch (ArgumentException)
        {
            return (
                null,
                null,
                CompanionFinderResult.Failed(
                    CompanionFinderStatus.InvalidRequest,
                    "COMPANION_FINDER_REQUEST_INVALID"));
        }

        return resolution.State switch
        {
            CompanionRoleDefinitionResolutionState.Supported =>
                (resolution.Definition, discipline, null),
            CompanionRoleDefinitionResolutionState.UnknownIdentity =>
                (null, null, CompanionFinderResult.Failed(
                    CompanionFinderStatus.UnknownRole,
                    resolution.DiagnosticIdentity)),
            CompanionRoleDefinitionResolutionState.UnsupportedVersion =>
                (null, null, CompanionFinderResult.Failed(
                    CompanionFinderStatus.UnsupportedRoleVersion,
                    resolution.DiagnosticIdentity)),
            _ => throw new InvalidOperationException(
                "Unknown role-definition resolution state "
                + $"'{resolution.State}'.")
        };
    }

    private static CompanionFinderResult MapReadFailure(
        CompanionCandidateSnapshotReadStatus status) => status switch
        {
            CompanionCandidateSnapshotReadStatus.SaveUnavailable =>
                CompanionFinderResult.Failed(
                    CompanionFinderStatus.SaveUnavailable,
                    "CANDIDATE_SAVE_UNAVAILABLE",
                    status),
            CompanionCandidateSnapshotReadStatus.UnsupportedVersion =>
                CompanionFinderResult.Failed(
                    CompanionFinderStatus.UnsupportedSourceVersion,
                    "CANDIDATE_SOURCE_VERSION_UNSUPPORTED",
                    status),
            CompanionCandidateSnapshotReadStatus.ChangedRevision =>
                CompanionFinderResult.Failed(
                    CompanionFinderStatus.ChangedRevision,
                    "CANDIDATE_SAVE_REVISION_CHANGED",
                    status),
            CompanionCandidateSnapshotReadStatus.ReadFailed =>
                CompanionFinderResult.Failed(
                    CompanionFinderStatus.ReadFailed,
                    "CANDIDATE_SNAPSHOT_READ_FAILED",
                    status),
            _ => CompanionFinderResult.Failed(
                CompanionFinderStatus.Failed,
                "CANDIDATE_SNAPSHOT_RESULT_INVALID",
                status)
        };
}
