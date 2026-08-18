using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Domain.UnitTests.VillageWorkforce;

public sealed class VillageWorkforceRuleTests
{
    [Fact]
    public void Verified_rule_pins_identity_versions_gates_component_and_limits()
    {
        var resolution = Resolve();

        Assert.True(resolution.IsResolved);
        Assert.Equal(
            WorkforceRuleResolutionStatus.Resolved,
            resolution.Status);
        var rule = Assert.IsType<WorkforceRuleDefinition>(resolution.Rule);
        Assert.Equal(
            "SHOP_MANAGER_REQUIRED_BASE_LIFE_SKILL_QUALIFICATION",
            rule.Identity.Value);
        Assert.Equal("1.0.0", rule.Version.Value);
        Assert.Equal(
            WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
            rule.Objective.Kind);
        Assert.Equal("1", rule.Objective.Version);
        Assert.Equal(
            VerifiedVillageWorkforceRules.SupportedGameDataVersion,
            rule.SupportedSource.GameDataVersion);
        Assert.Equal("1", rule.SupportedSource.MappingVersion);
        Assert.Equal("1", rule.SupportedSource.CandidateUniverseVersion);
        Assert.Equal("1", rule.SupportedSource.FingerprintSchemaVersion);
        Assert.Equal(WorkforceTargetKind.ShopManagerSlot, rule.TargetKind);
        Assert.Equal(
            Enum.GetValues<WorkforceRequirementKind>(),
            rule.Requirements.Select(item => item.Requirement));
        Assert.Equal(
            [
                WorkforceEvidenceRequirementKind.SourceVersions,
                WorkforceEvidenceRequirementKind.SupportedTarget,
                WorkforceEvidenceRequirementKind.ConfirmedFact,
                WorkforceEvidenceRequirementKind.ConfirmedFact,
                WorkforceEvidenceRequirementKind.MatchingProvenance
            ],
            rule.Requirements.Select(item => item.EvidenceRequirement));

        var component = Assert.Single(rule.Components);
        Assert.Equal(
            WorkforceComponentKind.RequiredBaseLifeSkillQualification,
            component.Identity.Kind);
        Assert.Equal(6, component.Identity.Discipline.Type);
        Assert.Equal(
            WorkforceFactKind.BaseLifeSkillQualification,
            component.SourceFact.Kind);
        Assert.Equal(WorkforceNormalizationKind.Identity, component.Normalization);
        Assert.Equal(WorkforceUnit.BaseQualificationPoint, component.Unit);
        Assert.Equal(WorkforceScoreDirection.HigherIsBetter, component.Direction);
        Assert.Equal(1m, component.Weight);
        Assert.Equal(
            [
                "NO_EFFICIENCY_OUTPUT_OR_REVENUE",
                "OCCUPIED_SHOP_REPLACEMENT_ONLY",
                "SAVED_BASE_QUALIFICATION_ONLY"
            ],
            rule.Limitations.Select(item => item.Identity));
        Assert.Equal(64, rule.Fingerprint.Length);
        Assert.Equal(rule.Fingerprint, Resolve().Rule?.Fingerprint);
    }

    [Fact]
    public void Unsupported_versions_and_target_return_typed_results_without_rule()
    {
        AssertUnsupported(
            Resolve(objectiveVersion: "2"),
            WorkforceRuleResolutionStatus.UnsupportedObjectiveVersion);
        AssertUnsupported(
            Resolve(gameDataVersion: "1.0.0+different"),
            WorkforceRuleResolutionStatus.UnsupportedGameDataVersion);
        AssertUnsupported(
            Resolve(mappingVersion: "2"),
            WorkforceRuleResolutionStatus.UnsupportedMappingVersion);
        AssertUnsupported(
            Resolve(candidateUniverseVersion: "2"),
            WorkforceRuleResolutionStatus.UnsupportedCandidateUniverseVersion);
        AssertUnsupported(
            Resolve(fingerprintSchemaVersion: "2"),
            WorkforceRuleResolutionStatus.UnsupportedFingerprintSchemaVersion);
        AssertUnsupported(
            Resolve(targetKind: (WorkforceTargetKind)99),
            WorkforceRuleResolutionStatus.UnsupportedTargetKind);
    }

