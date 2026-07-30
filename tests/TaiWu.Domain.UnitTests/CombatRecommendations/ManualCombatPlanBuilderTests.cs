using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatRecommendations;

public sealed class ManualCombatPlanBuilderTests
{
    [Fact]
    public void Reports_manual_add_remove_retain_and_direction_changes()
    {
        var retained = Skill(100, SkillCategory.Attack);
        var removed = Skill(101, SkillCategory.Defense);
        var added = Skill(102, SkillCategory.Defense);
        var player = Player(
            [retained, removed, added],
            attack: [retained.SkillId],
            defense: [removed.SkillId]);
        CombatLoadoutOption[] options =
        [
            CombatLoadoutOption.RetainCurrentSkill(
                retained.SkillId,
                "snapshot:retained"),
            Option(
                added,
                CombatCounterActivationTiming.ActiveDefense,
                requiredDirection: PracticeDirection.Reverse)
        ];
        var candidate = GenerateExact(
            player,
            options,
            retained.SkillId,
            added.SkillId);

        var result = Build(player, candidate);

        var plan = Assert.IsType<ManualCombatPlan>(result.Plan);
        Assert.Contains(
            plan.LoadoutChanges,
            change => change.Kind == ManualLoadoutChangeKind.Add
                && change.SkillId == added.SkillId);
        Assert.Contains(
            plan.LoadoutChanges,
            change => change.Kind == ManualLoadoutChangeKind.Remove
                && change.SkillId == removed.SkillId);
        Assert.Contains(
            plan.LoadoutChanges,
            change => change.Kind == ManualLoadoutChangeKind.Retain
                && change.SkillId == retained.SkillId);
        var direction = Assert.Single(
            plan.LoadoutChanges,
            change => change.Kind
                == ManualLoadoutChangeKind.ChangeDirection);
        Assert.Equal(added.SkillId, direction.SkillId);
        Assert.Equal(PracticeDirection.Reverse, direction.RequiredDirection);
    }

    [Fact]
    public void Every_manual_instruction_references_a_reason()
    {
        var current = Skill(100, SkillCategory.Attack);
        var replacement = Skill(101, SkillCategory.Attack);
        var player = Player(
            [current, replacement],
            attack: [current.SkillId]);
        var candidate = GenerateExact(
            player,
            [
                Option(
                    replacement,
                    CombatCounterActivationTiming.ActiveAttack)
            ],
            replacement.SkillId);

        var plan = Assert.IsType<ManualCombatPlan>(
            Build(player, candidate).Plan);
        var reasons = plan.LoadoutChanges
            .Select(change => change.Reason)
            .Concat(plan.OpeningActions.Select(action => action.Reason))
            .Concat(
                plan.SwitchingConditions.Select(action => action.Reason));

        Assert.All(
            reasons,
            reason =>
            {
                Assert.False(string.IsNullOrWhiteSpace(reason.Code));
                Assert.False(string.IsNullOrWhiteSpace(reason.Summary));
                Assert.All(
                    reason.EvidenceReferences,
                    evidence => Assert.False(
                        string.IsNullOrWhiteSpace(evidence)));
                Assert.NotEmpty(reason.EvidenceReferences);
            });
    }

