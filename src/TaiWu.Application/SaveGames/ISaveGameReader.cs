using TaiWu.Domain.SaveGames;

namespace TaiWu.Application.SaveGames;

public interface ISaveGameReader
{
    Task<SaveGameReport> ReadAsync(
        SaveGameReadRequest request,
        CancellationToken cancellationToken = default);
}
