using System.Collections.Immutable;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public sealed class TargetCounterPlaybook
{
    public TargetCounterPlaybook(
        TargetCounterPlaybookIdentity identity,
        IEnumerable<TargetCounterPlaybookGoal> goals,
        IEnumerable<string> evidenceReferences)
    {
        Identity = identity
            ?? throw new ArgumentNullException(nameof(identity));
        ArgumentNullException.ThrowIfNull(goals);
        var goalValues = goals.ToImmutableArray();
        if (goalValues.Length == 0)
        {
            throw new ArgumentException(
                "A counter playbook requires at least one response goal.",
                nameof(goals));
        }

        if (goalValues.Any(goal => goal is null))
        {
            throw new ArgumentException(
                "Playbook goals cannot contain null entries.",
                nameof(goals));
        }

        if (goalValues.DistinctBy(goal => goal.Code, StringComparer.Ordinal)
            .Count() != goalValues.Length)
        {
            throw new ArgumentException(
                "Playbook goal codes must be unique.",
                nameof(goals));
        }

        ArgumentNullException.ThrowIfNull(evidenceReferences);
        var references = evidenceReferences
            .Select(value => TargetProfileText.Code(
                value,
                nameof(evidenceReferences)))
            .ToImmutableArray();
        if (references.Length == 0)
        {
            throw new ArgumentException(
                "A counter playbook requires evidence.",
                nameof(evidenceReferences));
        }

        if (references.Distinct(StringComparer.Ordinal).Count()
            != references.Length)
        {
            throw new ArgumentException(
                "Playbook evidence references must be unique.",
                nameof(evidenceReferences));
        }

        Goals =
        [
            .. goalValues
                .OrderBy(goal => goal.Sequence)
                .ThenBy(goal => goal.Priority)
                .ThenBy(goal => goal.Code, StringComparer.Ordinal)
        ];
        EvidenceReferences = [.. references.Order(StringComparer.Ordinal)];
    }

    public TargetCounterPlaybookIdentity Identity { get; }

    public ImmutableArray<TargetCounterPlaybookGoal> Goals { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    public ImmutableArray<TargetCounterPlaybookGap> KnownGaps =>
        [.. Goals.SelectMany(goal => goal.KnownGaps)];

    public string StableKey => Identity.StableKey;

    internal string ContentKey => TargetProfileText.Stable(
        StableKey,
        TargetProfileText.StableCollection(
            Goals.Select(goal => goal.ContentKey)),
        TargetProfileText.StableCollection(EvidenceReferences));
}
