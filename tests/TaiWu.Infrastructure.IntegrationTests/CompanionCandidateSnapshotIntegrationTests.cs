using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Security.Cryptography;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWu.Infrastructure.Catalogue;
using TaiWu.Infrastructure.SaveGames;
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
            }
            .Concat(TaiwuGameTextResolver.CompanionDisplayLanguagePaths(savePath))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.All(guardedPaths, path => Assert.True(File.Exists(path)));
        var before = await CaptureAsync(guardedPaths);
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
        Assert.Equal(
            VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
            firstSnapshot.SourceVersions.ProfileMappingVersion);
        Assert.Equal(
            VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion,
            firstSnapshot.SourceVersions.FingerprintSchemaVersion);
        Assert.All(firstSnapshot.Profiles, profile =>
        {
            Assert.Equal(firstSnapshot.SourceVersions, profile.SourceVersions);
            Assert.Equal(108, profile.Facts.Length);
            Assert.NotNull(profile.FindFact(new CandidateProfileFieldIdentity(
                CandidateProfileField.VillageWorkCandidateMembership)));
            Assert.All(Enum.GetValues<CandidateMainAttribute>(), attribute =>
            {
                var fact = Assert.IsType<CandidateProfileFact>(profile.FindFact(
                    new CandidateProfileFieldIdentity(
                        CandidateProfileField.BaseMainAttribute,
                        attribute)));
                Assert.Equal(CandidateEvidenceState.Confirmed, fact.State);
                var value = Assert.IsType<CandidateFactValue>(fact.Value);
                Assert.Equal(CandidateFactValueKind.Int16, value.Kind);
            });
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
                Path.Combine(AppContext.BaseDirectory, "GameData.dll"),
                Path.Combine(AppContext.BaseDirectory, "GameData.Shared.dll"),
                paths.GameDataConfigurationAssembly,
                paths.TraditionalChineseCombatSkillLanguage,
                paths.EnglishCombatSkillLanguage,
                paths.TraditionalChineseSpecialEffectLanguage,
                paths.EnglishSpecialEffectLanguage,
                paths.TraditionalChineseLegendaryBookSlotLanguage,
                paths.EnglishLegendaryBookSlotLanguage
            }
            .Concat(TaiwuGameTextResolver.CompanionDisplayLanguagePaths(savePath))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.All(guardedPaths, path => Assert.True(File.Exists(path)));
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

    [Fact]
    public async Task Companion_finder_roles_are_repeatable_bounded_and_read_only()
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
                Path.Combine(AppContext.BaseDirectory, "GameData.dll"),
                Path.Combine(AppContext.BaseDirectory, "GameData.Shared.dll"),
                paths.GameDataConfigurationAssembly,
                paths.TraditionalChineseCombatSkillLanguage,
                paths.EnglishCombatSkillLanguage,
                paths.TraditionalChineseSpecialEffectLanguage,
                paths.EnglishSpecialEffectLanguage,
                paths.TraditionalChineseLegendaryBookSlotLanguage,
                paths.EnglishLegendaryBookSlotLanguage,
                Path.Combine(
                    Path.GetDirectoryName(paths.TraditionalChineseCombatSkillLanguage)!,
                    "CombatSkillType_language.txt"),
                Path.Combine(
                    Path.GetDirectoryName(paths.EnglishCombatSkillLanguage)!,
                    "CombatSkillType_language.txt"),
                Path.Combine(
                    Path.GetDirectoryName(paths.TraditionalChineseCombatSkillLanguage)!,
                    "LifeSkillType_language.txt"),
                Path.Combine(
                    Path.GetDirectoryName(paths.EnglishCombatSkillLanguage)!,
                    "LifeSkillType_language.txt")
            }
            .Concat(TaiwuGameTextResolver.CompanionDisplayLanguagePaths(savePath))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.All(guardedPaths, path => Assert.True(File.Exists(path)));
        var before = await CaptureAsync(guardedPaths);

        try
        {
            var displaySource = provider
                .GetRequiredService<ICompanionDisciplineDisplaySource>();
            var display = await displaySource.ReadAsync(
                TestContext.Current.CancellationToken);
            Assert.True(display.Status is CompanionDisciplineDisplayStatus.Complete
                or CompanionDisciplineDisplayStatus.Partial);
            Assert.Equal(30, display.Disciplines.Length);
            Assert.All(display.Disciplines, value =>
            {
                Assert.NotNull(value.TraditionalChineseName);
                Assert.NotNull(value.EnglishName);
            });

            var workflow = new FindCompanionCandidates(
                provider.GetRequiredService<ICompanionCandidateSnapshotReader>(),
                provider.GetRequiredService<ICombatSkillDefinitionSource>(),
                provider.GetRequiredService<ICombatSkillCatalogueRepository>());
            var martialRequest = new CompanionFinderRequest(
                "MARTIAL_DISCIPLINE_APTITUDE",
                "1",
                CandidateDisciplineDomain.Martial,
                0);
            var lifeRequest = new CompanionFinderRequest(
                "LIFE_SKILL_DISCIPLINE_APTITUDE",
                "1",
                CandidateDisciplineDomain.LifeSkill,
                0);
            var capabilityRequest = new CompanionFinderRequest(
                "COMPREHENSIVE_BASE_CAPABILITY",
                "1",
                CandidateDisciplineDomain.Capability,
                0);
            var successionRequest = new CompanionFinderRequest(
                "SUCCESSION_CANDIDATE_READINESS",
                "1",
                CandidateDisciplineDomain.Capability,
                0);

            var coldWatch = Stopwatch.StartNew();
            var firstMartial = await workflow.ExecuteAsync(
                martialRequest,
                TestContext.Current.CancellationToken);
            coldWatch.Stop();
            var warmMartialWatch = Stopwatch.StartNew();
            var secondMartial = await workflow.ExecuteAsync(
                martialRequest,
                TestContext.Current.CancellationToken);
            warmMartialWatch.Stop();
            var firstLifeWatch = Stopwatch.StartNew();
            var firstLife = await workflow.ExecuteAsync(
                lifeRequest,
                TestContext.Current.CancellationToken);
            firstLifeWatch.Stop();
            var secondLifeWatch = Stopwatch.StartNew();
            var secondLife = await workflow.ExecuteAsync(
                lifeRequest,
                TestContext.Current.CancellationToken);
            secondLifeWatch.Stop();
            var firstCapabilityWatch = Stopwatch.StartNew();
            var firstCapability = await workflow.ExecuteAsync(
                capabilityRequest,
                TestContext.Current.CancellationToken);
            firstCapabilityWatch.Stop();
            var secondCapabilityWatch = Stopwatch.StartNew();
            var secondCapability = await workflow.ExecuteAsync(
                capabilityRequest,
                TestContext.Current.CancellationToken);
            secondCapabilityWatch.Stop();
            var successionWatch = Stopwatch.StartNew();
            var succession = await workflow.ExecuteAsync(
                successionRequest,
                TestContext.Current.CancellationToken);
            successionWatch.Stop();

            AssertAuthoritative(firstMartial, CandidateDisciplineDomain.Martial);
            AssertAuthoritative(secondMartial, CandidateDisciplineDomain.Martial);
            AssertAuthoritative(firstLife, CandidateDisciplineDomain.LifeSkill);
            AssertAuthoritative(secondLife, CandidateDisciplineDomain.LifeSkill);
            AssertAuthoritative(
                firstCapability,
                CandidateDisciplineDomain.Capability);
            AssertAuthoritative(
                secondCapability,
                CandidateDisciplineDomain.Capability);
            AssertAuthoritative(
                succession,
                CandidateDisciplineDomain.Capability);
            Assert.All(
                succession.Shortlist!.Entries.Where(entry =>
                    entry.Candidate.IsRanked),
                entry => Assert.Equal(
                    2,
                    entry.Evaluation.Components.Length));
            Assert.Equal(firstMartial.Fingerprint, secondMartial.Fingerprint);
            Assert.Equal(firstLife.Fingerprint, secondLife.Fingerprint);
            Assert.Equal(
                firstCapability.Fingerprint,
                secondCapability.Fingerprint);
            Assert.Equal(
                firstMartial.SourceIdentity!.CandidateSourceVersions.SaveSha256,
                firstLife.SourceIdentity!.CandidateSourceVersions.SaveSha256);
            Assert.Equal(
                firstMartial.SourceIdentity.CandidateSourceVersions.SaveSha256,
                firstCapability.SourceIdentity!.CandidateSourceVersions.SaveSha256);
            Assert.Equal(
                DisplaySignatures(firstMartial.Snapshot!),
                DisplaySignatures(secondMartial.Snapshot!));
            Assert.Equal(
                DisplaySignatures(firstLife.Snapshot!),
                DisplaySignatures(secondLife.Snapshot!));
            Assert.Equal(
                DisplaySignatures(firstCapability.Snapshot!),
                DisplaySignatures(secondCapability.Snapshot!));
            Assert.Equal(
                firstMartial.Shortlist!.Counts.Total,
                firstLife.Shortlist!.Counts.Total);
            Assert.Equal(
                firstMartial.Shortlist.Counts.Total,
                firstCapability.Shortlist!.Counts.Total);
            Assert.True(
                coldWatch.Elapsed <= TimeSpan.FromSeconds(30),
                $"Cold companion finder took {coldWatch.Elapsed.TotalSeconds:F3} seconds.");
            Assert.All(
                new[]
                {
                    warmMartialWatch.Elapsed,
                    firstLifeWatch.Elapsed,
                    secondLifeWatch.Elapsed,
                    firstCapabilityWatch.Elapsed,
                    secondCapabilityWatch.Elapsed,
                    successionWatch.Elapsed
                },
                elapsed => Assert.True(
                    elapsed <= TimeSpan.FromSeconds(2),
                    $"Warm companion finder took {elapsed.TotalSeconds:F3} seconds."));

            output.WriteLine(
                "E6-014 companion finder: martial={0}; life={1}; "
                + "capability={2}; succession={3}; candidates={4}; "
                + "disciplines={5}; coldMs={6:F0}; warmMartialMs={7:F0}; "
                + "warmLifeMs={8:F0}/{9:F0}; warmCapabilityMs={10:F0}/{11:F0}; "
                + "warmSuccessionMs={12:F0}; guardedFiles={13}.",
                firstMartial.Status,
                firstLife.Status,
                firstCapability.Status,
                succession.Status,
                firstMartial.Shortlist.Counts.Total,
                display.Disciplines.Length,
                coldWatch.Elapsed.TotalMilliseconds,
                warmMartialWatch.Elapsed.TotalMilliseconds,
                firstLifeWatch.Elapsed.TotalMilliseconds,
                secondLifeWatch.Elapsed.TotalMilliseconds,
                firstCapabilityWatch.Elapsed.TotalMilliseconds,
                secondCapabilityWatch.Elapsed.TotalMilliseconds,
                successionWatch.Elapsed.TotalMilliseconds,
                guardedPaths.Length);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            Assert.Equal(before, after);
        }
    }

    private static void AssertAuthoritative(
        CompanionFinderResult result,
        CandidateDisciplineDomain expectedDomain)
    {
        Assert.True(result.Status is CompanionFinderStatus.Complete
            or CompanionFinderStatus.Partial);
        Assert.True(result.HasAuthoritativeResult);
        Assert.NotNull(result.Fingerprint);
        Assert.Equal(expectedDomain, result.SourceIdentity!.Discipline.Domain);
        Assert.NotEmpty(result.Snapshot!.Profiles);
        Assert.Equal(result.Snapshot.Profiles.Length, result.Shortlist!.Counts.Total);
        Assert.Contains(result.Shortlist.Entries, entry => entry.Candidate.IsRanked);
    }

    private static IEnumerable<(int CharacterId, string? ChineseName,
        string? EnglishName, string? ChineseLocation, string? EnglishLocation)>
        DisplaySignatures(CompanionCandidateSnapshot snapshot) =>
        snapshot.Displays.Select(value => (
            value.Identity.CharacterId,
            value.TraditionalChineseName,
            value.EnglishName,
            value.TraditionalChineseLocation,
            value.EnglishLocation));

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
