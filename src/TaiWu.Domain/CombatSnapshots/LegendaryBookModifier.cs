namespace TaiWu.Domain.CombatSnapshots;

public sealed record LegendaryBookModifier
{
    public LegendaryBookModifier(
        int skillId,
        SkillCategory category,
        int fixedCost,
        SnapshotDataSource source,
        string evidenceReference)
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

        if (fixedCost < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fixedCost),
                fixedCost,
                "A legendary-book fixed cost must be at least one.");
        }

        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unknown snapshot data source.");
        }

        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "A legendary-book modifier requires evidence.",
                nameof(evidenceReference));
        }

        SkillId = skillId;
        Category = category;
        FixedCost = fixedCost;
        Source = source;
        EvidenceReference = evidenceReference.Trim();
    }

    public int SkillId { get; }

    public SkillCategory Category { get; }

    public int FixedCost { get; }

    public SnapshotDataSource Source { get; }

    public string EvidenceReference { get; }

    public LegendaryBookModifier ForSkill(
        int skillId,
        SkillCategory category)
    {
        return new LegendaryBookModifier(
            skillId,
            category,
            FixedCost,
            Source,
            EvidenceReference);
    }
}
