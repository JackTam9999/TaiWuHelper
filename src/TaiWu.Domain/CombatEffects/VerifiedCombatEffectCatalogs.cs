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
                    "金猊鎮魔刀",
                    PracticeDirection.Direct,
                    338,
                    "開始施展此功法時，「打斷」敵人正在施展所有逆練功法，"
                    + "消除敵人正在運起的所有逆練功法；施展此功法的過程中，"
                    + "敵人無法施展任何逆練功法；此功法施展結束後，運用者"
                    + "獲得3層此效果：運用者無法施展任何逆練功法，每當"
                    + "運用者施展任何正練功法，此效果減少1層",
                    CombatEffectMechanic.SuppressEnemyReversePractice),
                Entry(
                    604,
                    "金猊鎮魔刀",
                    PracticeDirection.Reverse,
                    1064,
                    "開始施展此功法時，「打斷」敵人正在施展所有正練功法，"
                    + "消除敵人正在運起的所有正練功法；施展此功法的過程中，"
                    + "敵人無法施展任何正練功法；此功法施展結束後，運用者"
                    + "獲得3層此效果：運用者無法施展任何正練功法，每當"
                    + "運用者施展任何逆練功法，此效果減少1層",
                    CombatEffectMechanic.SuppressEnemyDirectPractice),
                Entry(
                    686,
                    "老君拂塵功",
                    PracticeDirection.Direct,
                    696,
                    "戰鬥開始時，運用者獲得6層此效果：每當運用者的戰敗"
                    + "標記總數超過戰敗條件的一半時，消耗此效果為運用者"
                    + "消除傷勢標記，每消除1個傷勢標記，此效果消耗1層；"
                    + "此功法發揮$0$成威力時，此效果恢復1層，最多恢復至3層",
                    CombatEffectMechanic.RemoveOwnInjuryMarks),
                Entry(
                    686,
                    "老君拂塵功",
                    PracticeDirection.Reverse,
                    1422,
                    "戰鬥開始時，運用者獲得6層此效果：每當運用者的戰敗"
                    + "標記總數超過戰敗條件的一半時，消耗此效果為運用者"
                    + "消除妨害標記，每消除1個妨害標記，此效果消耗1層；"
                    + "此功法發揮$0$成威力時，此效果恢復1層，最多恢復至3層",
                    CombatEffectMechanic.RemoveOwnHindranceMarks),
                Entry(
                    134,
                    "萬花聽雨式",
                    PracticeDirection.Direct,
                    247,
                    "此身法持續期間：運用者的動心不會被任何效果降低；"
                    + "運用者受到的所有提高動心的功法、狀態效果大幅提高；"
                    + "敵人心韻激盪的持續時間大幅提高",
                    CombatEffectMechanic.ExtendEnemyMindResonanceDuration),
                Entry(
                    134,
                    "萬花聽雨式",
                    PracticeDirection.Reverse,
                    973,
                    "此身法持續期間：敵人的動心不會被任何效果提高；"
                    + "敵人受到的所有降低動心的功法、狀態效果大幅提高；"
                    + "運用者心韻激盪的持續時間大幅降低",
                    CombatEffectMechanic.ShortenOwnMindResonanceDuration),
                Entry(
                    267,
                    "墨玉功",
                    PracticeDirection.Direct,
                    165,
                    "運用者受到的失神標記的持續時間大幅降低",
                    CombatEffectMechanic
                        .ShortenOwnDistractionMarkDuration),
                Entry(
                    267,
                    "墨玉功",
                    PracticeDirection.Reverse,
                    891,
                    "敵人受到的失神標記的持續時間大幅提高",
                    CombatEffectMechanic
                        .ExtendEnemyDistractionMarkDuration),
                Entry(
                    624,
                    "伏龍刀法",
                    PracticeDirection.Direct,
                    508,
                    "根據此功法發揮的成數，增加運用者所有「摧破」功法的"
                    + "威力，持續直到戰鬥結束",
                    CombatEffectMechanic.IncreaseOwnAttackSkillPower),
                Entry(
                    624,
                    "伏龍刀法",
                    PracticeDirection.Reverse,
                    1234,
                    "根據此功法發揮的成數，降低敵人所有「摧破」功法的"
                    + "威力，持續直到戰鬥結束",
                    CombatEffectMechanic.ReduceEnemyAttackSkillPower),
                Entry(
                    611,
                    "鬼庖丁刀法",
                    PracticeDirection.Direct,
                    439,
                    "持續增加運用者所有刀的解封進度；如果運用者使用的"
                    + "兵器為「鬼庖丁」，此功法對敵人造成的直接傷害根據"
                    + "運用者的戰敗標記提高，且根據此功法發揮的成數，額外"
                    + "增加「鬼庖丁」的解封進度；釋放「鬼庖丁」的解封時，"
                    + "犧牲兵器的耐久或2個「機」式，運用者將自身的5個"
                    + "隨機妨害標記轉移給敵人",
                    CombatEffectMechanic.TransferOwnHindranceMarks),
                Entry(
                    611,
                    "鬼庖丁刀法",
                    PracticeDirection.Reverse,
                    1165,
                    "持續增加運用者所有刀的解封進度；此功法對敵人造成的"
                    + "直接傷害根據運用者的戰敗標記提高，如果敵我雙方有"
                    + "任何人裝備有「毒砂」、「藥霜」，效果加倍，且可在"
                    + "解封任意刀時，大量犧牲解封兵器的耐久及3個任意可用"
                    + "的蓄式，運用者將自身的5個隨機妨害標記轉移給敵人",
                    CombatEffectMechanic.TransferOwnHindranceMarks),
                Entry(
                    291,
                    "七輪感應法",
                    PracticeDirection.Reverse,
                    915,
                    "敵人在受到損害狀態時，所有損害狀態的初始強度加倍，"
                    + "並向敵人額外施加一個緩慢減少隨機類型真氣的損害狀態；"
                    + "且敵人的損害狀態的強度在被增強時，增強的幅度也會提高",
                    CombatEffectMechanic.AmplifyEnemyDamageStates,
                    CombatEffectMechanic.DrainEnemyRandomTrueQi)
            ]);

    public static CombatEffectCatalog CurrentAntiMagic { get; } =
        new(
            "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20",
            [
                CurrentEntry(
                    604,
                    "金猊鎮魔刀",
                    PracticeDirection.Reverse,
                    1064,
                    "Suppresses Direct practice during the cast and then "
                    + "applies three Direct-practice self-lock layers.",
                    CombatEffectMechanic.SuppressEnemyDirectPractice,
                    CombatEffectMechanic.LockOwnDirectPracticeThreeLayers),
                CurrentEntry(
                    686,
                    "老君拂塵功",
                    PracticeDirection.Reverse,
                    1422,
                    "Starts with finite hindrance-mark removal layers and "
                    + "can restore a layer on a qualifying Reverse cast.",
                    CombatEffectMechanic.RemoveOwnHindranceMarks,
                    CombatEffectMechanic.RestoreFiniteMarkRemovalLayer,
                    CombatEffectMechanic
                        .RemoveOwnDirectPracticeLockLayerOnReverseCast),
                CurrentEntry(
                    602,
                    "斬鰲刀法",
                    PracticeDirection.Reverse,
                    1062,
                    "Raises leg direct damage and conditionally exhausts "
                    + "enemy mobility and clears active agility.",
                    CombatEffectMechanic.IncreaseOwnLegDirectDamage,
                    CombatEffectMechanic.ExhaustEnemyMobility,
                    CombatEffectMechanic.ClearEnemyActiveAgility,
                    CombatEffectMechanic
                        .RemoveOwnDirectPracticeLockLayerOnReverseCast),
                CurrentEntry(
                    616,
                    "羅剎刀法",
                    PracticeDirection.Reverse,
                    1251,
                    "Raises torso direct damage and conditionally worsens "
                    + "one enemy external injury.",
                    CombatEffectMechanic.IncreaseOwnTorsoDirectDamage,
                    CombatEffectMechanic.WorsenEnemyExternalInjury,
                    CombatEffectMechanic
                        .RemoveOwnDirectPracticeLockLayerOnReverseCast),
                CurrentEntry(
                    599,
                    "開山快刀",
                    PracticeDirection.Reverse,
                    1059,
                    "Grants two chop tricks when the Reverse cast reaches "
                    + "its verified branch.",
                    CombatEffectMechanic.GrantOwnChopTricks,
                    CombatEffectMechanic
                        .RemoveOwnDirectPracticeLockLayerOnReverseCast),
                CurrentEntry(
                    134,
                    "萬花聽雨式",
                    PracticeDirection.Reverse,
                    973,
                    "Shortens the player's mind-resonance duration while "
                    + "this agility skill is active.",
                    CombatEffectMechanic
                        .ShortenOwnMindResonanceDuration),
                CurrentEntry(
                    150,
                    "五鬼步",
                    PracticeDirection.Reverse,
                    989,
                    "Makes enemy weapon attacks parryable while this "
                    + "agility skill is active.",
                    CombatEffectMechanic.MakeEnemyWeaponAttacksParryable),
                CurrentEntry(
                    151,
                    "御風符",
                    PracticeDirection.Reverse,
                    990,
                    "Reduces enemy cast speed from accumulated movement "
                    + "while this agility skill is active.",
                    CombatEffectMechanic.ReduceEnemyCastSpeedByMovement),
                CurrentEntry(
                    147,
                    "鐵橋功",
                    PracticeDirection.Direct,
                    260,
                    "Reduces enemy hit probability at verified long range "
                    + "while this agility skill is active.",
                    CombatEffectMechanic.ReduceEnemyHitAtLongRange),
                CurrentEntry(
                    148,
                    "橫江鎖",
                    PracticeDirection.Direct,
                    261,
                    "Triggers a weapon counterattack after enough enemy "
                    + "forward movement while active.",
                    CombatEffectMechanic.CounterattackEnemyAdvance),
                CurrentEntry(
                    295,
                    "即身成佛",
                    PracticeDirection.Reverse,
                    919,
                    "Protects against non-vital critical injury and spends "
                    + "defense true Qi to remove hindrance marks.",
                    CombatEffectMechanic
                        .PreventOwnCriticalInjuryOnDefense,
                    CombatEffectMechanic.RemoveOwnHindranceMarks,
                    CombatEffectMechanic
                        .SpendDefenseTrueQiForHindranceMarkRemoval),
                CurrentEntry(
                    303,
                    "鬼降大法",
                    PracticeDirection.Reverse,
                    927,
                    "Reduces non-vital direct damage and conditionally "
                    + "converts mind-loss marks to acupoints.",
                    CombatEffectMechanic.ReduceOwnNonVitalDirectDamage,
                    CombatEffectMechanic.ConvertOwnMindLossMarksToAcupoints),
                CurrentEntry(
                    2,
                    "水火硬氣功",
                    PracticeDirection.Direct,
                    1739,
                    "Reduces incoming direct damage while this defense is "
                    + "active, with verified decay behavior.",
                    CombatEffectMechanic.ReduceOwnDirectDamage),
                CurrentEntry(
                    289,
                    "拿脈功",
                    PracticeDirection.Direct,
                    187,
                    "A successful weapon counter applies enemy "
                    + "stance-recovery pressure.",
                    CombatEffectMechanic
                        .ApplyEnemyStanceRecoveryPenaltyOnCounter),
                CurrentEntry(
                    267,
                    "墨玉功",
                    PracticeDirection.Direct,
                    165,
                    "Shortens the duration of the player's distraction "
                    + "marks while equipped.",
                    CombatEffectMechanic
                        .ShortenOwnDistractionMarkDuration),
                CurrentEntry(
                    265,
                    "冰清玉潔",
                    PracticeDirection.Reverse,
                    889,
                    "Converts the verified charm input into increased own "
                    + "mind defense while equipped.",
                    CombatEffectMechanic.IncreaseOwnMindDefenseFromCharm),
                CurrentEntry(
                    280,
                    "三部九候法",
                    PracticeDirection.Reverse,
                    904,
                    "Increases avoidance and mind defense as distance "
                    + "closes below the verified threshold.",
                    CombatEffectMechanic.IncreaseOwnAvoidanceAtCloseRange),
                CurrentEntry(
                    252,
                    "兵聞拙速",
                    PracticeDirection.Direct,
                    150,
                    "Restores own mobility when enemy direct damage creates "
                    + "a qualifying injury or fatal mark.",
                    CombatEffectMechanic.RestoreOwnMobilityOnEnemyInjury),
                CurrentEntry(
                    624,
                    "伏龍刀法",
                    PracticeDirection.Reverse,
                    1234,
                    "Reduces enemy attack-skill power by achieved power "
                    + "until combat ends.",
                    CombatEffectMechanic.ReduceEnemyAttackSkillPower)
            ]);

    public static CombatEffectCatalog Epic5TargetFamilies { get; } =
        new(
            GoldenGameDataVersion,
            [
                Entry(
                    282,
                    "五黃辟毒術",
                    PracticeDirection.Direct,
                    180,
                    "以此功法進行防禦時：運用者免受所有直接毒害；敵人對"
                    + "運用者施加的毒素，反而會減少運用者相應的毒素",
                    CombatEffectMechanic
                        .PreventOwnDirectPoisonWhileDefending,
                    CombatEffectMechanic
                        .ReduceOwnCorrespondingPoisonOnEnemyApplication),
                Entry(
                    282,
                    "五黃辟毒術",
                    PracticeDirection.Reverse,
                    906,
                    "以此功法進行防禦時：運用者免受所有直接毒害；敵人對"
                    + "運用者施加的毒素，反而會施加到敵人自己的身上",
                    CombatEffectMechanic
                        .PreventOwnDirectPoisonWhileDefending,
                    CombatEffectMechanic.ReflectEnemyAppliedPoison),
                Entry(
                    687,
                    "錯倒陰陽拂塵",
                    PracticeDirection.Direct,
                    697,
                    "此功法造成的直接外傷改由敵人的禦氣抵擋；施展此功法"
                    + "的過程中，敵人所有的正練「奇竅」功法無法生效；"
                    + "此功法施展結束後，極短時間內封禁敵人所有的正練"
                    + "「奇竅」功法",
                    CombatEffectMechanic
                        .RouteOwnOuterDamageThroughEnemyInnerResistance),
                Entry(
                    687,
                    "錯倒陰陽拂塵",
                    PracticeDirection.Reverse,
                    1423,
                    "此功法造成的直接內傷改由敵人的禦體抵擋；施展此功法"
                    + "的過程中，敵人所有的逆練「奇竅」功法無法生效；"
                    + "此功法施展結束後，極短時間內封禁敵人所有的逆練"
                    + "「奇竅」功法",
                    CombatEffectMechanic
                        .RouteOwnInnerDamageThroughEnemyOuterResistance)
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
            $"local-config:Language_CNH/SpecialEffect_language.txt"
            + $"#Desc_{rawEffectId}_0",
            mechanics);
    }

    private static CombatEffectCatalogEntry CurrentEntry(
        int skillId,
        string skillName,
        PracticeDirection direction,
        int rawEffectId,
        string verifiedBehaviorSummary,
        params CombatEffectMechanic[] mechanics) => new(
        skillId,
        skillName,
        direction,
        rawEffectId,
        verifiedBehaviorSummary,
        $"E8-F01-CURRENT-RUNTIME-BEHAVIOR#SKILL_{skillId}_"
        + direction.ToString().ToUpperInvariant(),
        mechanics.Order());
}
