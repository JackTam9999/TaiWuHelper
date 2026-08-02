using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatSource
{
    internal TargetThreatSource(
        int skillId,
        PracticeDirection direction,
        int rawEffectId,
        TargetThreatSourceScope scope)
    {
        SkillId = skillId;
        Direction = direction;
        RawEffectId = rawEffectId;
        Scope = scope;
    }

    public int SkillId { get; }

    public PracticeDirection Direction { get; }

    public int RawEffectId { get; }

    public TargetThreatSourceScope Scope { get; }
}
