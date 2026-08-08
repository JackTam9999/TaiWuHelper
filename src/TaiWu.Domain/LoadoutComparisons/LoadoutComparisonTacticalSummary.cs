using System.Collections.Immutable;
using TaiWu.Domain.CombatRecommendations;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonTacticalSummary
{
    public LoadoutComparisonTacticalSummary(
        LoadoutComparisonValue<int> manualActionCount,
        LoadoutComparisonValue<LoadoutComparisonSkillIdentity> activeDefense,
        LoadoutComparisonValue<LoadoutComparisonSkillIdentity> activeAgility,
        IEnumerable<LoadoutComparisonReference> coveredThreats,
        IEnumerable<LoadoutComparisonReference> unresolvedThreats,
        IEnumerable<LoadoutComparisonReference> conditions,
        IEnumerable<LoadoutComparisonReference> caveats,
        IEnumerable<LoadoutComparisonReference> evidenceReferences,
        IEnumerable<LoadoutComparisonScoreComponent> scoreComponents)
    {
        ManualActionCount = manualActionCount
            ?? throw new ArgumentNullException(nameof(manualActionCount));
        if (ManualActionCount.IsAvailable && ManualActionCount.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manualActionCount),
                ManualActionCount.Value,
                "A manual-action count cannot be negative.");
        }

        ActiveDefense = activeDefense
            ?? throw new ArgumentNullException(nameof(activeDefense));
        ActiveAgility = activeAgility
            ?? throw new ArgumentNullException(nameof(activeAgility));
        CoveredThreats = LoadoutComparisonDiagnostic.CopyOrderedReferences(
            coveredThreats,
            nameof(coveredThreats));
        UnresolvedThreats = LoadoutComparisonDiagnostic.CopyOrderedReferences(
            unresolvedThreats,
            nameof(unresolvedThreats));
        Conditions = LoadoutComparisonDiagnostic.CopyOrderedReferences(
            conditions,
            nameof(conditions));
        Caveats = LoadoutComparisonDiagnostic.CopyOrderedReferences(
            caveats,
            nameof(caveats));
        EvidenceReferences =
            LoadoutComparisonDiagnostic.CopyOrderedReferences(
                evidenceReferences,
                nameof(evidenceReferences));

        if (CoveredThreats.Intersect(UnresolvedThreats).Any())
        {
            throw new ArgumentException(
                "One tactical summary cannot mark a threat as both covered "
                + "and unresolved.",
                nameof(unresolvedThreats));
        }

        ArgumentNullException.ThrowIfNull(scoreComponents);
        ScoreComponents = [.. scoreComponents];
        if (ScoreComponents.Any(component => component is null))
        {
            throw new ArgumentException(
                "Score components cannot contain null entries.",
                nameof(scoreComponents));
        }

        var kinds = ScoreComponents.Select(component => component.Kind);
        if (kinds.Distinct().Count() != ScoreComponents.Length
            || !kinds.SequenceEqual(kinds.Order()))
        {
            throw new ArgumentException(
                "Score components must be unique and use canonical order.",
                nameof(scoreComponents));
        }
    }

    public LoadoutComparisonValue<int> ManualActionCount { get; }

    public LoadoutComparisonValue<LoadoutComparisonSkillIdentity>
        ActiveDefense
    { get; }

    public LoadoutComparisonValue<LoadoutComparisonSkillIdentity>
        ActiveAgility
    { get; }

    public ImmutableArray<LoadoutComparisonReference> CoveredThreats { get; }

    public ImmutableArray<LoadoutComparisonReference> UnresolvedThreats
    {
        get;
    }

    public ImmutableArray<LoadoutComparisonReference> Conditions { get; }

    public ImmutableArray<LoadoutComparisonReference> Caveats { get; }

    public ImmutableArray<LoadoutComparisonReference> EvidenceReferences
    {
        get;
    }

    public ImmutableArray<LoadoutComparisonScoreComponent> ScoreComponents
    {
        get;
    }
}
