namespace TaiWu.Domain.CombatSnapshots;

public sealed record FeasibleCombatLoadout
{
    internal FeasibleCombatLoadout(
        ProposedCombatLoadout proposal,
        SlotBudgetSet slotBudgets)
    {
        Proposal = proposal
            ?? throw new ArgumentNullException(nameof(proposal));
        SlotBudgets = slotBudgets
            ?? throw new ArgumentNullException(nameof(slotBudgets));
    }

    public ProposedCombatLoadout Proposal { get; }

    public SlotBudgetSet SlotBudgets { get; }
}