    [Fact]
    public void Rule_version_requires_semantic_major_minor_patch()
    {
        Assert.Equal(
            "1.0.0-alpha-1+build.7",
            new WorkforceRuleVersion("1.0.0-alpha-1+build.7").Value);
        Assert.Throws<ArgumentException>(() => new WorkforceRuleVersion("1"));
        Assert.Throws<ArgumentException>(
            () => new WorkforceRuleVersion("01.0.0"));
        Assert.Throws<ArgumentException>(
            () => new WorkforceRuleVersion("1.0.0-01"));
    }

    [Fact]
    public void Definition_rejects_duplicate_rule_identities()
    {
        var rule = Assert.IsType<WorkforceRuleDefinition>(Resolve().Rule);
        Assert.Throws<ArgumentException>(() => CopyRule(
            rule,
            requirements:
            [
                rule.Requirements[0],
                rule.Requirements[0],
                rule.Requirements[2],
                rule.Requirements[3],
                rule.Requirements[4]
            ]));
        Assert.Throws<ArgumentException>(() => CopyRule(
            rule,
            components: [rule.Components[0], rule.Components[0]]));
        Assert.Throws<ArgumentException>(() => CopyRule(
            rule,
            limitations: [rule.Limitations[0], rule.Limitations[0]]));
    }

    [Fact]
    public void Component_rejects_invalid_weight_unit_and_source_field()
    {
        var rule = Assert.IsType<WorkforceRuleDefinition>(Resolve().Rule);
        var component = rule.Components[0];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkforceComponentDefinition(
                component.Identity,
                component.SourceFact,
                component.Normalization,
                component.Unit,
                component.Direction,
                weight: 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkforceComponentDefinition(
                component.Identity,
                component.SourceFact,
                component.Normalization,
                (WorkforceUnit)99,
                component.Direction,
                component.Weight));
        Assert.Throws<ArgumentException>(() =>
            new WorkforceComponentDefinition(
                component.Identity,
                new WorkforceFactIdentity(
                    WorkforceFactKind.CandidateUniverseMembership),
                component.Normalization,
                component.Unit,
                component.Direction,
                component.Weight));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkforceFactIdentity((WorkforceFactKind)99));
    }

    private static WorkforceRuleResolution Resolve(
        string objectiveVersion = "1",
        string? gameDataVersion = null,
        string mappingVersion = "1",
        string candidateUniverseVersion = "1",
        string fingerprintSchemaVersion = "1",
        WorkforceTargetKind targetKind = WorkforceTargetKind.ShopManagerSlot) =>
        VerifiedVillageWorkforceRules.Resolve(
            new WorkforceObjectiveIdentity(
                WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
                objectiveVersion),
            new WorkforceSourceVersions(
                new string('A', 64),
                gameDataVersion
                    ?? VerifiedVillageWorkforceRules.SupportedGameDataVersion,
                mappingVersion,
                candidateUniverseVersion,
                fingerprintSchemaVersion),
            targetKind,
            new LifeSkillDisciplineIdentity(6));

    private static void AssertUnsupported(
        WorkforceRuleResolution resolution,
        WorkforceRuleResolutionStatus expected)
    {
        Assert.False(resolution.IsResolved);
        Assert.Equal(expected, resolution.Status);
        Assert.Null(resolution.Rule);
        Assert.NotEmpty(resolution.ReasonIdentity);
    }

    private static WorkforceRuleDefinition CopyRule(
        WorkforceRuleDefinition rule,
        IEnumerable<WorkforceRequirementDefinition>? requirements = null,
        IEnumerable<WorkforceComponentDefinition>? components = null,
        IEnumerable<WorkforceRuleLimitation>? limitations = null) =>
        new(
            rule.Identity,
            rule.Version,
            rule.Objective,
            rule.SupportedSource,
            rule.TargetKind,
            requirements ?? rule.Requirements,
            components ?? rule.Components,
            limitations ?? rule.Limitations);
}
