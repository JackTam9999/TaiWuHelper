using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record TargetLoadoutObservationMergeResult
{
    public TargetLoadoutObservationMergeResult(
        TargetLoadoutMergeStatus status,
        CombatSnapshot snapshot,
        TargetLoadoutObservation observation,
        SnapshotEvidenceField<CombatLoadoutSnapshot> loadoutEvidence,
        IEnumerable<TargetSkillDirectionEvidence>? directionEvidence = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown target-loadout merge status.");
        }

        Status = status;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Observation = observation
            ?? throw new ArgumentNullException(nameof(observation));
        LoadoutEvidence = loadoutEvidence
            ?? throw new ArgumentNullException(nameof(loadoutEvidence));

        var suppliedDirectionValues = (directionEvidence ?? [])
            .ToImmutableArray();
        if (suppliedDirectionValues.Any(value => value is null))
        {
            throw new ArgumentException(
                "Direction evidence cannot contain null entries.",
                nameof(directionEvidence));
        }

        var directionValues = suppliedDirectionValues
            .OrderBy(value => value.SkillId)
            .ToImmutableArray();

        if (directionValues
            .GroupBy(value => value.SkillId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Direction evidence cannot duplicate a skill ID.",
                nameof(directionEvidence));
        }

        DirectionEvidence = directionValues;
    }

    public TargetLoadoutMergeStatus Status { get; }

    public CombatSnapshot Snapshot { get; }

    public TargetLoadoutObservation Observation { get; }

    public SnapshotEvidenceField<CombatLoadoutSnapshot> LoadoutEvidence
    {
        get;
    }

    public ImmutableArray<TargetSkillDirectionEvidence> DirectionEvidence
    {
        get;
    }
}