    [Fact]
    public void Identifies_primary_and_alternative_defense_and_agility()
    {
        var defensePrimary = Skill(100, SkillCategory.Defense);
        var defenseAlternative = Skill(101, SkillCategory.Defense);
        var agilityPrimary = Skill(200, SkillCategory.Agility);
        var agilityAlternative = Skill(201, SkillCategory.Agility);
        var player = Player(
            [
                defensePrimary,
                defenseAlternative,
                agilityPrimary,
                agilityAlternative
            ]);
        var primaryCandidate = GenerateExact(
            player,
            [
                Option(
                    defensePrimary,
                    CombatCounterActivationTiming.ActiveDefense),
                Option(
                    agilityPrimary,
                    CombatCounterActivationTiming.ActiveAgility)
            ],
            defensePrimary.SkillId,
            agilityPrimary.SkillId);
        var alternativeCandidate = GenerateExact(
            player,
            [
                Option(
                    defenseAlternative,
                    CombatCounterActivationTiming.ActiveDefense),
                Option(
                    agilityAlternative,
                    CombatCounterActivationTiming.ActiveAgility)
            ],
            defenseAlternative.SkillId,
            agilityAlternative.SkillId);

        var plan = Assert.IsType<ManualCombatPlan>(
            Build(
                player,
                primaryCandidate,
                alternativeCandidate).Plan);

        Assert.Equal(
            defensePrimary.SkillId,
            plan.Defense.Primary!.SkillId);
        Assert.Equal(
            defenseAlternative.SkillId,
            Assert.Single(plan.Defense.Alternatives).SkillId);
        Assert.Equal(
            agilityPrimary.SkillId,
            plan.Agility.Primary!.SkillId);
        Assert.Equal(
            agilityAlternative.SkillId,
            Assert.Single(plan.Agility.Alternatives).SkillId);
    }

    [Fact]
    public void Opening_actions_follow_verified_activation_timing()
    {
        var passive = Skill(100, SkillCategory.Neigong);
        var attack = Skill(101, SkillCategory.Attack);
        var player = Player([passive, attack]);
        var candidate = GenerateExact(
            player,
            [
                Option(
                    passive,
                    CombatCounterActivationTiming.CombatStartPassive),
                Option(
                    attack,
                    CombatCounterActivationTiming.ActiveAttack)
            ],
            passive.SkillId,
            attack.SkillId);

        var plan = Assert.IsType<ManualCombatPlan>(
            Build(player, candidate).Plan);

        Assert.Collection(
            plan.OpeningActions,
            action =>
            {
                Assert.Equal(
                    BattlePlanInstructionKind.ConfirmEquipped,
                    action.Kind);
                Assert.Equal(passive.SkillId, action.SkillId);
                Assert.Contains("Before combat", action.Condition);
            },
            action =>
            {
                Assert.Equal(
                    BattlePlanInstructionKind.ActivateSkill,
                    action.Kind);
                Assert.Equal(attack.SkillId, action.SkillId);
                Assert.Contains("requirements", action.Condition);
            });
    }

    [Fact]
    public void Alternatives_create_precombat_switching_conditions()
    {
        var primary = Skill(100, SkillCategory.Defense);
        var alternative = Skill(101, SkillCategory.Defense);
        var player = Player([primary, alternative]);
        var primaryCandidate = GenerateExact(
            player,
            [
                Option(
                    primary,
                    CombatCounterActivationTiming.ActiveDefense)
            ],
            primary.SkillId);
        var alternativeCandidate = GenerateExact(
            player,
            [
                Option(
                    alternative,
                    CombatCounterActivationTiming.ActiveDefense)
            ],
            alternative.SkillId);

        var plan = Assert.IsType<ManualCombatPlan>(
            Build(
                player,
                primaryCandidate,
                alternativeCandidate).Plan);

        var instruction = Assert.Single(plan.SwitchingConditions);
        Assert.Equal(
            BattlePlanInstructionKind.SwitchBeforeCombat,
            instruction.Kind);
        Assert.Equal(primary.SkillId, instruction.SkillId);
        Assert.Equal(
            alternative.SkillId,
            instruction.AlternativeSkillId);
        Assert.Contains("Before combat", instruction.Condition);
    }

    [Fact]
    public void Empty_ranking_returns_a_diagnostic_instead_of_a_plan()
    {
        var player = Player([]);
        var scoring = CombatRecommendationScorer.Score(
            new CombatRecommendationScoringRequest(
                player,
                targetThreats: [],
                candidates: [],
                RecommendationPolicy.Balanced));

        var result = ManualCombatPlanBuilder.Build(player, scoring);

        Assert.False(result.HasPlan);
        Assert.Null(result.Plan);
        Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
    }

