using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Security.Cryptography;
using TaiWu.Domain.CombatEffects;
using TaiWu.Infrastructure.Catalogue;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

[Collection(TaiwuArchivePerformanceCollection.Name)]
public sealed class TacticalCombatEvidenceIntegrationTests(
    ITestOutputHelper output)
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";
    private const string E8000GameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";

    [Fact]
    public async Task Tactical_sources_are_repeatable_guarded_and_version_gated()
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
        var session = provider.GetRequiredService<TaiwuArchiveReadSession>();
        var guardedPaths = DiscoverGuardedPaths(savePath);
        var before = await CaptureAsync(guardedPaths);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            session.ReadAsync(
                savePath,
                TaiwuTacticalCombatEvidenceProbe.Project,
                cancelled.Token));

        var coldWatch = Stopwatch.StartNew();
        var first = await session.ReadAsync(
            savePath,
            TaiwuTacticalCombatEvidenceProbe.Project,
            TestContext.Current.CancellationToken);
        coldWatch.Stop();
        var warmWatch = Stopwatch.StartNew();
        var second = await session.ReadAsync(
            savePath,
            TaiwuTacticalCombatEvidenceProbe.Project,
            TestContext.Current.CancellationToken);
        warmWatch.Stop();
        var after = await CaptureAsync(guardedPaths);

        Assert.SkipUnless(
            string.Equals(
                first.GameDataVersion,
                E8000GameDataVersion,
                StringComparison.Ordinal),
            "E8-000 skipped: installed GameData differs from the evidence "
            + "version.");
        Assert.Equal(first, second);
        Assert.Equal(before, after);
        Assert.NotEqual(
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion,
            first.GameDataVersion);
        Assert.True(first.LearnedSkillCount > 0);
        Assert.True(first.EquippedSkillCount > 0);
        Assert.Equal(
            first.CandidateExpectationCount,
            first.ConfiguredCandidateCount);
        Assert.Equal(
            first.CandidateExpectationCount,
            first.MatchingCandidateDefinitionCount);
        Assert.True(first.LearnedCandidateCount >= 3);
        Assert.True(first.EquippedCandidateCount >= 3);
        Assert.True(first.RequiredDirectionReadyCandidateCount >= 3);
        Assert.Equal(
            first.MagicSoundExpectationCount,
            first.ConfiguredMagicSoundDefinitionCount);
        Assert.Equal(
            first.MagicSoundExpectationCount,
            first.MatchingMagicSoundDefinitionCount);
        Assert.True(first.ResetDefinitionMatches);
        Assert.True(first.EquippedWeaponCount > 0);
        Assert.Equal(
            first.EquippedWeaponCount,
            first.AvailableWeaponSubtypeCount);
        Assert.Equal(4, first.GenericAllocationValueCount);
        Assert.True(first.AssignedGenericSlotCount >= 0);
        Assert.True(first.LegendaryBookAssignmentCount >= 0);
        Assert.True(
            coldWatch.Elapsed <= TimeSpan.FromSeconds(30),
            $"Cold tactical evidence probe took "
            + $"{coldWatch.Elapsed.TotalSeconds:F3} seconds.");
        Assert.True(
            warmWatch.Elapsed <= TimeSpan.FromSeconds(3),
            $"Warm tactical evidence probe took "
            + $"{warmWatch.Elapsed.TotalSeconds:F3} seconds.");

        output.WriteLine(
            "E8-000 tactical evidence: gameData={0}; legacyRulesMatch={1}; "
            + "learnedSkills={2}; equippedSkills={3}; candidateDefinitions="
            + "{4}/{5}; learnedCandidates={6}; equippedCandidates={7}; "
            + "directionReadyCandidates={8}; magicSoundDefinitions={9}/{10}; "
            + "resetDefinitionMatch={11}; equippedWeapons={12}; "
            + "weaponSubtypes={13}; genericAllocationValues={14}; "
            + "assignedGenericSlots={15}; legendaryAssignments={16}; "
            + "loadWarning={17}; cancellation=observed; coldMs={18:F0}; "
            + "warmMs={19:F0}; guardedFiles={20}.",
            first.GameDataVersion,
            string.Equals(
                first.GameDataVersion,
                VerifiedCombatEffectCatalogs.GoldenGameDataVersion,
                StringComparison.Ordinal),
            first.LearnedSkillCount,
            first.EquippedSkillCount,
            first.MatchingCandidateDefinitionCount,
            first.CandidateExpectationCount,
            first.LearnedCandidateCount,
            first.EquippedCandidateCount,
            first.RequiredDirectionReadyCandidateCount,
            first.MatchingMagicSoundDefinitionCount,
            first.MagicSoundExpectationCount,
            first.ResetDefinitionMatches,
            first.EquippedWeaponCount,
            first.AvailableWeaponSubtypeCount,
            first.GenericAllocationValueCount,
            first.AssignedGenericSlotCount,
            first.LegendaryBookAssignmentCount,
            first.HasLoadWarning,
            coldWatch.Elapsed.TotalMilliseconds,
            warmWatch.Elapsed.TotalMilliseconds,
            guardedPaths.Length);
    }

    private static string RequireSavePath()
    {
        var configured = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configured),
            $"E8-000 skipped: set {SavePathVariable} to a local Taiwu save.");
        var path = Path.GetFullPath(configured!);
        Assert.SkipUnless(
            File.Exists(path),
            $"E8-000 skipped: {SavePathVariable} does not identify a file.");
        return path;
    }

    private static string[] DiscoverGuardedPaths(string savePath)
    {
        var catalogueSources = new TaiwuCatalogueSourcePathProvider().Resolve();
        Assert.SkipUnless(
            catalogueSources.IsAvailable,
            "E8-000 skipped: installed catalogue sources are unavailable.");
        var paths = catalogueSources.Paths!;
        return new[]
        {
            savePath,
            Path.Combine(AppContext.BaseDirectory, "GameData.dll"),
            Path.Combine(AppContext.BaseDirectory, "GameData.Shared.dll"),
            paths.TraditionalChineseCombatSkillLanguage,
            paths.EnglishCombatSkillLanguage,
            paths.TraditionalChineseSpecialEffectLanguage,
            paths.EnglishSpecialEffectLanguage
        }
        .Select(Path.GetFullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    }

    private static async Task<IReadOnlyList<GuardedFile>> CaptureAsync(
        IEnumerable<string> paths)
    {
        var values = new List<GuardedFile>();
        foreach (var path in paths)
        {
            Assert.True(File.Exists(path), $"Guarded source missing: {path}");
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await SHA256.HashDataAsync(
                stream,
                TestContext.Current.CancellationToken);
            values.Add(new GuardedFile(
                Path.GetFileName(path),
                stream.Length,
                File.GetLastWriteTimeUtc(path),
                Convert.ToHexString(hash)));
        }

        return values;
    }

    private sealed record GuardedFile(
        string Name,
        long Length,
        DateTime LastWriteUtc,
        string Sha256);
}
