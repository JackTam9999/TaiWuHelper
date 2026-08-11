using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatRecommendations;

public sealed class CombatLoadoutOptionTests
{
    [Fact]
    public void Counter_rule_can_be_scoped_only_to_its_applicable_threats()
    {
        var rule = FulongRule();

        var option = CombatLoadoutOption.FromCounterRule(
            rule,
            isCurrentlyEquipped: false,
            applicableThreatCodes: ["CONFIGURED_OUTER_DAMAGE_PRESSURE"]);

        Assert.Equal(
            ["CONFIGURED_OUTER_DAMAGE_PRESSURE"],
            option.ThreatCodes);
    }

    [Fact]
    public void Counter_rule_rejects_an_unverified_applicable_threat()
    {
        var rule = FulongRule();

        Assert.Throws<ArgumentException>(() =>
            CombatLoadoutOption.FromCounterRule(
                rule,
                isCurrentlyEquipped: false,
                applicableThreatCodes: ["UNRELATED_THREAT"]));
    }

    [Fact]
    public void Counter_rule_rejects_an_empty_applicable_threat_scope()
    {
        var rule = FulongRule();

        Assert.Throws<ArgumentException>(() =>
            CombatLoadoutOption.FromCounterRule(
                rule,
                isCurrentlyEquipped: false,
                applicableThreatCodes: []));
    }

    private static CombatCounterRule FulongRule() =>
        VerifiedCombatCounterRuleSets.GoldenMagicSound.Rules.Single(rule =>
            rule.Code == "REVERSE_FULONG_POWER_REDUCTION");
}
