using System.Collections.Immutable;
using System.Text;

namespace TaiWu.Domain.VillageWorkforce;

public sealed record WorkforceRankedCandidate
{
    public WorkforceRankedCandidate(
        int competitionRank,
        WorkforceEvaluation evaluation)
    {
        if (competitionRank <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(competitionRank),
                competitionRank,
                "A competition rank must be positive.");
        }

        Evaluation = evaluation
            ?? throw new ArgumentNullException(nameof(evaluation));
        if (!evaluation.IsRankable || evaluation.Result is null)
        {
            throw new ArgumentException(
                "Only an exact numeric workforce evaluation may receive a rank.",
                nameof(evaluation));
        }

        CompetitionRank = competitionRank;
    }

    public int CompetitionRank { get; }

    public WorkforceEvaluation Evaluation { get; }

    internal string StableKey => string.Join('|',
        WorkforceText.Number(CompetitionRank),
        Evaluation.Fingerprint);
}

public sealed record WorkforceShortlistCounts
{
    internal WorkforceShortlistCounts(
        int total,
        int ranked,
        int tied,
        int currentOnly,
        int ineligible,
        int incomplete,
        int unsupported,
        int conflicting)
    {
        Total = total;
        Ranked = ranked;
        Tied = tied;
        CurrentOnly = currentOnly;
        Ineligible = ineligible;
        Incomplete = incomplete;
        Unsupported = unsupported;
        Conflicting = conflicting;
        if (new[]
            {
                total,
                ranked,
                tied,
                currentOnly,
                ineligible,
                incomplete,
                unsupported,
                conflicting
            }.Any(value => value < 0)
            || total != ranked + tied + currentOnly + ineligible
                + incomplete + unsupported + conflicting)
        {
            throw new ArgumentException(
                "Shortlist state counts must be non-negative and sum to the total.");
        }
    }

    public int Total { get; }

    public int Comparable => Ranked + Tied;

    public int Ranked { get; }

    public int Tied { get; }

    public int CurrentOnly { get; }

    public int Ineligible { get; }

    public int Incomplete { get; }

    public int Unsupported { get; }

    public int Conflicting { get; }

    internal string StableKey => string.Join('|',
        WorkforceText.Number(Total),
        WorkforceText.Number(Ranked),
        WorkforceText.Number(Tied),
        WorkforceText.Number(CurrentOnly),
        WorkforceText.Number(Ineligible),
        WorkforceText.Number(Incomplete),
        WorkforceText.Number(Unsupported),
        WorkforceText.Number(Conflicting));
}

public sealed class VillageWorkforceShortlistView
{
    internal VillageWorkforceShortlistView(
        WorkforceShortlistFilter filter,
        string resultFingerprint,
        WorkforceShortlistCounts unfilteredCounts,
        IEnumerable<WorkforceEvaluation> visibleEvaluations)
    {
        WorkforceText.Defined(filter, nameof(filter));
        Filter = filter;
        ResultFingerprint = WorkforceText.Sha256(
            resultFingerprint,
            nameof(resultFingerprint));
        UnfilteredCounts = unfilteredCounts
            ?? throw new ArgumentNullException(nameof(unfilteredCounts));
        ArgumentNullException.ThrowIfNull(visibleEvaluations);
        var copied = visibleEvaluations.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.GroupBy(item => item.Worker)
                .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A shortlist view cannot contain null or duplicate workers.",
                nameof(visibleEvaluations));
        }

        VisibleEvaluations = copied;
    }

    public WorkforceShortlistFilter Filter { get; }

    public string ResultFingerprint { get; }

    public WorkforceShortlistCounts UnfilteredCounts { get; }

    public ImmutableArray<WorkforceEvaluation> VisibleEvaluations { get; }
}

