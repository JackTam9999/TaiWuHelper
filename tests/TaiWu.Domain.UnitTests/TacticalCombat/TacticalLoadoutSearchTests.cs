using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Domain.UnitTests.TacticalCombat;

public sealed class TacticalLoadoutSearchTests
{
    private static readonly TacticalCombatRuleSet Rules =
        VerifiedTacticalCombatRuleSets.HistoricalMagicSound;

    [Fact]
    public void Shuffled_inputs_have_identical_traversal_and_results()
    {
        var first = Fixture([Skill(604), Skill(624, reverseEffectId: 1234)]);
        var second = Fixture([Skill(624, reverseEffectId: 1234), Skill(604)]);

        var firstResult = Search(first);
        var secondResult = Search(second);

        Assert.Equal(firstResult.SemanticFingerprint, secondResult.SemanticFingerprint);
        Assert.Equal(
            [
                "604:REVERSE",
                "604:REVERSE+624:REVERSE",
                "624:REVERSE",
                "EMPTY"
            ],
            firstResult.FeasibleResults.Select(item => item.StableKey));
        Assert.True(firstResult.IsComplete);
        Assert.False(firstResult.IsOptimal);
        Assert.Equal(4, firstResult.Coverage.ExploredCombinationCount);
    }

    [Fact]
    public void Irrelevance_requires_an_explicit_proof_after_admission()
    {
        var fixture = Fixture([Skill(604)]);
        var admitted = Candidate(fixture, 604);

        var withoutProof = Search(fixture);
        var withProof = Search(
            fixture,
            irrelevanceProofs:
            [
                new TacticalIrrelevanceProof(
                    admitted,
                    fixture.Context.SemanticFingerprint,
                    [Evidence("NO_APPLICABLE_ROLE")])
            ]);

        Assert.Equal(1, withoutProof.Coverage.AdmittedCount);
        Assert.Empty(withoutProof.PrunedCandidates);
        var pruned = Assert.Single(withProof.PrunedCandidates);
        Assert.Equal(TacticalPruningRuleKind.IrrelevantToTarget, pruned.Rule);
        Assert.Equal(admitted, pruned.Candidate);
        Assert.Equal(1, withProof.Coverage.IrrelevantCount);
        Assert.Equal(0, withProof.Coverage.AdmittedCount);
    }

    [Fact]
    public void Pruning_cannot_bypass_hard_gate_admission()
    {
        var fixture = Fixture([Skill(999, reverseEffectId: 999)]);

        Assert.Throws<ArgumentException>(() => Search(
            fixture,
            irrelevanceProofs:
            [
                new TacticalIrrelevanceProof(
                    new TacticalCandidateIdentity(999, PracticeDirection.Reverse),
                    fixture.Context.SemanticFingerprint,
                    [Evidence("UNSUPPORTED_CANNOT_BE_PRUNED")])
            ]));
    }

