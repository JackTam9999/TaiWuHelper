namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatResourceAmount
{
    public CombatResourceAmount(
        CombatResourceKind resource,
        SnapshotValue<int> amount)
    {
        if (!Enum.IsDefined(resource))
        {
            throw new ArgumentOutOfRangeException(
                nameof(resource),
                resource,
                "Unknown combat resource.");
        }

        ArgumentNullException.ThrowIfNull(amount);
        if (amount.IsAvailable && amount.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "An available resource amount cannot be negative.");
        }

        Resource = resource;
        Amount = amount;
    }

    public CombatResourceKind Resource { get; }

    public SnapshotValue<int> Amount { get; }
}
