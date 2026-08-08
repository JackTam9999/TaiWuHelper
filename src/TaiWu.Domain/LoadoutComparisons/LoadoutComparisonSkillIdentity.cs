using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonSkillIdentity
{
    public LoadoutComparisonSkillIdentity(
        SkillCategory category,
        int skillId)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown skill category.");
        }

        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A comparison skill ID cannot be negative.");
        }

        Category = category;
        SkillId = skillId;
    }

    public SkillCategory Category { get; }

    public int SkillId { get; }
}