    [Fact]
    public void Plan_is_stable_for_the_same_ranked_candidates()
    {
        var first = Skill(100, SkillCategory.Defense);
        var second = Skill(101, SkillCategory.Defense);
        var player = Player([first, second]);
        var firstCandidate = GenerateExact(
            player,
            [
                Option(
                    first,
                    CombatCounterActivationTiming.ActiveDefense)
            ],
            first.SkillId);
        var secondCandidate = GenerateExact(
            player,
            [
                Option(
                    second,
                    CombatCounterActivationTiming.ActiveDefense)
            ],
            second.SkillId);

        var forward = Build(player, firstCandidate, secondCandidate).Plan!;
        var reverse = Build(player, secondCandidate, firstCandidate).Plan!;

        Assert.Equal(
            forward.SelectedRecommendation.Candidate.StableKey,
            reverse.SelectedRecommendation.Candidate.StableKey);
        Assert.Equal(
            forward.LoadoutChanges.Select(ChangeKey),
            reverse.LoadoutChanges.Select(ChangeKey));
        Assert.Equal(
            RoleKey(forward.Defense),
            RoleKey(reverse.Defense));
        Assert.Equal(
            forward.OpeningActions.Select(InstructionKey),
            reverse.OpeningActions.Select(InstructionKey));
        Assert.Equal(
            forward.SwitchingConditions.Select(InstructionKey),
            reverse.SwitchingConditions.Select(InstructionKey));
    }

    private static ManualCombatPlanResult Build(
        PlayerCombatSnapshot player,
        params GeneratedCombatLoadout[] candidates)
    {
        var scoring = CombatRecommendationScorer.Score(
            new CombatRecommendationScoringRequest(
                player,
                targetThreats: [],
                candidates,
                RecommendationPolicy.Balanced));
        return ManualCombatPlanBuilder.Build(player, scoring);
    }

    private static GeneratedCombatLoadout GenerateExact(
        PlayerCombatSnapshot player,
        CombatLoadoutOption[] options,
        params int[] selectedSkillIds)
    {
        var result = CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                player,
                options,
                Context(),
                player.GenericSlotAllocation));
        var expected = selectedSkillIds.Order().ToArray();
        return result.Candidates.Single(candidate =>
            candidate.SelectedOptions
                .Select(option => option.Candidate.SkillId)
                .Order()
                .SequenceEqual(expected));
    }

    private static CombatLoadoutOption Option(
        CombatSkillSnapshot skill,
        CombatCounterActivationTiming timing,
        PracticeDirection? requiredDirection = null)
    {
        return new CombatLoadoutOption(
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: requiredDirection,
                allowDirectionChange: requiredDirection.HasValue),
            requirements: [],
            threatCodes: ["VERIFIED_THREAT"],
            isCurrentlyEquipped: false,
            $"evidence:skill:{skill.SkillId}",
            CombatCounterStrength.Mitigation,
            timing,
            expectedEffectId: requiredDirection == PracticeDirection.Direct
                ? skill.DirectEffectId.Value
                : requiredDirection == PracticeDirection.Reverse
                    ? skill.ReverseEffectId.Value
                    : null);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(1000 + skillId),
            SnapshotValue<int>.Available(2000 + skillId));
    }

    private static PlayerCombatSnapshot Player(
        CombatSkillSnapshot[] skills,
        int[]? neigong = null,
        int[]? attack = null,
        int[]? agility = null,
        int[]? defense = null,
        int[]? assistance = null)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            new CombatLoadoutSnapshot(
                neigong ?? [],
                attack ?? [],
                agility ?? [],
                defense ?? [],
                assistance ?? []),
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

    private static CombatRequirementContext Context()
    {
        return new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(0),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: []);
    }

    private static string ChangeKey(ManualLoadoutChange change) =>
        $"{change.Kind}:{change.Category}:{change.SkillId}:"
        + $"{change.RequiredDirection}:{change.Reason.Code}";

    private static string RoleKey(CombatRoleRecommendation role) =>
        $"{role.Primary?.SkillId}:"
        + string.Join(",", role.Alternatives.Select(value => value.SkillId));

    private static string InstructionKey(BattlePlanInstruction instruction) =>
        $"{instruction.Sequence}:{instruction.Kind}:{instruction.SkillId}:"
        + $"{instruction.AlternativeSkillId}:{instruction.Reason.Code}";
}