    [Fact]
    public void Search_rejects_discovery_from_a_different_context()
    {
        var fixture = Fixture([Skill(604)]);
        var other = Fixture(
            [Skill(604)],
            proposedAttackCapacity: 9);

        Assert.Throws<ArgumentException>(() => TacticalLoadoutSearch.Search(
            new TacticalLoadoutSearchRequest(
                fixture.Snapshot.Player,
                fixture.Context,
                fixture.Resolution,
                other.Discovery,
                Bounds()),
            cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Dominance_requires_all_dimensions_and_retains_canonical_tie()
    {
        var fixture = Fixture([Skill(604), Skill(624, reverseEffectId: 1234)]);
        var smaller = Candidate(fixture, 604);
        var larger = Candidate(fixture, 624);

        var result = Search(
            fixture,
            dominanceProofs:
            [Dominance(fixture, larger, smaller, isStrictlyBetter: false)]);

        var pruned = Assert.Single(result.PrunedCandidates);
        Assert.Equal(TacticalPruningRuleKind.DominatedInSameContext, pruned.Rule);
        Assert.Equal(smaller, pruned.Dominator);
        Assert.Equal(1, result.Coverage.DominatedCount);
        Assert.Equal(2, result.Coverage.ExploredCombinationCount);

        Assert.Throws<ArgumentException>(() => Search(
            fixture,
            dominanceProofs:
            [Dominance(fixture, smaller, larger, isStrictlyBetter: false)]));
        Assert.Throws<ArgumentException>(() => new TacticalDominanceProof(
            larger,
            smaller,
            fixture.Context.SemanticFingerprint,
            isStrictlyBetter: true,
            dimensions: []));
    }

    [Fact]
    public void Option_limit_is_the_first_honest_terminator()
    {
        var fixture = Fixture([Skill(604), Skill(624, reverseEffectId: 1234)]);

        var result = Search(fixture, Bounds(maximumOptions: 1));

        Assert.Equal(TacticalSearchTerminator.OptionLimit, result.Coverage.FirstTerminator);
        Assert.Equal(1, result.Coverage.SearchedOptionCount);
        Assert.False(result.IsComplete);
        Assert.False(result.IsOptimal);
    }

    [Fact]
    public void Search_bounds_reject_unbounded_requests()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TacticalSearchBounds(25, 100, TimeSpan.FromSeconds(1), 10));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TacticalSearchBounds(
                8,
                1_000_001,
                TimeSpan.FromSeconds(1),
                10));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TacticalSearchBounds(8, 100, TimeSpan.FromMinutes(11), 10));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TacticalSearchBounds(
                8,
                100,
                TimeSpan.FromSeconds(1),
                10_001));
    }

    [Fact]
    public void Exploration_limit_stops_at_its_exact_bound()
    {
        var fixture = Fixture([Skill(604), Skill(624, reverseEffectId: 1234)]);

        var result = Search(
            fixture,
            Bounds(maximumExploredCombinations: 2));

        Assert.Equal(
            TacticalSearchTerminator.ExplorationLimit,
            result.Coverage.FirstTerminator);
        Assert.Equal(2, result.Coverage.ExploredCombinationCount);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public void Result_limit_counts_the_first_unretained_feasible_result()
    {
        var fixture = Fixture([Skill(604), Skill(624, reverseEffectId: 1234)]);

        var result = Search(fixture, Bounds(maximumResults: 1));

        Assert.Equal(TacticalSearchTerminator.ResultLimit, result.Coverage.FirstTerminator);
        Assert.Equal(2, result.Coverage.FeasibleResultCount);
        Assert.Equal(1, result.Coverage.RetainedResultCount);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public void Time_limit_excludes_elapsed_diagnostics_from_semantics()
    {
        var fixture = Fixture([Skill(604)]);
        var shortRun = Search(
            fixture,
            timeProvider: new FinalElapsedTimeProvider(TimeSpan.FromMilliseconds(1)));
        var longerRun = Search(
            fixture,
            timeProvider: new FinalElapsedTimeProvider(TimeSpan.FromMilliseconds(10)));

        Assert.NotEqual(shortRun.Coverage.Elapsed, longerRun.Coverage.Elapsed);
        Assert.Equal(shortRun.SemanticFingerprint, longerRun.SemanticFingerprint);

        var limited = Search(
            fixture,
            Bounds(maximumElapsed: TimeSpan.FromSeconds(1)),
            timeProvider: new AdvancingTimeProvider(TimeSpan.FromSeconds(1)));
        Assert.Equal(TacticalSearchTerminator.TimeLimit, limited.Coverage.FirstTerminator);
        Assert.Equal(0, limited.Coverage.ExploredCombinationCount);
        Assert.True(limited.Coverage.Elapsed >= TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Pre_cancelled_search_reports_cancellation_without_exploration()
    {
        var fixture = Fixture([Skill(604)]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = TacticalLoadoutSearch.Search(
            Request(fixture),
            cancellationToken: cancellation.Token);

        Assert.Equal(TacticalSearchTerminator.Cancelled, result.Coverage.FirstTerminator);
        Assert.Equal(0, result.Coverage.ExploredCombinationCount);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public void No_candidate_search_is_complete_and_explores_the_empty_loadout()
    {
        var fixture = Fixture([Skill(999, reverseEffectId: 999)]);

        var result = Search(fixture);

        Assert.Equal(0, result.Coverage.AdmittedCount);
        Assert.Equal(0, result.Coverage.UnsupportedCount);
        Assert.Equal(2, result.Coverage.IrrelevantCount);
        Assert.Equal(2, result.PrunedCandidates.Length);
        Assert.Equal(1, result.Coverage.ExploredCombinationCount);
        Assert.Equal(TacticalSearchTerminator.None, result.Coverage.FirstTerminator);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void Per_request_candidate_projection_cache_is_reused_and_bounded()
    {
        var fixture = Fixture([Skill(604), Skill(624, reverseEffectId: 1234)]);

        var result = Search(fixture);

        var candidateCache = Assert.Single(
            result.Coverage.Caches,
            item => item.CacheIdentity == "CANDIDATE_PROJECTION_CACHE");
        Assert.Equal(2, candidateCache.MissCount);
        Assert.True(candidateCache.HitCount > 0);
        Assert.True(candidateCache.HitCount + candidateCache.MissCount <= 4);
        var feasibilityCache = Assert.Single(
            result.Coverage.Caches,
            item => item.CacheIdentity == "FEASIBILITY_CACHE");
        Assert.Equal(4, feasibilityCache.MissCount);
        Assert.Equal(0, feasibilityCache.HitCount);
    }

    [Fact]
    public void Existing_validator_remains_final_loadout_authority()
    {
        var fixture = Fixture(
            [Skill(604), Skill(624, reverseEffectId: 1234)],
            actualAttackCapacity: 2,
            proposedAttackCapacity: 10);

        var result = Search(fixture);

        Assert.Equal(4, result.Coverage.ExploredCombinationCount);
        Assert.Equal(3, result.Coverage.FeasibleResultCount);
        Assert.DoesNotContain(
            result.FeasibleResults,
            item => item.StableKey == "604:REVERSE+624:REVERSE");
    }

    private static TacticalLoadoutSearchResult Search(
        FixtureData fixture,
        TacticalSearchBounds? bounds = null,
        IEnumerable<TacticalIrrelevanceProof>? irrelevanceProofs = null,
        IEnumerable<TacticalDominanceProof>? dominanceProofs = null,
        TimeProvider? timeProvider = null) =>
        TacticalLoadoutSearch.Search(
            Request(
                fixture,
                bounds,
                irrelevanceProofs,
                dominanceProofs),
            timeProvider,
            TestContext.Current.CancellationToken);

    private static TacticalLoadoutSearchRequest Request(
        FixtureData fixture,
        TacticalSearchBounds? bounds = null,
        IEnumerable<TacticalIrrelevanceProof>? irrelevanceProofs = null,
        IEnumerable<TacticalDominanceProof>? dominanceProofs = null) => new(
        fixture.Snapshot.Player,
        fixture.Context,
        fixture.Resolution,
        fixture.Discovery,
        bounds ?? Bounds(),
        irrelevanceProofs,
        dominanceProofs);

    private static TacticalCandidateIdentity Candidate(
        FixtureData fixture,
        int skillId) => Assert.Single(
            fixture.Discovery.Entries,
            item => item.SkillId == skillId
                && item.Direction == PracticeDirection.Reverse).Consideration.Identity;

    private static TacticalDominanceProof Dominance(
        FixtureData fixture,
        TacticalCandidateIdentity dominated,
        TacticalCandidateIdentity dominator,
        bool isStrictlyBetter) => new(
        dominated,
        dominator,
        fixture.Context.SemanticFingerprint,
        isStrictlyBetter,
        Enum.GetValues<TacticalDominanceDimension>().Select(dimension =>
            new TacticalDominanceDimensionEvidence(
                dimension,
                Evidence($"DOMINANCE_{dimension.ToString().ToUpperInvariant()}"))));

    private static TacticalEvidenceReference Evidence(string identity) => new(
        TacticalEvidenceSourceKind.VerifiedRule,
        identity,
        VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
        VerifiedTacticalCombatRuleSets.RuleVersion,
        "EXACT_TARGET");

    private static TacticalSearchBounds Bounds(
        int maximumOptions = 8,
        int maximumExploredCombinations = 100,
        TimeSpan? maximumElapsed = null,
        int maximumResults = 100) => new(
        maximumOptions,
        maximumExploredCombinations,
        maximumElapsed ?? TimeSpan.FromSeconds(30),
        maximumResults);

    private static FixtureData Fixture(
        IEnumerable<CombatSkillSnapshot> skills,
        int actualAttackCapacity = 10,
        int proposedAttackCapacity = 10)
    {
        var skillValues = skills.ToArray();
        var actualBudgets = Budgets(actualAttackCapacity);
        var proposedBudgets = Budgets(proposedAttackCapacity);
        var player = new PlayerCombatSnapshot(
            1,
            SnapshotValue<string>.Available("display-only player"),
            skillValues,
            new CombatLoadoutSnapshot([], [], [], [], []),
            equipment: [],
            actualBudgets,
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
                new string('C', 64),
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
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable("Not required."),
                equipment: []),
            warnings: []);
        var resolution = Rules.Resolve(
            VerifiedTacticalCombatRuleSets.HistoricalGameDataVersion,
            Rules.SupportedTargetGoalCodes,
            ConfirmedEvidence());
        var proposal = new TacticalExecutionProposal(
            new CombatRequirementContext(
                equippedWeaponTypeIds: [],
                trickCounts: [],
                SnapshotValue<int>.Available(5),
                resources: [],
                unlockedWeaponTypeIds: [],
                equippedSkillIds: skillValues.Select(item => item.SkillId)),
            proposedBudgets,
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
        return new FixtureData(snapshot, resolution, context, discovery);
    }

    private static TacticalRuleEvidenceObservation[] ConfirmedEvidence() =>
        Rules.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(Rules.Roles.SelectMany(item => item.EvidenceRequirements))
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

    private static CombatSkillSnapshot Skill(
        int skillId,
        int reverseEffectId = 1064) => new(
        skillId,
        SnapshotValue<string>.Available("display-only skill"),
        SkillCategory.Attack,
        SnapshotValue<int>.Available(3),
        SnapshotValue<bool>.Available(true),
        SnapshotValue<PracticeDirection>.Available(PracticeDirection.Reverse),
        SkillSlotContribution.None,
        SnapshotValue<int>.Available(338),
        SnapshotValue<int>.Available(reverseEffectId),
        breakthroughDirections: null,
        SnapshotValue<CombatSkillElement>.Available(CombatSkillElement.Water));

    private static SlotBudgetSet Budgets(int attackCapacity) => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, attackCapacity),
        new SlotBudget(SkillCategory.Agility, 0, 8),
        new SlotBudget(SkillCategory.Defense, 0, 8),
        new SlotBudget(SkillCategory.Assistance, 0, 2)
    ]);

    private sealed record FixtureData(
        CombatSnapshot Snapshot,
        TacticalCombatRuleResolution Resolution,
        TacticalExecutionContext Context,
        TacticalCandidateDiscoveryResult Discovery);

    private sealed class FinalElapsedTimeProvider(TimeSpan elapsed)
        : TimeProvider
    {
        private int _calls;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            _calls++;
            return _calls <= 3 ? 0 : elapsed.Ticks;
        }
    }

    private sealed class AdvancingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            var current = _timestamp;
            _timestamp += step.Ticks;
            return current;
        }
    }
}
