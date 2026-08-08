namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonValue<T>
{
    private readonly T? _value;

    private LoadoutComparisonValue(
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
            $"Comparison value is unavailable: {UnavailableReason}");

    public static LoadoutComparisonValue<T> Available(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new LoadoutComparisonValue<T>(true, value, null);
    }

    public static LoadoutComparisonValue<T> Unavailable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "An unavailable comparison value requires a reason.",
                nameof(reason));
        }

        return new LoadoutComparisonValue<T>(
            false,
            default,
            reason.Trim());
    }
}
