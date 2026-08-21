using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatThreats;

public static class VerifiedExactTargetEncounterRuleSets
{
    public const string CurrentGameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";

    public const int LaterMagicSoundTargetTemplateId = 719;

    public static ExactTargetEncounterPhaseRuleSet CurrentLaterMagicSound
    { get; } = CreateCurrentLaterMagicSound();

    private static ExactTargetEncounterPhaseRuleSet
        CreateCurrentLaterMagicSound()
    {
        var config = Evidence(
            TargetEncounterEvidenceSource.InstalledConfiguration,
            "E8-F02-CURRENT-INSTALLED-CONFIG");
        var runtime = Evidence(
            TargetEncounterEvidenceSource.RuntimeBehavior,
            "E8-F02-CURRENT-RUNTIME-BEHAVIOR");
        var save = Evidence(
            TargetEncounterEvidenceSource.SavedEquippedLoadout,
            "E8-F02-SANITIZED-LATER-PHASE-SNAPSHOT");
        var global = Evidence(
            TargetEncounterEvidenceSource.VerifiedGlobalRule,
            "E8-F02-CURRENT-GLOBAL-MIND-CHAIN");

        TargetThreatSkillSignature[] expected =
        [
            Direct(54, 47), Direct(55, 48), Direct(56, 49),
            Direct(57, 50), Direct(58, 51), Direct(59, 52),
            Direct(60, 53), Direct(61, 54), Direct(62, 55),
            Direct(157, 270), Direct(158, 271), Direct(159, 272),
            Direct(160, 273), Direct(161, 274), Direct(162, 275),
            Direct(163, 276), Direct(164, 277), Direct(165, 278),
            Direct(265, 163), Direct(266, 164), Direct(267, 165),
            Direct(268, 166), Direct(269, 167), Direct(270, 168),
            Direct(271, 169), Direct(440, 366), Direct(443, 369),
            Direct(446, 372), Direct(726, 350), Direct(727, 351),
            Direct(728, 352), Direct(729, 353), Direct(732, 356),
            Direct(733, 357)
        ];
        int[] magic = [726, 727, 728, 729, 732, 733];
        int[] agility = [157, 158, 159, 160, 161, 162, 163, 164, 165];
        int[] innerPower = [54, 55, 56, 57, 58, 59, 60, 61, 62];
        var facts = new[]
        {
            Fact("EXACT_LATER_PHASE_BOUND",
                TargetEncounterFactKind.EncounterPhase,
                TargetEncounterFactState.Confirmed, [],
                "STABLE_TEMPLATE_AND_COMPLETE_LOADOUT_REQUIRED", save),
            Fact("EQUIPPED_DIRECT_PRACTICE_SET",
                TargetEncounterFactKind.DirectPracticeCoverage,
                TargetEncounterFactState.Confirmed,
                expected.Select(item => item.SkillId),
                "CURRENT_ACTIVE_CAST_STILL_MANUAL", save, config),
            Fact("DIRECT_MAGIC_SOUND_CAST_SET",
                TargetEncounterFactKind.MagicSoundCastSet,
                TargetEncounterFactState.Confirmed, magic,
                "SIX_EXACT_ATTACKS_ONLY", save, config, runtime),
            Fact("DIRECT_MAGIC_SOUND_MIND_DAMAGE",
                TargetEncounterFactKind.MindDamagePressure,
                TargetEncounterFactState.Confirmed, magic,
                "HIT_STRENGTH_AND_FREQUENCY_NOT_PREDICTED", config, runtime),
            Fact("MIND_DAMAGE_TO_DISTRACTION_MARK",
                TargetEncounterFactKind.DistractionMarkAccumulation,
                TargetEncounterFactState.Confirmed, magic,
                "LIVE_THRESHOLD_NOT_IN_IMMUTABLE_SNAPSHOT", global, runtime),
            Fact("MIND_RHYTHM_COUNTDOWN",
                TargetEncounterFactKind.MindRhythmCountdown,
                TargetEncounterFactState.Confirmed, [],
                "LIVE_COUNT_REQUIRES_BATTLE_OBSERVATION", global, runtime),
            Fact("MIND_UPHEAVAL_CASCADE",
                TargetEncounterFactKind.MindUpheavalCascade,
                TargetEncounterFactState.Confirmed, [],
                "LIVE_DURATION_REQUIRES_BATTLE_OBSERVATION", global, runtime),
            Fact("DEFEAT_MARK_RESET_NOT_PRESENT",
                TargetEncounterFactKind.DefeatMarkReset,
                TargetEncounterFactState.NotPresent, [287],
                "DO_NOT_IMPORT_HISTORICAL_RESET_ASSUMPTION", save, config,
                runtime),
            Fact("REVERSE_604_FULL_DIRECT_COVERAGE",
                TargetEncounterFactKind.ReverseSuppressionApplicability,
                TargetEncounterFactState.Confirmed,
                expected.Select(item => item.SkillId),
                "PLAYER_CAST_FEASIBILITY_IS_SEPARATE", save, config, runtime),
            Fact("TARGET_INNER_POWER_SKILL_SET",
                TargetEncounterFactKind.InnerPowerSkillSet,
                TargetEncounterFactState.Confirmed, innerPower,
                "ACTIVE_INNER_POWER_STATE_NOT_PERSISTED", save, config),
            Manual("TARGET_ACTIVE_INNER_POWER_STATE",
                TargetEncounterFactKind.ActiveInnerPowerState,
                "CURRENT_INNER_POWER_REQUIRES_SCREEN", save),
            Fact("TARGET_AGILITY_SKILL_SET",
                TargetEncounterFactKind.AgilitySkillSet,
                TargetEncounterFactState.Confirmed, agility,
                "ONE_ACTIVE_AGILITY_AT_A_TIME", save, config, runtime),
            Fact("TARGET_FOOTWORK_SUSTAIN",
                TargetEncounterFactKind.FootworkSustain,
                TargetEncounterFactState.Confirmed, [157, 269],
                "ACTIVE_AGILITY_AND_EQUIPPED_PASSIVE_DIFFER", config, runtime),
            Fact("TARGET_FORWARD_DISTANCE_BURST",
                TargetEncounterFactKind.MovementPressure,
                TargetEncounterFactState.Confirmed, [161, 165],
                "DIRECTION_AND_ACTIVE_AGILITY_REQUIRED", config, runtime),
            Fact("TARGET_IN_RANGE_MOVEMENT_PRESSURE",
                TargetEncounterFactKind.RangePressure,
                TargetEncounterFactState.Confirmed, [164, 165],
                "ONLY_WHILE_INSIDE_ATTACK_RANGE", config, runtime),
            Fact("TARGET_CAST_SPEED_PRESSURE",
                TargetEncounterFactKind.SpeedPressure,
                TargetEncounterFactState.Confirmed, [160],
                "ONLY_WHILE_EXACT_AGILITY_ACTIVE", config, runtime),
            Fact("TARGET_QUICKNESS_PROTECTION",
                TargetEncounterFactKind.SpeedPressure,
                TargetEncounterFactState.Confirmed, [163],
                "ONLY_WHILE_EXACT_AGILITY_ACTIVE", config, runtime),
            Fact("TARGET_CLOSE_RANGE_PRESSURE_CONDITIONAL",
                TargetEncounterFactKind.CloseRangePressure,
                TargetEncounterFactState.Confirmed, [164, 165],
                "CURRENT_DISTANCE_AND_RANGE_REQUIRE_MANUAL_CONFIRMATION",
                config, runtime),
            Manual("LIVE_MIND_MARK_COUNT",
                TargetEncounterFactKind.LiveMarkCount,
                "BATTLE_ONLY_VALUE", global),
            Manual("LIVE_MIND_RHYTHM_COUNT",
                TargetEncounterFactKind.LiveRhythmCount,
                "BATTLE_ONLY_VALUE", global),
            Manual("LIVE_TEMPORARY_EFFECT_LAYERS",
                TargetEncounterFactKind.LiveTemporaryLayers,
                "BATTLE_ONLY_VALUE", runtime),
            Manual("CURRENT_DISTANCE",
                TargetEncounterFactKind.CurrentDistance,
                "BATTLE_ONLY_VALUE", runtime),
            Manual("CURRENT_RESOURCE_STATE",
                TargetEncounterFactKind.CurrentResourceState,
                "BATTLE_ONLY_VALUE", runtime),
            Manual("TARGET_ACTIVE_AGILITY",
                TargetEncounterFactKind.ActiveAgility,
                "BATTLE_ONLY_VALUE", save, runtime)
        };
        var transitions = new[]
        {
            Transition("DIRECT_MAGIC_CAST_APPLIES_MIND_DAMAGE",
                TargetEncounterTransitionState.Verified,
                TargetEncounterTransitionTiming.OnHit,
                ["DIRECT_MAGIC_SOUND_CAST_SET"],
                ["DIRECT_MAGIC_SOUND_MIND_DAMAGE"],
                "NO_HIT_OR_STRENGTH_PREDICTION", config, runtime),
            Transition("MIND_DAMAGE_THRESHOLD_ADDS_DISTRACTION_MARK",
                TargetEncounterTransitionState.Verified,
                TargetEncounterTransitionTiming.OnHit,
                ["DIRECT_MAGIC_SOUND_MIND_DAMAGE"],
                ["MIND_DAMAGE_TO_DISTRACTION_MARK"],
                "LIVE_THRESHOLD_NOT_PREDICTED", global, runtime),
            Transition("FIRST_DISTRACTION_MARK_STARTS_RHYTHM",
                TargetEncounterTransitionState.Verified,
                TargetEncounterTransitionTiming.OnMarkApplied,
                ["MIND_DAMAGE_TO_DISTRACTION_MARK"],
                ["MIND_RHYTHM_COUNTDOWN"],
                "LIVE_COUNT_REQUIRES_OBSERVATION", global, runtime),
            Transition("LATER_DISTRACTION_MARK_REDUCES_RHYTHM",
                TargetEncounterTransitionState.Verified,
                TargetEncounterTransitionTiming.OnMarkApplied,
                ["MIND_DAMAGE_TO_DISTRACTION_MARK",
                    "MIND_RHYTHM_COUNTDOWN"],
                ["MIND_RHYTHM_COUNTDOWN"],
                "NO_ELAPSED_TIME_SIMULATION", global, runtime),
            Transition("RHYTHM_ZERO_STARTS_UPHEAVAL",
                TargetEncounterTransitionState.Verified,
                TargetEncounterTransitionTiming.OnCountdownZero,
                ["MIND_RHYTHM_COUNTDOWN"],
                ["MIND_UPHEAVAL_CASCADE"],
                "LIVE_ZERO_REQUIRES_OBSERVATION", global, runtime),
            Transition("UPHEAVAL_REPEATS_MIND_PRESSURE",
                TargetEncounterTransitionState.Verified,
                TargetEncounterTransitionTiming.OnCountdownZero,
                ["MIND_UPHEAVAL_CASCADE"],
                ["DIRECT_MAGIC_SOUND_MIND_DAMAGE"],
                "NO_DURATION_OR_TICK_PREDICTION", global, runtime),
            Transition("DEFEAT_THRESHOLD_RESET_IS_NOT_APPLICABLE",
                TargetEncounterTransitionState.NotApplicable,
                TargetEncounterTransitionTiming.OnDefeatThreshold,
                ["DEFEAT_MARK_RESET_NOT_PRESENT"],
                ["DEFEAT_MARK_RESET_NOT_PRESENT"],
                "EXACT_PHASE_LACKS_RESET_SIGNATURE", save, config, runtime),
            Transition("REVERSE_604_SUPPRESSES_PHASE_DIRECT_PRACTICE",
                TargetEncounterTransitionState.Verified,
                TargetEncounterTransitionTiming.DuringCast,
                ["EQUIPPED_DIRECT_PRACTICE_SET"],
                ["REVERSE_604_FULL_DIRECT_COVERAGE"],
                "PLAYER_DIRECTION_WEAPON_AND_CAST_MUST_BE_FEASIBLE",
                save, config, runtime),
            Transition("ACTIVE_AGILITY_CHANGES_DISTANCE_PRESSURE",
                TargetEncounterTransitionState.ManualObservationRequired,
                TargetEncounterTransitionTiming.OnDistanceChanged,
                ["TARGET_AGILITY_SKILL_SET", "TARGET_ACTIVE_AGILITY",
                    "CURRENT_DISTANCE"],
                ["TARGET_FORWARD_DISTANCE_BURST",
                    "TARGET_IN_RANGE_MOVEMENT_PRESSURE"],
                "ACTIVE_AGILITY_AND_DISTANCE_MUST_BE_OBSERVED",
                config, runtime),
            Transition("ACTIVE_AGILITY_CHANGES_SPEED_PRESSURE",
                TargetEncounterTransitionState.ManualObservationRequired,
                TargetEncounterTransitionTiming.WhileAgilityActive,
                ["TARGET_AGILITY_SKILL_SET", "TARGET_ACTIVE_AGILITY"],
                ["TARGET_CAST_SPEED_PRESSURE",
                    "TARGET_QUICKNESS_PROTECTION"],
                "ACTIVE_AGILITY_MUST_BE_OBSERVED", config, runtime),
            Transition("IN_RANGE_MOVEMENT_APPLIES_CLOSE_PRESSURE",
                TargetEncounterTransitionState.ManualObservationRequired,
                TargetEncounterTransitionTiming.OnDistanceChanged,
                ["TARGET_IN_RANGE_MOVEMENT_PRESSURE", "CURRENT_DISTANCE"],
                ["TARGET_CLOSE_RANGE_PRESSURE_CONDITIONAL"],
                "CURRENT_ATTACK_RANGE_MUST_BE_OBSERVED", config, runtime)
        };

        return new ExactTargetEncounterPhaseRuleSet(
            CurrentGameDataVersion,
            LaterMagicSoundTargetTemplateId,
            "LATER_MAGIC_SOUND_PHASE",
            expected,
            magic,
            facts,
            transitions);
    }

