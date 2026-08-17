using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.Localization;
using TaiWu.Application.RegionStories;
using TaiWu.Application.SaveGames;
using TaiWu.Application.Targets;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.SaveGames;
using TaiWu.Infrastructure.Catalogue;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.IntegrationTests;

public sealed class LocalGameDataIntegrationTests
{
    private const string SavePathVariable = "TAIWU_INTEGRATION_SAVE_PATH";
    private const string CatalogueVariable =
        "TAIWU_INTEGRATION_SKILL_CATALOGUE";
    private const string GameDirectoryVariable = "TAIWU_GAME_DIRECTORY";
    private const string Epic2GoldenSaveSha256 =
        "C9EB00A368A6CE25B2D816DAE941AFAC67B6217ED561FF7563F613C3B297CECA";
    private const string Epic2CharacterProgressGoldenSaveSha256 =
        "9C30C00CF1ABD05973435B14B724A0A41A1B0DCD7847A8CA04D4E60E2B53C916";
    private const string RegionStoryGoldenSaveSha256 =
        "948BFD358AD4F952683B5BFB2A719B9890E6E7DCEB415DD0222B4CDF2DCF2F16";
    private const string StoryTargetGoldenSaveSha256 =
        "BCDD5C416779AA2A4557BACDD9F4940AFD795359BF21586D256926A57C437C24";
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
    public async Task Bilingual_catalogue_import_is_repeatable_and_read_only()
    {
        var gameDirectory = RequireGameDirectoryForCatalogue();
        var guardedPaths = CatalogueSourcePaths(gameDirectory);
        var before = await CaptureAsync(guardedPaths);

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var source =
                provider.GetRequiredService<ICombatSkillDefinitionSource>();
            var factionProfileSource = provider.GetRequiredService<
                ICombatSkillFactionProfileSource>();

            var first = await source.ReadAsync(
                TestContext.Current.CancellationToken);
            var second = await source.ReadAsync(
                TestContext.Current.CancellationToken);
            var firstFactionProfiles = await factionProfileSource.ReadAsync(
                TestContext.Current.CancellationToken);
            var secondFactionProfiles = await factionProfileSource.ReadAsync(
                TestContext.Current.CancellationToken);

            Assert.Equal(DefinitionSourceReadStatus.Available, first.Status);
            Assert.Equal(DefinitionSourceReadStatus.Available, second.Status);
            Assert.NotNull(first.SourceIdentity);
            Assert.Equal(first.SourceIdentity, second.SourceIdentity);
            Assert.Equal(
                TaiwuCombatSkillDefinitionSource.ImporterVersion,
                first.SourceIdentity!.ImporterVersion);
            Assert.True(first.Definitions.Length > 0);
            Assert.Equal(
                first.Definitions.Select(definition => definition.SkillId),
                second.Definitions.Select(definition => definition.SkillId));
            Assert.Equal(
                first.Definitions
                    .Select(definition => definition.SkillId)
                    .Order(),
                first.Definitions.Select(definition => definition.SkillId));
            Assert.Equal(
                first.Definitions.Length,
                first.Definitions.Select(definition => definition.SkillId)
                    .Distinct()
                    .Count());
            Assert.Equal(84, first.LegendaryBookEffects.Length);
            Assert.Equivalent(
                first.LegendaryBookEffects,
                second.LegendaryBookEffects);
            var bladeCounterBreak = Assert.Single(
                first.LegendaryBookEffects,
                effect => effect.EffectId == 83);
            var bladeCounterBreakCnh = bladeCounterBreak.Find(
                CatalogueLanguage.TraditionalChinese)!;
            Assert.Equal("解破", bladeCounterBreakCnh.Name);
            Assert.DoesNotContain(
                "施展此功法所需的時間提高50%",
                bladeCounterBreakCnh.Description);

            var golden = Assert.Single(
                first.Definitions,
                definition => definition.SkillId == 456);
            Assert.Equal(
                "黑血蠱降",
                golden.Names.Get(CatalogueLanguage.TraditionalChinese)
                    .Value.Text);
            Assert.Equal(
                "Corruptive Gu Infection",
                golden.Names.Get(CatalogueLanguage.English).Value.Text);
            Assert.All(
                new[]
                {
                    (RawCombatSkillDescriptionKind.DirectEffect,
                        CatalogueLanguage.TraditionalChinese),
                    (RawCombatSkillDescriptionKind.DirectEffect,
                        CatalogueLanguage.English),
                    (RawCombatSkillDescriptionKind.ReverseEffect,
                        CatalogueLanguage.TraditionalChinese),
                    (RawCombatSkillDescriptionKind.ReverseEffect,
                        CatalogueLanguage.English)
                },
                expected => Assert.Contains(
                    golden.RawDescriptions,
                    description =>
                        description.Kind == expected.Item1
                        && description.Language == expected.Item2
                        && !string.IsNullOrWhiteSpace(description.Text)));
            Assert.Equal(
                before[guardedPaths[0]].Sha256,
                first.SourceIdentity!.GameDataFingerprint,
                ignoreCase: true);
            Assert.Equal(
                before[guardedPaths[1]].Sha256,
                first.SourceIdentity.TraditionalChineseFingerprint,
                ignoreCase: true);
            Assert.Equal(
                before[guardedPaths[2]].Sha256,
                first.SourceIdentity.EnglishFingerprint,
                ignoreCase: true);
            Assert.Equal(
                before[guardedPaths[3]].Sha256,
                first.SourceIdentity.TraditionalChineseSpecialEffectFingerprint,
                ignoreCase: true);
            Assert.Equal(
                before[guardedPaths[4]].Sha256,
                first.SourceIdentity.EnglishSpecialEffectFingerprint,
                ignoreCase: true);
            Assert.Equal(
                before[guardedPaths[5]].Sha256,
                first.SourceIdentity.TraditionalChineseLegendaryBookFingerprint,
                ignoreCase: true);
            Assert.Equal(
                before[guardedPaths[6]].Sha256,
                first.SourceIdentity.EnglishLegendaryBookFingerprint,
                ignoreCase: true);
            Assert.DoesNotContain(
                first.Diagnostics,
                diagnostic => diagnostic.Severity
                    == CombatSkillImportDiagnosticSeverity.Error);
            Assert.NotEmpty(firstFactionProfiles);
            Assert.Equal(firstFactionProfiles, secondFactionProfiles);
            Assert.Equal(
                firstFactionProfiles.Select(profile => profile.Faction.Value),
                firstFactionProfiles
                    .Select(profile => profile.Faction.Value)
                    .Order());
            Assert.Equal(
                firstFactionProfiles.Count,
                firstFactionProfiles
                    .Select(profile => profile.Faction.Value)
                    .Distinct()
                    .Count());

            AssertFactionProfile(
                firstFactionProfiles,
                1,
                CombatSkillElement.Metal,
                CombatSkillFactionAlignment.Kind);
            AssertFactionProfile(
                firstFactionProfiles,
                4,
                CombatSkillElement.Fire,
                CombatSkillFactionAlignment.Even);
            AssertFactionProfile(
                firstFactionProfiles,
                15,
                CombatSkillElement.Earth,
                CombatSkillFactionAlignment.Egoistic);

            var helperRoot = Path.Combine(
                Path.GetTempPath(),
                $"taiwu-catalogue-integration-{Guid.NewGuid():N}");
            try
            {
                var store = new SqliteCombatSkillCatalogueStore(
                    new CatalogueStoragePathProvider(
                        helperRoot,
                        [gameDirectory]));
                Assert.True((await store.ReplaceAsync(
                    first.SourceIdentity,
                    first.Definitions,
                    first.Diagnostics,
                    first.LegendaryBookEffects,
                    TestContext.Current.CancellationToken)).Succeeded);
                var firstStored = await store.QueryAsync(
                    new CombatSkillCatalogueFilter(),
                    TestContext.Current.CancellationToken);
                var legendaryBookStore =
                    (ILegendaryBookEffectCatalogueRepository)store;
                var firstStoredLegendaryBookEffects =
                    await legendaryBookStore.QueryAsync(
                        TestContext.Current.CancellationToken);
                Assert.True((await store.ReplaceAsync(
                    second.SourceIdentity!,
                    second.Definitions,
                    second.Diagnostics,
                    second.LegendaryBookEffects,
                    TestContext.Current.CancellationToken)).Succeeded);
                var secondStored = await store.QueryAsync(
                    new CombatSkillCatalogueFilter(),
                    TestContext.Current.CancellationToken);
                var secondStoredLegendaryBookEffects =
                    await legendaryBookStore.QueryAsync(
                        TestContext.Current.CancellationToken);

                Assert.Equal(first.Definitions.Length, firstStored.Count);
                Assert.Equal(firstStored.Count, secondStored.Count);
                Assert.Equal(
                    CatalogueContentIdentity(firstStored),
                    CatalogueContentIdentity(secondStored));
                Assert.Equal(
                    first.LegendaryBookEffects.Length,
                    firstStoredLegendaryBookEffects.Count);
                Assert.Equivalent(
                    first.LegendaryBookEffects,
                    firstStoredLegendaryBookEffects);
                Assert.Equivalent(
                    firstStoredLegendaryBookEffects,
                    secondStoredLegendaryBookEffects);
            }
            finally
            {
                if (Directory.Exists(helperRoot))
                {
                    Directory.Delete(helperRoot, recursive: true);
                }
            }
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    private static void AssertFactionProfile(
        IReadOnlyList<CombatSkillFactionProfile> profiles,
        int factionId,
        CombatSkillElement element,
        CombatSkillFactionAlignment alignment)
    {
        var profile = Assert.Single(
            profiles,
            item => item.Faction.Value == factionId);
        Assert.Equal(element, profile.PrimaryElement);
        Assert.Equal(alignment, profile.PrimaryAlignment);
    }

    [Fact]
    public async Task Joined_atlas_is_repeatable_and_preserves_all_inspected_sources()
    {
        var savePath = RequireSavePath();
        var gameDirectory = RequireGameDirectoryForCatalogue();
        var guardedPaths = CatalogueSourcePaths(gameDirectory)
            .Concat(DiscoverGameOwnedReadDependencies(savePath))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var before = await CaptureAsync(guardedPaths);
        var helperRoot = Path.Combine(
            Path.GetTempPath(),
            $"taiwu-atlas-integration-{Guid.NewGuid():N}");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [ConfiguredTaiwuSaveFilePathProvider.ConfigurationKey] =
                        savePath
                })
                .Build();
            await using var provider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var source =
                provider.GetRequiredService<ICombatSkillDefinitionSource>();
            var progressReader = provider
                .GetRequiredService<ICharacterCombatSkillProgressReader>();
            var imported = await source.ReadAsync(
                TestContext.Current.CancellationToken);

