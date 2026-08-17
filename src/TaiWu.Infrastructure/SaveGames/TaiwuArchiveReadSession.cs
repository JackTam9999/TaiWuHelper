using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuArchiveReadSession(
    IReadOnlyFileRevisionProvider revisionProvider,
    IReadOnlyFileFingerprintProvider fingerprintProvider,
    ITaiwuArchiveLoader archiveLoader,
    TimeProvider? timeProvider = null,
    ILogger<TaiwuArchiveReadSession>? logger = null)
{
    // GameData stores loaded domains in process-wide static state. Keep one
    // lock across service providers as well as across all read adapters.
    private static readonly SemaphoreSlim ProcessReaderLock = new(1, 1);
    private static LoadedArchive? CurrentArchive;
    private readonly TimeProvider _timeProvider = timeProvider
        ?? TimeProvider.System;
    private readonly ILogger<TaiwuArchiveReadSession> _logger = logger
        ?? NullLogger<TaiwuArchiveReadSession>.Instance;

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
            var totalStarted = _timeProvider.GetTimestamp();
            var revisionStarted = _timeProvider.GetTimestamp();
            var revisionBefore = revisionProvider.Capture(fullSaveFilePath);
            var revisionElapsed = _timeProvider.GetElapsedTime(revisionStarted);
            if (CanReuseCurrentArchive(fullSaveFilePath, revisionBefore))
            {
                var reusedProjectStarted = _timeProvider.GetTimestamp();
                var reusedResult = ProjectCurrentArchive(
                    fullSaveFilePath,
                    revisionBefore,
                    project,
                    cancellationToken);
                LogTiming(
                    reused: true,
                    revisionElapsed,
                    fingerprintBeforeElapsed: TimeSpan.Zero,
                    loadElapsed: TimeSpan.Zero,
                    projectElapsed: _timeProvider.GetElapsedTime(
                        reusedProjectStarted),
                    fingerprintAfterElapsed: TimeSpan.Zero,
                    totalElapsed: _timeProvider.GetElapsedTime(totalStarted));
                return reusedResult;
            }

            // Loading a different or changed archive replaces GameData's
            // process-wide state. Do not expose an earlier cache entry if the
            // new load or its verification fails.
            CurrentArchive = null;
            var fingerprintBeforeStarted = _timeProvider.GetTimestamp();
            var fingerprintBefore =
                await fingerprintProvider.CaptureAsync(
                    fullSaveFilePath,
                    cancellationToken);
            var fingerprintBeforeElapsed = _timeProvider.GetElapsedTime(
                fingerprintBeforeStarted);

            var loadStarted = _timeProvider.GetTimestamp();
            var loadWarning = archiveLoader.Load(fullSaveFilePath);
            var loadElapsed = _timeProvider.GetElapsedTime(loadStarted);
            cancellationToken.ThrowIfCancellationRequested();

            var projectStarted = _timeProvider.GetTimestamp();
            var result = project(
                new TaiwuArchiveReadContext(
                    fullSaveFilePath,
                    fingerprintBefore,
                    loadWarning),
                cancellationToken);
            var projectElapsed = _timeProvider.GetElapsedTime(projectStarted);
            cancellationToken.ThrowIfCancellationRequested();

            var fingerprintAfterStarted = _timeProvider.GetTimestamp();
            var fingerprintAfter =
                await fingerprintProvider.CaptureAsync(
                    fullSaveFilePath,
                    cancellationToken);
            var fingerprintAfterElapsed = _timeProvider.GetElapsedTime(
                fingerprintAfterStarted);

            if (fingerprintBefore != fingerprintAfter)
            {
                throw new TaiwuArchiveChangedException();
            }

            CurrentArchive = new LoadedArchive(
                fullSaveFilePath,
                ReadOnlyFileRevision.From(fingerprintAfter),
                fingerprintBefore,
                loadWarning);
            LogTiming(
                reused: false,
                revisionElapsed,
                fingerprintBeforeElapsed,
                loadElapsed,
                projectElapsed,
                fingerprintAfterElapsed,
                _timeProvider.GetElapsedTime(totalStarted));
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
            throw new TaiwuArchiveChangedException();
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

    private void LogTiming(
        bool reused,
        TimeSpan revisionElapsed,
        TimeSpan fingerprintBeforeElapsed,
        TimeSpan loadElapsed,
        TimeSpan projectElapsed,
        TimeSpan fingerprintAfterElapsed,
        TimeSpan totalElapsed)
    {
        _logger.LogInformation(
            "Taiwu archive read: reused={Reused}; revisionMs={RevisionMs:F0}; "
            + "fingerprintBeforeMs={FingerprintBeforeMs:F0}; loadMs={LoadMs:F0}; "
            + "projectMs={ProjectMs:F0}; fingerprintAfterMs={FingerprintAfterMs:F0}; "
            + "totalMs={TotalMs:F0}.",
            reused,
            revisionElapsed.TotalMilliseconds,
            fingerprintBeforeElapsed.TotalMilliseconds,
            loadElapsed.TotalMilliseconds,
            projectElapsed.TotalMilliseconds,
            fingerprintAfterElapsed.TotalMilliseconds,
            totalElapsed.TotalMilliseconds);
    }

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

internal sealed class TaiwuArchiveChangedException : IOException
{
    public TaiwuArchiveChangedException()
        : base(
            "The Taiwu save changed while it was being read. "
            + "The result was discarded; retry after the save is stable.")
    {
    }
}
