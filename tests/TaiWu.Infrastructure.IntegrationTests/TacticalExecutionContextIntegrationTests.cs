using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.Localization;
using TaiWu.Application.TacticalCombat;
using TaiWu.Application.Targets;
using TaiWu.Domain.TacticalCombat;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

[Collection(TaiwuArchivePerformanceCollection.Name)]
public sealed class TacticalExecutionContextIntegrationTests
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";

    [Fact]
    public async Task Context_reads_are_repeatable_cancelable_and_read_only()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);

        await using var provider = new ServiceCollection()
            .AddTaiwuInfrastructure()
            .BuildServiceProvider();
        var targetLookup = await provider
            .GetRequiredService<ITargetLookupReader>()
            .ReadAsync(
                new TargetLookupReadRequest(
                    savePath,
                    TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);
        var target = targetLookup.Entries
            .OrderBy(item => item.CharacterId)
            .FirstOrDefault();
        Assert.SkipUnless(
            target is not null,
            "E8-004 skipped: the configured save has no target entry.");

        var useCase = provider.GetRequiredService<
            IReadTacticalExecutionContext>();
        var request = new TacticalExecutionContextReadRequest(
            new CombatSnapshotReadRequest(
                savePath,
                target!.CharacterId,
                language: TaiwuLanguage.Chinese),
            VerifiedTacticalCombatRuleSets.HistoricalMagicSound
                .SupportedTargetGoalCodes,
            evidence: []);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync(request, cancelled.Token));

        var first = await useCase.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var second = await useCase.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var after = await CaptureAsync(guardedPaths);

        Assert.Equal(before, after);
        Assert.Equal(
            first.Context.SourceRevisionFingerprint,
            second.Context.SourceRevisionFingerprint);
        Assert.Equal(
            first.Context.ObservationRevisionFingerprint,
            second.Context.ObservationRevisionFingerprint);
        Assert.Equal(
            first.Context.SemanticFingerprint,
            second.Context.SemanticFingerprint);
        Assert.True(first.Context.GameDataVersion.IsAvailable);
        Assert.Equal(
            first.Context.GameDataVersion.Value,
            second.Context.GameDataVersion.Value);
        Assert.False(first.Context.HasCompatibleRules);
        Assert.Empty(first.Context.ResolvedRules);
    }

    private static string RequireSavePath()
    {
        var configured = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configured),
            $"E8-004 skipped: set {SavePathVariable} to a local Taiwu save.");
        var path = Path.GetFullPath(configured!);
        Assert.SkipUnless(
            File.Exists(path),
            $"E8-004 skipped: {SavePathVariable} does not identify a file.");
        return path;
    }

    private static string[] DiscoverReadDependencies(string savePath)
    {
        var catalogueSources = new TaiwuCatalogueSourcePathProvider().Resolve();
        var languagePaths = catalogueSources.IsAvailable
            ? new[]
            {
                catalogueSources.Paths!.TraditionalChineseCombatSkillLanguage,
                catalogueSources.Paths.EnglishCombatSkillLanguage,
                catalogueSources.Paths.TraditionalChineseUiLanguage,
                catalogueSources.Paths.EnglishUiLanguage
            }
            : [];
        var paths = Directory
            .EnumerateFiles(
                AppContext.BaseDirectory,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(IsGameRuntimeFile)
            .Append(savePath)
            .Concat(languagePaths.Where(File.Exists))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.SkipWhen(
            paths.Length == 1,
            "E8-004 skipped: local GameData runtime assemblies are unavailable.");
        return paths;
    }

    private static bool IsGameRuntimeFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.StartsWith("GameData", StringComparison.OrdinalIgnoreCase)
               && Path.GetExtension(fileName).Equals(
                   ".dll",
                   StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("0Harmony.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("e_sqlite3.dll", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("steam_api64.dll", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<IReadOnlyList<GuardedFileState>> CaptureAsync(
        IEnumerable<string> paths)
    {
        List<GuardedFileState> values = [];
        foreach (var path in paths)
        {
            var options = new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous
                    | FileOptions.SequentialScan
            };
            await using var stream = new FileStream(path, options);
            var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(
                stream,
                TestContext.Current.CancellationToken));
            values.Add(new GuardedFileState(
                Path.GetFileName(path),
                stream.Length,
                sha256,
                File.GetLastWriteTimeUtc(path)));
        }

        return values;
    }

    private sealed record GuardedFileState(
        string Name,
        long Length,
        string Sha256,
        DateTime LastWriteTimeUtc);
}
