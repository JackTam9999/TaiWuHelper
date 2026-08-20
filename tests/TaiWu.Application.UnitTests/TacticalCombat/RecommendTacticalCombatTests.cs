using NSubstitute;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.TacticalCombat;
using Xunit;

namespace TaiWu.Application.UnitTests.TacticalCombat;

public sealed class RecommendTacticalCombatTests
{
    [Fact]
    public async Task Execute_builds_one_coherent_result_from_one_snapshot()
    {
        var fixture = Fixture();

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(TacticalCombatRecommendationStatus.Success, result.Status);
        Assert.NotNull(result.LegacyRecommendation);
        Assert.NotNull(result.LegacyComparison);
        Assert.NotNull(result.CompiledPlan);
        Assert.NotNull(result.Identity);
        Assert.Equal(
            result.Context!.Context.SourceRevisionFingerprint,
            result.Identity.SnapshotFingerprint);
        Assert.Equal(
            result.RuleResolution!.RuleSetFingerprint,
            result.Identity.RuleFingerprint);
        Assert.Equal(
            result.Search!.SemanticFingerprint,
            result.Identity.CandidateFingerprint);
        Assert.Equal(
            result.CompiledPlan.SelectedLoadoutFingerprint,
            result.Identity.SelectedLoadoutFingerprint);
        Assert.Equal(
            result.CompiledPlan.SemanticFingerprint,
            result.Identity.PlanFingerprint);
        Assert.Equal(1, result.WorkCounts.SnapshotReads);
        Assert.Equal(1, result.WorkCounts.LegacyRecommendationBuilds);
        Assert.Equal(1, result.WorkCounts.ComparisonBuilds);
        Assert.Equal(1, result.WorkCounts.RuleResolutions);
        Assert.Equal(1, result.WorkCounts.ContextProjections);
        Assert.Equal(1, result.WorkCounts.CandidateDiscoveries);
        Assert.Equal(1, result.WorkCounts.Searches);
        Assert.Equal(1, result.WorkCounts.Scores);
        Assert.Equal(1, result.WorkCounts.PlanCompilations);
        await fixture.Reader.Received(1).ReadAsync(
            fixture.SnapshotRequest,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Execute_retains_legacy_result_when_evidence_is_partial()
    {
        var fixture = Fixture(evidence: []);

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.PartialEvidence,
            result.Status);
        Assert.NotNull(result.LegacyRecommendation);
        Assert.NotNull(result.LegacyComparison);
        Assert.NotNull(result.Context);
        Assert.NotNull(result.Identity);
        Assert.Null(result.CompiledPlan);
    }

    [Fact]
    public async Task Execute_stops_safely_on_an_unsupported_rule_chain()
    {
        var fixture = Fixture(gameDataVersion: "UNSUPPORTED-GAME-DATA");

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.UnsupportedChain,
            result.Status);
        Assert.NotNull(result.LegacyRecommendation);
        Assert.NotNull(result.Context);
        Assert.NotNull(result.Identity);
        Assert.Equal(0, result.WorkCounts.CandidateDiscoveries);
        Assert.Null(result.Discovery);
        Assert.Null(result.Search);
    }

