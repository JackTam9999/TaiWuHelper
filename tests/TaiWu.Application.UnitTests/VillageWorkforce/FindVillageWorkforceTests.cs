using NSubstitute;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Application.UnitTests.VillageWorkforce;

public sealed class FindVillageWorkforceTests
{
    [Fact]
    public async Task Complete_request_reads_once_and_builds_one_coherent_result()
    {
        var snapshot = Snapshot([
            Worker(101, 60),
            Worker(202, 80)
        ]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var request = Request(
            snapshot,
            filter: WorkforceShortlistFilter.Comparable,
            firstComparisonWorker: new VillageWorkerIdentity(202),
            secondComparisonWorker: new VillageWorkerIdentity(101),
            proposedWorker: new VillageWorkerIdentity(202));

        var result = await new FindVillageWorkforce(reader).ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(VillageWorkforceFinderStatus.Complete, result.Status);
        Assert.True(result.HasAuthoritativeResult);
        Assert.Same(snapshot, result.Snapshot);
        Assert.Equal(
            WorkforceRuleResolutionStatus.Resolved,
            result.RuleResolutionStatus);
        Assert.NotNull(result.Rule);
        Assert.NotNull(result.EvaluationSet);
        Assert.Same(result.EvaluationSet, result.Shortlist?.EvaluationSet);
        Assert.Equal(
            result.Shortlist?.Fingerprint,
            result.View?.ResultFingerprint);
        Assert.Equal(
            WorkforceComparisonOutcome.Higher,
            result.Comparison?.Outcome);
        Assert.Equal(202, result.ManualPlan?.ProposedAssignment
            .Worker.CharacterId);
        Assert.Equal(64, result.Fingerprint?.Length);
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Partial_source_or_evaluation_produces_typed_partial_result()
    {
        var completeSnapshot = Snapshot([Worker(
            101,
            qualification: null,
            qualificationState: WorkforceEvidenceState.Incomplete)]);
        var partialSnapshot = Snapshot([Worker(101, 60)]);
        var incompleteReader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(completeSnapshot));
        var partialReader = Reader(
            VillageWorkforceSnapshotReadResult.Partial(partialSnapshot));

        var incomplete = await new FindVillageWorkforce(incompleteReader)
            .ExecuteAsync(
                Request(completeSnapshot),
                TestContext.Current.CancellationToken);
        var partial = await new FindVillageWorkforce(partialReader)
            .ExecuteAsync(
                Request(partialSnapshot),
                TestContext.Current.CancellationToken);

        Assert.Equal(VillageWorkforceFinderStatus.Partial, incomplete.Status);
        Assert.Equal(1, incomplete.Shortlist?.Counts.Incomplete);
        Assert.Equal(VillageWorkforceFinderStatus.Partial, partial.Status);
        await incompleteReader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
        await partialReader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(99, true, true, false)]
    [InlineData(0, true, false, false)]
    [InlineData(0, false, true, false)]
    [InlineData(0, true, true, true)]
    public async Task Invalid_filter_or_comparison_shape_stops_before_read(
        int filter,
        bool hasFirst,
        bool hasSecond,
        bool sameWorker)
    {
        var snapshot = Snapshot([Worker(101, 60), Worker(202, 80)]);
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        var first = hasFirst ? new VillageWorkerIdentity(101) : null;
        var second = hasSecond
            ? new VillageWorkerIdentity(sameWorker ? 101 : 202)
            : null;
        var request = Request(
            snapshot,
            (WorkforceShortlistFilter)filter,
            first,
            second);

        var result = await new FindVillageWorkforce(reader).ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            VillageWorkforceFinderStatus.InvalidRequest,
            result.Status);
        Assert.False(result.HasAuthoritativeResult);
        await reader.DidNotReceive().ReadAsync(
            Arg.Any<VillageWorkforceSnapshotReadRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(
        VillageWorkforceSnapshotReadStatus.SaveUnavailable,
        VillageWorkforceFinderStatus.SaveUnavailable)]
    [InlineData(
        VillageWorkforceSnapshotReadStatus.UnsupportedVersion,
        VillageWorkforceFinderStatus.UnsupportedSourceVersion)]
    [InlineData(
        VillageWorkforceSnapshotReadStatus.ConflictingSources,
        VillageWorkforceFinderStatus.ConflictingSources)]
    [InlineData(
        VillageWorkforceSnapshotReadStatus.ChangedRevision,
        VillageWorkforceFinderStatus.ChangedRevision)]
    [InlineData(
        VillageWorkforceSnapshotReadStatus.ReadFailed,
        VillageWorkforceFinderStatus.ReadFailed)]
    public async Task Snapshot_failures_are_typed(
        VillageWorkforceSnapshotReadStatus readStatus,
        VillageWorkforceFinderStatus expected)
    {
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Reader(VillageWorkforceSnapshotReadResult.Failed(
            readStatus,
            "SYNTHETIC_FAILURE",
            "Synthetic safe failure."));

        var result = await new FindVillageWorkforce(reader).ExecuteAsync(
            Request(snapshot),
            TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Status);
        Assert.Equal(readStatus, result.SnapshotReadStatus);
        Assert.False(result.HasAuthoritativeResult);
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_target_is_typed_after_one_source_read()
    {
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var missingTarget = new ShopManagerTargetIdentity(
            new ShopBuildingIdentity(1, 2, 99),
            0);
        var request = new VillageWorkforceFinderRequest(
            missingTarget,
            Objective());

        var result = await new FindVillageWorkforce(reader).ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            VillageWorkforceFinderStatus.TargetNotFound,
            result.Status);
        Assert.False(result.HasAuthoritativeResult);
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unsupported_objective_version_returns_typed_rule_failure()
    {
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var request = new VillageWorkforceFinderRequest(
            snapshot.Targets[0].Identity,
            Objective("2"));

        var result = await new FindVillageWorkforce(reader).ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            VillageWorkforceFinderStatus.UnsupportedRule,
            result.Status);
        Assert.Equal(
            WorkforceRuleResolutionStatus.UnsupportedObjectiveVersion,
            result.RuleResolutionStatus);
        Assert.False(result.HasAuthoritativeResult);
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_comparison_retains_authoritative_fingerprint()
    {
        var snapshot = Snapshot([Worker(101, 60), Worker(202, 80)]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var workflow = new FindVillageWorkforce(reader);
        var complete = await workflow.ExecuteAsync(
            Request(snapshot),
            TestContext.Current.CancellationToken);
        var invalid = await workflow.ExecuteAsync(
            Request(
                snapshot,
                firstComparisonWorker: new VillageWorkerIdentity(101),
                secondComparisonWorker: new VillageWorkerIdentity(999)),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            VillageWorkforceFinderStatus.InvalidComparison,
            invalid.Status);
        Assert.True(invalid.HasAuthoritativeResult);
        Assert.Equal(complete.Fingerprint, invalid.Fingerprint);
        Assert.Null(invalid.Comparison);
        await reader.Received(2).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(101)]
    [InlineData(999)]
    public async Task Current_or_unknown_proposal_is_typed_invalid(
        int proposedCharacterId)
    {
        var snapshot = Snapshot([Worker(101, 60), Worker(202, 80)]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));

        var result = await new FindVillageWorkforce(reader).ExecuteAsync(
            Request(
                snapshot,
                proposedWorker:
                    new VillageWorkerIdentity(proposedCharacterId)),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            VillageWorkforceFinderStatus.InvalidProposal,
            result.Status);
        Assert.True(result.HasAuthoritativeResult);
        Assert.Null(result.ManualPlan);
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancellation_propagates_and_is_not_mapped_to_failure()
    {
        using var cancellation = new CancellationTokenSource();
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<VillageWorkforceSnapshotReadResult>(
                    call.ArgAt<CancellationToken>(1));
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new FindVillageWorkforce(reader).ExecuteAsync(
                Request(snapshot),
                cancellation.Token));

        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            cancellation.Token);
    }

    [Fact]
    public async Task Unexpected_programmer_fault_reaches_the_host()
    {
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns<VillageWorkforceSnapshotReadResult>(_ =>
                throw new InvalidOperationException("synthetic fault"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new FindVillageWorkforce(reader).ExecuteAsync(
                Request(snapshot),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task New_revision_replaces_the_entire_result_without_mixing()
    {
        var firstSnapshot = Snapshot([Worker(101, 60)], ShaA);
        var secondSnapshot = Snapshot([
            Worker(101, 90, revision: ShaB)
        ], ShaB);
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(
                VillageWorkforceSnapshotReadResult.Complete(firstSnapshot),
                VillageWorkforceSnapshotReadResult.Complete(secondSnapshot));
        var workflow = new FindVillageWorkforce(reader);

        var first = await workflow.ExecuteAsync(
            Request(firstSnapshot),
            TestContext.Current.CancellationToken);
        var second = await workflow.ExecuteAsync(
            Request(secondSnapshot),
            TestContext.Current.CancellationToken);

        Assert.NotEqual(first.Fingerprint, second.Fingerprint);
        Assert.Same(firstSnapshot, first.Snapshot);
        Assert.Same(secondSnapshot, second.Snapshot);
        Assert.Equal(
            60m,
            first.EvaluationSet?.Evaluations[0].Result?.Value);
        Assert.Equal(
            90m,
            second.EvaluationSet?.Evaluations[0].Result?.Value);
        await reader.Received(2).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    private const string ShaA =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string ShaB =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private static IVillageWorkforceSnapshotReader Reader(
        VillageWorkforceSnapshotReadResult result)
    {
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(result);
        return reader;
    }

    private static VillageWorkforceFinderRequest Request(
        VillageWorkforceSnapshot snapshot,
        WorkforceShortlistFilter filter = WorkforceShortlistFilter.All,
        VillageWorkerIdentity? firstComparisonWorker = null,
        VillageWorkerIdentity? secondComparisonWorker = null,
        VillageWorkerIdentity? proposedWorker = null) =>
        new(
            snapshot.Targets[0].Identity,
            Objective(),
            filter,
            firstComparisonWorker,
            secondComparisonWorker,
            proposedWorker);

    private static WorkforceObjectiveIdentity Objective(
        string version = "1") =>
        new(
            WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
            version);

    private static VillageWorkforceSnapshot Snapshot(
        IEnumerable<VillageWorkerProfile> workers,
        string revision = ShaA)
    {
        var copiedWorkers = workers.ToArray();
        var versions = Versions(revision);
        if (copiedWorkers.Any(worker => worker.SourceVersions != versions))
        {
            throw new ArgumentException(
                "Synthetic workers must use the snapshot revision.",
                nameof(workers));
        }

        var provenance = SaveProvenance(revision);
        var target = new ShopManagerTarget(
            new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(1, 2, 7),
                0),
            new LifeSkillDisciplineIdentity(6),
            [new WorkforceEvidenceReference(
                "SHOP_TARGET",
                GameDataProvenance())]);
        return new VillageWorkforceSnapshot(
            new SettlementIdentity(12),
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
            versions,
            copiedWorkers,
            [target],
            [new CurrentShopManagerAssignment(
                target.Identity,
                copiedWorkers[0].Identity,
                provenance)],
            []);
    }

    private static VillageWorkerProfile Worker(
        int characterId,
        short? qualification,
        WorkforceEvidenceState qualificationState =
            WorkforceEvidenceState.Confirmed,
        string revision = ShaA)
    {
        var provenance = SaveProvenance(revision);
        var qualificationIdentity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));
        var qualificationFact = qualificationState switch
        {
            WorkforceEvidenceState.Confirmed => WorkforceFact.Confirmed(
                qualificationIdentity,
                WorkforceFactValue.Int16(qualification
                    ?? throw new ArgumentNullException(nameof(qualification))),
                provenance,
                [new WorkforceEvidenceReference("QUALIFICATION", provenance)]),
            WorkforceEvidenceState.Incomplete => WorkforceFact.Incomplete(
                qualificationIdentity,
                new WorkforceUnavailableReason("QUALIFICATION_MISSING"),
                [new WorkforceEvidenceReference("QUALIFICATION", provenance)]),
            _ => throw new ArgumentOutOfRangeException(
                nameof(qualificationState))
        };
        return new VillageWorkerProfile(
            new VillageWorkerIdentity(characterId),
            WorkforceWorkerState.Eligible,
            Versions(revision),
            [
                WorkforceFact.Confirmed(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.CandidateUniverseMembership),
                    WorkforceFactValue.Boolean(true),
                    provenance,
                    [new WorkforceEvidenceReference(
                        "WORK_CANDIDATE",
                        provenance)]),
                qualificationFact
            ],
            []);
    }

    private static WorkforceSourceVersions Versions(string revision) =>
        new(
            revision,
            VerifiedVillageWorkforceRules.SupportedGameDataVersion,
            "1",
            "1",
            "1");

    private static WorkforceProvenance SaveProvenance(string revision) =>
        new(
            WorkforceEvidenceSourceKind.ConfiguredSave,
            "CONFIGURED_SAVE",
            "1",
            revision);

    private static WorkforceProvenance GameDataProvenance() =>
        new(
            WorkforceEvidenceSourceKind.InstalledGameData,
            "GAMEDATA",
            VerifiedVillageWorkforceRules.SupportedGameDataVersion,
            "ASSEMBLY_A");
}
