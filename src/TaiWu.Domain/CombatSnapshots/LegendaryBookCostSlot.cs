namespace TaiWu.Domain.CombatSnapshots;

public sealed record LegendaryBookCostSlot
{
    public LegendaryBookCostSlot(
        string slotReference,
        LegendaryBookCostRule rule)
    {
        if (string.IsNullOrWhiteSpace(slotReference))
        {
            throw new ArgumentException(
                "A legendary-book cost slot requires a stable reference.",
                nameof(slotReference));
        }

        SlotReference = slotReference.Trim();
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    public string SlotReference { get; }

    public LegendaryBookCostRule Rule { get; }
}
