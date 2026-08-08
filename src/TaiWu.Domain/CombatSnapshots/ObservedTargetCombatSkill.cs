namespace TaiWu.Domain.CombatSnapshots;

public sealed record ObservedTargetCombatSkill
{
    public ObservedTargetCombatSkill(
        int skillId,
        SkillCategory category,
        PracticeDirection? direction = null,
        int? slotIndex = null,
        int? visiblePowerPercent = null)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Observed skill ID cannot be negative.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown observed skill category.");
        }

        if (direction is not null
            && direction is not PracticeDirection.Direct
                and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "Only visibly verified direct or reverse directions are "
                + "supported.");
        }

        if (slotIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotIndex),
                slotIndex,
                "An observed slot index cannot be negative.");
        }

        if (visiblePowerPercent < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(visiblePowerPercent),
                visiblePowerPercent,
                "A visible target-skill power percentage cannot be negative.");
        }

        SkillId = skillId;
        Category = category;
        Direction = direction;
        SlotIndex = slotIndex;
        VisiblePowerPercent = visiblePowerPercent;
    }

    public int SkillId { get; }

    public SkillCategory Category { get; }

    public PracticeDirection? Direction { get; }

    public int? SlotIndex { get; }

    public int? VisiblePowerPercent { get; }
}
