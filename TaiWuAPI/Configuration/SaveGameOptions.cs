namespace TaiWuAPI.Configuration;

public sealed class SaveGameOptions
{
    public const string SectionName = "SaveGames";

    public string DefaultSaveFilePath { get; init; } = string.Empty;
}
