namespace TaiWu.Domain.CombatSnapshots;

public sealed record CharacterFeatureSnapshot
{
    public CharacterFeatureSnapshot(
        int featureId,
        SnapshotValue<string> displayName,
        SnapshotValue<int> level)
    {
        if (featureId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(featureId),
                featureId,
                "Feature ID cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(level);

        if (level.IsAvailable && level.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level),
                "An available feature level cannot be negative.");
        }

        FeatureId = featureId;
        DisplayName = displayName;
        Level = level;
    }

    public int FeatureId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public SnapshotValue<int> Level { get; }
}
