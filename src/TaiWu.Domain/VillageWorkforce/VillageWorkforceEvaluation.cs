using System.Collections.Immutable;
using System.Text;

namespace TaiWu.Domain.VillageWorkforce;

public sealed record WorkforceComponentIdentity
{
    public WorkforceComponentIdentity(
        WorkforceComponentKind kind,
        LifeSkillDisciplineIdentity discipline)
    {
        WorkforceText.Defined(kind, nameof(kind));
        Kind = kind;
        Discipline = discipline
            ?? throw new ArgumentNullException(nameof(discipline));
    }

    public WorkforceComponentKind Kind { get; }

    public LifeSkillDisciplineIdentity Discipline { get; }

    internal string StableKey =>
        $"{WorkforceText.EnumKey(Kind)}:{Discipline.StableKey}";
}

public sealed record WorkforceRequirementEvaluation
{
    public WorkforceRequirementEvaluation(
        WorkforceRequirementKind requirement,
        WorkforceRequirementOutcome outcome,
        string reasonIdentity,
        IEnumerable<WorkforceEvidenceReference> evidence,
        IEnumerable<WorkforceConflictValue>? conflicts = null)
    {
        WorkforceText.Defined(requirement, nameof(requirement));
        WorkforceText.Defined(outcome, nameof(outcome));
        Requirement = requirement;
        Outcome = outcome;
        ReasonIdentity = WorkforceText.Stable(
            reasonIdentity,
            nameof(reasonIdentity));
        Evidence = CopyEvidence(evidence, nameof(evidence));
        Conflicts = CopyConflicts(conflicts ?? [], nameof(conflicts));
        if (!Conflicts.IsEmpty
            && Outcome != WorkforceRequirementOutcome.Conflicting)
        {
            throw new ArgumentException(
                "Only a conflicting requirement may retain conflict values.",
                nameof(conflicts));
        }
    }

    public WorkforceRequirementKind Requirement { get; }

    public WorkforceRequirementOutcome Outcome { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<WorkforceEvidenceReference> Evidence { get; }

    public ImmutableArray<WorkforceConflictValue> Conflicts { get; }

    internal string StableKey => string.Join('|',
        WorkforceText.EnumKey(Requirement),
        WorkforceText.EnumKey(Outcome),
        ReasonIdentity,
        string.Join("||", Evidence.Select(item => item.StableKey)),
        string.Join("||", Conflicts.Select(item => item.StableKey)));

    internal static ImmutableArray<WorkforceEvidenceReference> CopyEvidence(
        IEnumerable<WorkforceEvidenceReference> evidence,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var copied = evidence.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "Requirement evidence cannot contain null entries.",
                parameterName);
        }

        if (copied.GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Requirement evidence cannot contain duplicates.",
                parameterName);
        }

        return [.. copied.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
    }

    private static ImmutableArray<WorkforceConflictValue> CopyConflicts(
        IEnumerable<WorkforceConflictValue> conflicts,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        var copied = conflicts.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "Requirement conflicts cannot contain null entries.",
                parameterName);
        }

        if (copied.GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Requirement conflicts cannot contain duplicates.",
                parameterName);
        }

        return [.. copied.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
    }
}

public sealed record WorkforceScoreComponent
{
    public WorkforceScoreComponent(
        WorkforceComponentIdentity identity,
        short rawValue,
        decimal normalizedValue,
        decimal weight,
        decimal contribution,
        string explanationIdentity,
        IEnumerable<WorkforceEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        if (normalizedValue != rawValue)
        {
            throw new ArgumentException(
                "Version-1 qualification normalization must preserve the raw value.",
                nameof(normalizedValue));
        }

        if (weight != 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                weight,
                "Version-1 qualification weight must be one.");
        }

        if (contribution != normalizedValue)
        {
            throw new ArgumentException(
                "Version-1 contribution must equal the normalized value.",
                nameof(contribution));
        }

