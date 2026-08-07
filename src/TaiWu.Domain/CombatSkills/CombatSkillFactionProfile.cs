using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatSkills;

public enum CombatSkillFactionAlignment
{
    Just = 0,
    Kind = 1,
    Even = 2,
    Rebel = 3,
    Egoistic = 4
}

public sealed record CombatSkillFactionProfile
{
    public CombatSkillFactionProfile(
        CombatSkillFactionId faction,
        CombatSkillElement? primaryElement,
        CombatSkillFactionAlignment? primaryAlignment)
    {
        if (primaryElement is { } element && !Enum.IsDefined(element))
        {
            throw new ArgumentOutOfRangeException(
                nameof(primaryElement),
                primaryElement,
                "Unknown faction primary element.");
        }

        if (primaryAlignment is { } alignment && !Enum.IsDefined(alignment))
        {
            throw new ArgumentOutOfRangeException(
                nameof(primaryAlignment),
                primaryAlignment,
                "Unknown faction primary alignment.");
        }

        Faction = faction;
        PrimaryElement = primaryElement;
        PrimaryAlignment = primaryAlignment;
    }

    public CombatSkillFactionId Faction { get; }

    public CombatSkillElement? PrimaryElement { get; }

    public CombatSkillFactionAlignment? PrimaryAlignment { get; }
}
