using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;
using Xunit;

namespace TaiWu.Domain.UnitTests.TargetArchetypes;

public sealed class TargetArchetypeMatcherTests
{
    [Fact]
    public void Definition_owns_versioned_predicates_title_and_evidence()
    {
        var required = new List<TargetArchetypeFacetPredicate>
        {
            Predicate(
                "REQUIRES_OUTER_DAMAGE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED")
        };
        var supporting = new List<TargetArchetypeFacetPredicate>
        {
            Predicate(
                "SUPPORTS_POISON",
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED")
        };
        var exclusions = new List<TargetArchetypeFacetPredicate>();
        var evidence = new List<string> { "E5-001", "E5-000" };

        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            required,
            supporting,
            exclusions,
            evidence);

        required.Clear();
        supporting.Clear();
        evidence.Clear();

        Assert.Equal("OUTER_DAMAGE_PRESSURE@1.0.0", definition.StableKey);
        Assert.Equal(ProfileRuleVersion, definition.ApplicableProfileRuleVersion.Value);
        Assert.Equal(
            "TargetArchetype.OuterDamagePressure.Title",
            definition.LocalizedTitleKey);
        Assert.Single(definition.RequiredPredicates);
        Assert.Single(definition.SupportingPredicates);
        Assert.Empty(definition.Exclusions);
        Assert.Equal(["E5-000", "E5-001"], definition.EvidenceReferences);
    }

