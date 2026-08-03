using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class SaveProgressCacheStoragePathProviderTests
{
    [Fact]
    public void Database_path_is_fixed_below_helper_owned_storage()
    {
        using var fixture = Fixture.Create();
        var provider = fixture.CreateProvider();

        Assert.Equal(
            Path.Combine(
                fixture.HelperData,
                SaveProgressCacheStoragePathProvider.CacheDirectoryName,
                SaveProgressCacheStoragePathProvider.DatabaseFileName),
            provider.DatabasePath);
        Assert.False(Directory.Exists(provider.CacheDirectory));
    }

    [Theory]
    [InlineData(Overlap.Equal)]
    [InlineData(Overlap.HelperInsideGame)]
    [InlineData(Overlap.GameInsideHelper)]
    public void Cache_and_game_owned_directories_cannot_overlap(Overlap overlap)
    {
        using var fixture = Fixture.Create();
        var (helperData, protectedDirectory) = overlap switch
        {
            Overlap.Equal =>
                (fixture.Game, Path.Combine(fixture.Game, "save-cache")),
            Overlap.HelperInsideGame =>
                (Path.Combine(fixture.Game, "helper"), fixture.Game),
            Overlap.GameInsideHelper =>
                (fixture.HelperData, Path.Combine(
                    fixture.HelperData,
                    "save-cache",
                    "SaveGames")),
            _ => throw new ArgumentOutOfRangeException(nameof(overlap))
        };

        Assert.Throws<ArgumentException>(() =>
            new SaveProgressCacheStoragePathProvider(
                helperData,
                [protectedDirectory]));
    }

    [Fact]
    public void Resolving_the_path_does_not_change_protected_directories()
    {
        using var fixture = Fixture.Create();
        var gameTimestamp = Directory.GetLastWriteTimeUtc(fixture.Game);
        var savesTimestamp = Directory.GetLastWriteTimeUtc(fixture.Saves);

        _ = fixture.CreateProvider().DatabasePath;

        Assert.Equal(gameTimestamp, Directory.GetLastWriteTimeUtc(fixture.Game));
        Assert.Equal(savesTimestamp, Directory.GetLastWriteTimeUtc(fixture.Saves));
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.Game));
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.Saves));
    }

    public enum Overlap
    {
        Equal,
        HelperInsideGame,
        GameInsideHelper
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(string root, string helperData, string game, string saves)
        {
            Root = root;
            HelperData = helperData;
            Game = game;
            Saves = saves;
        }

        public string Root { get; }

        public string HelperData { get; }

        public string Game { get; }

        public string Saves { get; }

        public static Fixture Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "TaiWu.Infrastructure.UnitTests",
                Guid.NewGuid().ToString("N"));
            var helperData = Path.Combine(root, "helper");
            var game = Path.Combine(root, "game");
            var saves = Path.Combine(root, "saves");
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(game);
            Directory.CreateDirectory(saves);
            return new Fixture(root, helperData, game, saves);
        }

        public SaveProgressCacheStoragePathProvider CreateProvider() =>
            new(HelperData, [Game, Saves]);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
