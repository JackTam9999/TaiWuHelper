using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public static class VerifiedTacticalCombatRuleSets
{
    public const string HistoricalGameDataVersion =
        VerifiedCombatEffectCatalogs.GoldenGameDataVersion;

    public const string RuleVersion = "TACTICAL_COMBAT_RULES@1.0.0";

    public static TacticalSemanticVersion InitialSemanticVersion { get; } =
        new(1, 0, 0);

    public static TacticalCombatRuleSet HistoricalMagicSound { get; } =
        CreateHistoricalMagicSound();

    public static TacticalCombatRuleSet CurrentLaterMagicSound { get; } =
        CreateCurrentLaterMagicSound();

    public static TacticalCombatRuleResolution ResolveExact(
        string gameDataVersion,
        IEnumerable<string> targetGoalCodes,
        IEnumerable<TacticalRuleEvidenceObservation> evidence) =>
        string.Equals(
            gameDataVersion,
            CurrentLaterMagicSound.SupportedGameDataVersions[0],
            StringComparison.Ordinal)
            ? CurrentLaterMagicSound.Resolve(
                gameDataVersion,
                targetGoalCodes,
                evidence)
            : HistoricalMagicSound.Resolve(
                gameDataVersion,
                targetGoalCodes,
                evidence);

    private const string MindPressure =
        "POSITIVE_MAGIC_SOUND_MIND_DAMAGE";
    private const string Distraction =
        "DISTRACTION_MARK_ACCUMULATION";
    private const string Resonance = "MIND_RESONANCE_CASCADE";
    private const string Reset = "DEFEAT_MARK_RESET_LOOP";
    private const string DirectCoverage =
        "DIRECT_PRACTICE_PHASE_COVERAGE";
    private const string Movement = "TARGET_MOVEMENT_RANGE_PRESSURE";
    private const string Speed = "TARGET_CAST_SPEED_PRESSURE";

    private static TacticalCombatRuleSet CreateHistoricalMagicSound()
    {
        var evidence = Evidence();
        var transitions = new[]
        {
            Transition(
                "DIRECT_MAGIC_CAST_CREATES_MIND_PRESSURE",
                TacticalRulePurpose.DirectMagicMindPressure,
                TacticalTransitionTiming.DuringCast,
                [Fact(TacticalFactKind.TargetSkillPhase,
                    "TARGET_DIRECT_MAGIC_CAST_ACTIVE")],
                [Fact(TacticalFactKind.Other,
                    "PLAYER_POSITIVE_MAGIC_MIND_PRESSURE")],
                [MindPressure],
                [
                    Exact("TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE"),
                    Broad("MAGIC_SOUND_DIRECT_EFFECT_VERIFIED")
                ],
                "NO_STRENGTH_FREQUENCY_OR_HIT_INFERENCE",
                evidence),
            Transition(
                "MIND_PRESSURE_CREATES_DISTRACTION_MARKS",
                TacticalRulePurpose.DistractionMarkAccumulation,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Other,
                    "PLAYER_POSITIVE_MAGIC_MIND_PRESSURE")],
                [Fact(TacticalFactKind.Mark,
                    "PLAYER_DISTRACTION_MARK_PRESENT")],
                [Distraction, Resonance],
                [
                    Exact("TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE"),
                    Broad("MAGIC_SOUND_DIRECT_EFFECT_VERIFIED"),
                    Broad("MIND_LOSS_TO_DISTRACTION_VERIFIED")
                ],
                "NO_MARK_TIMING_PREDICTION",
                evidence),
            Transition(
                "FIRST_MARK_STARTS_RESONANCE_COUNTDOWN",
                TacticalRulePurpose.MindResonanceCountdown,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Mark,
                    "PLAYER_DISTRACTION_MARK_PRESENT")],
                [Fact(TacticalFactKind.Resonance,
                    "PLAYER_MIND_RESONANCE_COUNTDOWN_ACTIVE")],
                [Resonance],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("FIRST_MARK_RESONANCE_VERIFIED")
                ],
                "LIVE_COUNT_MUST_BE_CONFIRMED",
                evidence),
            Transition(
                "RESONANCE_ZERO_STARTS_CASCADE",
                TacticalRulePurpose.MindResonanceCascade,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Resonance,
                    "PLAYER_MIND_RESONANCE_COUNTDOWN_ZERO")],
                [Fact(TacticalFactKind.Resonance,
                    "PLAYER_MIND_RESONANCE_CASCADE_ACTIVE")],
                [Resonance],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("RESONANCE_ZERO_CASCADE_VERIFIED")
                ],
                "NO_ELAPSED_TIME_SIMULATION",
                evidence),
            Transition(
                "DEFEAT_THRESHOLD_CAN_TRIGGER_RESET",
                TacticalRulePurpose.DefeatMarkReset,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Mark,
                    "TARGET_DEFEAT_THRESHOLD_REACHED")],
                [Fact(TacticalFactKind.Mark,
                    "TARGET_DEFEAT_MARKS_CLEARED")],
                [Reset],
                [
                    Exact("TARGET_RESET_SIGNATURE_APPLICABLE"),
                    Broad("DEFEAT_MARK_RESET_VERIFIED")
                ],
                "LIVE_QIQIAO_AND_NEXT_RESET_COST_UNAVAILABLE",
                evidence),
            Transition(
                "REVERSE_604_SUPPRESSES_DIRECT_CAST",
                TacticalRulePurpose.CastSuppression,
                TacticalTransitionTiming.DuringCast,
                [Fact(TacticalFactKind.TargetSkillPhase,
                    "TARGET_DIRECT_MAGIC_CAST_ACTIVE")],
                [Fact(TacticalFactKind.TargetSkillPhase,
                    "TARGET_DIRECT_PRACTICE_SUPPRESSED")],
                [MindPressure, Distraction, Resonance],
                [
                    Exact("TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE"),
                    Broad("REVERSE_604_EFFECT_VERIFIED")
                ],
                "EXACT_REVERSE_DIRECTION_AND_FEASIBLE_CAST_REQUIRED",
                evidence),
            Transition(
                "REVERSE_604_APPLIES_DIRECT_PRACTICE_LOCK",
                TacticalRulePurpose.DirectPracticeSelfLock,
                TacticalTransitionTiming.AfterCast,
                [Fact(TacticalFactKind.PlayerReadiness,
                    "PLAYER_REVERSE_604_CAST_COMPLETED")],
                [Fact(TacticalFactKind.TemporaryLockout,
                    "PLAYER_DIRECT_PRACTICE_LOCK_THREE_LAYERS")],
                [MindPressure, Distraction, Resonance],
                [Broad("REVERSE_604_EFFECT_VERIFIED")],
                "DIRECT_PRACTICE_UNAVAILABLE_WHILE_LAYER_REMAINS",
                evidence),
            Transition(
                "FEASIBLE_REVERSE_CAST_REDUCES_LOCK_LAYER",
                TacticalRulePurpose.DirectPracticeLockRecovery,
                TacticalTransitionTiming.AfterManualAction,
                [Fact(TacticalFactKind.PlayerReadiness,
                    "PLAYER_FEASIBLE_REVERSE_CAST_COMPLETED")],
                [Fact(TacticalFactKind.TemporaryLockout,
                    "PLAYER_DIRECT_PRACTICE_LOCK_ONE_LAYER_REMOVED")],
                [MindPressure, Distraction, Resonance],
                [Broad("REVERSE_CAST_LOCK_RECOVERY_VERIFIED")],
                "THREE_EXACT_EXECUTABLE_CASTS_NOT_PRESELECTED",
                evidence),
            Transition(
                "REVERSE_686_REMOVES_HINDRANCE_MARK",
                TacticalRulePurpose.HindranceMarkRemoval,
                TacticalTransitionTiming.CombatStart,
                [Fact(TacticalFactKind.Mark,
                    "PLAYER_HINDRANCE_MARK_THRESHOLD_EXCEEDED")],
                [Fact(TacticalFactKind.Mark,
                    "PLAYER_HINDRANCE_MARK_REMOVED")],
                [Distraction, Resonance],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("REVERSE_686_EFFECT_VERIFIED")
                ],
                "FINITE_LAYER_POOL_AND_THRESHOLD_REQUIRED",
                evidence),
            Transition(
                "REVERSE_134_SHORTENS_RESONANCE_DURATION",
                TacticalRulePurpose.ResonanceDurationReduction,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.ActiveRole,
                    "PLAYER_REVERSE_134_AGILITY_ACTIVE")],
                [Fact(TacticalFactKind.Resonance,
                    "PLAYER_RESONANCE_DURATION_REDUCED")],
                [Resonance],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("REVERSE_134_EFFECT_VERIFIED")
                ],
                "APPLIES_ONLY_WHILE_EXACT_AGILITY_IS_ACTIVE",
                evidence),
            Transition(
                "DIRECT_267_SHORTENS_DISTRACTION_DURATION",
                TacticalRulePurpose.MarkDurationReduction,
                TacticalTransitionTiming.BeforeCombat,
                [Fact(TacticalFactKind.Equipment,
                    "PLAYER_DIRECT_267_EQUIPPED")],
                [Fact(TacticalFactKind.Mark,
                    "PLAYER_DISTRACTION_DURATION_REDUCED")],
                [Distraction, Resonance],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("DIRECT_267_EFFECT_VERIFIED")
                ],
                "EXACT_DIRECT_DIRECTION_AND_EQUIPMENT_REQUIRED",
                evidence),
            Transition(
                "REVERSE_624_REDUCES_ATTACK_POWER",
                TacticalRulePurpose.EnemyAttackPowerReduction,
                TacticalTransitionTiming.AfterCast,
                [Fact(TacticalFactKind.PlayerReadiness,
                    "PLAYER_REVERSE_624_CAST_COMPLETED")],
                [Fact(TacticalFactKind.Other,
                    "TARGET_ATTACK_SKILL_POWER_REDUCED")],
                [MindPressure, Distraction],
                [
                    Exact("TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE"),
                    Broad("REVERSE_624_EFFECT_VERIFIED")
                ],
                "REDUCTION_DEPENDS_ON_ACHIEVED_EFFECTIVENESS",
                evidence),
            Transition(
                "REVERSE_291_PRESSURES_RANDOM_TRUE_QI",
                TacticalRulePurpose.ResetResourcePressure,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Other,
                    "TARGET_DAMAGE_STATE_APPLIED")],
                [Fact(TacticalFactKind.Resource,
                    "TARGET_RANDOM_TRUE_QI_DRAIN_ACTIVE")],
                [Reset],
                [
                    Exact("TARGET_RESET_SIGNATURE_APPLICABLE"),
                    Broad("REVERSE_291_EFFECT_VERIFIED")
                ],
                "RANDOM_TRUE_QI_TYPE_DOES_NOT_GUARANTEE_QIQIAO",
                evidence),
            Transition(
                "REVERSE_611_TRANSFERS_HINDRANCE_MARKS",
                TacticalRulePurpose.ConditionalMarkTransfer,
                TacticalTransitionTiming.AfterManualAction,
                [Fact(TacticalFactKind.Equipment,
                    "PLAYER_ELIGIBLE_BLADE_RELEASE_COMPLETED")],
                [Fact(TacticalFactKind.Mark,
                    "TARGET_RECEIVES_TRANSFERRED_HINDRANCE_MARKS")],
                [Distraction],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("REVERSE_611_EFFECT_VERIFIED")
                ],
                "WEAPON_RELEASE_DURABILITY_AND_TRICK_COST_REQUIRED",
                evidence)
        };

        var roles = new[]
        {
            Role(
                TacticalRoleKind.Suppression,
                "REVERSE_604_DIRECT_CAST_SUPPRESSION",
                TacticalRulePurpose.CastSuppression,
                TacticalTransitionTiming.DuringCast,
                604,
                PracticeDirection.Reverse,
                1064,
                [CombatEffectMechanic.SuppressEnemyDirectPractice],
                [MindPressure, Distraction, Resonance],
                [
                    "REVERSE_604_SUPPRESSES_DIRECT_CAST",
                    "REVERSE_604_APPLIES_DIRECT_PRACTICE_LOCK"
                ],
                [
                    Exact("TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE"),
                    Broad("REVERSE_604_EFFECT_VERIFIED")
                ],
                "THREE_LAYER_SELF_LOCK_REQUIRES_RECOVERY",
                evidence,
                "REVERSE_JINNI_SUPPRESSION"),
            Role(
                TacticalRoleKind.Mitigation,
                "REVERSE_686_HINDRANCE_MARK_REMOVAL",
                TacticalRulePurpose.HindranceMarkRemoval,
                TacticalTransitionTiming.CombatStart,
                686,
                PracticeDirection.Reverse,
                1422,
                [CombatEffectMechanic.RemoveOwnHindranceMarks],
                [Distraction, Resonance],
                ["REVERSE_686_REMOVES_HINDRANCE_MARK"],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("REVERSE_686_EFFECT_VERIFIED")
                ],
                "FINITE_LAYER_POOL_AND_DIRECTION_REQUIRED",
                evidence,
                "REVERSE_LAOJUN_MARK_CLEAR"),
            Role(
                TacticalRoleKind.Mitigation,
                "REVERSE_134_RESONANCE_DURATION",
                TacticalRulePurpose.ResonanceDurationReduction,
                TacticalTransitionTiming.OnObservedState,
                134,
                PracticeDirection.Reverse,
                973,
                [CombatEffectMechanic.ShortenOwnMindResonanceDuration],
                [Resonance],
                ["REVERSE_134_SHORTENS_RESONANCE_DURATION"],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("REVERSE_134_EFFECT_VERIFIED")
                ],
                "ACTIVE_AGILITY_REQUIRED",
                evidence,
                "REVERSE_WANHUA_RESONANCE"),
            Role(
                TacticalRoleKind.Mitigation,
                "DIRECT_267_DISTRACTION_DURATION",
                TacticalRulePurpose.MarkDurationReduction,
                TacticalTransitionTiming.BeforeCombat,
                267,
                PracticeDirection.Direct,
                165,
                [CombatEffectMechanic.ShortenOwnDistractionMarkDuration],
                [Distraction, Resonance],
                ["DIRECT_267_SHORTENS_DISTRACTION_DURATION"],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("DIRECT_267_EFFECT_VERIFIED")
                ],
                "EQUIPPED_EXACT_DIRECT_DIRECTION_REQUIRED",
                evidence,
                "DIRECT_MOYU_MARK_DURATION"),
            Role(
                TacticalRoleKind.Mitigation,
                "REVERSE_624_ATTACK_POWER_REDUCTION",
                TacticalRulePurpose.EnemyAttackPowerReduction,
                TacticalTransitionTiming.AfterCast,
                624,
                PracticeDirection.Reverse,
                1234,
                [CombatEffectMechanic.ReduceEnemyAttackSkillPower],
                [MindPressure, Distraction],
                ["REVERSE_624_REDUCES_ATTACK_POWER"],
                [
                    Exact("TARGET_DIRECT_MAGIC_SIGNATURE_ACTIVE"),
                    Broad("REVERSE_624_EFFECT_VERIFIED")
                ],
                "EFFECTIVENESS_AND_FEASIBLE_CAST_REQUIRED",
                evidence,
                "REVERSE_FULONG_POWER_REDUCTION"),
            Role(
                TacticalRoleKind.Mitigation,
                "REVERSE_291_RESET_RESOURCE_PRESSURE",
                TacticalRulePurpose.ResetResourcePressure,
                TacticalTransitionTiming.OnObservedState,
                291,
                PracticeDirection.Reverse,
                915,
                [
                    CombatEffectMechanic.AmplifyEnemyDamageStates,
                    CombatEffectMechanic.DrainEnemyRandomTrueQi
                ],
                [Reset],
                ["REVERSE_291_PRESSURES_RANDOM_TRUE_QI"],
                [
                    Exact("TARGET_RESET_SIGNATURE_APPLICABLE"),
                    Broad("REVERSE_291_EFFECT_VERIFIED")
                ],
                "RANDOM_DRAIN_IS_NOT_RESET_LOCKOUT",
                evidence,
                "REVERSE_QILUN_TRUE_QI_DRAIN"),
            Role(
                TacticalRoleKind.Mitigation,
                "REVERSE_611_CONDITIONAL_MARK_TRANSFER",
                TacticalRulePurpose.ConditionalMarkTransfer,
                TacticalTransitionTiming.AfterManualAction,
                611,
                PracticeDirection.Reverse,
                1165,
                [CombatEffectMechanic.TransferOwnHindranceMarks],
                [Distraction],
                ["REVERSE_611_TRANSFERS_HINDRANCE_MARKS"],
                [
                    Exact("TARGET_MIND_CHAIN_APPLICABLE"),
                    Broad("REVERSE_611_EFFECT_VERIFIED")
                ],
                "CONDITIONAL_WEAPON_RELEASE_NOT_GENERIC_RECOVERY",
                evidence)
        };

        return new TacticalCombatRuleSet(
            InitialSemanticVersion,
            [HistoricalGameDataVersion],
            [MindPressure, Distraction, Resonance, Reset],
            transitions,
            roles);
    }

    private static TacticalCombatRuleSet CreateCurrentLaterMagicSound()
    {
        var evidence = CurrentEvidence();
        var coreExact = CurrentExact("CURRENT_LATER_PHASE_COMPLETE");
        var mindExact = CurrentExact("CURRENT_TARGET_MIND_CHAIN");
        var directExact = CurrentExact(
            "CURRENT_TARGET_FULL_DIRECT_COVERAGE");
        var movementExact = CurrentExact(
            "CURRENT_TARGET_MOVEMENT_PRESSURE");
        var speedExact = CurrentExact("CURRENT_TARGET_SPEED_PRESSURE");
        var transitions = new[]
        {
            CurrentTransition(
                "CURRENT_DIRECT_MAGIC_CAST_CREATES_MIND_PRESSURE",
                TacticalRulePurpose.DirectMagicMindPressure,
                TacticalTransitionTiming.DuringCast,
                [Fact(TacticalFactKind.TargetSkillPhase,
                    "CURRENT_TARGET_DIRECT_MAGIC_CAST_ACTIVE")],
                [Fact(TacticalFactKind.Other,
                    "CURRENT_PLAYER_MAGIC_MIND_PRESSURE")],
                [MindPressure],
                [coreExact, mindExact,
                    CurrentBroad("CURRENT_MAGIC_SOUND_EFFECTS_VERIFIED")],
                "NO_HIT_STRENGTH_OR_FREQUENCY_PREDICTION",
                evidence),
            CurrentTransition(
                "CURRENT_MIND_PRESSURE_CREATES_DISTRACTION_MARK",
                TacticalRulePurpose.DistractionMarkAccumulation,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Other,
                    "CURRENT_PLAYER_MAGIC_MIND_PRESSURE")],
                [Fact(TacticalFactKind.Mark,
                    "CURRENT_PLAYER_DISTRACTION_MARK_PRESENT")],
                [Distraction, Resonance],
                [mindExact,
                    CurrentBroad("CURRENT_MIND_MARK_CHAIN_VERIFIED")],
                "LIVE_MARK_COUNT_REQUIRES_OBSERVATION",
                evidence),
            CurrentTransition(
                "CURRENT_FIRST_MARK_STARTS_MIND_RHYTHM",
                TacticalRulePurpose.MindResonanceCountdown,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Mark,
                    "CURRENT_PLAYER_DISTRACTION_MARK_PRESENT")],
                [Fact(TacticalFactKind.Resonance,
                    "CURRENT_PLAYER_MIND_RHYTHM_ACTIVE")],
                [Resonance],
                [mindExact,
                    CurrentBroad("CURRENT_MIND_RHYTHM_VERIFIED")],
                "LIVE_RHYTHM_COUNT_REQUIRES_OBSERVATION",
                evidence),
            CurrentTransition(
                "CURRENT_MIND_RHYTHM_ZERO_STARTS_UPHEAVAL",
                TacticalRulePurpose.MindResonanceCascade,
                TacticalTransitionTiming.OnObservedState,
                [Fact(TacticalFactKind.Resonance,
                    "CURRENT_PLAYER_MIND_RHYTHM_ZERO")],
                [Fact(TacticalFactKind.Resonance,
                    "CURRENT_PLAYER_MIND_UPHEAVAL_ACTIVE")],
                [Resonance],
                [mindExact,
                    CurrentBroad("CURRENT_MIND_UPHEAVAL_VERIFIED")],
                "NO_ELAPSED_TIME_OR_DURATION_SIMULATION",
                evidence),
            CurrentTransition(
                "CURRENT_REVERSE_604_SUPPRESSES_DIRECT_PRACTICE",
                TacticalRulePurpose.CastSuppression,
                TacticalTransitionTiming.DuringCast,
                [Fact(TacticalFactKind.TargetSkillPhase,
                    "CURRENT_TARGET_DIRECT_PRACTICE_ACTIVE")],
                [Fact(TacticalFactKind.TargetSkillPhase,
                    "CURRENT_TARGET_DIRECT_PRACTICE_SUPPRESSED")],
                [MindPressure, Distraction, Resonance, DirectCoverage],
                [directExact,
                    CurrentBroad("CURRENT_REVERSE_604_EFFECT_VERIFIED")],
                "EXACT_REVERSE_DIRECTION_AND_CAST_REQUIRED",
                evidence),
            CurrentTransition(
                "CURRENT_REVERSE_604_APPLIES_THREE_LAYER_LOCK",
                TacticalRulePurpose.DirectPracticeSelfLock,
                TacticalTransitionTiming.AfterCast,
                [Fact(TacticalFactKind.PlayerReadiness,
                    "CURRENT_PLAYER_REVERSE_604_COMPLETED")],
                [Fact(TacticalFactKind.TemporaryLockout,
                    "CURRENT_PLAYER_DIRECT_LOCK_THREE_LAYERS")],
                [MindPressure, Distraction, Resonance, DirectCoverage],
                [CurrentBroad("CURRENT_REVERSE_604_EFFECT_VERIFIED")],
                "THREE_REVERSE_CASTS_REQUIRED_FOR_RECOVERY",
                evidence),
            CurrentTransition(
                "CURRENT_FEASIBLE_REVERSE_CAST_REMOVES_LOCK_LAYER",
                TacticalRulePurpose.DirectPracticeLockRecovery,
                TacticalTransitionTiming.AfterManualAction,
                [Fact(TacticalFactKind.PlayerReadiness,
                    "CURRENT_PLAYER_FEASIBLE_REVERSE_CAST_COMPLETED")],
                [Fact(TacticalFactKind.TemporaryLockout,
                    "CURRENT_PLAYER_DIRECT_LOCK_LAYER_REMOVED")],
                [MindPressure, Distraction, Resonance, DirectCoverage],
                [CurrentBroad("CURRENT_REVERSE_CAST_RECOVERY_VERIFIED")],
                "WEAPON_TRICK_RESOURCE_AND_DIRECTION_GATES_APPLY",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_134_SHORTENS_MIND_RHYTHM",
                TacticalRulePurpose.ResonanceDurationReduction,
                TacticalFactKind.Resonance,
                "CURRENT_PLAYER_RESONANCE_DURATION_REDUCED",
                [Resonance],
                mindExact,
                "CURRENT_REVERSE_134_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_150_ENABLES_WEAPON_PARRY",
                TacticalRulePurpose.WeaponAttackParry,
                TacticalFactKind.ActiveRole,
                "CURRENT_ENEMY_WEAPON_ATTACK_PARRYABLE",
                [Movement],
                movementExact,
                "CURRENT_REVERSE_150_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_151_REDUCES_CAST_SPEED",
                TacticalRulePurpose.CastSpeedControl,
                TacticalFactKind.ActiveRole,
                "CURRENT_ENEMY_CAST_SPEED_REDUCED",
                [Speed],
                speedExact,
                "CURRENT_REVERSE_151_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_DIRECT_147_REDUCES_LONG_RANGE_HIT",
                TacticalRulePurpose.HitChanceControl,
                TacticalFactKind.Distance,
                "CURRENT_ENEMY_LONG_RANGE_HIT_REDUCED",
                [Movement],
                movementExact,
                "CURRENT_DIRECT_147_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_DIRECT_148_COUNTERS_ADVANCE",
                TacticalRulePurpose.MovementCounterattack,
                TacticalFactKind.Distance,
                "CURRENT_ENEMY_ADVANCE_COUNTERED",
                [Movement],
                movementExact,
                "CURRENT_DIRECT_148_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_295_PROTECTS_AND_REMOVES_MARK",
                TacticalRulePurpose.CriticalInjuryProtection,
                TacticalFactKind.Mark,
                "CURRENT_PLAYER_HINDRANCE_MARK_REMOVABLE",
                [Distraction, Resonance],
                mindExact,
                "CURRENT_REVERSE_295_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_303_CONVERTS_MIND_MARK",
                TacticalRulePurpose.MindMarkConversion,
                TacticalFactKind.Mark,
                "CURRENT_PLAYER_MIND_MARK_CONVERSION_AVAILABLE",
                [Distraction, Resonance],
                mindExact,
                "CURRENT_REVERSE_303_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_DIRECT_2_REDUCES_DIRECT_DAMAGE",
                TacticalRulePurpose.DirectDamageReduction,
                TacticalFactKind.ActiveRole,
                "CURRENT_PLAYER_DIRECT_DAMAGE_REDUCED",
                [MindPressure, Movement],
                coreExact,
                "CURRENT_DIRECT_2_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_DIRECT_289_APPLIES_STANCE_PRESSURE",
                TacticalRulePurpose.CounterStancePressure,
                TacticalFactKind.Resource,
                "CURRENT_ENEMY_STANCE_RECOVERY_REDUCED",
                [Movement],
                movementExact,
                "CURRENT_DIRECT_289_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_DIRECT_267_SHORTENS_MARK_DURATION",
                TacticalRulePurpose.MarkDurationReduction,
                TacticalFactKind.Mark,
                "CURRENT_PLAYER_DISTRACTION_DURATION_REDUCED",
                [Distraction, Resonance],
                mindExact,
                "CURRENT_DIRECT_267_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_265_INCREASES_MIND_DEFENSE",
                TacticalRulePurpose.MindDefenseIncrease,
                TacticalFactKind.Other,
                "CURRENT_PLAYER_MIND_DEFENSE_INCREASED",
                [MindPressure, Distraction],
                mindExact,
                "CURRENT_REVERSE_265_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_280_INCREASES_CLOSE_AVOIDANCE",
                TacticalRulePurpose.CloseRangeAvoidance,
                TacticalFactKind.Distance,
                "CURRENT_PLAYER_CLOSE_AVOIDANCE_INCREASED",
                [MindPressure, Movement],
                movementExact,
                "CURRENT_REVERSE_280_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_DIRECT_252_RESTORES_MOBILITY",
                TacticalRulePurpose.MobilitySustain,
                TacticalFactKind.Resource,
                "CURRENT_PLAYER_MOBILITY_RESTORED",
                [Movement],
                movementExact,
                "CURRENT_DIRECT_252_EFFECT_VERIFIED",
                evidence),
            CurrentRoleTransition(
                "CURRENT_REVERSE_624_REDUCES_ATTACK_POWER",
                TacticalRulePurpose.EnemyAttackPowerReduction,
                TacticalFactKind.Other,
                "CURRENT_TARGET_ATTACK_POWER_REDUCED",
                [MindPressure, Distraction],
                coreExact,
                "CURRENT_REVERSE_624_EFFECT_VERIFIED",
                evidence)
        };

        var recoveryTransition =
            "CURRENT_FEASIBLE_REVERSE_CAST_REMOVES_LOCK_LAYER";
        var roles = new[]
        {
            CurrentRole(
                TacticalRoleKind.Suppression,
                "CURRENT_REVERSE_604_DIRECT_SUPPRESSION",
                TacticalRulePurpose.CastSuppression,
                TacticalTransitionTiming.DuringCast,
                604,
                PracticeDirection.Reverse,
                1064,
                [MindPressure, Distraction, Resonance, DirectCoverage],
                ["CURRENT_REVERSE_604_SUPPRESSES_DIRECT_PRACTICE",
                    "CURRENT_REVERSE_604_APPLIES_THREE_LAYER_LOCK"],
                [directExact,
                    CurrentBroad("CURRENT_REVERSE_604_EFFECT_VERIFIED")],
                "THREE_LAYER_LOCK_REQUIRES_EXACT_RECOVERY_CASTS",
                evidence,
                "CURRENT_REVERSE_604_SUPPRESSION",
                [TacticalRoleUseKind.ActiveAttack]),
            CurrentRecoveryRole(686, 1422,
                "CURRENT_REVERSE_686_LOCK_RECOVERY",
                recoveryTransition,
                evidence,
                [TacticalRoleUseKind.ActiveAttack,
                    TacticalRoleUseKind.OpeningUse,
                    TacticalRoleUseKind.PersistentState]),
            CurrentRecoveryRole(602, 1062,
                "CURRENT_REVERSE_602_LOCK_RECOVERY_CONTROL",
                recoveryTransition,
                evidence,
                [TacticalRoleUseKind.ActiveAttack]),
            CurrentRecoveryRole(616, 1251,
                "CURRENT_REVERSE_616_LOCK_RECOVERY_PRESSURE",
                recoveryTransition,
                evidence,
                [TacticalRoleUseKind.ActiveAttack]),
            CurrentRecoveryRole(599, 1059,
                "CURRENT_REVERSE_599_LOCK_RECOVERY_TRICKS",
                recoveryTransition,
                evidence,
                [TacticalRoleUseKind.ActiveAttack]),
            CurrentMitigationRole(134, PracticeDirection.Reverse, 973,
                TacticalRoleKind.Mitigation,
                "CURRENT_REVERSE_134_RESONANCE_DURATION",
                TacticalRulePurpose.ResonanceDurationReduction,
                [Resonance],
                "CURRENT_REVERSE_134_SHORTENS_MIND_RHYTHM",
                mindExact,
                evidence,
                "CURRENT_REVERSE_134_RESONANCE",
                [TacticalRoleUseKind.ActiveAgility,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(150, PracticeDirection.Reverse, 989,
                TacticalRoleKind.Mitigation,
                "CURRENT_REVERSE_150_WEAPON_PARRY",
                TacticalRulePurpose.WeaponAttackParry,
                [Movement],
                "CURRENT_REVERSE_150_ENABLES_WEAPON_PARRY",
                movementExact,
                evidence,
                "CURRENT_REVERSE_150_WEAPON_PARRY",
                [TacticalRoleUseKind.ActiveAgility,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(151, PracticeDirection.Reverse, 990,
                TacticalRoleKind.Interrupt,
                "CURRENT_REVERSE_151_CAST_SPEED",
                TacticalRulePurpose.CastSpeedControl,
                [Speed],
                "CURRENT_REVERSE_151_REDUCES_CAST_SPEED",
                speedExact,
                evidence,
                "CURRENT_REVERSE_151_CAST_SPEED_CONTROL",
                [TacticalRoleUseKind.ActiveAgility,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(147, PracticeDirection.Direct, 260,
                TacticalRoleKind.Mitigation,
                "CURRENT_DIRECT_147_LONG_RANGE_HIT",
                TacticalRulePurpose.HitChanceControl,
                [Movement],
                "CURRENT_DIRECT_147_REDUCES_LONG_RANGE_HIT",
                movementExact,
                evidence,
                "CURRENT_DIRECT_147_LONG_RANGE_HIT_CONTROL",
                [TacticalRoleUseKind.ActiveAgility,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(148, PracticeDirection.Direct, 261,
                TacticalRoleKind.Interrupt,
                "CURRENT_DIRECT_148_ADVANCE_COUNTER",
                TacticalRulePurpose.MovementCounterattack,
                [Movement],
                "CURRENT_DIRECT_148_COUNTERS_ADVANCE",
                movementExact,
                evidence,
                "CURRENT_DIRECT_148_ADVANCE_COUNTER",
                [TacticalRoleUseKind.ActiveAgility,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(295, PracticeDirection.Reverse, 919,
                TacticalRoleKind.Mitigation,
                "CURRENT_REVERSE_295_MARK_DEFENSE",
                TacticalRulePurpose.CriticalInjuryProtection,
                [Distraction, Resonance],
                "CURRENT_REVERSE_295_PROTECTS_AND_REMOVES_MARK",
                mindExact,
                evidence,
                "CURRENT_REVERSE_295_HINDRANCE_DEFENSE",
                [TacticalRoleUseKind.ActiveDefense,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(303, PracticeDirection.Reverse, 927,
                TacticalRoleKind.Mitigation,
                "CURRENT_REVERSE_303_MIND_MARK_CONVERSION",
                TacticalRulePurpose.MindMarkConversion,
                [Distraction, Resonance],
                "CURRENT_REVERSE_303_CONVERTS_MIND_MARK",
                mindExact,
                evidence,
                "CURRENT_REVERSE_303_MIND_MARK_CONVERSION",
                [TacticalRoleUseKind.ActiveDefense,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(2, PracticeDirection.Direct, 1739,
                TacticalRoleKind.Mitigation,
                "CURRENT_DIRECT_2_DAMAGE_REDUCTION",
                TacticalRulePurpose.DirectDamageReduction,
                [MindPressure, Movement],
                "CURRENT_DIRECT_2_REDUCES_DIRECT_DAMAGE",
                coreExact,
                evidence,
                "CURRENT_DIRECT_2_DAMAGE_REDUCTION",
                [TacticalRoleUseKind.ActiveDefense,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(289, PracticeDirection.Direct, 187,
                TacticalRoleKind.Interrupt,
                "CURRENT_DIRECT_289_COUNTER_PRESSURE",
                TacticalRulePurpose.CounterStancePressure,
                [Movement],
                "CURRENT_DIRECT_289_APPLIES_STANCE_PRESSURE",
                movementExact,
                evidence,
                "CURRENT_DIRECT_289_COUNTER_PRESSURE",
                [TacticalRoleUseKind.ActiveDefense,
                    TacticalRoleUseKind.SwitchOnlyBackup]),
            CurrentMitigationRole(267, PracticeDirection.Direct, 165,
                TacticalRoleKind.Mitigation,
                "CURRENT_DIRECT_267_MARK_DURATION",
                TacticalRulePurpose.MarkDurationReduction,
                [Distraction, Resonance],
                "CURRENT_DIRECT_267_SHORTENS_MARK_DURATION",
                mindExact,
                evidence,
                "CURRENT_DIRECT_267_MARK_DURATION",
                [TacticalRoleUseKind.EquippedPassive]),
            CurrentMitigationRole(265, PracticeDirection.Reverse, 889,
                TacticalRoleKind.Mitigation,
                "CURRENT_REVERSE_265_MIND_DEFENSE",
                TacticalRulePurpose.MindDefenseIncrease,
                [MindPressure, Distraction],
                "CURRENT_REVERSE_265_INCREASES_MIND_DEFENSE",
                mindExact,
                evidence,
                "CURRENT_REVERSE_265_MIND_DEFENSE",
                [TacticalRoleUseKind.EquippedPassive]),
            CurrentMitigationRole(280, PracticeDirection.Reverse, 904,
                TacticalRoleKind.Mitigation,
                "CURRENT_REVERSE_280_CLOSE_AVOIDANCE",
                TacticalRulePurpose.CloseRangeAvoidance,
                [MindPressure, Movement],
                "CURRENT_REVERSE_280_INCREASES_CLOSE_AVOIDANCE",
                movementExact,
                evidence,
                "CURRENT_REVERSE_280_CLOSE_AVOIDANCE",
                [TacticalRoleUseKind.EquippedPassive]),
            CurrentMitigationRole(252, PracticeDirection.Direct, 150,
                TacticalRoleKind.Mitigation,
                "CURRENT_DIRECT_252_MOBILITY_SUSTAIN",
                TacticalRulePurpose.MobilitySustain,
                [Movement],
                "CURRENT_DIRECT_252_RESTORES_MOBILITY",
                movementExact,
                evidence,
                "CURRENT_DIRECT_252_MOBILITY_SUSTAIN",
                [TacticalRoleUseKind.EquippedPassive]),
            CurrentMitigationRole(624, PracticeDirection.Reverse, 1234,
                TacticalRoleKind.Mitigation,
                "CURRENT_REVERSE_624_POWER_REDUCTION",
                TacticalRulePurpose.EnemyAttackPowerReduction,
                [MindPressure, Distraction],
                "CURRENT_REVERSE_624_REDUCES_ATTACK_POWER",
                coreExact,
                evidence,
                "CURRENT_REVERSE_624_POWER_REDUCTION",
                [TacticalRoleUseKind.ActiveAttack,
                    TacticalRoleUseKind.OpeningUse,
                    TacticalRoleUseKind.PersistentState])
        };

        return new TacticalCombatRuleSet(
            InitialSemanticVersion,
            [VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion],
            [MindPressure, Distraction, Resonance, DirectCoverage, Movement,
                Speed],
            transitions,
            roles);
    }

    private static TacticalTransitionRule CurrentRoleTransition(
        string code,
        TacticalRulePurpose purpose,
        TacticalFactKind resultKind,
        string resultCode,
        IEnumerable<string> goals,
        TacticalRuleEvidenceRequirement exactRequirement,
        string effectEvidenceCode,
        TacticalEvidenceReference evidence) => CurrentTransition(
        code,
        purpose,
        TacticalTransitionTiming.OnObservedState,
        [Fact(TacticalFactKind.PlayerReadiness,
            "CURRENT_ROLE_REQUIREMENTS_SATISFIED")],
        [Fact(resultKind, resultCode)],
        goals,
        [exactRequirement, CurrentBroad(effectEvidenceCode)],
        "ROLE_REQUIREMENTS_AND_LIVE_CONDITIONS_APPLY",
        evidence);

    private static TacticalTransitionRule CurrentTransition(
        string code,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        IEnumerable<TacticalFactIdentity> triggers,
        IEnumerable<TacticalFactIdentity> results,
        IEnumerable<string> goals,
        IEnumerable<TacticalRuleEvidenceRequirement> requirements,
        string limitation,
        TacticalEvidenceReference evidence) => new(
        new TacticalTransitionIdentity(code),
        InitialSemanticVersion,
        [VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion],
        purpose,
        timing,
        triggers,
        results,
        goals,
        requirements,
        limitation,
        [evidence]);

    private static TacticalSkillRoleRule CurrentRecoveryRole(
        int skillId,
        int effectId,
        string code,
        string transition,
        TacticalEvidenceReference evidence,
        IEnumerable<TacticalRoleUseKind> useKinds) => CurrentRole(
        TacticalRoleKind.Recovery,
        code,
        TacticalRulePurpose.DirectPracticeLockRecovery,
        TacticalTransitionTiming.AfterManualAction,
        skillId,
        PracticeDirection.Reverse,
        effectId,
        [MindPressure, Distraction, Resonance, DirectCoverage],
        [transition],
        [CurrentExact("CURRENT_TARGET_FULL_DIRECT_COVERAGE"),
            CurrentBroad($"CURRENT_REVERSE_{skillId}_EFFECT_VERIFIED")],
        "ONLY_AN_EXECUTABLE_REVERSE_CAST_REMOVES_ONE_LAYER",
        evidence,
        skillId switch
        {
            686 => "CURRENT_REVERSE_686_RECOVERY",
            602 => "CURRENT_REVERSE_602_RECOVERY_CONTROL",
            616 => "CURRENT_REVERSE_616_RECOVERY_PRESSURE",
            599 => "CURRENT_REVERSE_599_RECOVERY_TRICKS",
            _ => throw new ArgumentOutOfRangeException(nameof(skillId))
        },
        useKinds);

    private static TacticalSkillRoleRule CurrentMitigationRole(
        int skillId,
        PracticeDirection direction,
        int effectId,
        TacticalRoleKind kind,
        string code,
        TacticalRulePurpose purpose,
        IEnumerable<string> goals,
        string transition,
        TacticalRuleEvidenceRequirement exactRequirement,
        TacticalEvidenceReference evidence,
        string counterCode,
        IEnumerable<TacticalRoleUseKind> useKinds) => CurrentRole(
        kind,
        code,
        purpose,
        useKinds.Contains(TacticalRoleUseKind.ActiveAttack)
            ? TacticalTransitionTiming.AfterCast
            : TacticalTransitionTiming.OnObservedState,
        skillId,
        direction,
        effectId,
        goals,
        [transition],
        [exactRequirement,
            CurrentBroad($"CURRENT_{direction.ToString().ToUpperInvariant()}_"
                + $"{skillId}_EFFECT_VERIFIED")],
        "EXACT_ACTIVATION_AND_LIVE_CONDITIONS_APPLY",
        evidence,
        counterCode,
        useKinds);

    private static TacticalSkillRoleRule CurrentRole(
        TacticalRoleKind kind,
        string code,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        int skillId,
        PracticeDirection direction,
        int effectId,
        IEnumerable<string> goals,
        IEnumerable<string> transitions,
        IEnumerable<TacticalRuleEvidenceRequirement> requirements,
        string limitation,
        TacticalEvidenceReference evidence,
        string counterCode,
        IEnumerable<TacticalRoleUseKind> useKinds)
    {
        var effect = CurrentEffect(skillId, direction, effectId);
        return new TacticalSkillRoleRule(
            new TacticalRoleIdentity(kind, code),
            InitialSemanticVersion,
            [VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion],
            purpose,
            timing,
            effect,
            effect.Mechanics,
            goals,
            transitions.Select(value => new TacticalTransitionIdentity(value)),
            requirements,
            limitation,
            [evidence],
            CurrentCounter(counterCode),
            useKinds);
    }

    private static TacticalRuleEvidenceRequirement CurrentExact(
        string code) => new(
        new TacticalRuleEvidenceIdentity(code),
        TacticalRuleEvidenceScope.ExactTarget,
        TacticalEvidenceSourceKind.ConfirmedObservation);

    private static TacticalRuleEvidenceRequirement CurrentBroad(
        string code) => new(
        new TacticalRuleEvidenceIdentity(code),
        TacticalRuleEvidenceScope.BroadRule,
        TacticalEvidenceSourceKind.VerifiedRule);

    private static TacticalEvidenceReference CurrentEvidence() => new(
        TacticalEvidenceSourceKind.VerifiedRule,
        "E8-F03-CURRENT-TYPED-ROLE-EVIDENCE",
        VerifiedCombatEffectCatalogs.CurrentAntiMagic.GameDataVersion,
        RuleVersion,
        "CURRENT_LATER_MAGIC_SOUND_ROLE_ATLAS");

    private static CombatEffectCatalogEntry CurrentEffect(
        int skillId,
        PracticeDirection direction,
        int effectId)
    {
        var catalog = VerifiedCombatEffectCatalogs.CurrentAntiMagic;
        var resolution = catalog.Resolve(
            catalog.GameDataVersion,
            skillId,
            direction,
            effectId);
        return resolution.IsRecognized
            ? resolution.CatalogEntry!
            : throw new InvalidOperationException(
                $"Current tactical effect {effectId} is not verified.");
    }

    private static CombatCounterRule CurrentCounter(string code) =>
        VerifiedCombatCounterRuleSets.CurrentMagicSound.Rules.Single(item =>
            string.Equals(item.Code, code, StringComparison.Ordinal));

    private static TacticalTransitionRule Transition(
        string code,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        IEnumerable<TacticalFactIdentity> triggers,
        IEnumerable<TacticalFactIdentity> results,
        IEnumerable<string> goals,
        IEnumerable<TacticalRuleEvidenceRequirement> requirements,
        string limitation,
        TacticalEvidenceReference evidence) => new(
            new TacticalTransitionIdentity(code),
            InitialSemanticVersion,
            [HistoricalGameDataVersion],
            purpose,
            timing,
            triggers,
            results,
            goals,
            requirements,
            limitation,
            [evidence]);

    private static TacticalSkillRoleRule Role(
        TacticalRoleKind kind,
        string code,
        TacticalRulePurpose purpose,
        TacticalTransitionTiming timing,
        int skillId,
        PracticeDirection direction,
        int effectId,
        IEnumerable<CombatEffectMechanic> mechanics,
        IEnumerable<string> goals,
        IEnumerable<string> transitions,
        IEnumerable<TacticalRuleEvidenceRequirement> requirements,
        string limitation,
        TacticalEvidenceReference evidence,
        string? sharedCounterCode = null) => new(
            new TacticalRoleIdentity(kind, code),
            InitialSemanticVersion,
            [HistoricalGameDataVersion],
            purpose,
            timing,
            Effect(skillId, direction, effectId),
            mechanics,
            goals,
            transitions.Select(code => new TacticalTransitionIdentity(code)),
            requirements,
            limitation,
            [evidence],
            sharedCounterCode is null ? null : Counter(sharedCounterCode));

    private static TacticalFactIdentity Fact(
        TacticalFactKind kind,
        string code) => new(kind, code);

    private static TacticalRuleEvidenceRequirement Exact(string code) => new(
        new TacticalRuleEvidenceIdentity(code),
        TacticalRuleEvidenceScope.ExactTarget,
        TacticalEvidenceSourceKind.ConfirmedObservation);

    private static TacticalRuleEvidenceRequirement Broad(string code) => new(
        new TacticalRuleEvidenceIdentity(code),
        TacticalRuleEvidenceScope.BroadRule,
        TacticalEvidenceSourceKind.VerifiedRule);

    private static TacticalEvidenceReference Evidence() => new(
        TacticalEvidenceSourceKind.VerifiedRule,
        "E8-000-TACTICAL-EVIDENCE",
        HistoricalGameDataVersion,
        RuleVersion,
        "HISTORICAL_MAGIC_SOUND_VERTICAL");

    private static CombatEffectCatalogEntry Effect(
        int skillId,
        PracticeDirection direction,
        int effectId)
    {
        var resolution = VerifiedCombatEffectCatalogs.GoldenAntiMagic.Resolve(
            HistoricalGameDataVersion,
            skillId,
            direction,
            effectId);
        return resolution.IsRecognized
            ? resolution.CatalogEntry!
            : throw new InvalidOperationException(
                $"Tactical effect {effectId} is not in the verified catalogue.");
    }

    private static CombatCounterRule Counter(string code) =>
        VerifiedCombatCounterRuleSets.GoldenMagicSound.Rules.Single(
            item => string.Equals(item.Code, code, StringComparison.Ordinal));
}
