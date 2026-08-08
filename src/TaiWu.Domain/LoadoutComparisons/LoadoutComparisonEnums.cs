namespace TaiWu.Domain.LoadoutComparisons;

public enum LoadoutComparisonColumnKind
{
    Current,
    Safe,
    Balanced,
    Aggressive
}

public enum LoadoutComparisonColumnStatus
{
    Available,
    Infeasible,
    Unavailable
}

public enum LoadoutComparisonMembership
{
    Present,
    Retained,
    Added,
    Removed
}

public enum LoadoutComparisonSkillActionKind
{
    DirectionChangeRequired,
    BreakthroughRequired
}

public enum LoadoutComparisonBaselineField
{
    EquippedSkills,
    GenericSlotAllocation,
    SlotBudgets,
    LegendaryBookCostAssignments
}
