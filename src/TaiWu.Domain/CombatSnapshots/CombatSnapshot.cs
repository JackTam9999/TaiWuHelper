using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSnapshot
{
    public CombatSnapshot(
        CombatSnapshotMetadata metadata,
        PlayerCombatSnapshot player,
        TargetCombatSnapshot target,
        IEnumerable<SnapshotWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(warnings);

        var warningValues = warnings.ToImmutableArray();
        if (warningValues.Any(warning => warning is null))
        {
            throw new ArgumentException(
                "Snapshot warnings cannot contain null entries.",
                nameof(warnings));
        }

        Metadata = metadata;
        Player = player;
        Target = target;
        Warnings = warningValues;
    }

    public CombatSnapshotMetadata Metadata { get; }

    public PlayerCombatSnapshot Player { get; }

    public TargetCombatSnapshot Target { get; }

    public ImmutableArray<SnapshotWarning> Warnings { get; }
}
