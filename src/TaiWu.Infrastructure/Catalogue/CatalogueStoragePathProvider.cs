namespace TaiWu.Infrastructure.Catalogue;

internal sealed class CatalogueStoragePathProvider
{
    internal const string CatalogueDirectoryName = "catalogue";
    internal const string DatabaseFileName = "combat-skill-catalogue.db";
    internal const string RebuildDatabaseFileName =
        "combat-skill-catalogue.rebuild.db";

    private readonly string _catalogueDirectory;
    private readonly string _databasePath;

    internal CatalogueStoragePathProvider(
        string helperDataDirectory,
        IEnumerable<string> protectedGameOwnedDirectories)
    {
        ArgumentNullException.ThrowIfNull(protectedGameOwnedDirectories);

        var normalizedHelperDirectory = NormalizeRequiredDirectory(
            helperDataDirectory,
            nameof(helperDataDirectory));
        var protectedDirectories = protectedGameOwnedDirectories
            .Select(
                directory => NormalizeRequiredDirectory(
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

        _catalogueDirectory = NormalizeDirectory(
            Path.Combine(
                normalizedHelperDirectory,
                CatalogueDirectoryName));
        _databasePath = Path.Combine(
            _catalogueDirectory,
            DatabaseFileName);

        foreach (var protectedDirectory in protectedDirectories)
        {
            if (PathsOverlap(_catalogueDirectory, protectedDirectory))
            {
                throw new ArgumentException(
                    "The helper-owned catalogue directory must not equal, "
                    + "contain, or be contained by a game-owned directory.",
                    nameof(helperDataDirectory));
            }
        }

        EnsureNoExistingReparsePoint(_catalogueDirectory);
    }

    internal static CatalogueStoragePathProvider CreateDefault(
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

        return new CatalogueStoragePathProvider(
            Path.Combine(localApplicationData, "TaiWuHelper"),
            protectedGameOwnedDirectories);
    }

    internal string CatalogueDirectory
    {
        get
        {
            EnsureNoExistingReparsePoint(_catalogueDirectory);
            return _catalogueDirectory;
        }
    }

    internal string DatabasePath => EnsureOwnedFilePath(_databasePath);

    internal string RebuildDatabasePath => EnsureOwnedFilePath(
        Path.Combine(_catalogueDirectory, RebuildDatabaseFileName));

    internal string EnsureOwnedFilePath(string candidatePath)
    {
        var normalizedCandidate = NormalizeRequiredFile(
            candidatePath,
            nameof(candidatePath));
        var parentDirectory = Path.GetDirectoryName(normalizedCandidate);

        if (parentDirectory is null
            || !PathComparer.Equals(
                NormalizeDirectory(parentDirectory),
                _catalogueDirectory))
        {
            throw new ArgumentException(
                "Catalogue files must be direct children of the validated "
                + "helper-owned catalogue directory.",
                nameof(candidatePath));
        }

        var fileName = Path.GetFileName(normalizedCandidate);
        if (!IsAllowedFileName(fileName))
        {
            throw new ArgumentException(
                "The path is not a recognized catalogue database file.",
                nameof(candidatePath));
        }

        if (Directory.Exists(normalizedCandidate))
        {
            throw new ArgumentException(
                "A catalogue file path cannot identify a directory.",
                nameof(candidatePath));
        }

        EnsureNoExistingReparsePoint(normalizedCandidate);
        return normalizedCandidate;
    }

    private static string NormalizeRequiredDirectory(
        string path,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "A fully qualified directory is required.",
                parameterName);
        }

        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException(
                "The directory must be fully qualified.",
                parameterName);
        }

        return NormalizeDirectory(path);
    }

    private static string NormalizeRequiredFile(
        string path,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "A fully qualified file path is required.",
                parameterName);
        }

        var fileName = Path.GetFileName(path);
        if (!Path.IsPathFullyQualified(path)
            || string.IsNullOrWhiteSpace(fileName)
            || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                "The file path must be fully qualified.",
                parameterName);
        }

        return Path.GetFullPath(path);
    }

    private static bool IsAllowedFileName(string fileName) =>
        PathComparer.Equals(fileName, DatabaseFileName)
        || PathComparer.Equals(fileName, RebuildDatabaseFileName);

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

        var directoryPrefix = Path.EndsInDirectorySeparator(directory)
            ? directory
            : directory + Path.DirectorySeparatorChar;
        return candidate.StartsWith(directoryPrefix, PathComparison);
    }

    private static void EnsureNoExistingReparsePoint(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var pathRoot = Path.GetPathRoot(fullPath);
        if (string.IsNullOrEmpty(pathRoot))
        {
            throw new ArgumentException(
                "The path has no filesystem root.",
                nameof(path));
        }

        var current = pathRoot;
        var relative = fullPath[pathRoot.Length..];
        var segments = relative.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            current = Path.Combine(current, segment);
            if (!Directory.Exists(current) && !File.Exists(current))
            {
                break;
            }

            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException(
                    "The helper-owned catalogue path cannot traverse a "
                    + $"symbolic link or reparse point: '{current}'.");
            }
        }
    }

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
