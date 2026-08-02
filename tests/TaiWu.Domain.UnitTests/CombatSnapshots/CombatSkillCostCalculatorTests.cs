using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatSkillCostCalculatorTests
{
    [Fact]
    public void Configured_grid_cost_is_the_effective_cost_without_assignment()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);

        var result = CombatSkillCostCalculator.Calculate(
            CreatePlayer([skill]),
            skill.SkillId);

        Assert.Equal(3, result.BaseCost.Value);
        Assert.Equal(0, result.MasteryReduction.Value);
        Assert.Equal(0, result.LegendaryBookReduction.Value);
        Assert.Equal(3, result.EffectiveCost.Value);
    }

    [Fact]
    public void Mastery_reduces_cost_by_one_but_never_below_one()
    {
        var skill = CreateSkill(gridCost: 1, mastered: true);

        var result = CombatSkillCostCalculator.Calculate(
            CreatePlayer([skill]),
            skill.SkillId);

        Assert.Equal(0, result.MasteryReduction.Value);
        Assert.Equal(
            CombatSkillCostCalculator.MinimumEffectiveCost,
            result.EffectiveCost.Value);
    }

    [Fact]
    public void Current_shouzhi_assignment_sets_effective_cost_to_one()
    {
        var skill = CreateSkill(gridCost: 3, mastered: true);
        var slot = CreateSlot();
        var assignment = CreateAssignment(slot, skill);

        var result = CombatSkillCostCalculator.Calculate(
            CreatePlayer([skill], [slot], [assignment]),
            skill.SkillId);

        Assert.Equal(1, result.MasteryReduction.Value);
        Assert.Equal(1, result.LegendaryBookReduction.Value);
        Assert.Equal(1, result.EffectiveCost.Value);
        Assert.Same(
            assignment,
            Assert.Single(result.AppliedLegendaryBookCostAssignments));
    }

    [Fact]
    public void Owned_but_unassigned_shouzhi_slot_does_not_change_cost()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);

        var result = CombatSkillCostCalculator.Calculate(
            CreatePlayer([skill], [CreateSlot()]),
            skill.SkillId);

        Assert.Empty(result.AppliedLegendaryBookCostAssignments);
        Assert.Equal(0, result.LegendaryBookReduction.Value);
        Assert.Equal(3, result.EffectiveCost.Value);
    }

    [Fact]
    public void Exact_shouzhi_cost_remains_known_when_grid_cost_is_unknown()
    {
        var skill = CreateSkill(
            SnapshotValue<int>.Unavailable("GridCost was not available."),
            SnapshotValue<bool>.Available(false));
        var slot = CreateSlot();

        var result = CombatSkillCostCalculator.Calculate(
            CreatePlayer(
                [skill],
                [slot],
                [CreateAssignment(slot, skill)]),
            skill.SkillId);

        Assert.False(result.MasteryReduction.IsAvailable);
        Assert.False(result.LegendaryBookReduction.IsAvailable);
        Assert.True(result.EffectiveCost.IsAvailable);
        Assert.Equal(1, result.EffectiveCost.Value);
    }

    [Fact]
    public void Exact_shouzhi_cost_remains_known_when_mastery_is_unknown()
    {
        var skill = CreateSkill(
            SnapshotValue<int>.Available(3),
            SnapshotValue<bool>.Unavailable("Mastery was not available."));
        var slot = CreateSlot();

        var result = CombatSkillCostCalculator.Calculate(
            CreatePlayer(
                [skill],
                [slot],
                [CreateAssignment(slot, skill)]),
            skill.SkillId);

        Assert.False(result.MasteryReduction.IsAvailable);
        Assert.False(result.LegendaryBookReduction.IsAvailable);
        Assert.Equal(1, result.EffectiveCost.Value);
    }

    [Fact]
    public void Proposal_uses_owned_slot_without_changing_current_assignment()
    {
        var currentSkill = CreateSkill(
            gridCost: 3,
            mastered: false,
            skillId: 604);
        var proposedSkill = CreateSkill(
            gridCost: 3,
            mastered: false,
            skillId: 605);
        var slot = CreateSlot();
        var current = CreateAssignment(slot, currentSkill);
        var proposed = current.ProposeForSkill(
            proposedSkill.SkillId,
            proposedSkill.Category,
            "proposal:move-shouzhi-to-605");

        var result = CombatSkillCostCalculator.CalculateProposed(
            CreatePlayer(
                [currentSkill, proposedSkill],
                [slot],
                [current]),
            proposed);

        Assert.Equal(604, current.SkillId);
        Assert.Equal(LegendaryBookAssignmentOrigin.Save, current.Origin);
        Assert.Equal(605, proposed.SkillId);
        Assert.Equal(
            LegendaryBookAssignmentOrigin.Proposed,
            proposed.Origin);
        Assert.Equal(
            "proposal:move-shouzhi-to-605",
            proposed.AssignmentEvidenceReference);
        Assert.Equal(1, result.EffectiveCost.Value);
    }

    [Fact]
    public void Proposal_cannot_use_an_unowned_slot()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);
        var proposed = CreateAssignment(
            CreateSlot("book:unowned"),
            skill,
            LegendaryBookAssignmentOrigin.Proposed,
            "proposal:unowned");

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSkillCostCalculator.CalculateProposed(
                CreatePlayer([skill]),
                proposed));

        Assert.Contains("unavailable slot", exception.Message);
    }

    [Fact]
    public void Proposal_requires_proposed_origin()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);
        var slot = CreateSlot();
        var current = CreateAssignment(slot, skill);

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSkillCostCalculator.CalculateProposed(
                CreatePlayer([skill], [slot], [current]),
                current));

        Assert.Contains("proposed assignment", exception.Message);
    }

    [Fact]
    public void Player_rejects_assignment_for_unlearned_skill()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);
        var slot = CreateSlot();
        var assignment = new LegendaryBookCostAssignment(
            slot,
            skillId: 999,
            SkillCategory.Attack,
            LegendaryBookAssignmentOrigin.Save,
            "save:unknown-skill");

        var exception = Assert.Throws<ArgumentException>(
            () => CreatePlayer([skill], [slot], [assignment]));

        Assert.Contains("unlearned", exception.Message);
    }

    [Fact]
    public void Player_rejects_assignment_with_mismatched_category()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);
        var slot = CreateSlot();
        var assignment = new LegendaryBookCostAssignment(
            slot,
            skill.SkillId,
            SkillCategory.Defense,
            LegendaryBookAssignmentOrigin.Save,
            "save:wrong-category");

        var exception = Assert.Throws<ArgumentException>(
            () => CreatePlayer([skill], [slot], [assignment]));

        Assert.Contains("not Attack", exception.Message);
    }

    [Fact]
    public void Player_rejects_duplicate_slot_assignments()
    {
        var first = CreateSkill(
            gridCost: 3,
            mastered: false,
            skillId: 604);
        var second = CreateSkill(
            gridCost: 3,
            mastered: false,
            skillId: 605);
        var slot = CreateSlot();

        var exception = Assert.Throws<ArgumentException>(
            () => CreatePlayer(
                [first, second],
                [slot],
                [
                    CreateAssignment(slot, first),
                    CreateAssignment(slot, second)
                ]));

        Assert.Contains("more than one current assignment", exception.Message);
    }

    [Fact]
    public void Player_rejects_duplicate_fixed_cost_assignments_for_skill()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);
        var firstSlot = CreateSlot("book:slot:one");
        var secondSlot = CreateSlot("book:slot:two");

        var exception = Assert.Throws<ArgumentException>(
            () => CreatePlayer(
                [skill],
                [firstSlot, secondSlot],
                [
                    CreateAssignment(firstSlot, skill),
                    CreateAssignment(secondSlot, skill)
                ]));

        Assert.Contains("more than one", exception.Message);
    }

    [Fact]
    public void Current_snapshot_rejects_proposed_assignment()
    {
        var skill = CreateSkill(gridCost: 3, mastered: false);
        var slot = CreateSlot();
        var proposal = CreateAssignment(
            slot,
            skill,
            LegendaryBookAssignmentOrigin.Proposed,
            "proposal:test");

        var exception = Assert.Throws<ArgumentException>(
            () => CreatePlayer([skill], [slot], [proposal]));

        Assert.Contains("cannot contain proposed", exception.Message);
    }

    [Fact]
    public void Shouzhi_rule_has_only_its_evidence_backed_fixed_cost()
    {
        var rule = new LegendaryBookCostRule(
            LegendaryBookCostEffect.Shouzhi,
            SnapshotDataSource.CurrentScreenObservation,
            "docs/evidence/shouzhi.md");

        Assert.Equal(1, rule.FixedCost);
    }

    [Fact]
    public void Player_calculation_rejects_unknown_skill()
    {
        var player = CreatePlayer(
            [CreateSkill(gridCost: 3, mastered: false)]);

        Assert.Throws<KeyNotFoundException>(
            () => CombatSkillCostCalculator.Calculate(player, 999));
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
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            gridCost,
            mastered,
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(338),
            SnapshotValue<int>.Available(339));
    }

    private static LegendaryBookCostSlot CreateSlot(
        string slotReference = "book:slot:shouzhi")
    {
        return new LegendaryBookCostSlot(
            slotReference,
            new LegendaryBookCostRule(
                LegendaryBookCostEffect.Shouzhi,
                SnapshotDataSource.CurrentScreenObservation,
                "docs/evidence/shouzhi.md"));
    }

    private static LegendaryBookCostAssignment CreateAssignment(
        LegendaryBookCostSlot slot,
        CombatSkillSnapshot skill,
        LegendaryBookAssignmentOrigin origin =
            LegendaryBookAssignmentOrigin.Save,
        string evidence = "save:legendary-book:shouzhi")
    {
        return new LegendaryBookCostAssignment(
            slot,
            skill.SkillId,
            skill.Category,
            origin,
            evidence);
    }

    private static PlayerCombatSnapshot CreatePlayer(
        IEnumerable<CombatSkillSnapshot> skills,
        IEnumerable<LegendaryBookCostSlot>? slots = null,
        IEnumerable<LegendaryBookCostAssignment>? assignments = null)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            learnedSkills: skills,
            equippedSkills: new CombatLoadoutSnapshot(
                neigongSkillIds: [],
                attackSkillIds: [],
                agilitySkillIds: [],
                defenseSkillIds: [],
                assistanceSkillIds: []),
            equipment: [],
            slotBudgets: new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 0, 6),
                new SlotBudget(SkillCategory.Attack, 0, 10),
                new SlotBudget(SkillCategory.Agility, 0, 8),
                new SlotBudget(SkillCategory.Defense, 0, 8),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            genericSlotAllocation:
                new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: slots ?? [],
            legendaryBookCostAssignments: assignments ?? []);
    }
}
