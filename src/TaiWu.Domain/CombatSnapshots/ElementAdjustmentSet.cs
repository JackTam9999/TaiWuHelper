namespace TaiWu.Domain.CombatSnapshots;

public sealed record ElementAdjustmentSet
{
    public ElementAdjustmentSet(
        int metal,
        int wood,
        int water,
        int fire,
        int earth)
    {
        Metal = metal;
        Wood = wood;
        Water = water;
        Fire = fire;
        Earth = earth;
    }

    public int Metal { get; }

    public int Wood { get; }

    public int Water { get; }

    public int Fire { get; }

    public int Earth { get; }

    public int Get(CombatSkillElement element) => element switch
    {
        CombatSkillElement.Metal => Metal,
        CombatSkillElement.Wood => Wood,
        CombatSkillElement.Water => Water,
        CombatSkillElement.Fire => Fire,
        CombatSkillElement.Earth => Earth,
        CombatSkillElement.Mixed => 0,
        _ => throw new ArgumentOutOfRangeException(
            nameof(element),
            element,
            "Unknown combat-skill element.")
    };

    public static ElementAdjustmentSet None { get; } = new(0, 0, 0, 0, 0);
}
