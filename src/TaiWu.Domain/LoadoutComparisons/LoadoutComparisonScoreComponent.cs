using TaiWu.Domain.CombatRecommendations;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonScoreComponent
{
    public LoadoutComparisonScoreComponent(
        RecommendationScoreComponentKind kind,
        int weight,
        LoadoutComparisonValue<decimal> score,
        string explanation,
        LoadoutComparisonReference evidenceReference)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown recommendation score component.");
        }

        if (weight < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                weight,
                "A score weight cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(explanation))
        {
            throw new ArgumentException(
                "A score component requires an explanation.",
                nameof(explanation));
        }

        Kind = kind;
        Weight = weight;
        Score = score ?? throw new ArgumentNullException(nameof(score));
        Explanation = explanation.Trim();
        EvidenceReference = evidenceReference
            ?? throw new ArgumentNullException(nameof(evidenceReference));
    }

    public RecommendationScoreComponentKind Kind { get; }

    public int Weight { get; }

    public LoadoutComparisonValue<decimal> Score { get; }

    public string Explanation { get; }

    public LoadoutComparisonReference EvidenceReference { get; }
}
