namespace TaiWu.Domain.CombatSnapshots;

public sealed record SlotBudget
{
    public SlotBudget(SkillCategory category, int used, int capacity)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown skill category.");
        }

        if (used < 0)
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

        if (used > capacity)
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

    public int Used { get; }

    public int Capacity { get; }

    public int Remaining => Capacity - Used;
}
