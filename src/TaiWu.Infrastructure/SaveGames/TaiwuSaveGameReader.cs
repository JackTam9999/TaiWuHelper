using GameData.ArchiveData;
using GameData.Domains;
using TaiWu.Application.SaveGames;
using TaiWu.Domain.SaveGames;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuSaveGameReader : ISaveGameReader
{
    private static readonly SemaphoreSlim ReaderLock = new(1, 1);
    private static bool _runtimeInitialized;

    public async Task<SaveGameReport> ReadAsync(
        SaveGameReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var saveFilePath = Path.GetFullPath(request.SaveFilePath);
        if (!File.Exists(saveFilePath))
        {
            throw new FileNotFoundException(
                "The Taiwu save file was not found.",
                saveFilePath);
        }

        await ReaderLock.WaitAsync(cancellationToken);
        try
        {
            var fingerprintBefore = await ReadOnlyFileFingerprint.CaptureAsync(
                saveFilePath,
                cancellationToken);

            var report = ReadCore(
                saveFilePath,
                request.TargetCharacterId,
                cancellationToken);

            var fingerprintAfter = await ReadOnlyFileFingerprint.CaptureAsync(
                saveFilePath,
                cancellationToken);

            if (fingerprintBefore != fingerprintAfter)
            {
                throw new InvalidDataException(
                    "The Taiwu save changed while it was being read. "
                    + "The result was discarded; retry after the save is stable.");
            }

            return report;
        }
        finally
        {
            ReaderLock.Release();
        }
    }

    private static SaveGameReport ReadCore(
        string saveFilePath,
        int? targetCharacterId,
        CancellationToken cancellationToken)
    {
        var writer = new LegacyReportWriter();
        InitializeRuntime();
        writer.Write("CONFIGS|{0}", Config.ConfigCollection.Items.Length);

        // Archive loading registers one-shot data modification handlers.
        // Remove handlers left by the previous read before loading again.
        GameData.GameDataBridge.GameDataBridge.ClearMonitoredData();

        var archive = new LocalArchiveFile(saveFilePath);
        try
        {
            archive.Load();
        }
        catch (NullReferenceException exception)
            when (exception.TargetSite?.DeclaringType
                    == typeof(GameData.Domains.TaiwuEvent.TaiwuEventDomain)
                && exception.TargetSite.Name == "InitRuntimeEnvironment")
        {
            // The standalone reader has no event-script runtime. The domains
            // needed by this report have already loaded at this point.
            writer.Write(
                "LOADWARNING|{0}",
                exception.TargetSite?.ToString() ?? "(unknown)");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        var taiwu = DomainManager.Taiwu.GetTaiwu();
        if (taiwu is null)
        {
            throw new InvalidDataException(
                "The archive stopped loading before the Taiwu character was available. "
                + writer.Build().ToLegacyText());
        }

        var equipment = taiwu.GetCombatSkillEquipment();
        HashSet<short> equippedSkillIds = [];
        equipment.GetValidSkills(equippedSkillIds);

        var context = new TaiwuReportContext(
            writer,
            taiwuId,
            taiwu,
            equipment,
            equippedSkillIds,
            targetCharacterId);

        OverviewReportSection.Write(context);
        CombatSkillReportSection.Write(context);
        LegendaryBookReportSection.Write(context);
        StoryReportSection.Write(context);

        return writer.Build();
    }

    private static void InitializeRuntime()
    {
        if (_runtimeInitialized)
        {
            return;
        }

        foreach (Config.Common.IConfigData config in Config.ConfigCollection.Items)
        {
            config.Init();
        }

        GameData.ActionPlanning.MonthlyAI.CharacterActionPlanner.Instance.Initialize();
        _runtimeInitialized = true;
    }
}
