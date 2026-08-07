using System.Collections.Immutable;
using Microsoft.Data.Sqlite;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class SqliteCharacterCombatSkillProgressCacheTests
{
    [Fact]
    public async Task Stored_snapshot_round_trips_as_structured_rows()
    {
        using var fixture = Fixture.Create();
        var snapshot = Snapshot();

        await fixture.Cache.StoreAsync(
            fixture.SavePath,
            "0.0.85.0",
            mappingVersion: 1,
            snapshot,
            TestContext.Current.CancellationToken);
        var cached = await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(snapshot.SourceFingerprint),
            requestedCharacterId: null,
            "0.0.85.0",
            mappingVersion: 1,
            TestContext.Current.CancellationToken);

        Assert.NotNull(cached);
        Assert.Equal(snapshot.SourceFingerprint, cached.SourceFingerprint);
        Assert.Equal(snapshot.ReadAtUtc, cached.ReadAtUtc);
        Assert.Equal(snapshot.TaiwuCharacterId, cached.TaiwuCharacterId);
        Assert.Equal(snapshot.CharacterId, cached.CharacterId);
        Assert.Equal(snapshot.LoadWarning, cached.LoadWarning);
        Assert.Equal(snapshot.Progress, cached.Progress);
        Assert.True(File.Exists(fixture.Provider.DatabasePath));

        await using var connection = new SqliteConnection(
            $"Data Source={fixture.Provider.DatabasePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var count = connection.CreateCommand();
        count.CommandText = """
            SELECT COUNT(*), MIN(path_key)
            FROM combat_skill_progress;
            """;
        await using var reader = await count.ExecuteReaderAsync(
            TestContext.Current.CancellationToken);
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            snapshot.Progress.Length,
            reader.GetInt32(0));
        var pathKey = reader.GetString(1);
        Assert.Equal(64, pathKey.Length);
        Assert.DoesNotContain("SaveGames", pathKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Changed_revision_game_version_or_mapping_version_misses()
    {
        using var fixture = Fixture.Create();
        var snapshot = Snapshot();
        await fixture.Cache.StoreAsync(
            fixture.SavePath,
            "0.0.85.0",
            1,
            snapshot,
            TestContext.Current.CancellationToken);

        var changedRevision = new ReadOnlyFileRevision(
            snapshot.SourceFingerprint.Length + 1,
            snapshot.SourceFingerprint.LastWriteTimeUtc);
        Assert.Null(await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            changedRevision,
            null,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken));
        Assert.Null(await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(snapshot.SourceFingerprint),
            null,
            "0.0.86.0",
            1,
            TestContext.Current.CancellationToken));
        Assert.Null(await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(snapshot.SourceFingerprint),
            null,
            "0.0.85.0",
            2,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task New_save_snapshot_atomically_replaces_old_characters()
    {
        using var fixture = Fixture.Create();
        var first = Snapshot();
        var secondFingerprint = new ReadOnlyFileFingerprint(
            first.SourceFingerprint.Length + 10,
            new string('B', 64),
            first.SourceFingerprint.LastWriteTimeUtc.AddMinutes(1));
        var second = first with
        {
            SourceFingerprint = secondFingerprint,
            ReadAtUtc = first.ReadAtUtc.AddMinutes(1),
            Progress =
            [
                new RawCharacterCombatSkillProgress(
                    999,
                    true,
                    123,
                    4,
                    5,
                    true,
                    false,
                    true)
            ]
        };

        await fixture.Cache.StoreAsync(
            fixture.SavePath,
            "0.0.85.0",
            1,
            first,
            TestContext.Current.CancellationToken);
        await fixture.Cache.StoreAsync(
            fixture.SavePath,
            "0.0.85.0",
            1,
            second,
            TestContext.Current.CancellationToken);

        Assert.Null(await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(first.SourceFingerprint),
            null,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken));
        var cached = await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(second.SourceFingerprint),
            null,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken);
        Assert.Equal([999], cached!.Progress.Select(value => value.SkillId));
    }

    [Fact]
    public async Task Explicit_character_can_coexist_with_cached_taiwu()
    {
        using var fixture = Fixture.Create();
        var taiwu = Snapshot();
        var companion = taiwu with
        {
            CharacterId = 321,
            Progress =
            [
                new RawCharacterCombatSkillProgress(
                    777,
                    true,
                    null,
                    1,
                    2,
                    false,
                    false,
                    false)
            ]
        };
        await fixture.Cache.StoreAsync(
            fixture.SavePath,
            "0.0.85.0",
            1,
            taiwu,
            TestContext.Current.CancellationToken);
        await fixture.Cache.StoreAsync(
            fixture.SavePath,
            "0.0.85.0",
            1,
            companion,
            TestContext.Current.CancellationToken);

        var defaultCharacter = await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(taiwu.SourceFingerprint),
            null,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken);
        var explicitCharacter = await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(taiwu.SourceFingerprint),
            321,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken);

        Assert.Equal(100, defaultCharacter!.CharacterId);
        Assert.Equal(321, explicitCharacter!.CharacterId);
        Assert.Equal(777, Assert.Single(explicitCharacter.Progress).SkillId);
    }

    [Fact]
    public async Task Clear_removes_all_derived_snapshots_without_source_access()
    {
        using var fixture = Fixture.Create();
        var snapshot = Snapshot();
        await fixture.Cache.StoreAsync(
            fixture.SavePath,
            "0.0.85.0",
            1,
            snapshot,
            TestContext.Current.CancellationToken);

        var cleared = await fixture.Cache.ClearAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(1, cleared);
        Assert.Null(await fixture.Cache.TryReadAsync(
            fixture.SavePath,
            ReadOnlyFileRevision.From(snapshot.SourceFingerprint),
            null,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken));
        Assert.Equal(
            0,
            await fixture.Cache.ClearAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Retention_keeps_only_the_most_recent_save_paths()
    {
        using var fixture = Fixture.Create();
        var first = Snapshot();
        var paths = Enumerable.Range(
                0,
                SqliteCharacterCombatSkillProgressCache
                    .MaximumCachedSavePaths + 1)
            .Select(index => $"{fixture.SavePath}.{index}")
            .ToArray();
        for (var index = 0; index < paths.Length; index++)
        {
            await fixture.Cache.StoreAsync(
                paths[index],
                "0.0.85.0",
                1,
                first with { ReadAtUtc = first.ReadAtUtc.AddMinutes(index) },
                TestContext.Current.CancellationToken);
        }

        Assert.Null(await fixture.Cache.TryReadAsync(
            paths[0],
            ReadOnlyFileRevision.From(first.SourceFingerprint),
            null,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken));
        Assert.NotNull(await fixture.Cache.TryReadAsync(
            paths[^1],
            ReadOnlyFileRevision.From(first.SourceFingerprint),
            null,
            "0.0.85.0",
            1,
            TestContext.Current.CancellationToken));
    }

    private static RawCharacterCombatSkillSnapshot Snapshot() => new(
        new ReadOnlyFileFingerprint(
            213_937_298,
            new string('A', 64),
            DateTimeOffset.Parse("2026-08-03T12:00:00Z")),
        DateTimeOffset.Parse("2026-08-03T12:00:05Z"),
        TaiwuCharacterId: 100,
        CharacterId: 100,
        new TaiwuArchiveLoadWarning(
            TaiwuArchiveLoadWarning.StandaloneEventRuntimeUnavailable,
            "Void InitRuntimeEnvironment()"),
        ImmutableArray.Create(
            new RawCharacterCombatSkillProgress(
                686,
                true,
                270,
                11,
                22,
                true,
                true,
                true,
                DirectBreakthroughCompleted: true,
                ReverseBreakthroughCompleted: true,
                Power: 113,
                MaximumPower: 100),
            new RawCharacterCombatSkillProgress(
                687,
                true,
                null,
                33,
                44,
                false,
                false,
                false,
                PowerUnavailableReason:
                    "The standalone power context is unavailable.")));

    private sealed class Fixture : IDisposable
    {
        private Fixture(
            string root,
            string savePath,
            SaveProgressCacheStoragePathProvider provider)
        {
            Root = root;
            SavePath = savePath;
            Provider = provider;
            Cache = new SqliteCharacterCombatSkillProgressCache(provider);
        }

        public string Root { get; }

        public string SavePath { get; }

        public SaveProgressCacheStoragePathProvider Provider { get; }

        public SqliteCharacterCombatSkillProgressCache Cache { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "TaiWu.Infrastructure.UnitTests",
                Guid.NewGuid().ToString("N"));
            var helper = Path.Combine(root, "helper");
            var game = Path.Combine(root, "game");
            var saves = Path.Combine(game, "SaveGames", "world_1");
            Directory.CreateDirectory(saves);
            var savePath = Path.Combine(saves, "local.sav");
            var provider = new SaveProgressCacheStoragePathProvider(
                helper,
                [game, saves]);
            return new Fixture(root, savePath, provider);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
