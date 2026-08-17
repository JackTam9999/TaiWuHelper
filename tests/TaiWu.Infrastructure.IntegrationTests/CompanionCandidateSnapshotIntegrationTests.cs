using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Security.Cryptography;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Infrastructure;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

public sealed class CompanionCandidateSnapshotIntegrationTests(
    ITestOutputHelper output)
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";

    [Fact]
    public async Task Candidate_snapshot_is_one_revision_repeatable_bounded_and_read_only()
    {
        var savePath = RequireSavePath();
        var guardedPaths = new[]
        {
            savePath,
            Path.Combine(AppContext.BaseDirectory, "GameData.dll"),
            Path.Combine(AppContext.BaseDirectory, "GameData.Shared.dll")
        };
        var before = await CaptureAsync(guardedPaths);
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
        var reader = provider.GetRequiredService<ICompanionCandidateSnapshotReader>();

        var coldWatch = Stopwatch.StartNew();
        var first = await reader.ReadAsync(
            CompanionCandidateSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);
        coldWatch.Stop();
        var warmWatch = Stopwatch.StartNew();
        var second = await reader.ReadAsync(
            CompanionCandidateSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);
        warmWatch.Stop();
        var after = await CaptureAsync(guardedPaths);
        output.WriteLine(
            "E6-004 production snapshot: status={0}; profiles={1}; facts={2}; "
            + "omissions={3}; warnings={4}; coldMs={5:F0}; warmMs={6:F0}; "
            + "guardedFiles={7}.",
            first.Status,
            first.Snapshot?.Profiles.Length ?? 0,
            first.Snapshot?.Profiles.Sum(profile => profile.Facts.Length) ?? 0,
            first.Snapshot?.Omissions.Length ?? 0,
            first.Snapshot?.Warnings.Length ?? 0,
            coldWatch.Elapsed.TotalMilliseconds,
            warmWatch.Elapsed.TotalMilliseconds,
            guardedPaths.Length);

        Assert.True(
            first.Status is CompanionCandidateSnapshotReadStatus.Complete
                or CompanionCandidateSnapshotReadStatus.Partial);
        Assert.Equal(first.Status, second.Status);
        var firstSnapshot = Assert.IsType<CompanionCandidateSnapshot>(first.Snapshot);
        var secondSnapshot = Assert.IsType<CompanionCandidateSnapshot>(second.Snapshot);
        Assert.NotEmpty(firstSnapshot.Profiles);
        Assert.Equal(
            firstSnapshot.Profiles.Select(profile => profile.Fingerprint),
            secondSnapshot.Profiles.Select(profile => profile.Fingerprint));
        Assert.Equal(
            firstSnapshot.SourceVersions.SaveSha256,
            secondSnapshot.SourceVersions.SaveSha256);
        Assert.All(firstSnapshot.Profiles, profile =>
        {
            Assert.Equal(firstSnapshot.SourceVersions, profile.SourceVersions);
            Assert.NotNull(profile.FindFact(new CandidateProfileFieldIdentity(
                CandidateProfileField.BaseMartialQualification,
                new CandidateDisciplineIdentity(CandidateDisciplineDomain.Martial, 0))));
            Assert.Equal(
                CandidateEvidenceState.Unsupported,
                profile.FindFact(new CandidateProfileFieldIdentity(
                    CandidateProfileField.CurrentMartialQualification,
                    new CandidateDisciplineIdentity(
                        CandidateDisciplineDomain.Martial,
                        0)))!.State);
        });
        Assert.True(
            coldWatch.Elapsed <= TimeSpan.FromSeconds(30),
            $"Cold candidate snapshot took {coldWatch.Elapsed.TotalSeconds:F3} seconds.");
        Assert.True(
            warmWatch.Elapsed <= TimeSpan.FromSeconds(2),
            $"Warm candidate snapshot took {warmWatch.Elapsed.TotalSeconds:F3} seconds.");
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Candidate_enrichment_is_repeatable_versioned_and_read_only()
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
        var paths = provider
            .GetRequiredService<ITaiwuCatalogueSourcePathProvider>()
            .Resolve()
            .Paths;
        Assert.NotNull(paths);
        var guardedPaths = new[]
        {
            savePath,
            paths.GameDataConfigurationAssembly,
            paths.TraditionalChineseCombatSkillLanguage,
            paths.EnglishCombatSkillLanguage,
            paths.TraditionalChineseSpecialEffectLanguage,
            paths.EnglishSpecialEffectLanguage,
            paths.TraditionalChineseLegendaryBookSlotLanguage,
            paths.EnglishLegendaryBookSlotLanguage
        };
        var before = await CaptureAsync(guardedPaths);
        var snapshotResult = await provider
            .GetRequiredService<ICompanionCandidateSnapshotReader>()
            .ReadAsync(
                CompanionCandidateSnapshotReadRequest.Current,
                TestContext.Current.CancellationToken);
        var snapshot = Assert.IsType<CompanionCandidateSnapshot>(
            snapshotResult.Snapshot);
        var useCase = new EnrichCompanionCandidateProfiles(
            provider.GetRequiredService<ICombatSkillDefinitionSource>(),
            provider.GetRequiredService<ICombatSkillCatalogueRepository>());

        var first = await useCase.ExecuteAsync(
            snapshot,
            TestContext.Current.CancellationToken);
        var second = await useCase.ExecuteAsync(
            snapshot,
            TestContext.Current.CancellationToken);
        var after = await CaptureAsync(guardedPaths);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.CatalogueStatus, second.CatalogueStatus);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.False(
            first.Status is CompanionCandidateEnrichmentStatus.CatalogueUnsupported
                or CompanionCandidateEnrichmentStatus.CatalogueFailed);
        Assert.Equal(snapshot.Profiles.Length, first.Candidates.Length);
        Assert.All(first.Candidates, candidate =>
        {
            Assert.Contains(candidate.Profile, snapshot.Profiles);
            Assert.Equal(
                snapshot.Profiles.Single(profile =>
                    profile.Identity == candidate.Profile.Identity).Fingerprint,
                candidate.Profile.Fingerprint);
            Assert.All(candidate.CombatSkills, skill => Assert.Equal(
                CompanionDetailedProgressState.NotRequestedByApprovedRole,
                skill.DetailedProgressState));
        });
        Assert.Equal(before, after);
        output.WriteLine(
            "E6-005 production enrichment: status={0}; catalogue={1}; "
            + "candidates={2}; skills={3}; guardedFiles={4}.",
            first.Status,
            first.CatalogueStatus,
            first.Candidates.Length,
            first.Candidates.Sum(candidate => candidate.CombatSkills.Length),
            guardedPaths.Length);
    }

    private static string RequireSavePath()
    {
        var configured = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configured),
            $"E6-004 skipped: set {SavePathVariable} to a local Taiwu save.");

        var path = Path.GetFullPath(configured!);
        Assert.SkipUnless(
            File.Exists(path),
            $"E6-004 skipped: {SavePathVariable} does not identify a file.");

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
