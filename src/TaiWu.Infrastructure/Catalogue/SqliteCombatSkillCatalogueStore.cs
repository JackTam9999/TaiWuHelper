using Microsoft.Data.Sqlite;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Text;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LegendaryBooks;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed class SqliteCombatSkillCatalogueStore(
    CatalogueStoragePathProvider pathProvider,
    TimeProvider? timeProvider = null,
    Func<int, CancellationToken, ValueTask>? writeCheckpoint = null)
    : ICombatSkillCatalogueRepository,
      ILegendaryBookEffectCatalogueRepository
{
    internal const int SchemaVersion = 4;

    private const string CategoryField = "category";
    private const string GradeField = "grade";
    private const string FactionField = "faction";
    private const string ElementField = "element";
    private const string EquipmentTypeField = "equipment-type";
    private const string BaseGridCostField = "base-grid-cost";
    private const string SlotContributionField = "slot-contribution";
    private const string PreparationProgressField = "preparation-progress";
    private const string BreathStanceCostField = "breath-stance-cost";
    private const string CastSpeedField = "cast-speed";
    private const string DirectEffectField = "direct-effect";
    private const string ReverseEffectField = "reverse-effect";
    private const string NeutralEffectField = "neutral-effect";

    private readonly SemaphoreSlim _replacementGate = new(1, 1);
    private readonly TimeProvider _timeProvider = timeProvider
        ?? TimeProvider.System;
    private readonly Func<int, CancellationToken, ValueTask> _writeCheckpoint =
        writeCheckpoint ?? ((_, _) => ValueTask.CompletedTask);

    public async Task<CombatSkillCatalogueRepositorySnapshot> ReadStateAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string databasePath;
        try
        {
            databasePath = pathProvider.DatabasePath;
        }
        catch (Exception exception)
        {
            return FailedSnapshot(SafeFailure(exception));
        }

        if (!File.Exists(databasePath))
        {
            return new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Missing,
                sourceIdentity: null,
                definitionCount: 0,
                builtAtUtc: null);
        }

        try
        {
            await using var connection = CreateConnection(
                databasePath,
                SqliteOpenMode.ReadOnly);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (SqliteTransaction)
                await connection.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken)
                    .ConfigureAwait(false);
            var manifest = await ReadManifestAsync(
                    connection,
                    transaction,
                    cancellationToken)
                .ConfigureAwait(false);
            if (manifest.SchemaVersion != SchemaVersion)
            {
                return CorruptSnapshot(
                    $"Unsupported catalogue schema version "
                    + $"{manifest.SchemaVersion}.");
            }

            var actualCount = await CountDefinitionsAsync(
                    connection,
                    transaction,
                    cancellationToken)
                .ConfigureAwait(false);
            if (actualCount != manifest.DefinitionCount)
            {
                return CorruptSnapshot(
                    "The catalogue manifest definition count does not match "
                    + "the stored definitions.");
            }

            var actualLegendaryBookEffectCount =
                await CountLegendaryBookEffectsAsync(
                        connection,
                        transaction,
                        cancellationToken)
                    .ConfigureAwait(false);
            if (actualLegendaryBookEffectCount
                != manifest.LegendaryBookEffectCount)
            {
                return CorruptSnapshot(
                    "The catalogue manifest legendary-book effect count does "
                    + "not match the stored effects.");
            }

            var actualWarningCount = await CountDiagnosticsAsync(
                    connection,
                    transaction,
                    CombatSkillImportDiagnosticSeverity.Warning,
                    cancellationToken)
                .ConfigureAwait(false);
            var actualErrorCount = await CountDiagnosticsAsync(
                    connection,
                    transaction,
                    CombatSkillImportDiagnosticSeverity.Error,
                    cancellationToken)
                .ConfigureAwait(false);
            if (actualWarningCount != manifest.WarningCount
                || actualErrorCount != manifest.ErrorCount)
            {
                return CorruptSnapshot(
                    "The catalogue manifest diagnostic counts do not match "
                    + "the stored diagnostics.");
            }

            await transaction.CommitAsync(cancellationToken)
                .ConfigureAwait(false);
            return new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                manifest.SourceIdentity,
                actualCount,
                manifest.BuiltAtUtc);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SqliteException exception)
        {
            return CorruptSnapshot(
                $"SQLite rejected the catalogue (code {exception.SqliteErrorCode}).");
        }
        catch (Exception exception)
            when (exception is InvalidDataException
                  or FormatException
                  or ArgumentException)
        {
            return CorruptSnapshot(
                "The stored catalogue metadata is invalid.");
        }
        catch (Exception exception)
            when (exception is IOException
                  or UnauthorizedAccessException)
        {
            return FailedSnapshot(SafeFailure(exception));
        }
    }

    public async Task<IReadOnlyList<CombatSkillDefinition>> QueryAsync(
        CombatSkillCatalogueFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var databasePath = RequireExistingDatabasePath();
        await using var connection = CreateConnection(
            databasePath,
            SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)
            await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                .ConfigureAwait(false);

        var skillIds = await ReadFilteredSkillIdsAsync(
                connection,
                transaction,
                filter,
                cancellationToken)
            .ConfigureAwait(false);
        List<CombatSkillDefinition> definitions = [];
        foreach (var skillId in skillIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            definitions.Add(await ReadDefinitionAsync(
                    connection,
                    transaction,
                    skillId,
                    cancellationToken)
                .ConfigureAwait(false));
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return definitions;
    }

    public async Task<CombatSkillDefinition?> GetAsync(
        int skillId,
        CancellationToken cancellationToken = default)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A combat-skill ID cannot be negative.");
        }

        var databasePath = RequireExistingDatabasePath();
        await using var connection = CreateConnection(
            databasePath,
            SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)
            await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                .ConfigureAwait(false);
        if (!await DefinitionExistsAsync(
                connection,
                transaction,
                skillId,
                cancellationToken).ConfigureAwait(false))
        {
            await transaction.CommitAsync(cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        var definition = await ReadDefinitionAsync(
                connection,
                transaction,
                skillId,
                cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return definition;
    }

    async Task<IReadOnlyList<LegendaryBookEffectDefinition>>
        ILegendaryBookEffectCatalogueRepository.QueryAsync(
            CancellationToken cancellationToken)
    {
        var databasePath = RequireExistingDatabasePath();
        await using var connection = CreateConnection(
            databasePath,
            SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)
            await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                .ConfigureAwait(false);
        var values = await ReadLegendaryBookEffectsAsync(
                connection,
                transaction,
                effectId: null,
                cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return values;
    }

    async Task<LegendaryBookEffectDefinition?>
        ILegendaryBookEffectCatalogueRepository.GetAsync(
            int effectId,
            CancellationToken cancellationToken)
    {
        if (effectId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectId),
                effectId,
                "A legendary-book effect ID cannot be negative.");
        }

        var databasePath = RequireExistingDatabasePath();
        await using var connection = CreateConnection(
            databasePath,
            SqliteOpenMode.ReadOnly);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)
            await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                .ConfigureAwait(false);
        var values = await ReadLegendaryBookEffectsAsync(
                connection,
                transaction,
                effectId,
                cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return values.SingleOrDefault();
    }

    public async Task<CatalogueReplaceResult> ReplaceAsync(
        CombatSkillCatalogueSourceIdentity sourceIdentity,
        IReadOnlyList<CombatSkillDefinition> definitions,
        IReadOnlyList<CombatSkillImportDiagnostic> diagnostics,
        CancellationToken cancellationToken = default,
        IReadOnlyList<LegendaryBookEffectDefinition>? legendaryBookEffects = null)
    {
        ArgumentNullException.ThrowIfNull(sourceIdentity);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(diagnostics);
        var effectValues = legendaryBookEffects ?? [];
        var validationFailure = ValidateReplacement(
            definitions,
            diagnostics,
            effectValues);
        if (validationFailure is not null)
        {
            return CatalogueReplaceResult.Failure(validationFailure);
        }

        var orderedDefinitions = definitions
            .OrderBy(definition => definition.SkillId)
            .ToImmutableArray();
        var orderedDiagnostics = diagnostics
            .OrderBy(
                diagnostic => diagnostic.SourceRecordIdentity,
                StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ToImmutableArray();
        var orderedLegendaryBookEffects = effectValues
            .OrderBy(effect => effect.EffectId)
            .ToImmutableArray();

        await _replacementGate.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var catalogueDirectory = pathProvider.CatalogueDirectory;
            Directory.CreateDirectory(catalogueDirectory);
            var databasePath = pathProvider.DatabasePath;
            var state = await ReadStateAsync(cancellationToken)
                .ConfigureAwait(false);
            if (state.State == CatalogueRepositoryState.Failed)
            {
                return CatalogueReplaceResult.Failure(
                    state.Reason
                    ?? "The helper-owned catalogue is unavailable.");
            }

            if (state.State == CatalogueRepositoryState.Corrupt)
            {
                await RecoverCorruptDatabaseAsync(
                        databasePath,
                        sourceIdentity,
                        orderedDefinitions,
                        orderedDiagnostics,
                        orderedLegendaryBookEffects,
                        cancellationToken)
                    .ConfigureAwait(false);
                return CatalogueReplaceResult.Success();
            }

            await WriteCompleteDatabaseAsync(
                    databasePath,
                    sourceIdentity,
                    orderedDefinitions,
                    orderedDiagnostics,
                    orderedLegendaryBookEffects,
                    cancellationToken)
                .ConfigureAwait(false);
            return CatalogueReplaceResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is SqliteException
                  or IOException
                  or UnauthorizedAccessException
                  or InvalidDataException)
        {
            return CatalogueReplaceResult.Failure(
                $"The helper-owned catalogue could not be replaced: "
                + SafeFailure(exception));
        }
        finally
        {
            _replacementGate.Release();
        }
    }

    private async Task RecoverCorruptDatabaseAsync(
        string databasePath,
        CombatSkillCatalogueSourceIdentity sourceIdentity,
        ImmutableArray<CombatSkillDefinition> definitions,
        ImmutableArray<CombatSkillImportDiagnostic> diagnostics,
        ImmutableArray<LegendaryBookEffectDefinition> legendaryBookEffects,
        CancellationToken cancellationToken)
    {
        var rebuildPath = pathProvider.RebuildDatabasePath;
        if (File.Exists(rebuildPath))
        {
            File.Delete(rebuildPath);
        }

        try
        {
            await WriteCompleteDatabaseAsync(
                    rebuildPath,
                    sourceIdentity,
                    definitions,
                    diagnostics,
                    legendaryBookEffects,
                    cancellationToken)
                .ConfigureAwait(false);
            File.Replace(
                rebuildPath,
                databasePath,
                destinationBackupFileName: null,
                ignoreMetadataErrors: true);
        }
        finally
        {
            if (File.Exists(rebuildPath))
            {
                File.Delete(rebuildPath);
            }
        }
    }

    private async Task WriteCompleteDatabaseAsync(
        string databasePath,
        CombatSkillCatalogueSourceIdentity sourceIdentity,
        ImmutableArray<CombatSkillDefinition> definitions,
        ImmutableArray<CombatSkillImportDiagnostic> diagnostics,
        ImmutableArray<LegendaryBookEffectDefinition> legendaryBookEffects,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection(
            databasePath,
            SqliteOpenMode.ReadWriteCreate);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await ConfigureWriterAsync(connection, cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)
            await connection.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                .ConfigureAwait(false);
        try
        {
            await RecreateSchemaAsync(
                    connection,
                    transaction,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (var definition in definitions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InsertDefinitionAsync(
                        connection,
                        transaction,
                        definition,
                        cancellationToken)
                    .ConfigureAwait(false);
                await _writeCheckpoint(
                        definition.SkillId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (var effect in legendaryBookEffects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InsertLegendaryBookEffectAsync(
                        connection,
                        transaction,
                        effect,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            for (var index = 0; index < diagnostics.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await InsertDiagnosticAsync(
                        connection,
                        transaction,
                        index,
                        diagnostics[index],
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await InsertManifestAsync(
                    connection,
                    transaction,
                    sourceIdentity,
                    definitions.Length,
                    legendaryBookEffects.Length,
                    diagnostics.Count(diagnostic => diagnostic.Severity
                        == CombatSkillImportDiagnosticSeverity.Warning),
                    diagnostics.Count(diagnostic => diagnostic.Severity
                        == CombatSkillImportDiagnosticSeverity.Error),
                    _timeProvider.GetUtcNow(),
                    cancellationToken)
                .ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
    }

    private static SqliteConnection CreateConnection(
        string databasePath,
        SqliteOpenMode mode) => new(
            new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = mode,
                Cache = SqliteCacheMode.Shared,
                Pooling = false,
                ForeignKeys = true
            }.ToString());

    private string RequireExistingDatabasePath()
    {
        var databasePath = pathProvider.DatabasePath;
        return File.Exists(databasePath)
            ? databasePath
            : throw new FileNotFoundException(
                "The helper-owned combat-skill catalogue has not been built.");
    }

    private static string? ValidateReplacement(
        IReadOnlyList<CombatSkillDefinition> definitions,
        IReadOnlyList<CombatSkillImportDiagnostic> diagnostics,
        IReadOnlyList<LegendaryBookEffectDefinition> legendaryBookEffects)
    {
        if (definitions.Any(definition => definition is null))
        {
            return "Definitions cannot contain null.";
        }

        if (definitions.GroupBy(definition => definition.SkillId)
            .Any(group => group.Count() > 1))
        {
            return "Definitions cannot contain duplicate skill IDs.";
        }

        if (legendaryBookEffects.Any(effect => effect is null))
        {
            return "Legendary-book effects cannot contain null.";
        }

        if (legendaryBookEffects.GroupBy(effect => effect.EffectId)
            .Any(group => group.Count() > 1))
        {
            return "Legendary-book effects cannot contain duplicate effect IDs.";
        }

        return diagnostics.Any(diagnostic => diagnostic is null)
            ? "Import diagnostics cannot contain null."
            : null;
    }

    private static async Task ConfigureWriterAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = FULL;
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task RecreateSchemaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DROP TABLE IF EXISTS import_diagnostics;
            DROP TABLE IF EXISTS legendary_book_effect_texts;
            DROP TABLE IF EXISTS legendary_book_effects;
            DROP TABLE IF EXISTS raw_descriptions;
            DROP TABLE IF EXISTS requirements;
            DROP TABLE IF EXISTS definition_fields;
            DROP TABLE IF EXISTS localized_names;
            DROP TABLE IF EXISTS definitions;
            DROP TABLE IF EXISTS catalogue_manifest;

            CREATE TABLE catalogue_manifest (
                singleton_id INTEGER NOT NULL PRIMARY KEY CHECK (singleton_id = 1),
                schema_version INTEGER NOT NULL,
                game_data_version TEXT NOT NULL,
                importer_version INTEGER NOT NULL CHECK (importer_version >= 1),
                game_data_fingerprint TEXT NOT NULL CHECK (length(game_data_fingerprint) = 64),
                traditional_chinese_fingerprint TEXT NOT NULL CHECK (length(traditional_chinese_fingerprint) = 64),
                english_fingerprint TEXT NOT NULL CHECK (length(english_fingerprint) = 64),
                traditional_chinese_special_effect_fingerprint TEXT NOT NULL CHECK (length(traditional_chinese_special_effect_fingerprint) = 64),
                english_special_effect_fingerprint TEXT NOT NULL CHECK (length(english_special_effect_fingerprint) = 64),
                traditional_chinese_legendary_book_fingerprint TEXT NOT NULL CHECK (length(traditional_chinese_legendary_book_fingerprint) = 64),
                english_legendary_book_fingerprint TEXT NOT NULL CHECK (length(english_legendary_book_fingerprint) = 64),
                built_at_utc TEXT NOT NULL,
                definition_count INTEGER NOT NULL CHECK (definition_count >= 0),
                legendary_book_effect_count INTEGER NOT NULL CHECK (legendary_book_effect_count >= 0),
                warning_count INTEGER NOT NULL CHECK (warning_count >= 0),
                error_count INTEGER NOT NULL CHECK (error_count >= 0)
            ) STRICT;

            CREATE TABLE definitions (
                skill_id INTEGER NOT NULL PRIMARY KEY CHECK (skill_id >= 0),
                source_kind INTEGER NOT NULL CHECK (source_kind BETWEEN 0 AND 3),
                source_identity TEXT NOT NULL,
                source_record_identity TEXT NOT NULL
            ) STRICT;

            CREATE TABLE localized_names (
                skill_id INTEGER NOT NULL REFERENCES definitions(skill_id) ON DELETE CASCADE,
                language INTEGER NOT NULL CHECK (language IN (0, 1)),
                text TEXT NOT NULL CHECK (length(trim(text)) > 0),
                search_text TEXT NOT NULL,
                source_kind INTEGER NOT NULL CHECK (source_kind BETWEEN 0 AND 3),
                source_identity TEXT NOT NULL,
                source_record_identity TEXT NOT NULL,
                PRIMARY KEY (skill_id, language)
            ) STRICT;

            CREATE TABLE definition_fields (
                skill_id INTEGER NOT NULL REFERENCES definitions(skill_id) ON DELETE CASCADE,
                field_key TEXT NOT NULL,
                status INTEGER NOT NULL CHECK (status BETWEEN 0 AND 2),
                value_1 INTEGER,
                value_2 INTEGER,
                value_3 INTEGER,
                value_4 INTEGER,
                value_5 INTEGER,
                reason TEXT,
                source_kind INTEGER CHECK (source_kind BETWEEN 0 AND 3),
                source_identity TEXT,
                source_record_identity TEXT,
                PRIMARY KEY (skill_id, field_key),
                CHECK (
                    (status = 0 AND reason IS NULL AND source_kind IS NOT NULL
                        AND source_identity IS NOT NULL
                        AND source_record_identity IS NOT NULL)
                    OR
                    (status IN (1, 2) AND reason IS NOT NULL)
                )
            ) STRICT;

            CREATE TABLE requirements (
                skill_id INTEGER NOT NULL REFERENCES definitions(skill_id) ON DELETE CASCADE,
                requirement_id TEXT NOT NULL,
                sort_order INTEGER NOT NULL CHECK (sort_order >= 0),
                status INTEGER NOT NULL CHECK (status BETWEEN 0 AND 2),
                required_value INTEGER,
                reason TEXT,
                source_kind INTEGER NOT NULL CHECK (source_kind BETWEEN 0 AND 3),
                source_identity TEXT NOT NULL,
                source_record_identity TEXT NOT NULL,
                PRIMARY KEY (skill_id, requirement_id),
                UNIQUE (skill_id, sort_order),
                CHECK (
                    (status = 0 AND required_value IS NOT NULL AND reason IS NULL)
                    OR
                    (status IN (1, 2) AND reason IS NOT NULL)
                )
            ) STRICT;

            CREATE TABLE raw_descriptions (
                skill_id INTEGER NOT NULL REFERENCES definitions(skill_id) ON DELETE CASCADE,
                sort_order INTEGER NOT NULL CHECK (sort_order >= 0),
                kind INTEGER NOT NULL CHECK (kind BETWEEN 0 AND 4),
                language INTEGER NOT NULL CHECK (language IN (0, 1)),
                text TEXT NOT NULL CHECK (length(trim(text)) > 0),
                source_kind INTEGER NOT NULL CHECK (source_kind BETWEEN 0 AND 3),
                source_identity TEXT NOT NULL,
                source_record_identity TEXT NOT NULL,
                PRIMARY KEY (skill_id, sort_order)
            ) STRICT;

            CREATE TABLE legendary_book_effects (
                effect_id INTEGER NOT NULL PRIMARY KEY CHECK (effect_id >= 0)
            ) STRICT;

            CREATE TABLE legendary_book_effect_texts (
                effect_id INTEGER NOT NULL REFERENCES legendary_book_effects(effect_id) ON DELETE CASCADE,
                language INTEGER NOT NULL CHECK (language IN (0, 1)),
                name TEXT,
                description TEXT,
                name_source_kind INTEGER CHECK (name_source_kind BETWEEN 0 AND 3),
                name_source_identity TEXT,
                name_source_record_identity TEXT,
                description_source_kind INTEGER CHECK (description_source_kind BETWEEN 0 AND 3),
                description_source_identity TEXT,
                description_source_record_identity TEXT,
                PRIMARY KEY (effect_id, language),
                CHECK (name IS NOT NULL OR description IS NOT NULL),
                CHECK (
                    (name IS NULL
                        AND name_source_kind IS NULL
                        AND name_source_identity IS NULL
                        AND name_source_record_identity IS NULL)
                    OR
                    (length(trim(name)) > 0
                        AND name_source_kind IS NOT NULL
                        AND name_source_identity IS NOT NULL
                        AND name_source_record_identity IS NOT NULL)
                ),
                CHECK (
                    (description IS NULL
                        AND description_source_kind IS NULL
                        AND description_source_identity IS NULL
                        AND description_source_record_identity IS NULL)
                    OR
                    (length(trim(description)) > 0
                        AND description_source_kind IS NOT NULL
                        AND description_source_identity IS NOT NULL
                        AND description_source_record_identity IS NOT NULL)
                )
            ) STRICT;

            CREATE TABLE import_diagnostics (
                sort_order INTEGER NOT NULL PRIMARY KEY CHECK (sort_order >= 0),
                severity INTEGER NOT NULL CHECK (severity IN (0, 1)),
                code TEXT NOT NULL,
                source_record_identity TEXT NOT NULL,
                reason TEXT NOT NULL
            ) STRICT;

            CREATE INDEX ix_localized_names_search
                ON localized_names(search_text, skill_id);
            CREATE INDEX ix_definition_fields_filter
                ON definition_fields(field_key, status, value_1, skill_id);
            CREATE INDEX ix_requirements_skill_order
                ON requirements(skill_id, sort_order);
            CREATE INDEX ix_descriptions_skill_order
                ON raw_descriptions(skill_id, sort_order);
            CREATE INDEX ix_legendary_book_effect_texts_language
                ON legendary_book_effect_texts(language, effect_id);
            CREATE INDEX ix_diagnostics_source
                ON import_diagnostics(source_record_identity, code);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task InsertManifestAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CombatSkillCatalogueSourceIdentity identity,
        int definitionCount,
        int legendaryBookEffectCount,
        int warningCount,
        int errorCount,
        DateTimeOffset builtAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO catalogue_manifest (
                singleton_id,
                schema_version,
                game_data_version,
                importer_version,
                game_data_fingerprint,
                traditional_chinese_fingerprint,
                english_fingerprint,
                traditional_chinese_special_effect_fingerprint,
                english_special_effect_fingerprint,
                traditional_chinese_legendary_book_fingerprint,
                english_legendary_book_fingerprint,
                built_at_utc,
                definition_count,
                legendary_book_effect_count,
                warning_count,
                error_count)
            VALUES (
                1,
                $schemaVersion,
                $gameDataVersion,
                $importerVersion,
                $gameDataFingerprint,
                $traditionalChineseFingerprint,
                $englishFingerprint,
                $traditionalChineseSpecialEffectFingerprint,
                $englishSpecialEffectFingerprint,
                $traditionalChineseLegendaryBookFingerprint,
                $englishLegendaryBookFingerprint,
                $builtAtUtc,
                $definitionCount,
                $legendaryBookEffectCount,
                $warningCount,
                $errorCount);
            """;
        command.Parameters.AddWithValue("$schemaVersion", SchemaVersion);
        command.Parameters.AddWithValue(
            "$gameDataVersion",
            identity.GameDataVersion);
        command.Parameters.AddWithValue(
            "$importerVersion",
            identity.ImporterVersion);
        command.Parameters.AddWithValue(
            "$gameDataFingerprint",
            identity.GameDataFingerprint);
        command.Parameters.AddWithValue(
            "$traditionalChineseFingerprint",
            identity.TraditionalChineseFingerprint);
        command.Parameters.AddWithValue(
            "$englishFingerprint",
            identity.EnglishFingerprint);
        command.Parameters.AddWithValue(
            "$traditionalChineseSpecialEffectFingerprint",
            identity.TraditionalChineseSpecialEffectFingerprint);
        command.Parameters.AddWithValue(
            "$englishSpecialEffectFingerprint",
            identity.EnglishSpecialEffectFingerprint);
        command.Parameters.AddWithValue(
            "$traditionalChineseLegendaryBookFingerprint",
            identity.TraditionalChineseLegendaryBookFingerprint);
        command.Parameters.AddWithValue(
            "$englishLegendaryBookFingerprint",
            identity.EnglishLegendaryBookFingerprint);
        command.Parameters.AddWithValue(
            "$builtAtUtc",
            builtAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$definitionCount", definitionCount);
        command.Parameters.AddWithValue(
            "$legendaryBookEffectCount",
            legendaryBookEffectCount);
        command.Parameters.AddWithValue("$warningCount", warningCount);
        command.Parameters.AddWithValue("$errorCount", errorCount);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task InsertDefinitionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CombatSkillDefinition definition,
        CancellationToken cancellationToken)
    {
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO definitions (
                    skill_id,
                    source_kind,
                    source_identity,
                    source_record_identity)
                VALUES ($skillId, $sourceKind, $sourceIdentity, $sourceRecord);
                """;
            command.Parameters.AddWithValue("$skillId", definition.SkillId);
            AddRequiredSource(command, definition.SourceRecord);
            await command.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var name in definition.Names.Values.OrderBy(value => value.Language))
        {
            await InsertNameAsync(
                    connection,
                    transaction,
                    definition.SkillId,
                    name,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var fields = DefinitionFields(definition);
        foreach (var (key, field) in fields.OrderBy(value => value.Key, StringComparer.Ordinal))
        {
            await InsertFieldAsync(
                    connection,
                    transaction,
                    definition.SkillId,
                    key,
                    field,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        for (var index = 0; index < definition.Requirements.Length; index++)
        {
            await InsertRequirementAsync(
                    connection,
                    transaction,
                    definition.SkillId,
                    index,
                    definition.Requirements[index],
                    cancellationToken)
                .ConfigureAwait(false);
        }

        for (var index = 0; index < definition.RawDescriptions.Length; index++)
        {
            await InsertDescriptionAsync(
                    connection,
                    transaction,
                    definition.SkillId,
                    index,
                    definition.RawDescriptions[index],
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static IEnumerable<KeyValuePair<string, StoredField>> DefinitionFields(
        CombatSkillDefinition definition)
    {
        yield return KeyValuePair.Create(
            CategoryField,
            StoredField.From(definition.Category, value => [(int)value]));
        yield return KeyValuePair.Create(
            GradeField,
            StoredField.From(definition.Grade, value => [value.Value]));
        yield return KeyValuePair.Create(
            FactionField,
            StoredField.From(definition.Faction, value => [value.Value]));
        yield return KeyValuePair.Create(
            ElementField,
            StoredField.From(definition.Element, value => [(int)value]));
        yield return KeyValuePair.Create(
            EquipmentTypeField,
            StoredField.From(definition.EquipmentType, value => [(int)value]));
        yield return KeyValuePair.Create(
            BaseGridCostField,
            StoredField.From(definition.BaseGridCost, value => [value.Value]));
        yield return KeyValuePair.Create(
            SlotContributionField,
            StoredField.From(
                definition.SlotContribution,
                value =>
                [
                    value.Attack,
                    value.Agility,
                    value.Defense,
                    value.Assistance,
                    value.Generic
                ]));
        yield return KeyValuePair.Create(
            PreparationProgressField,
            StoredField.From(
                definition.Timing.PreparationProgress,
                value => [value]));
        yield return KeyValuePair.Create(
            BreathStanceCostField,
            StoredField.From(
                definition.Timing.BreathStanceCost,
                value => [value]));
        yield return KeyValuePair.Create(
            CastSpeedField,
            StoredField.From(
                definition.Timing.CastSpeed,
                value => [value]));
        yield return KeyValuePair.Create(
            DirectEffectField,
            StoredField.From(definition.Effects.Direct, value => [value.Value]));
        yield return KeyValuePair.Create(
            ReverseEffectField,
            StoredField.From(definition.Effects.Reverse, value => [value.Value]));
        yield return KeyValuePair.Create(
            NeutralEffectField,
            StoredField.From(definition.Effects.Neutral, value => [value.Value]));
    }

    private static async Task InsertNameAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int skillId,
        LocalizedCombatSkillName name,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO localized_names (
                skill_id,
                language,
                text,
                search_text,
                source_kind,
                source_identity,
                source_record_identity)
            VALUES (
                $skillId,
                $language,
                $text,
                $searchText,
                $sourceKind,
                $sourceIdentity,
                $sourceRecord);
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        command.Parameters.AddWithValue("$language", (int)name.Language);
        command.Parameters.AddWithValue("$text", name.Text);
        command.Parameters.AddWithValue("$searchText", NormalizeSearch(name.Text));
        AddRequiredSource(command, name.Source);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task InsertFieldAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int skillId,
        string key,
        StoredField field,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO definition_fields (
                skill_id,
                field_key,
                status,
                value_1,
                value_2,
                value_3,
                value_4,
                value_5,
                reason,
                source_kind,
                source_identity,
                source_record_identity)
            VALUES (
                $skillId,
                $fieldKey,
                $status,
                $value1,
                $value2,
                $value3,
                $value4,
                $value5,
                $reason,
                $sourceKind,
                $sourceIdentity,
                $sourceRecord);
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        command.Parameters.AddWithValue("$fieldKey", key);
        command.Parameters.AddWithValue("$status", (int)field.Status);
        for (var index = 0; index < 5; index++)
        {
            command.Parameters.AddWithValue(
                $"$value{index + 1}",
                DbValue(index < field.Values.Length
                    ? field.Values[index]
                    : null));
        }

        command.Parameters.AddWithValue("$reason", DbValue(field.Reason));
        AddOptionalSource(command, field.Source);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task InsertRequirementAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int skillId,
        int sortOrder,
        CombatSkillRequirementDefinition requirement,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO requirements (
                skill_id,
                requirement_id,
                sort_order,
                status,
                required_value,
                reason,
                source_kind,
                source_identity,
                source_record_identity)
            VALUES (
                $skillId,
                $requirementId,
                $sortOrder,
                $status,
                $requiredValue,
                $reason,
                $sourceKind,
                $sourceIdentity,
                $sourceRecord);
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        command.Parameters.AddWithValue(
            "$requirementId",
            requirement.RequirementId.Value);
        command.Parameters.AddWithValue("$sortOrder", sortOrder);
        command.Parameters.AddWithValue(
            "$status",
            (int)requirement.RequiredValue.Status);
        command.Parameters.AddWithValue(
            "$requiredValue",
            requirement.RequiredValue.IsAvailable
                ? requirement.RequiredValue.Value
                : DBNull.Value);
        command.Parameters.AddWithValue(
            "$reason",
            DbValue(requirement.RequiredValue.Reason));
        AddRequiredSource(command, requirement.Source);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task InsertDescriptionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int skillId,
        int sortOrder,
        RawCombatSkillDescription description,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO raw_descriptions (
                skill_id,
                sort_order,
                kind,
                language,
                text,
                source_kind,
                source_identity,
                source_record_identity)
            VALUES (
                $skillId,
                $sortOrder,
                $kind,
                $language,
                $text,
                $sourceKind,
                $sourceIdentity,
                $sourceRecord);
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        command.Parameters.AddWithValue("$sortOrder", sortOrder);
        command.Parameters.AddWithValue("$kind", (int)description.Kind);
        command.Parameters.AddWithValue("$language", (int)description.Language);
        command.Parameters.AddWithValue("$text", description.Text);
        AddRequiredSource(command, description.Source);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task InsertLegendaryBookEffectAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        LegendaryBookEffectDefinition effect,
        CancellationToken cancellationToken)
    {
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO legendary_book_effects (effect_id)
                VALUES ($effectId);
                """;
            command.Parameters.AddWithValue("$effectId", effect.EffectId);
            await command.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var localization in effect.Localizations)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO legendary_book_effect_texts (
                    effect_id,
                    language,
                    name,
                    description,
                    name_source_kind,
                    name_source_identity,
                    name_source_record_identity,
                    description_source_kind,
                    description_source_identity,
                    description_source_record_identity)
                VALUES (
                    $effectId,
                    $language,
                    $name,
                    $description,
                    $nameSourceKind,
                    $nameSourceIdentity,
                    $nameSourceRecord,
                    $descriptionSourceKind,
                    $descriptionSourceIdentity,
                    $descriptionSourceRecord);
                """;
            command.Parameters.AddWithValue("$effectId", effect.EffectId);
            command.Parameters.AddWithValue(
                "$language",
                (int)localization.Language);
            command.Parameters.AddWithValue("$name", DbValue(localization.Name));
            command.Parameters.AddWithValue(
                "$description",
                DbValue(localization.Description));
            AddSourceParameters(command, "name", localization.NameSource);
            AddSourceParameters(
                command,
                "description",
                localization.DescriptionSource);
            await command.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static void AddSourceParameters(
        SqliteCommand command,
        string prefix,
        CatalogueSourceReference? source)
    {
        command.Parameters.AddWithValue(
            $"${prefix}SourceKind",
            source is null ? DBNull.Value : (int)source.Kind);
        command.Parameters.AddWithValue(
            $"${prefix}SourceIdentity",
            DbValue(source?.SourceIdentity));
        command.Parameters.AddWithValue(
            $"${prefix}SourceRecord",
            DbValue(source?.RecordIdentity));
    }

    private static async Task InsertDiagnosticAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int sortOrder,
        CombatSkillImportDiagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO import_diagnostics (
                sort_order,
                severity,
                code,
                source_record_identity,
                reason)
            VALUES (
                $sortOrder,
                $severity,
                $code,
                $sourceRecord,
                $reason);
            """;
        command.Parameters.AddWithValue("$sortOrder", sortOrder);
        command.Parameters.AddWithValue("$severity", (int)diagnostic.Severity);
        command.Parameters.AddWithValue("$code", diagnostic.Code);
        command.Parameters.AddWithValue(
            "$sourceRecord",
            diagnostic.SourceRecordIdentity);
        command.Parameters.AddWithValue("$reason", diagnostic.Reason);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<StoredManifest> ReadManifestAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                schema_version,
                game_data_version,
                importer_version,
                game_data_fingerprint,
                traditional_chinese_fingerprint,
                english_fingerprint,
                traditional_chinese_special_effect_fingerprint,
                english_special_effect_fingerprint,
                traditional_chinese_legendary_book_fingerprint,
                english_legendary_book_fingerprint,
                built_at_utc,
                definition_count,
                legendary_book_effect_count,
                warning_count,
                error_count
            FROM catalogue_manifest
            WHERE singleton_id = 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidDataException(
                "The catalogue manifest is missing.");
        }

        var result = new StoredManifest(
            reader.GetInt32(0),
            new CombatSkillCatalogueSourceIdentity(
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                reader.GetString(9)),
            DateTimeOffset.ParseExact(
                reader.GetString(10),
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),
            reader.GetInt32(11),
            reader.GetInt32(12),
            reader.GetInt32(13),
            reader.GetInt32(14));
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidDataException(
                "The catalogue contains more than one manifest.");
        }

        return result;
    }

    private static async Task<int> CountDefinitionsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM definitions;";
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false),
            CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountLegendaryBookEffectsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM legendary_book_effects;";
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false),
            CultureInfo.InvariantCulture);
    }

    private static async Task<int> CountDiagnosticsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CombatSkillImportDiagnosticSeverity severity,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT COUNT(*)
            FROM import_diagnostics
            WHERE severity = $severity;
            """;
        command.Parameters.AddWithValue("$severity", (int)severity);
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false),
            CultureInfo.InvariantCulture);
    }

    private static async Task<ImmutableArray<int>> ReadFilteredSkillIdsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CombatSkillCatalogueFilter filter,
        CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("SELECT d.skill_id FROM definitions d WHERE 1=1");
        List<(string Field, int Value, string Parameter)> predicates = [];
        AddFilter(predicates, CategoryField, filter.Category, "$category");
        if (filter.Grade is { } grade)
        {
            predicates.Add((GradeField, grade.Value, "$grade"));
        }

        if (filter.Faction is { } faction)
        {
            predicates.Add((FactionField, faction.Value, "$faction"));
        }

        AddFilter(predicates, ElementField, filter.Element, "$element");
        AddFilter(
            predicates,
            EquipmentTypeField,
            filter.EquipmentType,
            "$equipmentType");
        foreach (var predicate in predicates)
        {
            sql.Append(" AND EXISTS (SELECT 1 FROM definition_fields f WHERE ");
            sql.Append("f.skill_id = d.skill_id AND f.field_key = '");
            sql.Append(predicate.Field);
            sql.Append("' AND f.status = 0 AND f.value_1 = ");
            sql.Append(predicate.Parameter);
            sql.Append(')');
        }

        sql.Append(" ORDER BY d.skill_id LIMIT $limit;");
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql.ToString();
        foreach (var predicate in predicates)
        {
            command.Parameters.AddWithValue(predicate.Parameter, predicate.Value);
        }

        command.Parameters.AddWithValue("$limit", filter.CandidateLimit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        ImmutableArray<int>.Builder result = ImmutableArray.CreateBuilder<int>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(reader.GetInt32(0));
        }

        return result.ToImmutable();
    }

    private static void AddFilter<TEnum>(
        ICollection<(string Field, int Value, string Parameter)> predicates,
        string field,
        TEnum? value,
        string parameter)
        where TEnum : struct, Enum
    {
        if (value is { } actual)
        {
            predicates.Add((field, Convert.ToInt32(actual), parameter));
        }
    }

    private static async Task<bool> DefinitionExistsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int skillId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT EXISTS(
                SELECT 1 FROM definitions WHERE skill_id = $skillId);
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        return Convert.ToInt32(
                   await command.ExecuteScalarAsync(cancellationToken)
                       .ConfigureAwait(false),
                   CultureInfo.InvariantCulture)
               == 1;
    }

    private static async Task<CombatSkillDefinition> ReadDefinitionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int skillId,
        CancellationToken cancellationToken)
    {
        var source = await ReadDefinitionSourceAsync(
                connection,
                transaction,
                skillId,
                cancellationToken)
            .ConfigureAwait(false);
        var names = await ReadNamesAsync(
                connection,
                transaction,
                skillId,
                cancellationToken)
            .ConfigureAwait(false);
        var fields = await ReadFieldsAsync(
                connection,
                transaction,
                skillId,
                cancellationToken)
            .ConfigureAwait(false);
        var requirements = await ReadRequirementsAsync(
                connection,
                transaction,
                skillId,
                cancellationToken)
            .ConfigureAwait(false);
        var descriptions = await ReadDescriptionsAsync(
                connection,
                transaction,
                skillId,
                cancellationToken)
            .ConfigureAwait(false);

        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(names),
            RequiredField(fields, CategoryField)
                .ToCatalogueField(value => (CombatSkillDiscipline)value),
            RequiredField(fields, GradeField)
                .ToCatalogueField(value => new CombatSkillGrade(value)),
            RequiredField(fields, FactionField)
                .ToCatalogueField(value => new CombatSkillFactionId(value)),
            RequiredField(fields, ElementField)
                .ToCatalogueField(value => (CombatSkillElement)value),
            RequiredField(fields, EquipmentTypeField)
                .ToCatalogueField(value => (CombatSkillEquipmentType)value),
            RequiredField(fields, BaseGridCostField)
                .ToCatalogueField(value => new CombatSkillGridCost(value)),
            RequiredField(fields, SlotContributionField)
                .ToCompositeCatalogueField(values => new SkillSlotContribution(
                    values[0],
                    values[1],
                    values[2],
                    values[3],
                    values[4])),
            requirements,
            new CombatSkillTimingDefinition(
                RequiredField(fields, PreparationProgressField)
                    .ToCatalogueField(value => value),
                RequiredField(fields, BreathStanceCostField)
                    .ToCatalogueField(value => value),
                RequiredField(fields, CastSpeedField)
                    .ToCatalogueField(value => value)),
            new CombatSkillEffectReferences(
                RequiredField(fields, DirectEffectField)
                    .ToCatalogueField(value => new CombatSkillEffectId(value)),
                RequiredField(fields, ReverseEffectField)
                    .ToCatalogueField(value => new CombatSkillEffectId(value)),
                RequiredField(fields, NeutralEffectField)
                    .ToCatalogueField(value => new CombatSkillEffectId(value))),
            descriptions,
            source);
    }

    private static async Task<CatalogueSourceReference> ReadDefinitionSourceAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int skillId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT source_kind, source_identity, source_record_identity
            FROM definitions
            WHERE skill_id = $skillId;
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidDataException(
                $"Catalogue definition {skillId} disappeared during its read.");
        }

        return ReadRequiredSource(reader, 0);
    }

    private static async Task<ImmutableArray<LocalizedCombatSkillName>>
        ReadNamesAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int skillId,
            CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                language,
                text,
                source_kind,
                source_identity,
                source_record_identity
            FROM localized_names
            WHERE skill_id = $skillId
            ORDER BY language;
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        ImmutableArray<LocalizedCombatSkillName>.Builder values =
            ImmutableArray.CreateBuilder<LocalizedCombatSkillName>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values.Add(new LocalizedCombatSkillName(
                (CatalogueLanguage)reader.GetInt32(0),
                reader.GetString(1),
                ReadRequiredSource(reader, 2)));
        }

        return values.ToImmutable();
    }

    private static async Task<IReadOnlyDictionary<string, ReadField>>
        ReadFieldsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int skillId,
            CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                field_key,
                status,
                value_1,
                value_2,
                value_3,
                value_4,
                value_5,
                reason,
                source_kind,
                source_identity,
                source_record_identity
            FROM definition_fields
            WHERE skill_id = $skillId
            ORDER BY field_key;
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<string, ReadField> values = new(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values.Add(
                reader.GetString(0),
                new ReadField(
                    (CatalogueFieldStatus)reader.GetInt32(1),
                    Enumerable.Range(2, 5)
                        .Select(index => reader.IsDBNull(index)
                            ? (int?)null
                            : reader.GetInt32(index))
                        .ToImmutableArray(),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    ReadOptionalSource(reader, 8)));
        }

        return values;
    }

    private static async Task<ImmutableArray<CombatSkillRequirementDefinition>>
        ReadRequirementsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int skillId,
            CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                requirement_id,
                status,
                required_value,
                reason,
                source_kind,
                source_identity,
                source_record_identity
            FROM requirements
            WHERE skill_id = $skillId
            ORDER BY sort_order;
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        ImmutableArray<CombatSkillRequirementDefinition>.Builder values =
            ImmutableArray.CreateBuilder<CombatSkillRequirementDefinition>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var source = ReadRequiredSource(reader, 4);
            var field = new ReadField(
                (CatalogueFieldStatus)reader.GetInt32(1),
                [reader.IsDBNull(2) ? null : reader.GetInt32(2)],
                reader.IsDBNull(3) ? null : reader.GetString(3),
                source);
            values.Add(new CombatSkillRequirementDefinition(
                new CombatSkillRequirementId(reader.GetString(0)),
                field.ToCatalogueField(value => value),
                source));
        }

        return values.ToImmutable();
    }

    private static async Task<ImmutableArray<RawCombatSkillDescription>>
        ReadDescriptionsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int skillId,
            CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                kind,
                language,
                text,
                source_kind,
                source_identity,
                source_record_identity
            FROM raw_descriptions
            WHERE skill_id = $skillId
            ORDER BY sort_order;
            """;
        command.Parameters.AddWithValue("$skillId", skillId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        ImmutableArray<RawCombatSkillDescription>.Builder values =
            ImmutableArray.CreateBuilder<RawCombatSkillDescription>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values.Add(new RawCombatSkillDescription(
                (RawCombatSkillDescriptionKind)reader.GetInt32(0),
                (CatalogueLanguage)reader.GetInt32(1),
                reader.GetString(2),
                ReadRequiredSource(reader, 3)));
        }

        return values.ToImmutable();
    }

    private static async Task<ImmutableArray<LegendaryBookEffectDefinition>>
        ReadLegendaryBookEffectsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int? effectId,
            CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                e.effect_id,
                t.language,
                t.name,
                t.description,
                t.name_source_kind,
                t.name_source_identity,
                t.name_source_record_identity,
                t.description_source_kind,
                t.description_source_identity,
                t.description_source_record_identity
            FROM legendary_book_effects e
            INNER JOIN legendary_book_effect_texts t
                ON t.effect_id = e.effect_id
            WHERE $effectId IS NULL OR e.effect_id = $effectId
            ORDER BY e.effect_id, t.language;
            """;
        command.Parameters.AddWithValue(
            "$effectId",
            effectId is null ? DBNull.Value : effectId.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<int, List<LocalizedLegendaryBookEffect>> values = [];
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetInt32(0);
            if (!values.TryGetValue(id, out var localizations))
            {
                localizations = [];
                values.Add(id, localizations);
            }

            localizations.Add(new LocalizedLegendaryBookEffect(
                (CatalogueLanguage)reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                ReadOptionalSource(reader, 4),
                ReadOptionalSource(reader, 7)));
        }

        return values
            .OrderBy(pair => pair.Key)
            .Select(pair => new LegendaryBookEffectDefinition(
                pair.Key,
                pair.Value))
            .ToImmutableArray();
    }

    private static ReadField RequiredField(
        IReadOnlyDictionary<string, ReadField> fields,
        string key) => fields.TryGetValue(key, out var value)
            ? value
            : throw new InvalidDataException(
                $"Catalogue definition is missing required field '{key}'.");

    private static void AddRequiredSource(
        SqliteCommand command,
        CatalogueSourceReference source)
    {
        command.Parameters.AddWithValue("$sourceKind", (int)source.Kind);
        command.Parameters.AddWithValue(
            "$sourceIdentity",
            source.SourceIdentity);
        command.Parameters.AddWithValue(
            "$sourceRecord",
            source.RecordIdentity);
    }

    private static void AddOptionalSource(
        SqliteCommand command,
        CatalogueSourceReference? source)
    {
        command.Parameters.AddWithValue(
            "$sourceKind",
            source is null ? DBNull.Value : (int)source.Kind);
        command.Parameters.AddWithValue(
            "$sourceIdentity",
            DbValue(source?.SourceIdentity));
        command.Parameters.AddWithValue(
            "$sourceRecord",
            DbValue(source?.RecordIdentity));
    }

    private static CatalogueSourceReference ReadRequiredSource(
        SqliteDataReader reader,
        int startIndex) => new(
            (CatalogueSourceKind)reader.GetInt32(startIndex),
            reader.GetString(startIndex + 1),
            reader.GetString(startIndex + 2));

    private static CatalogueSourceReference? ReadOptionalSource(
        SqliteDataReader reader,
        int startIndex) => reader.IsDBNull(startIndex)
            ? null
            : ReadRequiredSource(reader, startIndex);

    private static string NormalizeSearch(string value) =>
        value.Normalize(NormalizationForm.FormKC).ToUpperInvariant();

    private static object DbValue(object? value) => value ?? DBNull.Value;

    private static string SafeFailure(Exception exception) => exception switch
    {
        SqliteException sqlite => $"SQLite error {sqlite.SqliteErrorCode}.",
        UnauthorizedAccessException => "Access was denied.",
        IOException => "A filesystem operation failed.",
        InvalidDataException => exception.Message,
        _ => "An unexpected persistence error occurred."
    };

    private static CombatSkillCatalogueRepositorySnapshot CorruptSnapshot(
        string reason) => new(
            CatalogueRepositoryState.Corrupt,
            sourceIdentity: null,
            definitionCount: 0,
            builtAtUtc: null,
            reason);

    private static CombatSkillCatalogueRepositorySnapshot FailedSnapshot(
        string reason) => new(
            CatalogueRepositoryState.Failed,
            sourceIdentity: null,
            definitionCount: 0,
            builtAtUtc: null,
            string.IsNullOrWhiteSpace(reason)
                ? "The helper-owned catalogue could not be accessed."
                : reason);

    private sealed record StoredManifest(
        int SchemaVersion,
        CombatSkillCatalogueSourceIdentity SourceIdentity,
        DateTimeOffset BuiltAtUtc,
        int DefinitionCount,
        int LegendaryBookEffectCount,
        int WarningCount,
        int ErrorCount);

    private sealed record StoredField(
        CatalogueFieldStatus Status,
        ImmutableArray<int> Values,
        string? Reason,
        CatalogueSourceReference? Source)
    {
        internal static StoredField From<T>(
            CatalogueField<T> field,
            Func<T, int[]> values) => new(
                field.Status,
                field.IsAvailable
                    ? values(field.Value).ToImmutableArray()
                    : [],
                field.Reason,
                field.Source);
    }

    private sealed record ReadField(
        CatalogueFieldStatus Status,
        ImmutableArray<int?> Values,
        string? Reason,
        CatalogueSourceReference? Source)
    {
        internal CatalogueField<T> ToCatalogueField<T>(Func<int, T> factory) =>
            CreateCatalogueField(values => factory(values[0]), requiredValues: 1);

        internal CatalogueField<T> ToCompositeCatalogueField<T>(
            Func<IReadOnlyList<int>, T> factory) =>
            CreateCatalogueField(factory, requiredValues: 5);

        private CatalogueField<T> CreateCatalogueField<T>(
            Func<IReadOnlyList<int>, T> factory,
            int requiredValues)
        {
            return Status switch
            {
                CatalogueFieldStatus.Available => CatalogueField<T>.Available(
                    factory(Values.Take(requiredValues).Select(value => value
                        ?? throw new InvalidDataException(
                            "An available catalogue field is missing a value."))
                        .ToArray()),
                    Source ?? throw new InvalidDataException(
                        "An available catalogue field is missing provenance.")),
                CatalogueFieldStatus.Unavailable => CatalogueField<T>.Unavailable(
                    Reason ?? throw new InvalidDataException(
                        "An unavailable catalogue field is missing its reason."),
                    Source),
                CatalogueFieldStatus.Unsupported => CatalogueField<T>.Unsupported(
                    Reason ?? throw new InvalidDataException(
                        "An unsupported catalogue field is missing its reason."),
                    Source ?? throw new InvalidDataException(
                        "An unsupported catalogue field is missing provenance.")),
                _ => throw new InvalidDataException(
                    $"Unknown catalogue field status {(int)Status}.")
            };
        }
    }
}