            Assert.Equal(DefinitionSourceReadStatus.Available, imported.Status);
            Assert.NotNull(imported.SourceIdentity);
            Assert.True(imported.Definitions.Length > 100);
            var store = new SqliteCombatSkillCatalogueStore(
                new CatalogueStoragePathProvider(
                    helperRoot,
                    [
                        gameDirectory,
                        Path.GetDirectoryName(savePath)!
                    ]));
            Assert.True((await store.ReplaceAsync(
                imported.SourceIdentity!,
                imported.Definitions,
                imported.Diagnostics,
                imported.LegendaryBookEffects,
                TestContext.Current.CancellationToken)).Succeeded);

            var useCase = new ReadCharacterCombatSkillAtlas(
                source,
                store,
                progressReader);
            var request = new CharacterCombatSkillAtlasRequest(
                characterId: null,
                CatalogueLanguage.TraditionalChinese,
                limit: 100);
            var first = await useCase.ExecuteAsync(
                request,
                TestContext.Current.CancellationToken);
            var second = await useCase.ExecuteAsync(
                request,
                TestContext.Current.CancellationToken);
            var projectedProgress = await progressReader.ReadAsync(
                new CharacterCombatSkillProgressReadRequest(
                    characterId: null,
                    CatalogueLanguage.TraditionalChinese),
                TestContext.Current.CancellationToken);

