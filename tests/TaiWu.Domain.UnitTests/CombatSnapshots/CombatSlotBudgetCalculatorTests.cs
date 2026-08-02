using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatSlotBudgetCalculatorTests
{
    [Fact]
    public void Calculates_all_category_capacities_usage_and_remaining_slots()
    {
        var neigong = CreateSkill(
            100,
            SkillCategory.Neigong,
            gridCost: 2,
            slotContribution: new SkillSlotContribution(
                attack: 1,
                agility: 0,
                defense: 2,
                assistance: 0,
                generic: 2));
        var attack = CreateSkill(
            200,
            SkillCategory.Attack,
            gridCost: 3,
            mastered: true);
        var agility = CreateSkill(
            300,
            SkillCategory.Agility,
            gridCost: 1);
        var assistance = CreateSkill(
            500,
            SkillCategory.Assistance,
            gridCost: 2);
        var player = CreatePlayer(
            [neigong, attack, agility, assistance],
            CreateLoadout(
                neigong: [neigong.SkillId],
                attack: [attack.SkillId],
                agility: [agility.SkillId],
                assistance: [assistance.SkillId]),
            new GenericSlotAllocation(
                totalSlots: 2,
                attack: 1,
                agility: 1,
                defense: 0,
                assistance: 0));

        var result = CombatSlotBudgetCalculator.Calculate(player);

        AssertBudget(result, SkillCategory.Neigong, 2, 6, 4);
        AssertBudget(result, SkillCategory.Attack, 2, 4, 2);
        AssertBudget(result, SkillCategory.Agility, 1, 3, 2);
        AssertBudget(result, SkillCategory.Defense, 0, 4, 4);
        AssertBudget(result, SkillCategory.Assistance, 2, 2, 0);
    }

    [Fact]
    public void Only_equipped_neigong_contributes_category_capacity()
    {
        var equipped = CreateSkill(
            100,
            SkillCategory.Neigong,
            gridCost: 1,
            slotContribution: new SkillSlotContribution(
                attack: 1,
                agility: 0,
                defense: 0,
                assistance: 0,
                generic: 0));
        var unequipped = CreateSkill(
            101,
            SkillCategory.Neigong,
            gridCost: 1,
            slotContribution: new SkillSlotContribution(
                attack: 5,
                agility: 5,
                defense: 5,
                assistance: 5,
                generic: 0));

        var result = CombatSlotBudgetCalculator.Calculate(
            CreatePlayer(
                [equipped, unequipped],
                CreateLoadout(neigong: [equipped.SkillId])));

        Assert.Equal(3, result[SkillCategory.Attack].Capacity);
        Assert.Equal(2, result[SkillCategory.Agility].Capacity);
        Assert.Equal(2, result[SkillCategory.Defense].Capacity);
        Assert.Equal(2, result[SkillCategory.Assistance].Capacity);
    }

    [Fact]
    public void Non_neigong_slot_contribution_does_not_change_capacity()
    {
        var attack = CreateSkill(
            200,
            SkillCategory.Attack,
            gridCost: 1,
            slotContribution: new SkillSlotContribution(
                attack: 9,
                agility: 9,
                defense: 9,
                assistance: 9,
                generic: 9));

        var result = CombatSlotBudgetCalculator.Calculate(
            CreatePlayer(
                [attack],
                CreateLoadout(attack: [attack.SkillId])));

        Assert.Equal(2, result[SkillCategory.Attack].Capacity);
        Assert.Equal(2, result[SkillCategory.Agility].Capacity);
        Assert.Equal(2, result[SkillCategory.Defense].Capacity);
        Assert.Equal(2, result[SkillCategory.Assistance].Capacity);
    }

    [Fact]
    public void Generic_allocation_affects_only_its_selected_categories()
    {
        var result = CombatSlotBudgetCalculator.Calculate(
            CreatePlayer(
                skills: [],
                CreateLoadout(),
                new GenericSlotAllocation(
                    totalSlots: 4,
                    attack: 3,
                    agility: 0,
                    defense: 1,
                    assistance: 0)));

        Assert.Equal(5, result[SkillCategory.Attack].Capacity);
        Assert.Equal(2, result[SkillCategory.Agility].Capacity);
        Assert.Equal(3, result[SkillCategory.Defense].Capacity);
        Assert.Equal(2, result[SkillCategory.Assistance].Capacity);
        Assert.Equal(6, result[SkillCategory.Neigong].Capacity);
    }

    [Fact]
    public void Evidence_backed_shouzhi_cost_is_included_in_used_slots()
    {
        var first = CreateSkill(
            200,
            SkillCategory.Attack,
            gridCost: 3);
        var second = CreateSkill(
            201,
            SkillCategory.Attack,
            gridCost: 1);
        var slot = new LegendaryBookCostSlot(
            "save:book:shouzhi",
            new LegendaryBookCostRule(
                LegendaryBookCostEffect.Shouzhi,
                SnapshotDataSource.CurrentScreenObservation,
                "docs/scenarios/M1-007-effective-skill-cost-evidence.md"));
        var assignment = new LegendaryBookCostAssignment(
            slot,
            first.SkillId,
            first.Category,
            LegendaryBookAssignmentOrigin.Save,
            "save:book:shouzhi:skill-200");
        var player = CreatePlayer(
            [first, second],
            CreateLoadout(attack: [first.SkillId, second.SkillId]),
            legendaryBookCostSlots: [slot],
            legendaryBookCostAssignments: [assignment]);

        var result = CombatSlotBudgetCalculator.Calculate(player);

        AssertBudget(result, SkillCategory.Attack, 2, 2, 0);
    }

    [Fact]
    public void Proposed_budget_preserves_observed_capacity_adjustment()
    {
        var neigong = CreateSkill(
            100,
            SkillCategory.Neigong,
            gridCost: 1,
            slotContribution: new SkillSlotContribution(
                attack: 1,
                agility: 0,
                defense: 0,
                assistance: 0,
                generic: 0));
        var attacks = Enumerable.Range(200, 4)
            .Select(skillId => CreateSkill(
                skillId,
                SkillCategory.Attack,
                gridCost: 1))
            .ToArray();
        var loadout = CreateLoadout(
            neigong: [neigong.SkillId],
            attack: [.. attacks.Select(skill => skill.SkillId)]);
        var player = CreatePlayer(
            [neigong, .. attacks],
            loadout,
            slotBudgets: CreateSlotBudgets(attackCapacity: 4));

        var result = CombatSlotBudgetCalculator.CalculateProposed(
            player,
            loadout,
            player.GenericSlotAllocation);

        AssertBudget(result, SkillCategory.Attack, 4, 4, 0);
    }

    [Fact]
    public void Unavailable_skill_cost_preserves_unavailable_usage()
    {
        var attack = CreateSkill(
            200,
            SkillCategory.Attack,
            SnapshotValue<int>.Unavailable("GridCost was unavailable."));

        var result = CombatSlotBudgetCalculator.Calculate(
            CreatePlayer(
                [attack],
                CreateLoadout(attack: [attack.SkillId])));

        var attackBudget = result[SkillCategory.Attack];
        Assert.False(attackBudget.Used.IsAvailable);
        Assert.False(attackBudget.Remaining.IsAvailable);
        Assert.Contains(
            "skill 200",
            attackBudget.Used.UnavailableReason);
        Assert.Equal(2, attackBudget.Capacity);
    }

    [Theory]
    [InlineData(SkillCategory.Neigong, 6)]
    [InlineData(SkillCategory.Attack, 2)]
    [InlineData(SkillCategory.Agility, 2)]
    [InlineData(SkillCategory.Defense, 2)]
    [InlineData(SkillCategory.Assistance, 2)]
    public void Exact_capacity_is_valid_and_one_over_is_rejected(
        SkillCategory category,
        int capacity)
    {
        var skills = Enumerable.Range(200, capacity + 1)
            .Select(skillId => CreateSkill(
                skillId,
                category,
                gridCost: 1))
            .ToArray();
        var exactIds = skills
            .Take(capacity)
            .Select(skill => skill.SkillId)
            .ToArray();
        var exactResult = CombatSlotBudgetCalculator.Calculate(
            CreatePlayer(
                skills,
                CreateLoadoutFor(category, exactIds)));

        AssertBudget(
            exactResult,
            category,
            used: capacity,
            capacity: capacity,
            remaining: 0);

        var overIds = skills
            .Select(skill => skill.SkillId)
            .ToArray();
        var exception = Assert.Throws<ArgumentException>(
            () => CombatSlotBudgetCalculator.Calculate(
                CreatePlayer(
                    skills,
                    CreateLoadoutFor(category, overIds))));

        Assert.Contains("exceed capacity", exception.Message);
    }

    [Fact]
    public void Over_budget_loadout_is_rejected()
    {
        var skills = Enumerable.Range(200, 3)
            .Select(skillId => CreateSkill(
                skillId,
                SkillCategory.Attack,
                gridCost: 1))
            .ToArray();
        var player = CreatePlayer(
            skills,
            CreateLoadout(
                attack: [.. skills.Select(skill => skill.SkillId)]));

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSlotBudgetCalculator.Calculate(player));

        Assert.Contains("exceed capacity", exception.Message);
    }

    [Fact]
    public void Equipped_unlearned_skill_is_rejected()
    {
        var player = CreatePlayer(
            skills: [],
            CreateLoadout(attack: [999]));

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSlotBudgetCalculator.Calculate(player));

        Assert.Contains("not learned", exception.Message);
    }

    [Fact]
    public void Equipped_skill_in_wrong_category_is_rejected()
    {
        var attack = CreateSkill(
            200,
            SkillCategory.Attack,
            gridCost: 1);
        var player = CreatePlayer(
            [attack],
            CreateLoadout(defense: [attack.SkillId]));

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSlotBudgetCalculator.Calculate(player));

        Assert.Contains("not Defense", exception.Message);
    }

    [Fact]
    public void Contribution_that_makes_capacity_negative_is_rejected()
    {
        var neigong = CreateSkill(
            100,
            SkillCategory.Neigong,
            gridCost: 1,
            slotContribution: new SkillSlotContribution(
                attack: -3,
                agility: 0,
                defense: 0,
                assistance: 0,
                generic: 0));
        var player = CreatePlayer(
            [neigong],
            CreateLoadout(neigong: [neigong.SkillId]));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CombatSlotBudgetCalculator.Calculate(player));
    }

    private static void AssertBudget(
        SlotBudgetSet budgets,
        SkillCategory category,
        int used,
        int capacity,
        int remaining)
    {
        var budget = budgets[category];
        Assert.Equal(used, budget.Used.Value);
        Assert.Equal(capacity, budget.Capacity);
        Assert.Equal(remaining, budget.Remaining.Value);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId,
        SkillCategory category,
        int gridCost,
        bool mastered = false,
        SkillSlotContribution? slotContribution = null)
    {
        return CreateSkill(
            skillId,
            category,
            SnapshotValue<int>.Available(gridCost),
            mastered,
            slotContribution);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId,
        SkillCategory category,
        SnapshotValue<int> gridCost,
        bool mastered = false,
        SkillSlotContribution? slotContribution = null)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            gridCost,
            SnapshotValue<bool>.Available(mastered),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral),
            slotContribution ?? SkillSlotContribution.None,
            SnapshotValue<int>.Available(0),
            SnapshotValue<int>.Available(0));
    }

    private static CombatLoadoutSnapshot CreateLoadout(
        int[]? neigong = null,
        int[]? attack = null,
        int[]? agility = null,
        int[]? defense = null,
        int[]? assistance = null)
    {
        return new CombatLoadoutSnapshot(
            neigong ?? [],
            attack ?? [],
            agility ?? [],
            defense ?? [],
            assistance ?? []);
    }

    private static CombatLoadoutSnapshot CreateLoadoutFor(
        SkillCategory category,
        int[] skillIds)
    {
        return category switch
        {
            SkillCategory.Neigong => CreateLoadout(neigong: skillIds),
            SkillCategory.Attack => CreateLoadout(attack: skillIds),
            SkillCategory.Agility => CreateLoadout(agility: skillIds),
            SkillCategory.Defense => CreateLoadout(defense: skillIds),
            SkillCategory.Assistance => CreateLoadout(assistance: skillIds),
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }

    private static PlayerCombatSnapshot CreatePlayer(
        CombatSkillSnapshot[] skills,
        CombatLoadoutSnapshot loadout,
        GenericSlotAllocation? genericSlotAllocation = null,
        LegendaryBookCostSlot[]? legendaryBookCostSlots = null,
        LegendaryBookCostAssignment[]?
            legendaryBookCostAssignments = null,
        SlotBudgetSet? slotBudgets = null)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            loadout,
            equipment: [],
            slotBudgets: slotBudgets ?? CreateSlotBudgets(),
            genericSlotAllocation
                ?? new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots ?? [],
            legendaryBookCostAssignments ?? []);
    }

    private static SlotBudgetSet CreateSlotBudgets(
        int attackCapacity = 2)
    {
        return new SlotBudgetSet(
        [
            new SlotBudget(SkillCategory.Neigong, 0, 6),
            new SlotBudget(SkillCategory.Attack, 0, attackCapacity),
            new SlotBudget(SkillCategory.Agility, 0, 2),
            new SlotBudget(SkillCategory.Defense, 0, 2),
            new SlotBudget(SkillCategory.Assistance, 0, 2)
        ]);
    }
}
