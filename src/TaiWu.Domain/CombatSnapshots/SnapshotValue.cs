namespace TaiWu.Domain.CombatSnapshots;

public sealed record SnapshotValue<T>
{
    private readonly T? _value;

    private SnapshotValue(
        bool isAvailable,
        T? value,
        string? unavailableReason)
    {
        IsAvailable = isAvailable;
        _value = value;
        UnavailableReason = unavailableReason;
    }

    public bool IsAvailable { get; }

    public string? UnavailableReason { get; }

    public T Value => IsAvailable
        ? _value!
        : throw new InvalidOperationException(
            $"Snapshot value is unavailable: {UnavailableReason}");

    public static SnapshotValue<T> Available(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new SnapshotValue<T>(true, value, null);
    }

    public static SnapshotValue<T> Unavailable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "An unavailable snapshot value requires a reason.",
                nameof(reason));
        }

        return new SnapshotValue<T>(false, default, reason.Trim());
    }
}
