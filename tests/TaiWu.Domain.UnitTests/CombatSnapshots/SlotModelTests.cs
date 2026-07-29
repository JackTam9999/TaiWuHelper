using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class SlotModelTests
{
    [Fact]
    public void Slot_budget_set_requires_and_exposes_every_category()
    {
        var budgets = new SlotBudgetSet(
        [
            new SlotBudget(SkillCategory.Neigong, 6, 6),
            new SlotBudget(SkillCategory.Attack, 10, 10),
            new SlotBudget(SkillCategory.Agility, 8, 8),
            new SlotBudget(SkillCategory.Defense, 8, 8),
            new SlotBudget(SkillCategory.Assistance, 2, 2)
        ]);

        Assert.Equal(5, budgets.Values.Length);
        Assert.Equal(10, budgets[SkillCategory.Attack].Capacity);
        Assert.Equal(0, budgets[SkillCategory.Attack].Remaining);
    }

    [Fact]
    public void Slot_budget_rejects_used_slots_above_capacity()
    {
        Assert.Throws<ArgumentException>(
            () => new SlotBudget(SkillCategory.Assistance, 3, 2));
    }

    [Fact]
    public void Slot_budget_set_rejects_a_missing_category()
    {
        Assert.Throws<ArgumentException>(
            () => new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 0, 6),
                new SlotBudget(SkillCategory.Attack, 0, 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 2)
            ]));
    }

    [Fact]
    public void Generic_allocation_tracks_assigned_and_unallocated_slots()
    {
        var allocation = new GenericSlotAllocation(
            totalSlots: 6,
            attack: 4,
            agility: 2,
            defense: 0,
            assistance: 0);

        Assert.Equal(6, allocation.Assigned);
        Assert.Equal(0, allocation.Unallocated);
        Assert.Equal(4, allocation.Get(SkillCategory.Attack));
        Assert.Throws<ArgumentException>(
            () => allocation.Get(SkillCategory.Neigong));
    }

    [Fact]
    public void Generic_allocation_rejects_double_allocation()
    {
        Assert.Throws<ArgumentException>(
            () => new GenericSlotAllocation(
                totalSlots: 1,
                attack: 1,
                agility: 1,
                defense: 0,
                assistance: 0));
    }

    [Fact]
    public void Specific_slot_contribution_can_represent_a_category_penalty()
    {
        var contribution = new SkillSlotContribution(
            attack: 0,
            agility: 3,
            defense: -1,
            assistance: 0,
            generic: 1);

        Assert.Equal(-1, contribution.Defense);
        Assert.Equal(1, contribution.Generic);
    }
}
