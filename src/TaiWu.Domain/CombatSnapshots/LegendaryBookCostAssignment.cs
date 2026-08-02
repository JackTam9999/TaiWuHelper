namespace TaiWu.Domain.CombatSnapshots;

public sealed record LegendaryBookCostAssignment
{
    public LegendaryBookCostAssignment(
        LegendaryBookCostSlot slot,
        int skillId,
        SkillCategory category,
        LegendaryBookAssignmentOrigin origin,
        string assignmentEvidenceReference)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown skill category.");
        }

        if (!Enum.IsDefined(origin))
        {
            throw new ArgumentOutOfRangeException(
                nameof(origin),
                origin,
                "Unknown legendary-book assignment origin.");
        }

        if (string.IsNullOrWhiteSpace(assignmentEvidenceReference))
        {
            throw new ArgumentException(
                "A legendary-book assignment requires evidence.",
                nameof(assignmentEvidenceReference));
        }

        Slot = slot ?? throw new ArgumentNullException(nameof(slot));
        SkillId = skillId;
        Category = category;
        Origin = origin;
        AssignmentEvidenceReference = assignmentEvidenceReference.Trim();
    }

    public LegendaryBookCostSlot Slot { get; }

    public int SkillId { get; }

    public SkillCategory Category { get; }

    public LegendaryBookAssignmentOrigin Origin { get; }

    public string AssignmentEvidenceReference { get; }

    public LegendaryBookCostAssignment ProposeForSkill(
        int skillId,
        SkillCategory category,
        string proposalReference)
    {
        return new LegendaryBookCostAssignment(
            Slot,
            skillId,
            category,
            LegendaryBookAssignmentOrigin.Proposed,
            proposalReference);
    }
}
