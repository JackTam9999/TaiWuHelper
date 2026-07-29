using System.Reflection;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using GameData.Domains.LegendaryBook;
using GameData.Utilities;
using static TaiWu.Infrastructure.SaveGames.GameDataText;

namespace TaiWu.Infrastructure.SaveGames;

internal static class LegendaryBookReportSection
{
    public static void Write(TaiwuReportContext context)
    {
        var presetIndex =
            DomainManager.LegendaryBook.GetCurrentUsingPresetIndex();
        var unlocked =
            DomainManager.LegendaryBook.GetCurrentUnlockedPresetAmount();
        context.Writer.Write(
            "BOOKPRESET|current={0}|unlocked={1}",
            presetIndex,
            unlocked);

        WriteOwners(context);
        WriteStates(context);
        WriteDirectOwners(context);
        WriteSkillPreset(context, presetIndex);
        WriteExtraSkillSlots(context);
        WriteWeaponPreset(context, presetIndex);
    }

    private static void WriteOwners(TaiwuReportContext context)
    {
        var ownerData =
            DomainManager.LegendaryBook.GetLegendaryBookOwnerData();
        foreach (var (bookType, ownerId) in ownerData.BookMap)
        {
            if (DomainManager.Character.TryGetElement_Objects(
                    ownerId,
                    out Character owner))
            {
                var location = owner.GetLocation();
                context.Writer.Write(
                    "BOOKOWNER|type={0}|owner={1}|name={2}{3}|state={4}|consummate={5}|age={6}|health={7}|area={8}|block={9}",
                    bookType,
                    ownerId,
                    owner.GetSurname(),
                    owner.GetGivenName(),
                    DomainManager.LegendaryBook
                        .GetCharacterLegendaryBookOwnerState(ownerId),
                    owner.GetConsummateLevel(),
                    owner.GetCurrAge(),
                    owner.GetHealth(),
                    location.AreaId,
                    location.BlockId);
            }
            else
            {
                context.Writer.Write(
                    "BOOKOWNER|type={0}|owner={1}|missing=true|state={2}",
                    bookType,
                    ownerId,
                    DomainManager.LegendaryBook
                        .GetCharacterLegendaryBookOwnerState(ownerId));
            }
        }
    }

    private static void WriteStates(TaiwuReportContext context)
    {
        var bookType = 0;
        foreach (var state in
                 DomainManager.LegendaryBook.GmCmd_GetAllLegendaryBookStates())
        {
            context.Writer.Write(
                "BOOKSTATE|type={0}|first={1}|second={2}",
                bookType++,
                state.First,
                state.Second);
        }
    }

    private static void WriteDirectOwners(TaiwuReportContext context)
    {
        for (sbyte bookType = 0; bookType < 14; bookType++)
        {
            var ownerId = DomainManager.LegendaryBook.GetOwner(bookType);
            if (ownerId < 0)
            {
                context.Writer.Write(
                    "BOOKDIRECT|type={0}|owner={1}",
                    bookType,
                    ownerId);
                continue;
            }

            if (DomainManager.Character.TryGetElement_Objects(
                    ownerId,
                    out Character owner))
            {
                var location = owner.GetLocation();
                var hasShockedMonths =
                    DomainManager.LegendaryBook
                        .TryGetElement_LegendaryBookShockedMonths(
                            ownerId,
                            out var shockedMonths);
                context.Writer.Write(
                    "BOOKDIRECT|type={0}|owner={1}|name={2}{3}|state={4}|consummate={5}|shockedMonths={6}|age={7}|health={8}|area={9}|block={10}",
                    bookType,
                    ownerId,
                    owner.GetSurname(),
                    owner.GetGivenName(),
                    DomainManager.LegendaryBook
                        .GetCharacterLegendaryBookOwnerState(ownerId),
                    owner.GetConsummateLevel(),
                    hasShockedMonths ? shockedMonths : -1,
                    owner.GetCurrAge(),
                    owner.GetHealth(),
                    location.AreaId,
                    location.BlockId);
            }
            else
            {
                context.Writer.Write(
                    "BOOKDIRECT|type={0}|owner={1}|missing=true|state={2}",
                    bookType,
                    ownerId,
                    DomainManager.LegendaryBook
                        .GetCharacterLegendaryBookOwnerState(ownerId));
            }
        }
    }

    private static void WriteSkillPreset(
        TaiwuReportContext context,
        sbyte presetIndex)
    {
        ShortList skillPreset =
            DomainManager.LegendaryBook
                .GetElement_LegendaryBookSkillPresetSlot(presetIndex);
        if (skillPreset.Items is null)
        {
            return;
        }

        for (var index = 0; index < skillPreset.Items.Count; index++)
        {
            var skillId = skillPreset.Items[index];
            context.Writer.Write(
                "BOOKSKILL|{0}|{1}|{2}",
                index,
                skillId,
                SkillName(skillId));
        }
    }

    private static void WriteExtraSkillSlots(TaiwuReportContext context)
    {
        try
        {
            for (sbyte skillType = 0; skillType < 5; skillType++)
            {
                if (DomainManager.Extra.TryGetElement_LegendaryBookSkillSlot(
                        skillType,
                        out ShortList slots))
                {
                    context.Writer.Write(
                        "EXTRALEGENDARYBOOKSLOTS|skillType={0}|slots={1}",
                        skillType,
                        JoinNumbers(slots.Items));
                }
                else
                {
                    context.Writer.Write(
                        "EXTRALEGENDARYBOOKSLOTS|skillType={0}|slots=",
                        skillType);
                }
            }
        }
        catch (Exception exception)
        {
            context.Writer.Write(
                "EXTRALEGENDARYBOOKSLOTS|error={0}:{1}",
                exception.GetType().Name,
                exception.Message);
        }
    }

    private static void WriteWeaponPreset(
        TaiwuReportContext context,
        sbyte presetIndex)
    {
        var getWeaponPreset = typeof(LegendaryBookDomain).GetMethod(
            "GetElement_LegendaryBookWeaponPresetSlot",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (getWeaponPreset is null)
        {
            return;
        }

        var weaponPreset =
            getWeaponPreset.Invoke(
                DomainManager.LegendaryBook,
                [presetIndex]) as LegendaryBookWeaponPreset;
        if (weaponPreset?.WeaponPresets is null)
        {
            return;
        }

        for (var index = 0;
             index < weaponPreset.WeaponPresets.Length;
             index++)
        {
            ItemKey item = weaponPreset.WeaponPresets[index];
            context.Writer.Write(
                "BOOKWEAPON|{0}|type={1}|template={2}|id={3}|{4}",
                index,
                item.ItemType,
                item.TemplateId,
                item.Id,
                WeaponName(item));
        }
    }
}
