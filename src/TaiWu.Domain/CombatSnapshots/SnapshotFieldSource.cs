namespace TaiWu.Domain.CombatSnapshots;

public sealed record SnapshotFieldSource
{
    public const int MaximumEvidenceReferenceLength = 128;

    public SnapshotFieldSource(
        string fieldPath,
        SnapshotDataSource source,
        DateTimeOffset capturedAt,
        string evidenceReference)
    {
        if (string.IsNullOrWhiteSpace(fieldPath))
        {
            throw new ArgumentException(
                "A snapshot field source requires a field path.",
                nameof(fieldPath));
        }

        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unknown snapshot data source.");
        }

        FieldPath = ValidateFieldPath(fieldPath, nameof(fieldPath));
        Source = source;
        CapturedAtUtc = capturedAt.ToUniversalTime();
        EvidenceReference = NormalizeEvidenceReference(evidenceReference);
    }

    public string FieldPath { get; }

    public SnapshotDataSource Source { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public string EvidenceReference { get; }

    public static string NormalizeEvidenceReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A snapshot evidence reference is required.",
                nameof(value));
        }

        return ValidateEvidenceReference(value, nameof(value));
    }

    private static string ValidateFieldPath(
        string value,
        string parameterName)
    {
        var normalized = value.Trim();
        if (normalized.Length > MaximumEvidenceReferenceLength
            || normalized.Any(char.IsWhiteSpace)
            || normalized.Contains('\\')
            || normalized.Contains('/')
            || normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A snapshot field path must be a logical field identity, "
                + "not a local path.",
                parameterName);
        }

        return normalized;
    }

    private static string ValidateEvidenceReference(
        string value,
        string parameterName)
    {
        var normalized = value.Trim();
        if (normalized.Any(char.IsWhiteSpace)
            || normalized.Contains('\\')
            || normalized.Contains('/')
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.Length >= 2
                && char.IsAsciiLetter(normalized[0])
                && normalized[1] == ':')
        {
            throw new ArgumentException(
                "A snapshot evidence reference must be short and opaque, "
                + "not a path or exception detail.",
                parameterName);
        }

        return normalized;
    }
}
