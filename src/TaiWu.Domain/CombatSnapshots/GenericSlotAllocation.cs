namespace TaiWu.Domain.CombatSnapshots;

public sealed record GenericSlotAllocation
{
    public GenericSlotAllocation(
        int totalSlots,
        int attack,
        int agility,
        int defense,
        int assistance)
    {
        if (totalSlots < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalSlots),
                totalSlots,
                "Generic slot count cannot be negative.");
        }

        var allocations = new[]
        {
            attack,
            agility,
            defense,
            assistance
        };
        if (allocations.Any(value => value < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(attack),
                "Generic slot allocations cannot be negative.");
        }

        if (allocations.Sum() > totalSlots)
        {
            throw new ArgumentException(
                "Generic slots cannot be allocated more than once.",
                nameof(totalSlots));
        }

        TotalSlots = totalSlots;
        Attack = attack;
        Agility = agility;
        Defense = defense;
        Assistance = assistance;
    }

    public int TotalSlots { get; }

    public int Attack { get; }

    public int Agility { get; }

    public int Defense { get; }

    public int Assistance { get; }

    public int Assigned => Attack + Agility + Defense + Assistance;

    public int Unallocated => TotalSlots - Assigned;

    public int Get(SkillCategory category) => category switch
    {
        SkillCategory.Attack => Attack,
        SkillCategory.Agility => Agility,
        SkillCategory.Defense => Defense,
        SkillCategory.Assistance => Assistance,
        SkillCategory.Neigong => throw new ArgumentException(
            "Generic slots cannot be allocated to Neigong.",
            nameof(category)),
        _ => throw new ArgumentOutOfRangeException(
            nameof(category),
            category,
            "Unknown skill category.")
    };
}
