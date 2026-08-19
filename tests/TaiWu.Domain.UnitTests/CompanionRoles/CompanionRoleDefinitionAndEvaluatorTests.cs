using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using Xunit;

namespace TaiWu.Domain.UnitTests.CompanionRoles;

public sealed class CompanionRoleDefinitionAndEvaluatorTests
{
    [Fact]
    public void Verified_catalogue_exposes_four_stable_nonlocalized_role_definitions()
    {
        var roles = VerifiedCompanionRoleDefinitions.All;

        Assert.Equal(4, roles.Length);
        Assert.Equal(
            [
                "COMPREHENSIVE_BASE_CAPABILITY",
                "SUCCESSION_CANDIDATE_READINESS",
                "MARTIAL_DISCIPLINE_APTITUDE",
                "LIFE_SKILL_DISCIPLINE_APTITUDE"
            ],
            roles.Select(item => item.Identity.Value));
        Assert.All(roles, role =>
        {
            Assert.Equal("1", role.RoleVersion);
            Assert.Equal("1", role.EvaluationRuleVersion);
            Assert.Equal([GameDataVersion], role.SupportedGameDataVersions);
            Assert.Matches("^[0-9A-F]{64}$", role.Fingerprint);
        });
    }

    [Fact]
    public void Verified_roles_declare_different_typed_requirements()
    {
        var martial = VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude;
        var life = VerifiedCompanionRoleDefinitions.LifeSkillDisciplineAptitude;

        Assert.Equal(CandidateDisciplineDomain.Martial, martial.DisciplineDomain);
        Assert.Equal((short)13, martial.MaximumDisciplineType);
        Assert.Equal(
            CandidateProfileField.BaseMartialQualification,
            martial.ScoreDimensions.Single().Field);
        Assert.Equal(CandidateDisciplineDomain.LifeSkill, life.DisciplineDomain);
        Assert.Equal((short)15, life.MaximumDisciplineType);
        Assert.Equal(
            CandidateProfileField.BaseLifeSkillQualification,
            life.ScoreDimensions.Single().Field);
        Assert.Equal(
            [
                CompanionRoleRequirementKind.CandidateUniverseEligible,
                CompanionRoleRequirementKind.SourceVersionsSupported,
                CompanionRoleRequirementKind.DisciplineSupported,
                CompanionRoleRequirementKind.RequiredFactConfirmed,
                CompanionRoleRequirementKind.FactProvenanceCompatible
            ],
            martial.HardRequirements.Select(item => item.Kind));

        var capability = VerifiedCompanionRoleDefinitions
            .ComprehensiveBaseCapability;
        Assert.Equal(CandidateDisciplineDomain.Capability, capability.DisciplineDomain);
        Assert.False(capability.RequiresDisciplineSelection);
        Assert.Equal((short)0, capability.MinimumDisciplineType);
        Assert.Equal((short)0, capability.MaximumDisciplineType);
        var dimension = Assert.Single(capability.ScoreDimensions);
        Assert.Equal(CandidateProfileField.CapabilityBreadthIndex, dimension.Field);
        Assert.Equal(CompanionRoleNormalizationKind.Hundredth, dimension.Normalization);
        Assert.Equal(
            CompanionRoleRequirementKind.ObjectiveSupported,
            capability.HardRequirements[2].Kind);

        var succession = VerifiedCompanionRoleDefinitions
            .SuccessionCandidateReadiness;
        Assert.False(succession.RequiresDisciplineSelection);
        Assert.Equal(
            [
                CandidateProfileField.CapabilityBreadthIndex,
                CandidateProfileField.CurrentAge
            ],
            succession.ScoreDimensions.Select(item => item.Field));
        Assert.All(
            succession.HardRequirements.Where(item => item.Order == 3),
            requirement => Assert.Equal(
                CompanionRoleRequirementKind.ObjectiveSupported,
                requirement.Kind));
    }