    [Fact]
    public async Task Execute_reports_no_candidate_without_promoting_empty_loadout()
    {
        var fixture = Fixture(includeCandidate: false);

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.NoCandidate,
            result.Status);
        Assert.Empty(result.Discovery!.Entries);
        Assert.Single(result.Search!.FeasibleResults);
        Assert.Empty(result.Search.FeasibleResults[0].SelectedCandidates);
        Assert.Null(result.CompiledPlan);
        Assert.Equal(0, result.WorkCounts.PlanCompilations);
    }

    [Fact]
    public async Task Execute_labels_plan_from_bounded_search_as_truncated()
    {
        var fixture = Fixture(bounds: new TacticalSearchBounds(
            8,
            1,
            TimeSpan.FromSeconds(30),
            100));

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.SearchTruncated,
            result.Status);
        Assert.False(result.Search!.IsComplete);
        Assert.NotNull(result.CompiledPlan);
        Assert.Equal(1, result.WorkCounts.PlanCompilations);
        Assert.NotNull(result.Identity);
        Assert.Equal(
            result.Search.SemanticFingerprint,
            result.Identity.CandidateFingerprint);
    }

    [Fact]
    public async Task Execute_propagates_cancellation_without_partial_result()
    {
        var fixture = Fixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Subject.ExecuteAsync(fixture.Request, cancellation.Token));

        await fixture.Reader.DidNotReceive().ReadAsync(
            Arg.Any<CombatSnapshotReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_propagates_cancellation_from_bounded_search()
    {
        using var cancellation = new CancellationTokenSource();
        var fixture = Fixture(
            timeProvider: new CancelingTimeProvider(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Subject.ExecuteAsync(
                fixture.Request,
                cancellation.Token));

        await fixture.Reader.Received(1).ReadAsync(
            fixture.SnapshotRequest,
            cancellation.Token);
    }

    [Fact]
    public async Task Execute_treats_observation_set_as_atomic_replacement()
    {
        var fixture = Fixture();
        var clear = Request(fixture.SnapshotRequest, fixture.Snapshot, []);
        var applied = fixture.Request;
        var replacedEvidence = SearchTacticalLoadoutsTests
            .ConfirmedEvidence();
        var replaced = replacedEvidence[0];
        replacedEvidence[0] = new TacticalRuleEvidenceObservation(
            replaced.Identity,
            replaced.Scope,
            replaced.Source,
            TacticalRuleEvidenceDisposition.Contrary,
            replaced.Evidence);
        var replacement = Request(
            fixture.SnapshotRequest,
            fixture.Snapshot,
            replacedEvidence);

        var token = TestContext.Current.CancellationToken;
        var clearResult = await fixture.Subject.ExecuteAsync(clear, token);
        var applyResult = await fixture.Subject.ExecuteAsync(applied, token);
        var repeatResult = await fixture.Subject.ExecuteAsync(applied, token);
        var replacementResult = await fixture.Subject.ExecuteAsync(
            replacement,
            token);
        var clearedAgainResult = await fixture.Subject.ExecuteAsync(
            clear,
            token);

        Assert.Equal(
            clearResult.Identity!.SemanticFingerprint,
            clearedAgainResult.Identity!.SemanticFingerprint);
        Assert.Equal(
            applyResult.Identity!.SemanticFingerprint,
            repeatResult.Identity!.SemanticFingerprint);
        Assert.NotEqual(
            clearResult.Identity.SemanticFingerprint,
            applyResult.Identity.SemanticFingerprint);
        Assert.NotEqual(
            applyResult.Identity.SemanticFingerprint,
            replacementResult.Identity!.SemanticFingerprint);
    }

    [Fact]
    public async Task Execute_returns_typed_source_failure()
    {
        var fixture = Fixture();
        fixture.Reader.ReadAsync(
                fixture.SnapshotRequest,
                Arg.Any<CancellationToken>())
            .Returns<Task<TaiWu.Domain.CombatSnapshots.CombatSnapshot>>(_ =>
                throw new CombatSnapshotTargetNotFoundException(2));

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.SourceFailure,
            result.Status);
        Assert.Null(result.Identity);
        Assert.Null(result.LegacyRecommendation);
        fixture.FaultReporter.DidNotReceiveWithAnyArgs()
            .Report(default!, default!);
    }

    [Fact]
    public async Task Execute_returns_typed_evidence_failure()
    {
        var fixture = Fixture();
        var invalidContext = new TacticalExecutionContextReadRequest(
            fixture.SnapshotRequest,
            ["UNKNOWN_TARGET_GOAL"],
            SearchTacticalLoadoutsTests.ConfirmedEvidence(),
            SearchTacticalLoadoutsTests.Proposal(
                fixture.Snapshot.Player.LearnedSkills.Select(
                    item => item.SkillId)));
        var invalidRequest = new TacticalCombatRecommendationRequest(
            1,
            RecommendationPolicy.Balanced,
            new TacticalLoadoutSearchReadRequest(
                invalidContext,
                fixture.Request.SearchRequest.Bounds));

        var result = await fixture.Subject.ExecuteAsync(
            invalidRequest,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.EvidenceFailure,
            result.Status);
        Assert.NotNull(result.LegacyRecommendation);
        Assert.Null(result.RuleResolution);
        Assert.Null(result.Identity);
    }

    [Fact]
    public async Task Execute_logs_unexpected_fault_and_returns_safe_identity()
    {
        var fixture = Fixture();
        fixture.Reader.ReadAsync(
                fixture.SnapshotRequest,
                Arg.Any<CancellationToken>())
            .Returns<Task<TaiWu.Domain.CombatSnapshots.CombatSnapshot>>(_ =>
                throw new NotSupportedException("sensitive implementation"));

        var result = await fixture.Subject.ExecuteAsync(
            fixture.Request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.UnexpectedFailure,
            result.Status);
        Assert.Equal(
            "UNEXPECTED_TACTICAL_RECOMMENDATION_FAILURE",
            result.ReasonIdentity);
        Assert.Null(result.Identity);
        fixture.FaultReporter.Received(1).Report(
            Arg.Is<NotSupportedException>(exception =>
                exception != null
                && exception.Message == "sensitive implementation"),
            "TACTICAL_RECOMMENDATION_SOURCE");
    }

    private static TestFixture Fixture(
        bool includeCandidate = true,
        string? gameDataVersion = null,
        TacticalSearchBounds? bounds = null,
        TacticalRuleEvidenceObservation[]? evidence = null,
        TimeProvider? timeProvider = null)
    {
        var snapshot = SearchTacticalLoadoutsTests.Snapshot(
            includeCandidate,
            gameDataVersion);
        var snapshotRequest = new CombatSnapshotReadRequest("local.sav", 2);
        var request = Request(
            snapshotRequest,
            snapshot,
            evidence ?? SearchTacticalLoadoutsTests.ConfirmedEvidence(),
            bounds);
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(snapshotRequest, Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var reporter = Substitute.For<
            ITacticalCombatRecommendationFaultReporter>();
        var subject = new RecommendTacticalCombat(
            reader,
            timeProvider
                ?? new SearchTacticalLoadoutsTests.ZeroElapsedTimeProvider(),
            reporter);
        return new TestFixture(
            reader,
            reporter,
            subject,
            snapshot,
            snapshotRequest,
            request);
    }

    private static TacticalCombatRecommendationRequest Request(
        CombatSnapshotReadRequest snapshotRequest,
        TaiWu.Domain.CombatSnapshots.CombatSnapshot snapshot,
        IEnumerable<TacticalRuleEvidenceObservation> evidence,
        TacticalSearchBounds? bounds = null) => new(
        snapshot.Player.CharacterId,
        RecommendationPolicy.Balanced,
        new TacticalLoadoutSearchReadRequest(
            new TacticalExecutionContextReadRequest(
                snapshotRequest,
                SearchTacticalLoadoutsTests.Rules.SupportedTargetGoalCodes,
                evidence,
                SearchTacticalLoadoutsTests.Proposal(
                    snapshot.Player.LearnedSkills.Select(item => item.SkillId))),
            bounds ?? new TacticalSearchBounds(
                8,
                100,
                TimeSpan.FromSeconds(30),
                100)));

    private sealed record TestFixture(
        ICombatSnapshotReader Reader,
        ITacticalCombatRecommendationFaultReporter FaultReporter,
        RecommendTacticalCombat Subject,
        TaiWu.Domain.CombatSnapshots.CombatSnapshot Snapshot,
        CombatSnapshotReadRequest SnapshotRequest,
        TacticalCombatRecommendationRequest Request);

    private sealed class CancelingTimeProvider(
        CancellationTokenSource cancellation) : TimeProvider
    {
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            cancellation.Cancel();
            return 0;
        }
    }
}
