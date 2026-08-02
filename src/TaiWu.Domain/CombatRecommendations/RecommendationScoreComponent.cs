namespace TaiWu.Domain.CombatRecommendations;

public sealed record RecommendationScoreComponent
{
    internal RecommendationScoreComponent(
        RecommendationScoreComponentKind kind,
        int weight,
        decimal? score,
        string explanation,
        string evidenceReference)
    {
        Kind = kind;
        Weight = weight;
        Score = score;
        Explanation = explanation;
        EvidenceReference = evidenceReference;
    }

    public RecommendationScoreComponentKind Kind { get; }

    public int Weight { get; }

    public decimal? Score { get; }

    public bool IsAvailable => Score.HasValue;

    public decimal? WeightedPoints =>
        Score.HasValue ? Score.Value * Weight / 100m : null;

    public string Explanation { get; }

    public string EvidenceReference { get; }
}