    private static TargetThreatSkillSignature Direct(
        int skillId,
        int effectId) => new(
            skillId,
            PracticeDirection.Direct,
            effectId);

    private static TargetEncounterEvidence Evidence(
        TargetEncounterEvidenceSource source,
        string reference) => new(
            source,
            reference,
            CurrentGameDataVersion);

    private static TargetEncounterFact Fact(
        string code,
        TargetEncounterFactKind kind,
        TargetEncounterFactState state,
        IEnumerable<int> skillIds,
        string limitation,
        params TargetEncounterEvidence[] evidence) => new(
            code,
            kind,
            state,
            skillIds,
            limitation,
            evidence);

    private static TargetEncounterFact Manual(
        string code,
        TargetEncounterFactKind kind,
        string limitation,
        params TargetEncounterEvidence[] evidence) => Fact(
            code,
            kind,
            TargetEncounterFactState.ManualObservationRequired,
            [],
            limitation,
            evidence);

    private static TargetEncounterTransition Transition(
        string code,
        TargetEncounterTransitionState state,
        TargetEncounterTransitionTiming timing,
        IEnumerable<string> triggers,
        IEnumerable<string> results,
        string limitation,
        params TargetEncounterEvidence[] evidence) => new(
            code,
            state,
            timing,
            triggers,
            results,
            limitation,
            evidence);
}
