using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSnapshot
{
    public CombatSnapshot(
        CombatSnapshotMetadata metadata,
        PlayerCombatSnapshot player,
        TargetCombatSnapshot target,
        IEnumerable<SnapshotWarning> warnings,
        IEnumerable<SnapshotFieldSource>? fieldSources = null)
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

        var sourceValues =
            (fieldSources ?? []).ToImmutableArray();
        if (sourceValues.Any(source => source is null))
        {
            throw new ArgumentException(
                "Snapshot field sources cannot contain null entries.",
                nameof(fieldSources));
        }

        var duplicateSource = sourceValues
            .GroupBy(source => source.FieldPath, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateSource is not null)
        {
            throw new ArgumentException(
                $"Duplicate source for snapshot field "
                + $"'{duplicateSource.Key}'.",
                nameof(fieldSources));
        }

        Metadata = metadata;
        Player = player;
        Target = target;
        Warnings = warningValues;
        FieldSources = sourceValues;
    }

    public CombatSnapshotMetadata Metadata { get; }

    public PlayerCombatSnapshot Player { get; }

    public TargetCombatSnapshot Target { get; }

    public ImmutableArray<SnapshotWarning> Warnings { get; }

    public ImmutableArray<SnapshotFieldSource> FieldSources { get; }
}
