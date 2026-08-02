namespace TaiWu.Domain.CombatSnapshots;

public sealed record SlotBudget
{
    public SlotBudget(SkillCategory category, int used, int capacity)
        : this(
            category,
            SnapshotValue<int>.Available(used),
            capacity)
    {
    }

    public SlotBudget(
        SkillCategory category,
        SnapshotValue<int> used,
        int capacity)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown skill category.");
        }

        ArgumentNullException.ThrowIfNull(used);

        if (used.IsAvailable && used.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(used),
                used,
                "Used slots cannot be negative.");
        }

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                "Slot capacity cannot be negative.");
        }

        if (used.IsAvailable && used.Value > capacity)
        {
            throw new ArgumentException(
                "Used slots cannot exceed capacity.",
                nameof(used));
        }

        Category = category;
        Used = used;
        Capacity = capacity;
    }

    public SkillCategory Category { get; }

    public SnapshotValue<int> Used { get; }

    public int Capacity { get; }

    public SnapshotValue<int> Remaining => Used.IsAvailable
        ? SnapshotValue<int>.Available(Capacity - Used.Value)
        : SnapshotValue<int>.Unavailable(
            Used.UnavailableReason
            ?? "Slot usage is unavailable.");
}
