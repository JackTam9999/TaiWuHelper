namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSkillCandidate
{
    public CombatSkillCandidate(
        int skillId,
        bool requiresMastery = false,
        PracticeDirection? requiredDirection = null,
        bool allowDirectionChange = false)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (requiredDirection.HasValue
            && !Enum.IsDefined(requiredDirection.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredDirection),
                requiredDirection,
                "Unknown practice direction.");
        }

        SkillId = skillId;
        RequiresMastery = requiresMastery;
        RequiredDirection = requiredDirection;
        AllowDirectionChange = allowDirectionChange;
    }

    public int SkillId { get; }

    public bool RequiresMastery { get; }

    public PracticeDirection? RequiredDirection { get; }

    public bool AllowDirectionChange { get; }
}
