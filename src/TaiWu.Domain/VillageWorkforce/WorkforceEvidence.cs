using System.Collections.Immutable;

namespace TaiWu.Domain.VillageWorkforce;

public sealed record WorkforceFactIdentity
{
    public WorkforceFactIdentity(
        WorkforceFactKind kind,
        LifeSkillDisciplineIdentity? discipline = null)
    {
        WorkforceText.Defined(kind, nameof(kind));
        if (kind == WorkforceFactKind.BaseLifeSkillQualification
            && discipline is null)
        {
            throw new ArgumentException(
                "A base life-skill qualification requires a discipline.",
                nameof(discipline));
        }

        if (kind != WorkforceFactKind.BaseLifeSkillQualification
            && discipline is not null)
        {
            throw new ArgumentException(
                "Only a life-skill qualification fact may carry a discipline.",
                nameof(discipline));
        }

        Kind = kind;
        Discipline = discipline;
    }

    public WorkforceFactKind Kind { get; }

    public LifeSkillDisciplineIdentity? Discipline { get; }

    public WorkforceFactValueKind ExpectedValueKind => Kind switch
    {
        WorkforceFactKind.CandidateUniverseMembership =>
            WorkforceFactValueKind.Boolean,
        WorkforceFactKind.CurrentAssignmentMembership =>
            WorkforceFactValueKind.Boolean,
        WorkforceFactKind.BaseLifeSkillQualification =>
            WorkforceFactValueKind.Int16,
        _ => throw new ArgumentOutOfRangeException(
            nameof(Kind),
            Kind,
            "Unknown workforce fact kind.")
    };

    internal string StableKey =>
        $"{WorkforceText.EnumKey(Kind)}:{Discipline?.StableKey ?? "NONE"}";
}

public sealed class WorkforceFactValue
{
    private readonly int _scalar;

    private WorkforceFactValue(WorkforceFactValueKind kind, int scalar)
    {
        Kind = kind;
        _scalar = scalar;
    }

    public WorkforceFactValueKind Kind { get; }

    public bool BooleanValue => Kind == WorkforceFactValueKind.Boolean
        ? _scalar != 0
        : throw WrongKind(WorkforceFactValueKind.Boolean);

    public short Int16Value => Kind == WorkforceFactValueKind.Int16
        ? checked((short)_scalar)
        : throw WrongKind(WorkforceFactValueKind.Int16);

    public int Int32Value => Kind == WorkforceFactValueKind.Int32
        ? _scalar
        : throw WrongKind(WorkforceFactValueKind.Int32);

    public static WorkforceFactValue Boolean(bool value) =>
        new(WorkforceFactValueKind.Boolean, value ? 1 : 0);

    public static WorkforceFactValue Int16(short value) =>
        new(WorkforceFactValueKind.Int16, value);

    public static WorkforceFactValue Int32(int value) =>
        new(WorkforceFactValueKind.Int32, value);

    internal string StableKey =>
        $"{WorkforceText.EnumKey(Kind)}:{WorkforceText.Number(_scalar)}";

    private InvalidOperationException WrongKind(
        WorkforceFactValueKind expected) =>
        new($"Workforce fact is {Kind}, not {expected}.");
}

public sealed record WorkforceProvenance
{
    public WorkforceProvenance(
        WorkforceEvidenceSourceKind sourceKind,
        string sourceIdentity,
        string sourceVersion,
        string revisionIdentity)
    {
        WorkforceText.Defined(sourceKind, nameof(sourceKind));
        SourceKind = sourceKind;
        SourceIdentity = WorkforceText.Stable(
            sourceIdentity,
            nameof(sourceIdentity));
        SourceVersion = WorkforceText.Version(
            sourceVersion,
            nameof(sourceVersion));
        RevisionIdentity = WorkforceText.Stable(
            revisionIdentity,
            nameof(revisionIdentity));
    }

    public WorkforceEvidenceSourceKind SourceKind { get; }

    public string SourceIdentity { get; }

    public string SourceVersion { get; }

    public string RevisionIdentity { get; }

    internal string StableKey => string.Join('|',
        WorkforceText.EnumKey(SourceKind),
        SourceIdentity,
        SourceVersion,
        RevisionIdentity);
}

public sealed record WorkforceEvidenceReference
{
    public WorkforceEvidenceReference(
        string referenceIdentity,
        WorkforceProvenance provenance)
    {
        ReferenceIdentity = WorkforceText.Stable(
            referenceIdentity,
            nameof(referenceIdentity));
        Provenance = provenance
            ?? throw new ArgumentNullException(nameof(provenance));
    }

    public string ReferenceIdentity { get; }

    public WorkforceProvenance Provenance { get; }

    internal string StableKey =>
        $"{ReferenceIdentity}|{Provenance.StableKey}";
}

public sealed record WorkforceUnavailableReason
{
    public WorkforceUnavailableReason(string code)
    {
        Code = WorkforceText.Stable(code, nameof(code));
    }

    public string Code { get; }

    internal string StableKey => Code;
}

public sealed record WorkforceConflictValue
{
    public WorkforceConflictValue(
        WorkforceFactValue value,
        WorkforceProvenance provenance)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Provenance = provenance
            ?? throw new ArgumentNullException(nameof(provenance));
    }

    public WorkforceFactValue Value { get; }

    public WorkforceProvenance Provenance { get; }

    internal string StableKey =>
        $"{Value.StableKey}|{Provenance.StableKey}";
}

