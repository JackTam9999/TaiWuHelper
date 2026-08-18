using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Security.Cryptography;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

[Collection(TaiwuArchivePerformanceCollection.Name)]
public sealed class VillageWorkforceSnapshotIntegrationTests(
    ITestOutputHelper output)
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";

    [Fact]
    public async Task Snapshot_is_one_pass_repeatable_and_read_only()
    {
        var savePath = RequireSavePath();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SaveGames:DefaultSaveFilePath"] = savePath
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTaiwuInfrastructure();
        using var provider = services.BuildServiceProvider();
        var reader = provider
            .GetRequiredService<IVillageWorkforceSnapshotReader>();
        var guardedPaths = new[]
        {
            savePath,
            Path.Combine(AppContext.BaseDirectory, "GameData.dll"),
            Path.Combine(AppContext.BaseDirectory, "GameData.Shared.dll")
        };
        Assert.All(guardedPaths, path => Assert.True(File.Exists(path)));
        var before = await CaptureAsync(guardedPaths);

        var coldWatch = Stopwatch.StartNew();
        var firstRead = await reader.ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);
        coldWatch.Stop();
        var warmWatch = Stopwatch.StartNew();
        var secondRead = await reader.ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);
        warmWatch.Stop();
        var after = await CaptureAsync(guardedPaths);

        Assert.Equal(
            VillageWorkforceSnapshotReadStatus.Complete,
            firstRead.Status);
        Assert.Equal(firstRead.Status, secondRead.Status);
        var first = Assert.IsType<VillageWorkforceSnapshot>(
            firstRead.Snapshot);
        var second = Assert.IsType<VillageWorkforceSnapshot>(
            secondRead.Snapshot);
        Assert.Equal(before, after);
        Assert.Equal(first.SourceVersions, second.SourceVersions);
        Assert.Equal(
            first.Workers.Select(item => (
                item.Identity.CharacterId,
                item.Fingerprint)),
            second.Workers.Select(item => (
                item.Identity.CharacterId,
                item.Fingerprint)));
        Assert.Equal(
            first.Targets.Select(item => item.Fingerprint),
            second.Targets.Select(item => item.Fingerprint));
        Assert.Equal(first.CurrentAssignments, second.CurrentAssignments);
        Assert.Equal(first.Diagnostics, second.Diagnostics);
        Assert.Equal(first.Workers.Length, firstRead.WorkerDisplays.Length);
        Assert.Equal(first.Targets.Length, firstRead.TargetDisplays.Length);
        Assert.All(firstRead.TargetDisplays, display =>
        {
            Assert.False(string.IsNullOrWhiteSpace(
                display.TraditionalChineseBuildingName));
            Assert.False(string.IsNullOrWhiteSpace(display.EnglishBuildingName));
            Assert.False(string.IsNullOrWhiteSpace(
                display.TraditionalChineseDisciplineName));
            Assert.False(string.IsNullOrWhiteSpace(display.EnglishDisciplineName));
        });
        Assert.Contains(firstRead.WorkerDisplays, display =>
            display.TraditionalChineseName is not null
            && display.EnglishName is not null);
        Assert.All(firstRead.WorkerDisplays, display =>
        {
            var capability = Assert.IsType<VillageWorkerCapabilityDisplay>(
                display.Capability);
            Assert.Equal(6, capability.MainAttributes.Length);
            Assert.Equal(14, capability.MartialDisciplines.Length);
            Assert.Equal(16, capability.LifeSkillDisciplines.Length);
        });
        Assert.NotEqual(first.CapturedAt, second.CapturedAt);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.NotEmpty(first.Workers);
        Assert.NotEmpty(first.Targets);
        Assert.Equal(first.Targets.Length, first.CurrentAssignments.Length);
        Assert.All(first.Targets, target => Assert.Contains(
            first.CurrentAssignments,
            assignment => assignment.Target == target.Identity));
        Assert.All(first.CurrentAssignments, assignment => Assert.Contains(
            first.Workers,
            worker => worker.Identity == assignment.Worker));
        Assert.All(first.Workers, worker =>
        {
            Assert.Contains(
                worker.Facts,
                fact => fact.Identity.Kind
                    == WorkforceFactKind.CandidateUniverseMembership);
            Assert.Contains(
                worker.Facts,
                fact => fact.Identity.Kind
                    == WorkforceFactKind.CurrentAssignmentMembership);
        });
        Assert.True(
            coldWatch.Elapsed <= TimeSpan.FromSeconds(30),
            $"Cold workforce snapshot took "
            + $"{coldWatch.Elapsed.TotalSeconds:F3} seconds.");
        Assert.True(
            warmWatch.Elapsed <= TimeSpan.FromSeconds(3),
            $"Warm workforce snapshot took "
            + $"{warmWatch.Elapsed.TotalSeconds:F3} seconds.");

        output.WriteLine(
            "E7-003 workforce snapshot: status={0}; workers={1}; "
            + "targets={2}; assignments={3}; diagnostics={4}; "
            + "coldMs={5:F0}; warmMs={6:F0}; guardedFiles={7}.",
            firstRead.Status,
            first.Workers.Length,
            first.Targets.Length,
            first.CurrentAssignments.Length,
            first.Diagnostics.Length,
            coldWatch.Elapsed.TotalMilliseconds,
            warmWatch.Elapsed.TotalMilliseconds,
            guardedPaths.Length);
    }

    [Fact]
    public async Task Representative_loaded_snapshot_preserves_facts_and_is_information_only()
    {
        var savePath = RequireSavePath();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SaveGames:DefaultSaveFilePath"] = savePath
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTaiwuInfrastructure();
        using var provider = services.BuildServiceProvider();
        var reader = provider
            .GetRequiredService<IVillageWorkforceSnapshotReader>();
        var builder = new BuildVillageWorkforce();
        var guardedPaths = new[]
        {
            savePath,
            Path.Combine(AppContext.BaseDirectory, "GameData.dll"),
            Path.Combine(AppContext.BaseDirectory, "GameData.Shared.dll")
        };
        Assert.All(guardedPaths, path => Assert.True(File.Exists(path)));
        var before = await CaptureAsync(guardedPaths);

        var discovery = await reader.ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);
        var snapshot = Assert.IsType<VillageWorkforceSnapshot>(
            discovery.Snapshot);
        Assert.NotEmpty(snapshot.Targets);
        var target = snapshot.Targets[0];
        var objective = new WorkforceObjectiveIdentity(
            WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
            VerifiedVillageWorkforceRules.ObjectiveVersion);
        var baseline = builder.Execute(
            discovery,
            new VillageWorkforceFinderRequest(target.Identity, objective),
            TestContext.Current.CancellationToken);
        Assert.True(baseline.HasAuthoritativeResult);
        var evaluationSet = Assert.IsType<VillageWorkforceEvaluationSet>(
            baseline.EvaluationSet);
        var shortlist = Assert.IsType<VillageWorkforceShortlist>(
            baseline.Shortlist);
        var proposed = Assert.Single(
            shortlist.Comparable
                .Where(item => item.Evaluation.Worker
                    != evaluationSet.CurrentWorker)
                .Take(1))
            .Evaluation.Worker;
        var request = new VillageWorkforceFinderRequest(
            target.Identity,
            objective,
            firstComparisonWorker: evaluationSet.CurrentWorker,
            secondComparisonWorker: proposed,
            proposedWorker: proposed);

        var first = builder.Execute(
            discovery,
            request,
            TestContext.Current.CancellationToken);
        var second = builder.Execute(
            discovery,
            request,
            TestContext.Current.CancellationToken);
        var after = await CaptureAsync(guardedPaths);

        Assert.Equal(before, after);
        Assert.True(first.Status is VillageWorkforceFinderStatus.Complete
            or VillageWorkforceFinderStatus.Partial);
        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.Snapshot?.Fingerprint, second.Snapshot?.Fingerprint);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal(
            first.EvaluationSet?.Fingerprint,
            second.EvaluationSet?.Fingerprint);
        Assert.Equal(first.Shortlist?.Fingerprint, second.Shortlist?.Fingerprint);
        Assert.Equal(first.Comparison?.Fingerprint, second.Comparison?.Fingerprint);
        Assert.Equal(first.ManualPlan?.Fingerprint, second.ManualPlan?.Fingerprint);
        Assert.Equal(
            first.Snapshot?.SourceVersions,
            second.Snapshot?.SourceVersions);
        Assert.Equal(
            first.EvaluationSet?.ResultIdentity.Target,
            second.EvaluationSet?.ResultIdentity.Target);
        Assert.Equal(
            first.EvaluationSet?.ResultIdentity.Objective,
            second.EvaluationSet?.ResultIdentity.Objective);
        Assert.Equal(
            first.EvaluationSet?.ResultIdentity.RuleVersion,
            second.EvaluationSet?.ResultIdentity.RuleVersion);
        Assert.Equal(first.Shortlist?.Counts, second.Shortlist?.Counts);
        Assert.Equal(
            first.Shortlist?.Comparable.Select(item => (
                item.CompetitionRank,
                item.Evaluation.Worker.CharacterId,
                item.Evaluation.State,
                item.Evaluation.Result?.Unit,
                item.Evaluation.Result?.Value)),
            second.Shortlist?.Comparable.Select(item => (
                item.CompetitionRank,
                item.Evaluation.Worker.CharacterId,
                item.Evaluation.State,
                item.Evaluation.Result?.Unit,
                item.Evaluation.Result?.Value)));
        Assert.Equal(first.Comparison?.Outcome, second.Comparison?.Outcome);
        Assert.Equal(
            first.ManualPlan?.Checklist.Select(item => (
                item.Kind,
                item.Category)),
            second.ManualPlan?.Checklist.Select(item => (
                item.Kind,
                item.Category)));
        Assert.NotNull(first.Comparison);
        var manualPlan = Assert.IsType<VillageWorkforceManualPlan>(
            first.ManualPlan);
        Assert.Equal(
            WorkforceAssignmentOrigin.ProposedHelper,
            manualPlan.ProposedAssignment.Origin);
        Assert.Contains(
            manualPlan.Checklist,
            item => item.Kind
                == WorkforceChecklistItemKind.NoActionWasSentToGame);
        Assert.Contains(
            manualPlan.Checklist,
            item => item.Kind
                == WorkforceChecklistItemKind.EfficiencyWasNotCalculated);
        Assert.All(
            first.Shortlist!.Comparable,
            candidate =>
            {
                Assert.Equal(
                    WorkforceUnit.BaseQualificationPoint,
                    candidate.Evaluation.Result?.Unit);
                var component = Assert.Single(
                    candidate.Evaluation.Components);
                Assert.Equal(
                    target.RequiredDiscipline,
                    component.Identity.Discipline);
            });

        output.WriteLine(
            "E7-011 representative workforce: status={0}; candidates={1}; "
            + "rankedOrTied={2}; reviewStates={3}; comparison={4}; "
            + "manualChecklist={5}; guardedFiles={6}.",
            first.Status,
            first.Shortlist.Counts.Total,
            first.Shortlist.Counts.Ranked + first.Shortlist.Counts.Tied,
            first.Shortlist.Counts.Incomplete
                + first.Shortlist.Counts.Unsupported
                + first.Shortlist.Counts.Conflicting,
            first.Comparison is not null,
            manualPlan.Checklist.Length,
            guardedPaths.Length);
    }

    private static string RequireSavePath()
    {
        var configured = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configured),
            $"E7-003 skipped: set {SavePathVariable} to a local Taiwu save.");
        var path = Path.GetFullPath(configured!);
        Assert.SkipUnless(
            File.Exists(path),
            $"E7-003 skipped: {SavePathVariable} does not identify a file.");
        return path;
    }

    private static async Task<IReadOnlyList<GuardedFile>> CaptureAsync(
        IEnumerable<string> paths)
    {
        var values = new List<GuardedFile>();
        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(
                stream,
                TestContext.Current.CancellationToken);
            values.Add(new GuardedFile(
                Path.GetFileName(fullPath),
                stream.Length,
                File.GetLastWriteTimeUtc(fullPath),
                Convert.ToHexString(hash)));
        }

        return values;
    }

    private sealed record GuardedFile(
        string Name,
        long Length,
        DateTime LastWriteTimeUtc,
        string Sha256);
}
