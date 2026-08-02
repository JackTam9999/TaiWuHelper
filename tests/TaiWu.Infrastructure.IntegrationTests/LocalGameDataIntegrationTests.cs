using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.Localization;
using TaiWu.Application.Targets;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

public sealed class LocalGameDataIntegrationTests
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";
    private const int GoldenPlayerId = 21396;
    private const int GoldenTargetId = 16317;

    [Fact]
    public void Proprietary_sources_are_not_embedded_in_the_test_assembly()
    {
        var resources = typeof(LocalGameDataIntegrationTests)
            .Assembly
            .GetManifestResourceNames();

        Assert.DoesNotContain(
            resources,
            resource => Path.GetExtension(resource).Equals(
                            ".sav",
                            StringComparison.OrdinalIgnoreCase)
                        || resource.Contains(
                            "GameData",
                            StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Golden_snapshot_is_repeatable_and_preserves_source_files()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverGameOwnedReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var reader = provider.GetRequiredService<ICombatSnapshotReader>();
            var targetReader =
                provider.GetRequiredService<ITargetLookupReader>();
            var request = new CombatSnapshotReadRequest(
                savePath,
                GoldenTargetId,
                language: TaiwuLanguage.Chinese);

            var first = await reader.ReadAsync(
                request,
                TestContext.Current.CancellationToken);
            var second = await reader.ReadAsync(
                request,
                TestContext.Current.CancellationToken);
            var targetLookup = await targetReader.ReadAsync(
                new TargetLookupReadRequest(
                    savePath,
                    TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);

            AssertGoldenSnapshot(first, savePath);
            AssertGoldenSnapshot(second, savePath);
            AssertLocalizedNames(first);
            AssertLocalizedLocation(targetLookup);
            AssertRepeatable(first, second);
            Assert.True(
                string.Equals(
                    before[savePath].Sha256,
                    first.Metadata.SaveSha256,
                    StringComparison.OrdinalIgnoreCase),
                "The snapshot fingerprint did not match the guarded save.");
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    private static string RequireSavePath()
    {
        var configuredPath =
            Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configuredPath),
            $"M1-024 skipped: set {SavePathVariable} to a local Taiwu save.");

        var fullPath = Path.GetFullPath(configuredPath!);
        Assert.SkipUnless(
            File.Exists(fullPath),
            $"M1-024 skipped: {SavePathVariable} does not identify "
            + "an existing file.");
        return fullPath;
    }

    private static string[] DiscoverGameOwnedReadDependencies(string savePath)
    {
        var paths = Directory
            .EnumerateFiles(
                AppContext.BaseDirectory,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(IsGameRuntimeFile)
            .Append(savePath)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.SkipWhen(
            paths.Length == 1,
            "M1-024 skipped: the local GameData runtime assemblies "
            + "are unavailable.");
        return paths;
    }

    private static bool IsGameRuntimeFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.StartsWith(
                   "GameData",
                   StringComparison.OrdinalIgnoreCase)
               && Path.GetExtension(fileName).Equals(
                   ".dll",
                   StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("0Harmony.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("e_sqlite3.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals(
                "steam_api64.dll",
                StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, GameOwnedFileState>>
        CaptureAsync(IEnumerable<string> paths)
    {
        var result = new Dictionary<string, GameOwnedFileState>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            var options = new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options =
                    FileOptions.Asynchronous | FileOptions.SequentialScan
            };

            await using var stream = new FileStream(path, options);
            var hash = await SHA256.HashDataAsync(stream);
            result.Add(
                path,
                new GameOwnedFileState(
                    stream.Length,
                    Convert.ToHexString(hash),
                    File.GetLastWriteTimeUtc(path)));
        }

        return result;
    }

    private static void AssertGoldenSnapshot(
        Domain.CombatSnapshots.CombatSnapshot snapshot,
        string expectedSavePath)
    {
        Assert.Equal(GoldenPlayerId, snapshot.Player.CharacterId);
        Assert.Equal(GoldenTargetId, snapshot.Target.CharacterId);
        Assert.True(snapshot.Target.Age.IsAvailable);
        Assert.InRange(snapshot.Target.Age.Value, 1, 200);
        Assert.True(
            string.Equals(
                Path.GetFullPath(snapshot.Metadata.SavePath),
                expectedSavePath,
                StringComparison.OrdinalIgnoreCase),
            "The snapshot did not retain the configured save source.");
    }

    private static void AssertRepeatable(
        Domain.CombatSnapshots.CombatSnapshot first,
        Domain.CombatSnapshots.CombatSnapshot second)
    {
        Assert.True(
            string.Equals(
                first.Metadata.SaveSha256,
                second.Metadata.SaveSha256,
                StringComparison.Ordinal),
            "Consecutive reads produced different save fingerprints.");
        Assert.Equal(
            first.Player.LearnedSkills.Select(skill => skill.SkillId),
            second.Player.LearnedSkills.Select(skill => skill.SkillId));
        Assert.Equal(
            first.Target.LearnedSkills.Select(skill => skill.SkillId),
            second.Target.LearnedSkills.Select(skill => skill.SkillId));
        Assert.Equal(first.Target.Age, second.Target.Age);
        Assert.Equal(
            first.Warnings.Select(warning => warning.Code),
            second.Warnings.Select(warning => warning.Code));
    }

    private static void AssertLocalizedNames(
        Domain.CombatSnapshots.CombatSnapshot snapshot)
    {
        Assert.True(snapshot.Target.DisplayName.IsAvailable);
        Assert.Equal("葛貴嬋", snapshot.Target.DisplayName.Value);

        var firstNeigong = Assert.Single(
            snapshot.Player.LearnedSkills,
            skill => skill.SkillId == 0);
        Assert.True(firstNeigong.DisplayName.IsAvailable);
        Assert.Equal("沛然訣", firstNeigong.DisplayName.Value);
    }

    private static void AssertLocalizedLocation(
        TargetLookupSnapshot snapshot)
    {
        var localizedLocation = Assert.IsType<string>(
            snapshot.Entries
                .Select(entry => entry.LocationDisplayName)
                .FirstOrDefault(
                    value => !string.IsNullOrWhiteSpace(value)));
        var components = localizedLocation.Split(
            " · ",
            StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);
        Assert.Equal(3, components.Length);
        Assert.All(
            components,
            component => Assert.DoesNotContain("_", component));
    }

    private static void AssertUnchanged(
        Dictionary<string, GameOwnedFileState> before,
        Dictionary<string, GameOwnedFileState> after)
    {
        var changedFiles = before
            .Where(pair => !after.TryGetValue(pair.Key, out var current)
                           || current != pair.Value)
            .Select(pair => Path.GetFileName(pair.Key))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            changedFiles.Length == 0,
            "Game-owned read dependencies changed during the test: "
            + string.Join(", ", changedFiles));
    }

    private sealed record GameOwnedFileState(
        long Length,
        string Sha256,
        DateTime LastWriteTimeUtc);
}
