using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Security.Cryptography;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

public sealed class VillageWorkforceEvidenceIntegrationTests(
    ITestOutputHelper output)
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";

    [Fact]
    public async Task Shop_manager_potential_evidence_is_repeatable_and_read_only()
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
        var guardedPaths = new[]
        {
            savePath,
            Path.Combine(AppContext.BaseDirectory, "GameData.dll"),
            Path.Combine(AppContext.BaseDirectory, "GameData.Shared.dll")
        };
        Assert.All(guardedPaths, path => Assert.True(File.Exists(path)));
        var before = await CaptureAsync(guardedPaths);
        var session = provider.GetRequiredService<TaiwuArchiveReadSession>();

        var coldWatch = Stopwatch.StartNew();
        var first = await session.ReadAsync(
            savePath,
            TaiwuVillageWorkforceEvidenceProbe.Project,
            TestContext.Current.CancellationToken);
        coldWatch.Stop();
        var warmWatch = Stopwatch.StartNew();
        var second = await session.ReadAsync(
            savePath,
            TaiwuVillageWorkforceEvidenceProbe.Project,
            TestContext.Current.CancellationToken);
        warmWatch.Stop();
        var after = await CaptureAsync(guardedPaths);

        WriteEvidence(output, first, coldWatch.Elapsed, warmWatch.Elapsed,
            guardedPaths.Length);

        Assert.Equal(first, second);
        Assert.Equal(before, after);
        Assert.True(first.AreaCount > 0);
        Assert.True(first.NonEmptyBuildingCount > 0);
        Assert.True(first.AvailableWorkerCount > 0);
        Assert.True(first.ShopTargetCount > 0);
        Assert.True(first.EvaluatedPairCount > 0);
        Assert.Equal(0, first.EvaluationFailureCount);
        Assert.True(first.ManagerEntryCount >= first.CurrentManagerCount);
        Assert.True(first.ComparableTargetCount > 0);
        Assert.True(first.AlternativeEfficiencySentinelCount > 0
            || first.EfficiencyFailureCount > 0);
        if (first.EfficiencyFailureCount > 0)
        {
            Assert.StartsWith(
                "System.NullReferenceException:",
                first.EfficiencyFailureTypeProfile,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                ',',
                first.EfficiencyFailureTypeProfile);
        }

        Assert.NotNull(first.MinimumQualification);
        Assert.NotNull(first.MaximumQualification);
        Assert.True(first.DistinctQualificationCount > 1);
        Assert.True(
            coldWatch.Elapsed <= TimeSpan.FromSeconds(30),
            $"Cold village evidence probe took "
            + $"{coldWatch.Elapsed.TotalSeconds:F3} seconds.");
        Assert.True(
            warmWatch.Elapsed <= TimeSpan.FromSeconds(2),
            $"Warm village evidence probe took "
            + $"{warmWatch.Elapsed.TotalSeconds:F3} seconds.");
    }

    private static void WriteEvidence(
        ITestOutputHelper output,
        VillageWorkforceProbe evidence,
        TimeSpan coldElapsed,
        TimeSpan warmElapsed,
        int guardedFileCount)
    {
        output.WriteLine(
            "E7-000 shop-workforce evidence: gameData={0}; shared={1}; "
            + "areas={2}; buildings={3}; candidateUniverse={4}; "
            + "broadlyAvailableWorkers={5}; workRecords={6}; "
            + "workTypes={7}; shopTargets={8}; targetSkills={9}; "
            + "managedTargets={10}; managerEntries={11}; managers={12}; "
            + "unoccupiedManagerEntries={13}; evaluatedPairs={14}; "
            + "failedPairs={15}; failures={16}; comparableTargets={17}; "
            + "currentEfficiencyValues={18}; "
            + "alternativeEfficiencySentinels={19}; "
            + "efficiencyFailures={20}; efficiencyFailureTypes={21}; "
            + "qualificationRange={22}..{23}; "
            + "distinctQualification={24}; coldMs={25:F0}; "
            + "warmMs={26:F0}; guardedFiles={27}.",
            evidence.GameDataVersion,
            evidence.SharedVersion,
            evidence.AreaCount,
            evidence.NonEmptyBuildingCount,
            evidence.AvailableWorkerCount,
            evidence.BroadlyAvailableWorkerCount,
            evidence.CurrentWorkRecordCount,
            evidence.WorkTypeProfile,
            evidence.ShopTargetCount,
            evidence.TargetSkillProfile,
            evidence.ShopTargetsWithCurrentManagers,
            evidence.ManagerEntryCount,
            evidence.CurrentManagerCount,
            evidence.UnoccupiedManagerEntryCount,
            evidence.EvaluatedPairCount,
            evidence.EvaluationFailureCount,
            evidence.FailureTypeProfile,
            evidence.ComparableTargetCount,
            evidence.CurrentEfficiencyValueCount,
            evidence.AlternativeEfficiencySentinelCount,
            evidence.EfficiencyFailureCount,
            evidence.EfficiencyFailureTypeProfile,
            evidence.MinimumQualification?.ToString() ?? "none",
            evidence.MaximumQualification?.ToString() ?? "none",
            evidence.DistinctQualificationCount,
            coldElapsed.TotalMilliseconds,
            warmElapsed.TotalMilliseconds,
            guardedFileCount);
    }

    private static string RequireSavePath()
    {
        var configured = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configured),
            $"E7-000 skipped: set {SavePathVariable} to a local Taiwu save.");

        var path = Path.GetFullPath(configured!);
        Assert.SkipUnless(
            File.Exists(path),
            $"E7-000 skipped: {SavePathVariable} does not identify a file.");
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