    [Fact]
    public void Succession_objective_combines_saved_breadth_and_current_age_transparently()
    {
        var definition = VerifiedCompanionRoleDefinitions
            .SuccessionCandidateReadiness;
        var younger = CompanionRoleEvaluator.Evaluate(
            definition,
            Profile(
                characterId: 42,
                facts: CapabilityFacts(mainValue: 60, martialValue: 60, lifeValue: 60)
                    .Append(AgeFact(20))),
            CapabilityObjective());
        var older = CompanionRoleEvaluator.Evaluate(
            definition,
            Profile(
                characterId: 43,
                facts: CapabilityFacts(mainValue: 60, martialValue: 60, lifeValue: 60)
                    .Append(AgeFact(40))),
            CapabilityObjective());

        Assert.Equal(CompanionRoleEvaluationState.Rankable, younger.State);
        Assert.Equal(40m, younger.TotalScore);
        Assert.Equal(20m, older.TotalScore);
        Assert.Equal(
            [60m, -20m],
            younger.Components.Select(item => item.Contribution));
        Assert.Equal(
            CompanionRoleMeritComparison.FirstPreferred,
            CompanionRoleMeritComparer.Compare(younger, older));
    }

    [Fact]
    public void Comprehensive_objective_scores_and_ranks_complete_capability_breadth()
    {
        var definition = VerifiedCompanionRoleDefinitions
            .ComprehensiveBaseCapability;
        var objective = CapabilityObjective();
        var lower = Profile(
            characterId: 42,
            facts: CapabilityFacts(mainValue: 60, martialValue: 30, lifeValue: 90));
        var higher = Profile(
            characterId: 43,
            facts: CapabilityFacts(mainValue: 75, martialValue: 60, lifeValue: 90));

        var evaluation = CompanionRoleEvaluator.Evaluate(
            definition,
            lower,
            objective);
        var ranking = CompanionRoleShortlistBuilder.EvaluateAndRank(
            definition,
            objective,
            [lower, higher],
            TestContext.Current.CancellationToken);

        Assert.Equal(CompanionRoleEvaluationState.Rankable, evaluation.State);
        Assert.All(evaluation.Gates, gate =>
            Assert.Equal(CompanionRoleGateOutcome.Passed, gate.Outcome));
        var component = Assert.Single(evaluation.Components);
        Assert.Equal((short)6000, component.RawValue);
        Assert.Equal(60m, component.NormalizedValue);
        Assert.Equal(60m, component.Contribution);
        Assert.Equal(60m, evaluation.TotalScore);
        Assert.Equal([43, 42], ranking.RankedCandidates.Select(candidate =>
            candidate.Evaluation.Profile.Identity.CharacterId));
        Assert.Equal([75m, 60m], ranking.RankedCandidates.Select(candidate =>
            candidate.Evaluation.TotalScore));
    }

    [Fact]
    public void Comprehensive_objective_never_turns_missing_capability_into_zero()
    {
        var facts = CapabilityFacts().ToList();
        facts.RemoveAt(0);

        var evaluation = CompanionRoleEvaluator.Evaluate(
            VerifiedCompanionRoleDefinitions.ComprehensiveBaseCapability,
            Profile(facts: facts),
            CapabilityObjective());

        Assert.Equal(CompanionRoleEvaluationState.Incomplete, evaluation.State);
        Assert.Equal(
            CompanionRoleGateOutcome.Incomplete,
            evaluation.Gates[^1].Outcome);
        Assert.Null(evaluation.TotalScore);
        Assert.Empty(evaluation.Components);
    }

