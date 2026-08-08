namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonReference
{
    public const int MaximumLength = 128;

    public LoadoutComparisonReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A comparison reference is required.",
                nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > MaximumLength
            || normalized.Any(char.IsWhiteSpace)
            || normalized.Contains('\\')
            || normalized.Contains('/')
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.Length >= 2
                && char.IsAsciiLetter(normalized[0])
                && normalized[1] == ':')
        {
            throw new ArgumentException(
                "A comparison reference must be a short logical identity, "
                + "not a local path or exception detail.",
                nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
