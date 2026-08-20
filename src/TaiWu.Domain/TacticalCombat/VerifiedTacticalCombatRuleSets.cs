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

    private const string MindPressure =
        "POSITIVE_MAGIC_SOUND_MIND_DAMAGE";
    private const string Distraction =
        "DISTRACTION_MARK_ACCUMULATION";
    private const string Resonance = "MIND_RESONANCE_CASCADE";
    private const string Reset = "DEFEAT_MARK_RESET_LOOP";

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
