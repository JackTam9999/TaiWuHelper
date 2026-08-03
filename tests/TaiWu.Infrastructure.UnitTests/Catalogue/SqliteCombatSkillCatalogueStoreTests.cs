using Microsoft.Data.Sqlite;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class SqliteCombatSkillCatalogueStoreTests
{
    [Fact]
    public async Task Missing_catalogue_is_read_only_and_explicit()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();

        var state = await store.ReadStateAsync(CancellationToken);

        Assert.Equal(CatalogueRepositoryState.Missing, state.State);
        Assert.False(Directory.Exists(fixture.Provider.CatalogueDirectory));
        Assert.False(File.Exists(fixture.Provider.DatabasePath));
    }

    [Fact]
    public async Task Round_trip_preserves_manifest_definition_and_provenance()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        var definitions = new[]
        {
            Definition(2, "Second", CombatSkillDiscipline.Sword),
            Definition(1, "First", CombatSkillDiscipline.Finger)
        };
        var diagnostics = new[]
        {
            new CombatSkillImportDiagnostic(
                CombatSkillImportDiagnosticSeverity.Warning,
                "WARNING",
                "combat-skill:2",
                "Preserved warning."),
            new CombatSkillImportDiagnostic(
                CombatSkillImportDiagnosticSeverity.Error,
                "ERROR",
                "combat-skill:1",
                "Preserved error.")
        };

        var replacement = await store.ReplaceAsync(
            Identity,
            definitions,
            diagnostics,
            CancellationToken);
        var state = await store.ReadStateAsync(CancellationToken);
        var all = await store.QueryAsync(
            new CombatSkillCatalogueFilter(),
            CancellationToken);
        var first = await store.GetAsync(1, CancellationToken);

        Assert.True(replacement.Succeeded);
        Assert.Equal(CatalogueRepositoryState.Ready, state.State);
        Assert.Equal(Identity, state.SourceIdentity);
        Assert.Equal(2, state.DefinitionCount);
        Assert.Equal(FixedUtc, state.BuiltAtUtc);
        Assert.Equal([1, 2], all.Select(value => value.SkillId));
        Assert.NotNull(first);
        Assert.Equal("First", first.Names.Get(CatalogueLanguage.English).Value.Text);
        Assert.Equal(CombatSkillDiscipline.Finger, first.Category.Value);
        Assert.Equal(5, first.Grade.Value.Value);
        Assert.Equal(15, first.Faction.Value.Value);
        Assert.Equal(CombatSkillElement.Wood, first.Element.Value);
        Assert.Equal(
            CombatSkillEquipmentType.Attack,
            first.EquipmentType.Value);
        Assert.Equal(3, first.BaseGridCost.Value.Value);
        Assert.Equal(2, first.SlotContribution.Value.Attack);
        Assert.Equal(1, first.SlotContribution.Value.Generic);
        Assert.Equal(39000, first.Timing.PreparationProgress.Value);
        Assert.Equal(331, first.Effects.Direct.Value.Value);
        Assert.Equal(
            CatalogueFieldStatus.Unavailable,
            first.Effects.Neutral.Status);
        Assert.Equal(
            "character-property:17",
            Assert.Single(first.Requirements).RequirementId.Value);
        Assert.Collection(
            first.RawDescriptions,
            original =>
            {
                Assert.Equal(RawCombatSkillDescriptionKind.Effect, original.Kind);
                Assert.Equal("Display effect", original.Text);
            },
            direct =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.DirectEffect,
                    direct.Kind);
                Assert.Equal("Direct display effect", direct.Text);
            },
            reverse =>
            {
                Assert.Equal(
                    RawCombatSkillDescriptionKind.ReverseEffect,
                    reverse.Kind);
                Assert.Equal("Reverse display effect", reverse.Text);
            });
        Assert.Equal(
            "gamedata:test",
            first.SourceRecord.SourceIdentity);

        await using var connection = new SqliteConnection(
            $"Data Source={fixture.Provider.DatabasePath};Mode=ReadOnly;Pooling=False");
        await connection.OpenAsync(CancellationToken);
        Assert.Equal(
            2L,
            await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM import_diagnostics;"));
        Assert.Equal(
            Identity.ImporterVersion,
            await ScalarAsync(
                connection,
                "SELECT importer_version FROM catalogue_manifest;"));
        Assert.Equal(
            1L,
            await ScalarAsync(
                connection,
                "SELECT warning_count FROM catalogue_manifest;"));
        Assert.Equal(
            1L,
            await ScalarAsync(
                connection,
                "SELECT error_count FROM catalogue_manifest;"));
        Assert.Equal(
            2L,
            await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM localized_names;"));
        Assert.Equal(
            SqliteCombatSkillCatalogueStore.SchemaVersion,
            await ScalarAsync(
                connection,
                "SELECT schema_version FROM catalogue_manifest;"));
        Assert.True(await IndexExistsAsync(
            connection,
            "ix_localized_names_search"));
    }

    [Fact]
    public async Task Typed_filters_and_candidate_limit_are_deterministic()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        await store.ReplaceAsync(
            Identity,
            [
                Definition(
                    3,
                    "Third",
                    CombatSkillDiscipline.Sword,
                    grade: 2,
                    faction: 8,
                    element: CombatSkillElement.Fire,
                    equipment: CombatSkillEquipmentType.Defense),
                Definition(
                    1,
                    "First",
                    CombatSkillDiscipline.Finger,
                    grade: 5,
                    faction: 15,
                    element: CombatSkillElement.Wood,
                    equipment: CombatSkillEquipmentType.Attack),
                Definition(
                    2,
                    "Second",
                    CombatSkillDiscipline.Finger,
                    grade: 4,
                    faction: 15,
                    element: CombatSkillElement.Metal,
                    equipment: CombatSkillEquipmentType.Attack)
            ],
            diagnostics: [],
            CancellationToken);

        var exact = await store.QueryAsync(
            new CombatSkillCatalogueFilter(
                category: CombatSkillDiscipline.Finger,
                grade: new CombatSkillGrade(5),
                faction: new CombatSkillFactionId(15),
                element: CombatSkillElement.Wood,
                equipmentType: CombatSkillEquipmentType.Attack),
            CancellationToken);
        var bounded = await store.QueryAsync(
            new CombatSkillCatalogueFilter(candidateLimit: 2),
            CancellationToken);

        Assert.Equal(1, Assert.Single(exact).SkillId);
        Assert.Equal([1, 2], bounded.Select(value => value.SkillId));
    }

    [Fact]
    public async Task Schema_rejects_duplicate_skill_and_language_keys()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        Assert.True((await store.ReplaceAsync(
            Identity,
            [Definition(1, "First", CombatSkillDiscipline.Finger)],
            diagnostics: [],
            CancellationToken)).Succeeded);

        await using var connection = new SqliteConnection(
            $"Data Source={fixture.Provider.DatabasePath};"
            + "Mode=ReadWrite;Foreign Keys=True;Pooling=False");
        await connection.OpenAsync(CancellationToken);

        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO definitions (
                    skill_id,
                    source_kind,
                    source_identity,
                    source_record_identity)
                VALUES (1, 0, 'duplicate', 'duplicate');
                """;
            await command.ExecuteNonQueryAsync(CancellationToken);
        });
        await Assert.ThrowsAsync<SqliteException>(async () =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO localized_names (
                    skill_id,
                    language,
                    text,
                    search_text,
                    source_kind,
                    source_identity,
                    source_record_identity)
                SELECT
                    skill_id,
                    language,
                    text,
                    search_text,
                    source_kind,
                    source_identity,
                    source_record_identity
                FROM localized_names
                LIMIT 1;
                """;
            await command.ExecuteNonQueryAsync(CancellationToken);
        });
    }

    [Fact]
    public async Task Round_trip_preserves_unavailable_and_unsupported_fields()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        var definition = NonavailableDefinition(9);

        Assert.True((await store.ReplaceAsync(
            Identity,
            [definition],
            diagnostics: [],
            CancellationToken)).Succeeded);
        var actual = await store.GetAsync(9, CancellationToken);

        Assert.NotNull(actual);
        Assert.Empty(actual.Names.Values);
        Assert.Equal(CatalogueFieldStatus.Unsupported, actual.Category.Status);
        Assert.Equal("Unsupported category.", actual.Category.Reason);
        Assert.Equal(CatalogueFieldStatus.Unavailable, actual.Grade.Status);
        Assert.Equal(CatalogueFieldStatus.Unsupported, actual.SlotContribution.Status);
        Assert.Equal(
            CatalogueFieldStatus.Unsupported,
            actual.Timing.PreparationProgress.Status);
        Assert.Equal(
            CatalogueFieldStatus.Unavailable,
            actual.Effects.Direct.Status);
        Assert.Equal(
            CatalogueFieldStatus.Unsupported,
            Assert.Single(actual.Requirements).RequiredValue.Status);
        Assert.Equal(
            "gamedata:test",
            actual.Category.Source!.SourceIdentity);
    }

    [Fact]
    public async Task Replacement_removes_old_rows_and_updates_manifest_atomically()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        await store.ReplaceAsync(
            OlderIdentity,
            [Definition(1, "Old", CombatSkillDiscipline.Finger)],
            diagnostics: [],
            CancellationToken);

        var result = await store.ReplaceAsync(
            Identity,
            [Definition(2, "New", CombatSkillDiscipline.Sword)],
            diagnostics: [],
            CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(await store.GetAsync(1, CancellationToken));
        Assert.Equal(2, (await store.GetAsync(2, CancellationToken))!.SkillId);
        var state = await store.ReadStateAsync(CancellationToken);
        Assert.Equal(Identity, state.SourceIdentity);
        Assert.Equal(1, state.DefinitionCount);
    }

    [Fact]
    public async Task Mid_transaction_failure_rolls_back_complete_previous_catalogue()
    {
        using var fixture = StoreFixture.Create();
        var initial = fixture.CreateStore();
        await initial.ReplaceAsync(
            OlderIdentity,
            [Definition(1, "Old", CombatSkillDiscipline.Finger)],
            diagnostics: [],
            CancellationToken);
        var failing = fixture.CreateStore(
            writeCheckpoint: (skillId, _) => skillId == 2
                ? ValueTask.FromException(
                    new InvalidDataException("Injected write failure."))
                : ValueTask.CompletedTask);

        var result = await failing.ReplaceAsync(
            Identity,
            [
                Definition(2, "New", CombatSkillDiscipline.Sword),
                Definition(3, "Newer", CombatSkillDiscipline.Blade)
            ],
            diagnostics: [],
            CancellationToken);

        Assert.False(result.Succeeded);
        var state = await initial.ReadStateAsync(CancellationToken);
        Assert.Equal(OlderIdentity, state.SourceIdentity);
        Assert.Equal(1, state.DefinitionCount);
        Assert.NotNull(await initial.GetAsync(1, CancellationToken));
        Assert.Null(await initial.GetAsync(2, CancellationToken));
    }

    [Fact]
    public async Task Invalid_replacement_is_rejected_without_touching_current_data()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        var existing = Definition(1, "Old", CombatSkillDiscipline.Finger);
        await store.ReplaceAsync(
            OlderIdentity,
            [existing],
            diagnostics: [],
            CancellationToken);

        var result = await store.ReplaceAsync(
            Identity,
            [existing, existing],
            diagnostics: [],
            CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(OlderIdentity, (await store.ReadStateAsync(CancellationToken)).SourceIdentity);
        Assert.Single(await store.QueryAsync(
            new CombatSkillCatalogueFilter(),
            CancellationToken));
    }

    [Fact]
    public async Task Concurrent_readers_observe_only_complete_old_or_new_snapshot()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        var oldDefinitions = Enumerable.Range(1, 30)
            .Select(id => Definition(
                id,
                $"Old {id}",
                CombatSkillDiscipline.Finger))
            .ToArray();
        var newDefinitions = Enumerable.Range(101, 40)
            .Select(id => Definition(
                id,
                $"New {id}",
                CombatSkillDiscipline.Sword))
            .ToArray();
        await store.ReplaceAsync(
            OlderIdentity,
            oldDefinitions,
            diagnostics: [],
            CancellationToken);

        var readers = Enumerable.Range(0, 24)
            .Select(_ => store.QueryAsync(
                new CombatSkillCatalogueFilter(),
                CancellationToken))
            .ToArray();
        var replacement = store.ReplaceAsync(
            Identity,
            newDefinitions,
            diagnostics: [],
            CancellationToken);
        var snapshots = await Task.WhenAll(readers);
        Assert.True((await replacement).Succeeded);

        Assert.All(
            snapshots,
            snapshot => Assert.True(
                snapshot.Select(value => value.SkillId)
                    .SequenceEqual(oldDefinitions.Select(value => value.SkillId))
                || snapshot.Select(value => value.SkillId)
                    .SequenceEqual(newDefinitions.Select(value => value.SkillId)),
                "A reader observed a partially replaced catalogue."));
    }

    [Fact]
    public async Task Malformed_database_is_reported_as_corrupt()
    {
        using var fixture = StoreFixture.Create();
        Directory.CreateDirectory(fixture.Provider.CatalogueDirectory);
        await File.WriteAllTextAsync(
            fixture.Provider.DatabasePath,
            "not a sqlite database",
            CancellationToken);

        var state = await fixture.CreateStore().ReadStateAsync(CancellationToken);

        Assert.Equal(CatalogueRepositoryState.Corrupt, state.State);
        Assert.NotNull(state.Reason);
    }

    [Fact]
    public async Task Structurally_incomplete_database_is_reported_as_corrupt()
    {
        using var fixture = StoreFixture.Create();
        Directory.CreateDirectory(fixture.Provider.CatalogueDirectory);
        await using (var connection = new SqliteConnection(
                         $"Data Source={fixture.Provider.DatabasePath};"
                         + "Mode=ReadWriteCreate;Pooling=False"))
        {
            await connection.OpenAsync(CancellationToken);
        }

        var state = await fixture.CreateStore().ReadStateAsync(CancellationToken);

        Assert.Equal(CatalogueRepositoryState.Corrupt, state.State);
        Assert.Equal(
            "SQLite rejected the catalogue (code 1).",
            state.Reason);
    }

    [Fact]
    public async Task Manifest_count_mismatch_is_reported_as_corrupt()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        Assert.True((await store.ReplaceAsync(
            Identity,
            [Definition(1, "One", CombatSkillDiscipline.Finger)],
            [
                new CombatSkillImportDiagnostic(
                    CombatSkillImportDiagnosticSeverity.Warning,
                    "WARNING",
                    "combat-skill:1",
                    "warning")
            ],
            CancellationToken)).Succeeded);
        await ExecuteAsync(
            fixture.Provider.DatabasePath,
            "UPDATE catalogue_manifest SET warning_count = 2;");

        var state = await store.ReadStateAsync(CancellationToken);

        Assert.Equal(CatalogueRepositoryState.Corrupt, state.State);
        Assert.Contains("diagnostic counts", state.Reason);
    }

    [Fact]
    public async Task Ensure_recovers_empty_malformed_and_old_schema_databases()
    {
        foreach (var condition in new[] { "empty", "malformed", "old-schema" })
        {
            using var fixture = StoreFixture.Create();
            var store = fixture.CreateStore();
            Directory.CreateDirectory(fixture.Provider.CatalogueDirectory);
            if (condition == "empty")
            {
                await using var connection = new SqliteConnection(
                    $"Data Source={fixture.Provider.DatabasePath};"
                    + "Mode=ReadWriteCreate;Pooling=False");
                await connection.OpenAsync(CancellationToken);
            }
            else if (condition == "malformed")
            {
                await File.WriteAllTextAsync(
                    fixture.Provider.DatabasePath,
                    "not a sqlite database",
                    CancellationToken);
            }
            else
            {
                Assert.True((await store.ReplaceAsync(
                    OlderIdentity,
                    [Definition(2, "Old", CombatSkillDiscipline.Sword)],
                    diagnostics: [],
                    CancellationToken)).Succeeded);
                await ExecuteAsync(
                    fixture.Provider.DatabasePath,
                    "UPDATE catalogue_manifest SET schema_version = 1;");
            }

            var result = await new EnsureCombatSkillCatalogue(
                    new FixedDefinitionSource(
                        CombatSkillDefinitionSourceResult.Available(
                            Identity,
                            [Definition(1, "One", CombatSkillDiscipline.Finger)])),
                    store)
                .ExecuteAsync(CancellationToken);

            Assert.Equal(EnsureCombatSkillCatalogueStatus.Rebuilt, result.Status);
            var state = await store.ReadStateAsync(CancellationToken);
            Assert.Equal(CatalogueRepositoryState.Ready, state.State);
            Assert.Equal(Identity, state.SourceIdentity);
            Assert.False(File.Exists(fixture.Provider.RebuildDatabasePath));
        }
    }

    [Fact]
    public async Task Identical_sources_are_current_without_touching_the_database()
    {
        using var fixture = StoreFixture.Create();
        var store = fixture.CreateStore();
        var definitions = new[]
        {
            Definition(2, "Second", CombatSkillDiscipline.Sword),
            Definition(1, "First", CombatSkillDiscipline.Finger)
        };
        Assert.True((await store.ReplaceAsync(
            Identity,
            definitions,
            diagnostics: [],
            CancellationToken)).Succeeded);
        var beforeWriteTime = File.GetLastWriteTimeUtc(
            fixture.Provider.DatabasePath);
        var beforeLength = new FileInfo(fixture.Provider.DatabasePath).Length;

        var result = await new EnsureCombatSkillCatalogue(
                new FixedDefinitionSource(
                    CombatSkillDefinitionSourceResult.Available(
                        Identity,
                        definitions.Reverse())),
                store)
            .ExecuteAsync(CancellationToken);
        var queried = await store.QueryAsync(
            new CombatSkillCatalogueFilter(),
            CancellationToken);

        Assert.Equal(EnsureCombatSkillCatalogueStatus.Current, result.Status);
        Assert.Equal([1, 2], queried.Select(definition => definition.SkillId));
        Assert.Equal(
            beforeWriteTime,
            File.GetLastWriteTimeUtc(fixture.Provider.DatabasePath));
        Assert.Equal(
            beforeLength,
            new FileInfo(fixture.Provider.DatabasePath).Length);
    }

    [Fact]
    public async Task Interrupted_corrupt_recovery_keeps_corrupt_file_and_clear_status()
    {
        using var fixture = StoreFixture.Create();
        Directory.CreateDirectory(fixture.Provider.CatalogueDirectory);
        const string corruptContent = "not a sqlite database";
        await File.WriteAllTextAsync(
            fixture.Provider.DatabasePath,
            corruptContent,
            CancellationToken);
        var store = fixture.CreateStore(
            writeCheckpoint: (_, _) => ValueTask.FromException(
                new InvalidDataException("Injected recovery failure.")));

        var result = await new EnsureCombatSkillCatalogue(
                new FixedDefinitionSource(
                    CombatSkillDefinitionSourceResult.Available(
                        Identity,
                        [Definition(1, "One", CombatSkillDiscipline.Finger)])),
                store)
            .ExecuteAsync(CancellationToken);

        Assert.Equal(
            EnsureCombatSkillCatalogueStatus.RebuildFailed,
            result.Status);
        Assert.Equal(
            CatalogueRecoveryStatus.CorruptCatalogueRemains,
            result.RecoveryStatus);
        Assert.Equal(
            corruptContent,
            await File.ReadAllTextAsync(
                fixture.Provider.DatabasePath,
                CancellationToken));
        Assert.False(File.Exists(fixture.Provider.RebuildDatabasePath));
    }

    [Fact]
    public async Task Cancellation_is_propagated_without_creating_storage()
    {
        using var fixture = StoreFixture.Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var store = fixture.CreateStore();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.ReplaceAsync(
                Identity,
                [Definition(1, "One", CombatSkillDiscipline.Finger)],
                diagnostics: [],
                cancellation.Token));

        Assert.False(Directory.Exists(fixture.Provider.CatalogueDirectory));
    }

    private static CancellationToken CancellationToken =>
        TestContext.Current.CancellationToken;

    private static DateTimeOffset FixedUtc { get; } =
        DateTimeOffset.Parse("2026-08-02T16:00:00Z");

    private static CombatSkillCatalogueSourceIdentity Identity { get; } = new(
        "1.0.0-current",
        1,
        new string('A', 64),
        new string('B', 64),
        new string('C', 64),
        new string('D', 64),
        new string('E', 64));

    private static CombatSkillCatalogueSourceIdentity OlderIdentity { get; } = new(
        "1.0.0-older",
        1,
        new string('D', 64),
        new string('E', 64),
        new string('F', 64));

    private static CombatSkillDefinition Definition(
        int skillId,
        string englishName,
        CombatSkillDiscipline category,
        int grade = 5,
        int faction = 15,
        CombatSkillElement element = CombatSkillElement.Wood,
        CombatSkillEquipmentType equipment = CombatSkillEquipmentType.Attack)
    {
        var gameData = Source(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        var english = Source(
            CatalogueSourceKind.EnglishLanguageResource,
            "language-en:test",
            $"combat-skill-name:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(
            [
                new LocalizedCombatSkillName(
                    CatalogueLanguage.English,
                    englishName,
                    english)
            ]),
            CatalogueField<CombatSkillDiscipline>.Available(category, gameData),
            CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(grade),
                gameData),
            CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(faction),
                gameData),
            CatalogueField<CombatSkillElement>.Available(element, gameData),
            CatalogueField<CombatSkillEquipmentType>.Available(
                equipment,
                gameData),
            CatalogueField<CombatSkillGridCost>.Available(
                new CombatSkillGridCost(3),
                gameData),
            CatalogueField<SkillSlotContribution>.Available(
                new SkillSlotContribution(2, 0, 0, 0, 1),
                gameData),
            [
                new CombatSkillRequirementDefinition(
                    new CombatSkillRequirementId("character-property:17"),
                    CatalogueField<int>.Available(60, gameData),
                    gameData)
            ],
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Available(39000, gameData),
                CatalogueField<int>.Available(100, gameData),
                CatalogueField<int>.Available(25, gameData)),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(331),
                    gameData),
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(1057),
                    gameData),
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "No neutral effect.",
                    gameData)),
            [
                new RawCombatSkillDescription(
                    RawCombatSkillDescriptionKind.Effect,
                    CatalogueLanguage.English,
                    "Display effect",
                    Source(
                        CatalogueSourceKind.EnglishLanguageResource,
                        "language-en:test",
                        $"combat-skill-description:{skillId}")),
                new RawCombatSkillDescription(
                    RawCombatSkillDescriptionKind.DirectEffect,
                    CatalogueLanguage.English,
                    "Direct display effect",
                    Source(
                        CatalogueSourceKind.EnglishLanguageResource,
                        "special-effect-language-en:test",
                        "special-effect-description:331")),
                new RawCombatSkillDescription(
                    RawCombatSkillDescriptionKind.ReverseEffect,
                    CatalogueLanguage.English,
                    "Reverse display effect",
                    Source(
                        CatalogueSourceKind.EnglishLanguageResource,
                        "special-effect-language-en:test",
                        "special-effect-description:1057"))
            ],
            gameData);
    }

    private static CombatSkillDefinition NonavailableDefinition(int skillId)
    {
        var source = Source(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(),
            CatalogueField<CombatSkillDiscipline>.Unsupported(
                "Unsupported category.",
                source),
            CatalogueField<CombatSkillGrade>.Unavailable(
                "Grade missing.",
                source),
            CatalogueField<CombatSkillFactionId>.Unavailable(
                "Faction missing."),
            CatalogueField<CombatSkillElement>.Unsupported(
                "Unsupported element.",
                source),
            CatalogueField<CombatSkillEquipmentType>.Unsupported(
                "Unsupported equipment type.",
                source),
            CatalogueField<CombatSkillGridCost>.Unavailable(
                "Grid cost missing.",
                source),
            CatalogueField<SkillSlotContribution>.Unsupported(
                "Unsupported slot contribution.",
                source),
            [
                new CombatSkillRequirementDefinition(
                    new CombatSkillRequirementId("character-property:17"),
                    CatalogueField<int>.Unsupported(
                        "Unsupported requirement.",
                        source),
                    source)
            ],
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Unsupported(
                    "Unsupported preparation.",
                    source),
                CatalogueField<int>.Unavailable("Cost missing."),
                CatalogueField<int>.Unavailable("Speed missing.", source)),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "Direct effect missing."),
                CatalogueField<CombatSkillEffectId>.Unsupported(
                    "Reverse effect unsupported.",
                    source),
                CatalogueField<CombatSkillEffectId>.Unavailable(
                    "Neutral effect missing.",
                    source)),
            rawDescriptions: null,
            source);
    }

    private static CatalogueSourceReference Source(
        CatalogueSourceKind kind,
        string identity,
        string record) => new(kind, identity, record);

    private static async Task<long> ScalarAsync(
        SqliteConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(
            await command.ExecuteScalarAsync(CancellationToken));
    }

    private static async Task ExecuteAsync(string databasePath, string sql)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={databasePath};Mode=ReadWrite;Pooling=False");
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static async Task<bool> IndexExistsAsync(
        SqliteConnection connection,
        string indexName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS(
                SELECT 1
                FROM sqlite_master
                WHERE type = 'index' AND name = $name);
            """;
        command.Parameters.AddWithValue("$name", indexName);
        return Convert.ToInt32(
                   await command.ExecuteScalarAsync(CancellationToken))
               == 1;
    }

    private sealed class StoreFixture : IDisposable
    {
        private StoreFixture(string root)
        {
            Root = root;
            var helper = Path.Combine(root, "helper");
            var game = Path.Combine(root, "game");
            var saves = Path.Combine(root, "saves");
            Directory.CreateDirectory(game);
            Directory.CreateDirectory(saves);
            Provider = new CatalogueStoragePathProvider(
                helper,
                [game, saves]);
        }

        internal string Root { get; }

        internal CatalogueStoragePathProvider Provider { get; }

        internal static StoreFixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"taiwu-sqlite-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            return new StoreFixture(root);
        }

        internal SqliteCombatSkillCatalogueStore CreateStore(
            Func<int, CancellationToken, ValueTask>? writeCheckpoint = null) =>
            new(
                Provider,
                new FixedTimeProvider(FixedUtc),
                writeCheckpoint);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
    }

    private sealed class FixedDefinitionSource(
        CombatSkillDefinitionSourceResult result)
        : ICombatSkillDefinitionSource
    {
        public Task<CombatSkillDefinitionSourceResult> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }
}
