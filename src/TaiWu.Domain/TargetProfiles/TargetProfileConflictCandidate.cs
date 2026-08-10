using System.Collections.Immutable;

namespace TaiWu.Domain.TargetProfiles;

public sealed class TargetProfileConflictCandidate
{
    public TargetProfileConflictCandidate(
        TargetProfileFacetValue value,
        IEnumerable<TargetProfileEvidence> evidence)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        ArgumentNullException.ThrowIfNull(evidence);
        var values = evidence.ToImmutableArray();
        if (values.Length == 0)
        {
            throw new ArgumentException(
                "A conflicting value requires evidence.",
                nameof(evidence));
        }

        if (values.Any(item => item is null))
        {
            throw new ArgumentException(
                "Conflict evidence cannot contain null entries.",
                nameof(evidence));
        }

        var duplicate = values
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                "Conflict evidence cannot contain duplicate entries.",
                nameof(evidence));
        }

        Evidence = [.. values.OrderBy(item => item.StableKey,
            StringComparer.Ordinal)];
    }

    public TargetProfileFacetValue Value { get; }

    public ImmutableArray<TargetProfileEvidence> Evidence { get; }

    internal string StableKey => TargetProfileText.Stable(
        Value.StableKey,
        TargetProfileText.StableCollection(
            Evidence.Select(item => item.StableKey)));
}
