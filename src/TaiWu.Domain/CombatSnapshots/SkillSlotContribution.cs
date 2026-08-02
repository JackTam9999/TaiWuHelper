namespace TaiWu.Domain.CombatSnapshots;

public sealed record SkillSlotContribution
{
    public SkillSlotContribution(
        int attack,
        int agility,
        int defense,
        int assistance,
        int generic)
    {
        if (generic < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(generic),
                generic,
                "A skill cannot provide a negative generic-slot count.");
        }

        Attack = attack;
        Agility = agility;
        Defense = defense;
        Assistance = assistance;
        Generic = generic;
    }

    public int Attack { get; }

    public int Agility { get; }

    public int Defense { get; }

    public int Assistance { get; }

    public int Generic { get; }

    public int GetSpecific(SkillCategory category) => category switch
    {
        SkillCategory.Neigong => 0,
        SkillCategory.Attack => Attack,
        SkillCategory.Agility => Agility,
        SkillCategory.Defense => Defense,
        SkillCategory.Assistance => Assistance,
        _ => throw new ArgumentOutOfRangeException(
            nameof(category),
            category,
            "Unknown skill category.")
    };

    public static SkillSlotContribution None { get; } =
        new(0, 0, 0, 0, 0);
}
