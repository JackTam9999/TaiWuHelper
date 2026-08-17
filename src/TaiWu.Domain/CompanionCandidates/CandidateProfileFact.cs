using System.Collections.Immutable;

namespace TaiWu.Domain.CompanionCandidates;

public sealed class CandidateProfileFact
{
    private CandidateProfileFact(
        CandidateProfileFieldIdentity identity,
        CandidateEvidenceState state,
        CandidateFactValue? value,
        CandidateFactProvenance? provenance,
        CandidateUnavailableReason? unavailableReason,
        IEnumerable<CandidateConflictValue> conflicts,
        CandidateConflictDecision? conflictDecision,
        IEnumerable<CandidateEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown candidate evidence state.");
        }

        if (value is not null && value.Kind != identity.ExpectedValueKind)
        {
            throw new ArgumentException(
                $"Field {identity.Field} requires a {identity.ExpectedValueKind} value.",
                nameof(value));
        }

        State = state;
        Value = value;
        Provenance = provenance;
        UnavailableReason = unavailableReason;
        Evidence = CandidateProfileCollections.CopyEvidence(evidence, nameof(evidence));

        ArgumentNullException.ThrowIfNull(conflicts);
        var copiedConflicts = conflicts.ToImmutableArray();
        if (copiedConflicts.Any(item => item is null))
        {
            throw new ArgumentException("Conflicts cannot contain null entries.", nameof(conflicts));
        }

        if (copiedConflicts.Any(item => item.Value.Kind != identity.ExpectedValueKind))
        {
            throw new ArgumentException(
                "Every conflicting value must use the field's value kind.",
                nameof(conflicts));
        }

        var duplicateConflict = copiedConflicts
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateConflict is not null)
        {
            throw new ArgumentException("Conflicts cannot contain duplicate candidates.", nameof(conflicts));
        }

        Conflicts = [.. copiedConflicts.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
        ConflictDecision = conflictDecision;
        ValidateInvariant();
    }

    public CandidateProfileFieldIdentity Identity { get; }

    public CandidateEvidenceState State { get; }

    public CandidateFactValue? Value { get; }

    public CandidateFactProvenance? Provenance { get; }

    public CandidateUnavailableReason? UnavailableReason { get; }

    public ImmutableArray<CandidateConflictValue> Conflicts { get; }

    public CandidateConflictDecision? ConflictDecision { get; }

    public ImmutableArray<CandidateEvidenceReference> Evidence { get; }

    public static CandidateProfileFact Confirmed(
        CandidateProfileFieldIdentity identity,
        CandidateFactValue value,
        CandidateFactProvenance provenance,
        IEnumerable<CandidateEvidenceReference> evidence) =>
        new(identity, CandidateEvidenceState.Confirmed, value, provenance, null, [], null, evidence);

    public static CandidateProfileFact Incomplete(
        CandidateProfileFieldIdentity identity,
        CandidateUnavailableReason reason,
        IEnumerable<CandidateEvidenceReference> evidence) =>
        Unavailable(identity, CandidateEvidenceState.Incomplete, reason, evidence);

    public static CandidateProfileFact Unsupported(
        CandidateProfileFieldIdentity identity,
        CandidateUnavailableReason reason,
        IEnumerable<CandidateEvidenceReference> evidence) =>
        Unavailable(identity, CandidateEvidenceState.Unsupported, reason, evidence);

    public static CandidateProfileFact Stale(
        CandidateProfileFieldIdentity identity,
        CandidateFactValue lastObservedValue,
        CandidateFactProvenance provenance,
        CandidateUnavailableReason reason,
        IEnumerable<CandidateEvidenceReference> evidence) =>
        new(identity, CandidateEvidenceState.Stale, lastObservedValue, provenance, reason, [], null, evidence);

    public static CandidateProfileFact Conflicting(
        CandidateProfileFieldIdentity identity,
        IEnumerable<CandidateConflictValue> conflicts,
        CandidateConflictDecision decision,
        IEnumerable<CandidateEvidenceReference> evidence) =>
        new(identity, CandidateEvidenceState.Conflicting, null, null, null, conflicts, decision, evidence);

    internal string StableKey
    {
        get
        {
            var value = Value?.StableKey ?? "NONE";
            var provenance = Provenance?.StableKey ?? "NONE";
            var unavailable = UnavailableReason?.Code ?? "NONE";
            var conflicts = string.Join("||", Conflicts.Select(item => item.StableKey));
            var decision = ConflictDecision?.StableKey ?? "NONE";
            var evidence = string.Join("||", Evidence.Select(item => item.StableKey));
            return string.Join('|',
                Identity.StableKey,
                CandidateProfileText.EnumKey(State),
                value,
                provenance,
                unavailable,
                conflicts,
                decision,
                evidence);
        }
    }

    private static CandidateProfileFact Unavailable(
        CandidateProfileFieldIdentity identity,
        CandidateEvidenceState state,
        CandidateUnavailableReason reason,
        IEnumerable<CandidateEvidenceReference> evidence) =>
        new(identity, state, null, null, reason, [], null, evidence);

    private void ValidateInvariant()
    {
        switch (State)
        {
            case CandidateEvidenceState.Confirmed:
                if (Value is null || Provenance is null || UnavailableReason is not null
                    || !Conflicts.IsEmpty || ConflictDecision is not null)
                {
                    throw new ArgumentException("Confirmed evidence requires one value and provenance only.");
                }
                break;
            case CandidateEvidenceState.Incomplete:
            case CandidateEvidenceState.Unsupported:
                if (Value is not null || Provenance is not null || UnavailableReason is null
                    || !Conflicts.IsEmpty || ConflictDecision is not null)
                {
                    throw new ArgumentException("Unavailable evidence requires a reason and cannot carry a value.");
                }
                break;
            case CandidateEvidenceState.Stale:
                if (Value is null || Provenance is null || UnavailableReason is null
                    || !Conflicts.IsEmpty || ConflictDecision is not null)
                {
                    throw new ArgumentException("Stale evidence requires its last value, provenance, and reason.");
                }
                break;
            case CandidateEvidenceState.Conflicting:
                if (Value is not null || Provenance is not null || UnavailableReason is not null
                    || Conflicts.Length < 2 || ConflictDecision is null)
                {
                    throw new ArgumentException("Conflicting evidence requires at least two candidates and a precedence decision.");
                }

                if (ConflictDecision.Kind == CandidateConflictDecisionKind.SelectedBySourcePrecedence
                    && !Conflicts.Any(item => item.Provenance == ConflictDecision.SelectedProvenance))
                {
                    throw new ArgumentException("The selected conflict provenance must identify a retained candidate.");
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(State), State, "Unknown candidate evidence state.");
        }
    }
}
