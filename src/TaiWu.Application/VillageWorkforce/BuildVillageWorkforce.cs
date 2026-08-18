using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Application.VillageWorkforce;

/// <summary>
/// Builds one workforce result from an already coherent immutable snapshot.
/// This pure use case lets interactive clients evaluate multiple targets
/// without rereading the configured save.
/// </summary>
public sealed class BuildVillageWorkforce
{
    public VillageWorkforceFinderResult Execute(
        VillageWorkforceSnapshotReadResult read,
        VillageWorkforceFinderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ValidRequest(request))
        {
            return VillageWorkforceFinderResult.Failed(
                VillageWorkforceFinderStatus.InvalidRequest,
                "VILLAGE_WORKFORCE_REQUEST_INVALID");
        }

        if (read.Snapshot is null)
        {
            return MapReadFailure(read.Status);
        }

        var snapshot = read.Snapshot;
        var target = snapshot.Targets.SingleOrDefault(item =>
            item.Identity == request.Target);
        if (target is null)
        {
            return VillageWorkforceFinderResult.Failed(
                VillageWorkforceFinderStatus.TargetNotFound,
                "VILLAGE_WORKFORCE_TARGET_NOT_FOUND",
                read.Status);
        }

        var resolution = VerifiedVillageWorkforceRules.Resolve(
            request.Objective,
            snapshot.SourceVersions,
            target.Identity.Kind,
            target.RequiredDiscipline);
        if (!resolution.IsResolved)
        {
            return VillageWorkforceFinderResult.Failed(
                VillageWorkforceFinderStatus.UnsupportedRule,
                resolution.ReasonIdentity,
                read.Status,
                resolution.Status);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var rule = resolution.Rule!;
        var evaluationSet = VillageWorkforceEvaluator.Evaluate(
            snapshot,
            target.Identity,
            rule);
        cancellationToken.ThrowIfCancellationRequested();
        var shortlist = new VillageWorkforceShortlist(evaluationSet);
        var view = shortlist.ApplyFilter(request.Filter);

        WorkforceComparison? comparison = null;
        if (request.FirstComparisonWorker is not null)
        {
            if (!ContainsWorker(
                    evaluationSet,
                    request.FirstComparisonWorker)
                || !ContainsWorker(
                    evaluationSet,
                    request.SecondComparisonWorker!))
            {
                return VillageWorkforceFinderResult.InvalidSelection(
                    VillageWorkforceFinderStatus.InvalidComparison,
                    read.Status,
                    snapshot,
                    rule,
                    evaluationSet,
                    shortlist,
                    view,
                    "VILLAGE_WORKFORCE_COMPARISON_WORKER_NOT_FOUND");
            }

            comparison = shortlist.Compare(
                request.FirstComparisonWorker,
                request.SecondComparisonWorker!);
        }

        VillageWorkforceManualPlan? manualPlan = null;
        if (request.ProposedWorker is not null)
        {
            var proposed = evaluationSet.Evaluations.SingleOrDefault(item =>
                item.Worker == request.ProposedWorker);
            if (proposed is null
                || !proposed.IsRankable
                || proposed.Worker == evaluationSet.CurrentWorker)
            {
                return VillageWorkforceFinderResult.InvalidSelection(
                    VillageWorkforceFinderStatus.InvalidProposal,
                    read.Status,
                    snapshot,
                    rule,
                    evaluationSet,
                    shortlist,
                    view,
                    "VILLAGE_WORKFORCE_PROPOSAL_INVALID");
            }

            manualPlan = shortlist.CreateManualPlan(request.ProposedWorker);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var status = read.Status == VillageWorkforceSnapshotReadStatus.Partial
            || shortlist.Counts.Incomplete > 0
            || shortlist.Counts.Unsupported > 0
            || shortlist.Counts.Conflicting > 0
                ? VillageWorkforceFinderStatus.Partial
                : VillageWorkforceFinderStatus.Complete;
        return VillageWorkforceFinderResult.Authoritative(
            status,
            read.Status,
            snapshot,
            rule,
            evaluationSet,
            shortlist,
            view,
            comparison,
            manualPlan);
    }

    internal static bool ValidRequest(VillageWorkforceFinderRequest request)
    {
        if (!Enum.IsDefined(request.Filter))
        {
            return false;
        }

        var hasFirst = request.FirstComparisonWorker is not null;
        var hasSecond = request.SecondComparisonWorker is not null;
        return hasFirst == hasSecond
            && (!hasFirst
                || request.FirstComparisonWorker
                    != request.SecondComparisonWorker);
    }

    private static bool ContainsWorker(
        VillageWorkforceEvaluationSet set,
        VillageWorkerIdentity worker) =>
        set.Evaluations.Any(item => item.Worker == worker);

    private static VillageWorkforceFinderResult MapReadFailure(
        VillageWorkforceSnapshotReadStatus status) => status switch
        {
            VillageWorkforceSnapshotReadStatus.SaveUnavailable =>
                VillageWorkforceFinderResult.Failed(
                    VillageWorkforceFinderStatus.SaveUnavailable,
                    "VILLAGE_WORKFORCE_SAVE_UNAVAILABLE",
                    status),
            VillageWorkforceSnapshotReadStatus.UnsupportedVersion =>
                VillageWorkforceFinderResult.Failed(
                    VillageWorkforceFinderStatus.UnsupportedSourceVersion,
                    "VILLAGE_WORKFORCE_SOURCE_VERSION_UNSUPPORTED",
                    status),
            VillageWorkforceSnapshotReadStatus.ConflictingSources =>
                VillageWorkforceFinderResult.Failed(
                    VillageWorkforceFinderStatus.ConflictingSources,
                    "VILLAGE_WORKFORCE_SOURCES_CONFLICTING",
                    status),
            VillageWorkforceSnapshotReadStatus.ChangedRevision =>
                VillageWorkforceFinderResult.Failed(
                    VillageWorkforceFinderStatus.ChangedRevision,
                    "VILLAGE_WORKFORCE_SAVE_REVISION_CHANGED",
                    status),
            VillageWorkforceSnapshotReadStatus.ReadFailed =>
                VillageWorkforceFinderResult.Failed(
                    VillageWorkforceFinderStatus.ReadFailed,
                    "VILLAGE_WORKFORCE_SNAPSHOT_READ_FAILED",
                    status),
            _ => throw new InvalidOperationException(
                $"Snapshot status '{status}' has no failure mapping.")
        };
}
