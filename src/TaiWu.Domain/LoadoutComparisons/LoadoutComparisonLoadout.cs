using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.LoadoutComparisons;

public sealed record LoadoutComparisonLoadout
{
    public LoadoutComparisonLoadout(
        IEnumerable<LoadoutComparisonCategoryRow> categories,
        LoadoutComparisonValue<GenericSlotAllocation>
            genericSlotAllocation)
    {
        ArgumentNullException.ThrowIfNull(categories);
        Categories = [.. categories];
        GenericSlotAllocation = genericSlotAllocation
            ?? throw new ArgumentNullException(
                nameof(genericSlotAllocation));

        if (Categories.Any(category => category is null))
        {
            throw new ArgumentException(
                "A comparison loadout cannot contain null category rows.",
                nameof(categories));
        }

        var expected = Enum.GetValues<SkillCategory>();
        if (!Categories
                .Select(category => category.Category)
                .SequenceEqual(expected))
        {
            throw new ArgumentException(
                "A comparison loadout requires every category exactly once "
                + "in canonical order.",
                nameof(categories));
        }
    }

    public ImmutableArray<LoadoutComparisonCategoryRow> Categories { get; }

    public LoadoutComparisonValue<GenericSlotAllocation>
        GenericSlotAllocation
    { get; }
}
