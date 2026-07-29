using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record SlotBudgetSet
{
    private readonly ImmutableDictionary<SkillCategory, SlotBudget> _byCategory;

    public SlotBudgetSet(IEnumerable<SlotBudget> budgets)
    {
        ArgumentNullException.ThrowIfNull(budgets);

        var values = budgets.ToImmutableArray();
        if (values.Any(budget => budget is null))
        {
            throw new ArgumentException(
                "Slot budgets cannot contain null entries.",
                nameof(budgets));
        }

        var duplicate = values
            .GroupBy(budget => budget.Category)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate slot budget for {duplicate.Key}.",
                nameof(budgets));
        }

        var missing = Enum
            .GetValues<SkillCategory>()
            .Where(category => values.All(budget => budget.Category != category))
            .ToArray();
        if (missing.Length > 0)
        {
            throw new ArgumentException(
                "A slot budget is required for every category: "
                + string.Join(", ", missing),
                nameof(budgets));
        }

        Values = values
            .OrderBy(budget => budget.Category)
            .ToImmutableArray();
        _byCategory = Values.ToImmutableDictionary(
            budget => budget.Category);
    }

    public ImmutableArray<SlotBudget> Values { get; }

    public SlotBudget this[SkillCategory category] => _byCategory[category];
}
