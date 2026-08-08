using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonCategoryRow
{
    public LoadoutComparisonCategoryRow(
        SkillCategory category,
        LoadoutComparisonCapacitySummary capacity,
        IEnumerable<LoadoutComparisonSkillCell> skills)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown skill category.");
        }

        Category = category;
        Capacity = capacity
            ?? throw new ArgumentNullException(nameof(capacity));
        ArgumentNullException.ThrowIfNull(skills);
        Skills = [.. skills];
        if (Skills.Any(skill => skill is null))
        {
            throw new ArgumentException(
                "Comparison rows cannot contain null skill cells.",
                nameof(skills));
        }

        var mismatch = Skills.FirstOrDefault(
            skill => skill.Identity.Category != category);
        if (mismatch is not null)
        {
            throw new ArgumentException(
                $"Skill {mismatch.Identity.SkillId} belongs to "
                + $"{mismatch.Identity.Category}, not {category}.",
                nameof(skills));
        }

        var duplicate = Skills
            .GroupBy(skill => skill.Identity)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate comparison skill "
                + $"{duplicate.Key.SkillId} in {category}.",
                nameof(skills));
        }

        if (!Skills
                .Select(skill => skill.Identity.SkillId)
                .SequenceEqual(
                    Skills.Select(skill => skill.Identity.SkillId).Order()))
        {
            throw new ArgumentException(
                "Comparison skills must use stable skill-ID order.",
                nameof(skills));
        }
    }

    public SkillCategory Category { get; }

    public LoadoutComparisonCapacitySummary Capacity { get; }

    public ImmutableArray<LoadoutComparisonSkillCell> Skills { get; }
}