        RawValue = rawValue;
        NormalizedValue = normalizedValue;
        Weight = weight;
        Contribution = contribution;
        ExplanationIdentity = WorkforceText.Stable(
            explanationIdentity,
            nameof(explanationIdentity));
        Evidence = WorkforceRequirementEvaluation.CopyEvidence(
            evidence,
            nameof(evidence));
    }

    public WorkforceComponentIdentity Identity { get; }

    public WorkforceUnit Unit => WorkforceUnit.BaseQualificationPoint;

    public short RawValue { get; }

    public decimal NormalizedValue { get; }

    public decimal Weight { get; }

    public decimal Contribution { get; }

    public string ExplanationIdentity { get; }

    public ImmutableArray<WorkforceEvidenceReference> Evidence { get; }

    internal string StableKey => string.Join('|',
        Identity.StableKey,
        WorkforceText.EnumKey(Unit),
        WorkforceText.Number(RawValue),
        WorkforceText.Number(NormalizedValue),
        WorkforceText.Number(Weight),
        WorkforceText.Number(Contribution),
        ExplanationIdentity,
        string.Join("||", Evidence.Select(item => item.StableKey)));
}

public sealed record WorkforceResultValue
{
    public WorkforceResultValue(WorkforceUnit unit, decimal value)
    {
        WorkforceText.Defined(unit, nameof(unit));
        Unit = unit;
        Value = value;
    }

    public WorkforceUnit Unit { get; }

    public decimal Value { get; }

    internal string StableKey =>
        $"{WorkforceText.EnumKey(Unit)}:{WorkforceText.Number(Value)}";
}

public sealed class WorkforceEvaluation
{
    public WorkforceEvaluation(
        WorkforceResultIdentity resultIdentity,
        VillageWorkerIdentity worker,
        WorkforceWorkerState workerState,
        WorkforceEvaluationState state,
        IEnumerable<WorkforceRequirementEvaluation> requirements,
        IEnumerable<WorkforceScoreComponent> components,
        WorkforceResultValue? result,
        string outcomeIdentity)
    {
        ResultIdentity = resultIdentity
            ?? throw new ArgumentNullException(nameof(resultIdentity));
        Worker = worker ?? throw new ArgumentNullException(nameof(worker));
        WorkforceText.Defined(workerState, nameof(workerState));
        WorkforceText.Defined(state, nameof(state));
        WorkerState = workerState;
        State = state;
        Requirements = CopyUnique(
            requirements,
            item => WorkforceText.EnumKey(item.Requirement),
            "requirement",
            nameof(requirements));
        Components = CopyUnique(
            components,
            item => item.Identity.StableKey,
            "component",
            nameof(components));
        Result = result;
        OutcomeIdentity = WorkforceText.Stable(
            outcomeIdentity,
            nameof(outcomeIdentity));
        ValidateInvariant();
        Fingerprint = CreateFingerprint();
    }

    public WorkforceResultIdentity ResultIdentity { get; }

    public VillageWorkerIdentity Worker { get; }

    public WorkforceWorkerState WorkerState { get; }

    public WorkforceEvaluationState State { get; }

    public ImmutableArray<WorkforceRequirementEvaluation> Requirements
    {
        get;
    }

    public ImmutableArray<WorkforceScoreComponent> Components { get; }

    public WorkforceResultValue? Result { get; }

    public string OutcomeIdentity { get; }

    public string Fingerprint { get; }

    public bool IsRankable => State is WorkforceEvaluationState.Ranked
        or WorkforceEvaluationState.Tied;

    private void ValidateInvariant()
    {
        if (IsRankable)
        {
            if (WorkerState != WorkforceWorkerState.Eligible
                || Requirements.Length
                    != Enum.GetValues<WorkforceRequirementKind>().Length
                || Requirements.Any(item =>
                    item.Outcome != WorkforceRequirementOutcome.Passed)
                || Components.Length != 1
                || Result is null)
            {
                throw new ArgumentException(
                    "A rankable workforce evaluation requires an eligible worker, passed gates, one component, and a result.");
            }
        }
        else if (State != WorkforceEvaluationState.CurrentOnly
            && (!Components.IsEmpty || Result is not null))
        {
            throw new ArgumentException(
                "Only rankable or current-only evaluations may carry a numeric result.");
        }

        if (State == WorkforceEvaluationState.CurrentOnly
            && WorkerState != WorkforceWorkerState.CurrentOnly)
        {
            throw new ArgumentException(
                "A current-only evaluation requires a current-only worker.");
        }

        var expectedWorkerState = State switch
        {
            WorkforceEvaluationState.Ranked or WorkforceEvaluationState.Tied =>
                WorkforceWorkerState.Eligible,
            WorkforceEvaluationState.CurrentOnly =>
                WorkforceWorkerState.CurrentOnly,
            WorkforceEvaluationState.Ineligible =>
                WorkforceWorkerState.Ineligible,
            WorkforceEvaluationState.Incomplete =>
                WorkforceWorkerState.Incomplete,
            WorkforceEvaluationState.Unsupported =>
                WorkforceWorkerState.Unsupported,
            WorkforceEvaluationState.Conflicting =>
                WorkforceWorkerState.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(State))
        };
        if (WorkerState != expectedWorkerState)
        {
            throw new ArgumentException(
                "Worker and evaluation states must describe the same outcome.");
        }

