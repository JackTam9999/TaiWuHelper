using GameData.ArchiveData;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using GameData.Utilities;
using System.Reflection;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuCombatSnapshotReader(
    TaiwuArchiveReadSession readSession,
    TaiwuGameTextResolver textResolver) : ICombatSnapshotReader
{
    public Task<CombatSnapshot> ReadAsync(
        CombatSnapshotReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return readSession.ReadAsync(
            request.SaveFilePath,
            (context, token) =>
            {
                var snapshot = ProjectSnapshot(
                    context,
                    request.TargetCharacterId,
                    textResolver.CreateContext(
                        request.SaveFilePath,
                        request.Language),
                    token);
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
        TaiwuGameTextContext text,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<SnapshotWarning> warnings = [];
        if (readContext.LoadWarning is not null)
        {
            warnings.Add(
                new SnapshotWarning(
                    readContext.LoadWarning.Code,
                    "The archive reached the expected standalone event-runtime "
                    + $"boundary: {readContext.LoadWarning.Detail}"));
        }

        warnings.Add(
            new SnapshotWarning(
                "RUNTIME_GRID_COST_MODIFIERS_NOT_EVALUATED",
                "Configured GridCost and confirmed mastery were mapped, but "
                + "effective used capacity remains unavailable because the "
                + "standalone-unsafe SpecialEffect calculation was not invoked."));
        warnings.Add(
            new SnapshotWarning(
                "RUNTIME_SLOT_CAPACITY_MODIFIERS_NOT_EVALUATED",
                "Configured slot capacities were mapped, but runtime capacity "
                + "modifiers were not invoked. Supply current-screen displayed "
                + "slot budgets when exact capacities differ."));

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

        var player = MapPlayer(taiwuId, taiwu, text, warnings);
        cancellationToken.ThrowIfCancellationRequested();
        var targetSnapshot = MapTarget(
            targetCharacterId,
            target,
            text,
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
        TaiwuGameTextContext text,
        List<SnapshotWarning> warnings)
    {
        var equipment = character.GetCombatSkillEquipment();
        var loadout = MapLoadout(equipment);
        var learnedSkills = MapPlayerSkills(characterId, text, warnings);
        var learnedById = learnedSkills.ToDictionary(skill => skill.SkillId);
        var legendaryBookCosts = MapLegendaryBookCosts(
            learnedById,
            warnings);
        var genericSlotAllocation = MapGenericSlotAllocation(
            character,
            loadout,
            learnedById,
            warnings);

        return new PlayerCombatSnapshot(
            characterId,
            MapDisplayName(character, text),
            learnedSkills,
            loadout,
            MapEquipment(character.GetEquipment(), text),
            MapSlotBudgets(
                loadout,
                learnedById,
                genericSlotAllocation),
            genericSlotAllocation,
            legendaryBookCosts.Slots,
            legendaryBookCosts.Assignments,
            MapInnerPowerState(character, text, warnings));
    }

    private static TargetCombatSnapshot MapTarget(
        int characterId,
        Character character,
        TaiwuGameTextContext text,
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
                    "The target's active loadout is not present in this disk "
                    + "save; GameData may select NPC combat skills during "
                    + "combat preparation.");
            warnings.Add(
                new SnapshotWarning(
                    CombatSnapshotWarningCodes.TargetLoadoutNotPersisted,
                    $"Target {characterId}'s active loadout is not present in "
                    + "the current disk save. GameData may select NPC combat "
                    + "skills during combat preparation; recommendations use "
                    + "known skills and verified mechanics instead."));
        }
        else
        {
            equippedSkills =
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    MapLoadout(equipment));
        }

        return new TargetCombatSnapshot(
            characterId,
            MapDisplayName(character, text),
            SnapshotValue<int>.Available(character.GetCurrAge()),
            MapFeatures(character, text),
            MapTargetSkills(
                characterId,
                equippedSkillIds,
                text,
                warnings),
            equippedSkills,
            MapEquipment(character.GetEquipment(), text),
            baseChannelResistance: MapBaseChannelResistance(
                character,
                warnings));
    }

    private static List<CombatSkillSnapshot> MapPlayerSkills(
        int characterId,
        TaiwuGameTextContext text,
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
                text,
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
        TaiwuGameTextContext text,
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
                || item.EquipType == 4
                || item.Type == 13;
            if (!isRelevant)
            {
                continue;
            }

            var mapped = MapSkill(
                characterId,
                skillId,
                skill,
                text,
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
        TaiwuGameTextContext text,
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

        var readingState = skill.GetReadingState();
        var activationState = skill.GetActivationState();
        var direction = CombatSnapshotMapping.MapActivePracticeDirection(
            activationState,
            skillId);
        var breakthroughDirections = CombatSnapshotMapping
            .MapBreakthroughDirectionAvailability(
                readingState,
                activationState,
                skill.CanBreakout(),
                skillId);
        var mastered =
            DomainManager.Extra.IsCombatSkillMasteredByCharacter(
                characterId,
                skillId);

        return new CombatSkillSnapshot(
            skillId,
            MapText(
                text.Resolve("CombatSkill", item.Name),
                $"Skill {skillId} has no configured name."),
            category,
            item.GridCost > 0
                ? SnapshotValue<int>.Available(item.GridCost)
                : SnapshotValue<int>.Unavailable(
                    $"Skill {skillId} has no positive configured GridCost."),
            SnapshotValue<bool>.Available(mastered),
            direction,
            slotContribution,
            MapEffectId(item.DirectEffectID, "direct", skillId),
            MapEffectId(item.ReverseEffectID, "reverse", skillId),
            breakthroughDirections,
            CombatSnapshotMapping.MapCombatSkillElement(
                item.FiveElements),
            SnapshotValue<bool>.Available(
                item.OuterDamageSteps is { Length: > 0 }
                && item.OuterDamageSteps.Any(value => value > 0)),
            SnapshotValue<bool>.Available(item.Poisons.IsNonZero()));
    }

    private static SnapshotValue<TargetChannelResistanceSnapshot>
        MapBaseChannelResistance(
            Character character,
            List<SnapshotWarning> warnings)
    {
        try
        {
            var value = character.GetBasePenetrationResists();
            if (value.Outer > 0 && value.Inner > 0)
            {
                return SnapshotValue<TargetChannelResistanceSnapshot>.Available(
                    new TargetChannelResistanceSnapshot(
                        value.Outer,
                        value.Inner));
            }

            const string reason = "Base outer and inner resistance are not "
                + "both positive; zero cannot establish an Epic 5 profile "
                + "value.";
            warnings.Add(new SnapshotWarning(
                "TARGET_BASE_CHANNEL_RESISTANCE_UNAVAILABLE",
                reason));
            return SnapshotValue<TargetChannelResistanceSnapshot>.Unavailable(
                reason);
        }
        catch (Exception exception)
            when (exception is InvalidOperationException
                  or NullReferenceException
                  or IndexOutOfRangeException)
        {
            var reason = "Base channel resistance is unavailable at the "
                + $"standalone runtime boundary: {exception.GetType().Name}.";
            warnings.Add(new SnapshotWarning(
                "TARGET_BASE_CHANNEL_RESISTANCE_UNAVAILABLE",
                reason));
            return SnapshotValue<TargetChannelResistanceSnapshot>.Unavailable(
                reason);
        }
    }

    private static SnapshotValue<InnerPowerStateSnapshot>
        MapInnerPowerState(
            Character character,
            TaiwuGameTextContext text,
            List<SnapshotWarning> warnings)
    {
        var stateId = NeiliProportionHelper.GetNeiliType(
            character.GetBaseNeiliProportionOfFiveElements(),
            character.GetBirthMonth());
        var item = Config.NeiliType.Instance.GetItem(stateId);
        if (item is null)
        {
            var reason = $"Inner-power state {stateId} is missing from "
                + "configuration.";
            warnings.Add(
                new SnapshotWarning(
                    "INNER_POWER_STATE_UNAVAILABLE",
                    reason));
            return SnapshotValue<InnerPowerStateSnapshot>.Unavailable(
                reason);
        }

        try
        {
            CombatSkillElement? backlashElement =
                item.InjuryOnUseType is >= 0 and <= 4
                    ? (CombatSkillElement)item.InjuryOnUseType
                    : null;
            var result = SnapshotValue<InnerPowerStateSnapshot>.Available(
                new InnerPowerStateSnapshot(
                    stateId,
                    MapText(
                        text.Resolve("NeiliType", item.Name),
                        $"Inner-power state {stateId} has no configured "
                        + "name."),
                    MapText(
                        text.Resolve("NeiliType", item.EffectDesc),
                        $"Inner-power state {stateId} has no configured "
                        + "effect description."),
                    CombatSnapshotMapping.MapElementAdjustments(
                        item.MaxPowerChange),
                    CombatSnapshotMapping.MapElementAdjustments(
                    item.RequirementChange),
                    backlashElement));
            warnings.Add(
                new SnapshotWarning(
                    "INNER_POWER_RUNTIME_MODIFIERS_NOT_APPLIED",
                    "The inner-power state was derived from persisted base "
                    + "five-element proportions. Runtime SpecialEffect "
                    + "modifiers are intentionally not executed by the "
                    + "read-only standalone adapter."));
            return result;
        }
        catch (Exception exception)
            when (exception is InvalidDataException
                  or ArgumentOutOfRangeException)
        {
            var reason = $"Inner-power state {stateId} is invalid: "
                + exception.Message;
            warnings.Add(
                new SnapshotWarning(
                    "INNER_POWER_STATE_UNAVAILABLE",
                    reason));
            return SnapshotValue<InnerPowerStateSnapshot>.Unavailable(
                reason);
        }
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
        CombatLoadoutSnapshot loadout,
        Dictionary<int, CombatSkillSnapshot> learnedById,
        GenericSlotAllocation genericSlotAllocation)
    {
        var equippedNeigong = loadout.NeigongSkillIds
            .Select(skillId => learnedById[skillId])
            .ToArray();
        return new SlotBudgetSet(
            Enum.GetValues<SkillCategory>().Select(category =>
                MapSlotBudget(
                    category,
                    CombatSlotBudgetCalculator.CalculateConfiguredCapacity(
                        category,
                        equippedNeigong,
                        genericSlotAllocation),
                    loadout)));
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
        Character character,
        TaiwuGameTextContext text)
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
                            text.Resolve("CharacterFeature", feature.Name),
                            $"Feature {featureId} has no configured name."),
                    feature is null || feature.Level < 0
                        ? SnapshotValue<int>.Unavailable(
                            $"Feature {featureId} has no configured level.")
                        : SnapshotValue<int>.Available(feature.Level)));
        }

        return result;
    }

    private static List<EquipmentSnapshot> MapEquipment(
        ItemKey[] equipment,
        TaiwuGameTextContext text)
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
                    MapEquipmentName(item, text),
                    SnapshotValue<EquipmentKind>.Available(
                        CombatSnapshotMapping.MapEquipmentKind(
                            item.ItemType)),
                    MapEquipmentSubtype(item)));
        }

        return result;
    }

    private static SnapshotValue<int> MapEquipmentSubtype(ItemKey item)
    {
        if (item.ItemType != ItemType.Weapon)
        {
            return SnapshotValue<int>.Unavailable(
                "Only weapon equipment has a weapon subtype.");
        }

        var weapon = Config.Weapon.Instance.GetItem(item.TemplateId);
        return weapon is not null && weapon.ItemSubType > 0
            ? SnapshotValue<int>.Available((int)weapon.ItemSubType)
            : SnapshotValue<int>.Unavailable(
                $"Weapon template {item.TemplateId} has no positive subtype.");
    }

    private static SnapshotValue<string> MapEquipmentName(
        ItemKey item,
        TaiwuGameTextContext text)
    {
        if (item.ItemType != ItemType.Weapon)
        {
            return SnapshotValue<string>.Unavailable(
                $"Display-name mapping is not available for item type "
                + $"{item.ItemType}.");
        }

        var name = Config.Weapon.Instance.GetItem(item.TemplateId)?.Name;
        return MapText(
            text.Resolve("Weapon", name),
            $"Weapon template {item.TemplateId} has no configured name.");
    }

    private static LegendaryBookCostState MapLegendaryBookCosts(
        Dictionary<int, CombatSkillSnapshot> learnedById,
        List<SnapshotWarning> warnings)
    {
        List<LegendaryBookCostSlot> slots = [];
        List<LegendaryBookCostAssignment> assignments = [];
        HashSet<int> assignedSkillIds = [];

        for (sbyte skillType = 0; skillType < 5; skillType++)
        {
            if (!DomainManager.Extra.TryGetElement_LegendaryBookSkillSlot(
                    skillType,
                    out ShortList source)
                || source.Items is null)
            {
                continue;
            }

            CombatSnapshotMapping.TryMapSkillCategory(
                skillType,
                out var category);
            for (var slotIndex = 0;
                 slotIndex < source.Items.Count;
                 slotIndex++)
            {
                var skillId = source.Items[slotIndex];
                if (skillId < 0)
                {
                    continue;
                }

                if (!learnedById.TryGetValue(skillId, out var skill))
                {
                    warnings.Add(
                        new SnapshotWarning(
                            "LEGENDARY_BOOK_SKILL_UNLEARNED",
                            $"Legendary-book cost slot references unlearned "
                            + $"skill {skillId}; the assignment was omitted."));
                    continue;
                }

                if (skill.Category != category)
                {
                    warnings.Add(
                        new SnapshotWarning(
                            "LEGENDARY_BOOK_SKILL_CATEGORY_MISMATCH",
                            $"Legendary-book cost slot reports skill {skillId} "
                            + $"under {category}, not {skill.Category}; the "
                            + "assignment was omitted."));
                    continue;
                }

                if (!assignedSkillIds.Add(skillId))
                {
                    warnings.Add(
                        new SnapshotWarning(
                            "LEGENDARY_BOOK_SKILL_DUPLICATED",
                            $"Skill {skillId} appears in more than one "
                            + "legendary-book fixed-cost slot; later "
                            + "assignments were omitted."));
                    continue;
                }

                var slotReference =
                    $"save:legendary-book:shouzhi:{skillType}:{slotIndex}";
                var slot = new LegendaryBookCostSlot(
                    slotReference,
                    new LegendaryBookCostRule(
                        LegendaryBookCostEffect.Shouzhi,
                        SnapshotDataSource.CurrentScreenObservation,
                        "docs/scenarios/"
                        + "M1-007-effective-skill-cost-evidence.md"));
                slots.Add(slot);
                assignments.Add(
                    new LegendaryBookCostAssignment(
                        slot,
                        skillId,
                        category,
                        LegendaryBookAssignmentOrigin.Save,
                        slotReference));
            }
        }

        return new LegendaryBookCostState(slots, assignments);
    }

    private static SnapshotValue<string> MapDisplayName(
        Character character,
        TaiwuGameTextContext text)
    {
        var displayName = text.ResolveCharacterName(character);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = text.ResolveFixedTemplateCharacterName(character);
        }

        return MapText(
            displayName,
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

internal sealed record LegendaryBookCostState(
    IReadOnlyList<LegendaryBookCostSlot> Slots,
    IReadOnlyList<LegendaryBookCostAssignment> Assignments);
