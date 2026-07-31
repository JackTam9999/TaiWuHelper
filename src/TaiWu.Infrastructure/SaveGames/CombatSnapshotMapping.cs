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

    public static SnapshotValue<CombatSkillElement> MapCombatSkillElement(
        int element)
    {
        return element is >= 0 and <= 5
            ? SnapshotValue<CombatSkillElement>.Available(
                (CombatSkillElement)element)
            : SnapshotValue<CombatSkillElement>.Unavailable(
                $"Unsupported GameData combat-skill element: {element}.");
    }

    public static ElementAdjustmentSet MapElementAdjustments(
        IReadOnlyList<sbyte> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count < 5)
        {
            throw new InvalidDataException(
                "An element-adjustment source must contain five values.");
        }

        return new ElementAdjustmentSet(
            values[0],
            values[1],
            values[2],
            values[3],
            values[4]);
    }

    public static SnapshotValue<PracticeDirection> MapPracticeDirection(
        int direction)
    {
        // GameData uses None=-1, Direct=0, and Reverse=1. These values do not
        // match the Domain enum and must be translated by meaning.
        return direction switch
        {
            -1 => SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral),
            0 => SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            1 => SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            _ => SnapshotValue<PracticeDirection>.Unavailable(
                $"Unsupported GameData practice direction: {direction}.")
        };
    }

    public static SnapshotValue<PracticeDirection> MapPracticeDirection(
        int direction,
        bool isBrokenOut,
        int skillId)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        return isBrokenOut
            ? MapPracticeDirection(direction)
            : SnapshotValue<PracticeDirection>.Unavailable(
                $"Skill {skillId} has not completed breakthrough, so its "
                + "practice direction is not active.");
    }

    public static SnapshotValue<BreakthroughDirectionAvailability>
        MapBreakthroughDirectionAvailability(
            int readingState,
            bool isBrokenOut,
            bool canBreakthroughNow,
            int skillId)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (readingState < 0)
        {
            return SnapshotValue<BreakthroughDirectionAvailability>
                .Unavailable(
                    $"Skill {skillId} has invalid reading state "
                    + $"{readingState}.");
        }

        if (isBrokenOut)
        {
            if (canBreakthroughNow)
            {
                return SnapshotValue<BreakthroughDirectionAvailability>
                    .Unavailable(
                        $"Skill {skillId} is already broken through but was "
                        + "reported as immediately breakable.");
            }

            return AvailableBreakthrough(
                isBrokenOut: true,
                canBreakthroughNow: false,
                availableDirections: []);
        }

        if (!canBreakthroughNow)
        {
            return AvailableBreakthrough(
                isBrokenOut: false,
                canBreakthroughNow: false,
                availableDirections: []);
        }

        var directPageCount = CountReadNormalPages(
            readingState,
            startingBit: 5);
        var reversePageCount = CountReadNormalPages(
            readingState,
            startingBit: 10);
        List<PracticeDirection> directions = [];
        if (directPageCount >= 3)
        {
            directions.Add(PracticeDirection.Direct);
        }

        if (reversePageCount >= 3)
        {
            directions.Add(PracticeDirection.Reverse);
        }

        return directions.Count == 0
            ? SnapshotValue<BreakthroughDirectionAvailability>.Unavailable(
                $"Skill {skillId} can break through, but its readable page "
                + "directions do not produce a Direct or Reverse result.")
            : AvailableBreakthrough(
                isBrokenOut: false,
                canBreakthroughNow: true,
                directions);
    }

    private static SnapshotValue<BreakthroughDirectionAvailability>
        AvailableBreakthrough(
            bool isBrokenOut,
            bool canBreakthroughNow,
            IEnumerable<PracticeDirection> availableDirections)
    {
        return SnapshotValue<BreakthroughDirectionAvailability>.Available(
            new BreakthroughDirectionAvailability(
                isBrokenOut,
                canBreakthroughNow,
                availableDirections));
    }

    private static int CountReadNormalPages(
        int readingState,
        int startingBit)
    {
        var count = 0;
        for (var pageIndex = 0; pageIndex < 5; pageIndex++)
        {
            if ((readingState & (1 << (startingBit + pageIndex))) != 0)
            {
                count++;
            }
        }

        return count;
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
