namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuArchiveReadSession(
    IReadOnlyFileRevisionProvider revisionProvider,
    IReadOnlyFileFingerprintProvider fingerprintProvider,
    ITaiwuArchiveLoader archiveLoader)
{
    // GameData stores loaded domains in process-wide static state. Keep one
    // lock across service providers as well as across all read adapters.
    private static readonly SemaphoreSlim ProcessReaderLock = new(1, 1);
    private static LoadedArchive? CurrentArchive;

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
            var revisionBefore = revisionProvider.Capture(fullSaveFilePath);
            if (CanReuseCurrentArchive(fullSaveFilePath, revisionBefore))
            {
                return ProjectCurrentArchive(
                    fullSaveFilePath,
                    revisionBefore,
                    project,
                    cancellationToken);
            }

            // Loading a different or changed archive replaces GameData's
            // process-wide state. Do not expose an earlier cache entry if the
            // new load or its verification fails.
            CurrentArchive = null;
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

            CurrentArchive = new LoadedArchive(
                fullSaveFilePath,
                ReadOnlyFileRevision.From(fingerprintAfter),
                fingerprintBefore,
                loadWarning);
            return result;
        }
        finally
        {
            ProcessReaderLock.Release();
        }
    }

    private static bool CanReuseCurrentArchive(
        string saveFilePath,
        ReadOnlyFileRevision revision) =>
        CurrentArchive is { } current
        && PathsEqual(current.SaveFilePath, saveFilePath)
        && current.Revision == revision;

    private TResult ProjectCurrentArchive<TResult>(
        string saveFilePath,
        ReadOnlyFileRevision revisionBefore,
        Func<TaiwuArchiveReadContext, CancellationToken, TResult> project,
        CancellationToken cancellationToken)
    {
        var current = CurrentArchive
            ?? throw new InvalidOperationException(
                "The reusable Taiwu archive is unavailable.");
        var result = project(
            new TaiwuArchiveReadContext(
                saveFilePath,
                current.SourceFingerprint,
                current.LoadWarning),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var revisionAfter = revisionProvider.Capture(saveFilePath);
        if (revisionBefore != revisionAfter)
        {
            CurrentArchive = null;
            throw new InvalidDataException(
                "The Taiwu save changed while it was being read. "
                + "The result was discarded; retry after the save is stable.");
        }

        return result;
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            left,
            right,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal);

    private sealed record LoadedArchive(
        string SaveFilePath,
        ReadOnlyFileRevision Revision,
        ReadOnlyFileFingerprint SourceFingerprint,
        TaiwuArchiveLoadWarning? LoadWarning);
}

internal sealed record TaiwuArchiveReadContext(
    string SaveFilePath,
    ReadOnlyFileFingerprint SourceFingerprint,
    TaiwuArchiveLoadWarning? LoadWarning);
