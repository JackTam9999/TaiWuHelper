using GameData.ArchiveData;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.TaiwuEvent;
using System.Reflection;

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
        InitializeStandaloneEventRuntime(saveFilePath);

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
            when (TryGetStandaloneBoundary(exception, out var warningCode))
        {
            // The helper supplies only the minimum event runtime required to
            // let archive deserialization reach the read-only story domains.
            // A later live-game recalculation still has no complete runtime;
            // all domains used by projections have loaded by this boundary.
            return new TaiwuArchiveLoadWarning(
                warningCode,
                exception.TargetSite?.ToString() ?? "(unknown)");
        }
    }

    private static bool TryGetStandaloneBoundary(
        NullReferenceException exception,
        out string warningCode)
    {
        if (exception.TargetSite?.DeclaringType == typeof(TaiwuEventDomain)
            && exception.TargetSite.Name == "InitRuntimeEnvironment")
        {
            warningCode =
                TaiwuArchiveLoadWarning.StandaloneEventRuntimeUnavailable;
            return true;
        }

        if (exception.TargetSite?.DeclaringType
                == typeof(GameData.Domains.Extra.ExtraDomain)
            && exception.TargetSite.Name == "CheckCombatSkillOrderPlan")
        {
            warningCode =
                TaiwuArchiveLoadWarning.StandaloneLiveRuntimeUnavailable;
            return true;
        }

        warningCode = string.Empty;
        return false;
    }

    private static void InitializeStandaloneEventRuntime(string saveFilePath)
    {
        var gameDirectory = FindGameDirectory(saveFilePath);
        Common.Initialize(gameDirectory, false);

        var runtimeField = typeof(TaiwuEventDomain).GetField(
            "_scriptRuntime",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidDataException(
                "The installed GameData event runtime field is unavailable.");
        if (runtimeField.GetValue(null) is not null)
        {
            return;
        }

        var contextField = typeof(TaiwuEventDomain).GetField(
            "MainThreadDataContext",
            BindingFlags.Instance
            | BindingFlags.NonPublic
            | BindingFlags.Public)
            ?? throw new InvalidDataException(
                "The installed GameData event context field is unavailable.");
        var context = contextField.GetValue(DomainManager.TaiwuEvent)
            as DataContext
            ?? throw new InvalidDataException(
                "The installed GameData event context is unavailable.");
        runtimeField.SetValue(
            null,
            new EventScriptRuntime(context, false));
    }

    private static string FindGameDirectory(string saveFilePath)
    {
        var directory = new FileInfo(Path.GetFullPath(saveFilePath)).Directory;
        while (directory is not null
               && !string.Equals(
                   directory.Name,
                   "SaveGames",
                   StringComparison.OrdinalIgnoreCase))
        {
            directory = directory.Parent;
        }

        return directory?.Parent?.FullName
            ?? throw new InvalidDataException(
                "The configured save must be located below a SaveGames "
                + "directory so the standalone archive runtime can be "
                + "initialized read only.");
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

    public const string StandaloneLiveRuntimeUnavailable =
        "STANDALONE_LIVE_RUNTIME_UNAVAILABLE";
}
