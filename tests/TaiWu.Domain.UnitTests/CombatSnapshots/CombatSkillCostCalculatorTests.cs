using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatSkillCostCalculatorTests
{
    [Fact]
    public void Configured_grid_cost_is_the_base_cost()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 3, mastered: false),
            legendaryBookModifiers: []);

        Assert.Equal(3, result.BaseCost.Value);
        Assert.Equal(0, result.MasteryReduction);
        Assert.Equal(0, result.LegendaryBookReduction.Value);
        Assert.Equal(3, result.EffectiveCost.Value);
    }

    [Fact]
    public void Owned_but_unassigned_shouzhi_does_not_change_cost()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 3, mastered: false),
            legendaryBookModifiers: []);

        Assert.Empty(result.AppliedLegendaryBookModifiers);
        Assert.Equal(0, result.LegendaryBookReduction.Value);
        Assert.Equal(3, result.EffectiveCost.Value);
    }

    [Fact]
    public void Confirmed_mastery_reduces_cost_by_one()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 3, mastered: true),
            legendaryBookModifiers: []);

        Assert.Equal(1, result.MasteryReduction);
        Assert.Equal(2, result.EffectiveCost.Value);
    }

    [Fact]
    public void Mastery_never_reduces_cost_below_one()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 1, mastered: true),
            legendaryBookModifiers: []);

        Assert.Equal(
            CombatSkillCostCalculator.MinimumEffectiveCost,
            result.EffectiveCost.Value);
    }

    [Fact]
    public void Evidence_backed_shouzhi_sets_occupied_cost_to_one()
    {
        var modifier = CreateModifier(
            fixedCost: 1,
            evidence: "screen:fuxin-wuzijue:shouzhi");

        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 3, mastered: false),
            [modifier]);

        Assert.Equal(2, result.LegendaryBookReduction.Value);
        Assert.Equal(1, result.EffectiveCost.Value);
        Assert.Same(
            modifier,
            Assert.Single(result.AppliedLegendaryBookModifiers));
    }

    [Fact]
    public void Shouzhi_fixed_cost_applies_to_the_agility_category()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(
                gridCost: 3,
                mastered: false,
                category: SkillCategory.Agility),
            [
                CreateModifier(
                    fixedCost: 1,
                    evidence: "screen:baiyi-xinghua-ji:shouzhi",
                    category: SkillCategory.Agility)
            ]);

        Assert.Equal(SkillCategory.Agility, result.Category);
        Assert.Equal(2, result.LegendaryBookReduction.Value);
        Assert.Equal(1, result.EffectiveCost.Value);
    }

    [Fact]
    public void Proposed_shouzhi_assignment_is_a_new_helper_value()
    {
        var currentAssignment = CreateModifier(
            fixedCost: 1,
            evidence: "screen:legendary-book:shouzhi",
            category: SkillCategory.Attack);
        var proposedAssignment = currentAssignment.ForSkill(
            skillId: 605,
            category: SkillCategory.Assistance);

        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(
                gridCost: 3,
                mastered: false,
                category: SkillCategory.Assistance,
                skillId: 605),
            [proposedAssignment]);

        Assert.Equal(604, currentAssignment.SkillId);
        Assert.Equal(SkillCategory.Attack, currentAssignment.Category);
        Assert.NotSame(currentAssignment, proposedAssignment);
        Assert.Equal(605, proposedAssignment.SkillId);
        Assert.Equal(SkillCategory.Assistance, proposedAssignment.Category);
        Assert.Equal(1, result.EffectiveCost.Value);
    }

    [Fact]
    public void Mastery_is_applied_before_shouzhi_fixed_cost()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 3, mastered: true),
            [CreateModifier(1, "screen:fuxin-wuzijue:shouzhi")]);

        Assert.Equal(1, result.MasteryReduction);
        Assert.Equal(1, result.LegendaryBookReduction.Value);
        Assert.Equal(1, result.EffectiveCost.Value);
    }

    [Fact]
    public void Shouzhi_never_increases_a_cost_already_at_one()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 1, mastered: true),
            [CreateModifier(1, "screen:fuxin-wuzijue:shouzhi")]);

        Assert.Equal(0, result.LegendaryBookReduction.Value);
        Assert.Equal(1, result.EffectiveCost.Value);
    }

    [Fact]
    public void More_than_one_fixed_cost_modifier_is_rejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CombatSkillCostCalculator.Calculate(
                CreateSkill(gridCost: 3, mastered: false),
                [
                    CreateModifier(1, "screen:book-slot:1"),
                    CreateModifier(1, "screen:book-slot:2")
                ]));

        Assert.Contains("more than one", exception.Message);
    }

    [Fact]
    public void Modifier_for_another_skill_is_not_applied()
    {
        var result = CombatSkillCostCalculator.Calculate(
            CreateSkill(gridCost: 3, mastered: false),
            [
                new LegendaryBookModifier(
                    skillId: 999,
                    SkillCategory.Attack,
                    fixedCost: 1,
                    SnapshotDataSource.Save,
                    "save:other-skill")
            ]);

        Assert.Empty(result.AppliedLegendaryBookModifiers);
        Assert.Equal(0, result.LegendaryBookReduction.Value);
        Assert.Equal(3, result.EffectiveCost.Value);
    }

    [Fact]
    public void Unavailable_grid_cost_keeps_effective_cost_unavailable()
    {
        var skill = CreateSkill(
            SnapshotValue<int>.Unavailable(
                "Game configuration did not contain GridCost."),
            SnapshotValue<bool>.Available(false));

        var result = CombatSkillCostCalculator.Calculate(skill, []);

        Assert.False(result.EffectiveCost.IsAvailable);
        Assert.Contains(
            "GridCost",
            result.EffectiveCost.UnavailableReason);
        Assert.True(result.LegendaryBookReduction.IsAvailable);
        Assert.Equal(0, result.LegendaryBookReduction.Value);
    }

    [Fact]
    public void Shouzhi_reduction_is_unavailable_when_grid_cost_is_unavailable()
    {
        var skill = CreateSkill(
            SnapshotValue<int>.Unavailable(
                "Game configuration did not contain GridCost."),
            SnapshotValue<bool>.Available(false));

        var result = CombatSkillCostCalculator.Calculate(
            skill,
            [CreateModifier(1, "screen:fuxin-wuzijue:shouzhi")]);

        Assert.False(result.LegendaryBookReduction.IsAvailable);
        Assert.Contains(
            "GridCost",
            result.LegendaryBookReduction.UnavailableReason);
    }

    [Fact]
    public void Unconfirmed_mastery_keeps_effective_cost_unavailable()
    {
        var skill = CreateSkill(
            SnapshotValue<int>.Available(3),
            SnapshotValue<bool>.Unavailable(
                "Mastery could not be read."));

        var result = CombatSkillCostCalculator.Calculate(skill, []);

        Assert.Equal(0, result.MasteryReduction);
        Assert.False(result.EffectiveCost.IsAvailable);
        Assert.Contains(
            "mastery",
            result.EffectiveCost.UnavailableReason);
    }

    [Fact]
    public void Shouzhi_reduction_is_unavailable_when_mastery_is_unconfirmed()
    {
        var skill = CreateSkill(
            SnapshotValue<int>.Available(3),
            SnapshotValue<bool>.Unavailable(
                "Mastery could not be read."));

        var result = CombatSkillCostCalculator.Calculate(
            skill,
            [CreateModifier(1, "screen:fuxin-wuzijue:shouzhi")]);

        Assert.False(result.LegendaryBookReduction.IsAvailable);
        Assert.Contains(
            "mastery",
            result.LegendaryBookReduction.UnavailableReason);
    }

    [Fact]
    public void Category_mismatched_modifier_is_rejected()
    {
        var modifier = new LegendaryBookModifier(
            skillId: 604,
            SkillCategory.Defense,
            fixedCost: 1,
            SnapshotDataSource.Save,
            "save:wrong-category");

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSkillCostCalculator.Calculate(
                CreateSkill(gridCost: 3, mastered: false),
                [modifier]));

        Assert.Contains("not Attack", exception.Message);
    }

    [Fact]
    public void Duplicate_evidence_cannot_apply_fixed_cost_twice()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CombatSkillCostCalculator.Calculate(
                CreateSkill(gridCost: 3, mastered: false),
                [
                    CreateModifier(1, "save:same-evidence"),
                    CreateModifier(1, "save:same-evidence")
                ]));

        Assert.Contains("more than one", exception.Message);
    }

    [Fact]
    public void Player_calculation_uses_only_snapshot_modifiers()
    {
        var skill = CreateSkill(gridCost: 3, mastered: true);
        var player = CreatePlayer(
            skill,
            [CreateModifier(1, "save:confirmed-slot")]);

        var result = CombatSkillCostCalculator.Calculate(
            player,
            skill.SkillId);

        Assert.Equal(1, result.EffectiveCost.Value);
        Assert.Throws<KeyNotFoundException>(
            () => CombatSkillCostCalculator.Calculate(player, 999));
    }

    [Fact]
    public void Player_rejects_modifier_for_unlearned_skill()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);
        var modifier = new LegendaryBookModifier(
            skillId: 999,
            SkillCategory.Attack,
            fixedCost: 1,
            SnapshotDataSource.Save,
            "save:unknown-skill");

        var exception = Assert.Throws<ArgumentException>(
            () => CreatePlayer(skill, [modifier]));

        Assert.Contains("unlearned", exception.Message);
    }

    private static CombatSkillSnapshot CreateSkill(
        int gridCost,
        bool mastered,
        SkillCategory category = SkillCategory.Attack,
        int skillId = 604)
    {
        return CreateSkill(
            SnapshotValue<int>.Available(gridCost),
            SnapshotValue<bool>.Available(mastered),
            category,
            skillId);
    }

    private static CombatSkillSnapshot CreateSkill(
        SnapshotValue<int> gridCost,
        SnapshotValue<bool> mastered,
        SkillCategory category = SkillCategory.Attack,
        int skillId = 604)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available("金猊鎮魔刀"),
            category,
            gridCost,
            mastered,
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(338),
            SnapshotValue<int>.Available(339));
    }

    private static LegendaryBookModifier CreateModifier(
        int fixedCost,
        string evidence,
        SkillCategory category = SkillCategory.Attack)
    {
        return new LegendaryBookModifier(
            skillId: 604,
            category,
            fixedCost,
            SnapshotDataSource.CurrentScreenObservation,
            evidence);
    }

    private static PlayerCombatSnapshot CreatePlayer(
        CombatSkillSnapshot skill,
        IEnumerable<LegendaryBookModifier> modifiers)
    {
        return new PlayerCombatSnapshot(
            characterId: 21396,
            SnapshotValue<string>.Available("太吾"),
            learnedSkills: [skill],
            equippedSkills: new CombatLoadoutSnapshot(
                neigongSkillIds: [],
                attackSkillIds: [skill.SkillId],
                agilitySkillIds: [],
                defenseSkillIds: [],
                assistanceSkillIds: []),
            equipment: [],
            slotBudgets: new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 0, 6),
                new SlotBudget(SkillCategory.Attack, 3, 10),
                new SlotBudget(SkillCategory.Agility, 0, 8),
                new SlotBudget(SkillCategory.Defense, 0, 8),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            genericSlotAllocation:
                new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookModifiers: modifiers);
    }
}
