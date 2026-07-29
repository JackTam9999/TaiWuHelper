using GameData.Domains;
using GameData.Domains.Character;
using static TaiWu.Infrastructure.SaveGames.GameDataText;

namespace TaiWu.Infrastructure.SaveGames;

internal static class OverviewReportSection
{
    private const string StandaloneUnavailable =
        "(unavailable:standalone-runtime)";

    public static void Write(TaiwuReportContext context)
    {
        var writer = context.Writer;
        var taiwu = context.Taiwu;

        writer.Write(
            "TAIWU|{0}|{1}{2}",
            context.TaiwuId,
            taiwu.GetSurname(),
            taiwu.GetGivenName());
        writer.Write(
            "WORLD|year={0}|month={1}|date={2}|daysLeft={3}|xiangshuLevel={4}|xiangshuProgress={5}|invasionType={6}|invasionSpeed={7}|mainStory={8}",
            DomainManager.World.GetCurrYear(),
            DomainManager.World.GetCurrMonthInYear(),
            DomainManager.World.GetCurrDate(),
            DomainManager.World.GetLeftDaysInCurrMonth(),
            DomainManager.World.GetXiangshuLevel(),
            DomainManager.World.GetXiangshuProgress(),
            DomainManager.World.GetBossInvasionSpeedType(),
            DomainManager.World.GetBossInvasionSpeed(),
            DomainManager.World.GetMainStoryLineProgress());

        var location = taiwu.GetLocation();
        writer.Write(
            "CHARACTER|age={0}|physicalAge={1}|health={2}|leftMaxHealth={3}|maxHealth={4}|infection={5}|consummate={6}|maxConsummate={7}|exp={8}|combatPower={9}|neili={10}|maxNeili={11}|area={12}|block={13}|inventoryLoad={14}|maxInventoryLoad={15}|equipmentLoad={16}|maxEquipmentLoad={17}",
            SafeText(() => taiwu.GetCurrAge()),
            StandaloneUnavailable,
            SafeText(() => taiwu.GetHealth()),
            StandaloneUnavailable,
            StandaloneUnavailable,
            SafeText(() => taiwu.GetXiangshuInfection()),
            SafeText(() => taiwu.GetConsummateLevel()),
            SafeText(() => taiwu.GetMaxConsummateLevel()),
            SafeText(() => taiwu.GetExp()),
            StandaloneUnavailable,
            SafeText(() => taiwu.GetCurrNeili()),
            SafeText(() => taiwu.GetMaxNeili()),
            location.AreaId,
            location.BlockId,
            SafeText(() => taiwu.GetCurrInventoryLoad()),
            SafeText(() => taiwu.GetMaxInventoryLoad()),
            SafeText(() => taiwu.GetCurrEquipmentLoad()),
            StandaloneUnavailable);

        WriteFeatures(context);
        WriteResources(context);
        WriteMindMedicines(context);
        WriteVillage(context);
    }

    private static void WriteFeatures(TaiwuReportContext context)
    {
        foreach (var featureId in context.Taiwu.GetFeatureIds())
        {
            var feature = Config.CharacterFeature.Instance.GetItem(featureId);
            context.Writer.Write(
                "TAIWUFEATURE|id={0}|name={1}|level={2}|slotBonuses={3}",
                featureId,
                feature?.Name ?? "(unknown)",
                feature?.Level ?? -1,
                feature is null
                    ? string.Empty
                    : JoinNumbers(feature.CombatSkillSlotBonuses));
        }
    }

    private static void WriteResources(TaiwuReportContext context)
    {
        ResourceInts resources = context.Taiwu.GetResources();
        var values = Enumerable.Range(0, 8)
            .Select(resourceType => $"{resourceType}={resources.Get(resourceType)}");
        context.Writer.Write($"RESOURCES|{string.Join('|', values)}");
    }

    private static void WriteMindMedicines(TaiwuReportContext context)
    {
        foreach (Config.MedicineItem medicine in Config.Medicine.Instance)
        {
            if (medicine.DamageStepBonus <= 0)
            {
                continue;
            }

            var medicineCount = DomainManager.Taiwu.GetItemCount(
                medicine.ItemType,
                medicine.TemplateId);
            if (medicineCount <= 0)
            {
                continue;
            }

            context.Writer.Write(
                "MINDMEDICINE|template={0}|name={1}|grade={2}|breakEffect={3}|bonus={4}|count={5}",
                medicine.TemplateId,
                medicine.Name,
                medicine.Grade,
                medicine.BreakBonusEffect,
                medicine.DamageStepBonus,
                medicineCount);
        }
    }

    private static void WriteVillage(TaiwuReportContext context)
    {
        context.Writer.Write("VILLAGE|unavailable=standalone-runtime");
    }
}
