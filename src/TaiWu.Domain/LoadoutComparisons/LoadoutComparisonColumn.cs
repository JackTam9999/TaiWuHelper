using TaiWu.Domain.CombatRecommendations;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonColumn
{
    public LoadoutComparisonColumn(
        LoadoutComparisonColumnKind kind,
        LoadoutComparisonColumnStatus status,
        LoadoutComparisonLoadout? loadout,
        LoadoutComparisonTacticalSummary? tacticalSummary,
        LoadoutComparisonDiagnostic? diagnostic)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown comparison column.");
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown comparison column status.");
        }

        if (kind == LoadoutComparisonColumnKind.Current)
        {
            if (status != LoadoutComparisonColumnStatus.Available
                || loadout is null
                || tacticalSummary is not null
                || diagnostic is not null)
            {
                throw new ArgumentException(
                    "Current must be an available loadout without policy "
                    + "tactics or diagnostics.");
            }
        }
        else if (status == LoadoutComparisonColumnStatus.Available)
        {
            if (loadout is null
                || tacticalSummary is null
                || diagnostic is not null)
            {
                throw new ArgumentException(
                    "A feasible policy requires a loadout and tactical "
                    + "summary without a failure diagnostic.");
            }
        }
        else if (loadout is not null
            || tacticalSummary is not null
            || diagnostic is null)
        {
            throw new ArgumentException(
                "An infeasible or unavailable policy requires a diagnostic "
                + "and cannot contain a proposed loadout.");
        }

        if (loadout is not null)
        {
            ValidateMembership(kind, loadout);
        }

        Kind = kind;
        Status = status;
        Loadout = loadout;
        TacticalSummary = tacticalSummary;
        Diagnostic = diagnostic;
    }

    public LoadoutComparisonColumnKind Kind { get; }

    public LoadoutComparisonColumnStatus Status { get; }

    public LoadoutComparisonLoadout? Loadout { get; }

    public LoadoutComparisonTacticalSummary? TacticalSummary { get; }

    public LoadoutComparisonDiagnostic? Diagnostic { get; }

    public RecommendationPolicy? Policy => Kind switch
    {
        LoadoutComparisonColumnKind.Current => null,
        LoadoutComparisonColumnKind.Safe => RecommendationPolicy.Safe,
        LoadoutComparisonColumnKind.Balanced =>
            RecommendationPolicy.Balanced,
        LoadoutComparisonColumnKind.Aggressive =>
            RecommendationPolicy.Aggressive,
        _ => throw new InvalidOperationException(
            "Unknown comparison column kind.")
    };

    private static void ValidateMembership(
        LoadoutComparisonColumnKind kind,
        LoadoutComparisonLoadout loadout)
    {
        var invalid = loadout.Categories
            .SelectMany(category => category.Skills)
            .FirstOrDefault(cell => cell.Membership.IsAvailable
                && (kind == LoadoutComparisonColumnKind.Current
                    ? cell.Membership.Value
                        != LoadoutComparisonMembership.Present
                    : cell.Membership.Value
                        == LoadoutComparisonMembership.Present));
        if (invalid is not null)
        {
            throw new ArgumentException(
                $"Membership {invalid.Membership.Value} is invalid for "
                + $"the {kind} column.",
                nameof(loadout));
        }

        if (kind == LoadoutComparisonColumnKind.Current
            && loadout.Categories
                .SelectMany(category => category.Skills)
                .Any(cell => !cell.Actions.IsEmpty))
        {
            throw new ArgumentException(
                "Current skill cells cannot contain policy actions.",
                nameof(loadout));
        }
    }
}
