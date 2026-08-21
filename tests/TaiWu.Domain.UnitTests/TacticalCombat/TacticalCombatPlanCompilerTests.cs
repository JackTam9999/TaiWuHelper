using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class TacticalCombatPlanCompilerTests
{
    [Fact]
    public void Compile_builds_six_stage_conditional_fallback_only_plan()
    {
        var fixture = CreateFixture(
            observations: [Observable("SYNTHETIC_SUPPRESS")]);

        var compiled = Compile(fixture, 604, 624, 686);

        Assert.Equal(TacticalFinishDisposition.FallbackOnly,
            compiled.FinishDisposition);
        Assert.Equal(Enum.GetValues<TacticalPlanStage>(),
            compiled.Plan.Stages.Select(item => item.Stage));
        Assert.Equal(TacticalPlanStageState.Supported,
            Stage(compiled, TacticalPlanStage.Preparation).State);
        Assert.Equal(TacticalPlanStageState.Supported,
            Stage(compiled, TacticalPlanStage.Opening).State);
        Assert.Equal(TacticalPlanStageState.Supported,
            Stage(compiled, TacticalPlanStage.TargetStateResponse).State);
        Assert.Equal(TacticalPlanStageState.Unsupported,
            Stage(compiled, TacticalPlanStage.Finish).State);
        Assert.Empty(Stage(compiled, TacticalPlanStage.Finish).Steps);
        Assert.Equal(TacticalPlanStageState.Supported,
            Stage(compiled, TacticalPlanStage.Fallback).State);

        var suppression = Assert.Single(
            Stage(compiled, TacticalPlanStage.TargetStateResponse).Steps,
            item => item.ManualActionIdentity.Contains(
                "SKILL_604",
                StringComparison.Ordinal));
        Assert.Contains(suppression.Requirements, item =>
            item.Outcome == TacticalRequirementOutcome.Satisfied);
        Assert.Contains(suppression.Branches, item =>
            item.Outcome == TacticalBranchOutcome.Fallback);
        Assert.All(compiled.Plan.Stages.SelectMany(item => item.Steps), step =>
        {
            Assert.NotEmpty(step.ObservedFacts);
            Assert.NotEmpty(step.Transitions);
            Assert.NotEmpty(step.Branches);
            Assert.NotEmpty(step.Evidence);
        });
    }

    [Fact]
    public void Compile_keeps_unavailable_trigger_unknown_and_branches_to_fallback()
    {
        var fixture = CreateFixture(
            observations:
            [
                Observable(
                    "SYNTHETIC_SUPPRESS",
                    TacticalEvidenceState.Incomplete)
            ]);

        var compiled = Compile(fixture, 604, 624);
        var suppression = Assert.Single(
            Stage(compiled, TacticalPlanStage.TargetStateResponse).Steps,
            item => item.ManualActionIdentity.Contains(
                "SKILL_604",
                StringComparison.Ordinal));

        Assert.Contains(suppression.Requirements, item =>
            item.Outcome == TacticalRequirementOutcome.Unknown);
        Assert.Contains(suppression.Branches, item =>
            item.Outcome == TacticalBranchOutcome.Fallback
            && item.TargetStep is not null);
        Assert.Contains(compiled.Plan.Facts, item =>
            item.Identity.Code == "SYNTHETIC_SUPPRESS_TRIGGER"
            && item.State == TacticalEvidenceState.Incomplete
            && item.Value is null);
    }

    [Fact]
    public void Compile_emits_three_ordered_recovery_casts_only_when_preselected()
    {
        var fixture = CreateFixture();

        var compiled = Compile(fixture, 604, 624, 611);
        var recovery = Stage(compiled, TacticalPlanStage.Recovery);

        Assert.Equal(TacticalPlanStageState.Supported, recovery.State);
        Assert.Equal([1, 2, 3], recovery.Steps.Select(item => item.Order));
        Assert.Equal(
            [604, 611, 624],
            recovery.Steps.Select(item => item.SkillId!.Value)
                .Order());
        Assert.All(recovery.Steps, item => Assert.Equal(
            "SYNTHETIC_RECOVERY",
            Assert.Single(item.Transitions).Code));
    }

    [Fact]
    public void Compile_exposes_unsupported_recovery_finish_and_fallback_without_actions()
    {
        var fixture = CreateFixture();

        var compiled = Compile(fixture, 604);

        Assert.Equal(TacticalFinishDisposition.Unsupported,
            compiled.FinishDisposition);
        AssertUnsupported(compiled, TacticalPlanStage.Recovery);
        AssertUnsupported(compiled, TacticalPlanStage.Finish);
        AssertUnsupported(compiled, TacticalPlanStage.Fallback);
        Assert.DoesNotContain(compiled.Plan.Stages.SelectMany(item => item.Steps),
            item => item.ManualActionIdentity.Contains(
                "FINISH",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Preparation_checks_match_selected_proposal_and_direction_change()
    {
        var fixture = CreateFixture(requiresDirectionChange: true);

        var compiled = Compile(fixture, 604, 624);
        var proposed = compiled.SelectedLoadout.Candidate.Loadout.Proposal;
        var added = compiled.PreparationChecks
            .Where(item => item.Kind == TacticalPreparationCheckKind.AddSkill)
            .Select(item => item.SkillId!.Value)
            .Order()
            .ToArray();

        Assert.Equal([604, 624], added);
        var direction = Assert.Single(compiled.PreparationChecks, item =>
            item.Kind == TacticalPreparationCheckKind.ChangeDirection);
        Assert.Equal(604, direction.SkillId);
        Assert.Equal(PracticeDirection.Reverse, direction.Direction);
        Assert.Equal(
            proposed.Skills.AttackSkillIds.Order(),
            added.Order());
        Assert.Equal(
            compiled.PreparationChecks.Length,
            Stage(compiled, TacticalPlanStage.Preparation).Steps.Length);
        Assert.All(
            Stage(compiled, TacticalPlanStage.Preparation).Steps,
            step => Assert.Equal(
                compiled.PreparationChecks.Single(check =>
                    step.Identity.Code == $"STEP_{check.Identity}").SkillId,
                step.SkillId));
    }

    [Fact]
    public void Compile_supports_finish_only_with_exact_typed_proof()
    {
        var baseFixture = CreateFixture();
        var proof = FinishProof(baseFixture);
        var fixture = CreateFixture(finishProofs: [proof]);

        var compiled = Compile(fixture, 267, 291, 686);
        var finish = Stage(compiled, TacticalPlanStage.Finish);

        Assert.Equal(TacticalFinishDisposition.Supported,
            compiled.FinishDisposition);
        Assert.Equal(TacticalPlanStageState.Supported, finish.State);
        var step = Assert.Single(finish.Steps);
        Assert.Contains("SKILL_267", step.ManualActionIdentity,
            StringComparison.Ordinal);
        Assert.Contains(step.Transitions, item =>
            item.Code == "SYNTHETIC_FINISH");
    }

    [Fact]
    public void Observation_replacement_changes_the_coherent_result_atomically()
    {
        var clearedFixture = CreateFixture();
        var appliedFixture = CreateFixture(
            observations: [Observable("SYNTHETIC_SUPPRESS")]);
        var cleared = Compile(clearedFixture, 604, 624);
        var clearedFingerprint = cleared.SemanticFingerprint;

        var applied = Compile(appliedFixture, 604, 624);

        Assert.NotEqual(cleared.ObservationRevisionFingerprint,
            applied.ObservationRevisionFingerprint);
        Assert.NotEqual(cleared.ScoringSemanticFingerprint,
            applied.ScoringSemanticFingerprint);
        Assert.NotEqual(cleared.Plan.Fingerprint, applied.Plan.Fingerprint);
        Assert.NotEqual(cleared.SemanticFingerprint,
            applied.SemanticFingerprint);
        Assert.Equal(clearedFingerprint, cleared.SemanticFingerprint);
        Assert.NotSame(cleared.Plan, applied.Plan);
    }

    [Fact]
    public void Compile_is_deterministic_when_source_order_changes()
    {
        var first = Compile(CreateFixture(), 604, 624, 686);
        var second = Compile(CreateFixture(reverseSourceOrder: true),
            604,
            624,
            686);

        Assert.Equal(first.SelectedLoadoutFingerprint,
            second.SelectedLoadoutFingerprint);
        Assert.Equal(first.Plan.Fingerprint, second.Plan.Fingerprint);
        Assert.Equal(first.SemanticFingerprint, second.SemanticFingerprint);
    }

    [Fact]
    public void Compile_honors_precancelled_requests()
    {
        var fixture = CreateFixture();
        var selected = Select(fixture, 604, 624);
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            TacticalCombatPlanCompiler.Compile(
                new TacticalPlanCompilationRequest(
                    fixture.ScoringRequest,
                    fixture.ScoringResult,
                    selected.Candidate.StableKey),
                source.Token));
    }

    private static TacticalPlanStageDefinition Stage(
        TacticalCompiledCombatPlan result,
        TacticalPlanStage stage) => result.Plan.Stages.Single(item =>
            item.Stage == stage);

    private static void AssertUnsupported(
        TacticalCompiledCombatPlan result,
        TacticalPlanStage stage)
    {
        var value = Stage(result, stage);
        Assert.Equal(TacticalPlanStageState.Unsupported, value.State);
        Assert.Empty(value.Steps);
    }

    private static TacticalCompiledCombatPlan Compile(
        FixtureData fixture,
        params int[] selectedSkillIds)
    {
        var selected = Select(fixture, selectedSkillIds);
        return TacticalCombatPlanCompiler.Compile(
            new TacticalPlanCompilationRequest(
                fixture.ScoringRequest,
                fixture.ScoringResult,
                selected.Candidate.StableKey),
            TestContext.Current.CancellationToken);
    }

    private static TacticalScoredLoadout Select(
        FixtureData fixture,
        params int[] selectedSkillIds)
    {
        var expected = selectedSkillIds.ToHashSet();
        return Assert.Single(fixture.ScoringResult.RankedCandidates, item =>
            item.Candidate.SelectedCandidates.Select(value => value.SkillId)
                .ToHashSet().SetEquals(expected));
    }

    private static FixtureData CreateFixture(
        IEnumerable<TacticalTriggerObservability>? observations = null,
        IEnumerable<TacticalFinishPathProof>? finishProofs = null,
        bool requiresDirectionChange = false,
        bool reverseSourceOrder = false)
    {
        var rules = Rules(reverseSourceOrder);
        var skills = rules.Roles.Select(item => Skill(
                item,
                requiresDirectionChange && item.SkillId == 604))
            .DistinctBy(item => item.SkillId)
            .ToArray();
        if (reverseSourceOrder)
        {
            skills = [.. skills.Reverse()];
        }

        var budgets = Budgets();
        var player = new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("display-only player"),
            skills,
            new CombatLoadoutSnapshot([], [], [], [], []),
            equipment: [],
            budgets,
            new GenericSlotAllocation(10, 10, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: [],
            SnapshotValue<InnerPowerStateSnapshot>.Available(
                new InnerPowerStateSnapshot(
                    1,
                    SnapshotValue<string>.Available("display-only inner"),
                    SnapshotValue<string>.Available("raw description"),
                    ElementAdjustmentSet.None,
                    ElementAdjustmentSet.None,
                    CombatSkillElement.Fire)));
        var snapshot = new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('E', 64),
                DateTimeOffset.Parse("2026-08-20T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-20T11:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion)),
            player,
            new TargetCombatSnapshot(
                2,
                SnapshotValue<string>.Unavailable("Not required."),
                SnapshotValue<int>.Unavailable("Not required."),
                features: [],
                learnedSkills: [],
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "Not required."),
                equipment: []),
            warnings: []);
        var resolution = rules.Resolve(
            VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
            ["SYNTHETIC_GOAL"],
            ConfirmedEvidence(rules));
        var proposal = new TacticalExecutionProposal(
            new CombatRequirementContext(
                equippedWeaponTypeIds: [],
                trickCounts: [],
                SnapshotValue<int>.Available(5),
                resources: [],
                unlockedWeaponTypeIds: [],
                equippedSkillIds: skills.Select(item => item.SkillId)),
            budgets,
            new GenericSlotAllocation(10, 10, 0, 0, 0),
            legendaryCostAssignments: []);
        var context = TacticalExecutionContextProjector.Project(
            snapshot,
            resolution,
            proposal);
        var discovery = TacticalCandidateDiscovery.Discover(
            player,
            context,
            resolution);
        var searchRequest = new TacticalLoadoutSearchRequest(
            player,
            context,
            resolution,
            discovery,
            new TacticalSearchBounds(
                maximumOptions: 20,
                maximumExploredCombinations: 10_000,
                maximumElapsed: TimeSpan.FromMinutes(1),
                maximumResults: 10_000));
        var searchResult = TacticalLoadoutSearch.Search(
            searchRequest,
            new ZeroElapsedTimeProvider(),
            TestContext.Current.CancellationToken);
        var scoringRequest = new TacticalCombatScoringRequest(
            RecommendationPolicy.Balanced,
            searchRequest,
            searchResult,
            triggerObservations: observations,
            finishProofs: finishProofs);
        var scoringResult = TacticalCombatScorer.Score(
            scoringRequest,
            TestContext.Current.CancellationToken);
        return new FixtureData(
            context,
            discovery,
            scoringRequest,
            scoringResult);
    }

    private static TacticalCombatRuleSet Rules(bool reverse)
    {
        var transitions = new[]
        {
            Transition(
                "SYNTHETIC_SUPPRESS",
                TacticalRulePurpose.CastSuppression,
                TacticalTransitionTiming.DuringCast),
            Transition(
                "SYNTHETIC_SELF_LOCK",
                TacticalRulePurpose.DirectPracticeSelfLock,
                TacticalTransitionTiming.AfterCast,
                TacticalFactKind.PlayerReadiness,
                TacticalFactKind.TemporaryLockout),
            Transition(
                "SYNTHETIC_RECOVERY",
                TacticalRulePurpose.DirectPracticeLockRecovery,
                TacticalTransitionTiming.AfterManualAction,
                TacticalFactKind.PlayerReadiness,
                TacticalFactKind.TemporaryLockout),
            Transition(
                "SYNTHETIC_OPENING",
                TacticalRulePurpose.HindranceMarkRemoval,
                TacticalTransitionTiming.CombatStart),
            Transition(
                "SYNTHETIC_MITIGATION",
                TacticalRulePurpose.EnemyAttackPowerReduction,
                TacticalTransitionTiming.AfterCast),
            Transition(
                "SYNTHETIC_TRANSFER",
                TacticalRulePurpose.ConditionalMarkTransfer,
                TacticalTransitionTiming.AfterManualAction),
            Transition(
                "SYNTHETIC_CHANNEL",
                TacticalRulePurpose.DamageChannelChoice,
                TacticalTransitionTiming.AfterCast),
            Transition(
                "SYNTHETIC_FINISH",
                TacticalRulePurpose.FinishWindowSupport,
                TacticalTransitionTiming.AfterCast)
        };
        var roles = new[]
        {
            Role(
                604,
                TacticalRoleKind.Suppression,
                "SYNTHETIC_SUPPRESSION_ROLE",
                TacticalRulePurpose.CastSuppression,
                TacticalTransitionTiming.DuringCast,
                transitions[0].Identity,
                transitions[1].Identity),
            Role(
                686,
                TacticalRoleKind.Mitigation,
                "SYNTHETIC_OPENING_ROLE",
                TacticalRulePurpose.HindranceMarkRemoval,
                TacticalTransitionTiming.CombatStart,
                transitions[3].Identity),
            Role(
                624,
                TacticalRoleKind.Mitigation,
                "SYNTHETIC_MITIGATION_ROLE",
                TacticalRulePurpose.EnemyAttackPowerReduction,
                TacticalTransitionTiming.AfterCast,
                transitions[4].Identity),
            Role(
                611,
                TacticalRoleKind.Mitigation,
                "SYNTHETIC_TRANSFER_ROLE",
                TacticalRulePurpose.ConditionalMarkTransfer,
                TacticalTransitionTiming.AfterManualAction,
                transitions[5].Identity),
            Role(
                291,
                TacticalRoleKind.DamageChannel,
                "SYNTHETIC_DAMAGE_CHANNEL_ROLE",
                TacticalRulePurpose.DamageChannelChoice,
                TacticalTransitionTiming.AfterCast,
                transitions[6].Identity),
            Role(
                267,
                TacticalRoleKind.Finish,
                "SYNTHETIC_FINISH_ROLE",
                TacticalRulePurpose.FinishWindowSupport,
                TacticalTransitionTiming.AfterCast,
                transitions[7].Identity)
        };
        return new TacticalCombatRuleSet(
            new TacticalSemanticVersion(1, 0, 0),
            [VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion],
            ["SYNTHETIC_GOAL"],
            reverse ? transitions.Reverse() : transitions,
            reverse ? roles.Reverse() : roles);
    }

    private static TacticalTransitionRule Transition(
        string identity,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        TacticalFactKind triggerKind = TacticalFactKind.TargetSkillPhase,
        TacticalFactKind resultKind = TacticalFactKind.Other) => new(
        new TacticalTransitionIdentity(identity),
        new TacticalSemanticVersion(1, 0, 0),
        [VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion],
        purpose,
        timing,
        [new TacticalFactIdentity(triggerKind, $"{identity}_TRIGGER")],
        [new TacticalFactIdentity(resultKind, $"{identity}_RESULT")],
        ["SYNTHETIC_GOAL"],
        [EvidenceRequirement($"TRANSITION_{identity}")],
        $"LIMITATION_{identity}",
        [Evidence($"RULE_{identity}")]);

    private static TacticalSkillRoleRule Role(
        int skillId,
        TacticalRoleKind kind,
        string identity,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        params TacticalTransitionIdentity[] transitions)
    {
        var source = VerifiedTacticalCombatRuleSets.HistoricalMagicSound.Roles
            .Single(item => item.SkillId == skillId);
        return new TacticalSkillRoleRule(
            new TacticalRoleIdentity(kind, identity),
            new TacticalSemanticVersion(1, 0, 0),
            [VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion],
            purpose,
            timing,
            source.Effect,
            source.RequiredMechanics,
            ["SYNTHETIC_GOAL"],
            transitions,
            [EvidenceRequirement($"ROLE_{identity}")],
            $"LIMITATION_{identity}",
            [Evidence($"ROLE_{identity}")],
            new CombatCounterRule(
                $"COUNTER_{identity}",
                ["SYNTHETIC_GOAL"],
                CombatCounterStrength.Mitigation,
                timing == TacticalTransitionTiming.CombatStart
                    ? CombatCounterActivationTiming.CombatStartPassive
                    : CombatCounterActivationTiming.ActiveAttack,
                source.Effect,
                requirements: [],
                "Synthetic execution-feasibility evidence for plan compiler tests."));
    }

    private static TacticalRuleEvidenceRequirement EvidenceRequirement(
        string identity) => new(
        new TacticalRuleEvidenceIdentity($"EVIDENCE_{identity}"),
        TacticalRuleEvidenceScope.ExactTarget,
        TacticalEvidenceSourceKind.ConfirmedObservation);

    private static TacticalRuleEvidenceObservation[] ConfirmedEvidence(
        TacticalCombatRuleSet rules) => rules.Transitions
        .SelectMany(item => item.EvidenceRequirements)
        .Concat(rules.Roles.SelectMany(item => item.EvidenceRequirements))
        .DistinctBy(item => (item.Identity.Code, item.Scope, item.Source))
        .Select((item, index) => new TacticalRuleEvidenceObservation(
            item.Identity,
            item.Scope,
            item.Source,
            TacticalRuleEvidenceDisposition.Confirmed,
            Evidence($"CONFIRMED_{index:000}")))
        .ToArray();

    private static TacticalTriggerObservability Observable(
        string transition,
        TacticalEvidenceState state = TacticalEvidenceState.Available) => new(
        new TacticalTransitionIdentity(transition),
        state,
        state == TacticalEvidenceState.Available
            ? "TRIGGER_OBSERVABLE"
            : "TRIGGER_REQUIRES_MANUAL_CONFIRMATION",
        [Evidence($"OBSERVATION_{transition}")],
        "OBSERVATION_REQUIRED_AT_EXECUTION");

    private static TacticalFinishPathProof FinishProof(FixtureData fixture) =>
        new(
            Candidate(fixture, 291),
            new TacticalRoleIdentity(
                TacticalRoleKind.DamageChannel,
                "SYNTHETIC_DAMAGE_CHANNEL_ROLE"),
            Candidate(fixture, 267),
            new TacticalRoleIdentity(
                TacticalRoleKind.Finish,
                "SYNTHETIC_FINISH_ROLE"),
            new TacticalTransitionIdentity("SYNTHETIC_FINISH"),
            fixture.Context.SemanticFingerprint,
            [
                FinishInput(
                    TacticalFinishEvidenceKind.AttackChannelStrength,
                    TacticalFactValue.Integer(100)),
                FinishInput(
                    TacticalFinishEvidenceKind.HitOrCastReliabilityPercent,
                    TacticalFactValue.Integer(100)),
                FinishInput(
                    TacticalFinishEvidenceKind.TargetDefenseOrResistance,
                    TacticalFactValue.Integer(0)),
                FinishInput(
                    TacticalFinishEvidenceKind.ApplicableCondition,
                    TacticalFactValue.Boolean(true)),
                FinishInput(
                    TacticalFinishEvidenceKind.FinishWindow,
                    TacticalFactValue.Boolean(true))
            ],
            "SUPPORTED_CHANNEL_NOT_PREDICTED_DAMAGE");

    private static TacticalFinishEvidenceInput FinishInput(
        TacticalFinishEvidenceKind kind,
        TacticalFactValue value) => new(
        kind,
        $"FINISH_{kind.ToString().ToUpperInvariant()}",
        value,
        [Evidence($"FINISH_{kind.ToString().ToUpperInvariant()}")]);

    private static TacticalCandidateIdentity Candidate(
        FixtureData fixture,
        int skillId) => Assert.Single(fixture.Discovery.Entries, item =>
        item.SkillId == skillId && item.IsAdmitted).Consideration.Identity;

    private static CombatSkillSnapshot Skill(
        TacticalSkillRoleRule role,
        bool requiresDirectionChange)
    {
        var currentDirection = requiresDirectionChange
            ? PracticeDirection.Direct
            : role.Direction;
        return new CombatSkillSnapshot(
            role.SkillId,
            SnapshotValue<string>.Available("display-only skill"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(currentDirection),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(
                role.Direction == PracticeDirection.Direct
                    ? role.RawEffectId
                    : 8_000 + role.SkillId),
            SnapshotValue<int>.Available(
                role.Direction == PracticeDirection.Reverse
                    ? role.RawEffectId
                    : 9_000 + role.SkillId),
            requiresDirectionChange
                ? SnapshotValue<BreakthroughDirectionAvailability>.Available(
                    new BreakthroughDirectionAvailability(
                        isBrokenOut: true,
                        canBreakthroughNow: false,
                        availableDirections: [],
                        completedDirections: [PracticeDirection.Reverse]))
                : null,
            SnapshotValue<CombatSkillElement>.Available(
                CombatSkillElement.Water));
    }

    private static SlotBudgetSet Budgets() => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, 30),
        new SlotBudget(SkillCategory.Agility, 0, 8),
        new SlotBudget(SkillCategory.Defense, 0, 8),
        new SlotBudget(SkillCategory.Assistance, 0, 2)
    ]);

    private static TacticalEvidenceReference Evidence(string identity) => new(
        TacticalEvidenceSourceKind.ConfirmedObservation,
        identity,
        VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
        VerifiedTacticalCombatRuleSets.RuleVersion,
        "EXACT_TARGET");

    private sealed record FixtureData(
        TacticalExecutionContext Context,
        TacticalCandidateDiscoveryResult Discovery,
        TacticalCombatScoringRequest ScoringRequest,
        TacticalCombatScoringResult ScoringResult);

    private sealed class ZeroElapsedTimeProvider : TimeProvider
    {
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => 0;
    }
}
