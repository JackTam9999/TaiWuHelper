namespace TaiWu.Application.GameData;

/// <summary>
/// Marks an Application port that may query game-owned data but can never
/// mutate or control the game.
/// </summary>
public interface IReadOnlyGameDataSource;
