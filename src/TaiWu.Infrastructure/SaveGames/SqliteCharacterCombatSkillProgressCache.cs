using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using TaiWu.Application.CombatSkills;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record RawCharacterCombatSkillSnapshot(
    ReadOnlyFileFingerprint SourceFingerprint,
    DateTimeOffset ReadAtUtc,
    int TaiwuCharacterId,
    int CharacterId,
    TaiwuArchiveLoadWarning? LoadWarning,
    ImmutableArray<RawCharacterCombatSkillProgress> Progress);

internal sealed class SqliteCharacterCombatSkillProgressCache(
    SaveProgressCacheStoragePathProvider pathProvider)
    : ICharacterCombatSkillProgressCacheMaintenance
{
    internal const int SchemaVersion = 4;
    internal const int MaximumCachedSavePaths = 8;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _schemaReady;

    internal async Task<RawCharacterCombatSkillSnapshot?> TryReadAsync(
        string saveFilePath,
        ReadOnlyFileRevision revision,
        int? requestedCharacterId,
        string gameDataVersion,
        int mappingVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDataVersion);
        if (requestedCharacterId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedCharacterId));
        }

        if (mappingVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mappingVersion));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            await using var connection = await OpenConnectionAsync(
                    cancellationToken)
                .ConfigureAwait(false);
            var pathKey = PathKey(saveFilePath);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    snapshot.file_length,
                    snapshot.last_write_utc_ticks,
                    snapshot.save_sha256,
                    snapshot.read_at_utc_ticks,
                    snapshot.taiwu_character_id,
                    snapshot.load_warning_code,
                    snapshot.load_warning_detail,
                    character.character_id
                FROM save_snapshots AS snapshot
                JOIN cached_characters AS character
                  ON character.path_key = snapshot.path_key
                 AND character.character_id = COALESCE(
                     $requested_character_id,
                     snapshot.taiwu_character_id)
                WHERE snapshot.path_key = $path_key
                  AND snapshot.file_length = $file_length
                  AND snapshot.last_write_utc_ticks = $last_write_utc_ticks
                  AND snapshot.game_data_version = $game_data_version
                  AND snapshot.mapping_version = $mapping_version;
                """;
            command.Parameters.AddWithValue("$path_key", pathKey);
            command.Parameters.AddWithValue("$file_length", revision.Length);
            command.Parameters.AddWithValue(
                "$last_write_utc_ticks",
                revision.LastWriteTimeUtc.UtcDateTime.Ticks);
            command.Parameters.AddWithValue(
                "$game_data_version",
                gameDataVersion);
            command.Parameters.AddWithValue("$mapping_version", mappingVersion);
            command.Parameters.AddWithValue(
                "$requested_character_id",
                requestedCharacterId is { } characterId
                    ? characterId
                    : DBNull.Value);

            SnapshotRow? snapshot = null;
            await using (var reader = await command.ExecuteReaderAsync(
                             cancellationToken)
                         .ConfigureAwait(false))
            {
                if (await reader.ReadAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    snapshot = new SnapshotRow(
                        reader.GetInt64(0),
                        reader.GetInt64(1),
                        reader.GetString(2),
                        reader.GetInt64(3),
                        reader.GetInt32(4),
                        reader.IsDBNull(5) ? null : reader.GetString(5),
                        reader.IsDBNull(6) ? null : reader.GetString(6),
                        reader.GetInt32(7));
                }
            }

            if (snapshot is null)
            {
                return null;
            }

            Validate(snapshot);

            await using var progressCommand = connection.CreateCommand();
            progressCommand.CommandText = """
                SELECT
                    skill_id,
                    learned,
                    proficiency,
                    power,
                    maximum_power,
                    power_unavailable_reason,
                    reading_state,
                    activation_state,
                    meets_breakthrough_requirement,
                    simplified,
                    equipped,
                    direct_breakthrough_completed,
                    reverse_breakthrough_completed
                FROM combat_skill_progress
                WHERE path_key = $path_key
                  AND character_id = $character_id
                ORDER BY skill_id;
                """;
            progressCommand.Parameters.AddWithValue("$path_key", pathKey);
            progressCommand.Parameters.AddWithValue(
                "$character_id",
                snapshot.CharacterId);
            var progress = ImmutableArray
                .CreateBuilder<RawCharacterCombatSkillProgress>();
            await using (var reader = await progressCommand.ExecuteReaderAsync(
                             cancellationToken)
                         .ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken)
                           .ConfigureAwait(false))
                {
                    var value = new RawCharacterCombatSkillProgress(
                        reader.GetInt32(0),
                        reader.GetBoolean(1),
                        reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        reader.GetInt32(6),
                        reader.GetInt32(7),
                        reader.GetBoolean(8),
                        reader.GetBoolean(9),
                        reader.GetBoolean(10),
                        reader.GetBoolean(11),
                        reader.GetBoolean(12),
                        Power: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                        MaximumPower: reader.IsDBNull(4)
                            ? null
                            : reader.GetInt32(4),
                        PowerUnavailableReason: reader.IsDBNull(5)
                            ? null
                            : reader.GetString(5));
                    if (value.SkillId < 0)
                    {
                        throw new InvalidDataException(
                            "The save-progress cache contains a negative skill ID.");
                    }

                    progress.Add(value);
                }
            }

            return new RawCharacterCombatSkillSnapshot(
                new ReadOnlyFileFingerprint(
                    snapshot.FileLength,
                    snapshot.SaveSha256,
                    Utc(snapshot.LastWriteUtcTicks)),
                Utc(snapshot.ReadAtUtcTicks),
                snapshot.TaiwuCharacterId,
                snapshot.CharacterId,
                snapshot.LoadWarningCode is null
                    ? null
                    : new TaiwuArchiveLoadWarning(
                        snapshot.LoadWarningCode,
                        snapshot.LoadWarningDetail ?? "(cached warning)"),
                progress.ToImmutable());
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task StoreAsync(
        string saveFilePath,
        string gameDataVersion,
        int mappingVersion,
        RawCharacterCombatSkillSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDataVersion);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (mappingVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mappingVersion));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            await using var connection = await OpenConnectionAsync(
                    cancellationToken)
                .ConfigureAwait(false);
            await using var transaction = (SqliteTransaction)await connection
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            var pathKey = PathKey(saveFilePath);
            if (!await ExistingSnapshotMatchesAsync(
                    connection,
                    transaction,
                    pathKey,
                    gameDataVersion,
                    mappingVersion,
                    snapshot.SourceFingerprint,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                await DeleteSnapshotAsync(
                        connection,
                        transaction,
                        pathKey,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await UpsertSnapshotAsync(
                    connection,
                    transaction,
                    pathKey,
                    gameDataVersion,
                    mappingVersion,
                    snapshot,
                    cancellationToken)
                .ConfigureAwait(false);
            await ReplaceCharacterAsync(
                    connection,
                    transaction,
                    pathKey,
                    snapshot,
                    cancellationToken)
                .ConfigureAwait(false);
            await PruneOldSavePathsAsync(
                    connection,
                    transaction,
                    cancellationToken)
                .ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<int> ClearAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var databasePath = pathProvider.DatabasePath;
            if (!File.Exists(databasePath))
            {
                return 0;
            }

            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            await using var connection = await OpenConnectionAsync(
                    cancellationToken)
                .ConfigureAwait(false);
            await using var count = connection.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM save_snapshots;";
            var clearedSnapshotCount = Convert.ToInt32(
                await count.ExecuteScalarAsync(cancellationToken)
                    .ConfigureAwait(false));

            await using (var transaction = (SqliteTransaction)await connection
                             .BeginTransactionAsync(cancellationToken)
                             .ConfigureAwait(false))
            {
                await using var clear = connection.CreateCommand();
                clear.Transaction = transaction;
                clear.CommandText = "DELETE FROM save_snapshots;";
                await clear.ExecuteNonQueryAsync(cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            await using var compact = connection.CreateCommand();
            compact.CommandText = """
                PRAGMA wal_checkpoint(TRUNCATE);
                VACUUM;
                PRAGMA optimize;
                """;
            await compact.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
            return clearedSnapshotCount;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaReady)
        {
            return;
        }

        Directory.CreateDirectory(pathProvider.CacheDirectory);
        await using var connection = await OpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";
        var version = Convert.ToInt32(
            await versionCommand.ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false));
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        if (version != SchemaVersion)
        {
            await ExecuteAsync(
                    connection,
                    transaction,
                    """
                    DROP TABLE IF EXISTS combat_skill_progress;
                    DROP TABLE IF EXISTS cached_characters;
                    DROP TABLE IF EXISTS save_snapshots;
                    """,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await ExecuteAsync(
                connection,
                transaction,
                """
                CREATE TABLE IF NOT EXISTS save_snapshots (
                    path_key TEXT PRIMARY KEY,
                    file_length INTEGER NOT NULL,
                    last_write_utc_ticks INTEGER NOT NULL,
                    save_sha256 TEXT NOT NULL,
                    game_data_version TEXT NOT NULL,
                    mapping_version INTEGER NOT NULL,
                    read_at_utc_ticks INTEGER NOT NULL,
                    taiwu_character_id INTEGER NOT NULL,
                    load_warning_code TEXT NULL,
                    load_warning_detail TEXT NULL
                ) STRICT;

                CREATE TABLE IF NOT EXISTS cached_characters (
                    path_key TEXT NOT NULL,
                    character_id INTEGER NOT NULL,
                    is_taiwu INTEGER NOT NULL CHECK (is_taiwu IN (0, 1)),
                    PRIMARY KEY (path_key, character_id),
                    FOREIGN KEY (path_key) REFERENCES save_snapshots(path_key)
                        ON DELETE CASCADE
                ) STRICT;

                CREATE TABLE IF NOT EXISTS combat_skill_progress (
                    path_key TEXT NOT NULL,
                    character_id INTEGER NOT NULL,
                    skill_id INTEGER NOT NULL,
                    learned INTEGER NOT NULL CHECK (learned IN (0, 1)),
                    proficiency INTEGER NULL,
                    power INTEGER NULL,
                    maximum_power INTEGER NULL,
                    power_unavailable_reason TEXT NULL,
                    reading_state INTEGER NOT NULL,
                    activation_state INTEGER NOT NULL,
                    meets_breakthrough_requirement INTEGER NOT NULL
                        CHECK (meets_breakthrough_requirement IN (0, 1)),
                    simplified INTEGER NOT NULL CHECK (simplified IN (0, 1)),
                    equipped INTEGER NOT NULL CHECK (equipped IN (0, 1)),
                    direct_breakthrough_completed INTEGER NOT NULL
                        CHECK (direct_breakthrough_completed IN (0, 1)),
                    reverse_breakthrough_completed INTEGER NOT NULL
                        CHECK (reverse_breakthrough_completed IN (0, 1)),
                    PRIMARY KEY (path_key, character_id, skill_id),
                    FOREIGN KEY (path_key, character_id)
                        REFERENCES cached_characters(path_key, character_id)
                        ON DELETE CASCADE
                ) STRICT;

                CREATE INDEX IF NOT EXISTS ix_combat_skill_progress_character
                    ON combat_skill_progress(path_key, character_id);

                CREATE INDEX IF NOT EXISTS ix_save_snapshots_retention
                    ON save_snapshots(read_at_utc_ticks DESC, path_key DESC);
                """,
                cancellationToken)
            .ConfigureAwait(false);
        await ExecuteAsync(
                connection,
                transaction,
                $"PRAGMA user_version = {SchemaVersion};",
                cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        _schemaReady = true;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = pathProvider.DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = true
            }.ToString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            PRAGMA busy_timeout = 5000;
            PRAGMA journal_mode = WAL;
            PRAGMA secure_delete = ON;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
        return connection;
    }

    private static async Task<bool> ExistingSnapshotMatchesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string pathKey,
        string gameDataVersion,
        int mappingVersion,
        ReadOnlyFileFingerprint fingerprint,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT COUNT(*)
            FROM save_snapshots
            WHERE path_key = $path_key
              AND file_length = $file_length
              AND last_write_utc_ticks = $last_write_utc_ticks
              AND save_sha256 = $save_sha256
              AND game_data_version = $game_data_version
              AND mapping_version = $mapping_version;
            """;
        command.Parameters.AddWithValue("$path_key", pathKey);
        command.Parameters.AddWithValue("$file_length", fingerprint.Length);
        command.Parameters.AddWithValue(
            "$last_write_utc_ticks",
            fingerprint.LastWriteTimeUtc.UtcDateTime.Ticks);
        command.Parameters.AddWithValue("$save_sha256", fingerprint.Sha256);
        command.Parameters.AddWithValue("$game_data_version", gameDataVersion);
        command.Parameters.AddWithValue("$mapping_version", mappingVersion);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)
            .ConfigureAwait(false)) == 1;
    }

    private static async Task DeleteSnapshotAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string pathKey,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "DELETE FROM save_snapshots WHERE path_key = $path_key;";
        command.Parameters.AddWithValue("$path_key", pathKey);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task PruneOldSavePathsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM save_snapshots
            WHERE path_key IN (
                SELECT path_key
                FROM save_snapshots
                ORDER BY read_at_utc_ticks DESC, path_key DESC
                LIMIT -1 OFFSET $maximum_cached_save_paths
            );
            """;
        command.Parameters.AddWithValue(
            "$maximum_cached_save_paths",
            MaximumCachedSavePaths);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task UpsertSnapshotAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string pathKey,
        string gameDataVersion,
        int mappingVersion,
        RawCharacterCombatSkillSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO save_snapshots (
                path_key,
                file_length,
                last_write_utc_ticks,
                save_sha256,
                game_data_version,
                mapping_version,
                read_at_utc_ticks,
                taiwu_character_id,
                load_warning_code,
                load_warning_detail)
            VALUES (
                $path_key,
                $file_length,
                $last_write_utc_ticks,
                $save_sha256,
                $game_data_version,
                $mapping_version,
                $read_at_utc_ticks,
                $taiwu_character_id,
                $load_warning_code,
                $load_warning_detail)
            ON CONFLICT(path_key) DO UPDATE SET
                read_at_utc_ticks = excluded.read_at_utc_ticks,
                taiwu_character_id = excluded.taiwu_character_id,
                load_warning_code = excluded.load_warning_code,
                load_warning_detail = excluded.load_warning_detail;
            """;
        command.Parameters.AddWithValue("$path_key", pathKey);
        command.Parameters.AddWithValue(
            "$file_length",
            snapshot.SourceFingerprint.Length);
        command.Parameters.AddWithValue(
            "$last_write_utc_ticks",
            snapshot.SourceFingerprint.LastWriteTimeUtc.UtcDateTime.Ticks);
        command.Parameters.AddWithValue(
            "$save_sha256",
            snapshot.SourceFingerprint.Sha256);
        command.Parameters.AddWithValue("$game_data_version", gameDataVersion);
        command.Parameters.AddWithValue("$mapping_version", mappingVersion);
        command.Parameters.AddWithValue(
            "$read_at_utc_ticks",
            snapshot.ReadAtUtc.UtcDateTime.Ticks);
        command.Parameters.AddWithValue(
            "$taiwu_character_id",
            snapshot.TaiwuCharacterId);
        command.Parameters.AddWithValue(
            "$load_warning_code",
            snapshot.LoadWarning?.Code ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(
            "$load_warning_detail",
            snapshot.LoadWarning?.Detail ?? (object)DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task ReplaceCharacterAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string pathKey,
        RawCharacterCombatSkillSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot.CharacterId == snapshot.TaiwuCharacterId)
        {
            await using var clearTaiwu = connection.CreateCommand();
            clearTaiwu.Transaction = transaction;
            clearTaiwu.CommandText = """
                    UPDATE cached_characters
                    SET is_taiwu = 0
                    WHERE path_key = $path_key;
                    """;
            clearTaiwu.Parameters.AddWithValue("$path_key", pathKey);
            await clearTaiwu.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = """
                DELETE FROM cached_characters
                WHERE path_key = $path_key
                  AND character_id = $character_id;
                """;
            delete.Parameters.AddWithValue("$path_key", pathKey);
            delete.Parameters.AddWithValue(
                "$character_id",
                snapshot.CharacterId);
            await delete.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        await using (var character = connection.CreateCommand())
        {
            character.Transaction = transaction;
            character.CommandText = """
                INSERT INTO cached_characters (
                    path_key,
                    character_id,
                    is_taiwu)
                VALUES ($path_key, $character_id, $is_taiwu);
                """;
            character.Parameters.AddWithValue("$path_key", pathKey);
            character.Parameters.AddWithValue(
                "$character_id",
                snapshot.CharacterId);
            character.Parameters.AddWithValue(
                "$is_taiwu",
                snapshot.CharacterId == snapshot.TaiwuCharacterId ? 1 : 0);
            await character.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var progress in snapshot.Progress)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO combat_skill_progress (
                    path_key,
                    character_id,
                    skill_id,
                    learned,
                    proficiency,
                    power,
                    maximum_power,
                    power_unavailable_reason,
                    reading_state,
                    activation_state,
                    meets_breakthrough_requirement,
                    simplified,
                    equipped,
                    direct_breakthrough_completed,
                    reverse_breakthrough_completed)
                VALUES (
                    $path_key,
                    $character_id,
                    $skill_id,
                    $learned,
                    $proficiency,
                    $power,
                    $maximum_power,
                    $power_unavailable_reason,
                    $reading_state,
                    $activation_state,
                    $meets_breakthrough_requirement,
                    $simplified,
                    $equipped,
                    $direct_breakthrough_completed,
                    $reverse_breakthrough_completed);
                """;
            command.Parameters.AddWithValue("$path_key", pathKey);
            command.Parameters.AddWithValue(
                "$character_id",
                snapshot.CharacterId);
            command.Parameters.AddWithValue("$skill_id", progress.SkillId);
            command.Parameters.AddWithValue(
                "$learned",
                progress.Learned ? 1 : 0);
            command.Parameters.AddWithValue(
                "$proficiency",
                progress.Proficiency ?? (object)DBNull.Value);
            command.Parameters.AddWithValue(
                "$power",
                progress.Power ?? (object)DBNull.Value);
            command.Parameters.AddWithValue(
                "$maximum_power",
                progress.MaximumPower ?? (object)DBNull.Value);
            command.Parameters.AddWithValue(
                "$power_unavailable_reason",
                progress.PowerUnavailableReason ?? (object)DBNull.Value);
            command.Parameters.AddWithValue(
                "$reading_state",
                progress.ReadingState);
            command.Parameters.AddWithValue(
                "$activation_state",
                progress.ActivationState);
            command.Parameters.AddWithValue(
                "$meets_breakthrough_requirement",
                progress.MeetsBreakthroughReadingRequirement ? 1 : 0);
            command.Parameters.AddWithValue(
                "$simplified",
                progress.Simplified ? 1 : 0);
            command.Parameters.AddWithValue(
                "$equipped",
                progress.Equipped ? 1 : 0);
            command.Parameters.AddWithValue(
                "$direct_breakthrough_completed",
                progress.DirectBreakthroughCompleted ? 1 : 0);
            command.Parameters.AddWithValue(
                "$reverse_breakthrough_completed",
                progress.ReverseBreakthroughCompleted ? 1 : 0);
            await command.ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static DateTimeOffset Utc(long ticks) =>
        new(new DateTime(ticks, DateTimeKind.Utc));

    private static void Validate(SnapshotRow snapshot)
    {
        if (snapshot.FileLength < 0
            || snapshot.TaiwuCharacterId < 0
            || snapshot.CharacterId < 0
            || snapshot.SaveSha256.Length != 64
            || snapshot.SaveSha256.Any(character => !Uri.IsHexDigit(character))
            || snapshot.LastWriteUtcTicks < DateTime.MinValue.Ticks
            || snapshot.LastWriteUtcTicks > DateTime.MaxValue.Ticks
            || snapshot.ReadAtUtcTicks < DateTime.MinValue.Ticks
            || snapshot.ReadAtUtcTicks > DateTime.MaxValue.Ticks)
        {
            throw new InvalidDataException(
                "The save-progress cache contains invalid snapshot metadata.");
        }
    }

    private static string PathKey(string path)
    {
        var normalized = Path.GetFullPath(path);
        if (OperatingSystem.IsWindows())
        {
            normalized = normalized.ToUpperInvariant();
        }

        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(normalized)));
    }

    private sealed record SnapshotRow(
        long FileLength,
        long LastWriteUtcTicks,
        string SaveSha256,
        long ReadAtUtcTicks,
        int TaiwuCharacterId,
        string? LoadWarningCode,
        string? LoadWarningDetail,
        int CharacterId);
}
