using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatCounters;

public static class VerifiedCombatCounterRuleSets
{
    public static CombatCounterRuleSet GoldenMagicSound { get; } =
        CreateGoldenMagicSound();

    public static CombatCounterRuleSet Epic5TargetFamilies { get; } =
        CreateEpic5TargetFamilies();

    public static CombatCounterRuleSet CurrentMagicSound { get; } =
        CreateCurrentMagicSound();

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
                        "DISTRACTION_MARK_ACCUMULATION",
                        "CONFIGURED_OUTER_DAMAGE_PRESSURE"
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

    private static CombatCounterRuleSet CreateEpic5TargetFamilies()
    {
        var catalog = VerifiedCombatEffectCatalogs.Epic5TargetFamilies;
        return new CombatCounterRuleSet(
            catalog.GameDataVersion,
            [
                new CombatCounterRule(
                    "DIRECT_WUHUANG_POISON_DEFENSE",
                    ["CONFIGURED_POISON_APPLICATION"],
                    CombatCounterStrength.HardCounter,
                    CombatCounterActivationTiming.ActiveDefense,
                    Effect(catalog, 282, PracticeDirection.Direct, 180),
                    [ActiveDefense(282, "direct-wuhuang-active-defense")],
                    "While this defense is active, direct poisoning is "
                    + "prevented and enemy-applied poison instead reduces "
                    + "the player's corresponding poison."),
                new CombatCounterRule(
                    "REVERSE_WUHUANG_POISON_DEFENSE",
                    ["CONFIGURED_POISON_APPLICATION"],
                    CombatCounterStrength.HardCounter,
                    CombatCounterActivationTiming.ActiveDefense,
                    Effect(catalog, 282, PracticeDirection.Reverse, 906),
                    [ActiveDefense(282, "reverse-wuhuang-active-defense")],
                    "While this defense is active, direct poisoning is "
                    + "prevented and enemy-applied poison is applied to "
                    + "the enemy instead."),
                new CombatCounterRule(
                    "DIRECT_YINYANG_ROUTE_OUTER_TO_INNER",
                    ["CHANNEL_RESISTANCE_ASYMMETRY"],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 687, PracticeDirection.Direct, 697),
                    requirements: [],
                    "Routes this skill's direct outer injury through the "
                    + "target's inner resistance."),
                new CombatCounterRule(
                    "REVERSE_YINYANG_ROUTE_INNER_TO_OUTER",
                    ["CHANNEL_RESISTANCE_ASYMMETRY"],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 687, PracticeDirection.Reverse, 1423),
                    requirements: [],
                    "Routes this skill's direct inner injury through the "
                    + "target's outer resistance.")
            ]);
    }

    private static CombatCounterRuleSet CreateCurrentMagicSound()
    {
        var catalog = VerifiedCombatEffectCatalogs.CurrentAntiMagic;
        string[] mind =
        [
            "POSITIVE_MAGIC_SOUND_MIND_DAMAGE",
            "DISTRACTION_MARK_ACCUMULATION",
            "MIND_RESONANCE_CASCADE"
        ];
        string[] direct = [.. mind, "DIRECT_PRACTICE_PHASE_COVERAGE"];
        string[] movement =
        [
            "TARGET_MOVEMENT_RANGE_PRESSURE",
            "TARGET_CAST_SPEED_PRESSURE"
        ];
        return new CombatCounterRuleSet(
            catalog.GameDataVersion,
            [
                CurrentCounter(
                    "CURRENT_REVERSE_604_SUPPRESSION",
                    direct,
                    CombatCounterStrength.HardCounter,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 604, PracticeDirection.Reverse, 1064),
                    ActiveAttackRequirements(604, 9, 100,
                        "USABLE_BLADE_TRICKS")),
                CurrentCounter(
                    "CURRENT_REVERSE_686_RECOVERY",
                    [.. mind, "DIRECT_PRACTICE_PHASE_COVERAGE"],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 686, PracticeDirection.Reverse, 1422),
                    ActiveAttackRequirements(686, 6, 80,
                        "USABLE_WHISK_TRICKS")),
                CurrentCounter(
                    "CURRENT_REVERSE_602_RECOVERY_CONTROL",
                    [.. direct, .. movement],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 602, PracticeDirection.Reverse, 1062),
                    ActiveAttackRequirements(602, 9, 80,
                        "USABLE_BLADE_TRICKS")),
                CurrentCounter(
                    "CURRENT_REVERSE_616_RECOVERY_PRESSURE",
                    direct,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 616, PracticeDirection.Reverse, 1251),
                    ActiveAttackRequirements(616, 9, 60,
                        "USABLE_BLADE_TRICKS")),
                CurrentCounter(
                    "CURRENT_REVERSE_599_RECOVERY_TRICKS",
                    direct,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 599, PracticeDirection.Reverse, 1059),
                    ActiveAttackRequirements(599, 9, 60,
                        "USABLE_BLADE_TRICKS")),
                CurrentCounter(
                    "CURRENT_REVERSE_134_RESONANCE",
                    ["MIND_RESONANCE_CASCADE"],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAgility,
                    Effect(catalog, 134, PracticeDirection.Reverse, 973),
                    [ActiveAgility(134)]),
                CurrentCounter(
                    "CURRENT_REVERSE_150_WEAPON_PARRY",
                    movement,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAgility,
                    Effect(catalog, 150, PracticeDirection.Reverse, 989),
                    [ActiveAgility(150)]),
                CurrentCounter(
                    "CURRENT_REVERSE_151_CAST_SPEED_CONTROL",
                    ["TARGET_CAST_SPEED_PRESSURE"],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAgility,
                    Effect(catalog, 151, PracticeDirection.Reverse, 990),
                    [ActiveAgility(151)]),
                CurrentCounter(
                    "CURRENT_DIRECT_147_LONG_RANGE_HIT_CONTROL",
                    movement,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAgility,
                    Effect(catalog, 147, PracticeDirection.Direct, 260),
                    [
                        ActiveAgility(147),
                        new RangeRequirement(
                            5,
                            null,
                            CombatRequirementCriticality.Hard,
                            "E8-F01:DIRECT_147_RANGE")
                    ]),
                CurrentCounter(
                    "CURRENT_DIRECT_148_ADVANCE_COUNTER",
                    movement,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAgility,
                    Effect(catalog, 148, PracticeDirection.Direct, 261),
                    [
                        ActiveAgility(148),
                        Manual("USABLE_WEAPON_ATTACK",
                            "E8-F01:DIRECT_148_WEAPON")
                    ]),
                CurrentCounter(
                    "CURRENT_REVERSE_295_HINDRANCE_DEFENSE",
                    mind,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveDefense,
                    Effect(catalog, 295, PracticeDirection.Reverse, 919),
                    [
                        ActiveDefense(295,
                            "E8-F01:REVERSE_295_ACTIVE_DEFENSE"),
                        new ResourceRequirement(
                            CombatResourceKind.DefenseTrueQi,
                            3,
                            CombatRequirementCriticality.Hard,
                            "E8-F01:REVERSE_295_DEFENSE_TRUE_QI")
                    ]),
                CurrentCounter(
                    "CURRENT_REVERSE_303_MIND_MARK_CONVERSION",
                    mind,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveDefense,
                    Effect(catalog, 303, PracticeDirection.Reverse, 927),
                    [ActiveDefense(303,
                        "E8-F01:REVERSE_303_ACTIVE_DEFENSE")]),
                CurrentCounter(
                    "CURRENT_DIRECT_2_DAMAGE_REDUCTION",
                    [.. mind, .. movement],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveDefense,
                    Effect(catalog, 2, PracticeDirection.Direct, 1739),
                    [ActiveDefense(2,
                        "E8-F01:DIRECT_2_ACTIVE_DEFENSE")]),
                CurrentCounter(
                    "CURRENT_DIRECT_289_COUNTER_PRESSURE",
                    movement,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveDefense,
                    Effect(catalog, 289, PracticeDirection.Direct, 187),
                    [
                        ActiveDefense(289,
                            "E8-F01:DIRECT_289_ACTIVE_DEFENSE"),
                        Manual("SUCCESSFUL_WEAPON_COUNTER",
                            "E8-F01:DIRECT_289_COUNTER")
                    ]),
                CurrentCounter(
                    "CURRENT_DIRECT_267_MARK_DURATION",
                    ["DISTRACTION_MARK_ACCUMULATION",
                        "MIND_RESONANCE_CASCADE"],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.EquippedPassive,
                    Effect(catalog, 267, PracticeDirection.Direct, 165),
                    [Passive(267, "current-direct-267-equipped")]),
                CurrentCounter(
                    "CURRENT_REVERSE_265_MIND_DEFENSE",
                    mind,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.EquippedPassive,
                    Effect(catalog, 265, PracticeDirection.Reverse, 889),
                    [
                        Passive(265, "current-reverse-265-equipped"),
                        Manual("CHARM_INPUT_AVAILABLE",
                            "E8-F01:REVERSE_265_CHARM")
                    ]),
                CurrentCounter(
                    "CURRENT_REVERSE_280_CLOSE_AVOIDANCE",
                    [.. mind, .. movement],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.EquippedPassive,
                    Effect(catalog, 280, PracticeDirection.Reverse, 904),
                    [
                        Passive(280, "current-reverse-280-equipped"),
                        new RangeRequirement(
                            null,
                            4,
                            CombatRequirementCriticality.Hard,
                            "E8-F01:REVERSE_280_RANGE")
                    ]),
                CurrentCounter(
                    "CURRENT_DIRECT_252_MOBILITY_SUSTAIN",
                    movement,
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.EquippedPassive,
                    Effect(catalog, 252, PracticeDirection.Direct, 150),
                    [Passive(252, "current-direct-252-equipped")]),
                CurrentCounter(
                    "CURRENT_REVERSE_624_POWER_REDUCTION",
                    ["POSITIVE_MAGIC_SOUND_MIND_DAMAGE",
                        "DISTRACTION_MARK_ACCUMULATION"],
                    CombatCounterStrength.Mitigation,
                    CombatCounterActivationTiming.ActiveAttack,
                    Effect(catalog, 624, PracticeDirection.Reverse, 1234),
                    ActiveAttackRequirements(624, 9, 80,
                        "USABLE_BLADE_TRICKS"))
            ]);
    }

    private static CombatCounterRule CurrentCounter(
        string code,
        IEnumerable<string> goals,
        CombatCounterStrength strength,
        CombatCounterActivationTiming activation,
        CombatEffectCatalogEntry effect,
        IEnumerable<CombatRequirement> requirements) => new(
        code,
        goals,
        strength,
        activation,
        effect,
        requirements,
        "Exact current-version role contract; live conditional values remain "
        + "requirements rather than inferred facts.");

    private static CombatRequirement[] ActiveAttackRequirements(
        int skillId,
        int weaponSubtype,
        int stanceBreathCost,
        string trickCode) =>
    [
        new WeaponRequirement(
            weaponSubtype,
            CombatRequirementCriticality.Hard,
            $"E8-F01:SKILL_{skillId}:WEAPON"),
        new ResourceRequirement(
            CombatResourceKind.Stance,
            stanceBreathCost,
            CombatRequirementCriticality.Hard,
            $"E8-F01:SKILL_{skillId}:STANCE"),
        new ResourceRequirement(
            CombatResourceKind.Breath,
            stanceBreathCost,
            CombatRequirementCriticality.Hard,
            $"E8-F01:SKILL_{skillId}:BREATH"),
        Manual(trickCode, $"E8-F01:SKILL_{skillId}:TRICKS")
    ];

    private static SkillActivationRequirement ActiveAgility(int skillId) =>
        new(
            skillId,
            SkillActivationState.ActiveAgility,
            CombatRequirementCriticality.Hard,
            $"E8-F01:SKILL_{skillId}:ACTIVE_AGILITY");

    private static ManualConfirmationRequirement Manual(
        string code,
        string evidence) => new(
        code,
        CombatRequirementCriticality.Hard,
        evidence);

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

    private static SkillActivationRequirement ActiveDefense(
        int skillId,
        string evidenceName) => new(
            skillId,
            SkillActivationState.ActiveDefense,
            CombatRequirementCriticality.Hard,
            $"local-rule:{evidenceName}");

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
