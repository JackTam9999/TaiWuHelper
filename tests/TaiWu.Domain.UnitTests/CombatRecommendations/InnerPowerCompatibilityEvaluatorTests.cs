using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatRecommendations;

public sealed class InnerPowerCompatibilityEvaluatorTests
{
    [Fact]
    public void Active_skill_of_backlash_element_scores_zero()
    {
        var fireAttack = Skill(100, SkillCategory.Attack, CombatSkillElement.Fire);
        var player = Player([fireAttack]);
        var candidate = Generate(
            player,
            Option(
                fireAttack,
                CombatCounterActivationTiming.ActiveAttack));

        var evaluation = InnerPowerCompatibilityEvaluator.Evaluate(
            player,
            candidate);

        Assert.True(evaluation.Score.IsAvailable);
        Assert.Equal(0, evaluation.Score.Value);
        Assert.True(Assert.Single(evaluation.Evaluations).CausesBacklash);
    }

    [Fact]
    public void Equipped_fire_neigong_is_not_treated_as_a_cast()
    {
        var fireNeigong = Skill(
            100,
            SkillCategory.Neigong,
            CombatSkillElement.Fire);
        var player = Player(
            [fireNeigong],
            neigongIds: [fireNeigong.SkillId]);
        var candidate = Generate(
            player,
            CombatLoadoutOption.RetainCurrentSkill(
                fireNeigong.SkillId,
                "test:equipped-neigong"));

        var evaluation = InnerPowerCompatibilityEvaluator.Evaluate(
            player,
            candidate);

        Assert.True(evaluation.Score.IsAvailable);
        Assert.Equal(100, evaluation.Score.Value);
        Assert.Empty(evaluation.Evaluations);
    }

    [Fact]
    public void Negative_power_adjustment_lowers_active_skill_score()
    {
        var woodAttack = Skill(100, SkillCategory.Attack, CombatSkillElement.Wood);
        var player = Player([woodAttack]);
        var candidate = Generate(
            player,
            Option(
                woodAttack,
                CombatCounterActivationTiming.ActiveAttack));

        var evaluation = InnerPowerCompatibilityEvaluator.Evaluate(
            player,
            candidate);

        Assert.Equal(50, evaluation.Score.Value);
        Assert.Equal(
            -30,
            Assert.Single(evaluation.Evaluations).MaxPowerChange);
    }

    private static CombatLoadoutOption Option(
        CombatSkillSnapshot skill,
        CombatCounterActivationTiming timing) => new(
            new CombatSkillCandidate(skill.SkillId),
            requirements: [],
            threatCodes: ["THREAT"],
            isCurrentlyEquipped: false,
            "test:active-skill",
            CombatCounterStrength.Mitigation,
            timing);

    private static GeneratedCombatLoadout Generate(
        PlayerCombatSnapshot player,
        CombatLoadoutOption option)
    {
        var result = CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                player,
                [option],
                new CombatRequirementContext(
                    equippedWeaponTypeIds: [],
                    trickCounts: [],
                    SnapshotValue<int>.Available(0),
                    resources: [],
                    unlockedWeaponTypeIds: [],
                    equippedSkillIds: []),
                player.GenericSlotAllocation));
        return Assert.Single(result.Candidates);
    }

    private static CombatSkillSnapshot Skill(
        int id,
        SkillCategory category,
        CombatSkillElement element) => new(
            id,
            SnapshotValue<string>.Available($"Skill {id}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(1000 + id),
            SnapshotValue<int>.Available(2000 + id),
            element: SnapshotValue<CombatSkillElement>.Available(element));

    private static PlayerCombatSnapshot Player(
        CombatSkillSnapshot[] skills,
        int[]? neigongIds = null)
    {
        neigongIds ??= [];
        return new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            new CombatLoadoutSnapshot(neigongIds, [], [], [], []),
            equipment: [],
            new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, neigongIds.Length, 6),
                new SlotBudget(SkillCategory.Attack, 0, 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 2),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: [],
            SnapshotValue<InnerPowerStateSnapshot>.Available(
                new InnerPowerStateSnapshot(
                    0,
                    SnapshotValue<string>.Available("金剛·金剛伏魔"),
                    SnapshotValue<string>.Available("Test state"),
                    new ElementAdjustmentSet(30, -30, 0, 0, 0),
                    ElementAdjustmentSet.None,
                    CombatSkillElement.Fire)));
    }
}
