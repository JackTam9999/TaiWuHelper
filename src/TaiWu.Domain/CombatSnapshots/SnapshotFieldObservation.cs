namespace TaiWu.Domain.CombatSnapshots;

public sealed record SnapshotFieldObservation<T>
{
    public SnapshotFieldObservation(
        T value,
        SnapshotFieldSource source)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(source);

        Value = value;
        Source = source;
    }

    public T Value { get; }

    public SnapshotFieldSource Source { get; }
}
