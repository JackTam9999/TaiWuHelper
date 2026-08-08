using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonBaselineProvenance
{
    public LoadoutComparisonBaselineProvenance(
        LoadoutComparisonBaselineField field,
        SnapshotDataSource source,
        DateTimeOffset capturedAt,
        LoadoutComparisonReference evidenceReference)
    {
        if (!Enum.IsDefined(field))
        {
            throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "Unknown comparison baseline field.");
        }

        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unknown comparison baseline source.");
        }

        Field = field;
        Source = source;
        CapturedAtUtc = capturedAt.ToUniversalTime();
        EvidenceReference = evidenceReference
            ?? throw new ArgumentNullException(nameof(evidenceReference));
    }

    public LoadoutComparisonBaselineField Field { get; }

    public SnapshotDataSource Source { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public LoadoutComparisonReference EvidenceReference { get; }
}
