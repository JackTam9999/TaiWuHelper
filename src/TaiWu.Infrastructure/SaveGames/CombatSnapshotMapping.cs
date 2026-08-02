using GameData.Domains.CombatSkill;
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

    public static SnapshotValue<PracticeDirection> MapActivePracticeDirection(
        int activationState,
        int skillId)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (!IsSupportedPageState(activationState))
        {
            return SnapshotValue<PracticeDirection>.Unavailable(
                $"Skill {skillId} has unsupported activation state "
                + $"{activationState}.");
        }

        var source = (ushort)activationState;
        if (!CombatSkillStateHelper.IsBrokenOut(source))
        {
            return SnapshotValue<PracticeDirection>.Unavailable(
                $"Skill {skillId} has not completed breakthrough, so its "
                + "practice direction is not active.");
        }

        var mapped = MapPracticeDirection(
            CombatSkillStateHelper.GetCombatSkillDirection(source));
        return mapped is { IsAvailable: true, Value: PracticeDirection.Neutral }
            ? SnapshotValue<PracticeDirection>.Unavailable(
                $"Skill {skillId} is broken through but has no Direct or "
                + "Reverse practice direction.")
            : mapped;
    }

    public static SnapshotValue<BreakthroughDirectionAvailability>
        MapBreakthroughDirectionAvailability(
            int readingState,
            int activationState,
            bool meetsReadingRequirement,
            int skillId)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (!IsSupportedPageState(readingState))
        {
            return SnapshotValue<BreakthroughDirectionAvailability>
                .Unavailable(
                    $"Skill {skillId} has unsupported reading state "
                    + $"{readingState}.");
        }

        if (!IsSupportedPageState(activationState))
        {
            return SnapshotValue<BreakthroughDirectionAvailability>
                .Unavailable(
                    $"Skill {skillId} has unsupported activation state "
                    + $"{activationState}.");
        }

        var isBrokenOut = CombatSkillStateHelper.IsBrokenOut(
            (ushort)activationState);
        if (isBrokenOut)
        {
            return AvailableBreakthrough(
                isBrokenOut: true,
                canBreakthroughNow: false,
                availableDirections: []);
        }

        if (!meetsReadingRequirement)
        {
            return AvailableBreakthrough(
                isBrokenOut: false,
                canBreakthroughNow: false,
                availableDirections: []);
        }

        if (!CombatSkillStateHelper.IsReadNormalPagesMeetConditionOfBreakout(
                (ushort)readingState))
        {
            return SnapshotValue<BreakthroughDirectionAvailability>
                .Unavailable(
                    $"Skill {skillId} was reported as satisfying the reading "
                    + "prerequisite, but its reading state does not contain "
                    + "the required five normal pages.");
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

    public static SnapshotValue<IReadOnlyList<CombatSkillStudyDetail>>
        MapStudyDetails(
            int readingState,
            int activationState,
            int skillId)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (!IsSupportedPageState(readingState))
        {
            return SnapshotValue<IReadOnlyList<CombatSkillStudyDetail>>
                .Unavailable(
                    $"Skill {skillId} has unsupported reading state "
                    + $"{readingState}.");
        }

        if (!IsSupportedPageState(activationState))
        {
            return SnapshotValue<IReadOnlyList<CombatSkillStudyDetail>>
                .Unavailable(
                    $"Skill {skillId} has unsupported activation state "
                    + $"{activationState}.");
        }

        List<CombatSkillStudyDetail> details = [];
        for (var internalIndex = 0;
             internalIndex < CombatSkillStateHelper.TotalPagesCount;
             internalIndex++)
        {
            var (group, groupIndex, key) = DetailIdentity(internalIndex);
            var mask = 1 << internalIndex;
            details.Add(
                new CombatSkillStudyDetail(
                    $"{group.ToString().ToLowerInvariant()}-{groupIndex}",
                    group,
                    groupIndex,
                    internalIndex,
                    WheelOrderByInternalIndex[internalIndex],
                    mask,
                    key,
                    (readingState & mask) != 0,
                    (activationState & mask) != 0));
        }

        return SnapshotValue<IReadOnlyList<CombatSkillStudyDetail>>.Available(
            details);
    }

    private static readonly int[] WheelOrderByInternalIndex =
    [
        13, 14, 0, 1, 2,
        3, 4, 5, 6, 7,
        12, 11, 10, 9, 8
    ];

    private static (
        CombatSkillStudyDetailGroup Group,
        int GroupIndex,
        string LocalizationKey) DetailIdentity(int internalIndex)
    {
        return internalIndex switch
        {
            < 5 => (
                CombatSkillStudyDetailGroup.Outline,
                internalIndex,
                $"LK_CombatSkill_First_Page_Type_{internalIndex}"),
            < 10 => (
                CombatSkillStudyDetailGroup.Direct,
                internalIndex - 5,
                $"LK_CombatSkill_Direct_Page_{internalIndex - 5}"),
            _ => (
                CombatSkillStudyDetailGroup.Reverse,
                internalIndex - 10,
                $"LK_CombatSkill_Reverse_Page_{internalIndex - 10}")
        };
    }

    private static bool IsSupportedPageState(int state)
    {
        return state >= 0
               && (state & ~CombatSkillStateHelper.CompleteReadingState) == 0;
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

internal enum CombatSkillStudyDetailGroup
{
    Outline = 0,
    Direct = 1,
    Reverse = 2
}

internal sealed record CombatSkillStudyDetail(
    string StableId,
    CombatSkillStudyDetailGroup Group,
    int GroupIndex,
    int InternalIndex,
    int WheelOrder,
    int BitMask,
    string LocalizationKey,
    bool IsRead,
    bool IsActive);
