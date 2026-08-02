using System.Reflection;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed record TaiwuCatalogueSourcePaths(
    string GameDataConfigurationAssembly,
    string TraditionalChineseCombatSkillLanguage,
    string EnglishCombatSkillLanguage);

internal sealed record TaiwuCatalogueSourcePathResult(
    TaiwuCatalogueSourcePaths? Paths,
    string? Reason)
{
    public bool IsAvailable => Paths is not null;
}

internal interface ITaiwuCatalogueSourcePathProvider
{
    TaiwuCatalogueSourcePathResult Resolve();
}

internal sealed class TaiwuCatalogueSourcePathProvider
    : ITaiwuCatalogueSourcePathProvider
{
    internal const string GameDirectoryEnvironmentVariable =
        "TAIWU_GAME_DIRECTORY";

    private readonly string? _fixedGameDirectory;

    internal TaiwuCatalogueSourcePathProvider(string gameDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameDirectory);
        if (!Path.IsPathFullyQualified(gameDirectory))
        {
            throw new ArgumentException(
                "The trusted game installation directory must be absolute.",
                nameof(gameDirectory));
        }

        _fixedGameDirectory = Path.GetFullPath(gameDirectory);
    }

    internal TaiwuCatalogueSourcePathProvider()
    {
    }

    public TaiwuCatalogueSourcePathResult Resolve()
    {
        if (_fixedGameDirectory is not null)
        {
            return Available(_fixedGameDirectory);
        }

        var configured = Environment.GetEnvironmentVariable(
            GameDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (!Path.IsPathFullyQualified(configured))
            {
                return Missing(
                    $"{GameDirectoryEnvironmentVariable} must identify an "
                    + "absolute game installation directory.");
            }

            return Available(Path.GetFullPath(configured));
        }

        var assemblyCandidate = FindGameDirectoryFromLoadedAssembly();
        if (assemblyCandidate is not null)
        {
            return Available(assemblyCandidate);
        }

        if (OperatingSystem.IsWindows())
        {
            var programFilesX86 = Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86,
                Environment.SpecialFolderOption.DoNotVerify);
            if (!string.IsNullOrWhiteSpace(programFilesX86))
            {
                var defaultCandidate = Path.Combine(
                    programFilesX86,
                    "Steam",
                    "steamapps",
                    "common",
                    "The Scroll Of Taiwu");
                if (Directory.Exists(defaultCandidate))
                {
                    return Available(defaultCandidate);
                }
            }
        }

        return Missing(
            "The Taiwu installation could not be located. Configure the "
            + $"trusted {GameDirectoryEnvironmentVariable} value.");
    }

    private static TaiwuCatalogueSourcePathResult Available(
        string gameDirectory)
    {
        var streamingAssets = Path.Combine(
            gameDirectory,
            "The Scroll of Taiwu_Data",
            "StreamingAssets");
        return new TaiwuCatalogueSourcePathResult(
            new TaiwuCatalogueSourcePaths(
                Path.Combine(
                    gameDirectory,
                    "Backend",
                    "GameData.Shared.dll"),
                Path.Combine(
                    streamingAssets,
                    "Language_CNH",
                    "CombatSkill_language.txt"),
                Path.Combine(
                    streamingAssets,
                    "Language_EN",
                    "CombatSkill_language.txt")),
            Reason: null);
    }

    private static TaiwuCatalogueSourcePathResult Missing(string reason) =>
        new(Paths: null, reason);

    private static string? FindGameDirectoryFromLoadedAssembly()
    {
        var assemblyDirectory = new FileInfo(
            typeof(Config.CombatSkill).Assembly.Location).Directory;
        if (assemblyDirectory is null
            || !string.Equals(
                assemblyDirectory.Name,
                "Backend",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var gameDirectory = assemblyDirectory.Parent?.FullName;
        return gameDirectory is not null && Directory.Exists(gameDirectory)
            ? gameDirectory
            : null;
    }
}
