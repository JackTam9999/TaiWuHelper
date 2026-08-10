using System.Collections.Immutable;

namespace TaiWu.Domain.TargetProfiles;

public sealed class TargetProfileFacet
{
    private TargetProfileFacet(
        TargetProfileFacetIdentity identity,
        TargetProfileEvidenceState state,
        TargetProfileFacetValue? value,
        IEnumerable<TargetProfileEvidence> evidence,
        IEnumerable<TargetProfileConflictCandidate> conflictCandidates,
        TargetProfileUnavailableReason? unavailableReason)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown target-profile evidence state.");
        }

        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(conflictCandidates);
        var evidenceValues = evidence.ToImmutableArray();
        if (evidenceValues.Length == 0)
        {
            throw new ArgumentException(
                "A target-profile facet requires evidence.",
                nameof(evidence));
        }

        if (evidenceValues.Any(item => item is null))
        {
            throw new ArgumentException(
                "Target-profile evidence cannot contain null entries.",
                nameof(evidence));
        }

        var duplicateEvidence = evidenceValues
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateEvidence is not null)
        {
            throw new ArgumentException(
                "A target-profile facet cannot contain duplicate evidence.",
                nameof(evidence));
        }

        var conflicts = conflictCandidates.ToImmutableArray();
        if (conflicts.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Target-profile conflict candidates cannot contain null.",
                nameof(conflictCandidates));
        }

        if (state == TargetProfileEvidenceState.Confirmed)
        {
            if (value is null)
            {
                throw new ArgumentException(
                    "A confirmed target-profile facet requires a typed value.",
                    nameof(value));
            }

            EnsureCompatible(identity, value, nameof(value));
            if (unavailableReason is not null || conflicts.Length != 0)
            {
                throw new ArgumentException(
                    "A confirmed target-profile facet cannot have an "
                    + "unavailable reason or conflict candidates.");
            }
        }
        else
        {
            if (value is not null)
            {
                throw new ArgumentException(
                    "A non-confirmed target-profile facet cannot expose one "
                    + "authoritative value.",
                    nameof(value));
            }

            if (unavailableReason is null)
            {
                throw new ArgumentException(
                    "A non-confirmed target-profile facet requires an "
                    + "unavailable reason.",
                    nameof(unavailableReason));
            }
        }

        if (state == TargetProfileEvidenceState.Conflicting)
        {
            if (conflicts.Length < 2)
            {
                throw new ArgumentException(
                    "A conflicting facet requires at least two candidates.",
                    nameof(conflictCandidates));
            }

            foreach (var conflict in conflicts)
            {
                EnsureCompatible(
                    identity,
                    conflict.Value,
                    nameof(conflictCandidates));
            }

            var duplicateValue = conflicts
                .GroupBy(candidate => candidate.Value.StableKey,
                    StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateValue is not null)
            {
                throw new ArgumentException(
                    "Conflict candidates must contain distinct typed values.",
                    nameof(conflictCandidates));
            }
        }
        else if (conflicts.Length != 0)
        {
            throw new ArgumentException(
                "Only a conflicting facet can contain conflict candidates.",
                nameof(conflictCandidates));
        }

        State = state;
        Value = value;
        Evidence = [.. evidenceValues.OrderBy(item => item.StableKey,
            StringComparer.Ordinal)];
        ConflictCandidates = [.. conflicts.OrderBy(
            candidate => candidate.StableKey,
            StringComparer.Ordinal)];
        UnavailableReason = unavailableReason;
    }

    public TargetProfileFacetIdentity Identity { get; }

    public TargetProfileEvidenceState State { get; }

    public TargetProfileFacetValue? Value { get; }

    public ImmutableArray<TargetProfileEvidence> Evidence { get; }

    public ImmutableArray<TargetProfileConflictCandidate> ConflictCandidates
    { get; }

    public TargetProfileUnavailableReason? UnavailableReason { get; }

    public static TargetProfileFacet Confirmed(
        TargetProfileFacetIdentity identity,
        TargetProfileFacetValue value,
        IEnumerable<TargetProfileEvidence> evidence) => new(
            identity,
            TargetProfileEvidenceState.Confirmed,
            value,
            evidence,
            [],
            unavailableReason: null);

    public static TargetProfileFacet Incomplete(
        TargetProfileFacetIdentity identity,
        IEnumerable<TargetProfileEvidence> evidence,
        TargetProfileUnavailableReason unavailableReason) => new(
            identity,
            TargetProfileEvidenceState.Incomplete,
            value: null,
            evidence,
            [],
            unavailableReason);

    public static TargetProfileFacet Unsupported(
        TargetProfileFacetIdentity identity,
        IEnumerable<TargetProfileEvidence> evidence,
        TargetProfileUnavailableReason unavailableReason) => new(
            identity,
            TargetProfileEvidenceState.Unsupported,
            value: null,
            evidence,
            [],
            unavailableReason);

    public static TargetProfileFacet Conflicting(
        TargetProfileFacetIdentity identity,
        IEnumerable<TargetProfileConflictCandidate> conflictCandidates,
        TargetProfileUnavailableReason unavailableReason)
    {
        ArgumentNullException.ThrowIfNull(conflictCandidates);
        var conflicts = conflictCandidates.ToImmutableArray();
        var evidence = conflicts
            .Where(candidate => candidate is not null)
            .SelectMany(candidate => candidate.Evidence)
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal);
        return new TargetProfileFacet(
            identity,
            TargetProfileEvidenceState.Conflicting,
            value: null,
            evidence,
            conflicts,
            unavailableReason);
    }

    internal string StableKey
    {
        get
        {
            var conflicts = TargetProfileText.StableCollection(
                ConflictCandidates.Select(candidate => candidate.StableKey));
            return TargetProfileText.Stable(
                Identity.StableKey,
                ((int)State).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Value?.StableKey ?? string.Empty,
                TargetProfileText.StableCollection(
                    Evidence.Select(item => item.StableKey)),
                conflicts,
                UnavailableReason?.Code ?? string.Empty);
        }
    }

    private static void EnsureCompatible(
        TargetProfileFacetIdentity identity,
        TargetProfileFacetValue value,
        string parameterName)
    {
        if (identity.Dimension != value.Dimension
            || !string.Equals(
                identity.Code,
                value.Code,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A facet value must match the facet dimension and code.",
                parameterName);
        }
    }
}