    [Fact]
    public void Definition_rejects_invalid_predicates_and_evidence()
    {
        var required = Predicate(
            "REQUIRES_OUTER_DAMAGE",
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var duplicateCode = Predicate(
            "REQUIRES_OUTER_DAMAGE",
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        var duplicateFacet = Predicate(
            "ALSO_REQUIRES_OUTER_DAMAGE",
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");

        Assert.Throws<ArgumentException>(() => Definition(
            "OUTER_DAMAGE_PRESSURE",
            [],
            [],
            [],
            ["E5-000"]));
        Assert.Throws<ArgumentException>(() => Definition(
            "OUTER_DAMAGE_PRESSURE",
            [required],
            [duplicateCode],
            [],
            ["E5-000"]));
        Assert.Throws<ArgumentException>(() => Definition(
            "OUTER_DAMAGE_PRESSURE",
            [required],
            [],
            [duplicateFacet],
            ["E5-000"]));
        Assert.Throws<ArgumentException>(() => Definition(
            "OUTER_DAMAGE_PRESSURE",
            [required],
            [],
            [],
            []));
        Assert.Throws<ArgumentException>(() => Definition(
            "OUTER_DAMAGE_PRESSURE",
            [required],
            [],
            [],
            ["E5-000", "E5-000"]));
        Assert.Throws<ArgumentException>(() => new TargetArchetypeDefinition(
            new TargetArchetypeIdentity(
                "OUTER_DAMAGE_PRESSURE",
                Version("1.0.0")),
            Version(),
            "../localized-title",
            [required],
            [],
            [],
            ["E5-000"]));
    }

    [Fact]
    public void Predicates_enforce_operator_and_typed_value_compatibility()
    {
        var facet = Identity(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY");
        var expected = MeasuredValue(
            facet,
            outer: 1200,
            inner: 800);

        var equality = new TargetArchetypeFacetPredicate(
            "REQUIRES_OUTER_RESISTANCE_ADVANTAGE",
            facet,
            TargetArchetypePredicateOperator.ValueEquals,
            expected);

        Assert.Same(expected, equality.ExpectedValue);
        Assert.Throws<ArgumentException>(() =>
            new TargetArchetypeFacetPredicate(
                "INVALID_CONFIRMED",
                facet,
                TargetArchetypePredicateOperator.FacetConfirmed,
                expected));
        Assert.Throws<ArgumentException>(() =>
            new TargetArchetypeFacetPredicate(
                "INVALID_EQUALITY",
                facet,
                TargetArchetypePredicateOperator.ValueEquals));
        Assert.Throws<ArgumentException>(() =>
            new TargetArchetypeFacetPredicate(
                "INVALID_VALUE_DIMENSION",
                facet,
                TargetArchetypePredicateOperator.ValueEquals,
                TargetProfileFacetValue.Presence(
                    TargetProfileDimension.Control,
                    facet.Code)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetArchetypeFacetPredicate(
                "INVALID_OPERATOR",
                facet,
                (TargetArchetypePredicateOperator)99));
    }

    [Fact]
    public void Profile_can_match_multiple_archetypes_independently()
    {
        var profile = Profile(
            42,
            ConfirmedFacet(
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"),
            ConfirmedFacet(
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED"));
        var poison = Definition(
            "POISON_APPLICATION",
            [Predicate(
                "REQUIRES_POISON",
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED")]);
        var outer = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate(
                "REQUIRES_OUTER_DAMAGE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED")]);

        var result = TargetArchetypeMatcher.Match(
            profile,
            [poison, outer]);

        Assert.Equal(2, result.Matches.Length);
        Assert.Equal(
            ["OUTER_DAMAGE_PRESSURE", "POISON_APPLICATION"],
            result.Matches.Select(match => match.Definition.Identity.Code));
        Assert.All(
            result.Matches,
            match => Assert.Equal(TargetArchetypeMatchState.Matched, match.State));
        Assert.Equal(2, result.Matched.Length);
    }

    [Fact]
    public void One_archetype_matches_multiple_synthetic_targets()
    {
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate(
                "REQUIRES_OUTER_DAMAGE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED")]);
        var first = Profile(
            42,
            ConfirmedFacet(
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"));
        var second = Profile(
            99,
            ConfirmedFacet(
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"),
            ConfirmedFacet(
                TargetProfileDimension.AttackFamily,
                "WEAPON_CONTEXT"));

        var firstMatch = Assert.Single(
            TargetArchetypeMatcher.Match(first, [definition]).Matches);
        var secondMatch = Assert.Single(
            TargetArchetypeMatcher.Match(second, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Matched, firstMatch.State);
        Assert.Equal(TargetArchetypeMatchState.Matched, secondMatch.State);
        Assert.NotEqual(firstMatch.ProfileFingerprint, secondMatch.ProfileFingerprint);
    }

    [Fact]
    public void Partial_requires_support_and_missing_required_evidence()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var resistance = Identity(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY");
        var definition = Definition(
            "OUTER_DAMAGE_WITH_RESILIENCE",
            [
                Predicate("REQUIRES_OUTER_DAMAGE", outer),
                Predicate("REQUIRES_RESILIENCE", resistance)
            ]);
        var profile = Profile(42, ConfirmedFacet(outer));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Partial, match.State);
        Assert.Equal([outer], match.SupportingFacets);
        Assert.Equal([resistance], match.MissingFacets);
        Assert.Empty(match.ExcludingFacets);
        Assert.Empty(match.ConflictingFacets);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.RequiredFacetMissingCode);
    }

    [Fact]
    public void Incomplete_required_evidence_is_partial_when_another_requirement_matches()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var resistance = Identity(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY");
        var definition = Definition(
            "OUTER_DAMAGE_WITH_RESILIENCE",
            [
                Predicate("REQUIRES_OUTER_DAMAGE", outer),
                Predicate("REQUIRES_RESILIENCE", resistance)
            ]);
        var profile = Profile(
            42,
            ConfirmedFacet(outer),
            IncompleteFacet(resistance));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Partial, match.State);
        Assert.Equal([resistance], match.MissingFacets);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.RequiredFacetIncompleteCode);
    }

    [Fact]
    public void Missing_optional_support_does_not_block_match()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var poison = Identity(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate("REQUIRES_OUTER_DAMAGE", outer)],
            supporting: [Predicate("SUPPORTS_POISON", poison)]);

        var match = Assert.Single(TargetArchetypeMatcher.Match(
            Profile(42, ConfirmedFacet(outer)),
            [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Matched, match.State);
        Assert.Equal([outer], match.SupportingFacets);
        Assert.Empty(match.MissingFacets);
    }

    [Fact]
    public void Unavailable_required_facet_is_unsupported_not_not_matched()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate("REQUIRES_OUTER_DAMAGE", outer)]);
        var profile = Profile(42, UnsupportedFacet(outer));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Unsupported, match.State);
        Assert.Equal([outer], match.MissingFacets);
        Assert.Empty(match.ExcludingFacets);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.RequiredFacetUnsupportedCode);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.NoRequiredFacetAvailableCode);
    }

    [Fact]
    public void Missing_required_facets_alone_never_produce_not_matched()
    {
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate(
                "REQUIRES_OUTER_DAMAGE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED")]);

