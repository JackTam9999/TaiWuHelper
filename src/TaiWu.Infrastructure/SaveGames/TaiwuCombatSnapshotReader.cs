using GameData.ArchiveData;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using System.Reflection;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuCombatSnapshotReader : ICombatSnapshotReader
{
    public Task<CombatSnapshot> ReadAsync(
        CombatSnapshotReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TaiwuArchiveReadSession.ReadAsync(
            request.SaveFilePath,
            context =>
            {
                var snapshot = ProjectSnapshot(
                    context,
                    request.TargetCharacterId,
                    cancellationToken);
                return request.CurrentLoadoutObservation is null
                    ? snapshot
                    : CombatSnapshotObservationMerger.Merge(
                        snapshot,
                        request.CurrentLoadoutObservation);
            },
            cancellationToken);
    }

    private static CombatSnapshot ProjectSnapshot(
        TaiwuArchiveReadContext readContext,
        int targetCharacterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<SnapshotWarning> warnings = [];
        if (readContext.LoadWarning is not null)
        {
            warnings.Add(
                new SnapshotWarning(
                    "STANDALONE_EVENT_RUNTIME_UNAVAILABLE",
                    "The archive reached the expected standalone event-runtime "
                    + $"boundary: {readContext.LoadWarning}"));
        }

        warnings.Add(
            new SnapshotWarning(
                "RUNTIME_GRID_COST_MODIFIERS_NOT_EVALUATED",
                "Configured GridCost and confirmed mastery were mapped, but "
                + "effective used capacity remains unavailable because the "
                + "standalone-unsafe SpecialEffect calculation was not invoked."));

        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        var taiwu = DomainManager.Taiwu.GetTaiwu()
            ?? throw new InvalidDataException(
                "The archive stopped loading before the Taiwu character "
                + "was available.");

        if (!DomainManager.Character.TryGetElement_Objects(
                targetCharacterId,
                out Character target))
        {
            throw new KeyNotFoundException(
                $"Target character {targetCharacterId} was not found "
                + "in the save.");
        }

        var player = MapPlayer(taiwuId, taiwu, warnings);
        cancellationToken.ThrowIfCancellationRequested();
        var targetSnapshot = MapTarget(
            targetCharacterId,
            target,
            warnings);

        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                readContext.SaveFilePath,
                readContext.SourceFingerprint.Sha256,
                DateTimeOffset.UtcNow,
                SnapshotValue<DateTimeOffset>.Available(
                    readContext.SourceFingerprint.LastWriteTimeUtc),
                GetGameDataVersion()),
            player,
            targetSnapshot,
            warnings);
    }

    private static PlayerCombatSnapshot MapPlayer(
        int characterId,
        Character character,
        List<SnapshotWarning> warnings)
    {
        var equipment = character.GetCombatSkillEquipment();
        var loadout = MapLoadout(equipment);
        var learnedSkills = MapPlayerSkills(characterId, warnings);
        var learnedById = learnedSkills.ToDictionary(skill => skill.SkillId);

        return new PlayerCombatSnapshot(
            characterId,
            MapDisplayName(character),
            learnedSkills,
            loadout,
            MapEquipment(character.GetEquipment()),
            MapSlotBudgets(
                equipment,
                loadout),
            MapGenericSlotAllocation(
                character,
                loadout,
                learnedById,
                warnings),
            MapLegendaryBookModifiers(warnings));
    }

    private static TargetCombatSnapshot MapTarget(
        int characterId,
        Character character,
        List<SnapshotWarning> warnings)
    {
        var equipment = character.GetCombatSkillEquipment();
        HashSet<short> equippedSkillIds = [];
        equipment.GetValidSkills(equippedSkillIds);

        SnapshotValue<CombatLoadoutSnapshot> equippedSkills;
        if (equippedSkillIds.Count == 0)
        {
            equippedSkills =
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "The current save contains no equipped target skills.");
            warnings.Add(
                new SnapshotWarning(
                    "TARGET_LOADOUT_UNAVAILABLE",
                    $"Target {characterId} has no equipped skills in the "
                    + "current disk save. Current-screen evidence may be newer."));
        }
        else
        {
            equippedSkills =
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    MapLoadout(equipment));
        }

        return new TargetCombatSnapshot(
            characterId,
            MapDisplayName(character),
            SnapshotValue<int>.Available(character.GetCurrAge()),
            MapFeatures(character),
            MapTargetSkills(
                characterId,
                equippedSkillIds,
                warnings),
            equippedSkills,
            MapEquipment(character.GetEquipment()));
    }

    private static List<CombatSkillSnapshot> MapPlayerSkills(
        int characterId,
        List<SnapshotWarning> warnings)
    {
        var sourceSkills =
            DomainManager.CombatSkill.GetCharCombatSkills(characterId);
        List<CombatSkillSnapshot> result = [];

        foreach (var (skillId, skill) in sourceSkills.OrderBy(pair => pair.Key))
        {
            var mapped = MapSkill(
                characterId,
                skillId,
                skill,
                warnings);
            if (mapped is not null)
            {
                result.Add(mapped);
            }
        }

        return result;
    }

    private static List<CombatSkillSnapshot> MapTargetSkills(
        int characterId,
        HashSet<short> equippedSkillIds,
        List<SnapshotWarning> warnings)
    {
        var sourceSkills =
            DomainManager.CombatSkill.GetCharCombatSkills(characterId);
        List<CombatSkillSnapshot> result = [];

        foreach (var (skillId, skill) in sourceSkills.OrderBy(pair => pair.Key))
        {
            var item = skill.Template;
            var isRelevant =
                equippedSkillIds.Contains(skillId)
                || item.EquipType == 1
                || item.Type == 13;
            if (!isRelevant)
            {
                continue;
            }

            var mapped = MapSkill(
                characterId,
                skillId,
                skill,
                warnings);
            if (mapped is not null)
            {
                result.Add(mapped);
            }
        }

        return result;
    }

    private static CombatSkillSnapshot? MapSkill(
        int characterId,
        short skillId,
        CombatSkill skill,
        List<SnapshotWarning> warnings)
    {
        var item = skill.Template;
        if (!CombatSnapshotMapping.TryMapSkillCategory(
                item.EquipType,
                out var category))
        {
            warnings.Add(
                new SnapshotWarning(
                    "SKILL_CATEGORY_UNSUPPORTED",
                    $"Skill {skillId} has unsupported equip type "
                    + $"{item.EquipType} and was omitted."));
            return null;
        }

        SkillSlotContribution slotContribution;
        try
        {
            slotContribution = CombatSnapshotMapping.MapSlotContribution(
                item.SpecificGrids,
                item.GenericGrid);
        }
        catch (Exception exception)
            when (exception is InvalidDataException
                  or ArgumentOutOfRangeException)
        {
            slotContribution = SkillSlotContribution.None;
            warnings.Add(
                new SnapshotWarning(
                    "SKILL_GRID_BONUS_UNAVAILABLE",
                    $"Skill {skillId} grid bonuses were invalid: "
                    + exception.Message));
        }

        var direction = CombatSkillStateHelper.GetCombatSkillDirection(
            skill.GetActivationState());
        var mastered =
            DomainManager.Extra.IsCombatSkillMasteredByCharacter(
                characterId,
                skillId);

        return new CombatSkillSnapshot(
            skillId,
            MapText(item.Name, $"Skill {skillId} has no configured name."),
            category,
            item.GridCost > 0
                ? SnapshotValue<int>.Available(item.GridCost)
                : SnapshotValue<int>.Unavailable(
                    $"Skill {skillId} has no positive configured GridCost."),
            SnapshotValue<bool>.Available(mastered),
            CombatSnapshotMapping.MapPracticeDirection(direction),
            slotContribution,
            MapEffectId(item.DirectEffectID, "direct", skillId),
            MapEffectId(item.ReverseEffectID, "reverse", skillId));
    }

    private static CombatLoadoutSnapshot MapLoadout(
        CombatSkillEquipment equipment)
    {
        return new CombatLoadoutSnapshot(
            GetEquippedSkillIds(equipment, 0),
            GetEquippedSkillIds(equipment, 1),
            GetEquippedSkillIds(equipment, 2),
            GetEquippedSkillIds(equipment, 3),
            GetEquippedSkillIds(equipment, 4));
    }

    private static int[] GetEquippedSkillIds(
        CombatSkillEquipment equipment,
        sbyte equipType)
    {
        List<short> skillIds = [];
        equipment.GetValidSkills(equipType, skillIds);
        return [.. skillIds.Select(skillId => (int)skillId)];
    }

    private static SlotBudgetSet MapSlotBudgets(
        CombatSkillEquipment equipment,
        CombatLoadoutSnapshot loadout)
    {
        return new SlotBudgetSet(
        [
            MapSlotBudget(
                SkillCategory.Neigong,
                equipment.Neigong.Capacity,
                loadout),
            MapSlotBudget(
                SkillCategory.Attack,
                equipment.Attack.Capacity,
                loadout),
            MapSlotBudget(
                SkillCategory.Agility,
                equipment.Agility.Capacity,
                loadout),
            MapSlotBudget(
                SkillCategory.Defense,
                equipment.Defense.Capacity,
                loadout),
            MapSlotBudget(
                SkillCategory.Assistance,
                equipment.Assistance.Capacity,
                loadout)
        ]);
    }

    private static SlotBudget MapSlotBudget(
        SkillCategory category,
        int capacity,
        CombatLoadoutSnapshot loadout)
    {
        if (loadout.Get(category).Length == 0)
        {
            return new SlotBudget(category, used: 0, capacity);
        }

        return new SlotBudget(
            category,
            SnapshotValue<int>.Unavailable(
                "Effective used capacity requires combat-skill cost rules "
                + "that are not evaluated by the read-only adapter."),
            capacity);
    }

    private static GenericSlotAllocation MapGenericSlotAllocation(
        Character character,
        CombatLoadoutSnapshot loadout,
        Dictionary<int, CombatSkillSnapshot> learnedById,
        List<SnapshotWarning> warnings)
    {
        var source = DomainManager.Taiwu.GetGenericGridAllocation();
        if (source.Length < 4)
        {
            throw new InvalidDataException(
                "The save does not contain all four generic-grid "
                + "allocation values.");
        }

        var attack = source[0];
        var agility = source[1];
        var defense = source[2];
        var assistance = source[3];
        var assigned = attack + agility + defense + assistance;

        var configuredTotal = loadout
            .Get(SkillCategory.Neigong)
            .Where(learnedById.ContainsKey)
            .Sum(skillId => learnedById[skillId].SlotContribution.Generic);
        configuredTotal += GetFeatureGenericGridBonus(character);

        if (assigned > configuredTotal)
        {
            warnings.Add(
                new SnapshotWarning(
                    "GENERIC_SLOT_SOURCE_INCOMPLETE",
                    $"The save allocates {assigned} generic slots but the "
                    + $"mapped skill and feature configuration explains "
                    + $"{configuredTotal}. The allocated total was retained."));
        }

        return new GenericSlotAllocation(
            Math.Max(assigned, configuredTotal),
            attack,
            agility,
            defense,
            assistance);
    }

    private static int GetFeatureGenericGridBonus(Character character)
    {
        var total = 0;
        foreach (var featureId in character.GetFeatureIds())
        {
            var feature = Config.CharacterFeature.Instance.GetItem(featureId);
            if (feature?.CombatSkillSlotBonuses is { Length: > 4 } bonuses)
            {
                total += bonuses[4];
            }
        }

        return total;
    }

    private static List<CharacterFeatureSnapshot> MapFeatures(
        Character character)
    {
        List<CharacterFeatureSnapshot> result = [];
        foreach (var featureId in character.GetFeatureIds().Distinct())
        {
            var feature = Config.CharacterFeature.Instance.GetItem(featureId);
            result.Add(
                new CharacterFeatureSnapshot(
                    featureId,
                    feature is null
                        ? SnapshotValue<string>.Unavailable(
                            $"Feature {featureId} is missing from configuration.")
                        : MapText(
                            feature.Name,
                            $"Feature {featureId} has no configured name."),
                    feature is null || feature.Level < 0
                        ? SnapshotValue<int>.Unavailable(
                            $"Feature {featureId} has no configured level.")
                        : SnapshotValue<int>.Available(feature.Level)));
        }

        return result;
    }

    private static List<EquipmentSnapshot> MapEquipment(ItemKey[] equipment)
    {
        List<EquipmentSnapshot> result = [];
        for (var slotIndex = 0;
             slotIndex < equipment.Length;
             slotIndex++)
        {
            var item = equipment[slotIndex];
            if (!item.HasTemplate)
            {
                continue;
            }

            result.Add(
                new EquipmentSnapshot(
                    slotIndex,
                    item.Id >= 0
                        ? SnapshotValue<long>.Available(item.Id)
                        : SnapshotValue<long>.Unavailable(
                            "The equipped item has no instance ID."),
                    item.TemplateId >= 0
                        ? SnapshotValue<int>.Available(item.TemplateId)
                        : SnapshotValue<int>.Unavailable(
                            "The equipped item has no template ID."),
                    MapEquipmentName(item),
                    SnapshotValue<EquipmentKind>.Available(
                        CombatSnapshotMapping.MapEquipmentKind(
                            item.ItemType))));
        }

        return result;
    }

    private static SnapshotValue<string> MapEquipmentName(ItemKey item)
    {
        if (item.ItemType != ItemType.Weapon)
        {
            return SnapshotValue<string>.Unavailable(
                $"Display-name mapping is not available for item type "
                + $"{item.ItemType}.");
        }

        var name = Config.Weapon.Instance.GetItem(item.TemplateId)?.Name;
        return MapText(
            name,
            $"Weapon template {item.TemplateId} has no configured name.");
    }

    private static List<LegendaryBookModifier> MapLegendaryBookModifiers(
        List<SnapshotWarning> warnings)
    {
        var presetIndex =
            DomainManager.LegendaryBook.GetCurrentUsingPresetIndex();
        var preset =
            DomainManager.LegendaryBook
                .GetElement_LegendaryBookSkillPresetSlot(presetIndex);
        if (preset.Items is null
            || preset.Items.All(skillId => skillId < 0))
        {
            return [];
        }

        warnings.Add(
            new SnapshotWarning(
                "LEGENDARY_BOOK_REDUCTION_UNCONFIRMED",
                "The save contains legendary-book skill slots, but their "
                + "effective reductions were not inferred without explicit "
                + "mechanic evidence."));
        return [];
    }

    private static SnapshotValue<string> MapDisplayName(Character character)
    {
        return MapText(
            character.GetSurname() + character.GetGivenName(),
            "The character name was unavailable in the standalone runtime.");
    }

    private static SnapshotValue<string> MapText(
        string? value,
        string unavailableReason)
    {
        return string.IsNullOrWhiteSpace(value)
            ? SnapshotValue<string>.Unavailable(unavailableReason)
            : SnapshotValue<string>.Available(value);
    }

    private static SnapshotValue<int> MapEffectId(
        int effectId,
        string direction,
        short skillId)
    {
        return effectId >= 0
            ? SnapshotValue<int>.Available(effectId)
            : SnapshotValue<int>.Unavailable(
                $"Skill {skillId} has no configured {direction} effect ID.");
    }

    private static SnapshotValue<string> GetGameDataVersion()
    {
        var assembly = typeof(LocalArchiveFile).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        var value = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString()
            : informationalVersion;

        return MapText(
            value,
            "The loaded GameData assembly has no version metadata.");
    }
}
