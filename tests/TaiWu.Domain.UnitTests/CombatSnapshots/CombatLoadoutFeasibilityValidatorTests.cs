using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatLoadoutFeasibilityValidatorTests
{
    private const string Evidence = "docs/evidence/verified-rule.md";

    [Fact]
    public void Valid_proposal_produces_accepted_only_loadout()
    {
        var attack = CreateSkill(200, SkillCategory.Attack);
        var loadout = CreateLoadout(attack: [attack.SkillId]);
        var proposal = CreateProposal(
            loadout,
            [new CombatSkillCandidate(attack.SkillId)]);

        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([attack]),
            proposal);

        Assert.True(result.IsFeasible);
        Assert.Empty(result.Failures);
        Assert.NotNull(result.SlotBudgets);
        Assert.Same(proposal, result.FeasibleLoadout!.Proposal);
        Assert.Same(result.SlotBudgets, result.FeasibleLoadout.SlotBudgets);
    }

    [Fact]
    public void Over_budget_proposal_is_rejected_before_scoring()
    {
        var attacks = Enumerable.Range(200, 3)
            .Select(skillId =>
                CreateSkill(skillId, SkillCategory.Attack))
            .ToArray();
        var loadout = CreateLoadout(
            attack: [.. attacks.Select(skill => skill.SkillId)]);
        var proposal = CreateProposal(
            loadout,
            [.. attacks.Select(skill =>
                new CombatSkillCandidate(skill.SkillId))]);

        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer(attacks),
            proposal);

        Assert.False(result.IsFeasible);
        Assert.Null(result.FeasibleLoadout);
        Assert.Null(result.SlotBudgets);
        AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.SlotBudgetInvalid);
    }

    [Fact]
    public void Observed_capacity_adjustment_is_preserved_for_proposal()
    {
        var neigong = CreateSkill(
            100,
            SkillCategory.Neigong,
            contribution: new SkillSlotContribution(
                attack: 1,
                agility: 0,
                defense: 0,
                assistance: 0,
                generic: 0));
        var attacks = Enumerable.Range(200, 4)
            .Select(skillId =>
                CreateSkill(skillId, SkillCategory.Attack))
            .ToArray();
        var loadout = CreateLoadout(
            neigong: [neigong.SkillId],
            attack: [.. attacks.Select(skill => skill.SkillId)]);
        CombatSkillSnapshot[] skills = [neigong, .. attacks];
        var candidates = skills
            .Select(skill => new CombatSkillCandidate(skill.SkillId))
            .ToArray();
        var player = CreatePlayer(
            skills,
            loadout,
            CreateSlotBudgets(attackCapacity: 4));

        var result = CombatLoadoutFeasibilityValidator.Validate(
            player,
            CreateProposal(loadout, candidates));

        Assert.True(result.IsFeasible);
        Assert.Equal(
            4,
            result.FeasibleLoadout!
                .SlotBudgets[SkillCategory.Attack].Capacity);
    }

    [Fact]
    public void Mutually_incompatible_active_skills_are_rejected()
    {
        var first = CreateSkill(300, SkillCategory.Defense);
        var second = CreateSkill(301, SkillCategory.Defense);
        var loadout = CreateLoadout(
            defense: [first.SkillId, second.SkillId]);
        CombatRequirement[] requirements =
        [
            ActiveDefense(first.SkillId),
            ActiveDefense(second.SkillId)
        ];
        var context = CreateContext(
            [first.SkillId, second.SkillId],
            activeDefenseSkillId: first.SkillId);
        var proposal = CreateProposal(
            loadout,
            [
                new CombatSkillCandidate(first.SkillId),
                new CombatSkillCandidate(second.SkillId)
            ],
            requirements,
            context);

        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([first, second]),
            proposal);

        Assert.False(result.IsFeasible);
        Assert.Null(result.FeasibleLoadout);
        var failure = AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.RequirementRejected);
        Assert.Contains(second.SkillId.ToString(), failure.Reason);
        Assert.Single(result.RequirementEvaluation.Rejections);
    }

    [Fact]
    public void Every_independent_failure_is_returned()
    {
        var rejectedAttack = CreateSkill(
            200,
            SkillCategory.Attack,
            direction: PracticeDirection.Reverse);
        var unknownAttack = 999;
        var loadout = CreateLoadout(
            attack: [rejectedAttack.SkillId, unknownAttack]);
        var proposal = CreateProposal(
            loadout,
            [
                new CombatSkillCandidate(
                    rejectedAttack.SkillId,
                    requiredDirection: PracticeDirection.Direct)
            ],
            [
                new WeaponRequirement(
                    weaponTypeId: 10,
                    CombatRequirementCriticality.Hard,
                    Evidence)
            ]);

        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([rejectedAttack]),
            proposal);

        Assert.False(result.IsFeasible);
        Assert.Null(result.FeasibleLoadout);
        AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.CandidateRejected);
        AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.CandidateMissing);
        AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.RequirementRejected);
        AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.SlotBudgetInvalid);
    }

    [Fact]
    public void Candidate_for_unselected_skill_is_rejected()
    {
        var attack = CreateSkill(200, SkillCategory.Attack);
        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([attack]),
            CreateProposal(
                CreateLoadout(),
                [new CombatSkillCandidate(attack.SkillId)]));

        var failure = AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.CandidateNotSelected);
        Assert.Equal(attack.SkillId, failure.SkillId);
        Assert.Null(result.FeasibleLoadout);
    }

    [Fact]
    public void Requirement_context_must_describe_proposed_loadout()
    {
        var attack = CreateSkill(200, SkillCategory.Attack);
        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([attack]),
            CreateProposal(
                CreateLoadout(attack: [attack.SkillId]),
                [new CombatSkillCandidate(attack.SkillId)],
                context: CreateContext([])));

        AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode
                .RequirementContextMismatch);
        Assert.Null(result.FeasibleLoadout);
    }

    [Fact]
    public void Generic_slot_total_must_match_selected_neigong()
    {
        var neigong = CreateSkill(
            100,
            SkillCategory.Neigong,
            contribution: new SkillSlotContribution(
                attack: 0,
                agility: 0,
                defense: 0,
                assistance: 0,
                generic: 2));
        var loadout = CreateLoadout(neigong: [neigong.SkillId]);
        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([neigong]),
            CreateProposal(
                loadout,
                [new CombatSkillCandidate(neigong.SkillId)],
                allocation: new GenericSlotAllocation(
                    totalSlots: 1,
                    attack: 0,
                    agility: 0,
                    defense: 0,
                    assistance: 0)));

        var failure = AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode
                .GenericSlotTotalMismatch);
        Assert.Contains("2", failure.Reason);
        Assert.Null(result.FeasibleLoadout);
    }

    [Fact]
    public void Unavailable_skill_cost_blocks_feasible_output()
    {
        var attack = CreateSkill(
            200,
            SkillCategory.Attack,
            cost: SnapshotValue<int>.Unavailable(
                "Grid cost was not mapped."));
        var loadout = CreateLoadout(attack: [attack.SkillId]);
        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([attack]),
            CreateProposal(
                loadout,
                [new CombatSkillCandidate(attack.SkillId)]));

        AssertFailure(
            result,
            CombatLoadoutFeasibilityFailureCode.SlotUsageUnavailable);
        Assert.NotNull(result.SlotBudgets);
        Assert.Null(result.FeasibleLoadout);
    }

    [Fact]
    public void Conditional_requirement_warning_does_not_block_output()
    {
        var attack = CreateSkill(200, SkillCategory.Attack);
        var loadout = CreateLoadout(attack: [attack.SkillId]);
        var result = CombatLoadoutFeasibilityValidator.Validate(
            CreatePlayer([attack]),
            CreateProposal(
                loadout,
                [new CombatSkillCandidate(attack.SkillId)],
                [
                    new WeaponRequirement(
                        weaponTypeId: 10,
                        CombatRequirementCriticality.Conditional,
                        Evidence)
                ]));

        Assert.True(result.IsFeasible);
        Assert.Single(result.RequirementEvaluation.Warnings);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Proposal_rejects_duplicate_candidate_specifications()
    {
        var attack = CreateSkill(200, SkillCategory.Attack);
        var loadout = CreateLoadout(attack: [attack.SkillId]);

        var exception = Assert.Throws<ArgumentException>(
            () => CreateProposal(
                loadout,
                [
                    new CombatSkillCandidate(attack.SkillId),
                    new CombatSkillCandidate(attack.SkillId)
                ]));

        Assert.Contains("Duplicate candidate", exception.Message);
    }

    private static SkillActivationRequirement ActiveDefense(int skillId)
    {
        return new SkillActivationRequirement(
            skillId,
            SkillActivationState.ActiveDefense,
            CombatRequirementCriticality.Hard,
            Evidence);
    }

    private static CombatLoadoutFeasibilityFailure AssertFailure(
        CombatLoadoutFeasibilityResult result,
        CombatLoadoutFeasibilityFailureCode code)
    {
        return Assert.Single(
            result.Failures,
            failure => failure.Code == code);
    }

    private static ProposedCombatLoadout CreateProposal(
        CombatLoadoutSnapshot loadout,
        CombatSkillCandidate[] candidates,
        CombatRequirement[]? requirements = null,
        CombatRequirementContext? context = null,
        GenericSlotAllocation? allocation = null)
    {
        var selectedSkillIds = Enum
            .GetValues<SkillCategory>()
            .SelectMany(category => loadout.Get(category))
            .ToArray();
        return new ProposedCombatLoadout(
            loadout,
            allocation ?? new GenericSlotAllocation(0, 0, 0, 0, 0),
            candidates,
            requirements ?? [],
            context ?? CreateContext(selectedSkillIds));
    }

    private static CombatRequirementContext CreateContext(
        int[] equippedSkillIds,
        int? activeDefenseSkillId = null)
    {
        return new CombatRequirementContext(
            equippedWeaponTypeIds: [],
            trickCounts: [],
            SnapshotValue<int>.Available(0),
            resources: [],
            unlockedWeaponTypeIds: [],
            equippedSkillIds,
            activeDefenseSkillId);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId,
        SkillCategory category,
        SnapshotValue<int>? cost = null,
        PracticeDirection direction = PracticeDirection.Neutral,
        SkillSlotContribution? contribution = null)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            cost ?? SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(direction),
            contribution ?? SkillSlotContribution.None,
            SnapshotValue<int>.Available(1000),
            SnapshotValue<int>.Available(1001));
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

    private static PlayerCombatSnapshot CreatePlayer(
        CombatSkillSnapshot[] skills,
        CombatLoadoutSnapshot? loadout = null,
        SlotBudgetSet? slotBudgets = null)
    {
        return new PlayerCombatSnapshot(
            characterId: 1,
            SnapshotValue<string>.Available("Taiwu"),
            skills,
            loadout ?? CreateLoadout(),
            equipment: [],
            slotBudgets ?? CreateSlotBudgets(),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: []);
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