    [Fact]
    public void Comprehensive_objective_requires_every_component_to_match_profile_source()
    {
        var facts = CapabilityFacts().ToList();
        var original = facts[0];
        facts[0] = CandidateProfileFact.Confirmed(
            original.Identity,
            original.Value!,
            SaveProvenance(revision: OtherSha),
            [Evidence(revision: OtherSha)]);

        var evaluation = CompanionRoleEvaluator.Evaluate(
            VerifiedCompanionRoleDefinitions.ComprehensiveBaseCapability,
            Profile(facts: facts),
            CapabilityObjective());

        Assert.Equal(CompanionRoleEvaluationState.Conflicting, evaluation.State);
        Assert.Equal(
            "CAPABILITY_PROVENANCE_CONFLICTS_WITH_PROFILE",
            evaluation.Gates[^1].ReasonIdentity);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Comprehensive_objective_rejects_out_of_range_breadth_without_throwing()
    {
        var evaluation = CompanionRoleEvaluator.Evaluate(
            VerifiedCompanionRoleDefinitions.ComprehensiveBaseCapability,
            Profile(facts: CapabilityFacts(
                mainValue: 400,
                martialValue: 400,
                lifeValue: 400)),
            CapabilityObjective());

        Assert.Equal(CompanionRoleEvaluationState.Conflicting, evaluation.State);
        Assert.Equal(
            "FACT_OUTSIDE_NORMALIZATION_RANGE",
            evaluation.Gates[^1].ReasonIdentity);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Definition_resolution_fails_closed_for_unknown_role_or_version()
    {
        var supported = VerifiedCompanionRoleDefinitions.Resolve(
            new CompanionRoleIdentity("MARTIAL_DISCIPLINE_APTITUDE"),
            "1");
        var unknown = VerifiedCompanionRoleDefinitions.Resolve(
            new CompanionRoleIdentity("UNKNOWN_ROLE"),
            "1");
        var unsupported = VerifiedCompanionRoleDefinitions.Resolve(
            new CompanionRoleIdentity("MARTIAL_DISCIPLINE_APTITUDE"),
            "2");

        Assert.Equal(CompanionRoleDefinitionResolutionState.Supported, supported.State);
        Assert.NotNull(supported.Definition);
        Assert.Equal(CompanionRoleDefinitionResolutionState.UnknownIdentity, unknown.State);
        Assert.Null(unknown.Definition);
        Assert.Equal("ROLE_IDENTITY_UNKNOWN", unknown.DiagnosticIdentity);
        Assert.Equal(CompanionRoleDefinitionResolutionState.UnsupportedVersion, unsupported.State);
        Assert.Null(unsupported.Definition);
        Assert.Equal("ROLE_VERSION_UNSUPPORTED", unsupported.DiagnosticIdentity);
    }

    [Fact]
    public void Score_dimension_rejects_invalid_weight_range_and_enums()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Dimension(weight: 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dimension(weight: -1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Dimension(weight: 1_001m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompanionRoleScoreDimension(
            "BASE_MARTIAL_QUALIFICATION",
            CandidateProfileField.BaseMartialQualification,
            "BASE_QUALIFICATION_POINT",
            CompanionRoleScoreDirection.HigherIsBetter,
            CompanionRoleNormalizationKind.Identity,
            normalizationMinimum: 10,
            normalizationMaximum: 0,
            weight: 1,
            CompanionRoleMissingEvidenceBehavior.EvaluationIncomplete,
            "BASE_MARTIAL_QUALIFICATION_EXACT_SAVED_VALUE"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompanionRoleScoreDimension(
            "BASE_MARTIAL_QUALIFICATION",
            CandidateProfileField.BaseMartialQualification,
            "BASE_QUALIFICATION_POINT",
            (CompanionRoleScoreDirection)99,
            CompanionRoleNormalizationKind.Identity,
            short.MinValue,
            short.MaxValue,
            1,
            CompanionRoleMissingEvidenceBehavior.EvaluationIncomplete,
            "BASE_MARTIAL_QUALIFICATION_EXACT_SAVED_VALUE"));
    }

    [Fact]
    public void Definition_copies_sorts_versions_and_has_deterministic_identity()
    {
        var versions = new List<string> { "VERSION-B", GameDataVersion };
        var first = Definition(supportedVersions: versions);
        versions.Clear();
        var second = Definition(supportedVersions: [GameDataVersion, "VERSION-B"]);

        Assert.Equal([GameDataVersion, "VERSION-B"], first.SupportedGameDataVersions);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal(64, first.Fingerprint.Length);
    }

    [Fact]
    public void Definition_rejects_empty_duplicate_or_wrong_domain_rules()
    {
        Assert.Throws<ArgumentException>(() => Definition(supportedVersions: []));
        Assert.Throws<ArgumentException>(() => Definition(
            supportedVersions: [GameDataVersion, GameDataVersion]));
        Assert.Throws<ArgumentException>(() => Definition(dimensions: []));
        var dimension = Dimension();
        Assert.Throws<ArgumentException>(() => Definition(
            dimensions: [dimension, dimension]));
        Assert.Throws<ArgumentException>(() => Definition(
            dimensions: [new CompanionRoleScoreDimension(
                "BASE_LIFE_QUALIFICATION",
                CandidateProfileField.BaseLifeSkillQualification,
                "BASE_QUALIFICATION_POINT",
                CompanionRoleScoreDirection.HigherIsBetter,
                CompanionRoleNormalizationKind.Identity,
                short.MinValue,
                short.MaxValue,
                1,
                CompanionRoleMissingEvidenceBehavior.EvaluationIncomplete,
                "BASE_LIFE_QUALIFICATION_EXACT_SAVED_VALUE")]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompanionRoleDefinition(
            new CompanionRoleIdentity("BAD_RANGE"),
            "1",
            "1",
            [GameDataVersion],
            "1",
            "1",
            CandidateDisciplineDomain.Martial,
            3,
            2,
            [dimension],
            CompanionRoleTiePolicy.ExactTotalRemainsTie));
    }

    [Fact]
    public void Valid_evaluation_applies_ordered_gates_before_transparent_score()
    {
        var profile = Profile(facts: [MartialFact(73)]);
        var evaluation = Evaluate(profile);

        Assert.Equal(CompanionRoleEvaluationState.Rankable, evaluation.State);
        Assert.Equal(5, evaluation.Gates.Length);
        Assert.All(evaluation.Gates, gate =>
            Assert.Equal(CompanionRoleGateOutcome.Passed, gate.Outcome));
        var component = Assert.Single(evaluation.Components);
        Assert.Equal("BASE_MARTIAL_QUALIFICATION", component.Dimension.Identity);
        Assert.Equal((short)73, component.RawValue);
        Assert.Equal(73m, component.NormalizedValue);
        Assert.Equal(1m, component.Weight);
        Assert.Equal(73m, component.Contribution);
        Assert.Equal(73m, evaluation.TotalScore);
        Assert.Equal("E6-SAVE-001", Assert.Single(component.Evidence).Reference);
    }

    [Fact]
    public void Both_verified_roles_evaluate_different_typed_facts_on_one_profile()
    {
        var profile = Profile(facts: [MartialFact(73), LifeFact(61)]);
        var martial = Evaluate(profile);
        var life = CompanionRoleEvaluator.Evaluate(
            VerifiedCompanionRoleDefinitions.LifeSkillDisciplineAptitude,
            profile,
            new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, 0));

        Assert.Equal(73m, martial.TotalScore);
        Assert.Equal(61m, life.TotalScore);
        Assert.Equal(
            CandidateProfileField.BaseMartialQualification,
            martial.Components.Single().Field.Field);
        Assert.Equal(
            CandidateProfileField.BaseLifeSkillQualification,
            life.Components.Single().Field.Field);
        Assert.Equal(
            CompanionRoleMeritComparison.NotComparable,
            CompanionRoleMeritComparer.Compare(martial, life));
    }

    [Fact]
    public void Ineligible_universe_stops_before_hard_requirements_and_scoring()
    {
        var evaluation = Evaluate(Profile(
            state: CandidateUniverseState.Ineligible,
            facts: [MartialFact(99)]));

        Assert.Equal(CompanionRoleEvaluationState.Ineligible, evaluation.State);
        Assert.Equal(CompanionRoleGateOutcome.Failed, Assert.Single(evaluation.Gates).Outcome);
        Assert.Empty(evaluation.Components);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Missing_required_field_is_incomplete_without_numeric_penalty()
    {
        var evaluation = Evaluate(Profile(facts: []));

        Assert.Equal(CompanionRoleEvaluationState.Incomplete, evaluation.State);
        Assert.Equal(4, evaluation.Gates.Length);
        Assert.Equal(CompanionRoleGateOutcome.Incomplete, evaluation.Gates[^1].Outcome);
        Assert.Equal("REQUIRED_FACT_MISSING", evaluation.Gates[^1].ReasonIdentity);
        Assert.Empty(evaluation.Components);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Unsupported_source_version_fails_closed_before_discipline_or_score()
    {
        var evaluation = Evaluate(Profile(
            gameDataVersion: "UNSUPPORTED-VERSION",
            facts: [MartialFact(73)]));

        Assert.Equal(CompanionRoleEvaluationState.Unsupported, evaluation.State);
        Assert.Equal(2, evaluation.Gates.Length);
        Assert.Equal(CompanionRoleGateOutcome.Unsupported, evaluation.Gates[^1].Outcome);
        Assert.Equal("SOURCE_VERSIONS_UNSUPPORTED", evaluation.Gates[^1].ReasonIdentity);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Unsupported_discipline_fails_before_fact_lookup()
    {
        var wrongDomain = CompanionRoleEvaluator.Evaluate(
            VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude,
            Profile(facts: [MartialFact(73)]),
            new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, 0));
        var outOfRange = Evaluate(
            Profile(facts: [MartialFact(73)]),
            disciplineType: 14);

        Assert.Equal(CompanionRoleEvaluationState.Unsupported, wrongDomain.State);
        Assert.Equal(3, wrongDomain.Gates.Length);
        Assert.Equal(CompanionRoleEvaluationState.Unsupported, outOfRange.State);
        Assert.Equal("DISCIPLINE_UNSUPPORTED", outOfRange.Gates[^1].ReasonIdentity);
    }

    [Theory]
    [InlineData(CandidateEvidenceState.Incomplete, CompanionRoleEvaluationState.Incomplete)]
    [InlineData(CandidateEvidenceState.Unsupported, CompanionRoleEvaluationState.Unsupported)]
    [InlineData(CandidateEvidenceState.Stale, CompanionRoleEvaluationState.Incomplete)]
    [InlineData(CandidateEvidenceState.Conflicting, CompanionRoleEvaluationState.Conflicting)]
    public void Nonconfirmed_required_evidence_never_produces_a_score(
        CandidateEvidenceState evidenceState,
        CompanionRoleEvaluationState expectedState)
    {
        var evaluation = Evaluate(Profile(facts: [MartialFact(evidenceState)]));

        Assert.Equal(expectedState, evaluation.State);
        Assert.Empty(evaluation.Components);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Mismatched_fact_provenance_is_conflicting_not_rankable()
    {
        var fact = CandidateProfileFact.Confirmed(
            MartialField(),
            CandidateFactValue.Int16(73),
            SaveProvenance(revision: OtherSha),
            [Evidence(revision: OtherSha)]);
        var evaluation = Evaluate(Profile(facts: [fact]));

        Assert.Equal(CompanionRoleEvaluationState.Conflicting, evaluation.State);
        Assert.Equal(CompanionRoleGateOutcome.Conflicting, evaluation.Gates[^1].Outcome);
        Assert.Equal("FACT_PROVENANCE_CONFLICTS_WITH_PROFILE", evaluation.Gates[^1].ReasonIdentity);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Confirmed_value_outside_declared_range_is_conflicting()
    {
        var definition = Definition(dimensions: [Dimension(minimum: 0, maximum: 50)]);
        var evaluation = CompanionRoleEvaluator.Evaluate(
            definition,
            Profile(facts: [MartialFact(51)]),
            MartialDiscipline());

        Assert.Equal(CompanionRoleEvaluationState.Conflicting, evaluation.State);
        Assert.Equal("FACT_OUTSIDE_NORMALIZATION_RANGE", evaluation.Gates[^1].ReasonIdentity);
        Assert.Null(evaluation.TotalScore);
    }

    [Fact]
    public void Lower_is_better_direction_is_reflected_in_contribution_and_merit()
    {
        var definition = Definition(dimensions: [Dimension(
            direction: CompanionRoleScoreDirection.LowerIsBetter)]);
        var lower = CompanionRoleEvaluator.Evaluate(
            definition,
            Profile(characterId: 42, facts: [MartialFact(10)]),
            MartialDiscipline());
        var higher = CompanionRoleEvaluator.Evaluate(
            definition,
            Profile(characterId: 43, facts: [MartialFact(20)]),
            MartialDiscipline());

        Assert.Equal(-10m, lower.TotalScore);
        Assert.Equal(-20m, higher.TotalScore);
        Assert.Equal(
            CompanionRoleMeritComparison.FirstPreferred,
            CompanionRoleMeritComparer.Compare(lower, higher));
    }

    [Fact]
    public void Equal_scores_remain_exact_ties_and_character_id_does_not_break_merit()
    {
        var first = Evaluate(Profile(characterId: 99, facts: [MartialFact(73)]));
        var second = Evaluate(Profile(characterId: 2, facts: [MartialFact(73)]));

        Assert.Equal(
            CompanionRoleMeritComparison.ExactTie,
            CompanionRoleMeritComparer.Compare(first, second));
        Assert.Equal(first.TotalScore, second.TotalScore);
    }

    [Fact]
    public void Different_role_or_discipline_results_are_not_comparable()
    {
        var martial = Evaluate(Profile(facts: [MartialFact(73)]));
        var life = CompanionRoleEvaluator.Evaluate(
            VerifiedCompanionRoleDefinitions.LifeSkillDisciplineAptitude,
            Profile(facts: [LifeFact(73)]),
            new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, 0));
        var otherDiscipline = Evaluate(
            Profile(facts: [MartialFact(73, type: 1)]),
            disciplineType: 1);

        Assert.Equal(
            CompanionRoleMeritComparison.NotComparable,
            CompanionRoleMeritComparer.Compare(martial, life));
        Assert.Equal(
            CompanionRoleMeritComparison.NotComparable,
            CompanionRoleMeritComparer.Compare(martial, otherDiscipline));
    }

    [Fact]
    public void Rule_and_evaluation_fingerprints_are_deterministic_and_semantic()
    {
        var baselineDefinition = Definition();
        var repeatedDefinition = Definition();
        var changedDefinition = Definition(dimensions: [Dimension(weight: 2)]);
        var first = CompanionRoleEvaluator.Evaluate(
            baselineDefinition,
            Profile(facts: [MartialFact(73)]),
            MartialDiscipline());
        var repeated = CompanionRoleEvaluator.Evaluate(
            repeatedDefinition,
            Profile(facts: [MartialFact(73)]),
            MartialDiscipline());
        var changedValue = CompanionRoleEvaluator.Evaluate(
            baselineDefinition,
            Profile(facts: [MartialFact(74)]),
            MartialDiscipline());

        Assert.Equal(baselineDefinition.Fingerprint, repeatedDefinition.Fingerprint);
        Assert.NotEqual(baselineDefinition.Fingerprint, changedDefinition.Fingerprint);
        Assert.Equal(first.Fingerprint, repeated.Fingerprint);
        Assert.NotEqual(first.Fingerprint, changedValue.Fingerprint);
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string OtherSha =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private const string GameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";

    private static CompanionRoleEvaluation Evaluate(
        CandidateProfile profile,
        short disciplineType = 0) => CompanionRoleEvaluator.Evaluate(
            VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude,
            profile,
            MartialDiscipline(disciplineType));

    private static CompanionRoleDefinition Definition(
        IEnumerable<string>? supportedVersions = null,
        IEnumerable<CompanionRoleScoreDimension>? dimensions = null) => new(
            new CompanionRoleIdentity("SYNTHETIC_MARTIAL_APTITUDE"),
            "1",
            "1",
            supportedVersions ?? [GameDataVersion],
            VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
            VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion,
            CandidateDisciplineDomain.Martial,
            0,
            13,
            dimensions ?? [Dimension()],
            CompanionRoleTiePolicy.ExactTotalRemainsTie);

    private static CompanionRoleScoreDimension Dimension(
        decimal weight = 1m,
        decimal minimum = short.MinValue,
        decimal maximum = short.MaxValue,
        CompanionRoleScoreDirection direction = CompanionRoleScoreDirection.HigherIsBetter) => new(
            "BASE_MARTIAL_QUALIFICATION",
            CandidateProfileField.BaseMartialQualification,
            "BASE_QUALIFICATION_POINT",
            direction,
            CompanionRoleNormalizationKind.Identity,
            minimum,
            maximum,
            weight,
            CompanionRoleMissingEvidenceBehavior.EvaluationIncomplete,
            "BASE_MARTIAL_QUALIFICATION_EXACT_SAVED_VALUE");

    private static CandidateProfile Profile(
        int characterId = 42,
        CandidateUniverseState state = CandidateUniverseState.Eligible,
        string gameDataVersion = GameDataVersion,
        IEnumerable<CandidateProfileFact>? facts = null) => new(
            new CandidateIdentity(characterId),
            state,
            new CandidateProfileSourceVersions(
                Sha,
                gameDataVersion,
                VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
                "1",
                VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion),
            facts ?? [],
            []);

    private static CandidateProfileFieldIdentity MartialField(short type = 0) => new(
        CandidateProfileField.BaseMartialQualification,
        MartialDiscipline(type));

    private static CandidateDisciplineIdentity MartialDiscipline(short type = 0) => new(
        CandidateDisciplineDomain.Martial,
        type);

    private static CandidateDisciplineIdentity CapabilityObjective() => new(
        CandidateDisciplineDomain.Capability,
        0);

    private static IEnumerable<CandidateProfileFact> CapabilityFacts(
        short mainValue = 60,
        short martialValue = 30,
        short lifeValue = 90)
    {
        foreach (var attribute in Enum.GetValues<CandidateMainAttribute>())
        {
            yield return CandidateProfileFact.Confirmed(
                new CandidateProfileFieldIdentity(
                    CandidateProfileField.BaseMainAttribute,
                    attribute),
                CandidateFactValue.Int16(mainValue),
                SaveProvenance(),
                [Evidence()]);
        }

        foreach (var type in Enumerable.Range(
                     0,
                     CompanionCapabilitySummary.MartialDisciplineCount))
        {
            yield return MartialFact(martialValue, checked((short)type));
        }

        foreach (var type in Enumerable.Range(
                     0,
                     CompanionCapabilitySummary.LifeSkillDisciplineCount))
        {
            yield return LifeFact(lifeValue, checked((short)type));
        }
    }

    private static CandidateProfileFact MartialFact(short value, short type = 0) =>
        CandidateProfileFact.Confirmed(
            MartialField(type),
            CandidateFactValue.Int16(value),
            SaveProvenance(),
            [Evidence()]);

    private static CandidateProfileFact LifeFact(short value, short type = 0) =>
        CandidateProfileFact.Confirmed(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.BaseLifeSkillQualification,
                new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, type)),
            CandidateFactValue.Int16(value),
            SaveProvenance(),
            [Evidence()]);

    private static CandidateProfileFact AgeFact(short value) =>
        CandidateProfileFact.Confirmed(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.CurrentAge),
            CandidateFactValue.Int16(value),
            SaveProvenance(),
            [Evidence()]);

    private static CandidateProfileFact MartialFact(CandidateEvidenceState state) => state switch
    {
        CandidateEvidenceState.Incomplete => CandidateProfileFact.Incomplete(
            MartialField(),
            Reason("REQUIRED_FACT_INCOMPLETE"),
            []),
        CandidateEvidenceState.Unsupported => CandidateProfileFact.Unsupported(
            MartialField(),
            Reason("REQUIRED_FACT_UNSUPPORTED"),
            []),
        CandidateEvidenceState.Stale => CandidateProfileFact.Stale(
            MartialField(),
            CandidateFactValue.Int16(73),
            SaveProvenance(revision: OtherSha),
            Reason("SAVE_REVISION_CHANGED"),
            [Evidence(revision: OtherSha)]),
        CandidateEvidenceState.Conflicting => CandidateProfileFact.Conflicting(
            MartialField(),
            [
                Conflict(70, SaveProvenance("SAVE_BUFFER"), "E6-SAVE-001"),
                Conflict(73, SaveProvenance("SAVE_PROJECTION"), "E6-SAVE-002")
            ],
            new CandidateConflictDecision(
                CandidateConflictDecisionKind.Unresolved,
                "NO_SAFE_PRECEDENCE"),
            []),
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Test fixture requires unavailable evidence.")
    };

    private static CandidateFactProvenance SaveProvenance(
        string source = "CONFIGURED_SAVE",
        string revision = Sha) => new(
            CandidateEvidenceSourceKind.ConfiguredSave,
            source,
            VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
            revision);

    private static CandidateEvidenceReference Evidence(
        string revision = Sha) => new(
            "E6-SAVE-001",
            SaveProvenance(revision: revision));

    private static CandidateConflictValue Conflict(
        short value,
        CandidateFactProvenance provenance,
        string reference) => new(
            CandidateFactValue.Int16(value),
            provenance,
            [new CandidateEvidenceReference(reference, provenance)]);

    private static CandidateUnavailableReason Reason(string code) => new(
        code,
        "Synthetic unavailable evidence for a Domain test.");
}
