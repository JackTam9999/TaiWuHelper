using TaiWu.Domain.SaveGames;

namespace TaiWuAPI.Contracts.SaveGames;

internal static class SaveGameResponseMapper
{
    internal const string SchemaVersion = "1";

    public static SaveGameResponse Map(SaveGameReport source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new SaveGameResponse(
            SchemaVersion,
            source.Lines,
            source.ToLegacyText());
    }
}
