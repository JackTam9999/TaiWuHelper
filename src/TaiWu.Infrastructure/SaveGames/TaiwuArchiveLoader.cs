using GameData.ArchiveData;

namespace TaiWu.Infrastructure.SaveGames;

internal interface ITaiwuArchiveLoader
{
    TaiwuArchiveLoadWarning? Load(string saveFilePath);
}

internal sealed class TaiwuArchiveLoader : ITaiwuArchiveLoader
{
    private static readonly Lock RuntimeInitializationLock = new();
    private static bool _runtimeInitialized;

    public TaiwuArchiveLoadWarning? Load(string saveFilePath)
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
            when (IsStandaloneEventRuntimeBoundary(exception))
        {
            // The standalone helper has no event-script runtime. The domains
            // used by the read-only projections have already loaded.
            return new TaiwuArchiveLoadWarning(
                TaiwuArchiveLoadWarning.StandaloneEventRuntimeUnavailable,
                exception.TargetSite?.ToString() ?? "(unknown)");
        }
    }

    private static bool IsStandaloneEventRuntimeBoundary(
        NullReferenceException exception)
    {
        return exception.TargetSite?.DeclaringType
                   == typeof(GameData.Domains.TaiwuEvent.TaiwuEventDomain)
               && exception.TargetSite.Name == "InitRuntimeEnvironment";
    }

    private static void InitializeRuntime()
    {
        lock (RuntimeInitializationLock)
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
}

internal sealed record TaiwuArchiveLoadWarning(string Code, string Detail)
{
    public const string StandaloneEventRuntimeUnavailable =
        "STANDALONE_EVENT_RUNTIME_UNAVAILABLE";
}