public sealed record WorkforceManualChecklistItem
{
    public WorkforceManualChecklistItem(
        WorkforceChecklistItemKind kind,
        WorkforceChecklistCategory category)
    {
        WorkforceText.Defined(kind, nameof(kind));
        WorkforceText.Defined(category, nameof(category));
        var expected = kind switch
        {
            WorkforceChecklistItemKind.TargetIdentityMustMatch =>
                WorkforceChecklistCategory.FactToVerify,
            WorkforceChecklistItemKind
                .ReassignmentAvailabilityMustBeVerified =>
                WorkforceChecklistCategory.Prerequisite,
            WorkforceChecklistItemKind
                .QualificationAndEvidenceMustBeReviewed =>
                WorkforceChecklistCategory.FactToVerify,
            WorkforceChecklistItemKind.EfficiencyWasNotCalculated =>
                WorkforceChecklistCategory.Caution,
            WorkforceChecklistItemKind.NoActionWasSentToGame =>
                WorkforceChecklistCategory.Caution,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (category != expected)
        {
            throw new ArgumentException(
                "The checklist item has a fixed semantic category.",
                nameof(category));
        }

        Kind = kind;
        Category = category;
    }

    public WorkforceChecklistItemKind Kind { get; }

    public WorkforceChecklistCategory Category { get; }

    internal string StableKey => string.Join('|',
        WorkforceText.EnumKey(Kind),
        WorkforceText.EnumKey(Category));
}

public sealed class VillageWorkforceManualPlan
{
    internal VillageWorkforceManualPlan(
        WorkforceResultIdentity resultIdentity,
        VillageWorkerIdentity currentWorker,
        ProposedShopManagerAssignment proposedAssignment,
        IEnumerable<WorkforceManualChecklistItem> checklist)
    {
        ResultIdentity = resultIdentity
            ?? throw new ArgumentNullException(nameof(resultIdentity));
        CurrentWorker = currentWorker
            ?? throw new ArgumentNullException(nameof(currentWorker));
        ProposedAssignment = proposedAssignment
            ?? throw new ArgumentNullException(nameof(proposedAssignment));
        if (proposedAssignment.ResultIdentity != resultIdentity)
        {
            throw new ArgumentException(
                "A manual plan proposal must belong to the shortlist result.",
                nameof(proposedAssignment));
        }

        if (proposedAssignment.Worker == currentWorker)
        {
            throw new ArgumentException(
                "A replacement plan requires a worker other than the current assignment.",
                nameof(proposedAssignment));
        }

        ArgumentNullException.ThrowIfNull(checklist);
        var copied = checklist.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.GroupBy(item => item.Kind).Any(group => group.Count() > 1)
            || copied.Length != Enum.GetValues<WorkforceChecklistItemKind>().Length)
        {
            throw new ArgumentException(
                "A manual plan requires every unique checklist fact exactly once.",
                nameof(checklist));
        }

        Checklist = [.. copied.OrderBy(item => item.Kind)];
        Fingerprint = CreateFingerprint();
    }

    public WorkforceResultIdentity ResultIdentity { get; }

    public VillageWorkerIdentity CurrentWorker { get; }

    public ProposedShopManagerAssignment ProposedAssignment { get; }

    public ImmutableArray<WorkforceManualChecklistItem> Checklist { get; }

    public string Fingerprint { get; }

    private string CreateFingerprint() => WorkforceText.Fingerprint(string.Join(
        '|',
        "VILLAGE_WORKFORCE_MANUAL_PLAN_V1",
        ResultIdentity.StableKey,
        CurrentWorker.StableKey,
        ProposedAssignment.StableKey,
        string.Join("||", Checklist.Select(item => item.StableKey))));
}

