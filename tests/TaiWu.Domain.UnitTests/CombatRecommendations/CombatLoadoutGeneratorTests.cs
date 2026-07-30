using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatRecommendations;

public sealed class CombatLoadoutGeneratorTests
{
    [Fact]
    public void Every_emitted_candidate_has_passed_feasibility()
    {
        var first = CreateSkill(100);
        var second = CreateSkill(101);
        var result = Generate(
            CreatePlayer([first, second]),
            [Option(first), Option(second)]);

        Assert.Equal(3, result.Candidates.Length);
        Assert.All(
            result.Candidates,
            candidate =>
            {
                Assert.NotNull(candidate.FeasibleLoadout);
                Assert.NotNull(
                    candidate.FeasibleLoadout.SlotBudgets);
            });
    }

    [Fact]
    public void Over_budget_combinations_are_excluded_with_diagnostics()
    {
        var skills = Enumerable.Range(100, 3)
            .Select(skillId => CreateSkill(skillId))
            .ToArray();

        var result = Generate(
            CreatePlayer(skills),
            [.. skills.Select(skill => Option(skill))]);

        Assert.DoesNotContain(
            result.Candidates,
            candidate => candidate.SelectedOptions.Length == 3);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code
                == CombatLoadoutGenerationDiagnosticCode
                    .CombinationInfeasible
                && diagnostic.FeasibilityFailures.Any(
                    failure => failure.Code
                        == CombatLoadoutFeasibilityFailureCode
                            .SlotBudgetInvalid));
    }

    [Fact]
    public void Combat_start_counter_is_considered_first()
    {
        var combatStart = CreateSkill(100);
        var ordinary = CreateSkill(101);
        var result = Generate(
            CreatePlayer([combatStart, ordinary]),
            [
                Option(
                    combatStart,
                    threatCodes: ["THREAT"],
                    strength: CombatCounterStrength.Mitigation,
                    timing:
                        CombatCounterActivationTiming.CombatStartPassive),
                Option(ordinary, threatCodes: ["THREAT"])
            ]);

        Assert.Equal(
            combatStart.SkillId,
            result.Candidates[0].SelectedOptions[0].Candidate.SkillId);
        Assert.Equal(1, result.Candidates[0].CombatStartCounterCount);
    }

    [Fact]
    public void Exploration_is_bounded_and_reports_truncation()
    {
        var skills = Enumerable.Range(100, 5)
            .Select(skillId => CreateSkill(skillId))
            .ToArray();

        var result = Generate(
            CreatePlayer(skills),
            [.. skills.Select(skill => Option(skill))],
            maxExploredCombinations: 2);

        Assert.Equal(2, result.ExploredCombinations);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code
                == CombatLoadoutGenerationDiagnosticCode
                    .ExplorationLimitReached);
    }

    [Fact]
    public void Curated_option_limit_supports_a_full_observed_loadout()
    {
        var skills = Enumerable
            .Range(100, CombatLoadoutGenerationRequest.MaximumOptions + 1)
            .Select(skillId => CreateSkill(skillId))
            .ToArray();
        var player = CreatePlayer(skills);
        var acceptedOptions = skills
            .Take(CombatLoadoutGenerationRequest.MaximumOptions)
            .Select(skill => Option(skill))
            .ToArray();

        var accepted = new CombatLoadoutGenerationRequest(
            player,
            acceptedOptions,
            CreateContext(),
            player.GenericSlotAllocation);

        Assert.Equal(
            CombatLoadoutGenerationRequest.MaximumOptions,
            accepted.Options.Length);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatLoadoutGenerationRequest(
                player,
                skills.Select(skill => Option(skill)),
                CreateContext(),
                player.GenericSlotAllocation));
    }

    [Fact]
    public void Input_order_does_not_change_candidate_order()
    {
        var skills = Enumerable.Range(100, 3)
            .Select(skillId => CreateSkill(skillId))
            .ToArray();
        var options = skills.Select(skill => Option(skill)).ToArray();

        var first = Generate(CreatePlayer(skills), options);
        var second = Generate(
            CreatePlayer(skills),
            [.. options.Reverse()]);

        Assert.Equal(
            first.Candidates.Select(candidate => candidate.StableKey),
            second.Candidates.Select(candidate => candidate.StableKey));
    }

    [Fact]
    public void Current_skill_is_retained_when_coverage_is_equal()
    {
        var current = CreateSkill(100);
        var replacement = CreateSkill(101);
        var player = CreatePlayer(
            [current, replacement],
            CreateLoadout(attack: [current.SkillId]));

        var result = Generate(
            player,
            [
                Option(
                    current,
                    threatCodes: ["THREAT"],
                    isCurrentlyEquipped: true),
                Option(replacement, threatCodes: ["THREAT"])
            ]);

        var best = result.Candidates[0];
        var selected = Assert.Single(best.SelectedOptions);
        Assert.Equal(current.SkillId, selected.Candidate.SkillId);
        Assert.Equal(1, best.RetainedCurrentSkillCount);
    }

    [Fact]
    public void Equipped_neigong_is_required_to_preserve_slot_capacity()
    {
        var neigong = CreateSkill(100, SkillCategory.Neigong);
        var attack = CreateSkill(101);
        var player = CreatePlayer(
            [neigong, attack],
            new CombatLoadoutSnapshot(
                neigongSkillIds: [neigong.SkillId],
                attackSkillIds: [],
                agilitySkillIds: [],
                defenseSkillIds: [],
                assistanceSkillIds: []));

        var result = Generate(
            player,
            [
                Option(neigong, isCurrentlyEquipped: true),
                Option(attack, threatCodes: ["THREAT"])
            ]);

        Assert.NotEmpty(result.Candidates);
        Assert.All(
            result.Candidates,
            candidate => Assert.Contains(
                neigong.SkillId,
                candidate.FeasibleLoadout.Proposal.Skills.NeigongSkillIds));
    }

    [Fact]
    public void Strategic_counter_preserves_every_current_skill_that_fits()
    {
        var firstCurrent = CreateSkill(100);
        var secondCurrent = CreateSkill(101);
        var counter = CreateSkill(102);
        var player = CreatePlayer(
            [firstCurrent, secondCurrent, counter],
            CreateLoadout(
                attack:
                [
                    firstCurrent.SkillId,
                    secondCurrent.SkillId
                ]));

        var result = Generate(
            player,
            [
                Option(firstCurrent, isCurrentlyEquipped: true),
                Option(secondCurrent, isCurrentlyEquipped: true),
                Option(
                    counter,
                    threatCodes: ["THREAT"],
                    strength: CombatCounterStrength.HardCounter,
                    timing: CombatCounterActivationTiming.ActiveAttack)
            ]);

        var counterCandidate = Assert.Single(
            result.Candidates,
            candidate => candidate.ThreatCodes.Contains("THREAT"));
        Assert.Equal(1, counterCandidate.RetainedCurrentSkillCount);
        Assert.Equal(2, counterCandidate.SelectedOptions.Length);
    }

    [Fact]
    public void Rejected_option_is_explained_before_combinations()
    {
        var learned = CreateSkill(100);
        var missing = CreateSkill(999);

        var result = Generate(
            CreatePlayer([learned]),
            [Option(learned), Option(missing)]);

        var diagnostic = Assert.Single(
            result.Diagnostics,
            value => value.Code
                == CombatLoadoutGenerationDiagnosticCode.OptionRejected);
        Assert.Equal(missing.SkillId, diagnostic.SkillId);
        Assert.Contains("learned-skill snapshot", diagnostic.Reason);
    }

    [Fact]
    public void Changed_verified_effect_is_rejected_before_combinations()
    {
        var skill = CreateSkill(100);
        var option = new CombatLoadoutOption(
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct),
            requirements: [],
            threatCodes: ["THREAT"],
            isCurrentlyEquipped: false,
            evidenceReference: "local-config:effect-expected",
            CombatCounterStrength.HardCounter,
            CombatCounterActivationTiming.ActiveAttack,
            expectedEffectId: skill.DirectEffectId.Value + 1);

        var result = Generate(CreatePlayer([skill]), [option]);

        Assert.Empty(result.Candidates);
        var diagnostic = Assert.Single(
            result.Diagnostics,
            value => value.Code
                == CombatLoadoutGenerationDiagnosticCode.OptionRejected);
        Assert.Contains("expected effect", diagnostic.Reason);
    }

    [Fact]
    public void Matching_verified_effect_is_accepted()
    {
        var skill = CreateSkill(100);
        var option = new CombatLoadoutOption(
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Direct),
            requirements: [],
            threatCodes: ["THREAT"],
            isCurrentlyEquipped: false,
            evidenceReference: "local-config:effect-expected",
            CombatCounterStrength.HardCounter,
            CombatCounterActivationTiming.ActiveAttack,
            expectedEffectId: skill.DirectEffectId.Value);

        var result = Generate(CreatePlayer([skill]), [option]);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(
            skill.SkillId,
            Assert.Single(candidate.SelectedOptions).Candidate.SkillId);
    }

    [Fact]
    public void Rejected_current_counter_falls_back_to_plain_retention()
    {
        var skill = CreateSkill(
            100,
            direction: PracticeDirection.Direct);
        var player = CreatePlayer(
            [skill],
            CreateLoadout(attack: [skill.SkillId]));
        var option = new CombatLoadoutOption(
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Reverse),
            requirements: [],
            threatCodes: ["THREAT"],
            isCurrentlyEquipped: true,
            evidenceReference: "local-config:reverse-effect",
            CombatCounterStrength.HardCounter,
            CombatCounterActivationTiming.ActiveAttack,
            expectedEffectId: skill.ReverseEffectId.Value);

        var result = Generate(player, [option]);

        var candidate = Assert.Single(result.Candidates);
        var retained = Assert.Single(candidate.SelectedOptions);
        Assert.True(retained.IsCurrentlyEquipped);
        Assert.Empty(retained.ThreatCodes);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code
                    == CombatLoadoutGenerationDiagnosticCode.OptionRejected
                && diagnostic.SkillId == skill.SkillId);
    }

    [Fact]
    public void Request_rejects_duplicate_options_and_out_of_range_bounds()
    {
        var skill = CreateSkill(100);
        var player = CreatePlayer([skill]);
        var option = Option(skill);

        Assert.Throws<ArgumentException>(
            () => new CombatLoadoutGenerationRequest(
                player,
                [option, option],
                CreateContext(),
                player.GenericSlotAllocation));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatLoadoutGenerationRequest(
                player,
                [option],
                CreateContext(),
                player.GenericSlotAllocation,
                maxExploredCombinations: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatLoadoutGenerationRequest(
                player,
                [option],
                CreateContext(),
                player.GenericSlotAllocation,
                maxResults: CombatLoadoutGenerationRequest.MaximumResults + 1));
    }

    [Fact]
    public void Conflicting_active_agility_options_are_not_emitted()
    {
        var first = CreateSkill(100, SkillCategory.Agility);
        var second = CreateSkill(101, SkillCategory.Agility);
        var result = Generate(
            CreatePlayer([first, second]),
            [
                Option(
                    first,
                    strength: CombatCounterStrength.Mitigation,
                    timing: CombatCounterActivationTiming.ActiveAgility),
                Option(
                    second,
                    strength: CombatCounterStrength.Mitigation,
                    timing: CombatCounterActivationTiming.ActiveAgility)
            ]);

        Assert.DoesNotContain(
            result.Candidates,
            candidate => candidate.SelectedOptions.Length == 2);
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code
                == CombatLoadoutGenerationDiagnosticCode
                    .ActiveRoleConflict);
    }

    [Fact]
    public void Manual_direction_change_can_produce_feasible_candidate()
    {
        var skill = CreateSkill(
            100,
            direction: PracticeDirection.Neutral);
        var option = new CombatLoadoutOption(
            new CombatSkillCandidate(
                skill.SkillId,
                requiredDirection: PracticeDirection.Reverse,
                allowDirectionChange: true),
            requirements: [],
            threatCodes: ["THREAT"],
            isCurrentlyEquipped: false,
            evidenceReference: "local-config:effect-2000",
            CombatCounterStrength.HardCounter,
            CombatCounterActivationTiming.ActiveAttack);

        var result = Generate(CreatePlayer([skill]), [option]);

        var candidate = Assert.Single(result.Candidates);
        var validation = Assert.Single(
            candidate.FeasibleLoadout.Proposal.SkillCandidates);
        Assert.True(validation.AllowDirectionChange);
        Assert.Equal(
            PracticeDirection.Reverse,
            validation.RequiredDirection);
    }

    private static CombatLoadoutGenerationResult Generate(
        PlayerCombatSnapshot player,
        CombatLoadoutOption[] options,
        int maxExploredCombinations = 4096)
    {
        return CombatLoadoutGenerator.Generate(
            new CombatLoadoutGenerationRequest(
                player,
                options,
                CreateContext(),
                player.GenericSlotAllocation,
                maxExploredCombinations,
                maxResults: 32));
    }

    private static CombatLoadoutOption Option(
        CombatSkillSnapshot skill,
        string[]? threatCodes = null,
        bool isCurrentlyEquipped = false,
        CombatCounterStrength? strength = null,
        CombatCounterActivationTiming? timing = null)
    {
        return new CombatLoadoutOption(
            new CombatSkillCandidate(skill.SkillId),
            requirements: [],
            threatCodes ?? [],
            isCurrentlyEquipped,
            $"snapshot:skill:{skill.SkillId}",
            strength,
            timing);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId,
        SkillCategory category = SkillCategory.Attack,
        PracticeDirection direction = PracticeDirection.Direct)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(1000 + skillId),
            SnapshotValue<int>.Available(2000 + skillId));
    }

    private static CombatRequirementContext CreateContext()
    {
        return new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(0),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds: []);
    }

    private static PlayerCombatSnapshot CreatePlayer(
        CombatSkillSnapshot[] skills,
        CombatLoadoutSnapshot? loadout = null)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            loadout ?? CreateLoadout(),
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

    private static CombatLoadoutSnapshot CreateLoadout(
        int[]? attack = null)
    {
        return new CombatLoadoutSnapshot(
            neigongSkillIds: [],
            attack ?? [],
            agilitySkillIds: [],
            defenseSkillIds: [],
            assistanceSkillIds: []);
    }
}
