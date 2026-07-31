using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatCounters;

public static class VerifiedCombatCounterRuleSets
{
    public static CombatCounterRuleSet GoldenMagicSound { get; } =
        CreateGoldenMagicSound();

    private static CombatCounterRuleSet CreateGoldenMagicSound()
    {
        var catalog = VerifiedCombatEffectCatalogs.GoldenAntiMagic;
        var version = catalog.GameDataVersion;

        return new CombatCounterRuleSet(
            version,
            [
                new CombatCounterRule(
                    "REVERSE_JINNI_SUPPRESSION",
                    [
                        "POSITIVE_MAGIC_SOUND_MIND_DAMAGE",
                        "DISTRACTION_MARK_ACCUMULATION",
                        "MIND_RESONANCE_CASCADE"
                    ],
                    CombatCounterStrength.HardCounter,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 604, PracticeDirection.Reverse, 1064),
                    requirements: [],
                    "Interrupts, clears, and temporarily prevents the "
                    + "target's Direct-practice skills."),
                new CombatCounterRule(
                    "REVERSE_LAOJUN_MARK_CLEAR",
                    [
                        "DISTRACTION_MARK_ACCUMULATION",
                        "MIND_RESONANCE_CASCADE"
                    ],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.CombatStartPassive,
                    Effect(catalog, 686, PracticeDirection.Reverse, 1422),
                    [
                        Passive(686, "reverse-laojun-equipped")
                    ],
                    "Starts with a finite pool that automatically removes "
                    + "the player's hindrance marks after the defeat-mark "
                    + "threshold."),
                new CombatCounterRule(
                    "REVERSE_WANHUA_RESONANCE",
                    [
                        "MIND_RESONANCE_CASCADE"
                    ],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAgility,
                    Effect(catalog, 134, PracticeDirection.Reverse, 973),
                    [
                        new SkillActivationRequirement(
                            134,
                            SkillActivationState.ActiveAgility,
                            CombatRequirementCriticality.Hard,
                            "local-rule:reverse-wanhua-active")
                    ],
                    "Greatly shortens the player's mind-resonance duration "
                    + "while this agility skill is active."),
                new CombatCounterRule(
                    "DIRECT_MOYU_MARK_DURATION",
                    [
                        "DISTRACTION_MARK_ACCUMULATION",
                        "MIND_RESONANCE_CASCADE"
                    ],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.EquippedPassive,
                    Effect(catalog, 267, PracticeDirection.Direct, 165),
                    [
                        Passive(267, "direct-moyu-equipped")
                    ],
                    "Greatly shortens the duration of the player's "
                    + "distraction marks."),
                new CombatCounterRule(
                    "REVERSE_FULONG_POWER_REDUCTION",
                    [
                        "POSITIVE_MAGIC_SOUND_MIND_DAMAGE",
                        "DISTRACTION_MARK_ACCUMULATION"
                    ],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 624, PracticeDirection.Reverse, 1234),
                    requirements: [],
                    "Reduces all enemy attack-skill power according to "
                    + "achieved effectiveness for the rest of combat."),
                new CombatCounterRule(
                    "REVERSE_QILUN_TRUE_QI_DRAIN",
                    [
                        "DEFEAT_MARK_RESET_LOOP"
                    ],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.EquippedPassive,
                    Effect(catalog, 291, PracticeDirection.Reverse, 915),
                    [
                        Passive(291, "reverse-qilun-equipped")
                    ],
                    "When the target receives a damage state, it doubles "
                    + "the state's initial intensity and adds a slowly "
                    + "decreasing random true-Qi state. This can deplete "
                    + "the Qiqiao resource that powers repeated resets, "
                    + "but the drained true-Qi type is random, so this is "
                    + "mitigation rather than a guaranteed counter.")
            ]);
    }

    private static SkillActivationRequirement Passive(
        int skillId,
        string evidenceName)
    {
        return new SkillActivationRequirement(
            skillId,
            SkillActivationState.EquippedPassive,
            CombatRequirementCriticality.Hard,
            $"local-rule:{evidenceName}");
    }

    private static CombatEffectCatalogEntry Effect(
        CombatEffectCatalog catalog,
        int skillId,
        PracticeDirection direction,
        int rawEffectId)
    {
        var resolution = catalog.Resolve(
            catalog.GameDataVersion,
            skillId,
            direction,
            rawEffectId);
        return resolution.IsRecognized
            ? resolution.CatalogEntry!
            : throw new InvalidOperationException(
                $"Verified counter effect {rawEffectId} was not resolved.");
    }
}
