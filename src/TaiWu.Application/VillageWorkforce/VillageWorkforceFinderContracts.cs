using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Application.VillageWorkforce;

public enum VillageWorkforceFinderStatus
{
    Complete = 0,
    Partial = 1,
    InvalidRequest = 2,
    SaveUnavailable = 3,
    UnsupportedSourceVersion = 4,
    ConflictingSources = 5,
    ChangedRevision = 6,
    ReadFailed = 7,
    TargetNotFound = 8,
    UnsupportedRule = 9,
    InvalidComparison = 10,
    InvalidProposal = 11
}

public interface IFindVillageWorkforce
{
    Task<VillageWorkforceFinderResult> ExecuteAsync(
        VillageWorkforceFinderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class VillageWorkforceFinderRequest
{
    public VillageWorkforceFinderRequest(
        ShopManagerTargetIdentity target,
        WorkforceObjectiveIdentity objective,
        WorkforceShortlistFilter filter = WorkforceShortlistFilter.All,
        VillageWorkerIdentity? firstComparisonWorker = null,
        VillageWorkerIdentity? secondComparisonWorker = null,
        VillageWorkerIdentity? proposedWorker = null)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Objective = objective
            ?? throw new ArgumentNullException(nameof(objective));
        Filter = filter;
        FirstComparisonWorker = firstComparisonWorker;
        SecondComparisonWorker = secondComparisonWorker;
        ProposedWorker = proposedWorker;
    }

    public ShopManagerTargetIdentity Target { get; }

    public WorkforceObjectiveIdentity Objective { get; }

    public WorkforceShortlistFilter Filter { get; }

    public VillageWorkerIdentity? FirstComparisonWorker { get; }

    public VillageWorkerIdentity? SecondComparisonWorker { get; }

    public VillageWorkerIdentity? ProposedWorker { get; }
}

public sealed class VillageWorkforceFinderResult
{
    private VillageWorkforceFinderResult(
        VillageWorkforceFinderStatus status,
        VillageWorkforceSnapshotReadStatus? snapshotReadStatus,
        WorkforceRuleResolutionStatus? ruleResolutionStatus,
        VillageWorkforceSnapshot? snapshot,
        WorkforceRuleDefinition? rule,
        VillageWorkforceEvaluationSet? evaluationSet,
        VillageWorkforceShortlist? shortlist,
        VillageWorkforceShortlistView? view,
        WorkforceComparison? comparison,
        VillageWorkforceManualPlan? manualPlan,
        string? failureIdentity)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown village-workforce finder status.");
        }

        var authoritative = snapshot is not null;
        if (authoritative != (rule is not null)
            || authoritative != (evaluationSet is not null)
            || authoritative != (shortlist is not null)
            || authoritative != (view is not null))
        {
            throw new ArgumentException(
                "An authoritative workforce result requires one complete immutable source chain.");
        }

        if (authoritative)
        {
            if (snapshotReadStatus is not (
                    VillageWorkforceSnapshotReadStatus.Complete
                    or VillageWorkforceSnapshotReadStatus.Partial)
                || ruleResolutionStatus
                    != WorkforceRuleResolutionStatus.Resolved
                || !ReferenceEquals(evaluationSet!.Rule, rule)
                || !ReferenceEquals(shortlist!.EvaluationSet, evaluationSet)
                || !string.Equals(
                    view!.ResultFingerprint,
                    shortlist.Fingerprint,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Workforce result payloads do not share one immutable source chain.");
            }

            if (comparison is not null
                && (!evaluationSet.Evaluations.Contains(comparison.First)
                    || !evaluationSet.Evaluations.Contains(comparison.Second)))
            {
                throw new ArgumentException(
                    "A workforce comparison must use this result's evaluations.");
            }

            if (manualPlan is not null
                && manualPlan.ResultIdentity != evaluationSet.ResultIdentity)
            {
                throw new ArgumentException(
                    "A workforce manual plan must use this result identity.");
            }

            var invalidSelection = status is
                VillageWorkforceFinderStatus.InvalidComparison
                or VillageWorkforceFinderStatus.InvalidProposal;
            if (invalidSelection != (failureIdentity is not null)
                || status is not (
                    VillageWorkforceFinderStatus.Complete
                    or VillageWorkforceFinderStatus.Partial
                    or VillageWorkforceFinderStatus.InvalidComparison
                    or VillageWorkforceFinderStatus.InvalidProposal))
            {
                throw new ArgumentException(
                    "The authoritative workforce status is incompatible with its selection state.");
            }

            var needsReview = snapshotReadStatus
                    == VillageWorkforceSnapshotReadStatus.Partial
                || shortlist.Counts.Incomplete > 0
                || shortlist.Counts.Unsupported > 0
                || shortlist.Counts.Conflicting > 0;
            if (status == VillageWorkforceFinderStatus.Complete && needsReview
                || status == VillageWorkforceFinderStatus.Partial
                    && !needsReview)
            {
                throw new ArgumentException(
                    "Workforce completion status must match source and evaluation completeness.");
            }
        }
        else if (status is VillageWorkforceFinderStatus.Complete
                or VillageWorkforceFinderStatus.Partial
                or VillageWorkforceFinderStatus.InvalidComparison
                or VillageWorkforceFinderStatus.InvalidProposal
            || comparison is not null
            || manualPlan is not null
            || failureIdentity is null)
        {
            throw new ArgumentException(
                "A failed workforce result has an incompatible payload.");
        }

        Status = status;
        SnapshotReadStatus = snapshotReadStatus;
        RuleResolutionStatus = ruleResolutionStatus;
        Snapshot = snapshot;
        Rule = rule;
        EvaluationSet = evaluationSet;
        Shortlist = shortlist;
        View = view;
        Comparison = comparison;
        ManualPlan = manualPlan;
        FailureIdentity = failureIdentity;
        if (authoritative)
        {
            Fingerprint = CreateFingerprint();
        }
    }

