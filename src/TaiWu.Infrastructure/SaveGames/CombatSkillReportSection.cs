using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.CombatSkill;
using GameData.Domains.Item;
using static TaiWu.Infrastructure.SaveGames.GameDataText;

namespace TaiWu.Infrastructure.SaveGames;

internal static class CombatSkillReportSection
{
    private const string StandaloneUnavailable =
        "(unavailable:standalone-runtime)";

    public static void Write(TaiwuReportContext context)
    {
        WriteSlots(context);
        WriteTargetCharacter(context);
        WriteLearnedSkills(context);
        WriteEquipment(context);
    }

    private static void WriteSlots(TaiwuReportContext context)
    {
        var writer = context.Writer;
        var equipment = context.Equipment;

        writer.Write(
            "SLOTS|NEIGONG={0}/{1}:{2}|ATTACK={3}/{4}:{5}|AGILITY={6}/{7}:{8}|DEFENSE={9}/{10}:{11}|ASSISTANCE={12}/{13}:{14}",
            equipment.Neigong.Count,
            equipment.Neigong.Capacity,
            JoinNumbers(equipment.Neigong.ToArray()),
            equipment.Attack.Count,
            equipment.Attack.Capacity,
            JoinNumbers(equipment.Attack.ToArray()),
            equipment.Agility.Count,
            equipment.Agility.Capacity,
            JoinNumbers(equipment.Agility.ToArray()),
            equipment.Defense.Count,
            equipment.Defense.Capacity,
            JoinNumbers(equipment.Defense.ToArray()),
            equipment.Assistance.Count,
            equipment.Assistance.Capacity,
            JoinNumbers(equipment.Assistance.ToArray()));
        writer.Write(
            "SLOTCOUNTS|NEIGONG={0}|ATTACK={1}|AGILITY={2}|DEFENSE={3}|ASSISTANCE={4}",
            SafeText(() => context.Taiwu.GetCombatSkillSlotCountWithGeneric(0)),
            StandaloneUnavailable,
            StandaloneUnavailable,
            StandaloneUnavailable,
            StandaloneUnavailable);

        WriteGenericGridAllocation(context);
        WriteExtraSlots(context);
        WriteSkillList(writer, "NEIGONG", 0, equipment);
        WriteSkillList(writer, "ATTACK", 1, equipment);
        WriteSkillList(writer, "AGILITY", 2, equipment);
        WriteSkillList(writer, "DEFENSE", 3, equipment);
        WriteSkillList(writer, "ASSISTANCE", 4, equipment);
    }

    private static void WriteGenericGridAllocation(TaiwuReportContext context)
    {
        try
        {
            context.Writer.Write(
                "GRIDALLOCATION|generic={0}",
                JoinNumbers(DomainManager.Taiwu.GetGenericGridAllocation()));
        }
        catch (Exception exception)
        {
            context.Writer.Write(
                "GRIDALLOCATION|error={0}",
                exception.GetType().Name);
        }
    }

    private static void WriteExtraSlots(TaiwuReportContext context)
    {
        context.Writer.Write("EXTRASLOTS|unavailable=standalone-runtime");
        context.Writer.Write(
            "EXTRASLOTSREFLECT|unavailable=standalone-runtime");
    }

    private static void WriteTargetCharacter(TaiwuReportContext context)
    {
        if (context.TargetCharacterId is not { } targetId)
        {
            return;
        }

        if (!DomainManager.Character.TryGetElement_Objects(
                targetId,
                out Character target))
        {
            context.Writer.Write("TARGET|id={0}|missing=true", targetId);
            return;
        }

        context.Writer.Write(
            "TARGET|id={0}|name={1}{2}|age={3}|health={4}|consummate={5}|neili={6}|maxNeili={7}|canDefeat={8}|dieImmunity={9}|fatalImmunity={10}",
            targetId,
            target.GetSurname(),
            target.GetGivenName(),
            target.GetCurrAge(),
            target.GetHealth(),
            target.GetConsummateLevel(),
            target.GetCurrNeili(),
            target.GetMaxNeili(),
            SafeText(() => target.GetCanDefeat()),
            SafeText(() => target.GetDieImmunity()),
            SafeText(() => target.GetFatalImmunity()));

        WriteTargetFeatures(context.Writer, target);
        WriteTargetSkills(context.Writer, targetId, target);
        WriteTargetItems(context.Writer, target);
    }

