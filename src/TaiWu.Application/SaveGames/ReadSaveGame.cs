using TaiWu.Domain.SaveGames;

namespace TaiWu.Application.SaveGames;

public sealed class ReadSaveGame(ISaveGameReader reader)
{
    public Task<SaveGameReport> ExecuteAsync(
        SaveGameReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SaveFilePath))
        {
            throw new ArgumentException(
                "A save-file path is required.",
                nameof(request));
        }

        return reader.ReadAsync(request, cancellationToken);
    }
}
