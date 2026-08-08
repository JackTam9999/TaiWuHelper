using System.Collections.Immutable;
using TaiWu.Domain.CombatRecommendations;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparison
{
    public LoadoutComparison(
        LoadoutComparisonReference comparisonReference,
        LoadoutComparisonReference snapshotReference,
        LoadoutComparisonReference targetReference,
        IEnumerable<LoadoutComparisonColumn> columns,
        IEnumerable<LoadoutComparisonBaselineProvenance>
            baselineProvenance)
    {
        ComparisonReference = comparisonReference
            ?? throw new ArgumentNullException(nameof(comparisonReference));
        SnapshotReference = snapshotReference
            ?? throw new ArgumentNullException(nameof(snapshotReference));
        TargetReference = targetReference
            ?? throw new ArgumentNullException(nameof(targetReference));
        ArgumentNullException.ThrowIfNull(columns);
        Columns = [.. columns];
        if (Columns.Any(column => column is null))
        {
            throw new ArgumentException(
                "A comparison cannot contain null columns.",
                nameof(columns));
        }

        if (Columns.Count(column =>
                column.Kind == LoadoutComparisonColumnKind.Current) != 1)
        {
            throw new ArgumentException(
                "A comparison requires exactly one Current column.",
                nameof(columns));
        }

        var kinds = Columns.Select(column => column.Kind);
        if (kinds.Distinct().Count() != Columns.Length)
        {
            throw new ArgumentException(
                "A comparison can contain each policy column at most once.",
                nameof(columns));
        }

        if (!kinds.SequenceEqual(kinds.Order()))
        {
            throw new ArgumentException(
                "Comparison columns must use Current, Safe, Balanced, "
                + "Aggressive order.",
                nameof(columns));
        }

        ArgumentNullException.ThrowIfNull(baselineProvenance);
        BaselineProvenance = [.. baselineProvenance];
        if (BaselineProvenance.Any(value => value is null))
        {
            throw new ArgumentException(
                "Baseline provenance cannot contain null entries.",
                nameof(baselineProvenance));
        }

        var fields = BaselineProvenance.Select(value => value.Field);
        if (fields.Distinct().Count() != BaselineProvenance.Length
            || !fields.SequenceEqual(fields.Order()))
        {
            throw new ArgumentException(
                "Baseline provenance fields must be unique and use "
                + "canonical order.",
                nameof(baselineProvenance));
        }
    }

    public LoadoutComparisonReference ComparisonReference { get; }

    public LoadoutComparisonReference SnapshotReference { get; }

    public LoadoutComparisonReference TargetReference { get; }

    public ImmutableArray<LoadoutComparisonColumn> Columns { get; }

    public ImmutableArray<LoadoutComparisonBaselineProvenance>
        BaselineProvenance
    { get; }

    public LoadoutComparisonColumn Current => Columns.Single(column =>
        column.Kind == LoadoutComparisonColumnKind.Current);

    public LoadoutComparisonColumn? GetPolicy(RecommendationPolicy policy)
    {
        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unknown recommendation policy.");
        }

        var kind = policy switch
        {
            RecommendationPolicy.Safe => LoadoutComparisonColumnKind.Safe,
            RecommendationPolicy.Balanced =>
                LoadoutComparisonColumnKind.Balanced,
            RecommendationPolicy.Aggressive =>
                LoadoutComparisonColumnKind.Aggressive,
            _ => throw new InvalidOperationException(
                "Unknown recommendation policy.")
        };
        return Columns.SingleOrDefault(column => column.Kind == kind);
    }
}