public sealed class VillageWorkforceShortlist
{
    public VillageWorkforceShortlist(VillageWorkforceEvaluationSet evaluationSet)
    {
        EvaluationSet = evaluationSet
            ?? throw new ArgumentNullException(nameof(evaluationSet));
        CurrentEvaluation = evaluationSet.Evaluations.Single(item =>
            item.Worker == evaluationSet.CurrentWorker);
        Comparable = Rank(evaluationSet.Evaluations);
        CurrentOnly = CopyState(
            evaluationSet.Evaluations,
            WorkforceEvaluationState.CurrentOnly);
        Ineligible = CopyState(
            evaluationSet.Evaluations,
            WorkforceEvaluationState.Ineligible);
        Incomplete = CopyState(
            evaluationSet.Evaluations,
            WorkforceEvaluationState.Incomplete);
        Unsupported = CopyState(
            evaluationSet.Evaluations,
            WorkforceEvaluationState.Unsupported);
        Conflicting = CopyState(
            evaluationSet.Evaluations,
            WorkforceEvaluationState.Conflicting);
        Counts = CreateCounts(evaluationSet.Evaluations);
        VacancyState = WorkforceVacancyState.NoExplicitVacancy;
        Limitations = evaluationSet.Rule.Limitations;
        Fingerprint = CreateFingerprint();
    }

    public VillageWorkforceEvaluationSet EvaluationSet { get; }

    public WorkforceEvaluation CurrentEvaluation { get; }

    public ImmutableArray<WorkforceRankedCandidate> Comparable { get; }

    public ImmutableArray<WorkforceEvaluation> CurrentOnly { get; }

    public ImmutableArray<WorkforceEvaluation> Ineligible { get; }

    public ImmutableArray<WorkforceEvaluation> Incomplete { get; }

    public ImmutableArray<WorkforceEvaluation> Unsupported { get; }

    public ImmutableArray<WorkforceEvaluation> Conflicting { get; }

    public WorkforceShortlistCounts Counts { get; }

    public WorkforceVacancyState VacancyState { get; }

    public ImmutableArray<WorkforceRuleLimitation> Limitations { get; }

    public string Fingerprint { get; }

    public VillageWorkforceShortlistView ApplyFilter(
        WorkforceShortlistFilter filter)
    {
        WorkforceText.Defined(filter, nameof(filter));
        var visible = filter switch
        {
            WorkforceShortlistFilter.All => AllInDisplayOrder(),
            WorkforceShortlistFilter.Comparable =>
                Comparable.Select(item => item.Evaluation),
            WorkforceShortlistFilter.NeedsReview =>
                Incomplete.Concat(Unsupported).Concat(Conflicting),
            WorkforceShortlistFilter.Ineligible => Ineligible,
            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };
        return new VillageWorkforceShortlistView(
            filter,
            Fingerprint,
            Counts,
            visible);
    }

    public WorkforceComparison Compare(
        VillageWorkerIdentity first,
        VillageWorkerIdentity second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var firstEvaluation = EvaluationSet.Evaluations.SingleOrDefault(item =>
            item.Worker == first)
            ?? throw new ArgumentException(
                "The first worker is not in the shortlist result.",
                nameof(first));
        var secondEvaluation = EvaluationSet.Evaluations.SingleOrDefault(item =>
            item.Worker == second)
            ?? throw new ArgumentException(
                "The second worker is not in the shortlist result.",
                nameof(second));
        return new WorkforceComparison(firstEvaluation, secondEvaluation);
    }

    public VillageWorkforceManualPlan CreateManualPlan(
        VillageWorkerIdentity proposedWorker)
    {
        ArgumentNullException.ThrowIfNull(proposedWorker);
        var evaluation = EvaluationSet.Evaluations.SingleOrDefault(item =>
            item.Worker == proposedWorker)
            ?? throw new ArgumentException(
                "The proposed worker is not in the shortlist result.",
                nameof(proposedWorker));
        if (!evaluation.IsRankable)
        {
            throw new ArgumentException(
                "Only a rankable alternative may appear in a manual plan.",
                nameof(proposedWorker));
        }

        var proposal = new ProposedShopManagerAssignment(
            EvaluationSet.ResultIdentity,
            proposedWorker);
        return new VillageWorkforceManualPlan(
            EvaluationSet.ResultIdentity,
            EvaluationSet.CurrentWorker,
            proposal,
            CreateChecklist());
    }

