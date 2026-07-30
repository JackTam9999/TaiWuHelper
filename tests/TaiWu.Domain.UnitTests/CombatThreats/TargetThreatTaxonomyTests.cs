using TaiWu.Domain.CombatThreats;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatThreats;

public sealed class TargetThreatTaxonomyTests
{
    private const string Evidence =
        "docs/scenarios/verified-target-rule.md";

    [Fact]
    public void Threat_preserves_typed_kind_severity_timing_and_evidence()
    {
        var evidence = new TargetThreatEvidence(
            Evidence,
            "Verified effect.",
            TargetThreatEvidenceConfidence.Snapshot,
            sourceSkillId: 100,
            rawEffectId: 200);

        var threat = CreateThreat(evidence: [evidence]);

        Assert.Equal(
            TargetThreatKind.MindResonanceCascade,
            threat.Kind);
        Assert.Equal(TargetThreatSeverity.Critical, threat.Severity);
        Assert.Equal(
            TargetThreatActivationTiming.OnMarkApplied,
            threat.ActivationTiming);
        Assert.Same(evidence, Assert.Single(threat.Evidence));
        Assert.Equal(100, evidence.SourceSkillId);
        Assert.Equal(200, evidence.RawEffectId);
    }

    [Fact]
    public void Severity_values_define_stable_ascending_priority()
    {
        Assert.True(
            TargetThreatSeverity.Informational
            < TargetThreatSeverity.Moderate);
        Assert.True(
            TargetThreatSeverity.Moderate
            < TargetThreatSeverity.High);
        Assert.True(
            TargetThreatSeverity.High
            < TargetThreatSeverity.Critical);
    }

    [Fact]
    public void Every_threat_requires_source_evidence()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateThreat(evidence: []));

        Assert.Contains("requires source evidence", exception.Message);
    }

    [Fact]
    public void Unknown_mechanic_generates_visible_warning_without_threat()
    {
        var unknown = new UnknownTargetMechanic(
            "Effect 4321 has not been classified.",
            Evidence,
            sourceSkillId: 999,
            rawEffectId: 4321);

        var result = TargetThreatTaxonomy.Normalize([], [unknown]);

        Assert.Empty(result.Threats);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(
            TargetThreatTaxonomy.UnrecognizedMechanicWarningCode,
            warning.Code);
        Assert.Contains("4321", warning.Message);
        Assert.Same(unknown, warning.Mechanic);
        Assert.Equal(Evidence, warning.Mechanic.EvidenceReference);
    }

    [Fact]
    public void Golden_taxonomy_contains_critical_mind_resonance_chain()
    {
        var result = VerifiedTargetThreatTaxonomies.GoldenMagicSound;

        var threat = Assert.Single(
            result.Threats,
            value => value.Code == "MIND_RESONANCE_CASCADE");
        Assert.Equal(
            TargetThreatKind.MindResonanceCascade,
            threat.Kind);
        Assert.Equal(TargetThreatSeverity.Critical, threat.Severity);
        Assert.Contains("countdown", threat.Explanation);
        Assert.Contains("persistent", threat.Explanation);
        Assert.NotEmpty(threat.Evidence);
    }

    [Fact]
    public void Unconfirmed_golden_reset_remains_warning_not_threat()
    {
        var result = VerifiedTargetThreatTaxonomies.GoldenMagicSound;

        Assert.DoesNotContain(
            result.Threats,
            threat => threat.Kind
                == TargetThreatKind.PersistentDefeatMarks);
        var warning = Assert.Single(result.Warnings);
        Assert.Contains("36 defeat marks", warning.Message);
        Assert.Contains("九色玉蟬法", warning.Message);
    }

    [Fact]
    public void Duplicate_threat_codes_are_rejected()
    {
        var threat = CreateThreat();

        var exception = Assert.Throws<ArgumentException>(
            () => TargetThreatTaxonomy.Normalize(
                [threat, threat],
                []));

        Assert.Contains("Duplicate target-threat code", exception.Message);
    }

    [Fact]
    public void Threat_collections_are_copied()
    {
        TargetThreat[] threats = [CreateThreat()];
        UnknownTargetMechanic[] unknowns =
        [
            new("Unmapped.", Evidence)
        ];

        var result = TargetThreatTaxonomy.Normalize(threats, unknowns);
        threats[0] = CreateThreat(code: "REPLACED");
        unknowns[0] = new UnknownTargetMechanic("Replaced.", Evidence);

        Assert.Equal("MIND_RESONANCE_CASCADE", result.Threats[0].Code);
        Assert.Contains("Unmapped.", result.Warnings[0].Message);
    }

    [Fact]
    public void Invalid_identity_enums_and_evidence_are_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => CreateThreat(code: "not-valid"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateThreat(kind: (TargetThreatKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateThreat(severity: (TargetThreatSeverity)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TargetThreatEvidence(
                Evidence,
                "Invalid source.",
                TargetThreatEvidenceConfidence.VerifiedRule,
                sourceSkillId: -1));
        Assert.Throws<ArgumentException>(
            () => new UnknownTargetMechanic(
                "Unknown.",
                evidenceReference: " "));
    }

    private static TargetThreat CreateThreat(
        string code = "MIND_RESONANCE_CASCADE",
        TargetThreatKind kind = TargetThreatKind.MindResonanceCascade,
        TargetThreatSeverity severity = TargetThreatSeverity.Critical,
        TargetThreatEvidence[]? evidence = null)
    {
        return new TargetThreat(
            code,
            kind,
            severity,
            title: "Mind resonance",
            explanation: "Repeated mind-loss pressure.",
            TargetThreatActivationTiming.OnMarkApplied,
            evidence
                ??
                [
                    new TargetThreatEvidence(
                        Evidence,
                        "Verified rule.",
                        TargetThreatEvidenceConfidence.VerifiedRule)
                ]);
    }
}
