using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record SkillDirectionExplanation
{
    internal SkillDirectionExplanation(
        SnapshotValue<PracticeDirection> currentDirection,
        PracticeDirection? requiredDirection,
        bool requiresManualDirectionChange,
        bool requiresBreakthrough,
        int? expectedEffectId,
        string evidenceReference)
    {
        CurrentDirection = currentDirection;
        RequiredDirection = requiredDirection;
        RequiresManualDirectionChange = requiresManualDirectionChange;
        RequiresBreakthrough = requiresBreakthrough;
        ExpectedEffectId = expectedEffectId;
        EvidenceReference = evidenceReference;
    }

    public SnapshotValue<PracticeDirection> CurrentDirection { get; }

    public PracticeDirection? RequiredDirection { get; }

    public bool RequiresManualDirectionChange { get; }

    public bool RequiresBreakthrough { get; }

    public bool RequiresManualChange =>
        RequiresManualDirectionChange || RequiresBreakthrough;

    public int? ExpectedEffectId { get; }

    public string EvidenceReference { get; }
}
