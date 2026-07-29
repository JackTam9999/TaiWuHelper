using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Infrastructure.SaveGames;

internal static class CombatSnapshotMapping
{
    private const sbyte WeaponItemType = 0;
    private const sbyte ArmorItemType = 1;
    private const sbyte AccessoryItemType = 2;

    public static bool TryMapSkillCategory(
        int equipType,
        out SkillCategory category)
    {
        category = equipType switch
        {
            0 => SkillCategory.Neigong,
            1 => SkillCategory.Attack,
            2 => SkillCategory.Agility,
            3 => SkillCategory.Defense,
            4 => SkillCategory.Assistance,
            _ => default
        };

        return equipType is >= 0 and <= 4;
    }

    public static SnapshotValue<PracticeDirection> MapPracticeDirection(
        int direction)
    {
        return direction switch
        {
            -1 => SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            0 => SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral),
            1 => SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            _ => SnapshotValue<PracticeDirection>.Unavailable(
                $"Unsupported GameData practice direction: {direction}.")
        };
    }

    public static SkillSlotContribution MapSlotContribution(
        sbyte[] specificGrids,
        int genericGrid)
    {
        ArgumentNullException.ThrowIfNull(specificGrids);
        if (specificGrids.Length != 4)
        {
            throw new InvalidDataException(
                "Combat-skill configuration must contain exactly four "
                + "category-specific grid values.");
        }

        return new SkillSlotContribution(
            attack: specificGrids[0],
            agility: specificGrids[1],
            defense: specificGrids[2],
            assistance: specificGrids[3],
            generic: genericGrid);
    }

    public static EquipmentKind MapEquipmentKind(sbyte itemType)
    {
        return itemType switch
        {
            WeaponItemType => EquipmentKind.Weapon,
            ArmorItemType => EquipmentKind.Armor,
            AccessoryItemType => EquipmentKind.Accessory,
            _ => EquipmentKind.Other
        };
    }
}
