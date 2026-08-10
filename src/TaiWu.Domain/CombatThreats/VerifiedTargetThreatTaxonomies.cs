namespace TaiWu.Domain.CombatThreats;

public static class VerifiedTargetThreatTaxonomies
{
    private const string GoldenEvidence =
        "docs/scenarios/M1-001-golden-target-selection.md"
        + "#critical-mechanic-to-verify";

    public static TargetThreatSet GoldenMagicSound { get; } =
        TargetThreatTaxonomy.Normalize(
            [
                Threat(
                    "POSITIVE_MAGIC_SOUND_MIND_DAMAGE",
                    TargetThreatKind.MindDamagePressure,
                    TargetThreatSeverity.High,
                    "Positive-practice magic-sound mind damage",
                    "Positive-practice magic-sound attacks accumulate "
                    + "mind-loss damage and pressure guarding-mind defense.",
                    TargetThreatActivationTiming.OnSkillUse,
                    "Verified magic-sound rule aligned with the target's "
                    + "positive-practice learned attacks."),
                Threat(
                    "DISTRACTION_MARK_ACCUMULATION",
                    TargetThreatKind.DistractionMarkAccumulation,
                    TargetThreatSeverity.Critical,
                    "Distraction-mark accumulation",
                    "Mind-loss damage produces distraction marks that can "
                    + "directly advance the player's defeat condition.",
                    TargetThreatActivationTiming.OnHit,
                    "Verified rule for mind-loss damage and distraction "
                    + "marks."),
                Threat(
                    "MIND_RESONANCE_CASCADE",
                    TargetThreatKind.MindResonanceCascade,
                    TargetThreatSeverity.Critical,
                    "Mind-resonance cascade",
                    "The first distraction mark begins a countdown; when it "
                    + "expires, mind resonance applies repeated mind-loss "
                    + "pressure and can make new marks persistent.",
                    TargetThreatActivationTiming.OnMarkApplied,
                    "Verified countdown, resonance, repeated-pressure, and "
                    + "persistent-mark rules."),
                Threat(
                    "DEFEAT_MARK_RESET_LOOP",
                    TargetThreatKind.DefeatMarkReset,
                    TargetThreatSeverity.Critical,
                    "Repeatable defeat-mark reset",
                    "Reverse-practice 九色玉蝉法 consumes 9 Qiqiao true-Qi "
                    + "when the target reaches the defeat condition, clears "
                    + "all injury, hindrance, and critical-injury marks, then "
                    + "raises the next cost by 9 up to 99. Surviving alone "
                    + "cannot win while the target can keep paying this cost.",
                    TargetThreatActivationTiming.Threshold,
                    "Verified from reverse 九色玉蝉法 effect 911 and the "
                    + "observed in-battle 消除己之标记 trigger.",
                    sourceSkillId: 287,
                    rawEffectId: 911)
            ],
            unknownMechanics: []);

    public static TargetThreatSet Epic5TargetFamilies { get; } =
        TargetThreatTaxonomy.Normalize(
            [
                Threat(
                    "CONFIGURED_OUTER_DAMAGE_PRESSURE",
                    TargetThreatKind.ConfiguredOuterDamagePressure,
                    TargetThreatSeverity.High,
                    "Configured outer-damage pressure",
                    "An active target attack is configured to cause outer "
                    + "damage, so reducing enemy attack-skill power is a "
                    + "verified mitigation path.",
                    TargetThreatActivationTiming.OnSkillUse,
                    "Verified from the active-skill outer-damage profile "
                    + "facet and exact 伏龍刀法 reverse effect 1234."),
                Threat(
                    "CHANNEL_RESISTANCE_ASYMMETRY",
                    TargetThreatKind.ChannelResistanceAsymmetry,
                    TargetThreatSeverity.High,
                    "Outer/inner resistance asymmetry",
                    "The target's base outer and inner resistances differ, "
                    + "so an exact channel-routing attack can target the "
                    + "lower resistance.",
                    TargetThreatActivationTiming.Always,
                    "Verified from exact base resistance measurements and "
                    + "錯倒陰陽拂塵 effects 697 and 1423."),
                Threat(
                    "CONFIGURED_POISON_APPLICATION",
                    TargetThreatKind.ConfiguredPoisonApplication,
                    TargetThreatSeverity.High,
                    "Configured poison application",
                    "An active target attack is configured to apply poison, "
                    + "so active direct-poison defense is a verified "
                    + "response.",
                    TargetThreatActivationTiming.OnHit,
                    "Verified from the active-skill poison profile facet "
                    + "and 五黃辟毒術 effects 180 and 906.")
            ],
            unknownMechanics: []);

    private static TargetThreat Threat(
        string code,
        TargetThreatKind kind,
        TargetThreatSeverity severity,
        string title,
        string explanation,
        TargetThreatActivationTiming activationTiming,
        string evidenceSummary,
        int? sourceSkillId = null,
        int? rawEffectId = null)
    {
        return new TargetThreat(
            code,
            kind,
            severity,
            title,
            explanation,
            activationTiming,
            [
                new TargetThreatEvidence(
                    GoldenEvidence,
                    evidenceSummary,
                    TargetThreatEvidenceConfidence.VerifiedRule,
                    sourceSkillId,
                    rawEffectId)
            ]);
    }
}
