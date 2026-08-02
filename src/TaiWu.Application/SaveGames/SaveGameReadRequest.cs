namespace TaiWu.Application.SaveGames;

public sealed record SaveGameReadRequest(
    string SaveFilePath,
    int? TargetCharacterId = null);
