using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record ManualLoadoutChange
{
    internal ManualLoadoutChange(
        ManualLoadoutChangeKind kind,
        SkillCategory category,
        int skillId,
        PracticeDirection? requiredDirection,
        RecommendationReason reason)
    {
        Kind = kind;
        Category = category;
        SkillId = skillId;
        RequiredDirection = requiredDirection;
        Reason = reason;
    }

    public ManualLoadoutChangeKind Kind { get; }

    public SkillCategory Category { get; }

    public int SkillId { get; }

    public PracticeDirection? RequiredDirection { get; }

    public RecommendationReason Reason { get; }
}
