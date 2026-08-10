using TaiWu.Domain.CombatThreats;

namespace TaiWu.Domain.TargetProfiles;

public static class VerifiedTargetProfileExtractionRuleSets
{
    public const string InitialRuleVersion = "E5.PROFILE.1";

    public const string SupportedGameDataVersion =
        "1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a";

    public static TargetProfileExtractionRuleSet Initial { get; } = new(
        new TargetProfileVersion(InitialRuleVersion),
        new TargetProfileVersion(SupportedGameDataVersion),
        new TargetProfileFacetIdentity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED"),
        new TargetProfileFacetIdentity(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY"),
        new TargetProfileFacetIdentity(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED"),
        "WEAPON_SUBTYPE",
        [
            new TargetProfileThreatFacetRule(
                TargetThreatKind.MindDamagePressure,
                new TargetProfileFacetIdentity(
                    TargetProfileDimension.Pressure,
                    "MIND_DAMAGE_PRESSURE")),
            new TargetProfileThreatFacetRule(
                TargetThreatKind.DistractionMarkAccumulation,
                new TargetProfileFacetIdentity(
                    TargetProfileDimension.Control,
                    "DISTRACTION_MARK_CONTROL")),
            new TargetProfileThreatFacetRule(
                TargetThreatKind.MindResonanceCascade,
                new TargetProfileFacetIdentity(
                    TargetProfileDimension.Control,
                    "MIND_RESONANCE_CONTROL")),
            new TargetProfileThreatFacetRule(
                TargetThreatKind.DefeatMarkReset,
                new TargetProfileFacetIdentity(
                    TargetProfileDimension.Resilience,
                    "DEFEAT_MARK_RESET"))
        ]);
}
