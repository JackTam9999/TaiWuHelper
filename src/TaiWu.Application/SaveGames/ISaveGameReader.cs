using TaiWu.Application.GameData;
using TaiWu.Domain.SaveGames;

namespace TaiWu.Application.SaveGames;

public interface ISaveGameReader : IReadOnlyGameDataSource
{
    Task<SaveGameReport> ReadAsync(
        SaveGameReadRequest request,
        CancellationToken cancellationToken = default);
}
