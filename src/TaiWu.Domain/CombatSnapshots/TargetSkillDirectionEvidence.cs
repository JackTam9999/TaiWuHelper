namespace TaiWu.Domain.CombatSnapshots;

public sealed record TargetSkillDirectionEvidence
{
    public TargetSkillDirectionEvidence(
        int skillId,
        SnapshotEvidenceField<PracticeDirection> evidence)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Direction-evidence skill ID cannot be negative.");
        }

        SkillId = skillId;
        Evidence = evidence
            ?? throw new ArgumentNullException(nameof(evidence));
    }

    public int SkillId { get; }

    public SnapshotEvidenceField<PracticeDirection> Evidence { get; }
}
