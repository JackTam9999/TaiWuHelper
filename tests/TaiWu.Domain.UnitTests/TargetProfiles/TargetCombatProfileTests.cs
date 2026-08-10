using TaiWu.Domain.TargetProfiles;
using Xunit;

namespace TaiWu.Domain.UnitTests.TargetProfiles;

public sealed class TargetCombatProfileTests
{
    [Fact]
    public void Dimensions_are_independent_profile_axes()
    {
        Assert.Equal(
            [
                TargetProfileDimension.AttackFamily,
                TargetProfileDimension.Pressure,
                TargetProfileDimension.Resilience,
                TargetProfileDimension.Control,
                TargetProfileDimension.Tempo
            ],
            Enum.GetValues<TargetProfileDimension>());
    }

    [Fact]
    public void Confirmed_facet_owns_typed_value_evidence_and_version()
    {
        var measurements = new List<TargetProfileMeasurement>
        {
            new("OUTER", 1200, "RAW_GAME_UNIT"),
            new("INNER", 800, "RAW_GAME_UNIT")
        };
        var evidence = new List<TargetProfileEvidence>
        {
            Evidence(
                "E5-CONFIG-001",
                TargetProfileEvidenceSourceKind.InstalledConfiguration,
                "SKILL:456"),
            Evidence(
                "E5-SAVE-001",
                TargetProfileEvidenceSourceKind.SavedEquippedMembership,
                "TARGET:42")
        };
        var identity = Identity(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY");
        var value = TargetProfileFacetValue.Measured(
            identity.Dimension,
            identity.Code,
            measurements);
        var facet = TargetProfileFacet.Confirmed(
            identity,
            value,
            evidence);

        measurements.Clear();
        evidence.Clear();

        Assert.Equal(TargetProfileEvidenceState.Confirmed, facet.State);
        Assert.Same(value, facet.Value);
        Assert.Null(facet.UnavailableReason);
        Assert.Empty(facet.ConflictCandidates);
        Assert.Equal(["INNER", "OUTER"], facet.Value!.Measurements
            .Select(item => item.Code));
        Assert.Equal(2, facet.Evidence.Length);
        Assert.All(
            facet.Evidence,
            item => Assert.Equal(GameDataVersion, item.SourceVersion.Value));
    }

    [Fact]
    public void Incomplete_and_unsupported_facets_have_no_authoritative_value()
    {
        var evidence = new[] { Evidence() };
        var identity = Identity();
        var missing = new TargetProfileUnavailableReason(
            "MISSING_ACTIVE_BINDING",
            "No positive equipped membership was available.");
        var unsafeRuntime = new TargetProfileUnavailableReason(
            "UNSAFE_RUNTIME_ONLY");

        var incomplete = TargetProfileFacet.Incomplete(
            identity,
            evidence,
            missing);
        var unsupported = TargetProfileFacet.Unsupported(
            identity,
            evidence,
            unsafeRuntime);

        Assert.Equal(TargetProfileEvidenceState.Incomplete, incomplete.State);
        Assert.Null(incomplete.Value);
        Assert.Equal("MISSING_ACTIVE_BINDING", incomplete.UnavailableReason!.Code);
        Assert.Equal(TargetProfileEvidenceState.Unsupported, unsupported.State);
        Assert.Null(unsupported.Value);
        Assert.Equal("UNSAFE_RUNTIME_ONLY", unsupported.UnavailableReason!.Code);
    }

    [Fact]
    public void Conflicting_facet_preserves_distinct_typed_candidates()
    {
        var identity = Identity(
            TargetProfileDimension.AttackFamily,
            "WEAPON_CONTEXT");
        var first = new TargetProfileConflictCandidate(
            TargetProfileFacetValue.Presence(
                TargetProfileDimension.AttackFamily,
                "WEAPON_CONTEXT"),
            [Evidence("E5-SAVE-001")]);
        var second = new TargetProfileConflictCandidate(
            TargetProfileFacetValue.Measured(
                TargetProfileDimension.AttackFamily,
                "WEAPON_CONTEXT",
                [new TargetProfileMeasurement(
                    "ITEM_SUBTYPE",
                    16,
                    "CONFIG_CODE")]),
            [Evidence(
                "E5-SCREEN-001",
                TargetProfileEvidenceSourceKind.CurrentScreenObservation,
                "OBSERVATION:E5-SCREEN-001")]);

        var facet = TargetProfileFacet.Conflicting(
            identity,
            [second, first],
            new TargetProfileUnavailableReason("CONFLICTING_EVIDENCE"));

        Assert.Equal(TargetProfileEvidenceState.Conflicting, facet.State);
        Assert.Null(facet.Value);
        Assert.Equal(2, facet.ConflictCandidates.Length);
        Assert.Equal(2, facet.Evidence.Length);
        Assert.Equal(
            [
                TargetProfileFacetValueKind.Presence,
                TargetProfileFacetValueKind.Measurements
            ],
            facet.ConflictCandidates.Select(item => item.Value.Kind));
    }

    [Fact]
    public void Missing_evidence_and_zero_cannot_become_confirmed_values()
    {
        var identity = Identity();
        var value = TargetProfileFacetValue.Presence(
            identity.Dimension,
            identity.Code);

        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacet.Confirmed(identity, value, []));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetProfileMeasurement("OUTER", 0, "RAW_GAME_UNIT"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetProfileMeasurement("OUTER", -1, "RAW_GAME_UNIT"));
    }

