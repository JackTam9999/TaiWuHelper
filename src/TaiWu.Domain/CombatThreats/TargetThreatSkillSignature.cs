using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatSkillSignature
{
    public TargetThreatSkillSignature(
        int skillId,
        PracticeDirection direction,
        int rawEffectId)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (direction is not (
            PracticeDirection.Direct or PracticeDirection.Reverse))
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                direction,
                "A threat signature must be Direct or Reverse.");
        }

        if (rawEffectId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rawEffectId),
                rawEffectId,
                "Raw effect ID cannot be negative.");
        }

        SkillId = skillId;
        Direction = direction;
        RawEffectId = rawEffectId;
    }

    public int SkillId { get; }

    public PracticeDirection Direction { get; }

    public int RawEffectId { get; }
}
