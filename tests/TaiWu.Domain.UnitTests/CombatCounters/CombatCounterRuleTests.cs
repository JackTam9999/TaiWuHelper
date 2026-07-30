using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatCounters;

public sealed class CombatCounterRuleTests
{
    [Fact]
    public void Golden_rules_cite_exact_effect_direction_and_source()
    {
        var ruleSet = VerifiedCombatCounterRuleSets.GoldenMagicSound;

        Assert.Equal(
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion,
            ruleSet.GameDataVersion);
        Assert.Equal(5, ruleSet.Rules.Length);
        Assert.All(
            ruleSet.Rules,
            rule =>
            {
                Assert.True(rule.Effect.HasTypedMechanics);
                Assert.Equal(
                    rule.RequiredDirection,
                    rule.Effect.Direction);
                Assert.StartsWith(
                    "local-config:",
                    rule.Effect.SourceReference);
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        rule.Effect.RawSourceText));
            });
    }

    [Fact]
    public void Golden_rules_distinguish_hard_counter_from_mitigation()
    {
        var rules = VerifiedCombatCounterRuleSets
            .GoldenMagicSound
            .Rules;

        var hardCounter = Assert.Single(
            rules,
            rule => rule.Strength
                == CombatCounterStrength.HardCounter);
        Assert.Equal(
            "REVERSE_JINNI_SUPPRESSION",
            hardCounter.Code);
        Assert.All(
            rules.Where(rule => rule != hardCounter),
            rule => Assert.Equal(
                CombatCounterStrength.Mitigation,
                rule.Strength));
    }

    [Fact]
    public void Golden_rules_represent_required_activation_timing()
    {
        var rules = VerifiedCombatCounterRuleSets
            .GoldenMagicSound
            .Rules;

        Assert.Contains(
            rules,
            rule => rule.ActivationTiming
                == CombatCounterActivationTiming.ActiveAttack);
        Assert.Contains(
            rules,
            rule => rule.ActivationTiming
                == CombatCounterActivationTiming.ActiveAgility);
        Assert.Contains(
            rules,
            rule => rule.ActivationTiming
                == CombatCounterActivationTiming.CombatStartPassive);
        Assert.Contains(
            rules,
            rule => rule.ActivationTiming
                == CombatCounterActivationTiming.EquippedPassive);
    }

    [Fact]
    public void Accessible_counter_requires_skill_direction_effect_and_state()
    {
        var rule = GoldenRule("REVERSE_WANHUA_RESONANCE");
        var skill = CreateSkill(rule);
        var context = CreateContext(
            equippedSkillIds: [skill.SkillId],
            activeAgilitySkillId: skill.SkillId);

        var result = Evaluate([skill], context, rule);

        var evaluation = Assert.Single(result.Evaluations);
        Assert.True(evaluation.IsAccessible);
        Assert.Empty(evaluation.Issues);
        Assert.True(evaluation.CandidateValidation.IsAccepted);
        Assert.True(evaluation.RequirementEvaluation.IsAccepted);
    }

    [Fact]
    public void Missing_player_skill_is_reported()
    {
        var rule = GoldenRule("REVERSE_JINNI_SUPPRESSION");

        var result = Evaluate(
            skills: [],
            CreateContext(),
            rule);

        var missing = Assert.Single(result.MissingAccess);
        var issue = Assert.Single(missing.Issues);
        Assert.Equal(
            CombatCounterAccessIssueCode.CandidateRejected,
            issue.Code);
        Assert.Contains("learned-skill snapshot", issue.Reason);
    }

    [Fact]
    public void Wrong_direction_and_changed_effect_are_both_reported()
    {
        var rule = GoldenRule("REVERSE_JINNI_SUPPRESSION");
        var skill = CreateSkill(
            rule,
            direction: PracticeDirection.Direct,
            reverseEffectId: rule.Effect.RawEffectId + 1);

        var result = Evaluate([skill], CreateContext(), rule);

        var missing = Assert.Single(result.MissingAccess);
        Assert.Contains(
            missing.Issues,
            issue => issue.Code
                == CombatCounterAccessIssueCode.CandidateRejected);
        Assert.Contains(
            missing.Issues,
            issue => issue.Code
                == CombatCounterAccessIssueCode.EffectIdMismatch);
    }

    [Fact]
    public void Missing_active_state_is_reported_as_requirement_failure()
    {
        var rule = GoldenRule("REVERSE_WANHUA_RESONANCE");
        var skill = CreateSkill(rule);

        var result = Evaluate(
            [skill],
            CreateContext(equippedSkillIds: [skill.SkillId]),
            rule);

        var missing = Assert.Single(result.MissingAccess);
        var issue = Assert.Single(
            missing.Issues,
            value => value.Code
                == CombatCounterAccessIssueCode.RequirementRejected);
        Assert.Contains("ActiveAgility", issue.Reason);
    }

    [Fact]
    public void Current_profile_directions_report_available_and_missing_rules()
    {
        var rules = VerifiedCombatCounterRuleSets.GoldenMagicSound;
        var skills = rules.Rules
            .Select(
                rule => rule.Code switch
                {
                    "REVERSE_JINNI_SUPPRESSION" => CreateSkill(
                        rule,
                        PracticeDirection.Neutral),
                    "DIRECT_MOYU_MARK_DURATION" => CreateSkill(
                        rule,
                        PracticeDirection.Reverse),
                    _ => CreateSkill(rule)
                })
            .ToArray();
        var context = CreateContext(
            equippedSkillIds: [686, 134],
            activeAgilitySkillId: 134);

        var result = CombatCounterAccessEvaluator.Evaluate(
            CreatePlayer(skills),
            context,
            rules);

        Assert.Equal(
            [
                "REVERSE_LAOJUN_MARK_CLEAR",
                "REVERSE_WANHUA_RESONANCE",
                "REVERSE_FULONG_POWER_REDUCTION"
            ],
            result.AccessibleCounters.Select(value => value.Rule.Code));
        Assert.Equal(
            [
                "REVERSE_JINNI_SUPPRESSION",
                "DIRECT_MOYU_MARK_DURATION"
            ],
            result.MissingAccess.Select(value => value.Rule.Code));
    }

    [Fact]
    public void Invalid_or_duplicate_rules_are_rejected()
    {
        var effect = GoldenRule("REVERSE_JINNI_SUPPRESSION").Effect;
        Assert.Throws<ArgumentException>(
            () => new CombatCounterRule(
                code: "invalid-code",
                threatCodes: ["THREAT"],
                CombatCounterStrength.HardCounter,
                CombatCounterActivationTiming.ActiveAttack,
                effect,
                requirements: [],
                rationale: "Rationale."));

        var rule = GoldenRule("REVERSE_JINNI_SUPPRESSION");
        Assert.Throws<ArgumentException>(
            () => new CombatCounterRuleSet(
                "1.0.0+test",
                [rule, rule]));
    }

    private static CombatCounterAccessReport Evaluate(
        CombatSkillSnapshot[] skills,
        CombatRequirementContext context,
        CombatCounterRule rule)
    {
        return CombatCounterAccessEvaluator.Evaluate(
            CreatePlayer(skills),
            context,
            new CombatCounterRuleSet("1.0.0+test", [rule]));
    }

    private static CombatCounterRule GoldenRule(string code)
    {
        return Assert.Single(
            VerifiedCombatCounterRuleSets.GoldenMagicSound.Rules,
            rule => rule.Code == code);
    }

    private static CombatSkillSnapshot CreateSkill(
        CombatCounterRule rule,
        PracticeDirection? direction = null,
        int? reverseEffectId = null)
    {
        return new CombatSkillSnapshot(
            rule.Effect.SkillId,
            SnapshotValue<string>.Available(rule.Effect.SkillName),
            Category(rule.Effect.SkillId),
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(
                direction ?? rule.RequiredDirection),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(
                rule.RequiredDirection == PracticeDirection.Direct
                    ? rule.Effect.RawEffectId
                    : rule.Effect.RawEffectId - 1),
            SnapshotValue<int>.Available(
                reverseEffectId
                    ?? (rule.RequiredDirection == PracticeDirection.Reverse
                        ? rule.Effect.RawEffectId
                        : rule.Effect.RawEffectId + 1)));
    }

    private static SkillCategory Category(int skillId) => skillId switch
    {
        134 => SkillCategory.Agility,
        267 => SkillCategory.Assistance,
        _ => SkillCategory.Attack
    };

    private static CombatRequirementContext CreateContext(
        int[]? equippedSkillIds = null,
        int? activeAgilitySkillId = null)
    {
        return new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(0),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds ?? [],
            activeDefenseSkillId: null,
            activeAgilitySkillId);
    }

    private static PlayerCombatSnapshot CreatePlayer(
        CombatSkillSnapshot[] skills)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            new CombatLoadoutSnapshot([], [], [], [], []),
            equipment: [],
            new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 0, 6),
                new SlotBudget(SkillCategory.Attack, 0, 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 2),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: []);
    }
}
