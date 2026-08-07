using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public sealed record TargetThreatSource
{
    internal TargetThreatSource(
        int skillId,
        PracticeDirection direction,
        int rawEffectId,
        TargetThreatSourceScope scope,
        TargetThreatSourceKind kind,
        string evidenceReference)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown target-threat source kind.");
        }

        SkillId = skillId;
        Direction = direction;
        RawEffectId = rawEffectId;
        Scope = scope;
        Kind = kind;
        EvidenceReference = SnapshotFieldSource.NormalizeEvidenceReference(
            evidenceReference);
    }

    public int SkillId { get; }

    public PracticeDirection Direction { get; }

    public int RawEffectId { get; }

    public TargetThreatSourceScope Scope { get; }

    public TargetThreatSourceKind Kind { get; }

    public string EvidenceReference { get; }
}
