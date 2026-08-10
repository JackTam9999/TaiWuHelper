using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;
using Xunit;

namespace TaiWu.Domain.UnitTests.TargetPlaybooks;

public sealed class TargetCounterPlaybookTests
{
    [Fact]
    public void Initial_catalog_delivers_four_versioned_families()
    {
        var catalog = VerifiedTargetCounterPlaybooks.Initial;

        Assert.Equal(
            VerifiedCombatEffectCatalogs.GoldenGameDataVersion,
            catalog.GameDataVersion.Value);
        Assert.Equal(4, catalog.Archetypes.Length);
        Assert.Equal(4, catalog.Playbooks.Length);
        Assert.Equal(
            [
                "CHANNEL_RESISTANCE_ASYMMETRY",
                "MIND_RESONANCE_RESET_BASELINE",
                "OUTER_DAMAGE_CONFIGURED",
                "POISON_APPLICATION_CONFIGURED"
            ],
            catalog.Playbooks.Select(
                playbook => playbook.Identity.Archetype.Code));
        Assert.All(
            catalog.Playbooks,
            playbook =>
            {
                Assert.Equal(
                    VerifiedTargetCounterPlaybooks.InitialArchetypeVersion,
                    playbook.Identity.Archetype.Version.Value);
                Assert.Equal(
                    VerifiedTargetCounterPlaybooks.InitialPlaybookVersion,
                    playbook.Identity.Version.Value);
                Assert.Equal(
                    $"{playbook.Identity.Archetype.StableKey}"
                    + "/PLAYBOOK@1.0.0",
                    playbook.StableKey);
            });
    }

    [Fact]
    public void Baseline_preserves_all_verified_threats_and_counter_rules()
    {
        var catalog = VerifiedTargetCounterPlaybooks.Initial;
        var baseline = Playbook("MIND_RESONANCE_RESET_BASELINE");
        var expectedThreatCodes = VerifiedTargetThreatTaxonomies
            .GoldenMagicSound
            .Threats
            .Select(threat => threat.Code)
            .Order(StringComparer.Ordinal);
        var expectedRules = VerifiedCombatCounterRuleSets
            .GoldenMagicSound
            .Rules
            .OrderBy(rule => rule.Code, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedThreatCodes,
            baseline.Goals
                .SelectMany(goal => goal.Threats)
                .Select(threat => threat.Code)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));

        var actualOptions = baseline.Goals
            .SelectMany(goal => goal.Options)
            .DistinctBy(option => option.Code, StringComparer.Ordinal)
            .OrderBy(option => option.Code, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            expectedRules.Select(rule => rule.Code),
            actualOptions.Select(option => option.Code));
        Assert.All(
            actualOptions,
            option =>
            {
                var verified = Assert.Single(
                    catalog.VerifiedCounterRules,
                    rule => rule.Code == option.Code);
                Assert.Same(verified, option.CounterRule);
                Assert.Same(verified.Effect, option.Effect);
                Assert.True(option.Effect.HasTypedMechanics);
                Assert.Equal(
                    verified.ActivationTiming,
                    option.ActivationTiming);
                Assert.Equal(verified.Requirements, option.Requirements);
                Assert.Equal(
                    [verified.Effect.SourceReference],
                    option.EvidenceReferences);
            });

