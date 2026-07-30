namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuArchiveReadSession(
    IReadOnlyFileFingerprintProvider fingerprintProvider,
    ITaiwuArchiveLoader archiveLoader)
{
    // GameData stores loaded domains in process-wide static state. Keep one
    // lock across service providers as well as across the three read adapters.
    private static readonly SemaphoreSlim ProcessReaderLock = new(1, 1);

    public async Task<TResult> ReadAsync<TResult>(
        string saveFilePath,
        Func<TaiwuArchiveReadContext, CancellationToken, TResult> project,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveFilePath);
        ArgumentNullException.ThrowIfNull(project);

        var fullSaveFilePath = Path.GetFullPath(saveFilePath);
        if (!File.Exists(fullSaveFilePath))
        {
            throw new FileNotFoundException(
                "The Taiwu save file was not found.",
                fullSaveFilePath);
        }

        await ProcessReaderLock.WaitAsync(cancellationToken);
        try
        {
            var fingerprintBefore =
                await fingerprintProvider.CaptureAsync(
                    fullSaveFilePath,
                    cancellationToken);

            var loadWarning = archiveLoader.Load(fullSaveFilePath);
            cancellationToken.ThrowIfCancellationRequested();

            var result = project(
                new TaiwuArchiveReadContext(
                    fullSaveFilePath,
                    fingerprintBefore,
                    loadWarning),
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var fingerprintAfter =
                await fingerprintProvider.CaptureAsync(
                    fullSaveFilePath,
                    cancellationToken);

            if (fingerprintBefore != fingerprintAfter)
            {
                throw new InvalidDataException(
                    "The Taiwu save changed while it was being read. "
                    + "The result was discarded; retry after the save is stable.");
            }

            return result;
        }
        finally
        {
            ProcessReaderLock.Release();
        }
    }
}

internal sealed record TaiwuArchiveReadContext(
    string SaveFilePath,
    ReadOnlyFileFingerprint SourceFingerprint,
    TaiwuArchiveLoadWarning? LoadWarning);
