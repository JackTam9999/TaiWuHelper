using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public sealed class TacticalExecutionProposal
{
    public TacticalExecutionProposal(
        CombatRequirementContext requirementContext,
        SlotBudgetSet? slotBudgets = null,
        GenericSlotAllocation? universalSlotAllocation = null,
        IEnumerable<LegendaryBookCostAssignment>?
            legendaryCostAssignments = null)
    {
        RequirementContext = requirementContext
            ?? throw new ArgumentNullException(nameof(requirementContext));
        SlotBudgets = slotBudgets;
        UniversalSlotAllocation = universalSlotAllocation;
        HasLegendaryCostAssignments = legendaryCostAssignments is not null;
        var assignments = (legendaryCostAssignments ?? []).ToImmutableArray();
        if (assignments.Any(item => item is null)
            || assignments.Any(item =>
                item.Origin != LegendaryBookAssignmentOrigin.Proposed)
            || assignments.Select(item => item.Slot.SlotReference)
                .Distinct(StringComparer.Ordinal)
                .Count() != assignments.Length
            || assignments.Select(item => item.SkillId).Distinct().Count()
                != assignments.Length)
        {
            throw new ArgumentException(
                "Proposed legendary-cost assignments require unique proposed slots and skills.",
                nameof(legendaryCostAssignments));
        }

        LegendaryCostAssignments =
        [
            .. assignments.OrderBy(
                item => item.Slot.SlotReference,
                StringComparer.Ordinal)
        ];
    }

    public CombatRequirementContext RequirementContext { get; }

    public SlotBudgetSet? SlotBudgets { get; }

    public GenericSlotAllocation? UniversalSlotAllocation { get; }

    public bool HasLegendaryCostAssignments { get; }

    public ImmutableArray<LegendaryBookCostAssignment>
        LegendaryCostAssignments
    { get; }
}
