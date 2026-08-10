using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.LoadoutComparisons;
using TaiWu.Application.Localization;
using TaiWu.Application.TargetObservations;
using TaiWu.Application.Targets;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;
using TaiWu.Domain.TargetProfiles;
using TaiWu.Infrastructure;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

public sealed class TargetObservationReadOnlyIntegrationTests
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";
    [Fact]
    public async Task Observation_and_comparison_repeat_clear_preserve_sources()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var reader = provider.GetRequiredService<ICombatSnapshotReader>();
            var targetReader = provider.GetRequiredService<ITargetLookupReader>();
            var targetLookup = await targetReader.ReadAsync(
                new TargetLookupReadRequest(
                    savePath,
                    TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);
            var target = targetLookup.Entries
                .OrderBy(value => value.CharacterId)
                .FirstOrDefault();
            Assert.SkipUnless(
                target is not null,
                "E3-010 skipped: the current save has no target lookup entry.");
            var targetCharacterId = target!.CharacterId;
            var saveOnlyUseCase = new RecommendCombatLoadout(reader);
            var saveOnlyRequest = new RecommendCombatLoadoutRequest(
                savePath,
                targetCharacterId,
                RecommendationPolicy.Balanced,
                language: TaiwuLanguage.Chinese);
            var initial = await saveOnlyUseCase.ExecuteAsync(
                saveOnlyRequest,
                TestContext.Current.CancellationToken);
            Assert.SkipUnless(
                initial.Snapshot.Metadata.GameDataVersion.IsAvailable
                && string.Equals(
                    initial.Snapshot.Metadata.GameDataVersion.Value,
                    TargetLoadoutCompletenessEvidence.E3000GameDataVersion,
                    StringComparison.Ordinal),
                "E3-010 skipped: installed GameData does not match the "
                + "E3-000 observable-loadout rule version.");

            var saveTime = initial.Snapshot.Metadata.SaveLastWriteTimeUtc;
            var observedAt = saveTime.IsAvailable
                ? saveTime.Value.AddMinutes(1)
                : DateTimeOffset.UtcNow;
            var targetObservation = new TargetObservationRequest(
                TargetObservationContext.Sparring,
                observedAt,
                "E3-010-LOCAL-READONLY",
                TargetLoadoutCoverageKind.CompleteCurrentLoadout,
                selectedSkills: [],
                confirmPrecedenceWhenSaveTimeUnavailable: true);
            var observedRequest = new RecommendCombatLoadoutRequest(
                savePath,
                targetCharacterId,
                RecommendationPolicy.Balanced,
                language: TaiwuLanguage.Chinese,
                targetObservation: targetObservation);
            var workflow = new TargetObservationRecommendationWorkflow(
                reader,
                new UnexpectedResolver());

            var first = await workflow.ExecuteAsync(
                observedRequest,
                TestContext.Current.CancellationToken);
            var second = await workflow.ExecuteAsync(
                observedRequest,
                TestContext.Current.CancellationToken);
            var cleared = await saveOnlyUseCase.ExecuteAsync(
                saveOnlyRequest,
                TestContext.Current.CancellationToken);
            var initialProfile = TargetCombatProfileExtractor.Extract(
                initial.Snapshot,
                initial.ThreatAnalysis,
                VerifiedTargetProfileExtractionRuleSets.Initial);
            var clearedProfile = TargetCombatProfileExtractor.Extract(
                cleared.Snapshot,
                cleared.ThreatAnalysis,
                VerifiedTargetProfileExtractionRuleSets.Initial);

            Assert.Equal(targetCharacterId, first.Snapshot.Target.CharacterId);
            Assert.Equal(
                TargetLoadoutMergeStatus.Applied,
                first.TargetObservation!.Merge.Status);
            Assert.NotNull(first.TargetObservationImpact);
            Assert.Equal(ObservationSignature(first), ObservationSignature(second));
            Assert.Equal(SaveOnlySignature(initial), SaveOnlySignature(cleared));
            AssertComparisonColumns(initial);
            AssertComparisonColumns(first);
            Assert.Equal(
                ComparisonFactSignature(first),
                ComparisonFactSignature(second));
            Assert.Equal(
                ComparisonFactSignature(initial),
                ComparisonFactSignature(cleared));
            Assert.Null(cleared.TargetObservation);
            Assert.Null(cleared.TargetObservationImpact);
            Assert.Equal(
                initialProfile.Fingerprint,
                clearedProfile.Fingerprint);
            Assert.All(
                initial.Snapshot.Target.LearnedSkills,
                skill =>
                {
                    Assert.True(skill.HasConfiguredOuterDamage.IsAvailable);
                    Assert.True(
                        skill.HasConfiguredPoisonApplication.IsAvailable);
                });
            Assert.DoesNotContain(
                initialProfile.Diagnostics,
                diagnostic => diagnostic.Code ==
                    TargetCombatProfileExtractor
                        .GameDataVersionUnsupportedCode);
            Assert.Equal(
                before[savePath].Sha256,
                first.Snapshot.Metadata.SaveSha256,
                ignoreCase: true);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    private static string SaveOnlySignature(
        CombatLoadoutRecommendation value) => string.Join(
        "|",
        value.Snapshot.Metadata.SaveSha256,
        string.Join(
            ",",
            value.ThreatAnalysis.Threats.Select(threat => threat.Threat.Code)),
        string.Join(
            ";",
            value.Styles.Select(style => $"{style.Policy}:"
                + string.Join(",", style.Scoring.RankedCandidates.Select(
                    candidate => $"{candidate.Candidate.StableKey}/"
                        + candidate.TotalScore)))));

    private static string ObservationSignature(
        CombatLoadoutRecommendation value) => string.Join(
        "\n",
        SaveOnlySignature(value),
        string.Join(
            ";",
            value.TargetObservationImpact!.Threats.Select(impact =>
                $"{impact.ThreatCode}/{impact.Kind}/"
                + string.Join(",", impact.SourceKinds))),
        string.Join(
            ";",
            value.TargetObservationImpact.RecommendationChanges.Select(impact =>
                $"{impact.Policy}/{impact.Kind}/{impact.Cause}/"
                + $"{impact.SkillId}/{impact.RequiredDirection}")),
        string.Join(
            ";",
            value.TargetObservationImpact.Conflicts.Select(conflict =>
                $"{conflict.Field}/{conflict.PrecedenceRule}/"
                + string.Join(",", conflict.Sources.Select(source =>
                            $"{source.Source}/{source.CapturedAtUtc:O}")))));

    private static void AssertComparisonColumns(
        CombatLoadoutRecommendation recommendation)
    {
        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);

        Assert.Equal(4, comparison.Columns.Length);
        Assert.Equal(
            LoadoutComparisonColumnStatus.Available,
            comparison.Current.Status);
        Assert.NotNull(comparison.Current.Loadout);
        foreach (var style in recommendation.Styles)
        {
            var column = Assert.IsType<LoadoutComparisonColumn>(
                comparison.GetPolicy(style.Policy));
            if (style.ManualPlan.HasPlan)
            {
                Assert.Equal(
                    LoadoutComparisonColumnStatus.Available,
                    column.Status);
                Assert.NotNull(column.Loadout);
                Assert.NotNull(column.TacticalSummary);
            }
            else
            {
                Assert.Equal(
                    LoadoutComparisonColumnStatus.Infeasible,
                    column.Status);
                Assert.Null(column.Loadout);
                Assert.Null(column.TacticalSummary);
                Assert.NotNull(column.Diagnostic);
            }
        }
    }

    private static string ComparisonFactSignature(
        CombatLoadoutRecommendation recommendation)
    {
        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);
        List<string> facts =
        [
            $"target:{recommendation.Snapshot.Target.CharacterId}"
        ];
        facts.AddRange(comparison.BaselineProvenance.Select(value =>
            $"provenance:{value.Field}/{value.Source}"));
        foreach (var column in comparison.Columns)
        {
            facts.Add(
                $"column:{column.Kind}/{column.Status}/"
                + $"{column.Diagnostic?.Code.Value}/"
                + column.Diagnostic?.Summary);
            if (column.Loadout is not null)
            {
                facts.Add(
                    $"allocation:{column.Kind}/"
                    + ComparisonValue(column.Loadout.GenericSlotAllocation));
                foreach (var category in column.Loadout.Categories)
                {
                    var capacity = category.Capacity;
                    facts.Add(
                        $"capacity:{column.Kind}/{category.Category}/"
                        + $"{ComparisonValue(capacity.Used)}/"
                        + $"{ComparisonValue(capacity.Capacity)}/"
                        + $"{ComparisonValue(capacity.Remaining)}/"
                        + $"{ComparisonValue(capacity.CategoryContribution)}/"
                        + ComparisonValue(capacity.GenericContribution));
                    facts.AddRange(category.Skills.Select(skill =>
                        $"skill:{column.Kind}/{skill.Identity.Category}/"
                        + $"{skill.Identity.SkillId}/"
                        + $"{ComparisonValue(skill.Membership)}/"
                        + $"{ComparisonValue(skill.EffectiveCost)}/"
                        + string.Join(
                            ",",
                            skill.Actions.Select(action =>
                                $"{action.Kind}/{action.RequiredDirection}/"
                                + $"{action.Reason.Code.Value}/"
                                + $"{action.Reason.Summary}/"
                                + string.Join(
                                    "+",
                                    action.Reason.ThreatReferences.Select(
                                        value => value.Value))))));
                }
            }

            var tactical = column.TacticalSummary;
            if (tactical is null)
            {
                continue;
            }

            facts.Add(
                $"tactical:{column.Kind}/"
                + $"{ComparisonValue(tactical.ManualActionCount)}/"
                + $"{ComparisonValue(tactical.ActiveDefense)}/"
                + $"{ComparisonValue(tactical.ActiveAgility)}");
            facts.Add(
                $"covered:{column.Kind}/"
                + string.Join(",", tactical.CoveredThreats.Select(
                    value => value.Value)));
            facts.Add(
                $"unresolved:{column.Kind}/"
                + string.Join(",", tactical.UnresolvedThreats.Select(
                    value => value.Value)));
            facts.Add(
                $"conditions:{column.Kind}/"
                + string.Join(",", tactical.Conditions.Select(
                    value => value.Value)));
            facts.Add(
                $"caveats:{column.Kind}/"
                + string.Join(",", tactical.Caveats.Select(
                    value => value.Value)));
            facts.AddRange(tactical.ScoreComponents.Select(score =>
                $"score:{column.Kind}/{score.Kind}/{score.Weight}/"
                + $"{ComparisonValue(score.Score)}/{score.Explanation}"));
        }

        return string.Join("\n", facts);
    }

    private static string ComparisonValue<T>(
        LoadoutComparisonValue<T> value) where T : notnull =>
        value.IsAvailable
            ? $"available:{value.Value}"
            : $"unavailable:{value.UnavailableReason}";

    private static string RequireSavePath()
    {
        var configured = Environment.GetEnvironmentVariable(SavePathVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configured),
            $"E3-010 skipped: set {SavePathVariable} to a local Taiwu save.");
        var path = Path.GetFullPath(configured!);
        Assert.SkipUnless(
            File.Exists(path),
            $"E3-010 skipped: {SavePathVariable} does not identify a file.");
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
            "E3-010 skipped: local GameData runtime assemblies are unavailable.");
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

    private static async Task<Dictionary<string, GameOwnedFileState>>
        CaptureAsync(IEnumerable<string> paths)
    {
        var values = new Dictionary<string, GameOwnedFileState>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            var options = new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            };
            await using var stream = new FileStream(path, options);
            var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(stream));
            values.Add(
                path,
                new GameOwnedFileState(
                    stream.Length,
                    sha256,
                    File.GetLastWriteTimeUtc(path)));
        }

        return values;
    }

    private static void AssertUnchanged(
        IReadOnlyDictionary<string, GameOwnedFileState> before,
        IReadOnlyDictionary<string, GameOwnedFileState> after)
    {
        Assert.Equal(before.Keys, after.Keys);
        foreach (var path in before.Keys)
        {
            Assert.Equal(before[path], after[path]);
        }
    }

    private sealed class UnexpectedResolver : IResolveTargetSkillSelection
    {
        public Task<TargetSkillSelectionResult> ExecuteAsync(
            TargetSkillSelectionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(
                "An empty complete observation must not resolve skills.");
    }

    private sealed record GameOwnedFileState(
        long Length,
        string Sha256,
        DateTime LastWriteTimeUtc);
}