    [Fact]
    public void Conflict_requires_two_distinct_compatible_values()
    {
        var identity = Identity();
        var value = TargetProfileFacetValue.Presence(
            identity.Dimension,
            identity.Code);
        var candidate = new TargetProfileConflictCandidate(
            value,
            [Evidence()]);
        var wrongDimension = new TargetProfileConflictCandidate(
            TargetProfileFacetValue.Presence(
                TargetProfileDimension.Control,
                identity.Code),
            [Evidence("E5-OTHER-001")]);
        var reason = new TargetProfileUnavailableReason(
            "CONFLICTING_EVIDENCE");

        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacet.Conflicting(identity, [candidate], reason));
        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacet.Conflicting(
                identity,
                [candidate, candidate],
                reason));
        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacet.Conflicting(
                identity,
                [candidate, wrongDimension],
                reason));
    }

    [Fact]
    public void Facet_rejects_incompatible_value_dimension_or_code()
    {
        var identity = Identity();

        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacet.Confirmed(
                identity,
                TargetProfileFacetValue.Presence(
                    TargetProfileDimension.Control,
                    identity.Code),
                [Evidence()]));
        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacet.Confirmed(
                identity,
                TargetProfileFacetValue.Presence(
                    identity.Dimension,
                    "POISON_APPLICATION_CONFIGURED"),
                [Evidence()]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("lower_case")]
    [InlineData("LOCAL/PATH")]
    [InlineData("LOCAL\\PATH")]
    public void Stable_codes_reject_blank_localized_or_path_values(string code)
    {
        Assert.Throws<ArgumentException>(() =>
            new TargetProfileFacetIdentity(
                TargetProfileDimension.Pressure,
                code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\game\\GameData.dll")]
    [InlineData("../GameData.dll")]
    public void Source_versions_reject_blank_or_path_values(string version)
    {
        Assert.Throws<ArgumentException>(() =>
            new TargetProfileVersion(version));
    }

    [Fact]
    public void Evidence_rejects_blank_duplicate_and_invalid_source_values()
    {
        Assert.Throws<ArgumentException>(() => new TargetProfileEvidence(
            " ",
            TargetProfileEvidenceSourceKind.InstalledConfiguration,
            "SKILL:456",
            Version()));
        Assert.Throws<ArgumentException>(() => new TargetProfileEvidence(
            "E5-CONFIG-001",
            TargetProfileEvidenceSourceKind.InstalledConfiguration,
            " ",
            Version()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetProfileEvidence(
                "E5-CONFIG-001",
                (TargetProfileEvidenceSourceKind)99,
                "SKILL:456",
                Version()));
        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacet.Confirmed(
                Identity(),
                Value(),
                [Evidence(), Evidence()]));
    }

    [Fact]
    public void Invalid_enum_values_fail_construction()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetProfileFacetIdentity(
                (TargetProfileDimension)99,
                "OUTER_DAMAGE_CONFIGURED"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TargetProfileFacetValue.Presence(
                (TargetProfileDimension)99,
                "OUTER_DAMAGE_CONFIGURED"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetProfileDiagnostic(
                "UNAVAILABLE_FIELD",
                (TargetProfileDiagnosticSeverity)99,
                facet: null));
    }

    [Fact]
    public void Measurements_are_positive_unique_and_immutable()
    {
        var measurements = new List<TargetProfileMeasurement>
        {
            new("OUTER", 1200, "RAW_GAME_UNIT")
        };
        var value = TargetProfileFacetValue.Measured(
            TargetProfileDimension.Resilience,
            "CHANNEL_RESISTANCE_ASYMMETRY",
            measurements);

        measurements.Add(new TargetProfileMeasurement(
            "INNER",
            800,
            "RAW_GAME_UNIT"));

        Assert.Single(value.Measurements);
        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacetValue.Measured(
                TargetProfileDimension.Resilience,
                "CHANNEL_RESISTANCE_ASYMMETRY",
                [
                    new TargetProfileMeasurement(
                        "OUTER",
                        1200,
                        "RAW_GAME_UNIT"),
                    new TargetProfileMeasurement(
                        "OUTER",
                        800,
                        "RAW_GAME_UNIT")
                ]));
        Assert.Throws<ArgumentException>(() =>
            TargetProfileFacetValue.Measured(
                TargetProfileDimension.Resilience,
                "CHANNEL_RESISTANCE_ASYMMETRY",
                []));
    }

    [Fact]
    public void Profile_allows_empty_facts_and_has_stable_identity()
    {
        var profile = new TargetCombatProfile(
            42,
            Version("E5.PROFILE.1"),
            [],
            []);

        Assert.Empty(profile.Facets);
        Assert.Empty(profile.Diagnostics);
        Assert.Equal(64, profile.Fingerprint.Length);
        Assert.Matches("^[0-9A-F]{64}$", profile.Fingerprint);
    }

    [Fact]
    public void Profile_copies_sorts_and_rejects_duplicate_facets()
    {
        var pressure = ConfirmedFacet(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED",
            "E5-OUTER-001");
        var attackFamily = ConfirmedFacet(
            TargetProfileDimension.AttackFamily,
            "WEAPON_CONTEXT",
            "E5-WEAPON-001");
        var facets = new List<TargetProfileFacet> { pressure, attackFamily };
        var profile = new TargetCombatProfile(42, Version(), facets, []);

        facets.Clear();

        Assert.Equal(
            [
                TargetProfileDimension.AttackFamily,
                TargetProfileDimension.Pressure
            ],
            profile.Facets.Select(item => item.Identity.Dimension));
        Assert.Same(
            pressure,
            profile.FindFacet(
                TargetProfileDimension.Pressure,
                "OUTER_DAMAGE_CONFIGURED"));
        Assert.Throws<ArgumentException>(() => new TargetCombatProfile(
            42,
            Version(),
            [pressure, pressure],
            []));
    }

    [Fact]
    public void Diagnostics_are_typed_sorted_immutable_and_unique()
    {
        var references = new List<string> { "E5-SAVE-002", "E5-SAVE-001" };
        var warning = new TargetProfileDiagnostic(
            "MISSING_ACTIVE_BINDING",
            TargetProfileDiagnosticSeverity.Warning,
            Identity(),
            references);
        var information = new TargetProfileDiagnostic(
            "EMPTY_PROFILE",
            TargetProfileDiagnosticSeverity.Information,
            facet: null);
        var diagnostics = new List<TargetProfileDiagnostic>
        {
            warning,
            information
        };
        var profile = new TargetCombatProfile(
            42,
            Version(),
            [],
            diagnostics);

        references.Clear();
        diagnostics.Clear();

        Assert.Equal(
            ["E5-SAVE-001", "E5-SAVE-002"],
            warning.EvidenceReferences);
        Assert.Equal(
            ["EMPTY_PROFILE", "MISSING_ACTIVE_BINDING"],
            profile.Diagnostics.Select(item => item.Code));
        Assert.Throws<ArgumentException>(() => new TargetCombatProfile(
            42,
            Version(),
            [],
            [warning, warning]));
        Assert.Throws<ArgumentException>(() => new TargetProfileDiagnostic(
            "DUPLICATE_REFERENCE",
            TargetProfileDiagnosticSeverity.Warning,
            facet: null,
            ["E5-SAVE-001", "E5-SAVE-001"]));
    }

    [Fact]
    public void Fingerprint_is_deterministic_for_reordered_inputs()
    {
        var pressure = ConfirmedFacet(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED",
            "E5-OUTER-001",
            reverseEvidence: false);
        var reorderedPressure = ConfirmedFacet(
            TargetProfileDimension.Pressure,
            "OUTER_DAMAGE_CONFIGURED",
            "E5-OUTER-001",
            reverseEvidence: true);
        var control = ConfirmedFacet(
            TargetProfileDimension.Control,
            "POISON_APPLICATION_CONFIGURED",
            "E5-POISON-001");
        var diagnostic = new TargetProfileDiagnostic(
            "PROFILE_DISCOVERY_COMPLETE",
            TargetProfileDiagnosticSeverity.Information,
            facet: null,
            ["E5-POISON-001", "E5-OUTER-001"]);
        var reorderedDiagnostic = new TargetProfileDiagnostic(
            "PROFILE_DISCOVERY_COMPLETE",
            TargetProfileDiagnosticSeverity.Information,
            facet: null,
            ["E5-OUTER-001", "E5-POISON-001"]);

        var first = new TargetCombatProfile(
            42,
            Version(),
            [pressure, control],
            [diagnostic]);
        var second = new TargetCombatProfile(
            42,
            Version(),
            [control, reorderedPressure],
            [reorderedDiagnostic]);

        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public void Fingerprint_changes_when_stable_facts_change()
    {
        var facet = ConfirmedFacet();
        var baseline = new TargetCombatProfile(42, Version(), [facet], []);
        var targetChanged = new TargetCombatProfile(43, Version(), [facet], []);
        var versionChanged = new TargetCombatProfile(
            42,
            Version("E5.PROFILE.2"),
            [facet],
            []);
        var facetChanged = new TargetCombatProfile(
            42,
            Version(),
            [ConfirmedFacet(
                TargetProfileDimension.Control,
                "POISON_APPLICATION_CONFIGURED",
                "E5-POISON-001")],
            []);

        Assert.NotEqual(baseline.Fingerprint, targetChanged.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, versionChanged.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, facetChanged.Fingerprint);
    }

    [Fact]
    public void Fingerprint_excludes_unavailable_detail_and_local_path_text()
    {
        var identity = Identity();
        var evidence = new[] { Evidence() };
        var first = TargetProfileFacet.Incomplete(
            identity,
            evidence,
            new TargetProfileUnavailableReason(
                "MISSING_ACTIVE_BINDING",
                "No equipped membership was present."));
        var second = TargetProfileFacet.Incomplete(
            identity,
            evidence,
            new TargetProfileUnavailableReason(
                "MISSING_ACTIVE_BINDING",
                @"Diagnostic detail from C:\local\save.sav."));

        var firstProfile = new TargetCombatProfile(
            42,
            Version(),
            [first],
            []);
        var secondProfile = new TargetCombatProfile(
            42,
            Version(),
            [second],
            []);

        Assert.Equal(firstProfile.Fingerprint, secondProfile.Fingerprint);
        Assert.DoesNotContain("local", firstProfile.Fingerprint);
    }

    [Fact]
    public void Profile_rejects_invalid_target_and_lookup_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TargetCombatProfile(0, Version(), [], []));
        var profile = new TargetCombatProfile(42, Version(), [], []);
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.FindFacet(
            (TargetProfileDimension)99,
            "OUTER_DAMAGE_CONFIGURED"));
        Assert.Throws<ArgumentException>(() => profile.FindFacet(
            TargetProfileDimension.Pressure,
            " "));
    }

    private const string GameDataVersion =
        "1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a";

    private static TargetProfileVersion Version(
        string value = GameDataVersion) => new(value);

    private static TargetProfileFacetIdentity Identity(
        TargetProfileDimension dimension = TargetProfileDimension.Pressure,
        string code = "OUTER_DAMAGE_CONFIGURED") => new(dimension, code);

    private static TargetProfileEvidence Evidence(
        string reference = "E5-CONFIG-001",
        TargetProfileEvidenceSourceKind sourceKind =
            TargetProfileEvidenceSourceKind.InstalledConfiguration,
        string sourceIdentity = "SKILL:456") => new(
            reference,
            sourceKind,
            sourceIdentity,
            Version());

    private static TargetProfileFacetValue Value(
        TargetProfileDimension dimension = TargetProfileDimension.Pressure,
        string code = "OUTER_DAMAGE_CONFIGURED") =>
        TargetProfileFacetValue.Presence(dimension, code);

    private static TargetProfileFacet ConfirmedFacet(
        TargetProfileDimension dimension = TargetProfileDimension.Pressure,
        string code = "OUTER_DAMAGE_CONFIGURED",
        string reference = "E5-OUTER-001",
        bool reverseEvidence = false)
    {
        var evidence = new[]
        {
            Evidence(
                reference,
                TargetProfileEvidenceSourceKind.SavedEquippedMembership,
                "TARGET:42"),
            Evidence(
                "E5-CONFIG-001",
                TargetProfileEvidenceSourceKind.InstalledConfiguration,
                "SKILL:456")
        };
        return TargetProfileFacet.Confirmed(
            Identity(dimension, code),
            Value(dimension, code),
            reverseEvidence ? evidence.Reverse() : evidence);
    }
}
