using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetProfiles;
using Xunit;

namespace TaiWu.Domain.UnitTests.TargetProfiles;

public sealed class TargetProfileExtractionRuleSetTests
{
    [Fact]
    public void Initial_rules_use_only_evidence_approved_exact_facets()
    {
        var rules = VerifiedTargetProfileExtractionRuleSets.Initial;

        Assert.Equal(
            "E5.PROFILE.1",
            rules.RuleVersion.Value);
        Assert.Equal(
            VerifiedTargetProfileExtractionRuleSets.SupportedGameDataVersion,
            rules.GameDataVersion.Value);
        Assert.Equal(
            "OUTER_DAMAGE_CONFIGURED",
            rules.OuterDamageFacet.Code);
        Assert.Equal(
            "CHANNEL_RESISTANCE_ASYMMETRY",
            rules.ChannelResistanceFacet.Code);
        Assert.Equal(
            "POISON_APPLICATION_CONFIGURED",
            rules.PoisonApplicationFacet.Code);
        Assert.Equal(
            "WEAPON_SUBTYPE:16",
            rules.WeaponSubtypeFacet(16).Code);
        Assert.Equal(
            [
                TargetThreatKind.MindDamagePressure,
                TargetThreatKind.DistractionMarkAccumulation,
                TargetThreatKind.MindResonanceCascade,
                TargetThreatKind.DefeatMarkReset
            ],
            rules.ThreatFacetRules.Select(rule => rule.ThreatKind));
        Assert.DoesNotContain(
            "HIGH",
            string.Join(',', rules.ThreatFacetRules.Select(rule =>
                rule.Facet.Code)));
    }

    [Fact]
    public void Rule_set_copies_sorts_and_rejects_duplicate_threat_mappings()
    {
        var values = new List<TargetProfileThreatFacetRule>
        {
            ThreatRule(
                TargetThreatKind.MindResonanceCascade,
                TargetProfileDimension.Control,
                "MIND_RESONANCE_CONTROL"),
            ThreatRule(
                TargetThreatKind.MindDamagePressure,
                TargetProfileDimension.Pressure,
                "MIND_DAMAGE_PRESSURE")
        };
        var rules = Rules(values);

        values.Clear();

        Assert.Equal(
            [
                TargetThreatKind.MindDamagePressure,
                TargetThreatKind.MindResonanceCascade
            ],
            rules.ThreatFacetRules.Select(rule => rule.ThreatKind));
        Assert.Throws<ArgumentException>(() => Rules(
        [
            ThreatRule(
                TargetThreatKind.MindDamagePressure,
                TargetProfileDimension.Pressure,
                "MIND_DAMAGE_PRESSURE"),
            ThreatRule(
                TargetThreatKind.MindDamagePressure,
                TargetProfileDimension.Control,
                "OTHER_MIND_DAMAGE")
        ]));
        Assert.Throws<ArgumentException>(() => Rules(
        [
            ThreatRule(
                TargetThreatKind.MindDamagePressure,
                TargetProfileDimension.Pressure,
                "SHARED_FACET"),
            ThreatRule(
                TargetThreatKind.MindResonanceCascade,
                TargetProfileDimension.Pressure,
                "SHARED_FACET")
        ]));
    }

    [Fact]
    public void Core_facets_require_their_independent_dimensions()
    {
        Assert.Throws<ArgumentException>(() => new TargetProfileExtractionRuleSet(
            new TargetProfileVersion("E5.PROFILE.1"),
            new TargetProfileVersion("1.0.0"),
            new TargetProfileFacetIdentity(
                TargetProfileDimension.AttackFamily,
                "OUTER_DAMAGE_CONFIGURED"),
            Identity(
                TargetProfileDimension.Resilience,
                "CHANNEL_RESISTANCE_ASYMMETRY"),
            Identity(
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED"),
            "WEAPON_SUBTYPE",
            []));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VerifiedTargetProfileExtractionRuleSets.Initial
                .WeaponSubtypeFacet(0));
    }

    private static TargetProfileExtractionRuleSet Rules(
        IEnumerable<TargetProfileThreatFacetRule> threats) => new(
            new TargetProfileVersion("E5.PROFILE.1"),
            new TargetProfileVersion("1.0.0"),
            Identity(
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"),
            Identity(
                TargetProfileDimension.Resilience,
                "CHANNEL_RESISTANCE_ASYMMETRY"),
            Identity(
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED"),
            "WEAPON_SUBTYPE",
            threats);

    private static TargetProfileThreatFacetRule ThreatRule(
        TargetThreatKind kind,
        TargetProfileDimension dimension,
        string code) => new(kind, Identity(dimension, code));

    private static TargetProfileFacetIdentity Identity(
        TargetProfileDimension dimension,
        string code) => new(dimension, code);
}
