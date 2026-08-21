using System.Collections.Immutable;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed class TacticalExecutionContextReadRequest
{
    public TacticalExecutionContextReadRequest(
        CombatSnapshotReadRequest snapshotRequest,
        IEnumerable<string> targetGoalCodes,
        IEnumerable<TacticalRuleEvidenceObservation> evidence,
        TacticalExecutionProposal? proposal = null,
        TacticalExecutionObservation? currentObservation = null,
        DateTimeOffset? currentObservationAt = null)
    {
        SnapshotRequest = snapshotRequest
            ?? throw new ArgumentNullException(nameof(snapshotRequest));
        TargetGoalCodes = CopyUnique(
            targetGoalCodes,
            nameof(targetGoalCodes));
        Evidence = CopyUniqueEvidence(evidence);
        Proposal = proposal;
        CurrentObservation = currentObservation;
        if (currentObservation is null && currentObservationAt.HasValue)
        {
            throw new ArgumentException(
                "An execution-observation time requires an observation.",
                nameof(currentObservationAt));
        }

        CurrentObservationAtUtc = currentObservationAt?.ToUniversalTime();
    }

    public CombatSnapshotReadRequest SnapshotRequest { get; }

    public ImmutableArray<string> TargetGoalCodes { get; }

    public ImmutableArray<TacticalRuleEvidenceObservation> Evidence { get; }

    public TacticalExecutionProposal? Proposal { get; }

    public TacticalExecutionObservation? CurrentObservation { get; }

    public DateTimeOffset? CurrentObservationAtUtc { get; }

    private static ImmutableArray<string> CopyUnique(
        IEnumerable<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.Select(value =>
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Tactical target goal codes cannot be blank.",
                    parameterName);
            }

            return value.Trim();
        }).ToImmutableArray();
        if (copied.IsEmpty
            || copied.Distinct(StringComparer.Ordinal).Count()
                != copied.Length)
        {
            throw new ArgumentException(
                "At least one unique tactical target goal code is required.",
                parameterName);
        }

        return [.. copied.Order(StringComparer.Ordinal)];
    }

    private static ImmutableArray<TacticalRuleEvidenceObservation>
        CopyUniqueEvidence(
            IEnumerable<TacticalRuleEvidenceObservation> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "Tactical evidence cannot contain null entries.",
                nameof(values));
        }

        var duplicate = copied.GroupBy(
                item => string.Join('|',
                    item.Identity.Code,
                    item.Scope,
                    item.Source),
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                "Tactical evidence identities must be unique per scope and source.",
                nameof(values));
        }

        return [.. copied.OrderBy(
            item => string.Join('|',
                item.Identity.Code,
                item.Scope,
                item.Source),
            StringComparer.Ordinal)];
    }
}
