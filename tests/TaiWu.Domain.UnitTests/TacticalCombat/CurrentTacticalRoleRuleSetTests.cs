using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class CurrentTacticalRoleRuleSetTests
{
    private static readonly TacticalCombatRuleSet Rules =
        VerifiedTacticalCombatRuleSets.CurrentLaterMagicSound;

    [Fact]
    public void Current_rule_set_pins_exact_version_roles_and_omits_reset()
    {
        Assert.Equal(
            VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion,
            Assert.Single(Rules.SupportedGameDataVersions));
        Assert.Equal(19, Rules.Roles.Length);
        Assert.Equal(21, Rules.Transitions.Length);
        Assert.Equal(
            "64051C1234CECDFDCE070134FDA0380826154D16C1F171B52B6F7FE1C64ECD5D",
            Rules.Fingerprint);
        Assert.DoesNotContain(
            "DEFEAT_MARK_RESET_LOOP",
            Rules.SupportedTargetGoalCodes);
        Assert.DoesNotContain(
            Rules.Transitions,
            item => item.Purpose == TacticalRulePurpose.DefeatMarkReset);
        Assert.Equal(
            new[]
            {
                (2, PracticeDirection.Direct, 1739),
                (134, PracticeDirection.Reverse, 973),
                (147, PracticeDirection.Direct, 260),
                (148, PracticeDirection.Direct, 261),
                (150, PracticeDirection.Reverse, 989),
                (151, PracticeDirection.Reverse, 990),
                (252, PracticeDirection.Direct, 150),
                (265, PracticeDirection.Reverse, 889),
                (267, PracticeDirection.Direct, 165),
                (280, PracticeDirection.Reverse, 904),
                (289, PracticeDirection.Direct, 187),
                (295, PracticeDirection.Reverse, 919),
                (303, PracticeDirection.Reverse, 927),
                (599, PracticeDirection.Reverse, 1059),
                (602, PracticeDirection.Reverse, 1062),
                (604, PracticeDirection.Reverse, 1064),
                (616, PracticeDirection.Reverse, 1251),
                (624, PracticeDirection.Reverse, 1234),
                (686, PracticeDirection.Reverse, 1422)
            },
            Rules.Roles
                .Select(item => (item.SkillId, item.Direction,
                    item.RawEffectId))
                .OrderBy(item => item.SkillId));
    }

    [Fact]
    public void Every_role_has_exact_effect_counter_requirements_and_use_kind()
    {
        Assert.All(
            Rules.Roles,
            role =>
            {
                Assert.NotNull(role.SharedCounter);
                Assert.NotEmpty(role.RequiredMechanics);
                Assert.Equal(role.Effect.Mechanics, role.RequiredMechanics);
                Assert.NotEmpty(role.SharedCounter!.Requirements);
                Assert.NotEmpty(role.UseKinds);
                Assert.Equal(
                    Rules.SupportedGameDataVersions,
                    role.SupportedGameDataVersions);
                var effect = VerifiedCombatEffectCatalogs.CurrentAntiMagic
                    .Resolve(
                        Rules.SupportedGameDataVersions[0],
                        role.SkillId,
                        role.Direction,
                        role.RawEffectId);
                Assert.True(effect.IsRecognized);
                Assert.Same(effect.CatalogEntry, role.Effect);
            });

        Assert.All(
            Rules.Roles.Where(item => item.SkillId is
                134 or 147 or 148 or 150 or 151),
            item => Assert.Contains(
                TacticalRoleUseKind.ActiveAgility,
                item.UseKinds));
        Assert.All(
            Rules.Roles.Where(item => item.SkillId is 2 or 289 or 295 or 303),
            item => Assert.Contains(
                TacticalRoleUseKind.ActiveDefense,
                item.UseKinds));
        Assert.All(
            Rules.Roles.Where(item => item.SkillId is 252 or 265 or 267 or 280),
            item => Assert.Equal(
                [TacticalRoleUseKind.EquippedPassive],
                item.UseKinds));
    }

    [Fact]
    public void Recovery_roles_are_reverse_casts_with_hard_execution_gates()
    {
        var recovery = Rules.Roles.Where(item =>
                item.Identity.Kind == TacticalRoleKind.Recovery)
            .OrderBy(item => item.SkillId)
            .ToArray();

        Assert.Equal(new[] { 599, 602, 616, 686 },
            recovery.Select(item => item.SkillId));
        Assert.All(recovery, role =>
        {
            Assert.Equal(PracticeDirection.Reverse, role.Direction);
            Assert.Contains(
                CombatEffectMechanic
                    .RemoveOwnDirectPracticeLockLayerOnReverseCast,
                role.RequiredMechanics);
            Assert.Contains(
                role.SharedCounter!.Requirements,
                item => item is WeaponRequirement);
            Assert.Contains(
                role.SharedCounter.Requirements,
                item => item is ResourceRequirement resource
                    && resource.Resource == CombatResourceKind.Stance);
            Assert.Contains(
                role.SharedCounter.Requirements,
                item => item is ResourceRequirement resource
                    && resource.Resource == CombatResourceKind.Breath);
            Assert.Contains(
                role.SharedCounter.Requirements,
                item => item is ManualConfirmationRequirement);
        });
    }

    [Fact]
    public void Distance_true_qi_and_manual_requirements_remain_typed()
    {
        Assert.Contains(
            Role(147).SharedCounter!.Requirements,
            item => item is RangeRequirement { MinimumInclusive: 5 });
        Assert.Contains(
            Role(280).SharedCounter!.Requirements,
            item => item is RangeRequirement { MaximumInclusive: 4 });
        Assert.Contains(
            Role(295).SharedCounter!.Requirements,
            item => item is ResourceRequirement
            {
                Resource: CombatResourceKind.DefenseTrueQi,
                MinimumAmount: 3
            });

        var manual = Assert.IsType<ManualConfirmationRequirement>(
            Role(265).SharedCounter!.Requirements.Single(item =>
                item is ManualConfirmationRequirement));
        var context = new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Unavailable("not observed"),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: []);
        var evaluation = Assert.Single(
            CombatRequirementEvaluator.Evaluate([manual], context)
                .Evaluations);

        Assert.Equal(CombatRequirementStatus.Unknown, evaluation.Status);
        Assert.Contains(manual.Code, evaluation.Reason);
    }

    [Fact]
    public void Complete_evidence_applies_all_current_roles()
    {
        var observations = Rules.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(Rules.Roles.SelectMany(item => item.EvidenceRequirements))
            .DistinctBy(item => new
            {
                item.Identity.Code,
                item.Scope,
                item.Source
            })
            .Select(Observation)
            .ToArray();
        var resolution = Rules.Resolve(
            Rules.SupportedGameDataVersions[0],
            Rules.SupportedTargetGoalCodes,
            observations);

        Assert.True(resolution.IsResolved);
        Assert.All(
            resolution.Transitions,
            item => Assert.Equal(
                TacticalRuleApplicability.Applicable,
                item.Applicability));
        Assert.All(
            resolution.Roles,
            item => Assert.Equal(
                TacticalRuleApplicability.Applicable,
                item.Applicability));
    }

    [Fact]
    public void Unsupported_version_exposes_no_current_roles()
    {
        var resolution = Rules.Resolve(
            "different-version",
            Rules.SupportedTargetGoalCodes,
            []);

        Assert.Equal(
            TacticalRuleSetResolutionStatus.UnsupportedGameDataVersion,
            resolution.Status);
        Assert.Empty(resolution.Roles);
        Assert.Empty(resolution.Transitions);
    }

    private static TacticalSkillRoleRule Role(int skillId) =>
        Rules.Roles.Single(item => item.SkillId == skillId);

    private static TacticalRuleEvidenceObservation Observation(
        TacticalRuleEvidenceRequirement requirement) => new(
        requirement.Identity,
        requirement.Scope,
        requirement.Source,
        TacticalRuleEvidenceDisposition.Confirmed,
        new TacticalEvidenceReference(
            requirement.Source,
            $"E8-F03-{requirement.Identity.Code}",
            Rules.SupportedGameDataVersions[0],
            VerifiedTacticalCombatRuleSets.RuleVersion,
            "CURRENT_ROLE_FIXTURE"));
}