    private static ImmutableArray<WorkforceRankedCandidate> Rank(
        IEnumerable<WorkforceEvaluation> evaluations)
    {
        var ordered = evaluations
            .Where(item => item.IsRankable)
            .OrderByDescending(item => item.Result!.Value)
            .ThenBy(item => item.Worker.CharacterId)
            .ToArray();
        var ranked = new List<WorkforceRankedCandidate>(ordered.Length);
        decimal? previousValue = null;
        var competitionRank = 0;
        for (var index = 0; index < ordered.Length; index++)
        {
            var value = ordered[index].Result!.Value;
            if (previousValue is null || value != previousValue.Value)
            {
                competitionRank = index + 1;
                previousValue = value;
            }

            ranked.Add(new WorkforceRankedCandidate(
                competitionRank,
                ordered[index]));
        }

        return [.. ranked];
    }

    private static ImmutableArray<WorkforceEvaluation> CopyState(
        IEnumerable<WorkforceEvaluation> evaluations,
        WorkforceEvaluationState state) =>
        [.. evaluations
            .Where(item => item.State == state)
            .OrderBy(item => item.Worker.CharacterId)];

    private static WorkforceShortlistCounts CreateCounts(
        ImmutableArray<WorkforceEvaluation> evaluations) =>
        new(
            evaluations.Length,
            evaluations.Count(item =>
                item.State == WorkforceEvaluationState.Ranked),
            evaluations.Count(item =>
                item.State == WorkforceEvaluationState.Tied),
            evaluations.Count(item =>
                item.State == WorkforceEvaluationState.CurrentOnly),
            evaluations.Count(item =>
                item.State == WorkforceEvaluationState.Ineligible),
            evaluations.Count(item =>
                item.State == WorkforceEvaluationState.Incomplete),
            evaluations.Count(item =>
                item.State == WorkforceEvaluationState.Unsupported),
            evaluations.Count(item =>
                item.State == WorkforceEvaluationState.Conflicting));

    private static ImmutableArray<WorkforceManualChecklistItem>
        CreateChecklist() =>
        [
            new WorkforceManualChecklistItem(
                WorkforceChecklistItemKind.TargetIdentityMustMatch,
                WorkforceChecklistCategory.FactToVerify),
            new WorkforceManualChecklistItem(
                WorkforceChecklistItemKind
                    .ReassignmentAvailabilityMustBeVerified,
                WorkforceChecklistCategory.Prerequisite),
            new WorkforceManualChecklistItem(
                WorkforceChecklistItemKind
                    .QualificationAndEvidenceMustBeReviewed,
                WorkforceChecklistCategory.FactToVerify),
            new WorkforceManualChecklistItem(
                WorkforceChecklistItemKind.EfficiencyWasNotCalculated,
                WorkforceChecklistCategory.Caution),
            new WorkforceManualChecklistItem(
                WorkforceChecklistItemKind.NoActionWasSentToGame,
                WorkforceChecklistCategory.Caution)
        ];

    private IEnumerable<WorkforceEvaluation> AllInDisplayOrder()
        => Comparable.Select(item => item.Evaluation)
            .Concat(CurrentOnly)
            .Concat(Incomplete)
            .Concat(Unsupported)
            .Concat(Conflicting)
            .Concat(Ineligible);

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("VILLAGE_WORKFORCE_SHORTLIST_V1\n")
            .Append(EvaluationSet.Fingerprint).Append('|')
            .Append(Counts.StableKey).Append('|')
            .Append(WorkforceText.EnumKey(VacancyState)).Append('\n');
        foreach (var candidate in Comparable)
        {
            canonical.Append("RANKED|")
                .Append(candidate.StableKey).Append('\n');
        }

        foreach (var limitation in Limitations)
        {
            canonical.Append("LIMITATION|")
                .Append(limitation.StableKey).Append('\n');
        }

        return WorkforceText.Fingerprint(canonical.ToString());
    }
}
