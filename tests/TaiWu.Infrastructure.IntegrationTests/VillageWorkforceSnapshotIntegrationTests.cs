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
