namespace TaiWu.Infrastructure.SaveGames;

internal sealed class SaveProgressCacheStoragePathProvider
{
    internal const string CacheDirectoryName = "save-cache";
    internal const string DatabaseFileName =
        "character-combat-skill-progress.db";

    private readonly string _cacheDirectory;
    private readonly string _databasePath;

    internal SaveProgressCacheStoragePathProvider(
        string helperDataDirectory,
        IEnumerable<string> protectedGameOwnedDirectories)
    {
        ArgumentNullException.ThrowIfNull(protectedGameOwnedDirectories);

        var helperDirectory = NormalizeRequiredDirectory(
            helperDataDirectory,
            nameof(helperDataDirectory));
        var protectedDirectories = protectedGameOwnedDirectories
            .Select(directory => NormalizeRequiredDirectory(
                directory,
                nameof(protectedGameOwnedDirectories)))
            .Distinct(PathComparer)
            .ToArray();
        if (protectedDirectories.Length == 0)
        {
            throw new ArgumentException(
                "At least one game-owned directory must be protected.",
                nameof(protectedGameOwnedDirectories));
        }

        _cacheDirectory = NormalizeDirectory(Path.Combine(
            helperDirectory,
            CacheDirectoryName));
        _databasePath = Path.Combine(_cacheDirectory, DatabaseFileName);

        if (protectedDirectories.Any(directory =>
                PathsOverlap(_cacheDirectory, directory)))
        {
            throw new ArgumentException(
                "The helper-owned save cache must not overlap a game-owned "
                + "directory.",
                nameof(helperDataDirectory));
        }

        EnsureNoExistingReparsePoint(_cacheDirectory);
    }

    internal static SaveProgressCacheStoragePathProvider CreateDefault(
        IEnumerable<string> protectedGameOwnedDirectories)
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException(
                "The local application-data directory is unavailable.");
        }

        return new SaveProgressCacheStoragePathProvider(
            Path.Combine(localApplicationData, "TaiWuHelper"),
            protectedGameOwnedDirectories);
    }

    internal string CacheDirectory
    {
        get
        {
            EnsureNoExistingReparsePoint(_cacheDirectory);
            return _cacheDirectory;
        }
    }

    internal string DatabasePath
    {
        get
        {
            EnsureNoExistingReparsePoint(_databasePath);
            if (Directory.Exists(_databasePath))
            {
                throw new InvalidOperationException(
                    "The save-cache database path identifies a directory.");
            }

            return _databasePath;
        }
    }

    private static string NormalizeRequiredDirectory(
        string path,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path)
            || !Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException(
                "A fully qualified directory is required.",
                parameterName);
        }

        return NormalizeDirectory(path);
    }

    private static string NormalizeDirectory(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool PathsOverlap(string left, string right) =>
        IsSameOrDescendant(left, right)
        || IsSameOrDescendant(right, left);

    private static bool IsSameOrDescendant(
        string candidate,
        string directory)
    {
        if (PathComparer.Equals(candidate, directory))
        {
            return true;
        }

        var prefix = Path.EndsInDirectorySeparator(directory)
            ? directory
            : directory + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, PathComparison);
    }

    private static void EnsureNoExistingReparsePoint(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(root))
        {
            throw new ArgumentException(
                "The path has no filesystem root.",
                nameof(path));
        }

        var current = root;
        foreach (var segment in fullPath[root.Length..].Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!Directory.Exists(current) && !File.Exists(current))
            {
                break;
            }

            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    "The save-cache path cannot traverse a symbolic link or "
                    + $"reparse point: '{current}'.");
            }
        }
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
