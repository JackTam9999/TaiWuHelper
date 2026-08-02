using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record CombatRoleChoice
{
    internal CombatRoleChoice(
        int skillId,
        PracticeDirection? requiredDirection,
        decimal candidateScore,
        RecommendationReason reason)
    {
        SkillId = skillId;
        RequiredDirection = requiredDirection;
        CandidateScore = candidateScore;
        Reason = reason;
    }

    public int SkillId { get; }

    public PracticeDirection? RequiredDirection { get; }

    public decimal CandidateScore { get; }

    public RecommendationReason Reason { get; }
}
