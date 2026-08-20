using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class TacticalCombatContractsTests
{
    [Fact]
    public void Aggregate_copies_and_canonically_orders_every_collection()
    {
        var parts = Fixture.CreateParts();
        var facts = parts.Facts.ToList();
        var plan = Fixture.CreatePlan(parts, reverse: true);

        facts.Clear();

        Assert.Equal(
            plan.Facts.OrderBy(item =>
                $"{item.Identity.Kind}:{item.Identity.Code}"),
            plan.Facts);
        Assert.Equal(
            Enum.GetValues<TacticalPlanStage>(),
            plan.Stages.Select(item => item.Stage));
        Assert.Equal(2, plan.Facts.Length);
        Assert.Equal(64, plan.Fingerprint.Length);
        Assert.All(
            plan.Fingerprint,
            character => Assert.True(char.IsAsciiHexDigit(character)));
    }

    [Fact]
    public void Equivalent_input_order_has_one_semantic_fingerprint()
    {
        var normal = Fixture.CreatePlan();
        var reversed = Fixture.CreatePlan(reverse: true);

        Assert.Equal(normal.Fingerprint, reversed.Fingerprint);
        Assert.Equal(
            normal.Stages.SelectMany(item => item.Steps)
                .Select(item => item.Identity),
            reversed.Stages.SelectMany(item => item.Steps)
                .Select(item => item.Identity));
    }

    [Fact]
    public void Identities_and_fact_values_use_semantic_value_equality()
    {
        Assert.Equal(
            new TacticalFactIdentity(
                TacticalFactKind.Mark,
                "DISTRACTION_MARK"),
            new TacticalFactIdentity(
                TacticalFactKind.Mark,
                "DISTRACTION_MARK"));
        Assert.Equal(
            TacticalFactValue.Integer(3),
            TacticalFactValue.Integer(3));
        Assert.NotEqual(
            TacticalFactValue.Integer(3),
            TacticalFactValue.Code("THREE"));
    }

    [Fact]
    public void Elapsed_time_and_cache_counts_are_diagnostics_not_identity()
    {
        var first = Fixture.CreatePlan(
            elapsed: TimeSpan.FromMilliseconds(10),
            caches: [new("TACTICAL_ROLE_CACHE", 0, 1)]);
        var second = Fixture.CreatePlan(
            elapsed: TimeSpan.FromSeconds(2),
            caches: [new("TACTICAL_ROLE_CACHE", 9, 0)]);

        Assert.NotEqual(
            first.SearchCoverage.Elapsed,
            second.SearchCoverage.Elapsed);
        Assert.NotEqual(
            first.SearchCoverage.Caches[0].HitCount,
            second.SearchCoverage.Caches[0].HitCount);
        Assert.Equal(
            first.SearchCoverage.Fingerprint,
            second.SearchCoverage.Fingerprint);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void A_semantic_fact_change_changes_the_fingerprint()
    {
        var original = Fixture.CreatePlan();
        var parts = Fixture.CreateParts();
        var changedFact = new TacticalStateFact(
            parts.CastFact.Identity,
            TacticalEvidenceState.Available,
            TacticalFactValue.Boolean(false),
            "CONFIRMED_FALSE",
            [parts.Evidence]);
        var changed = Fixture.CreatePlan(parts with
        {
            Facts = [changedFact, parts.ReadinessFact]
        });

        Assert.NotEqual(original.Fingerprint, changed.Fingerprint);
    }

    [Fact]
    public void Fact_states_preserve_available_incomplete_unsupported_and_conflict()
    {
        var evidence = Fixture.Evidence();
        var other = new TacticalEvidenceReference(
            TacticalEvidenceSourceKind.ConfirmedObservation,
            "E8-OBSERVATION",
            Fixture.GameDataVersion,
            Fixture.RuleVersion,
            "TARGET_STATE");
        var identity = new TacticalFactIdentity(
            TacticalFactKind.Mark,
            "DISTRACTION_MARK");

        var available = new TacticalStateFact(
            identity,
            TacticalEvidenceState.Available,
            TacticalFactValue.Integer(1),
            "SAVE_VALUE_AVAILABLE",
            [evidence]);
        var incomplete = new TacticalStateFact(
            identity,
            TacticalEvidenceState.Incomplete,
            null,
            "LIVE_VALUE_MISSING",
            [evidence]);
        var unsupported = new TacticalStateFact(
            identity,
            TacticalEvidenceState.Unsupported,
            null,
            "VERSION_UNSUPPORTED",
            [evidence]);
        var conflicting = new TacticalStateFact(
            identity,
            TacticalEvidenceState.Conflicting,
            null,
            "SOURCES_CONFLICT",
            [evidence, other],
            [
                new(TacticalFactValue.Integer(1), evidence),
                new(TacticalFactValue.Integer(2), other)
            ]);

        Assert.Equal(TacticalFactValue.Integer(1), available.Value);
        Assert.Null(incomplete.Value);
        Assert.Null(unsupported.Value);
        Assert.Equal(2, conflicting.Conflicts.Length);
        Assert.Throws<ArgumentException>(() => new TacticalStateFact(
            identity,
            TacticalEvidenceState.Conflicting,
            null,
            "SOURCES_CONFLICT",
            [evidence, other],
            [
                new(TacticalFactValue.Integer(1), evidence),
                new(TacticalFactValue.Integer(1), other)
            ]));
    }

    [Theory]
    [InlineData(TacticalRequirementOutcome.Satisfied)]
    [InlineData(TacticalRequirementOutcome.Unsatisfied)]
    [InlineData(TacticalRequirementOutcome.Unknown)]
    [InlineData(TacticalRequirementOutcome.Unsupported)]
    [InlineData(TacticalRequirementOutcome.Conflicting)]
    public void Requirement_outcomes_are_distinct(
        TacticalRequirementOutcome outcome)
    {
        var evidence = Fixture.Evidence();
        var values = outcome == TacticalRequirementOutcome.Conflicting
            ? new[]
            {
                evidence,
                new TacticalEvidenceReference(
                    TacticalEvidenceSourceKind.ConfirmedObservation,
                    "E8-OBSERVATION",
                    Fixture.GameDataVersion,
                    Fixture.RuleVersion,
                    "PLAYER_STATE")
            }
            : [evidence];

        var evaluation = new TacticalRequirementEvaluation(
            new TacticalRequirementIdentity("EXACT_DIRECTION_READY"),
            outcome,
            $"OUTCOME_{outcome.ToString().ToUpperInvariant()}",
            values);

        Assert.Equal(outcome, evaluation.Outcome);
    }

    [Fact]
    public void Transition_separates_conditions_results_timing_and_purpose()
    {
        var parts = Fixture.CreateParts();
        var transition = parts.SuppressTransition;

        Assert.Equal(
            [parts.CastRequirement.Identity],
            transition.Preconditions);
        Assert.Equal(
            [parts.ReadinessFact.Identity],
            transition.ResultingFacts);
        Assert.Equal(TacticalTransitionTiming.DuringCast, transition.Timing);
        Assert.Equal("INTERRUPT_DIRECT_CAST", transition.ExpectedPurposeIdentity);
        Assert.Equal("THREE_LAYER_SELF_LOCK", transition.LimitationIdentity);
        Assert.DoesNotContain(
            transition.GetType().GetMethods(),
            method => method.Name.Contains("Advance", StringComparison.Ordinal)
                || method.Name.Contains("Simulate", StringComparison.Ordinal));
    }

    [Fact]
    public void Candidate_decisions_enforce_role_and_requirement_meanings()
    {
        var parts = Fixture.CreateParts();
        var satisfied = parts.SatisfiedDirection;
        var unsatisfied = new TacticalRequirementEvaluation(
            parts.DirectionRequirement.Identity,
            TacticalRequirementOutcome.Unsatisfied,
            "DIRECTION_NOT_READY",
            [parts.Evidence]);
        var unknown = new TacticalRequirementEvaluation(
            parts.DirectionRequirement.Identity,
            TacticalRequirementOutcome.Unknown,
            "DIRECTION_UNKNOWN",
            [parts.Evidence]);
        var role = parts.SuppressRole.Identity;

        var admitted = new TacticalCandidateConsideration(
            new(604, PracticeDirection.Reverse),
            TacticalCandidateDecision.Admitted,
            [role],
            [satisfied],
            "ADMITTED_EXACT_ROLE",
            [parts.Evidence]);
        var rejected = new TacticalCandidateConsideration(
            new(604, PracticeDirection.Reverse),
            TacticalCandidateDecision.Rejected,
            [role],
            [unsatisfied],
            "REJECTED_DIRECTION",
            [parts.Evidence]);
        var unsupported = new TacticalCandidateConsideration(
            new(686, PracticeDirection.Reverse),
            TacticalCandidateDecision.Unsupported,
            [],
            [unknown],
            "ROLE_VERSION_UNSUPPORTED",
            [parts.Evidence]);
        var irrelevant = new TacticalCandidateConsideration(
            new(604, PracticeDirection.Reverse),
            TacticalCandidateDecision.Irrelevant,
            [role],
            [satisfied],
            "NO_APPLICABLE_TRANSITION",
            [parts.Evidence]);
        var dominated = new TacticalCandidateConsideration(
            new(624, PracticeDirection.Reverse),
            TacticalCandidateDecision.Dominated,
            [new TacticalRoleIdentity(
                TacticalRoleKind.Mitigation,
                "REDUCE_ATTACK_POWER")],
            [satisfied],
            "STRICTLY_DOMINATED",
            [parts.Evidence],
            admitted.Identity);

        Assert.Equal(TacticalCandidateDecision.Admitted, admitted.Decision);
        Assert.Equal(TacticalCandidateDecision.Rejected, rejected.Decision);
        Assert.Equal(
            TacticalCandidateDecision.Unsupported,
            unsupported.Decision);
        Assert.Equal(TacticalCandidateDecision.Irrelevant, irrelevant.Decision);
        Assert.Equal(admitted.Identity, dominated.DominatedBy);
        Assert.Throws<ArgumentException>(() =>
            new TacticalCandidateConsideration(
                new(604, PracticeDirection.Reverse),
                TacticalCandidateDecision.Admitted,
                [role],
                [unsatisfied],
                "INVALID_ADMISSION",
                [parts.Evidence]));
    }

    [Fact]
    public void Search_coverage_requires_exact_accounting_and_honest_completion()
    {
        var bounds = Fixture.Bounds();
        var complete = Fixture.Coverage();
        var optionLimited = new TacticalSearchCoverage(
            bounds,
            candidateUniverseCount: 7,
            roleSupportedCount: 6,
            admittedCount: 6,
            rejectedCount: 0,
            unsupportedCount: 1,
            irrelevantCount: 0,
            dominatedCount: 0,
            searchedOptionCount: 5,
            exploredCombinationCount: 2,
            feasibleResultCount: 1,
            retainedResultCount: 1,
            TacticalSearchTerminator.OptionLimit,
            TimeSpan.FromMilliseconds(5));

        Assert.True(complete.IsComplete);
        Assert.False(optionLimited.IsComplete);
        Assert.Equal(
            TacticalSearchTerminator.OptionLimit,
            optionLimited.FirstTerminator);
        Assert.Throws<ArgumentException>(() => new TacticalSearchCoverage(
            bounds,
            candidateUniverseCount: 3,
            roleSupportedCount: 2,
            admittedCount: 1,
            rejectedCount: 0,
            unsupportedCount: 0,
            irrelevantCount: 0,
            dominatedCount: 0,
            searchedOptionCount: 1,
            exploredCombinationCount: 1,
            feasibleResultCount: 1,
            retainedResultCount: 1,
            TacticalSearchTerminator.None,
            TimeSpan.Zero));
    }

    [Fact]
    public void Aggregate_rejects_dangling_references()
    {
        var parts = Fixture.CreateParts();
        var dangling = new TacticalTransition(
            parts.SuppressTransition.Identity,
            [new TacticalRequirementIdentity("MISSING_REQUIREMENT")],
            parts.SuppressTransition.ResultingFacts,
            parts.SuppressTransition.Timing,
            parts.SuppressTransition.ExpectedPurposeIdentity,
            parts.SuppressTransition.LimitationIdentity,
            parts.SuppressTransition.Evidence);

        Assert.Throws<ArgumentException>(() => Fixture.CreatePlan(parts with
        {
            Transitions = [dangling, parts.PrepareTransition]
        }));
    }

    [Fact]
    public void Aggregate_rejects_incompatible_evidence_versions()
    {
        var parts = Fixture.CreateParts();
        var mismatched = new TacticalEvidenceReference(
            TacticalEvidenceSourceKind.VerifiedRule,
            "E8-MISMATCH",
            Fixture.GameDataVersion + ".changed",
            Fixture.RuleVersion,
            "TARGET_CHAIN");

        Assert.Throws<ArgumentException>(() => Fixture.CreatePlan(parts with
        {
            SharedEvidence = [mismatched]
        }));
    }

    [Fact]
    public void Aggregate_rejects_duplicate_identities()
    {
        var parts = Fixture.CreateParts();

        Assert.Throws<ArgumentException>(() => Fixture.CreatePlan(parts with
        {
            Facts = [parts.CastFact, parts.CastFact]
        }));
    }

    [Fact]
    public void Aggregate_rejects_backward_or_cyclic_plan_branches()
    {
        var parts = Fixture.CreateParts();
        var backwardRecovery = Fixture.Step(
            "RECOVER_LOCKOUT",
            TacticalPlanStage.Recovery,
            1,
            parts,
            branches:
            [
                new TacticalPlanBranch(
                    "RETURN_TO_RESPONSE",
                    TacticalBranchOutcome.Continue,
                    new TacticalPlanStepIdentity("RESPOND_TO_CAST"))
            ],
            transition: parts.SuppressTransition.Identity);
        var stages = parts.Stages
            .Select(stage => stage.Stage == TacticalPlanStage.Recovery
                ? new TacticalPlanStageDefinition(
                    stage.Stage,
                    TacticalPlanStageState.Supported,
                    "RECOVERY_AVAILABLE",
                    [backwardRecovery],
                    [parts.Evidence])
                : stage)
            .ToArray();

        Assert.Throws<ArgumentException>(() => Fixture.CreatePlan(parts with
        {
            Stages = stages
        }));
    }

    [Fact]
    public void Branch_outcomes_require_their_exact_target_shape()
    {
        var target = new TacticalPlanStepIdentity("NEXT_STEP");

        Assert.NotNull(new TacticalPlanBranch(
            "CONTINUE_READY",
            TacticalBranchOutcome.Continue,
            target).TargetStep);
        Assert.Null(new TacticalPlanBranch(
            "VALUE_UNKNOWN",
            TacticalBranchOutcome.Unresolved).TargetStep);
        Assert.Throws<ArgumentException>(() => new TacticalPlanBranch(
            "INVALID_CONTINUE",
            TacticalBranchOutcome.Continue));
        Assert.Throws<ArgumentException>(() => new TacticalPlanBranch(
            "INVALID_STOP",
            TacticalBranchOutcome.Stop,
            target));
    }

    [Fact]
    public void Aggregate_rejects_missing_or_impossible_stage_sets()
    {
        var parts = Fixture.CreateParts();

        Assert.Throws<ArgumentException>(() => Fixture.CreatePlan(parts with
        {
            Stages = parts.Stages.Where(item =>
                item.Stage != TacticalPlanStage.Finish).ToArray()
        }));
        Assert.Throws<ArgumentException>(() =>
            new TacticalPlanStageDefinition(
                TacticalPlanStage.Finish,
                TacticalPlanStageState.Unsupported,
                "FINISH_UNSUPPORTED",
                [parts.Stages[0].Steps[0]],
                [parts.Evidence]));
    }

    [Fact]
    public void Domain_contract_exposes_no_runtime_or_presentation_types()
    {
        var domainAssembly = typeof(TacticalCombatPlan).Assembly;
        var tacticalTypes = domainAssembly.GetTypes()
            .Where(type => type.Namespace == "TaiWu.Domain.TacticalCombat")
            .ToArray();

        Assert.NotEmpty(tacticalTypes);
        Assert.DoesNotContain(
            tacticalTypes.SelectMany(type => type.GetProperties()),
            property => property.PropertyType == typeof(DateTime)
                || property.PropertyType == typeof(DateTimeOffset)
                || property.Name.Contains("Display", StringComparison.Ordinal)
                || property.PropertyType.Namespace?.StartsWith(
                    "GameData",
                    StringComparison.Ordinal) == true);
    }

    private static class Fixture
    {
        internal const string GameDataVersion =
            "1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a";
        internal const string RuleVersion = "TACTICAL_MAGIC_SOUND_V1";

        internal static TacticalEvidenceReference Evidence() => new(
            TacticalEvidenceSourceKind.VerifiedRule,
            "E8-000-HISTORICAL",
            GameDataVersion,
            RuleVersion,
            "TARGET_CHAIN");

        internal static TacticalSearchBounds Bounds() =>
            new(5, 10, TimeSpan.FromSeconds(5), 5);

        internal static TacticalSearchCoverage Coverage(
            TimeSpan? elapsed = null,
            IEnumerable<TacticalCacheReuseDiagnostic>? caches = null) => new(
                Bounds(),
                candidateUniverseCount: 2,
                roleSupportedCount: 1,
                admittedCount: 1,
                rejectedCount: 0,
                unsupportedCount: 1,
                irrelevantCount: 0,
                dominatedCount: 0,
                searchedOptionCount: 1,
                exploredCombinationCount: 1,
                feasibleResultCount: 1,
                retainedResultCount: 1,
                TacticalSearchTerminator.None,
                elapsed ?? TimeSpan.FromMilliseconds(25),
                caches);

        internal static TacticalCombatPlan CreatePlan(
            bool reverse = false,
            TimeSpan? elapsed = null,
            IEnumerable<TacticalCacheReuseDiagnostic>? caches = null) =>
            CreatePlan(CreateParts(), reverse, elapsed, caches);

        internal static TacticalCombatPlan CreatePlan(
            Parts parts,
            bool reverse = false,
            TimeSpan? elapsed = null,
            IEnumerable<TacticalCacheReuseDiagnostic>? caches = null)
        {
            return new TacticalCombatPlan(
                GameDataVersion,
                RuleVersion,
                Reverse(parts.SharedEvidence, reverse),
                Reverse(parts.Facts, reverse),
                Reverse(parts.Requirements, reverse),
                Reverse(parts.Transitions, reverse),
                Reverse(parts.Roles, reverse),
                Reverse(parts.Candidates, reverse),
                Coverage(elapsed, caches),
                Reverse(parts.Stages, reverse));
        }

        internal static Parts CreateParts()
        {
            var evidence = Evidence();
            var castFact = new TacticalStateFact(
                new TacticalFactIdentity(
                    TacticalFactKind.TargetSkillPhase,
                    "DIRECT_MAGIC_CAST_ACTIVE"),
                TacticalEvidenceState.Available,
                TacticalFactValue.Boolean(true),
                "HISTORICAL_RULE_CONFIRMED",
                [evidence]);
            var readinessFact = new TacticalStateFact(
                new TacticalFactIdentity(
                    TacticalFactKind.PlayerReadiness,
                    "REVERSE_604_READY"),
                TacticalEvidenceState.Available,
                TacticalFactValue.Boolean(true),
                "SYNTHETIC_CONTEXT_READY",
                [evidence]);
            var castRequirement = new TacticalRequirementDefinition(
                new TacticalRequirementIdentity("DIRECT_CAST_OBSERVED"),
                castFact.Identity,
                TacticalRequirementOperator.Equal,
                TacticalFactValue.Boolean(true));
            var directionRequirement = new TacticalRequirementDefinition(
                new TacticalRequirementIdentity("EXACT_DIRECTION_READY"),
                readinessFact.Identity,
                TacticalRequirementOperator.Equal,
                TacticalFactValue.Boolean(true));
            var suppressTransition = new TacticalTransition(
                new TacticalTransitionIdentity("INTERRUPT_DIRECT_CAST"),
                [castRequirement.Identity],
                [readinessFact.Identity],
                TacticalTransitionTiming.DuringCast,
                "INTERRUPT_DIRECT_CAST",
                "THREE_LAYER_SELF_LOCK",
                [evidence]);
            var prepareTransition = new TacticalTransition(
                new TacticalTransitionIdentity("CONFIRM_REVERSE_DIRECTION"),
                [directionRequirement.Identity],
                [readinessFact.Identity],
                TacticalTransitionTiming.BeforeCombat,
                "CONFIRM_EXACT_DIRECTION",
                "NO_GAME_CHANGE_PERFORMED",
                [evidence]);
            var suppressRole = new TacticalSkillRole(
                new TacticalRoleIdentity(
                    TacticalRoleKind.Suppression,
                    "REVERSE_604_SUPPRESSION"),
                604,
                PracticeDirection.Reverse,
                1064,
                TacticalTransitionTiming.DuringCast,
                [suppressTransition.Identity],
                [directionRequirement.Identity],
                "THREE_LAYER_SELF_LOCK",
                [evidence]);
            var satisfiedDirection = new TacticalRequirementEvaluation(
                directionRequirement.Identity,
                TacticalRequirementOutcome.Satisfied,
                "EXACT_DIRECTION_CONFIRMED",
                [evidence]);
            var admitted = new TacticalCandidateConsideration(
                new TacticalCandidateIdentity(604, PracticeDirection.Reverse),
                TacticalCandidateDecision.Admitted,
                [suppressRole.Identity],
                [satisfiedDirection],
                "ADMITTED_EXACT_ROLE",
                [evidence]);
            var unsupported = new TacticalCandidateConsideration(
                new TacticalCandidateIdentity(686, PracticeDirection.Reverse),
                TacticalCandidateDecision.Unsupported,
                [],
                [],
                "ROLE_NOT_APPROVED_FOR_RESULT",
                [evidence]);

            var parts = new Parts(
                evidence,
                castFact,
                readinessFact,
                castRequirement,
                directionRequirement,
                suppressTransition,
                prepareTransition,
                suppressRole,
                satisfiedDirection,
                [evidence],
                [castFact, readinessFact],
                [castRequirement, directionRequirement],
                [suppressTransition, prepareTransition],
                [suppressRole],
                [admitted, unsupported],
                []);

            return parts with { Stages = CreateStages(parts) };
        }

        internal static TacticalPlanStep Step(
            string code,
            TacticalPlanStage stage,
            int order,
            Parts parts,
            IEnumerable<TacticalPlanBranch> branches,
            TacticalTransitionIdentity transition,
            TacticalStepBranchKind? branchKind = null) => new(
                new TacticalPlanStepIdentity(code),
                stage,
                order,
                branchKind ?? (stage == TacticalPlanStage.Fallback
                    ? TacticalStepBranchKind.Fallback
                    : TacticalStepBranchKind.Conditional),
                [parts.CastFact.Identity],
                [parts.SatisfiedDirection],
                [transition],
                $"ACTION_{code}",
                $"PURPOSE_{code}",
                "HISTORICAL_VERSION_ONLY",
                branches,
                [parts.Evidence]);

        private static TacticalPlanStageDefinition[] CreateStages(Parts parts)
        {
            var fallback = Step(
                "USE_VERIFIED_FALLBACK",
                TacticalPlanStage.Fallback,
                1,
                parts,
                [new TacticalPlanBranch(
                    "FALLBACK_COMPLETE",
                    TacticalBranchOutcome.Stop)],
                parts.PrepareTransition.Identity);
            var recovery = Step(
                "RECOVER_LOCKOUT",
                TacticalPlanStage.Recovery,
                1,
                parts,
                [new TacticalPlanBranch(
                    "RECOVERY_COMPLETE",
                    TacticalBranchOutcome.Stop)],
                parts.PrepareTransition.Identity);
            var response = Step(
                "RESPOND_TO_CAST",
                TacticalPlanStage.TargetStateResponse,
                1,
                parts,
                [
                    new TacticalPlanBranch(
                        "SUPPRESSION_APPLIED",
                        TacticalBranchOutcome.Continue,
                        recovery.Identity),
                    new TacticalPlanBranch(
                        "SUPPRESSION_UNAVAILABLE",
                        TacticalBranchOutcome.Fallback,
                        fallback.Identity)
                ],
                parts.SuppressTransition.Identity);
            var preparation = Step(
                "CONFIRM_PREPARATION",
                TacticalPlanStage.Preparation,
                1,
                parts,
                [new TacticalPlanBranch(
                    "PREPARATION_CONFIRMED",
                    TacticalBranchOutcome.Continue,
                    response.Identity)],
                parts.PrepareTransition.Identity,
                TacticalStepBranchKind.Primary);

            return
            [
                new(
                    TacticalPlanStage.Preparation,
                    TacticalPlanStageState.Supported,
                    "PREPARATION_AVAILABLE",
                    [preparation],
                    [parts.Evidence]),
                new(
                    TacticalPlanStage.Opening,
                    TacticalPlanStageState.Omitted,
                    "NO_VERIFIED_OPENING_ACTION",
                    [],
                    [parts.Evidence]),
                new(
                    TacticalPlanStage.TargetStateResponse,
                    TacticalPlanStageState.Supported,
                    "RESPONSE_AVAILABLE",
                    [response],
                    [parts.Evidence]),
                new(
                    TacticalPlanStage.Recovery,
                    TacticalPlanStageState.Supported,
                    "RECOVERY_CONDITIONAL",
                    [recovery],
                    [parts.Evidence]),
                new(
                    TacticalPlanStage.Finish,
                    TacticalPlanStageState.Unsupported,
                    "FINISH_EVIDENCE_UNAVAILABLE",
                    [],
                    [parts.Evidence]),
                new(
                    TacticalPlanStage.Fallback,
                    TacticalPlanStageState.Supported,
                    "FALLBACK_ONLY",
                    [fallback],
                    [parts.Evidence])
            ];
        }

        private static IEnumerable<T> Reverse<T>(
            IEnumerable<T> values,
            bool reverse) => reverse ? values.Reverse() : values;

        internal sealed record Parts(
            TacticalEvidenceReference Evidence,
            TacticalStateFact CastFact,
            TacticalStateFact ReadinessFact,
            TacticalRequirementDefinition CastRequirement,
            TacticalRequirementDefinition DirectionRequirement,
            TacticalTransition SuppressTransition,
            TacticalTransition PrepareTransition,
            TacticalSkillRole SuppressRole,
            TacticalRequirementEvaluation SatisfiedDirection,
            IReadOnlyList<TacticalEvidenceReference> SharedEvidence,
            IReadOnlyList<TacticalStateFact> Facts,
            IReadOnlyList<TacticalRequirementDefinition> Requirements,
            IReadOnlyList<TacticalTransition> Transitions,
            IReadOnlyList<TacticalSkillRole> Roles,
            IReadOnlyList<TacticalCandidateConsideration> Candidates,
            IReadOnlyList<TacticalPlanStageDefinition> Stages);
    }
}
