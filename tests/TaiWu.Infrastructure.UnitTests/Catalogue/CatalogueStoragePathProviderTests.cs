using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class CatalogueStoragePathProviderTests
{
    [Fact]
    public void Database_path_is_a_fixed_helper_owned_location()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();

        Assert.Equal(
            Path.Combine(
                fixture.HelperData,
                CatalogueStoragePathProvider.CatalogueDirectoryName,
                CatalogueStoragePathProvider.DatabaseFileName),
            provider.DatabasePath);
        Assert.Equal(
            Path.Combine(
                fixture.HelperData,
                CatalogueStoragePathProvider.CatalogueDirectoryName,
                CatalogueStoragePathProvider.RebuildDatabaseFileName),
            provider.RebuildDatabasePath);
        Assert.False(Directory.Exists(provider.CatalogueDirectory));
        Assert.False(File.Exists(provider.DatabasePath));
    }

    [Fact]
    public void Direct_helper_owned_sibling_file_is_allowed()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();
        var rebuildPath = Path.Combine(
            provider.CatalogueDirectory,
            CatalogueStoragePathProvider.RebuildDatabaseFileName);

        Assert.Equal(
            Path.GetFullPath(rebuildPath),
            provider.EnsureOwnedFilePath(rebuildPath));
    }

    [Fact]
    public void Unknown_direct_sibling_file_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();
        var unknownPath = Path.Combine(
            provider.CatalogueDirectory,
            "unrelated.db");

        Assert.Throws<ArgumentException>(
            () => provider.EnsureOwnedFilePath(unknownPath));
    }

    [Theory]
    [InlineData(ProtectedOverlap.Equal)]
    [InlineData(ProtectedOverlap.HelperInsideGame)]
    [InlineData(ProtectedOverlap.GameInsideHelper)]
    public void Catalogue_and_game_owned_directories_cannot_overlap(
        ProtectedOverlap overlap)
    {
        using var fixture = TemporaryDirectories.Create();
        var (helperData, protectedDirectory) = overlap switch
        {
            ProtectedOverlap.Equal =>
                (fixture.Game, Path.Combine(fixture.Game, "catalogue")),
            ProtectedOverlap.HelperInsideGame =>
                (Path.Combine(fixture.Game, "helper"), fixture.Game),
            ProtectedOverlap.GameInsideHelper =>
                (fixture.HelperData, Path.Combine(
                    fixture.HelperData,
                    "catalogue",
                    "SaveGames")),
            _ => throw new ArgumentOutOfRangeException(nameof(overlap))
        };

        Assert.Throws<ArgumentException>(
            () => new CatalogueStoragePathProvider(
                helperData,
                [protectedDirectory]));
    }

    [Fact]
    public void Traversal_outside_catalogue_directory_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();
        var escapedPath = Path.Combine(
            provider.CatalogueDirectory,
            "..",
            "outside.db");

        Assert.Throws<ArgumentException>(
            () => provider.EnsureOwnedFilePath(escapedPath));
    }

    [Fact]
    public void Nested_catalogue_file_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();
        var nestedPath = Path.Combine(
            provider.CatalogueDirectory,
            "nested",
            "catalogue.db");

        Assert.Throws<ArgumentException>(
            () => provider.EnsureOwnedFilePath(nestedPath));
    }

    [Fact]
    public void Game_owned_file_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();

        Assert.Throws<ArgumentException>(
            () => provider.EnsureOwnedFilePath(
                Path.Combine(fixture.Game, "catalogue.db")));
    }

    [Fact]
    public void Similar_directory_prefix_is_not_treated_as_overlap()
    {
        using var fixture = TemporaryDirectories.Create();
        var gameCopy = fixture.Game + "-copy";

        var provider = new CatalogueStoragePathProvider(
            gameCopy,
            [fixture.Game, fixture.Saves]);

        Assert.Equal(
            Path.Combine(
                gameCopy,
                CatalogueStoragePathProvider.CatalogueDirectoryName,
                CatalogueStoragePathProvider.DatabaseFileName),
            provider.DatabasePath);
    }

    [Fact]
    public void Windows_path_case_does_not_bypass_the_boundary()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var fixture = TemporaryDirectories.Create();
        var helperInsideGame = Path.Combine(fixture.Game, "HELPER");

        Assert.Throws<ArgumentException>(
            () => new CatalogueStoragePathProvider(
                helperInsideGame.ToUpperInvariant(),
                [fixture.Game.ToLowerInvariant()]));
    }

    [Fact]
    public void Relative_helper_directory_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();

        Assert.Throws<ArgumentException>(
            () => new CatalogueStoragePathProvider(
                "relative-helper-data",
                [fixture.Game]));
    }

    [Fact]
    public void Relative_candidate_file_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();

        Assert.Throws<ArgumentException>(
            () => provider.EnsureOwnedFilePath("catalogue.db"));
    }

    [Fact]
    public void Relative_protected_directory_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();

        Assert.Throws<ArgumentException>(
            () => new CatalogueStoragePathProvider(
                fixture.HelperData,
                ["relative-game-directory"]));
    }

    [Fact]
    public void Empty_protected_directory_set_is_rejected()
    {
        using var fixture = TemporaryDirectories.Create();

        Assert.Throws<ArgumentException>(
            () => new CatalogueStoragePathProvider(
                fixture.HelperData,
                []));
    }

    [Fact]
    public void Existing_directory_cannot_be_used_as_a_catalogue_file()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();
        Directory.CreateDirectory(provider.CatalogueDirectory);
        var directoryPath = Path.Combine(
            provider.CatalogueDirectory,
            "not-a-file.db");
        Directory.CreateDirectory(directoryPath);

        Assert.Throws<ArgumentException>(
            () => provider.EnsureOwnedFilePath(directoryPath));
    }

    [Fact]
    public void Reparse_point_added_after_construction_is_rejected_when_supported()
    {
        using var fixture = TemporaryDirectories.Create();
        var provider = fixture.CreateProvider();
        var linkTarget = Path.Combine(fixture.Root, "link-target");
        Directory.CreateDirectory(linkTarget);

        try
        {
            Directory.CreateSymbolicLink(fixture.HelperData, linkTarget);
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException
                or PlatformNotSupportedException
                or IOException)
        {
            return;
        }

        Assert.Throws<InvalidOperationException>(() => _ = provider.DatabasePath);
    }

    [Fact]
    public void Provider_does_not_create_or_change_protected_directories()
    {
        using var fixture = TemporaryDirectories.Create();
        var gameTimestamp = Directory.GetLastWriteTimeUtc(fixture.Game);
        var savesTimestamp = Directory.GetLastWriteTimeUtc(fixture.Saves);

        var provider = fixture.CreateProvider();
        _ = provider.DatabasePath;

        Assert.Equal(gameTimestamp, Directory.GetLastWriteTimeUtc(fixture.Game));
        Assert.Equal(savesTimestamp, Directory.GetLastWriteTimeUtc(fixture.Saves));
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.Game));
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.Saves));
    }

    public enum ProtectedOverlap
    {
        Equal,
        HelperInsideGame,
        GameInsideHelper
    }

    private sealed class TemporaryDirectories : IDisposable
    {
        private TemporaryDirectories(
            string root,
            string helperData,
            string game,
            string saves)
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

        public static TemporaryDirectories Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "TaiWu.Infrastructure.UnitTests",
                Guid.NewGuid().ToString("N"));
            var helperData = Path.Combine(root, "helper-data");
            var game = Path.Combine(root, "game");
            var saves = Path.Combine(root, "saves");
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(game);
            Directory.CreateDirectory(saves);
            return new TemporaryDirectories(root, helperData, game, saves);
        }

        public CatalogueStoragePathProvider CreateProvider() =>
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
