using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatSkillCandidateValidatorTests
{
    [Fact]
    public void Known_skill_without_special_requirements_is_accepted()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(skill.SkillId));

        Assert.True(result.IsAccepted);
        Assert.Empty(result.Rejections);
        Assert.Same(skill, result.Skill);
    }

    [Fact]
    public void Available_direct_effect_in_direct_practice_is_accepted()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct));

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void Available_reverse_effect_in_reverse_practice_is_accepted()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Reverse));

        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void Neutral_cannot_be_requested_as_a_direction_specific_effect()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Neutral));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode
                .NeutralDirectionCannotActivateEffect);
    }

    [Fact]
    public void Neutral_practice_cannot_activate_direct_effect()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode
                .NeutralDirectionCannotActivateEffect);
    }

    [Fact]
    public void Opposite_practice_direction_is_rejected()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.DirectionMismatch);
        Assert.Contains("Reverse", result.Rejections[0].Reason);
        Assert.Contains("Direct", result.Rejections[0].Reason);
    }

    [Theory]
    [InlineData(PracticeDirection.Neutral)]
    [InlineData(PracticeDirection.Reverse)]
    public void Explicit_manual_direction_change_can_satisfy_candidate(
        PracticeDirection currentDirection)
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                currentDirection));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct,
                allowDirectionChange: true));

        Assert.True(result.IsAccepted);
        Assert.Equal(
            PracticeDirection.Direct,
            result.RequiredDirectionChange);
    }

    [Fact]
    public void Direction_change_still_requires_verified_target_effect()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            directEffectId: SnapshotValue<int>.Unavailable(
                "Direct effect was absent."));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct,
                allowDirectionChange: true));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.DirectEffectUnavailable);
        Assert.Null(result.RequiredDirectionChange);
    }

    [Fact]
    public void Unavailable_practice_direction_is_rejected_with_reason()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Unavailable(
                "Practice state was not mapped."));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.DirectionStatusUnavailable);
        Assert.Contains(
            "Practice state was not mapped.",
            result.Rejections[0].Reason);
    }

    [Fact]
    public void Unavailable_direct_effect_is_rejected()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            directEffectId: SnapshotValue<int>.Unavailable(
                "Direct effect was absent."));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.DirectEffectUnavailable);
    }

    [Fact]
    public void Unavailable_reverse_effect_is_rejected()
    {
        var skill = CreateSkill(
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            reverseEffectId: SnapshotValue<int>.Unavailable(
                "Reverse effect was absent."));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Reverse));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.ReverseEffectUnavailable);
    }

    [Fact]
    public void Unknown_skill_is_rejected_instead_of_throwing()
    {
        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([]),
            new CombatSkillCandidate(999));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.SkillNotLearned);
        Assert.Null(result.Skill);
        Assert.Contains("999", result.Rejections[0].Reason);
    }

    [Fact]
    public void Required_mastery_rejects_unmastered_skill()
    {
        var skill = CreateSkill(
            mastered: SnapshotValue<bool>.Available(false));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiresMastery: true));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.MasteryRequired);
    }

    [Fact]
    public void Required_mastery_rejects_unavailable_mastery_status()
    {
        var skill = CreateSkill(
            mastered: SnapshotValue<bool>.Unavailable(
                "Mastery was not mapped."));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiresMastery: true));

        AssertRejected(
            result,
            CombatSkillCandidateRejectionCode.MasteryStatusUnavailable);
        Assert.Contains(
            "Mastery was not mapped.",
            result.Rejections[0].Reason);
    }

    [Fact]
    public void Every_independent_rejection_reason_is_returned()
    {
        var skill = CreateSkill(
            mastered: SnapshotValue<bool>.Available(false),
            direction: SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            directEffectId: SnapshotValue<int>.Unavailable(
                "Direct effect was absent."));

        var result = CombatSkillCandidateValidator.Validate(
            CreatePlayer([skill]),
            new CombatSkillCandidate(
                skill.SkillId,
                requiresMastery: true,
                requiredDirection: PracticeDirection.Direct));

        Assert.False(result.IsAccepted);
        Assert.Equal(3, result.Rejections.Length);
        Assert.Contains(
            result.Rejections,
            rejection => rejection.Code
                == CombatSkillCandidateRejectionCode.MasteryRequired);
        Assert.Contains(
            result.Rejections,
            rejection => rejection.Code
                == CombatSkillCandidateRejectionCode.DirectionMismatch);
        Assert.Contains(
            result.Rejections,
            rejection => rejection.Code
                == CombatSkillCandidateRejectionCode.DirectEffectUnavailable);
        Assert.All(
            result.Rejections,
            rejection => Assert.False(
                string.IsNullOrWhiteSpace(rejection.Reason)));
    }

    [Fact]
    public void Candidate_rejects_invalid_identity_and_direction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillCandidate(-1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillCandidate(
                1,
                requiredDirection: (PracticeDirection)99));
    }

    private static void AssertRejected(
        CombatSkillCandidateValidationResult result,
        CombatSkillCandidateRejectionCode expectedCode)
    {
        Assert.False(result.IsAccepted);
        Assert.Contains(
            result.Rejections,
            rejection => rejection.Code == expectedCode);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId = 604,
        SnapshotValue<bool>? mastered = null,
        SnapshotValue<PracticeDirection>? direction = null,
        SnapshotValue<int>? directEffectId = null,
        SnapshotValue<int>? reverseEffectId = null)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(1),
            mastered ?? SnapshotValue<bool>.Available(true),
            direction
                ?? SnapshotValue<PracticeDirection>.Available(
                    PracticeDirection.Direct),
            SkillSlotContribution.None,
            directEffectId ?? SnapshotValue<int>.Available(1000),
            reverseEffectId ?? SnapshotValue<int>.Available(1001));
    }

    private static PlayerCombatSnapshot CreatePlayer(
        CombatSkillSnapshot[] learnedSkills)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            learnedSkills,
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
                new SlotBudget(SkillCategory.Attack, 0, 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 2),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            genericSlotAllocation:
                new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: []);
    }
}