            Assert.Equal(CombatSkillCatalogueStatus.Current, first.Catalogue.Status);
            Assert.Equal(CharacterProgressReadStatus.Available, first.ProgressStatus);
            Assert.Equal(CombatSkillCatalogueStatus.Current, second.Catalogue.Status);
            Assert.Equal(CharacterProgressReadStatus.Available, second.ProgressStatus);
            Assert.NotNull(first.ProgressMetadata);
            Assert.NotNull(second.ProgressMetadata);
            Assert.Equal(
                CharacterProgressReadStatus.Available,
                projectedProgress.Status);
            Assert.NotEmpty(projectedProgress.Progress);
            Assert.All(
                projectedProgress.Progress,
                progress =>
                {
                    Assert.False(progress.Power.Current.IsAvailable);
                    Assert.False(progress.Power.Maximum.IsAvailable);
                    Assert.Contains(
                        "live GameData special-effect context",
                        progress.Power.Current.Reason,
                        StringComparison.Ordinal);
                    Assert.True(progress.AttainmentMastered.IsAvailable);
                    Assert.Equal(
                        progress.Breakthrough.Value.IsBrokenOut,
                        progress.AttainmentMastered.Value);
                });
            Assert.True(
                string.Equals(
                    before[savePath].Sha256,
                    first.ProgressMetadata.SaveSnapshot.Sha256,
                    StringComparison.OrdinalIgnoreCase),
                "The atlas fingerprint did not match the guarded save.");
            Assert.True(
                string.Equals(
                    first.ProgressMetadata.SaveSnapshot.Sha256,
                    second.ProgressMetadata.SaveSnapshot.Sha256,
                    StringComparison.OrdinalIgnoreCase),
                "Consecutive atlas reads produced different save fingerprints.");
            Assert.Equal(imported.Definitions.Length, first.TotalMatches);
            Assert.Equal(first.TotalMatches, second.TotalMatches);
            Assert.Equal(100, first.Entries.Length);
            Assert.False(first.CandidateSetMayBeTruncated);
            Assert.Contains(
                first.Entries,
                entry => entry.Definition is not null
                         && entry.Progress is not null);
            Assert.Equal(
                first.Entries.Select(AtlasEntrySignature),
                second.Entries.Select(AtlasEntrySignature));
        }
        finally
        {
            if (Directory.Exists(helperRoot))
            {
                Directory.Delete(helperRoot, recursive: true);
            }

            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    private static string AtlasEntrySignature(
        CharacterCombatSkillAtlasEntry entry) => string.Join(
        '|',
        entry.StableKey,
        entry.DisplayName.Value.IsAvailable
            ? entry.DisplayName.Value.Value.Text
            : "?",
        entry.BaseGridCost.IsAvailable
            ? entry.BaseGridCost.Value.Value
            : -1,
        FieldSignature(entry.Learned),
        FieldSignature(entry.CurrentEffectiveGridCost),
        entry.Progress is null ? "-" : ProgressSignature(entry.Progress));

    private static string CatalogueContentIdentity(
        IEnumerable<CombatSkillDefinition> definitions)
    {
        var content = new StringBuilder();
        foreach (var definition in definitions.OrderBy(value => value.SkillId))
        {
            content.Append("skill:").Append(definition.SkillId).Append('|');
            foreach (var name in definition.Names.Values.OrderBy(value => value.Language))
            {
                content.Append("name:").Append((int)name.Language)
                    .Append(':').Append(name.Text).Append('|');
                AppendSource(content, name.Source);
            }

            AppendField(content, "category", definition.Category);
            AppendField(content, "grade", definition.Grade);
            AppendField(content, "faction", definition.Faction);
            AppendField(content, "element", definition.Element);
            AppendField(content, "equipment", definition.EquipmentType);
            AppendField(content, "grid", definition.BaseGridCost);
            AppendField(content, "slots", definition.SlotContribution);
            AppendField(
                content,
                "preparation",
                definition.Timing.PreparationProgress);
            AppendField(content, "cost", definition.Timing.BreathStanceCost);
            AppendField(content, "speed", definition.Timing.CastSpeed);
            AppendField(content, "direct", definition.Effects.Direct);
            AppendField(content, "reverse", definition.Effects.Reverse);
            AppendField(content, "neutral", definition.Effects.Neutral);
            foreach (var requirement in definition.Requirements)
            {
                content.Append("requirement:")
                    .Append(requirement.RequirementId.Value).Append('|');
                AppendField(content, "required", requirement.RequiredValue);
                AppendSource(content, requirement.Source);
            }

            foreach (var description in definition.RawDescriptions)
            {
                content.Append("description:").Append((int)description.Kind)
                    .Append(':').Append((int)description.Language)
                    .Append(':').Append(description.Text).Append('|');
                AppendSource(content, description.Source);
            }

            AppendSource(content, definition.SourceRecord);
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString())));
    }

    private static void AppendField<T>(
        StringBuilder content,
        string key,
        CatalogueField<T> field)
    {
        content.Append(key).Append(':').Append((int)field.Status).Append(':');
        if (field.IsAvailable)
        {
            content.Append(Convert.ToString(
                field.Value,
                CultureInfo.InvariantCulture));
        }

        content.Append(':').Append(field.Reason).Append('|');
        if (field.Source is not null)
        {
            AppendSource(content, field.Source);
        }
    }

    private static void AppendSource(
        StringBuilder content,
        CatalogueSourceReference source) => content
        .Append("source:").Append((int)source.Kind)
        .Append(':').Append(source.SourceIdentity)
        .Append(':').Append(source.RecordIdentity).Append('|');

    [Fact]
    public async Task Golden_snapshot_is_repeatable_and_preserves_source_files()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverGameOwnedReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);
        Assert.SkipUnless(
            string.Equals(
                before[savePath].Sha256,
                Epic2GoldenSaveSha256,
                StringComparison.OrdinalIgnoreCase),
            "M1-024 skipped: the configured save does not match the "
            + "golden snapshot fingerprint.");

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

    [Fact]
    public async Task Region_story_progress_uses_endings_and_active_task_chains()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverGameOwnedReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);
        Assert.SkipUnless(
            string.Equals(
                before[savePath].Sha256,
                RegionStoryGoldenSaveSha256,
                StringComparison.OrdinalIgnoreCase),
            "Region-story integration skipped: the configured save does "
            + "not match the verified story snapshot fingerprint.");

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var reader = provider.GetRequiredService<
                IRegionStoryProgressReader>();

            var snapshot = await reader.ReadAsync(
                new RegionStoryProgressReadRequest(
                    savePath,
                    TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);

            Assert.Equal(15, snapshot.Stories.Count);
            Assert.Equal(
                RegionStoryGoldenSaveSha256,
                snapshot.SaveSha256,
                ignoreCase: true);
            Assert.Empty(snapshot.Warnings);
            AssertStory(
                snapshot,
                organizationId: 1,
                RegionStoryProgressStatus.InProgress,
                currentTaskId: 142,
                currentTaskTitle: "老僧傳授");
            var shaolin = Assert.Single(
                snapshot.Stories,
                story => story.OrganizationId == 1);
            Assert.DoesNotContain("{0}", shaolin.CurrentTaskDescription);
            Assert.DoesNotContain("<color", shaolin.CurrentTaskDescription);
            AssertStory(
                snapshot,
                organizationId: 4,
                RegionStoryProgressStatus.InProgress,
                currentTaskId: 168,
                currentTaskTitle: "等待道長");
            AssertStory(
                snapshot,
                organizationId: 9,
                RegionStoryProgressStatus.ProsperousEnding,
                completionDate: 860);
            AssertStory(
                snapshot,
                organizationId: 12,
                RegionStoryProgressStatus.ProsperousEnding,
                completionDate: 1071);
            Assert.Equal(
                11,
                snapshot.Stories.Count(story =>
                    story.Status
                    == RegionStoryProgressStatus.NotCompleted));
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    [Fact]
    public async Task Story_target_lookup_uses_fixed_template_name_and_real_character()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverGameOwnedReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);
        Assert.SkipUnless(
            string.Equals(
                before[savePath].Sha256,
                StoryTargetGoldenSaveSha256,
                StringComparison.OrdinalIgnoreCase),
            "Story-target integration skipped: the configured save does "
            + "not match the verified Wudang story snapshot fingerprint.");

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var targetReader =
                provider.GetRequiredService<ITargetLookupReader>();
            var lookup = new FindTargets(targetReader);

            var result = await lookup.ExecuteAsync(
                new FindTargetsRequest(
                    savePath,
                    "邋遢道長",
                    language: TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);

            Assert.True(
                result.Status == TargetLookupStatus.Found,
                string.Join(
                    " | ",
                    result.Warnings
                        .Where(warning => warning.Message.Contains(
                            "61848",
                            StringComparison.Ordinal)
                            || warning.Message.Contains(
                                "63020",
                                StringComparison.Ordinal))
                        .Select(warning =>
                            $"{warning.Code}:{warning.Message}")));
            var target = Assert.Single(result.Matches);
            Assert.Equal(61848, target.CharacterId);
            Assert.Equal("邋遢道長", target.DisplayName);
            Assert.Equal(TargetLookupKind.StoryCharacter, target.Kind);
            Assert.Equal(633, target.TemplateId);
            Assert.True(target.HasValidLocation);
            Assert.DoesNotContain(
                result.Warnings,
                warning => (warning.Code is "TARGET_NAME_UNAVAILABLE"
                    or "TARGET_CONTEXT_UNAVAILABLE")
                    && warning.Message.Contains(
                        "Character 61848 ",
                        StringComparison.Ordinal));
            Assert.Contains(
                result.Warnings,
                warning => warning.Code == "TARGET_LOCATION_UNAVAILABLE"
                    && warning.Message.Contains(
                        "Character 61848 ",
                        StringComparison.Ordinal));

            var snapshotReader =
                provider.GetRequiredService<ICombatSnapshotReader>();
            var combat = await snapshotReader.ReadAsync(
                new CombatSnapshotReadRequest(
                    savePath,
                    target.CharacterId,
                    language: TaiwuLanguage.Chinese),
                TestContext.Current.CancellationToken);
            Assert.True(combat.Target.DisplayName.IsAvailable);
            Assert.Equal("邋遢道長", combat.Target.DisplayName.Value);
            Assert.NotEmpty(combat.Target.LearnedSkills);

            var recommendation = await new RecommendCombatLoadout(
                    snapshotReader)
                .ExecuteAsync(
                    new RecommendCombatLoadoutRequest(
                        savePath,
                        target.CharacterId,
                        RecommendationPolicy.Safe,
                        language: TaiwuLanguage.Chinese),
                    TestContext.Current.CancellationToken);
            Assert.Equal(
                target.CharacterId,
                recommendation.Snapshot.Target.CharacterId);
            Assert.Equal(3, recommendation.Styles.Length);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    [Fact]
    public async Task Combat_skill_page_source_search_is_typed_and_read_only()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverGameOwnedReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [ConfiguredTaiwuSaveFilePathProvider.ConfigurationKey] =
                        savePath
                })
                .Build();
            await using var provider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var reader = provider.GetRequiredService<
                ICombatSkillPageSourceReader>();

            var result = await reader.ReadAsync(
                new CombatSkillPageSourceReadRequest(
                    606,
                    ["outline-0"],
                    CatalogueLanguage.TraditionalChinese),
                TestContext.Current.CancellationToken);

            Assert.Equal(
                CombatSkillPageSourceReadStatus.Available,
                result.Status);
            Assert.NotNull(result.Metadata);
            Assert.Equal(
                before[savePath].Sha256,
                result.Metadata.SaveSnapshot.Sha256,
                ignoreCase: true);
            Assert.Equal(["outline-0"], result.RequestedDetailIds);
            Assert.All(result.Candidates, candidate =>
            {
                Assert.Contains("outline-0", candidate.DetailIds);
                Assert.True(candidate.Quantity > 0);
                if (candidate.IsActionable
                    && candidate.Availability
                        == CombatSkillPageSourceAvailability.Locatable)
                {
                    Assert.False(string.IsNullOrWhiteSpace(
                        candidate.CharacterName));
                    Assert.False(string.IsNullOrWhiteSpace(
                        candidate.LocationName));
                }
            });
            Assert.Equal(
                result.Candidates.Length,
                result.Candidates
                    .Select(candidate => (
                        candidate.Kind,
                        candidate.CharacterId,
                        candidate.BookItemId))
                    .Distinct()
                    .Count());
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    private static void AssertStory(
        RegionStoryProgressSnapshot snapshot,
        int organizationId,
        RegionStoryProgressStatus expectedStatus,
        int? completionDate = null,
        int? currentTaskId = null,
        string? currentTaskTitle = null)
    {
        var story = Assert.Single(
            snapshot.Stories,
            value => value.OrganizationId == organizationId);
        Assert.Equal(expectedStatus, story.Status);
        Assert.Equal(completionDate, story.CompletionDate);
        Assert.Equal(currentTaskId, story.CurrentTaskId);
        Assert.Equal(currentTaskTitle, story.CurrentTaskTitle);
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

    private static string RequireGameDirectoryForCatalogue()
    {
        Assert.SkipUnless(
            string.Equals(
                Environment.GetEnvironmentVariable(CatalogueVariable),
                "1",
                StringComparison.Ordinal),
            $"E2-006 skipped: set {CatalogueVariable}=1 to verify the local "
            + "installed catalogue sources.");

        var configured = Environment.GetEnvironmentVariable(
            GameDirectoryVariable);
        var candidate = !string.IsNullOrWhiteSpace(configured)
            ? configured
            : Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86),
                "Steam",
                "steamapps",
                "common",
                "The Scroll Of Taiwu");
        Assert.SkipUnless(
            Path.IsPathFullyQualified(candidate)
            && Directory.Exists(candidate),
            $"E2-006 skipped: {GameDirectoryVariable} does not identify an "
            + "installed game directory.");
        return Path.GetFullPath(candidate);
    }

    private static string[] CatalogueSourcePaths(string gameDirectory)
    {
        var streamingAssets = Path.Combine(
            gameDirectory,
            "The Scroll of Taiwu_Data",
            "StreamingAssets");
        var paths = new[]
        {
            Path.Combine(gameDirectory, "Backend", "GameData.Shared.dll"),
            Path.Combine(
                streamingAssets,
                "Language_CNH",
                "CombatSkill_language.txt"),
            Path.Combine(
                streamingAssets,
                "Language_EN",
                "CombatSkill_language.txt"),
            Path.Combine(
                streamingAssets,
                "Language_CNH",
                "SpecialEffect_language.txt"),
            Path.Combine(
                streamingAssets,
                "Language_EN",
                "SpecialEffect_language.txt"),
            Path.Combine(
                streamingAssets,
                "Language_CNH",
                "LegendaryBookSlot_language.txt"),
            Path.Combine(
                streamingAssets,
                "Language_EN",
                "LegendaryBookSlot_language.txt")
        };
        Assert.SkipWhen(
            paths.Any(path => !File.Exists(path)),
            "E2-006 skipped: one or more installed catalogue sources are "
            + "missing.");
        return paths;
    }

    private static string[] DiscoverGameOwnedReadDependencies(string savePath)
    {
        var catalogueSources = new TaiwuCatalogueSourcePathProvider().Resolve();
        var languagePaths = catalogueSources.IsAvailable
            ? new[]
            {
                catalogueSources.Paths!
                    .TraditionalChineseCombatSkillLanguage,
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

    [Fact]
    public async Task Golden_skill_progression_is_repeatable_and_read_only()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverGameOwnedReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);
        Assert.SkipUnless(
            string.Equals(
                before[savePath].Sha256,
                Epic2GoldenSaveSha256,
                StringComparison.OrdinalIgnoreCase),
            "E2-002 skipped: the configured save does not match the "
            + "E2-001 golden fingerprint.");

        try
        {
            await using var provider = new ServiceCollection()
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var reader = provider.GetRequiredService<ISaveGameReader>();

            var first = await reader.ReadAsync(
                new SaveGameReadRequest(savePath),
                TestContext.Current.CancellationToken);
            var second = await reader.ReadAsync(
                new SaveGameReadRequest(savePath),
                TestContext.Current.CancellationToken);

            AssertGoldenSkillProgress(first);
            AssertGoldenSkillProgress(second);
            Assert.Equal(first.Lines, second.Lines);
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    [Fact]
    public async Task Golden_character_progress_overlay_is_typed_and_read_only()
    {
        var savePath = RequireSavePath();
        var guardedPaths = DiscoverGameOwnedReadDependencies(savePath);
        var before = await CaptureAsync(guardedPaths);
        Assert.SkipUnless(
            string.Equals(
                before[savePath].Sha256,
                Epic2CharacterProgressGoldenSaveSha256,
                StringComparison.OrdinalIgnoreCase),
            "E2-009 skipped: the configured save does not match its "
            + "golden fingerprint.");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [ConfiguredTaiwuSaveFilePathProvider.ConfigurationKey] =
                        savePath
                })
                .Build();
            await using var provider = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddTaiwuInfrastructure()
                .BuildServiceProvider();
            var reader = provider
                .GetRequiredService<ICharacterCombatSkillProgressReader>();
            var request = new CharacterCombatSkillProgressReadRequest(
                GoldenPlayerId);

            var first = await reader.ReadAsync(
                request,
                TestContext.Current.CancellationToken);
            var second = await reader.ReadAsync(
                request,
                TestContext.Current.CancellationToken);

            AssertGoldenCharacterProgress(first, before[savePath].Sha256);
            AssertGoldenCharacterProgress(second, before[savePath].Sha256);
            Assert.Equal(
                first.Progress.Select(ProgressSignature),
                second.Progress.Select(ProgressSignature));
        }
        finally
        {
            var after = await CaptureAsync(guardedPaths);
            AssertUnchanged(before, after);
        }
    }

    private static void AssertGoldenCharacterProgress(
        CharacterCombatSkillProgressReadResult result,
        string expectedSaveSha256)
    {
        Assert.True(
            result.Status == CharacterProgressReadStatus.Available,
            result.Reason);
        Assert.NotNull(result.Metadata);
        Assert.Equal(
            expectedSaveSha256,
            result.Metadata.SaveSnapshot.Sha256,
            ignoreCase: true);
        Assert.Equal(
            TaiwuCharacterCombatSkillProgressReader.SupportedGameDataVersion,
            result.Metadata.GameDataVersion);
        Assert.Equal(506, result.Progress.Length);
        Assert.Equal(
            result.Progress.Select(progress => progress.SkillId).Order(),
            result.Progress.Select(progress => progress.SkillId));
        Assert.DoesNotContain(
            result.Metadata.Warnings,
            warning => warning.Code == "ATTAINMENT_MASTERY_UNAVAILABLE");
        Assert.DoesNotContain(
            result.Metadata.Warnings,
            warning => warning.Code == "PROFICIENCY_PERCENTAGE_UNAVAILABLE");
        Assert.DoesNotContain(
            result.Metadata.Warnings,
            warning => warning.Code.StartsWith(
                "STUDY_DETAIL_",
                StringComparison.Ordinal));

        var reverse = Assert.Single(
            result.Progress,
            progress => progress.SkillId == 40);
        Assert.True(reverse.Learned.Value);
        Assert.True(reverse.Breakthrough.Value.IsBrokenOut);
        Assert.True(reverse.AttainmentMastered.Value);
        Assert.False(reverse.Power.Current.IsAvailable);
        Assert.False(reverse.Power.Maximum.IsAvailable);
        Assert.Equal(
            PracticeDirection.Reverse,
            reverse.ActiveDirection.Value);
        Assert.True(reverse.Activated.Value);
        Assert.True(reverse.Equipped.Value);

        var direct = Assert.Single(
            result.Progress,
            progress => progress.SkillId == 41);
        Assert.True(direct.Breakthrough.Value.IsBrokenOut);
        Assert.True(direct.AttainmentMastered.Value);
        Assert.Equal(PracticeDirection.Direct, direct.ActiveDirection.Value);
        Assert.True(direct.Equipped.Value);

        var zeroState = Assert.Single(
            result.Progress,
            progress => progress.SkillId == 498);
        Assert.True(zeroState.Learned.Value);
        Assert.False(zeroState.Activated.Value);
        Assert.False(zeroState.Breakthrough.Value.IsBrokenOut);
        Assert.False(zeroState.AttainmentMastered.Value);
        Assert.Equal(15, zeroState.StudyDetails.Length);
        Assert.Equal(15, zeroState.MissingStudyDetails.Length);
        Assert.Equal(15, zeroState.StudySummary.AvailableCount);
        Assert.False(zeroState.StudySummary.IsComplete.Value);

        var fullyRead = Assert.Single(
            result.Progress,
            progress => progress.SkillId == 456);
        Assert.Equal(15, fullyRead.StudySummary.ReadCount);
        Assert.True(fullyRead.StudySummary.IsComplete.Value);
        Assert.Empty(fullyRead.MissingStudyDetails);
        Assert.Equal(
            [
                "outline-2", "outline-3", "outline-4",
                "direct-0", "direct-1", "direct-2", "direct-3", "direct-4",
                "reverse-4", "reverse-3", "reverse-2", "reverse-1",
                "reverse-0", "outline-0", "outline-1"
            ],
            fullyRead.StudyDetails.Select(detail => detail.DetailId));
        Assert.Equal("解", fullyRead.StudyDetails[0].Label.Value);
        Assert.Equal(
            CatalogueSourceKind.TraditionalChineseLanguageResource,
            fullyRead.StudyDetails[0].Label.Source!.Kind);
        Assert.All(
            fullyRead.StudyDetails.Where(detail =>
                detail.Group
                == Domain.CombatSkills.CombatSkillStudyDetailGroup.Reverse),
            detail => Assert.True(detail.IsActive.Value));

        var ready = Assert.Single(
            result.Progress,
            progress => progress.SkillId == 686);
        Assert.True(ready.Breakthrough.Value.CanBreakthroughNow);
        Assert.Equal(
            [PracticeDirection.Direct],
            ready.Breakthrough.Value.AvailableDirections);

        Assert.All(
            result.Progress,
            progress =>
            {
                Assert.False(progress.AttainmentMastered.IsAvailable);
                Assert.Equal(15, progress.StudyDetails.Length);
                Assert.Equal(
                    15,
                    progress.StudySummary.AvailableCount
                    + progress.StudySummary.UnavailableCount);
                Assert.Equal(
                    result.Metadata.SaveSnapshot,
                    progress.SaveSnapshot);
            });
    }

    private static string ProgressSignature(
        CharacterCombatSkillProgress progress) => string.Join(
            '|',
            progress.SkillId,
            FieldSignature(progress.Learned),
            FieldSignature(progress.Proficiency.Current),
            FieldSignature(progress.Proficiency.Maximum),
            FieldSignature(progress.Power.Current),
            FieldSignature(progress.Power.Maximum),
            FieldSignature(progress.Breakthrough),
            FieldSignature(progress.ActiveDirection),
            FieldSignature(progress.AttainmentMastered),
            FieldSignature(progress.Simplified),
            FieldSignature(progress.Activated),
            FieldSignature(progress.Equipped),
            string.Join(
                ';',
                progress.StudyDetails.Select(detail => string.Join(
                    ':',
                    detail.DetailId,
                    detail.DisplayOrder,
                    detail.Group,
                    detail.Label.IsAvailable ? detail.Label.Value : "?",
                    FieldSignature(detail.ReadState),
                    FieldSignature(detail.IsActive)))));

    private static string FieldSignature<T>(SkillProgressField<T> field) =>
        field.IsAvailable
            ? $"{field.Status}:{field.Value}"
            : $"{field.Status}:{field.Reason}";

    private static void AssertGoldenSkillProgress(SaveGameReport rawReport)
    {
        AssertRawSkill(
            rawReport,
            skillId: 40,
            "read=32767",
            "active=14881");
        AssertRawSkill(
            rawReport,
            skillId: 41,
            "read=32767",
            "active=996");
        AssertRawSkill(
            rawReport,
            skillId: 361,
            "read=4",
            "active=0");
        AssertRawSkill(
            rawReport,
            skillId: 456,
            "read=32767",
            "active=31744");
        AssertRawSkill(
            rawReport,
            skillId: 498,
            "read=0",
            "active=0");
        AssertRawSkill(
            rawReport,
            skillId: 686,
            "read=9928",
            "active=9920");
    }

    private static void AssertRawSkill(
        SaveGameReport report,
        int skillId,
        params string[] expectedFields)
    {
        var line = Assert.Single(
            report.Lines,
            value => value.StartsWith(
                $"SKILL|{skillId}|",
                StringComparison.Ordinal));
        Assert.All(expectedFields, field => Assert.Contains(field, line));
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
