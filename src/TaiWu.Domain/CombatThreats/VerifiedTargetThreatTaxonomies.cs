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
                    + "persistent-mark rules.")
            ],
            [
                new UnknownTargetMechanic(
                    "The observed reset at 36 defeat marks resembles reverse "
                    + "九色玉蟬法, but the target's equipped source effect "
                    + "is not confirmed.",
                    GoldenEvidence)
            ]);

    private static TargetThreat Threat(
        string code,
        TargetThreatKind kind,
        TargetThreatSeverity severity,
        string title,
        string explanation,
        TargetThreatActivationTiming activationTiming,
        string evidenceSummary)
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
                    TargetThreatEvidenceConfidence.VerifiedRule)
            ]);
    }
}
