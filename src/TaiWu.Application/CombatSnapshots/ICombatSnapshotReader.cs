using TaiWu.Application.GameData;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.CombatSnapshots;

/// <summary>
/// Queries one immutable combat snapshot from a game-owned save source.
/// Implementations must never modify or control the game.
/// </summary>
public interface ICombatSnapshotReader : IReadOnlyGameDataSource
{
    Task<CombatSnapshot> ReadAsync(
        CombatSnapshotReadRequest request,
        CancellationToken cancellationToken = default);
}
