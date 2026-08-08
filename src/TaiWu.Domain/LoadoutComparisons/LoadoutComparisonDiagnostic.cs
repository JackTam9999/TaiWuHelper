using System.Collections.Immutable;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonDiagnostic
{
    public LoadoutComparisonDiagnostic(
        LoadoutComparisonReference code,
        string summary,
        IEnumerable<LoadoutComparisonReference> evidenceReferences)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException(
                "A comparison diagnostic requires a summary.",
                nameof(summary));
        }

        Summary = summary.Trim();
        EvidenceReferences = CopyOrderedReferences(
            evidenceReferences,
            nameof(evidenceReferences));
    }

    public LoadoutComparisonReference Code { get; }

    public string Summary { get; }

    public ImmutableArray<LoadoutComparisonReference> EvidenceReferences
    {
        get;
    }

    internal static ImmutableArray<LoadoutComparisonReference>
        CopyOrderedReferences(
            IEnumerable<LoadoutComparisonReference> references,
            string parameterName)
    {
        ArgumentNullException.ThrowIfNull(references, parameterName);
        var values = references.ToImmutableArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Logical references cannot contain null entries.",
                parameterName);
        }

        if (values
            .Select(value => value.Value)
            .Distinct(StringComparer.Ordinal)
            .Count() != values.Length)
        {
            throw new ArgumentException(
                "Logical references cannot contain duplicates.",
                parameterName);
        }

        if (!values
                .Select(value => value.Value)
                .SequenceEqual(
                    values
                        .Select(value => value.Value)
                        .Order(StringComparer.Ordinal)))
        {
            throw new ArgumentException(
                "Logical references must use ordinal order.",
                parameterName);
        }

        return values;
    }
}
