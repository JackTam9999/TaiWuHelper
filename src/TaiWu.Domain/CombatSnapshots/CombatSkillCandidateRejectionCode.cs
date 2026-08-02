namespace TaiWu.Domain.CombatSnapshots;

public enum CombatSkillCandidateRejectionCode
{
    SkillNotLearned,
    MasteryStatusUnavailable,
    MasteryRequired,
    DirectionStatusUnavailable,
    DirectionMismatch,
    NeutralDirectionCannotActivateEffect,
    DirectEffectUnavailable,
    ReverseEffectUnavailable
}