        if (State == WorkforceEvaluationState.CurrentOnly
            && ((Components.IsEmpty && Result is not null)
                || (!Components.IsEmpty
                    && (Components.Length != 1 || Result is null))))
        {
            throw new ArgumentException(
                "A current-only value requires exactly one component and result.");
        }

        if (Result is not null)
        {
            if (Components.Length != 1
                || Result.Unit != Components[0].Unit
                || Result.Value != Components[0].Contribution)
            {
                throw new ArgumentException(
                    "A workforce result must equal its single version-1 component.");
            }
        }
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("WORKFORCE_EVALUATION_V1\n")
            .Append(ResultIdentity.StableKey).Append('\n')
            .Append(Worker.StableKey).Append('|')
            .Append(WorkforceText.EnumKey(WorkerState)).Append('|')
            .Append(WorkforceText.EnumKey(State)).Append('|')
            .Append(Result?.StableKey ?? "NONE").Append('|')
            .Append(OutcomeIdentity).Append('\n');
        foreach (var requirement in Requirements)
        {
            canonical.Append("REQUIREMENT|")
                .Append(requirement.StableKey).Append('\n');
        }

        foreach (var component in Components)
        {
            canonical.Append("COMPONENT|")
                .Append(component.StableKey).Append('\n');
        }

        return WorkforceText.Fingerprint(canonical.ToString());
    }

    private static ImmutableArray<T> CopyUnique<T>(
        IEnumerable<T> source,
        Func<T, string> keySelector,
        string itemName,
        string parameterName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        var copied = source.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                $"An evaluation cannot contain a null {itemName}.",
                parameterName);
        }

        if (copied.GroupBy(keySelector, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                $"An evaluation cannot contain duplicate {itemName} entries.",
                parameterName);
        }

        return [.. copied.OrderBy(keySelector, StringComparer.Ordinal)];
    }
}

public sealed class WorkforceComparison
{
    public WorkforceComparison(
        WorkforceEvaluation first,
        WorkforceEvaluation second)
    {
        First = first ?? throw new ArgumentNullException(nameof(first));
        Second = second ?? throw new ArgumentNullException(nameof(second));
        if (first.Worker == second.Worker)
        {
            throw new ArgumentException(
                "A workforce comparison requires two distinct workers.",
                nameof(second));
        }

        if (first.ResultIdentity != second.ResultIdentity)
        {
            throw new ArgumentException(
                "Compared workers must belong to the same immutable result.",
                nameof(second));
        }

        Outcome = DetermineOutcome(first, second);
        Fingerprint = WorkforceText.Fingerprint(string.Join('|',
            "WORKFORCE_COMPARISON_V1",
            first.Fingerprint,
            second.Fingerprint,
            WorkforceText.EnumKey(Outcome)));
    }

    public WorkforceEvaluation First { get; }

    public WorkforceEvaluation Second { get; }

    public WorkforceComparisonOutcome Outcome { get; }

    public string Fingerprint { get; }

    private static WorkforceComparisonOutcome DetermineOutcome(
        WorkforceEvaluation first,
        WorkforceEvaluation second)
    {
        if (first.State == WorkforceEvaluationState.Conflicting
            || second.State == WorkforceEvaluationState.Conflicting)
        {
            return WorkforceComparisonOutcome.Conflicting;
        }

        if (!first.IsRankable || !second.IsRankable
            || first.Result is null || second.Result is null)
        {
            return WorkforceComparisonOutcome.Unavailable;
        }

        return first.Result.Value.CompareTo(second.Result.Value) switch
        {
            > 0 => WorkforceComparisonOutcome.Higher,
            < 0 => WorkforceComparisonOutcome.Lower,
            _ => WorkforceComparisonOutcome.Equal
        };
    }
}