public sealed class WorkforceFact
{
    private WorkforceFact(
        WorkforceFactIdentity identity,
        WorkforceEvidenceState state,
        WorkforceFactValue? value,
        WorkforceProvenance? provenance,
        WorkforceUnavailableReason? unavailableReason,
        IEnumerable<WorkforceConflictValue> conflicts,
        IEnumerable<WorkforceEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        WorkforceText.Defined(state, nameof(state));
        if (value is not null && value.Kind != identity.ExpectedValueKind)
        {
            throw new ArgumentException(
                $"Fact {identity.Kind} requires {identity.ExpectedValueKind}.",
                nameof(value));
        }

        ArgumentNullException.ThrowIfNull(conflicts);
        var copiedConflicts = conflicts.ToImmutableArray();
        if (copiedConflicts.Any(item => item is null))
        {
            throw new ArgumentException(
                "Workforce conflicts cannot contain null entries.",
                nameof(conflicts));
        }

        if (copiedConflicts.Any(item =>
            item.Value.Kind != identity.ExpectedValueKind))
        {
            throw new ArgumentException(
                "Every conflict value must match the fact kind.",
                nameof(conflicts));
        }

        var duplicateConflict = copiedConflicts
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateConflict is not null)
        {
            throw new ArgumentException(
                "Workforce conflicts cannot contain duplicates.",
                nameof(conflicts));
        }

        ArgumentNullException.ThrowIfNull(evidence);
        var copiedEvidence = evidence.ToImmutableArray();
        if (copiedEvidence.Any(item => item is null))
        {
            throw new ArgumentException(
                "Workforce evidence cannot contain null entries.",
                nameof(evidence));
        }

        var duplicateEvidence = copiedEvidence
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateEvidence is not null)
        {
            throw new ArgumentException(
                "Workforce evidence cannot contain duplicates.",
                nameof(evidence));
        }

        State = state;
        Value = value;
        Provenance = provenance;
        UnavailableReason = unavailableReason;
        Conflicts = [.. copiedConflicts.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
        Evidence = [.. copiedEvidence.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal)];
        ValidateInvariant();
    }

    public WorkforceFactIdentity Identity { get; }

    public WorkforceEvidenceState State { get; }

    public WorkforceFactValue? Value { get; }

    public WorkforceProvenance? Provenance { get; }

    public WorkforceUnavailableReason? UnavailableReason { get; }

    public ImmutableArray<WorkforceConflictValue> Conflicts { get; }

    public ImmutableArray<WorkforceEvidenceReference> Evidence { get; }

    public static WorkforceFact Confirmed(
        WorkforceFactIdentity identity,
        WorkforceFactValue value,
        WorkforceProvenance provenance,
        IEnumerable<WorkforceEvidenceReference> evidence) =>
        new(identity, WorkforceEvidenceState.Confirmed, value, provenance,
            null, [], evidence);

    public static WorkforceFact Incomplete(
        WorkforceFactIdentity identity,
        WorkforceUnavailableReason reason,
        IEnumerable<WorkforceEvidenceReference> evidence) =>
        Unavailable(identity, WorkforceEvidenceState.Incomplete, reason,
            evidence);

    public static WorkforceFact Unsupported(
        WorkforceFactIdentity identity,
        WorkforceUnavailableReason reason,
        IEnumerable<WorkforceEvidenceReference> evidence) =>
        Unavailable(identity, WorkforceEvidenceState.Unsupported, reason,
            evidence);

    public static WorkforceFact Stale(
        WorkforceFactIdentity identity,
        WorkforceFactValue lastObservedValue,
        WorkforceProvenance provenance,
        WorkforceUnavailableReason reason,
        IEnumerable<WorkforceEvidenceReference> evidence) =>
        new(identity, WorkforceEvidenceState.Stale, lastObservedValue,
            provenance, reason, [], evidence);

    public static WorkforceFact Conflicting(
        WorkforceFactIdentity identity,
        IEnumerable<WorkforceConflictValue> conflicts,
        IEnumerable<WorkforceEvidenceReference> evidence) =>
        new(identity, WorkforceEvidenceState.Conflicting, null, null, null,
            conflicts, evidence);

    internal string StableKey => string.Join('|',
        Identity.StableKey,
        WorkforceText.EnumKey(State),
        Value?.StableKey ?? "NONE",
        Provenance?.StableKey ?? "NONE",
        UnavailableReason?.StableKey ?? "NONE",
        string.Join("||", Conflicts.Select(item => item.StableKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));

    private static WorkforceFact Unavailable(
        WorkforceFactIdentity identity,
        WorkforceEvidenceState state,
        WorkforceUnavailableReason reason,
        IEnumerable<WorkforceEvidenceReference> evidence) =>
        new(identity, state, null, null,
            reason ?? throw new ArgumentNullException(nameof(reason)), [],
            evidence);

    private void ValidateInvariant()
    {
        switch (State)
        {
            case WorkforceEvidenceState.Confirmed:
                if (Value is null || Provenance is null
                    || UnavailableReason is not null || !Conflicts.IsEmpty)
                {
                    throw new ArgumentException(
                        "Confirmed workforce evidence requires one value and provenance only.");
                }
                break;
            case WorkforceEvidenceState.Incomplete:
            case WorkforceEvidenceState.Unsupported:
                if (Value is not null || Provenance is not null
                    || UnavailableReason is null || !Conflicts.IsEmpty)
                {
                    throw new ArgumentException(
                        "Unavailable workforce evidence requires a reason and no value.");
                }
                break;
            case WorkforceEvidenceState.Stale:
                if (Value is null || Provenance is null
                    || UnavailableReason is null || !Conflicts.IsEmpty)
                {
                    throw new ArgumentException(
                        "Stale workforce evidence requires its last value, provenance, and reason.");
                }
                break;
            case WorkforceEvidenceState.Conflicting:
                if (Value is not null || Provenance is not null
                    || UnavailableReason is not null || Conflicts.Length < 2)
                {
                    throw new ArgumentException(
                        "Conflicting workforce evidence requires at least two values.");
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(State));
        }
    }
}
