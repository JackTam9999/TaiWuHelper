namespace TaiWu.Domain.CombatSnapshots;

public sealed record SnapshotFieldSource
{
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

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "A snapshot field source requires an evidence reference.",
                nameof(evidenceReference));
        }

        FieldPath = fieldPath.Trim();
        Source = source;
        CapturedAtUtc = capturedAt.ToUniversalTime();
        EvidenceReference = evidenceReference.Trim();
    }

    public string FieldPath { get; }

    public SnapshotDataSource Source { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public string EvidenceReference { get; }
}