        var match = Assert.Single(TargetArchetypeMatcher.Match(
            Profile(42),
            [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Unsupported, match.State);
        Assert.Empty(match.ExcludingFacets);
        Assert.Single(match.MissingFacets);
    }

    [Fact]
    public void Profile_rule_version_mismatch_is_typed_unsupported()
    {
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate(
                "REQUIRES_OUTER_DAMAGE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED")]);
        var profile = new TargetCombatProfile(
            42,
            Version("E5.PROFILE.2"),
            [ConfirmedFacet(
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED")],
            []);

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Unsupported, match.State);
        Assert.Empty(match.SupportingFacets);
        Assert.Empty(match.MissingFacets);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.UnsupportedProfileRuleVersionCode);
    }

    [Fact]
    public void Required_value_contradiction_produces_not_matched()
    {
        var identity = Identity(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY");
        var expected = MeasuredValue(identity, outer: 1200, inner: 800);
        var actual = MeasuredValue(identity, outer: 800, inner: 1200);
        var definition = Definition(
            "OUTER_RESISTANCE_ADVANTAGE",
            [new TargetArchetypeFacetPredicate(
                "REQUIRES_OUTER_ADVANTAGE",
                identity,
                TargetArchetypePredicateOperator.ValueEquals,
                expected)]);
        var profile = Profile(42, ConfirmedFacet(identity, actual));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.NotMatched, match.State);
        Assert.Equal([identity], match.ExcludingFacets);
        Assert.Empty(match.MissingFacets);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.RequiredValueContradictedCode);
    }

    [Fact]
    public void Confirmed_explicit_exclusion_produces_not_matched()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var poison = Identity(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        var definition = Definition(
            "OUTER_WITHOUT_POISON",
            [Predicate("REQUIRES_OUTER_DAMAGE", outer)],
            exclusions: [Predicate("EXCLUDES_POISON", poison)]);
        var profile = Profile(
            42,
            ConfirmedFacet(outer),
            ConfirmedFacet(poison));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.NotMatched, match.State);
        Assert.Equal([outer], match.SupportingFacets);
        Assert.Equal([poison], match.ExcludingFacets);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.ExclusionConfirmedCode);
    }

    [Fact]
    public void Unresolved_exclusion_blocks_matched_without_becoming_negative()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var poison = Identity(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        var definition = Definition(
            "OUTER_WITHOUT_POISON",
            [Predicate("REQUIRES_OUTER_DAMAGE", outer)],
            exclusions: [Predicate("EXCLUDES_POISON", poison)]);
        var profile = Profile(42, ConfirmedFacet(outer));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Partial, match.State);
        Assert.Equal([outer], match.SupportingFacets);
        Assert.Equal([poison], match.MissingFacets);
        Assert.Empty(match.ExcludingFacets);
    }

    [Fact]
    public void Contradicted_value_exclusion_is_sufficiently_cleared()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var weapon = Identity(
            TargetProfileDimension.AttackFamily,
            "WEAPON_CONTEXT");
        var excludedValue = TargetProfileFacetValue.Measured(
            weapon.Dimension,
            weapon.Code,
            [new TargetProfileMeasurement(
                "ITEM_SUBTYPE",
                16,
                "CONFIG_CODE")]);
        var actualValue = TargetProfileFacetValue.Measured(
            weapon.Dimension,
            weapon.Code,
            [new TargetProfileMeasurement(
                "ITEM_SUBTYPE",
                17,
                "CONFIG_CODE")]);
        var definition = Definition(
            "OUTER_EXCLUDING_SUBTYPE_16",
            [Predicate("REQUIRES_OUTER_DAMAGE", outer)],
            exclusions:
            [
                new TargetArchetypeFacetPredicate(
                    "EXCLUDES_SUBTYPE_16",
                    weapon,
                    TargetArchetypePredicateOperator.ValueEquals,
                    excludedValue)
            ]);
        var profile = Profile(
            42,
            ConfirmedFacet(outer),
            ConfirmedFacet(weapon, actualValue));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Matched, match.State);
        Assert.Equal([outer], match.SupportingFacets);
        Assert.Empty(match.MissingFacets);
        Assert.Empty(match.ExcludingFacets);
    }

    [Fact]
    public void Conflicting_required_evidence_produces_conflicting_match()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate("REQUIRES_OUTER_DAMAGE", outer)]);
        var profile = Profile(42, ConflictingFacet(outer));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Conflicting, match.State);
        Assert.Equal([outer], match.ConflictingFacets);
        Assert.Empty(match.ExcludingFacets);
        Assert.Contains(
            match.Diagnostics,
            diagnostic => diagnostic.Code ==
                TargetArchetypeMatcher.PredicateEvidenceConflictingCode);
    }

    [Fact]
    public void Conflicting_supporting_evidence_remains_visible()
    {
        var outer = Identity(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var poison = Identity(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate("REQUIRES_OUTER_DAMAGE", outer)],
            supporting: [Predicate("SUPPORTS_POISON", poison)]);
        var profile = Profile(
            42,
            ConfirmedFacet(outer),
            ConflictingFacet(poison));

        var match = Assert.Single(
            TargetArchetypeMatcher.Match(profile, [definition]).Matches);

        Assert.Equal(TargetArchetypeMatchState.Conflicting, match.State);
        Assert.Equal([outer], match.SupportingFacets);
        Assert.Equal([poison], match.ConflictingFacets);
    }

    [Fact]
    public void Stable_keys_and_order_are_deterministic()
    {
        var outerPredicate = Predicate(
            "REQUIRES_OUTER_DAMAGE",
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED");
        var poisonPredicate = Predicate(
            "SUPPORTS_POISON",
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED");
        var firstDefinition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [outerPredicate],
            supporting: [poisonPredicate],
            evidence: ["E5-000", "E5-001"]);
        var reorderedDefinition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [outerPredicate],
            supporting: [poisonPredicate],
            evidence: ["E5-001", "E5-000"]);
        var other = Definition(
            "POISON_APPLICATION",
            [Predicate(
                "REQUIRES_POISON",
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED")]);
        var profile = Profile(
            42,
            ConfirmedFacet(
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED"),
            ConfirmedFacet(
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"));

        var first = TargetArchetypeMatcher.Match(
            profile,
            [other, firstDefinition]);
        var second = TargetArchetypeMatcher.Match(
            profile,
            [reorderedDefinition, other]);

        Assert.Equal(first.StableKey, second.StableKey);
        Assert.Equal(
            first.Matches.Select(match => match.StableKey),
            second.Matches.Select(match => match.StableKey));
        Assert.Equal(
            ["OUTER_DAMAGE_PRESSURE", "POISON_APPLICATION"],
            first.Matches.Select(match => match.Definition.Identity.Code));
    }

    [Fact]
    public void Matcher_allows_empty_catalogue_and_rejects_duplicates()
    {
        var profile = Profile(42);
        var definition = Definition(
            "OUTER_DAMAGE_PRESSURE",
            [Predicate(
                "REQUIRES_OUTER_DAMAGE",
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED")]);

        var empty = TargetArchetypeMatcher.Match(profile, []);

        Assert.Empty(empty.Matches);
        Assert.Empty(empty.Matched);
        Assert.Equal(64, empty.StableKey.Length);
        Assert.Throws<ArgumentException>(() => TargetArchetypeMatcher.Match(
            profile,
            [definition, definition]));
    }

    [Fact]
    public void Definition_contract_has_no_target_or_loadout_payload()
    {
        var properties = typeof(TargetArchetypeDefinition).GetProperties();

        Assert.DoesNotContain(
            properties,
            property => property.Name.Contains(
                "CharacterId",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            properties,
            property => property.Name.Contains(
                "Loadout",
                StringComparison.Ordinal));
        Assert.DoesNotContain(
            properties,
            property => property.PropertyType.FullName?.Contains(
                "GameData",
                StringComparison.Ordinal) == true);
    }

    private const string ProfileRuleVersion = "E5.PROFILE.1";

    private static TargetProfileVersion Version(
        string value = ProfileRuleVersion) => new(value);

    private static TargetProfileFacetIdentity Identity(
        TargetProfileDimension dimension,
        string code) => new(dimension, code);

    private static TargetArchetypeFacetPredicate Predicate(
        string predicateCode,
        TargetProfileDimension dimension,
        string facetCode) => Predicate(
            predicateCode,
            Identity(dimension, facetCode));

    private static TargetArchetypeFacetPredicate Predicate(
        string predicateCode,
        TargetProfileFacetIdentity facet) => new(
            predicateCode,
            facet,
            TargetArchetypePredicateOperator.FacetConfirmed);

    private static TargetArchetypeDefinition Definition(
        string code,
        IEnumerable<TargetArchetypeFacetPredicate> required,
        IEnumerable<TargetArchetypeFacetPredicate>? supporting = null,
        IEnumerable<TargetArchetypeFacetPredicate>? exclusions = null,
        IEnumerable<string>? evidence = null) => new(
            new TargetArchetypeIdentity(code, Version("1.0.0")),
            Version(),
            code == "OUTER_DAMAGE_PRESSURE"
                ? "TargetArchetype.OuterDamagePressure.Title"
                : $"TargetArchetype.{code}.Title",
            required,
            supporting ?? [],
            exclusions ?? [],
            evidence ?? ["E5-000"]);

    private static TargetCombatProfile Profile(
        int targetCharacterId,
        params TargetProfileFacet[] facets) => new(
            targetCharacterId,
            Version(),
            facets,
            []);

    private static TargetProfileFacet ConfirmedFacet(
        TargetProfileDimension dimension,
        string code) => ConfirmedFacet(Identity(dimension, code));

    private static TargetProfileFacet ConfirmedFacet(
        TargetProfileFacetIdentity identity,
        TargetProfileFacetValue? value = null) =>
        TargetProfileFacet.Confirmed(
            identity,
            value ?? TargetProfileFacetValue.Presence(
                identity.Dimension,
                identity.Code),
            [Evidence()]);

    private static TargetProfileFacet UnsupportedFacet(
        TargetProfileFacetIdentity identity) => TargetProfileFacet.Unsupported(
            identity,
            [Evidence()],
            new TargetProfileUnavailableReason("UNSAFE_RUNTIME_ONLY"));

    private static TargetProfileFacet IncompleteFacet(
        TargetProfileFacetIdentity identity) => TargetProfileFacet.Incomplete(
            identity,
            [Evidence()],
            new TargetProfileUnavailableReason("MISSING_ACTIVE_BINDING"));

    private static TargetProfileFacet ConflictingFacet(
        TargetProfileFacetIdentity identity)
    {
        var presence = new TargetProfileConflictCandidate(
            TargetProfileFacetValue.Presence(
                identity.Dimension,
                identity.Code),
            [Evidence("E5-SAVE-001")]);
        var measured = new TargetProfileConflictCandidate(
            TargetProfileFacetValue.Measured(
                identity.Dimension,
                identity.Code,
                [new TargetProfileMeasurement(
                    "RAW_VALUE",
                    1,
                    "RAW_GAME_UNIT")]),
            [Evidence("E5-SCREEN-001")]);
        return TargetProfileFacet.Conflicting(
            identity,
            [presence, measured],
            new TargetProfileUnavailableReason("CONFLICTING_EVIDENCE"));
    }

    private static TargetProfileEvidence Evidence(
        string reference = "E5-CONFIG-001") => new(
            reference,
            TargetProfileEvidenceSourceKind.InstalledConfiguration,
            "PROFILE:FIXTURE",
            Version());

    private static TargetProfileFacetValue MeasuredValue(
        TargetProfileFacetIdentity identity,
        int outer,
        int inner) => TargetProfileFacetValue.Measured(
            identity.Dimension,
            identity.Code,
            [
                new TargetProfileMeasurement(
                    "OUTER",
                    outer,
                    "RAW_GAME_UNIT"),
                new TargetProfileMeasurement(
                    "INNER",
                    inner,
                    "RAW_GAME_UNIT")
            ]);
}