    private static void WriteTargetFeatures(
        LegacyReportWriter writer,
        Character target)
    {
        foreach (var featureId in target.GetFeatureIds())
        {
            var feature = Config.CharacterFeature.Instance.GetItem(featureId);
            writer.Write(
                "TARGETFEATURE|id={0}|name={1}|level={2}",
                featureId,
                feature?.Name ?? "(unknown)",
                feature?.Level ?? -1);
        }
    }

    private static void WriteTargetSkills(
        LegacyReportWriter writer,
        int targetId,
        Character target)
    {
        var equipment = target.GetCombatSkillEquipment();
        HashSet<short> equippedIds = [];
        equipment.GetValidSkills(equippedIds);
        var skills = DomainManager.CombatSkill.GetCharCombatSkills(targetId);

        foreach (var skillId in equippedIds)
        {
            if (!skills.TryGetValue(skillId, out var skill))
            {
                continue;
            }

            var item = skill.Template;
            writer.Write(
                "TARGETSKILL|id={0}|name={1}|grade={2}|type={3}|equipType={4}|direction={5}|read={6}|active={7}|directEffect={8}|reverseEffect={9}|prepare={10}|breathStance={11}|tricks={12}|penetrate={13}",
                skillId,
                item.Name,
                item.Grade,
                item.Type,
                item.EquipType,
                CombatSkillStateHelper.GetCombatSkillDirection(
                    skill.GetActivationState()),
                skill.GetReadingState(),
                skill.GetActivationState(),
                item.DirectEffectID,
                item.ReverseEffectID,
                item.PrepareTotalProgress,
                item.BreathStanceTotalCost,
                TrickText(item),
                item.Penetrate);
        }

        foreach (var (skillId, skill) in skills)
        {
            var item = skill.Template;
            if (item.Type != 13 && item.EquipType != 1)
            {
                continue;
            }

            writer.Write(
                "TARGETLEARNED|id={0}|name={1}|grade={2}|type={3}|equipType={4}|direction={5}|read={6}|active={7}|directEffect={8}|reverseEffect={9}|prepare={10}|tricks={11}|penetrate={12}",
                skillId,
                item.Name,
                item.Grade,
                item.Type,
                item.EquipType,
                CombatSkillStateHelper.GetCombatSkillDirection(
                    skill.GetActivationState()),
                skill.GetReadingState(),
                skill.GetActivationState(),
                item.DirectEffectID,
                item.ReverseEffectID,
                item.PrepareTotalProgress,
                TrickText(item),
                item.Penetrate);
        }
    }

