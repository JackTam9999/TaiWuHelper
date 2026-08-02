namespace TaiWu.Domain.CombatThreats;

public enum TargetThreatActivationTiming
{
    Unknown,
    Always,
    CombatStart,
    OnSkillUse,
    OnHit,
    OnMarkApplied,
    Threshold
}
