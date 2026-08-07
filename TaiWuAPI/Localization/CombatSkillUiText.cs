using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Localization;

public static class CombatSkillUiText
{
    public static string Category(
        TaiwuLanguage language,
        CombatSkillDiscipline category) => language == TaiwuLanguage.Chinese
            ? category switch
            {
                CombatSkillDiscipline.Neigong => "內功",
                CombatSkillDiscipline.Agility => "身法",
                CombatSkillDiscipline.SpecialTechnique => "絕技",
                CombatSkillDiscipline.FistAndPalm => "拳掌",
                CombatSkillDiscipline.Finger => "指法",
                CombatSkillDiscipline.Leg => "腿法",
                CombatSkillDiscipline.HiddenWeapon => "暗器",
                CombatSkillDiscipline.Sword => "劍法",
                CombatSkillDiscipline.Blade => "刀法",
                CombatSkillDiscipline.LongWeapon => "長兵",
                CombatSkillDiscipline.ExoticWeapon => "奇門",
                CombatSkillDiscipline.FlexibleWeapon => "軟兵",
                CombatSkillDiscipline.Archery => "御射",
                CombatSkillDiscipline.Music => "樂器",
                _ => category.ToString()
            }
            : category.ToString();

    public static string Faction(
        TaiwuLanguage language,
        CombatSkillFactionId faction) => Faction(language, faction.Value);

    public static string Faction(TaiwuLanguage language, int factionId) =>
        language == TaiwuLanguage.Chinese
            ? factionId switch
            {
                0 => "無門無派",
                1 => "少林派",
                2 => "峨眉派",
                3 => "百花谷",
                4 => "武當派",
                5 => "元山派",
                6 => "獅相門",
                7 => "然山派",
                8 => "璇女派",
                9 => "鑄劍山莊",
                10 => "空桑派",
                11 => "金剛宗",
                12 => "五仙教",
                13 => "界青門",
                14 => "伏龍壇",
                15 => "血犼教",
                16 => "太吾村",
                17 => "外道",
                18 => "任俠",
                19 => "相樞爪牙",
                _ => $"門派 {factionId}"
            }
            : factionId switch
            {
                0 => "Sectless",
                1 => "Shaolin Sect",
                2 => "Emei Sect",
                3 => "Valley of Flowers",
                4 => "Wudang Sect",
                5 => "Yuanshan Sect",
                6 => "Lion-Face Clan",
                7 => "Ranshan Sect",
                8 => "Jade-Maiden Sect",
                9 => "Swordsmith Villa",
                10 => "Kongsang Sect",
                11 => "Vajra Sect",
                12 => "Five Immortals Sect",
                13 => "Veil Scar Sect",
                14 => "Fuloong Sanctuary",
                15 => "Sanguine Sect",
                16 => "Taiwu Village",
                17 => "Outlaw",
                18 => "Vigilante",
                19 => "Xiangshu Underling",
                _ => $"Faction {factionId}"
            };

    public static string Element(
        TaiwuLanguage language,
        CombatSkillElement element) => language == TaiwuLanguage.Chinese
            ? element switch
            {
                CombatSkillElement.Metal => "金剛",
                CombatSkillElement.Wood => "紫霞",
                CombatSkillElement.Water => "玄陰",
                CombatSkillElement.Fire => "純陽",
                CombatSkillElement.Earth => "歸元",
                CombatSkillElement.Mixed => "混元",
                _ => element.ToString()
            }
            : element switch
            {
                CombatSkillElement.Metal => "Metal Qi",
                CombatSkillElement.Wood => "Wood Qi",
                CombatSkillElement.Water => "Water Qi",
                CombatSkillElement.Fire => "Fire Qi",
                CombatSkillElement.Earth => "Earth Qi",
                CombatSkillElement.Mixed => "Hunyuan Qi",
                _ => element.ToString()
            };

    public static string Alignment(
        TaiwuLanguage language,
        CombatSkillFactionAlignment alignment) =>
        language == TaiwuLanguage.Chinese
            ? alignment switch
            {
                CombatSkillFactionAlignment.Just => "剛正",
                CombatSkillFactionAlignment.Kind => "仁善",
                CombatSkillFactionAlignment.Even => "中庸",
                CombatSkillFactionAlignment.Rebel => "叛逆",
                CombatSkillFactionAlignment.Egoistic => "唯我",
                _ => alignment.ToString()
            }
            : alignment switch
            {
                CombatSkillFactionAlignment.Just => "Principled",
                CombatSkillFactionAlignment.Kind => "Benevolent",
                CombatSkillFactionAlignment.Even => "Moderate",
                CombatSkillFactionAlignment.Rebel => "Rebellious",
                CombatSkillFactionAlignment.Egoistic => "Egocentric",
                _ => alignment.ToString()
            };
}