    public VillageWorkforceFinderStatus Status { get; }

    public VillageWorkforceSnapshotReadStatus? SnapshotReadStatus { get; }

    public WorkforceRuleResolutionStatus? RuleResolutionStatus { get; }

    public VillageWorkforceSnapshot? Snapshot { get; }

    public WorkforceRuleDefinition? Rule { get; }

    public VillageWorkforceEvaluationSet? EvaluationSet { get; }

    public VillageWorkforceShortlist? Shortlist { get; }

    public VillageWorkforceShortlistView? View { get; }

    public WorkforceComparison? Comparison { get; }

    public VillageWorkforceManualPlan? ManualPlan { get; }

    public string? FailureIdentity { get; }

    public string? Fingerprint { get; }

    public bool HasAuthoritativeResult => Snapshot is not null;

    internal static VillageWorkforceFinderResult Authoritative(
        VillageWorkforceFinderStatus status,
        VillageWorkforceSnapshotReadStatus snapshotReadStatus,
        VillageWorkforceSnapshot snapshot,
        WorkforceRuleDefinition rule,
        VillageWorkforceEvaluationSet evaluationSet,
        VillageWorkforceShortlist shortlist,
        VillageWorkforceShortlistView view,
        WorkforceComparison? comparison,
        VillageWorkforceManualPlan? manualPlan) =>
        new(
            status,
            snapshotReadStatus,
            WorkforceRuleResolutionStatus.Resolved,
            snapshot,
            rule,
            evaluationSet,
            shortlist,
            view,
            comparison,
            manualPlan,
            failureIdentity: null);

    internal static VillageWorkforceFinderResult InvalidSelection(
        VillageWorkforceFinderStatus status,
        VillageWorkforceSnapshotReadStatus snapshotReadStatus,
        VillageWorkforceSnapshot snapshot,
        WorkforceRuleDefinition rule,
        VillageWorkforceEvaluationSet evaluationSet,
        VillageWorkforceShortlist shortlist,
        VillageWorkforceShortlistView view,
        string failureIdentity) =>
        new(
            status,
            snapshotReadStatus,
            WorkforceRuleResolutionStatus.Resolved,
            snapshot,
            rule,
            evaluationSet,
            shortlist,
            view,
            comparison: null,
            manualPlan: null,
            StableFailure(failureIdentity));

    internal static VillageWorkforceFinderResult Failed(
        VillageWorkforceFinderStatus status,
        string failureIdentity,
        VillageWorkforceSnapshotReadStatus? snapshotReadStatus = null,
        WorkforceRuleResolutionStatus? ruleResolutionStatus = null) =>
        new(
            status,
            snapshotReadStatus,
            ruleResolutionStatus,
            snapshot: null,
            rule: null,
            evaluationSet: null,
            shortlist: null,
            view: null,
            comparison: null,
            manualPlan: null,
            StableFailure(failureIdentity));

    private static string StableFailure(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.IndexOfAny(['|', '/', '\\', '\r', '\n']) >= 0)
        {
            throw new ArgumentException(
                "A finder failure requires a stable path-free identity.",
                nameof(value));
        }

        return value.Trim();
    }

    private string CreateFingerprint()
    {
        var snapshot = Snapshot!;
        var evaluationSet = EvaluationSet!;
        var canonical = new StringBuilder()
            .Append("VILLAGE_WORKFORCE_FINDER_RESULT_V1\n")
            .Append(snapshot.Fingerprint).Append('|')
            .Append(snapshot.Targets.Single(item =>
                item.Identity == evaluationSet.ResultIdentity.Target)
                .Fingerprint).Append('|')
            .Append((int)evaluationSet.ResultIdentity.Objective.Kind).Append('|')
            .Append(evaluationSet.ResultIdentity.Objective.Version).Append('|')
            .Append(Rule!.Fingerprint).Append('|')
            .Append(evaluationSet.Fingerprint).Append('|')
            .Append(Shortlist!.Fingerprint).Append('\n');
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
