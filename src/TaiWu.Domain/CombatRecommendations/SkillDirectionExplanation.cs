using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record SkillDirectionExplanation
{
    internal SkillDirectionExplanation(
        SnapshotValue<PracticeDirection> currentDirection,
        PracticeDirection? requiredDirection,
        bool requiresManualChange,
        int? expectedEffectId,
        string evidenceReference)
    {
        CurrentDirection = currentDirection;
        RequiredDirection = requiredDirection;
        RequiresManualChange = requiresManualChange;
        ExpectedEffectId = expectedEffectId;
        EvidenceReference = evidenceReference;
    }

    public SnapshotValue<PracticeDirection> CurrentDirection { get; }

    public PracticeDirection? RequiredDirection { get; }

    public bool RequiresManualChange { get; }

    public int? ExpectedEffectId { get; }

    public string EvidenceReference { get; }
}
