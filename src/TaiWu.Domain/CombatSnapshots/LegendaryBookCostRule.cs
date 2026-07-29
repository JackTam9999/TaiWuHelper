namespace TaiWu.Domain.CombatSnapshots;

public sealed record LegendaryBookCostRule
{
    public LegendaryBookCostRule(
        LegendaryBookCostEffect effect,
        SnapshotDataSource source,
        string evidenceReference)
    {
        if (!Enum.IsDefined(effect))
        {
            throw new ArgumentOutOfRangeException(
                nameof(effect),
                effect,
                "Unknown legendary-book cost effect.");
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
                "A legendary-book cost rule requires evidence.",
                nameof(evidenceReference));
        }

        Effect = effect;
        Source = source;
        EvidenceReference = evidenceReference.Trim();
    }

    public LegendaryBookCostEffect Effect { get; }

    public int FixedCost => Effect switch
    {
        LegendaryBookCostEffect.Shouzhi => 1,
        _ => throw new InvalidOperationException(
            $"No fixed cost is defined for {Effect}.")
    };

    public SnapshotDataSource Source { get; }

    public string EvidenceReference { get; }
}
