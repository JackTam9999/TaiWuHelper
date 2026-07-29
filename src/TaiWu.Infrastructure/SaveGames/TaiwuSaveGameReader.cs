using GameData.Domains;
using TaiWu.Application.SaveGames;
using TaiWu.Domain.SaveGames;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuSaveGameReader : ISaveGameReader
{
    public Task<SaveGameReport> ReadAsync(
        SaveGameReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TaiwuArchiveReadSession.ReadAsync(
            request.SaveFilePath,
            context => ReadLoadedArchive(
                context,
                request.TargetCharacterId,
                cancellationToken),
            cancellationToken);
    }

    private static SaveGameReport ReadLoadedArchive(
        TaiwuArchiveReadContext readContext,
        int? targetCharacterId,
        CancellationToken cancellationToken)
    {
        var writer = new LegacyReportWriter();
        writer.Write("CONFIGS|{0}", Config.ConfigCollection.Items.Length);
        if (readContext.LoadWarning is not null)
        {
            writer.Write(
                "LOADWARNING|{0}",
                readContext.LoadWarning);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        var taiwu = DomainManager.Taiwu.GetTaiwu() ?? throw new InvalidDataException(
                "The archive stopped loading before the Taiwu character was available. "
                + writer.Build().ToLegacyText());
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
}
