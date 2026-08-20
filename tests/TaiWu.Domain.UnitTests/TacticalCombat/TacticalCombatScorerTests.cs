using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class TacticalCombatScorerTests
{
    private static readonly TacticalCombatRuleSet HistoricalRules =
        VerifiedTacticalCombatRuleSets.HistoricalMagicSound;

    [Fact]
    public void Published_policy_weights_are_stable_and_distinct()
    {
        var safe = TacticalScoringPolicyWeights.For(RecommendationPolicy.Safe);
        var balanced = TacticalScoringPolicyWeights.For(
            RecommendationPolicy.Balanced);
        var aggressive = TacticalScoringPolicyWeights.For(
            RecommendationPolicy.Aggressive);

        Assert.Equal([28, 24, 10, 20, 15, 3], Values(safe));
        Assert.Equal([29, 18, 16, 16, 13, 8], Values(balanced));
        Assert.Equal([28, 10, 24, 12, 8, 18], Values(aggressive));
        Assert.Equal(100, Values(safe).Sum());
        Assert.Equal(100, Values(balanced).Sum());
        Assert.Equal(100, Values(aggressive).Sum());
    }

    [Fact]
    public void Duplicate_transition_coverage_receives_one_causal_reward()
    {
        var fixture = DuplicateTransitionFixture(shuffleSkills: false);

        var result = Score(fixture, RecommendationPolicy.Balanced);

        var first = Candidate(result, "604:REVERSE");
        var second = Candidate(result, "624:REVERSE");
        var combined = Candidate(result, "604:REVERSE+624:REVERSE");
        Assert.Equal(100, Causal(first).NormalizedValue);
        Assert.Equal(100, Causal(second).NormalizedValue);
        Assert.Equal(100, Causal(combined).NormalizedValue);
        Assert.Single(
            Causal(combined).RawInputs,
            item => item.Kind == TacticalScoreInputKind.CoveredTransition);
        Assert.Contains(
            Causal(combined).RawInputs,
            item => item.Kind == TacticalScoreInputKind.CausalTriggerState);
        Assert.Contains(
            Causal(combined).RawInputs,
            item => item.Kind == TacticalScoreInputKind.CausalResultingState);
    }

    [Fact]
    public void Layering_requires_a_documented_interaction_or_fallback_rule()
    {
        var fixture = HistoricalFixture([Skill(604), Skill(624, 1234)]);
        var combinedKey = "604:REVERSE+624:REVERSE";
        var withoutProof = Score(
            fixture,
            RecommendationPolicy.Safe,
            observations: [Observable604()]);
        var proof = new TacticalLayeringProof(
            CandidateIdentity(fixture, 604),
            CandidateIdentity(fixture, 624),
            new TacticalTransitionIdentity(
                "REVERSE_624_REDUCES_ATTACK_POWER"),
            TacticalLayeringKind.SeparateMitigation,
            fixture.Context.SemanticFingerprint,
            [Evidence("LAYERED_MITIGATION")],
            "SEPARATE_ATTACK_POWER_MITIGATION");
        var withProof = Score(
            fixture,
            RecommendationPolicy.Safe,
            layeringProofs: [proof],
            observations: [Observable604()]);

        Assert.Equal(
            0,
            Candidate(withoutProof, combinedKey)
                .Get(TacticalScoreComponentKind.LayeredProtection)
                .NormalizedValue);
        var layered = Candidate(withProof, combinedKey)
            .Get(TacticalScoreComponentKind.LayeredProtection);
        Assert.Equal(50, layered.NormalizedValue);
        Assert.Single(layered.RawInputs);
        Assert.Contains(
            "SEPARATE_ATTACK_POWER_MITIGATION",
            layered.Limitations);

        var wrongContext = new TacticalLayeringProof(
            proof.PrimaryCandidate,
            proof.LayeredCandidate,
            proof.MarginalTransition,
            proof.Kind,
            new string('F', 64),
            proof.Evidence,
            proof.LimitationIdentity);
        Assert.Throws<ArgumentException>(() => Score(
            fixture,
            RecommendationPolicy.Safe,
            layeringProofs: [wrongContext],
            observations: [Observable604()]));
    }

    [Fact]
    public void Unknown_trigger_timing_remains_unavailable()
    {
        var fixture = HistoricalFixture([Skill(604)]);

        var unknown = Score(fixture, RecommendationPolicy.Balanced);
        var known = Score(
            fixture,
            RecommendationPolicy.Balanced,
            observations: [Observable604()]);

        var unknownCandidate = Candidate(unknown, "604:REVERSE");
        Assert.False(
            unknownCandidate.Get(
                TacticalScoreComponentKind.TimingOpportunity).IsAvailable);
        Assert.False(
            unknownCandidate.Get(
                TacticalScoreComponentKind.ExecutionReliability).IsAvailable);
        Assert.Contains(
            unknownCandidate.Get(TacticalScoreComponentKind.TimingOpportunity)
                .RawInputs,
            item => item.Kind
                    == TacticalScoreInputKind.TriggerObservability
                && item.State == TacticalEvidenceState.Incomplete);

        Assert.True(Candidate(known, "604:REVERSE")
            .Get(TacticalScoreComponentKind.TimingOpportunity).IsAvailable);
        Assert.True(Candidate(known, "604:REVERSE")
            .Get(TacticalScoreComponentKind.ExecutionReliability).IsAvailable);
    }

    [Fact]
    public void Verified_self_lock_lowers_recovery_score_and_exposes_route()
    {
        var fixture = HistoricalFixture([Skill(604), Skill(624, 1234)]);

        var result = Score(
            fixture,
            RecommendationPolicy.Safe,
            observations: [Observable604()]);

        var suppression = Candidate(result, "604:REVERSE")
            .Get(TacticalScoreComponentKind.RecoveryCost);
        var mitigation = Candidate(result, "624:REVERSE")
            .Get(TacticalScoreComponentKind.RecoveryCost);
        Assert.Equal(55, suppression.NormalizedValue);
        Assert.Equal(100, mitigation.NormalizedValue);
        Assert.Contains(
            suppression.RawInputs,
            item => item.Kind == TacticalScoreInputKind.SelfLock);
        Assert.Contains(
            suppression.RawInputs,
            item => item.Kind == TacticalScoreInputKind.RecoveryRoute);
    }

    [Fact]
    public void Missing_finish_evidence_is_unavailable_and_weight_is_excluded()
    {
        var fixture = HistoricalFixture([Skill(624, 1234)]);

        var result = Score(fixture, RecommendationPolicy.Aggressive);
        var candidate = Candidate(result, "624:REVERSE");
        var finish = candidate.Get(TacticalScoreComponentKind.FinishPath);

        Assert.False(finish.IsAvailable);
        Assert.Null(finish.NormalizedValue);
        Assert.Null(finish.AppliedWeight);
        Assert.Null(finish.Contribution);
        Assert.Equal(18, finish.BaseWeight);
        Assert.Equal(5, finish.RawInputs.Length);
        Assert.All(
            finish.RawInputs,
            item => Assert.Equal(
                TacticalEvidenceState.Unsupported,
                item.State));
        Assert.Equal(
            1m,
            candidate.Components.Where(item => item.IsAvailable)
                .Sum(item => item.AppliedWeight!.Value));
    }

    [Fact]
    public void Supported_channel_uses_all_typed_finish_inputs()
    {
        var fixture = FinishFixture(shuffleSkills: false);
        var proof = FinishProof(fixture);

        var result = Score(
            fixture,
            RecommendationPolicy.Aggressive,
            finishProofs: [proof]);
        var candidate = Candidate(result, "604:REVERSE+624:REVERSE");
        var finish = candidate.Get(TacticalScoreComponentKind.FinishPath);

        Assert.True(finish.IsAvailable);
        Assert.Equal(100, finish.NormalizedValue);
        Assert.Equal(5, finish.RawInputs.Length);
        Assert.Equal(
            Enum.GetValues<TacticalFinishEvidenceKind>().Length,
            proof.Inputs.Length);
        Assert.DoesNotContain(
            finish.Limitations,
            item => item.Contains("PROBABILITY", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new TacticalFinishPathProof(
            proof.ChannelCandidate,
            proof.ChannelRole,
            proof.FinishCandidate,
            proof.FinishRole,
            proof.FinishTransition,
            proof.ContextSemanticFingerprint,
            proof.Inputs.Take(4),
            proof.LimitationIdentity));
    }

    [Fact]
    public void Unused_capacity_is_neutral_and_does_not_change_components()
    {
        var roomy = HistoricalFixture(
            [Skill(624, 1234)],
            attackCapacity: 10);
        var tighter = HistoricalFixture(
            [Skill(624, 1234)],
            attackCapacity: 6);

        var roomyScore = Candidate(
            Score(roomy, RecommendationPolicy.Balanced),
            "624:REVERSE");
        var tighterScore = Candidate(
            Score(tighter, RecommendationPolicy.Balanced),
            "624:REVERSE");

        Assert.NotEqual(
            roomyScore.UnusedCapacity.Categories.Single(item =>
                item.Category == SkillCategory.Attack).Remaining,
            tighterScore.UnusedCapacity.Categories.Single(item =>
                item.Category == SkillCategory.Attack).Remaining);
        Assert.False(roomyScore.UnusedCapacity.HasDocumentedMarginalValue);
        Assert.False(tighterScore.UnusedCapacity.HasDocumentedMarginalValue);
        Assert.Equal(
            roomyScore.Components.Select(item => item.NormalizedValue),
            tighterScore.Components.Select(item => item.NormalizedValue));
        Assert.Equal(roomyScore.TotalScore, tighterScore.TotalScore);
    }

    [Fact]
    public void Policies_retain_distinct_ranking_behavior()
    {
        var fixture = FinishFixture(shuffleSkills: false);
        var proof = FinishProof(fixture);

        var safe = Score(
            fixture,
            RecommendationPolicy.Safe,
            finishProofs: [proof]);
        var aggressive = Score(
            fixture,
            RecommendationPolicy.Aggressive,
            finishProofs: [proof]);
        var balanced = Score(
            fixture,
            RecommendationPolicy.Balanced,
            finishProofs: [proof]);

        Assert.Equal("267:DIRECT", safe.RankedCandidates[0].Candidate.StableKey);
        Assert.Equal(
            "604:REVERSE+624:REVERSE",
            aggressive.RankedCandidates[0].Candidate.StableKey);
        Assert.Equal(
            "SAFE_IS_NOT_GUARANTEED_SURVIVAL",
            safe.PolicyLimitationIdentity);
        Assert.Equal(
            "AGGRESSIVE_IS_NOT_VICTORY_OR_DAMAGE_PREDICTION",
            aggressive.PolicyLimitationIdentity);
        Assert.Equal(
            3,
            new[] { safe, balanced, aggressive }
                .Select(item => item.SemanticFingerprint)
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public void Policies_remain_distinct_when_finish_evidence_is_unavailable()
    {
        var fixture = FinishFixture(shuffleSkills: false);
        var results = Enum.GetValues<RecommendationPolicy>()
            .Select(policy => Score(fixture, policy))
            .ToArray();

        Assert.Equal(
            Enum.GetValues<RecommendationPolicy>().Length,
            results.Select(item => item.SemanticFingerprint)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Equal(
            Enum.GetValues<RecommendationPolicy>().Length,
            results.Select(item => item.PolicyLimitationIdentity)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.All(
            results.SelectMany(item => item.RankedCandidates),
            candidate =>
            {
                var finish = candidate.Get(
                    TacticalScoreComponentKind.FinishPath);
                Assert.False(finish.IsAvailable);
                Assert.Null(finish.AppliedWeight);
                Assert.Null(finish.Contribution);
            });
    }

    [Fact]
    public void Ranking_contains_only_hard_feasible_search_results()
    {
        var fixture = FinishFixture(shuffleSkills: false);

        var result = Score(
            fixture,
            RecommendationPolicy.Balanced,
            finishProofs: [FinishProof(fixture)]);

        Assert.Equal(
            fixture.SearchResult.FeasibleResults.Select(item => item.StableKey)
                .Order(StringComparer.Ordinal),
            result.RankedCandidates.Select(item => item.Candidate.StableKey)
                .Order(StringComparer.Ordinal));
        Assert.DoesNotContain(
            result.RankedCandidates,
            item => item.Candidate.SelectedCandidates.Length == 3);
        Assert.All(
            result.RankedCandidates.SelectMany(item => item.Components),
            component =>
            {
                Assert.NotEmpty(component.RawInputs);
                Assert.NotEmpty(component.Evidence);
                Assert.False(string.IsNullOrWhiteSpace(
                    component.NormalizationIdentity));
                Assert.True(component.BaseWeight > 0);
            });
    }

    [Fact]
    public void Ties_and_input_shuffling_are_deterministic()
    {
        var first = DuplicateTransitionFixture(shuffleSkills: false);
        var second = DuplicateTransitionFixture(shuffleSkills: true);

        var firstScore = Score(first, RecommendationPolicy.Balanced);
        var secondScore = Score(second, RecommendationPolicy.Balanced);

        Assert.Equal(
            firstScore.SemanticFingerprint,
            secondScore.SemanticFingerprint);
        var firstSingle = firstScore.RankedCandidates
            .Select((item, index) => (item, index))
            .Single(value => value.item.Candidate.StableKey == "604:REVERSE");
        var secondSingle = firstScore.RankedCandidates
            .Select((item, index) => (item, index))
            .Single(value => value.item.Candidate.StableKey == "624:REVERSE");
        Assert.Equal(firstSingle.item.TotalScore, secondSingle.item.TotalScore);
        Assert.True(firstSingle.index < secondSingle.index);
    }

    [Fact]
    public void Pre_cancelled_scoring_publishes_no_partial_ranking()
    {
        var fixture = HistoricalFixture([Skill(624, 1234)]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            TacticalCombatScorer.Score(
                new TacticalCombatScoringRequest(
                    RecommendationPolicy.Balanced,
                    fixture.SearchRequest,
                    fixture.SearchResult),
                cancellation.Token));
    }

    private static TacticalCombatScoringResult Score(
        FixtureData fixture,
        RecommendationPolicy policy,
        IEnumerable<TacticalLayeringProof>? layeringProofs = null,
        IEnumerable<TacticalTriggerObservability>? observations = null,
        IEnumerable<TacticalFinishPathProof>? finishProofs = null) =>
        TacticalCombatScorer.Score(
            new TacticalCombatScoringRequest(
                policy,
                fixture.SearchRequest,
                fixture.SearchResult,
                layeringProofs,
                observations,
                finishProofs),
            TestContext.Current.CancellationToken);

    private static TacticalScoredLoadout Candidate(
        TacticalCombatScoringResult result,
        string stableKey) => Assert.Single(
        result.RankedCandidates,
        item => item.Candidate.StableKey == stableKey);

    private static TacticalScoreComponent Causal(
        TacticalScoredLoadout candidate) =>
        candidate.Get(TacticalScoreComponentKind.CausalValue);

    private static int[] Values(TacticalScoringPolicyWeights value) =>
        Enum.GetValues<TacticalScoreComponentKind>()
            .Select(value.Get)
            .ToArray();

    private static TacticalTriggerObservability Observable604() => new(
        new TacticalTransitionIdentity("REVERSE_604_SUPPRESSES_DIRECT_CAST"),
        TacticalEvidenceState.Available,
        "TARGET_CAST_TRIGGER_OBSERVABLE",
        [Evidence("TARGET_CAST_OBSERVABLE")],
        "OBSERVATION_REQUIRED_AT_EXECUTION");

    private static TacticalFinishPathProof FinishProof(FixtureData fixture) =>
        new(
            CandidateIdentity(fixture, 604),
            new TacticalRoleIdentity(
                TacticalRoleKind.DamageChannel,
                "SYNTHETIC_DAMAGE_CHANNEL"),
            CandidateIdentity(fixture, 624),
            new TacticalRoleIdentity(
                TacticalRoleKind.Finish,
                "SYNTHETIC_FINISH_ROLE"),
            new TacticalTransitionIdentity("SYNTHETIC_FINISH_WINDOW"),
            fixture.Context.SemanticFingerprint,
            [
                FinishInput(
                    TacticalFinishEvidenceKind.AttackChannelStrength,
                    "ATTACK_CHANNEL_STRENGTH",
                    TacticalFactValue.Integer(100)),
                FinishInput(
                    TacticalFinishEvidenceKind.HitOrCastReliabilityPercent,
                    "CAST_RELIABILITY_PERCENT",
                    TacticalFactValue.Integer(100)),
                FinishInput(
                    TacticalFinishEvidenceKind.TargetDefenseOrResistance,
                    "TARGET_CHANNEL_RESISTANCE",
                    TacticalFactValue.Integer(0)),
                FinishInput(
                    TacticalFinishEvidenceKind.ApplicableCondition,
                    "CHANNEL_CONDITION_APPLICABLE",
                    TacticalFactValue.Boolean(true)),
                FinishInput(
                    TacticalFinishEvidenceKind.FinishWindow,
                    "FINISH_WINDOW_APPLICABLE",
                    TacticalFactValue.Boolean(true))
            ],
            "SUPPORTED_CHANNEL_NOT_PREDICTED_DAMAGE");

    private static TacticalFinishEvidenceInput FinishInput(
        TacticalFinishEvidenceKind kind,
        string identity,
        TacticalFactValue value) => new(
        kind,
        identity,
        value,
        [Evidence($"FINISH_{kind.ToString().ToUpperInvariant()}")]);

    private static TacticalCandidateIdentity CandidateIdentity(
        FixtureData fixture,
        int skillId) => Assert.Single(
            fixture.Discovery.Entries,
            item => item.SkillId == skillId && item.IsAdmitted)
        .Consideration.Identity;

    private static FixtureData HistoricalFixture(
        IEnumerable<CombatSkillSnapshot> skills,
        int attackCapacity = 10) => CreateFixture(
        HistoricalRules,
        skills,
        attackCapacity,
        HistoricalRules.SupportedTargetGoalCodes,
        ConfirmedEvidence(HistoricalRules));

    private static FixtureData DuplicateTransitionFixture(bool shuffleSkills)
    {
        var transition = Transition(
            "SHARED_MITIGATION_TRANSITION",
            TacticalRulePurpose.EnemyAttackPowerReduction,
            TacticalTransitionTiming.AfterCast,
            "POSITIVE_MAGIC_SOUND_MIND_DAMAGE");
        var rules = new TacticalCombatRuleSet(
            new TacticalSemanticVersion(1, 0, 0),
            [VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion],
            ["POSITIVE_MAGIC_SOUND_MIND_DAMAGE"],
            [transition],
            [
                Role(
                    604,
                    "DUPLICATE_MITIGATION_604",
                    TacticalRoleKind.Mitigation,
                    TacticalRulePurpose.EnemyAttackPowerReduction,
                    TacticalTransitionTiming.AfterCast,
                    transition.Identity,
                    "POSITIVE_MAGIC_SOUND_MIND_DAMAGE"),
                Role(
                    624,
                    "DUPLICATE_MITIGATION_624",
                    TacticalRoleKind.Mitigation,
                    TacticalRulePurpose.EnemyAttackPowerReduction,
                    TacticalTransitionTiming.AfterCast,
                    transition.Identity,
                    "POSITIVE_MAGIC_SOUND_MIND_DAMAGE")
            ]);
        var skills = new[] { Skill(604), Skill(624, 1234) };
        return CreateFixture(
            rules,
            shuffleSkills ? skills.Reverse() : skills,
            attackCapacity: 10,
            ["POSITIVE_MAGIC_SOUND_MIND_DAMAGE"],
            ConfirmedEvidence(rules));
    }

    private static FixtureData FinishFixture(bool shuffleSkills)
    {
        var channel = Transition(
            "SYNTHETIC_DAMAGE_CHANNEL_TRANSITION",
            TacticalRulePurpose.DamageChannelChoice,
            TacticalTransitionTiming.AfterCast,
            "POSITIVE_MAGIC_SOUND_MIND_DAMAGE");
        var finish = Transition(
            "SYNTHETIC_FINISH_WINDOW",
            TacticalRulePurpose.FinishWindowSupport,
            TacticalTransitionTiming.AfterCast,
            "POSITIVE_MAGIC_SOUND_MIND_DAMAGE");
        var mitigation = Transition(
            "SYNTHETIC_SAFE_MITIGATION",
            TacticalRulePurpose.EnemyAttackPowerReduction,
            TacticalTransitionTiming.OnObservedState,
            "DISTRACTION_MARK_ACCUMULATION",
            TacticalFactKind.ActiveRole);
        var rules = new TacticalCombatRuleSet(
            new TacticalSemanticVersion(1, 0, 0),
            [VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion],
            [
                "POSITIVE_MAGIC_SOUND_MIND_DAMAGE",
                "DISTRACTION_MARK_ACCUMULATION"
            ],
            [channel, finish, mitigation],
            [
                Role(
                    604,
                    "SYNTHETIC_DAMAGE_CHANNEL",
                    TacticalRoleKind.DamageChannel,
                    TacticalRulePurpose.DamageChannelChoice,
                    TacticalTransitionTiming.AfterCast,
                    channel.Identity,
                    "POSITIVE_MAGIC_SOUND_MIND_DAMAGE"),
                Role(
                    624,
                    "SYNTHETIC_FINISH_ROLE",
                    TacticalRoleKind.Finish,
                    TacticalRulePurpose.FinishWindowSupport,
                    TacticalTransitionTiming.AfterCast,
                    finish.Identity,
                    "POSITIVE_MAGIC_SOUND_MIND_DAMAGE"),
                Role(
                    267,
                    "SYNTHETIC_SAFE_MITIGATION_ROLE",
                    TacticalRoleKind.Mitigation,
                    TacticalRulePurpose.EnemyAttackPowerReduction,
                    TacticalTransitionTiming.OnObservedState,
                    mitigation.Identity,
                    "DISTRACTION_MARK_ACCUMULATION")
            ]);
        var skills = new[]
        {
            Skill(
                604,
                direction: PracticeDirection.Direct,
                completedReverse: true),
            Skill(
                624,
                1234,
                direction: PracticeDirection.Direct,
                completedReverse: true),
            Skill(
                267,
                reverseEffectId: 166,
                direction: PracticeDirection.Direct,
                directEffectId: 165,
                gridCost: 5)
        };
        return CreateFixture(
            rules,
            shuffleSkills ? skills.Reverse() : skills,
            attackCapacity: 4,
            [
                "POSITIVE_MAGIC_SOUND_MIND_DAMAGE",
                "DISTRACTION_MARK_ACCUMULATION"
            ],
            ConfirmedEvidence(rules));
    }

    private static TacticalTransitionRule Transition(
        string identity,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        string goal,
        TacticalFactKind triggerKind = TacticalFactKind.PlayerReadiness) => new(
        new TacticalTransitionIdentity(identity),
        new TacticalSemanticVersion(1, 0, 0),
        [VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion],
        purpose,
        timing,
        [new TacticalFactIdentity(triggerKind,
            $"{identity}_TRIGGER")],
        [new TacticalFactIdentity(TacticalFactKind.Other,
            $"{identity}_RESULT")],
        [goal],
        [EvidenceRequirement(identity)],
        "SYNTHETIC_TEST_LIMITATION",
        [Evidence($"RULE_{identity}")]);

    private static TacticalSkillRoleRule Role(
        int skillId,
        string identity,
        TacticalRoleKind kind,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        TacticalTransitionIdentity transition,
        string goal)
    {
        var source = HistoricalRules.Roles.Single(item =>
            item.SkillId == skillId);
        return new TacticalSkillRoleRule(
            new TacticalRoleIdentity(kind, identity),
            new TacticalSemanticVersion(1, 0, 0),
            [VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion],
            purpose,
            timing,
            source.Effect,
            source.RequiredMechanics,
            [goal],
            [transition],
            [EvidenceRequirement(identity)],
            "SYNTHETIC_TEST_LIMITATION",
            [Evidence($"ROLE_{identity}")],
            source.SharedCounter);
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
            new TacticalEvidenceReference(
                item.Source,
                $"CONFIRMED_{index:000}",
                VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
                VerifiedTacticalCombatRuleSets.RuleVersion,
                item.Scope == TacticalRuleEvidenceScope.ExactTarget
                    ? "EXACT_TARGET"
                    : "BROAD_RULE")))
        .ToArray();

    private static FixtureData CreateFixture(
        TacticalCombatRuleSet rules,
        IEnumerable<CombatSkillSnapshot> skills,
        int attackCapacity,
        IEnumerable<string> goals,
        IEnumerable<TacticalRuleEvidenceObservation> evidence)
    {
        var skillValues = skills.ToArray();
        var budgets = Budgets(attackCapacity);
        var player = new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("display-only player"),
            skillValues,
            new CombatLoadoutSnapshot([], [], [], [], []),
            equipment: [],
            budgets,
            new GenericSlotAllocation(2, 1, 1, 0, 0),
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
            goals,
            evidence);
        var proposal = new TacticalExecutionProposal(
            new CombatRequirementContext(
                equippedWeaponTypeIds: [],
                trickCounts: [],
                SnapshotValue<int>.Available(5),
                resources: [],
                unlockedWeaponTypeIds: [],
                equippedSkillIds: skillValues.Select(item => item.SkillId)),
            budgets,
            new GenericSlotAllocation(2, 1, 1, 0, 0),
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
                maximumOptions: 12,
                maximumExploredCombinations: 1000,
                maximumElapsed: TimeSpan.FromSeconds(30),
                maximumResults: 1000));
        var searchResult = TacticalLoadoutSearch.Search(
            searchRequest,
            new ZeroElapsedTimeProvider(),
            TestContext.Current.CancellationToken);
        return new FixtureData(
            context,
            discovery,
            searchRequest,
            searchResult);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        int reverseEffectId = 1064,
        PracticeDirection direction = PracticeDirection.Reverse,
        bool completedReverse = false,
        int directEffectId = 338,
        int gridCost = 3) => new(
        skillId,
        SnapshotValue<string>.Available("display-only skill"),
        SkillCategory.Attack,
        SnapshotValue<int>.Available(gridCost),
        SnapshotValue<bool>.Available(true),
        SnapshotValue<PracticeDirection>.Available(direction),
        SkillSlotContribution.None,
        SnapshotValue<int>.Available(directEffectId),
        SnapshotValue<int>.Available(reverseEffectId),
        completedReverse
            ? SnapshotValue<BreakthroughDirectionAvailability>.Available(
                new BreakthroughDirectionAvailability(
                    isBrokenOut: true,
                    canBreakthroughNow: false,
                    availableDirections: [],
                    completedDirections: [PracticeDirection.Reverse]))
            : null,
        SnapshotValue<CombatSkillElement>.Available(CombatSkillElement.Water));

    private static SlotBudgetSet Budgets(int attackCapacity) => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, attackCapacity),
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
        TacticalLoadoutSearchRequest SearchRequest,
        TacticalLoadoutSearchResult SearchResult);

    private sealed class ZeroElapsedTimeProvider : TimeProvider
    {
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => 0;
    }
}
