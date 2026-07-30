namespace TaiWuAPI.Configuration;

public sealed class SaveGameOptions
{
    public const string SectionName = "SaveGames";
    public const string ValidationMessage =
        "SaveGames:DefaultSaveFilePath must be an absolute .sav path.";

    public string DefaultSaveFilePath { get; init; } = string.Empty;

    public bool HasValidSaveFilePath() =>
        Path.IsPathFullyQualified(DefaultSaveFilePath)
        && string.Equals(
            Path.GetExtension(DefaultSaveFilePath),
            ".sav",
            StringComparison.OrdinalIgnoreCase);
}
