using System.Collections.Immutable;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonReason
{
    public LoadoutComparisonReason(
        LoadoutComparisonReference code,
        string summary,
        IEnumerable<LoadoutComparisonReference> evidenceReferences,
        IEnumerable<LoadoutComparisonReference> threatReferences)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException(
                "A comparison reason requires a summary.",
                nameof(summary));
        }

        Summary = summary.Trim();
        EvidenceReferences =
            LoadoutComparisonDiagnostic.CopyOrderedReferences(
                evidenceReferences,
                nameof(evidenceReferences));
        ThreatReferences =
            LoadoutComparisonDiagnostic.CopyOrderedReferences(
                threatReferences,
                nameof(threatReferences));
    }

    public LoadoutComparisonReference Code { get; }

    public string Summary { get; }

    public ImmutableArray<LoadoutComparisonReference> EvidenceReferences
    {
        get;
    }

    public ImmutableArray<LoadoutComparisonReference> ThreatReferences
    {
        get;
    }
}
