using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatRecommendations;

public sealed class NeigongLoadoutOptimizerTests
{
    [Fact]
    public void Retains_current_neigong_when_it_supports_outer_skills()
    {
        var current = Neigong(100, new SkillSlotContribution(1, 0, 0, 0, 0));
        var attack = Outer(200, SkillCategory.Attack);
        var player = Player(
            [current, attack],
            neigongIds: [current.SkillId]);

        var result = NeigongLoadoutOptimizer.Optimize(
            player,
            [Option(attack)]);

        Assert.NotNull(result);
        Assert.Equal([current.SkillId], result.NeigongSkillIds);
        Assert.Equal(1, result.UsedNeigongCapacity);
    }

    [Fact]
    public void Replaces_only_what_is_needed_to_supply_specific_capacity()
    {
        var current = Enumerable.Range(100, 6)
            .Select(id => Neigong(id, SkillSlotContribution.None))
            .ToArray();
        var provider = Neigong(
            150,
            new SkillSlotContribution(2, 0, 0, 0, 0));
        var attacks = Enumerable.Range(200, 3)
            .Select(id => Outer(id, SkillCategory.Attack))
            .ToArray();
        var player = Player(
            [.. current, provider, .. attacks],
            [.. current.Select(skill => skill.SkillId)]);

        var result = NeigongLoadoutOptimizer.Optimize(
            player,
            [.. attacks.Select(Option)]);

        Assert.NotNull(result);
        Assert.Contains(provider.SkillId, result.NeigongSkillIds);
        Assert.Equal(
            5,
            result.NeigongSkillIds.Count(id => current.Any(
                skill => skill.SkillId == id)));
        Assert.Equal(6, result.UsedNeigongCapacity);
    }

    [Fact]
    public void Reallocates_generic_slots_to_the_outer_skill_deficits()
    {
        var provider = Neigong(
            100,
            new SkillSlotContribution(0, 0, 0, 0, 2));
        var attacks = Enumerable.Range(200, 3)
            .Select(id => Outer(id, SkillCategory.Attack));
        var defenses = Enumerable.Range(300, 3)
            .Select(id => Outer(id, SkillCategory.Defense));
        var outer = attacks.Concat(defenses).ToArray();
        var player = Player([provider, .. outer], neigongIds: []);

        var result = NeigongLoadoutOptimizer.Optimize(
            player,
            [.. outer.Select(Option)]);

        Assert.NotNull(result);
        Assert.Equal([provider.SkillId], result.NeigongSkillIds);
        Assert.Equal(2, result.GenericSlotAllocation.TotalSlots);
        Assert.Equal(1, result.GenericSlotAllocation.Attack);
        Assert.Equal(1, result.GenericSlotAllocation.Defense);
        Assert.Equal(0, result.GenericSlotAllocation.Agility);
        Assert.Equal(0, result.GenericSlotAllocation.Assistance);
    }

    private static CombatLoadoutOption Option(CombatSkillSnapshot skill) =>
        new(
            new CombatSkillCandidate(skill.SkillId),
            requirements: [],
            threatCodes: [],
            isCurrentlyEquipped: false,
            $"test:skill:{skill.SkillId}");

    private static CombatSkillSnapshot Neigong(
        int id,
        SkillSlotContribution contribution) =>
        Skill(id, SkillCategory.Neigong, contribution);

    private static CombatSkillSnapshot Outer(
        int id,
        SkillCategory category) =>
        Skill(id, category, SkillSlotContribution.None);

    private static CombatSkillSnapshot Skill(
        int id,
        SkillCategory category,
        SkillSlotContribution contribution) => new(
            id,
            SnapshotValue<string>.Available($"Skill {id}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            contribution,
            SnapshotValue<int>.Available(1000 + id),
            SnapshotValue<int>.Available(2000 + id));

    private static PlayerCombatSnapshot Player(
        CombatSkillSnapshot[] skills,
        int[] neigongIds)
    {
        var loadout = new CombatLoadoutSnapshot(
            neigongIds, [], [], [], []);
        var neigong = skills
            .Where(skill => neigongIds.Contains(skill.SkillId))
            .ToArray();
        return new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            loadout,
            equipment: [],
            new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, neigongIds.Length, 6),
                .. new[]
                {
                    SkillCategory.Attack,
                    SkillCategory.Agility,
                    SkillCategory.Defense,
                    SkillCategory.Assistance
                }.Select(category => new SlotBudget(
                    category,
                    used: 0,
                    CombatSlotBudgetCalculator.CalculateConfiguredCapacity(
                        category,
                        neigong,
                        new GenericSlotAllocation(0, 0, 0, 0, 0))))
            ]),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: []);
    }
}