        var resetGoal = Assert.Single(
            baseline.Goals,
            goal => goal.Code == "PRESSURE_DEFEAT_MARK_RESET");
        Assert.Contains(
            resetGoal.KnownGaps,
            gap => gap.Code == "NO_GUARANTEED_RESET_LOCKOUT"
                && gap.Kind
                    == TargetCounterPlaybookGapKind.IncompleteEvidence);
    }

    [Fact]
    public void New_families_are_typed_goals_with_explicit_verified_gaps()
    {
        var newFamilies = VerifiedTargetCounterPlaybooks
            .Initial
            .Playbooks
            .Where(playbook => playbook.Identity.Archetype.Code
                != "MIND_RESONANCE_RESET_BASELINE")
            .ToArray();

        Assert.Equal(3, newFamilies.Length);
        Assert.All(
            newFamilies,
            playbook =>
            {
                var goal = Assert.Single(playbook.Goals);
                Assert.Single(goal.ProfileFacets);
                Assert.Empty(goal.Threats);
                Assert.Empty(goal.Options);
                var gap = Assert.Single(goal.KnownGaps);
                Assert.Equal(
                    TargetCounterPlaybookGapKind.NoVerifiedOption,
                    gap.Kind);
                Assert.Equal(["E5-000"], gap.EvidenceReferences);
            });

        Assert.Equal(
            [
                "CHANNEL_RESISTANCE_ASYMMETRY",
                "OUTER_DAMAGE_CONFIGURED",
                "POISON_APPLICATION_CONFIGURED"
            ],
            newFamilies
                .SelectMany(playbook => playbook.Goals)
                .SelectMany(goal => goal.ProfileFacets)
                .Select(facet => facet.Code)
                .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Delivered_archetypes_reference_only_initial_profile_facets()
    {
        var catalog = VerifiedTargetCounterPlaybooks.Initial;
        var rules = VerifiedTargetProfileExtractionRuleSets.Initial;
        var permitted = rules.ThreatFacetRules
            .Select(rule => rule.Facet)
            .Append(rules.OuterDamageFacet)
            .Append(rules.ChannelResistanceFacet)
            .Append(rules.PoisonApplicationFacet)
            .Select(facet => $"{facet.Dimension}:{facet.Code}")
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(
            catalog.Archetypes,
            archetype =>
            {
                Assert.Equal(
                    rules.RuleVersion,
                    archetype.ApplicableProfileRuleVersion);
                Assert.All(
                    archetype.RequiredPredicates,
                    predicate => Assert.Contains(
                        $"{predicate.Facet.Dimension}:{predicate.Facet.Code}",
                        permitted));
                Assert.Empty(archetype.SupportingPredicates);
                Assert.Empty(archetype.Exclusions);
            });
    }

    [Fact]
    public void Catalog_resolution_is_exact_version_and_exact_archetype()
    {
        var catalog = VerifiedTargetCounterPlaybooks.Initial;
        var archetype = catalog.Archetypes[0].Identity;

        var resolved = catalog.Resolve(
            catalog.GameDataVersion.Value,
            archetype);
        var versionMismatch = catalog.Resolve(
            catalog.GameDataVersion.Value + ".changed",
            archetype);
        var missing = catalog.Resolve(
            catalog.GameDataVersion.Value,
            new TargetArchetypeIdentity(
                "UNREVIEWED_ARCHETYPE",
                archetype.Version));

        Assert.Equal(
            TargetCounterPlaybookResolutionStatus.Resolved,
            resolved.Status);
        Assert.True(resolved.IsResolved);
        Assert.NotNull(resolved.Playbook);
        Assert.Equal(
            TargetCounterPlaybookResolutionStatus.UnsupportedGameDataVersion,
            versionMismatch.Status);
        Assert.False(versionMismatch.IsResolved);
        Assert.Null(versionMismatch.Playbook);
        Assert.Equal(
            TargetCounterPlaybookResolutionStatus.ArchetypeNotFound,
            missing.Status);
        Assert.Null(missing.Playbook);
    }

    [Fact]
    public void Goal_and_option_ordering_ignore_source_declaration_order()
    {
        var baseline = Playbook("MIND_RESONANCE_RESET_BASELINE");
        var reordered = new TargetCounterPlaybook(
            baseline.Identity,
            baseline.Goals.Reverse(),
            baseline.EvidenceReferences.Reverse());

        Assert.Equal(
            baseline.Goals.Select(goal => goal.Code),
            reordered.Goals.Select(goal => goal.Code));
        Assert.Equal(
            baseline.EvidenceReferences,
            reordered.EvidenceReferences);

        var mindThreat = Threat("POSITIVE_MAGIC_SOUND_MIND_DAMAGE");
        var jinni = Option("REVERSE_JINNI_SUPPRESSION");
        var fulong = Option("REVERSE_FULONG_POWER_REDUCTION");
        var goal = Goal(
            threats: [mindThreat],
            options: [fulong, jinni]);

        Assert.Equal(
            ["REVERSE_JINNI_SUPPRESSION", "REVERSE_FULONG_POWER_REDUCTION"],
            goal.Options.Select(option => option.Code));

        var catalog = VerifiedTargetCounterPlaybooks.Initial;
        var reorderedCatalog = new TargetCounterPlaybookCatalog(
            catalog.GameDataVersion,
            catalog.Archetypes.Reverse(),
            [VerifiedCombatCounterRuleSets.GoldenMagicSound],
            catalog.Playbooks.Reverse());
        Assert.Equal(
            catalog.Playbooks.Select(playbook => playbook.StableKey),
            reorderedCatalog.Playbooks.Select(playbook => playbook.StableKey));
    }

    [Fact]
    public void Mechanical_goals_require_typed_references_and_explicit_gaps()
    {
        Assert.Throws<ArgumentException>(() => Goal(
            facets: [],
            threats: [],
            options: [],
            gaps: [Gap()]));
        Assert.Throws<ArgumentException>(() => Goal(
            facets: [Facet()],
            threats: [],
            options: [],
            gaps: []));
        Assert.Throws<ArgumentException>(() =>
            new TargetCounterPlaybookOption(
                Rule("REVERSE_JINNI_SUPPRESSION"),
                ["ACTIVE_ATTACK_ROLE", "ACTIVE_ATTACK_ROLE"]));
    }

    [Fact]
    public void Counter_options_must_address_the_goal_threat()
    {
        Assert.Throws<ArgumentException>(() => Goal(
            threats: [Threat("POSITIVE_MAGIC_SOUND_MIND_DAMAGE")],
            options: [Option("REVERSE_QILUN_TRUE_QI_DRAIN")]));
        Assert.Throws<ArgumentException>(() => Goal(
            facets: [Facet()],
            threats: [],
            options: [Option("REVERSE_JINNI_SUPPRESSION")]));
    }

    [Fact]
    public void Inaccessible_option_gaps_require_and_retain_an_exact_option()
    {
        var option = Option("REVERSE_JINNI_SUPPRESSION");
        var gap = new TargetCounterPlaybookGap(
            "PLAYER_CANNOT_ACCESS_JINNI",
            TargetCounterPlaybookGapKind.InaccessibleVerifiedOption,
            "TargetPlaybook.Gap.PlayerCannotAccessJinni",
            ["E5-004"],
            option.Code);
        var goal = Goal(
            threats: [Threat("POSITIVE_MAGIC_SOUND_MIND_DAMAGE")],
            options: [option],
            gaps: [gap]);

        Assert.Same(gap, Assert.Single(goal.KnownGaps));
        Assert.Throws<ArgumentException>(() => new TargetCounterPlaybookGap(
            "PLAYER_CANNOT_ACCESS_COUNTER",
            TargetCounterPlaybookGapKind.InaccessibleVerifiedOption,
            "TargetPlaybook.Gap.PlayerCannotAccessCounter",
            ["E5-004"]));
        Assert.Throws<ArgumentException>(() => Goal(
            threats: [Threat("POSITIVE_MAGIC_SOUND_MIND_DAMAGE")],
            options: [option],
            gaps:
            [
                new TargetCounterPlaybookGap(
                    "PLAYER_CANNOT_ACCESS_OTHER_COUNTER",
                    TargetCounterPlaybookGapKind.InaccessibleVerifiedOption,
                    "TargetPlaybook.Gap.PlayerCannotAccessOtherCounter",
                    ["E5-004"],
                    "OTHER_COUNTER")
            ]));
    }

    [Fact]
    public void Catalog_rejects_a_reconstructed_or_unregistered_rule()
    {
        var verified = Rule("REVERSE_JINNI_SUPPRESSION");
        var reconstructed = new CombatCounterRule(
            verified.Code,
            verified.ThreatCodes,
            verified.Strength,
            verified.ActivationTiming,
            verified.Effect,
            verified.Requirements,
            verified.Rationale);
        var definition = VerifiedTargetCounterPlaybooks
            .Initial
            .Archetypes
            .Single(value => value.Identity.Code
                == "MIND_RESONANCE_RESET_BASELINE");
        var playbook = new TargetCounterPlaybook(
            new TargetCounterPlaybookIdentity(
                definition.Identity,
                new TargetProfileVersion("TEST.1")),
            [Goal(
                threats: [Threat("POSITIVE_MAGIC_SOUND_MIND_DAMAGE")],
                options: [new TargetCounterPlaybookOption(
                    reconstructed,
                    [])])],
            ["E5-004"]);

        Assert.Throws<ArgumentException>(() =>
            new TargetCounterPlaybookCatalog(
                new TargetProfileVersion(
                    VerifiedCombatEffectCatalogs.GoldenGameDataVersion),
                [definition],
                [VerifiedCombatCounterRuleSets.GoldenMagicSound],
                [playbook]));
    }

    [Fact]
    public void Playbook_contract_has_no_target_identity_or_complete_loadout()
    {
        var exposedNames = new[]
        {
            typeof(TargetCounterPlaybook),
            typeof(TargetCounterPlaybookGoal),
            typeof(TargetCounterPlaybookOption),
            typeof(TargetCounterPlaybookGap)
        }
        .SelectMany(type => type.GetProperties())
        .Select(property => property.Name)
        .ToArray();

        Assert.DoesNotContain("CharacterId", exposedNames);
        Assert.DoesNotContain("TargetCharacterId", exposedNames);
        Assert.DoesNotContain("Loadout", exposedNames);
        Assert.DoesNotContain("TargetName", exposedNames);
    }

    private static TargetCounterPlaybook Playbook(string archetypeCode) =>
        VerifiedTargetCounterPlaybooks.Initial.Playbooks.Single(playbook =>
            playbook.Identity.Archetype.Code == archetypeCode);

    private static CombatCounterRule Rule(string code) =>
        VerifiedCombatCounterRuleSets.GoldenMagicSound.Rules.Single(rule =>
            rule.Code == code);

    private static TargetCounterPlaybookOption Option(string code) =>
        new(Rule(code), []);

    private static TargetThreat Threat(string code) =>
        VerifiedTargetThreatTaxonomies.GoldenMagicSound.Threats.Single(
            threat => threat.Code == code);

    private static TargetProfileFacetIdentity Facet() =>
        VerifiedTargetProfileExtractionRuleSets.Initial.OuterDamageFacet;

    private static TargetCounterPlaybookGap Gap() => new(
        "NO_VERIFIED_OPTION",
        TargetCounterPlaybookGapKind.NoVerifiedOption,
        "TargetPlaybook.Gap.NoVerifiedOption",
        ["E5-004"]);

    private static TargetCounterPlaybookGoal Goal(
        TargetProfileFacetIdentity[]? facets = null,
        TargetThreat[]? threats = null,
        TargetCounterPlaybookOption[]? options = null,
        TargetCounterPlaybookGap[]? gaps = null)
    {
        return new TargetCounterPlaybookGoal(
            "TEST_GOAL",
            10,
            TargetResponsePriority.High,
            CombatCounterActivationTiming.ActiveAttack,
            facets ?? [Facet()],
            threats ?? [],
            options ?? [],
            ["TEST_CONFLICT_GROUP"],
            ["E5-004"],
            gaps ?? (options is null or { Length: 0 } ? [Gap()] : []));
    }
}
