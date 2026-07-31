using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatEffects;

public static class VerifiedCombatEffectCatalogs
{
    public const string GoldenGameDataVersion =
        "1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a";

    public static CombatEffectCatalog GoldenAntiMagic { get; } =
        new(
            GoldenGameDataVersion,
            [
                Entry(
                    604,
                    "金猊镇魔刀",
                    PracticeDirection.Direct,
                    338,
                    "开始施展此功法时，「打断」敌人正在施展所有逆练功法，"
                    + "消除敌人正在运起的所有逆练功法；施展此功法的过程中，"
                    + "敌人无法施展任何逆练功法；此功法施展结束后，运用者"
                    + "获得3层此效果：运用者无法施展任何逆练功法，每当"
                    + "运用者施展任何正练功法，此效果减少1层",
                    CombatEffectMechanic.SuppressEnemyReversePractice),
                Entry(
                    604,
                    "金猊镇魔刀",
                    PracticeDirection.Reverse,
                    1064,
                    "开始施展此功法时，「打断」敌人正在施展所有正练功法，"
                    + "消除敌人正在运起的所有正练功法；施展此功法的过程中，"
                    + "敌人无法施展任何正练功法；此功法施展结束后，运用者"
                    + "获得3层此效果：运用者无法施展任何正练功法，每当"
                    + "运用者施展任何逆练功法，此效果减少1层",
                    CombatEffectMechanic.SuppressEnemyDirectPractice),
                Entry(
                    686,
                    "老君拂尘功",
                    PracticeDirection.Direct,
                    696,
                    "战斗开始时，运用者获得6层此效果：每当运用者的战败"
                    + "标记总数超过战败条件的一半时，消耗此效果为运用者"
                    + "消除伤势标记，每消除1个伤势标记，此效果消耗1层；"
                    + "此功法发挥$0$成威力时，此效果恢复1层，最多恢复至3层",
                    CombatEffectMechanic.RemoveOwnInjuryMarks),
                Entry(
                    686,
                    "老君拂尘功",
                    PracticeDirection.Reverse,
                    1422,
                    "战斗开始时，运用者获得6层此效果：每当运用者的战败"
                    + "标记总数超过战败条件的一半时，消耗此效果为运用者"
                    + "消除妨害标记，每消除1个妨害标记，此效果消耗1层；"
                    + "此功法发挥$0$成威力时，此效果恢复1层，最多恢复至3层",
                    CombatEffectMechanic.RemoveOwnHindranceMarks),
                Entry(
                    134,
                    "万花听雨式",
                    PracticeDirection.Direct,
                    247,
                    "此身法持续期间：运用者的动心不会被任何效果降低；"
                    + "运用者受到的所有提高动心的功法、状态效果大幅提高；"
                    + "敌人心韵激荡的持续时间大幅提高",
                    CombatEffectMechanic.ExtendEnemyMindResonanceDuration),
                Entry(
                    134,
                    "万花听雨式",
                    PracticeDirection.Reverse,
                    973,
                    "此身法持续期间：敌人的动心不会被任何效果提高；"
                    + "敌人受到的所有降低动心的功法、状态效果大幅提高；"
                    + "运用者心韵激荡的持续时间大幅降低",
                    CombatEffectMechanic.ShortenOwnMindResonanceDuration),
                Entry(
                    267,
                    "墨玉功",
                    PracticeDirection.Direct,
                    165,
                    "运用者受到的失神标记的持续时间大幅降低",
                    CombatEffectMechanic
                        .ShortenOwnDistractionMarkDuration),
                Entry(
                    267,
                    "墨玉功",
                    PracticeDirection.Reverse,
                    891,
                    "敌人受到的失神标记的持续时间大幅提高",
                    CombatEffectMechanic
                        .ExtendEnemyDistractionMarkDuration),
                Entry(
                    624,
                    "伏龙刀法",
                    PracticeDirection.Direct,
                    508,
                    "根据此功法发挥的成数，增加运用者所有「摧破」功法的"
                    + "威力，持续直到战斗结束",
                    CombatEffectMechanic.IncreaseOwnAttackSkillPower),
                Entry(
                    624,
                    "伏龙刀法",
                    PracticeDirection.Reverse,
                    1234,
                    "根据此功法发挥的成数，降低敌人所有「摧破」功法的"
                    + "威力，持续直到战斗结束",
                    CombatEffectMechanic.ReduceEnemyAttackSkillPower),
                Entry(
                    611,
                    "鬼庖丁刀法",
                    PracticeDirection.Direct,
                    439,
                    "持续增加运用者所有刀的解封进度；如果运用者使用的"
                    + "兵器为“鬼庖丁”，此功法对敌人造成的直接伤害根据"
                    + "运用者的战败标记提高，且根据此功法发挥的成数，额外"
                    + "增加“鬼庖丁”的解封进度；释放“鬼庖丁”的解封时，"
                    + "牺牲兵器的耐久或2个「机」式，运用者将自身的5个"
                    + "随机妨害标记转移给敌人",
                    CombatEffectMechanic.TransferOwnHindranceMarks),
                Entry(
                    611,
                    "鬼庖丁刀法",
                    PracticeDirection.Reverse,
                    1165,
                    "持续增加运用者所有刀的解封进度；此功法对敌人造成的"
                    + "直接伤害根据运用者的战败标记提高，如果敌我双方有"
                    + "任何人装备有「毒砂」、「药霜」，效果加倍，且可在"
                    + "解封任意刀时，大量牺牲解封兵器的耐久及3个任意可用"
                    + "的蓄式，运用者将自身的5个随机妨害标记转移给敌人",
                    CombatEffectMechanic.TransferOwnHindranceMarks),
                Entry(
                    291,
                    "七轮感应法",
                    PracticeDirection.Reverse,
                    915,
                    "敌人在受到损害状态时，所有损害状态的初始强度加倍，"
                    + "并向敌人额外施加一个缓慢减少随机类型真气的损害状态；"
                    + "且敌人的损害状态的强度在被增强时，增强的幅度也会提高",
                    CombatEffectMechanic.AmplifyEnemyDamageStates,
                    CombatEffectMechanic.DrainEnemyRandomTrueQi)
            ]);

    private static CombatEffectCatalogEntry Entry(
        int skillId,
        string skillName,
        PracticeDirection direction,
        int rawEffectId,
        string rawSourceText,
        params CombatEffectMechanic[] mechanics)
    {
        return new CombatEffectCatalogEntry(
            skillId,
            skillName,
            direction,
            rawEffectId,
            rawSourceText,
            $"local-config:Language_CN/SpecialEffect_language.txt"
            + $"#Desc_{rawEffectId}_0",
            mechanics);
    }
}