    private static void WriteTargetItems(
        LegacyReportWriter writer,
        Character target)
    {
        var items = target.GetEquipment();
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            writer.Write(
                "TARGETITEM|slot={0}|type={1}|template={2}|id={3}|weapon={4}",
                index,
                item.ItemType,
                item.TemplateId,
                item.Id,
                index < 3 ? WeaponName(item) : string.Empty);
        }
    }

    private static void WriteLearnedSkills(TaiwuReportContext context)
    {
        var skills = DomainManager.CombatSkill.GetCharCombatSkills(context.TaiwuId);
        var skillCountByGrade = new int[9];
        var fullPowerByGrade = new int[9];
        var activatedByGrade = new int[9];
        List<KeyValuePair<short, int>> mindStepSkills = [];

        foreach (var id in skills.Keys.Order())
        {
            var skill = skills[id];
            var item = skill.Template;
            var power = PrivateField<short>(skill, "_power");
            var maxPower = PrivateField<short>(skill, "_maxPower");
            if (item.Grade >= 0 && item.Grade < skillCountByGrade.Length)
            {
                skillCountByGrade[item.Grade]++;
                if (power >= maxPower)
                {
                    fullPowerByGrade[item.Grade]++;
                }

                if (skill.GetActivationState() != 0)
                {
                    activatedByGrade[item.Grade]++;
                }
            }

            var mindStep = item.MindDamageStep;
            try
            {
                mindStep = skill.CalcMindDamageStep();
            }
            catch
            {
                // Some skills require combat state; template data is the fallback.
            }

            if (mindStep > 0)
            {
                mindStepSkills.Add(new KeyValuePair<short, int>(id, mindStep));
            }

            var mastered =
                DomainManager.Extra.IsCombatSkillMasteredByCharacter(
                    context.TaiwuId,
                    id);
            context.Writer.Write(
                "SKILL|{0}|{1}|grade={2}|gridCost={3}|mastered={4}|specificGrids={5}|genericGrid={6}|type={7}|equipType={8}|direction={9}|power={10}|maxPower={11}|read={12}|active={13}|equipped={14}|cast={15}|hits={16}|flaw={17}|directEffect={18}|reverseEffect={19}|prepare={20}|breathStance={21}|tricks={22}|damageDist={23}|outerSteps={24}|innerSteps={25}|penetrate={26}",
                id,
                item.Name,
                item.Grade,
                item.GridCost,
                mastered,
                JoinNumbers(item.SpecificGrids),
                item.GenericGrid,
                item.Type,
                item.EquipType,
                CombatSkillStateHelper.GetCombatSkillDirection(
                    skill.GetActivationState()),
                power,
                maxPower,
                skill.GetReadingState(),
                skill.GetActivationState(),
                context.EquippedSkillIds.Contains(id),
                item.CastSpeed,
                item.TotalHit,
                item.HasAtkFlawEffect,
                item.DirectEffectID,
                item.ReverseEffectID,
                item.PrepareTotalProgress,
                item.BreathStanceTotalCost,
                TrickText(item),
                JoinNumbers(item.PerHitDamageRateDistribution),
                JoinNumbers(item.OuterDamageSteps),
                JoinNumbers(item.InnerDamageSteps),
                item.Penetrate);
        }

        WriteMindStepSkills(context, mindStepSkills);
        WriteSkillSummary(
            context.Writer,
            skills.Count,
            skillCountByGrade,
            fullPowerByGrade,
            activatedByGrade);
    }

    private static void WriteMindStepSkills(
        TaiwuReportContext context,
        List<KeyValuePair<short, int>> skills)
    {
        foreach (var skill in skills
                     .OrderByDescending(pair => pair.Value)
                     .Take(12))
        {
            context.Writer.Write(
                "MINDSKILL|id={0}|name={1}|mindStep={2}|equipped={3}",
                skill.Key,
                SkillName(skill.Key),
                skill.Value,
                context.EquippedSkillIds.Contains(skill.Key));
        }
    }

    private static void WriteSkillSummary(
        LegacyReportWriter writer,
        int total,
        IReadOnlyList<int> skillCountByGrade,
        IReadOnlyList<int> fullPowerByGrade,
        IReadOnlyList<int> activatedByGrade)
    {
        writer.Write("SKILLSUMMARY|total={0}", total);
        for (var grade = 0; grade < skillCountByGrade.Count; grade++)
        {
            writer.Write(
                "SKILLGRADE|grade={0}|count={1}|fullPower={2}|activated={3}",
                grade,
                skillCountByGrade[grade],
                fullPowerByGrade[grade],
                activatedByGrade[grade]);
        }
    }

    private static void WriteEquipment(TaiwuReportContext context)
    {
        ItemKey[] equipment = context.Taiwu.GetEquipment();
        for (var index = 0; index < equipment.Length; index++)
        {
            var item = equipment[index];
            context.Writer.Write(
                "ITEMEQUIP|{0}|type={1}|template={2}|id={3}|{4}",
                index,
                item.ItemType,
                item.TemplateId,
                item.Id,
                index < 3 ? WeaponName(item) : string.Empty);
        }
    }
}
