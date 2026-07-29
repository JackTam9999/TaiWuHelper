using GameData.ArchiveData;

namespace TaiWu.Infrastructure.SaveGames;

internal static class TaiwuArchiveReadSession
{
    private static readonly SemaphoreSlim ReaderLock = new(1, 1);
    private static bool _runtimeInitialized;

    public static async Task<TResult> ReadAsync<TResult>(
        string saveFilePath,
        Func<TaiwuArchiveReadContext, TResult> project,
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

        await ReaderLock.WaitAsync(cancellationToken);
        try
        {
            var fingerprintBefore =
                await ReadOnlyFileFingerprint.CaptureAsync(
                    fullSaveFilePath,
                    cancellationToken);

            var loadWarning = LoadArchive(fullSaveFilePath);
            cancellationToken.ThrowIfCancellationRequested();

            var result = project(
                new TaiwuArchiveReadContext(
                    fullSaveFilePath,
                    fingerprintBefore,
                    loadWarning));

            var fingerprintAfter =
                await ReadOnlyFileFingerprint.CaptureAsync(
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
            ReaderLock.Release();
        }
    }

    private static string? LoadArchive(string saveFilePath)
    {
        InitializeRuntime();

        // Archive loading registers one-shot data modification handlers.
        // Removing handlers left by an earlier read keeps repeated queries
        // deterministic without writing to the archive or live game.
        GameData.GameDataBridge.GameDataBridge.ClearMonitoredData();

        var archive = new LocalArchiveFile(saveFilePath);
        try
        {
            archive.Load();
            return null;
        }
        catch (NullReferenceException exception)
            when (exception.TargetSite?.DeclaringType
                    == typeof(GameData.Domains.TaiwuEvent.TaiwuEventDomain)
                && exception.TargetSite.Name == "InitRuntimeEnvironment")
        {
            // The standalone helper has no event-script runtime. The domains
            // used by the read-only projections have already loaded.
            return exception.TargetSite?.ToString() ?? "(unknown)";
        }
    }

    private static void InitializeRuntime()
    {
        if (_runtimeInitialized)
        {
            return;
        }

        foreach (Config.Common.IConfigData config
                 in Config.ConfigCollection.Items)
        {
            config.Init();
        }

        GameData.ActionPlanning.MonthlyAI.CharacterActionPlanner
            .Instance
            .Initialize();
        _runtimeInitialized = true;
    }
}

internal sealed record TaiwuArchiveReadContext(
    string SaveFilePath,
    ReadOnlyFileFingerprint SourceFingerprint,
    string? LoadWarning);
