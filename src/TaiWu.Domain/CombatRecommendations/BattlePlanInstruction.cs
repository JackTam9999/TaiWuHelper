namespace TaiWu.Domain.CombatRecommendations;

public sealed record BattlePlanInstruction
{
    internal BattlePlanInstruction(
        int sequence,
        BattlePlanInstructionKind kind,
        int skillId,
        int? alternativeSkillId,
        string condition,
        RecommendationReason reason)
    {
        Sequence = sequence;
        Kind = kind;
        SkillId = skillId;
        AlternativeSkillId = alternativeSkillId;
        Condition = condition;
        Reason = reason;
    }

    public int Sequence { get; }

    public BattlePlanInstructionKind Kind { get; }

    public int SkillId { get; }

    public int? AlternativeSkillId { get; }

    public string Condition { get; }

    public RecommendationReason Reason { get; }
}
