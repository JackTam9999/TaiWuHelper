using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatRequirementEvaluatorTests
{
    private const int BladeWeaponType = 10;
    private const int BladeTrickType = 20;
    private const int LaoJunSkill = 100;
    private const int WanhuaSkill = 200;
    private const int DefenseSkill = 300;
    private const int OtherDefenseSkill = 301;
    private const string Evidence = "docs/evidence/verified-rule.md";

    [Fact]
    public void All_supported_requirement_types_can_be_satisfied()
    {
        CombatRequirement[] requirements =
        [
            new WeaponRequirement(
                BladeWeaponType,
                CombatRequirementCriticality.Hard,
                Evidence),
            new TrickRequirement(
                BladeTrickType,
                minimumCount: 3,
                CombatRequirementCriticality.Hard,
                Evidence),
            new RangeRequirement(
                minimumInclusive: 3,
                maximumInclusive: 7,
                CombatRequirementCriticality.Hard,
                Evidence),
            new ResourceRequirement(
                CombatResourceKind.Neili,
                minimumAmount: 10,
                CombatRequirementCriticality.Hard,
                Evidence),
            new ResourceRequirement(
                CombatResourceKind.Stance,
                minimumAmount: 5,
                CombatRequirementCriticality.Hard,
                Evidence),
            new ResourceRequirement(
                CombatResourceKind.Breath,
                minimumAmount: 6,
                CombatRequirementCriticality.Hard,
                Evidence),
            new WeaponUnlockRequirement(
                BladeWeaponType,
                CombatRequirementCriticality.Hard,
                Evidence),
            new SkillActivationRequirement(
                LaoJunSkill,
                SkillActivationState.EquippedPassive,
                CombatRequirementCriticality.Hard,
                Evidence),
            new SkillActivationRequirement(
                DefenseSkill,
                SkillActivationState.ActiveDefense,
                CombatRequirementCriticality.Hard,
                Evidence),
            new SkillActivationRequirement(
                WanhuaSkill,
                SkillActivationState.ActiveAgility,
                CombatRequirementCriticality.Hard,
                Evidence)
        ];
        var context = CreateContext(
            equippedWeaponTypeIds: [BladeWeaponType],
            trickCounts: [new CombatTrickCount(BladeTrickType, 3)],
            distance: SnapshotValue<int>.Available(4),
            resources:
            [
                Resource(CombatResourceKind.Neili, 10),
                Resource(CombatResourceKind.Stance, 5),
                Resource(CombatResourceKind.Breath, 6)
            ],
            unlockedWeaponTypeIds: [BladeWeaponType],
            equippedSkillIds:
                [LaoJunSkill, DefenseSkill, WanhuaSkill],
            activeDefenseSkillId: DefenseSkill,
            activeAgilitySkillId: WanhuaSkill);

        var result = CombatRequirementEvaluator.Evaluate(
            requirements,
            context);

        Assert.True(result.IsAccepted);
        Assert.Equal(requirements.Length, result.Evaluations.Length);
        Assert.Empty(result.Rejections);
        Assert.Empty(result.Warnings);
        Assert.All(
            result.Evaluations,
            evaluation => Assert.Equal(
                CombatRequirementStatus.Satisfied,
                evaluation.Status));
    }

    [Fact]
    public void Unsatisfied_hard_requirement_rejects_candidate()
    {
        var requirement = new WeaponRequirement(
            BladeWeaponType,
            CombatRequirementCriticality.Hard,
            Evidence);

        var result = CombatRequirementEvaluator.Evaluate(
            [requirement],
            CreateContext());

        Assert.False(result.IsAccepted);
        var rejection = Assert.Single(result.Rejections);
        Assert.Same(requirement, rejection.Requirement);
        Assert.Equal(CombatRequirementStatus.Unsatisfied, rejection.Status);
        Assert.Contains("not equipped", rejection.Reason);
    }

    [Fact]
    public void Every_supported_hard_requirement_has_a_rejection_case()
    {
        var cases = new (CombatRequirement Requirement,
            CombatRequirementContext Context)[]
        {
            (
                new WeaponRequirement(
                    BladeWeaponType,
                    CombatRequirementCriticality.Hard,
                    Evidence),
                CreateContext()),
            (
                new TrickRequirement(
                    BladeTrickType,
                    minimumCount: 3,
                    CombatRequirementCriticality.Hard,
                    Evidence),
                CreateContext(
                    trickCounts: [new CombatTrickCount(BladeTrickType, 2)])),
            (
                new RangeRequirement(
                    minimumInclusive: 3,
                    maximumInclusive: 7,
                    CombatRequirementCriticality.Hard,
                    Evidence),
                CreateContext(distance: SnapshotValue<int>.Available(8))),
            (
                new ResourceRequirement(
                    CombatResourceKind.Neili,
                    minimumAmount: 10,
                    CombatRequirementCriticality.Hard,
                    Evidence),
                CreateContext(
                    resources: [Resource(CombatResourceKind.Neili, 9)])),
            (
                new WeaponUnlockRequirement(
                    BladeWeaponType,
                    CombatRequirementCriticality.Hard,
                    Evidence),
                CreateContext()),
            (
                new SkillActivationRequirement(
                    LaoJunSkill,
                    SkillActivationState.EquippedPassive,
                    CombatRequirementCriticality.Hard,
                    Evidence),
                CreateContext())
        };

        foreach (var (requirement, context) in cases)
        {
            var result = CombatRequirementEvaluator.Evaluate(
                [requirement],
                context);

            Assert.False(result.IsAccepted);
            var rejection = Assert.Single(result.Rejections);
            Assert.Same(requirement, rejection.Requirement);
            Assert.Equal(
                CombatRequirementStatus.Unsatisfied,
                rejection.Status);
        }
    }

    [Fact]
    public void Unsatisfied_conditional_requirement_becomes_warning()
    {
        var requirement = new WeaponUnlockRequirement(
            BladeWeaponType,
            CombatRequirementCriticality.Conditional,
            Evidence);

        var result = CombatRequirementEvaluator.Evaluate(
            [requirement],
            CreateContext());

        Assert.True(result.IsAccepted);
        Assert.Empty(result.Rejections);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Unknown_hard_distance_rejects_with_evidence_preserved()
    {
        var requirement = new RangeRequirement(
            minimumInclusive: null,
            maximumInclusive: 7,
            CombatRequirementCriticality.Hard,
            "screen:tiejiao-range");
        var context = CreateContext(
            distance: SnapshotValue<int>.Unavailable(
                "Current distance was not reported."));

        var result = CombatRequirementEvaluator.Evaluate(
            [requirement],
            context);

        var rejection = Assert.Single(result.Rejections);
        Assert.Equal(CombatRequirementStatus.Unknown, rejection.Status);
        Assert.Contains(
            "Current distance was not reported.",
            rejection.Reason);
        Assert.Equal(
            "screen:tiejiao-range",
            rejection.Requirement.EvidenceReference);
    }

    [Fact]
    public void Unknown_conditional_resource_becomes_warning()
    {
        var requirement = new ResourceRequirement(
            CombatResourceKind.Neili,
            minimumAmount: 10,
            CombatRequirementCriticality.Conditional,
            Evidence);

        var result = CombatRequirementEvaluator.Evaluate(
            [requirement],
            CreateContext(resources: []));

        Assert.True(result.IsAccepted);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(CombatRequirementStatus.Unknown, warning.Status);
        Assert.Contains("not reported", warning.Reason);
    }

    [Theory]
    [InlineData(SkillActivationState.ActiveDefense)]
    [InlineData(SkillActivationState.ActiveAgility)]
    public void Only_one_defense_or_agility_skill_can_be_active(
        SkillActivationState state)
    {
        CombatRequirement[] requirements =
        [
            new SkillActivationRequirement(
                DefenseSkill,
                state,
                CombatRequirementCriticality.Hard,
                Evidence),
            new SkillActivationRequirement(
                OtherDefenseSkill,
                state,
                CombatRequirementCriticality.Hard,
                Evidence)
        ];
        var context = state == SkillActivationState.ActiveDefense
            ? CreateContext(
                equippedSkillIds: [DefenseSkill, OtherDefenseSkill],
                activeDefenseSkillId: DefenseSkill)
            : CreateContext(
                equippedSkillIds: [DefenseSkill, OtherDefenseSkill],
                activeAgilitySkillId: DefenseSkill);

        var result = CombatRequirementEvaluator.Evaluate(
            requirements,
            context);

        Assert.False(result.IsAccepted);
        Assert.Single(result.Rejections);
        Assert.Equal(
            OtherDefenseSkill,
            Assert.IsType<SkillActivationRequirement>(
                result.Rejections[0].Requirement).SkillId);
    }

    [Fact]
    public void Active_skill_must_also_be_equipped()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateContext(activeDefenseSkillId: DefenseSkill));

        Assert.Contains("must also be equipped", exception.Message);
    }

    [Fact]
    public void One_skill_cannot_be_active_defense_and_agility()
    {
        Assert.Throws<ArgumentException>(
            () => CreateContext(
                equippedSkillIds: [DefenseSkill],
                activeDefenseSkillId: DefenseSkill,
                activeAgilitySkillId: DefenseSkill));
    }

    [Fact]
    public void Golden_anti_magic_conditions_are_supported()
    {
        CombatRequirement[] requirements =
        [
            new SkillActivationRequirement(
                LaoJunSkill,
                SkillActivationState.EquippedPassive,
                CombatRequirementCriticality.Hard,
                "anti-magic:laojun-equipped"),
            new SkillActivationRequirement(
                WanhuaSkill,
                SkillActivationState.ActiveAgility,
                CombatRequirementCriticality.Hard,
                "anti-magic:wanhua-active"),
            new WeaponUnlockRequirement(
                BladeWeaponType,
                CombatRequirementCriticality.Conditional,
                "anti-magic:guipaoding-unlock"),
            new TrickRequirement(
                BladeTrickType,
                minimumCount: 3,
                CombatRequirementCriticality.Conditional,
                "anti-magic:guipaoding-tricks"),
            new RangeRequirement(
                minimumInclusive: null,
                maximumInclusive: 4,
                CombatRequirementCriticality.Conditional,
                "anti-magic:sanbu-range")
        ];
        var context = CreateContext(
            distance: SnapshotValue<int>.Available(6),
            equippedSkillIds: [LaoJunSkill, WanhuaSkill],
            activeAgilitySkillId: WanhuaSkill);

        var result = CombatRequirementEvaluator.Evaluate(
            requirements,
            context);

        Assert.True(result.IsAccepted);
        Assert.Equal(3, result.Warnings.Length);
        Assert.Contains(
            result.Warnings,
            warning => warning.Requirement.EvidenceReference
                == "anti-magic:guipaoding-unlock");
        Assert.Contains(
            result.Warnings,
            warning => warning.Requirement.EvidenceReference
                == "anti-magic:guipaoding-tricks");
        Assert.Contains(
            result.Warnings,
            warning => warning.Requirement.EvidenceReference
                == "anti-magic:sanbu-range");
    }

    [Fact]
    public void Every_unsatisfied_hard_requirement_is_returned()
    {
        CombatRequirement[] requirements =
        [
            new WeaponRequirement(
                BladeWeaponType,
                CombatRequirementCriticality.Hard,
                Evidence),
            new ResourceRequirement(
                CombatResourceKind.Stance,
                minimumAmount: 5,
                CombatRequirementCriticality.Hard,
                Evidence),
            new SkillActivationRequirement(
                LaoJunSkill,
                SkillActivationState.EquippedPassive,
                CombatRequirementCriticality.Hard,
                Evidence)
        ];

        var result = CombatRequirementEvaluator.Evaluate(
            requirements,
            CreateContext(
                resources:
                [
                    Resource(CombatResourceKind.Stance, 0)
                ]));

        Assert.False(result.IsAccepted);
        Assert.Equal(3, result.Rejections.Length);
    }

    [Fact]
    public void Requirement_construction_rejects_invalid_or_unevidenced_rules()
    {
        Assert.Throws<ArgumentException>(
            () => new WeaponRequirement(
                BladeWeaponType,
                CombatRequirementCriticality.Hard,
                " "));
        Assert.Throws<ArgumentException>(
            () => new RangeRequirement(
                minimumInclusive: null,
                maximumInclusive: null,
                CombatRequirementCriticality.Hard,
                Evidence));
        Assert.Throws<ArgumentException>(
            () => new RangeRequirement(
                minimumInclusive: 8,
                maximumInclusive: 7,
                CombatRequirementCriticality.Hard,
                Evidence));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TrickRequirement(
                BladeTrickType,
                minimumCount: 0,
                CombatRequirementCriticality.Hard,
                Evidence));
    }

    private static CombatResourceAmount Resource(
        CombatResourceKind resource,
        int amount)
    {
        return new CombatResourceAmount(
            resource,
            SnapshotValue<int>.Available(amount));
    }

    private static CombatRequirementContext CreateContext(
        int[]? equippedWeaponTypeIds = null,
        CombatTrickCount[]? trickCounts = null,
        SnapshotValue<int>? distance = null,
        CombatResourceAmount[]? resources = null,
        int[]? unlockedWeaponTypeIds = null,
        int[]? equippedSkillIds = null,
        int? activeDefenseSkillId = null,
        int? activeAgilitySkillId = null)
    {
        return new CombatRequirementContext(
            equippedWeaponTypeIds ?? [],
            trickCounts ?? [],
            distance ?? SnapshotValue<int>.Available(0),
            resources ?? [],
            unlockedWeaponTypeIds ?? [],
            equippedSkillIds ?? [],
            activeDefenseSkillId,
            activeAgilitySkillId);
    }
}
