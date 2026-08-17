using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using Xunit;

namespace TaiWu.Domain.UnitTests.CompanionRoles;

public sealed class CompanionRoleShortlistBuilderTests
{
    [Fact]
    public void Ranks_by_descending_merit_and_retains_competition_ties()
    {
        var result = Build(
            Profile(99, value: 90),
            Profile(4, value: 80),
            Profile(7, value: 90),
            Profile(2, value: 100));

        Assert.Equal([2, 7, 99, 4], CandidateIds(result.RankedCandidates));
        Assert.Equal([1, 2, 2, 4], result.RankedCandidates.Select(item => item.CompetitionRank));
        Assert.Equal(
            [
                CompanionRoleCandidateRankingState.Ranked,
                CompanionRoleCandidateRankingState.Tied,
                CompanionRoleCandidateRankingState.Tied,
                CompanionRoleCandidateRankingState.Ranked
            ],
            result.RankedCandidates.Select(item => item.State));
        Assert.Empty(result.UnrankedCandidates);
    }

    [Fact]
    public void Character_identity_canonicalizes_a_tie_without_changing_merit()
    {
        var result = Build(
            Profile(90, value: 73),
            Profile(3, value: 73));

        Assert.Equal([3, 90], CandidateIds(result.Candidates));
        Assert.All(
            result.Candidates,
            item =>
            {
                Assert.Equal(CompanionRoleCandidateRankingState.Tied, item.State);
                Assert.Equal(1, item.CompetitionRank);
                Assert.Equal(73m, item.Evaluation.TotalScore);
            });
        Assert.Equal(
            CompanionRoleMeritComparison.ExactTie,
            CompanionRoleMeritComparer.Compare(
                result.Candidates[0].Evaluation,
                result.Candidates[1].Evaluation));
    }

    [Fact]
    public void Retains_every_unranked_state_without_numeric_penalties()
    {
        var ranked = Profile(1, value: 50);
        var ineligible = Profile(
            2,
            CandidateUniverseState.Ineligible,
            [MartialFact(short.MaxValue)]);
        var incomplete = Profile(3);
        var unsupported = Profile(
            4,
            CandidateUniverseState.Eligible,
            [UnsupportedMartialFact()]);
        var conflicting = Profile(
            5,
            CandidateUniverseState.Eligible,
            [MartialFact(90, revision: OtherSha)]);

        var result = Build(
            conflicting,
            unsupported,
            incomplete,
            ineligible,
            ranked);

        Assert.Single(result.RankedCandidates);
        Assert.Equal(
            [
                CompanionRoleCandidateRankingState.Ineligible,
                CompanionRoleCandidateRankingState.Incomplete,
                CompanionRoleCandidateRankingState.Unsupported,
                CompanionRoleCandidateRankingState.Conflicting
            ],
            result.UnrankedCandidates.Select(item => item.State));
        Assert.Equal([2, 3, 4, 5], CandidateIds(result.UnrankedCandidates));
        Assert.All(result.UnrankedCandidates, item =>
        {
            Assert.False(item.IsRanked);
            Assert.Null(item.CompetitionRank);
            Assert.Null(item.Evaluation.TotalScore);
            Assert.Empty(item.Evaluation.Components);
        });
    }

    [Fact]
    public void Eligibility_gate_precedes_a_maximum_saved_score()
    {
        var result = Build(Profile(
            8,
            CandidateUniverseState.Ineligible,
            [MartialFact(short.MaxValue)]));

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(CompanionRoleCandidateRankingState.Ineligible, candidate.State);
        Assert.Single(candidate.Evaluation.Gates);
        Assert.Equal(
            CompanionRoleRequirementKind.CandidateUniverseEligible,
            candidate.Evaluation.Gates[0].Requirement.Kind);
        Assert.Equal(CompanionRoleGateOutcome.Failed, candidate.Evaluation.Gates[0].Outcome);
        Assert.Null(candidate.Evaluation.TotalScore);
        Assert.Empty(candidate.Evaluation.Components);
    }

    [Fact]
    public void Both_verified_roles_rank_their_exact_typed_base_aptitude()
    {
        var martial = Build(
            VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude,
            new CandidateDisciplineIdentity(CandidateDisciplineDomain.Martial, 13),
            Profile(2, facts: [MartialFact(72, type: 13)]),
            Profile(1, facts: [MartialFact(91, type: 13)]));
        var life = Build(
            VerifiedCompanionRoleDefinitions.LifeSkillDisciplineAptitude,
            new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, 15),
            Profile(2, facts: [LifeFact(72, type: 15)]),
            Profile(1, facts: [LifeFact(91, type: 15)]));

        Assert.Equal([1, 2], CandidateIds(martial.Candidates));
        Assert.Equal([1, 2], CandidateIds(life.Candidates));
        Assert.All(
            martial.Candidates.Concat(life.Candidates),
            item => Assert.Equal(CompanionRoleCandidateRankingState.Ranked, item.State));
    }

    [Fact]
    public void Component_retains_rule_evidence_arithmetic_and_explanation()
    {
        var evidence = Evidence();
        var profile = Profile(1, facts: [MartialFact(73, evidence: [evidence])]);

        var result = Build(profile);

        var component = Assert.Single(Assert.Single(result.Candidates).Evaluation.Components);
        Assert.Equal("BASE_MARTIAL_QUALIFICATION", component.Dimension.Identity);
        Assert.Equal("BASE_QUALIFICATION_POINT", component.Dimension.Unit);
        Assert.Equal(
            "BASE_MARTIAL_QUALIFICATION_EXACT_SAVED_VALUE",
            component.Dimension.ExplanationIdentity);
        Assert.Equal(CandidateProfileField.BaseMartialQualification, component.Field.Field);
        Assert.Equal((short)73, component.RawValue);
        Assert.Equal(73m, component.NormalizedValue);
        Assert.Equal(1m, component.Weight);
        Assert.Equal(73m, component.Contribution);
        Assert.Equal([evidence], component.Evidence);
    }

    [Fact]
    public void Extreme_scores_use_bounded_decimal_arithmetic()
    {
        var result = Build(
            Profile(1, value: short.MinValue),
            Profile(2, value: short.MaxValue));

        Assert.Equal([2, 1], CandidateIds(result.Candidates));
        Assert.Equal(
            [(decimal)short.MaxValue, (decimal)short.MinValue],
            result.Candidates.Select(item => item.Evaluation.TotalScore!.Value));
    }

    [Fact]
    public void Missing_unapproved_optional_fields_do_not_affect_ranking()
    {
        var onlyRequiredFact = Profile(1, value: 70);

        var result = Build(onlyRequiredFact);

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(CompanionRoleCandidateRankingState.Ranked, candidate.State);
        Assert.Equal(70m, candidate.Evaluation.TotalScore);
        Assert.Single(candidate.Evaluation.Components);
    }

    [Fact]
    public void Unsupported_discipline_is_retained_as_unsupported()
    {
        var result = Build(
            VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude,
            new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, 0),
            Profile(1, value: 70));

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(CompanionRoleCandidateRankingState.Unsupported, candidate.State);
        Assert.Equal(
            "DISCIPLINE_UNSUPPORTED",
            candidate.Evaluation.Gates[^1].ReasonIdentity);
    }

    [Fact]
    public void Unsupported_source_version_is_retained_for_the_whole_comparable_set()
    {
        var first = Profile(1, value: 70, gameDataVersion: "unsupported-version");
        var second = Profile(2, value: 90, gameDataVersion: "unsupported-version");

        var result = Build(first, second);

        Assert.All(
            result.Candidates,
            item => Assert.Equal(
                CompanionRoleCandidateRankingState.Unsupported,
                item.State));
        Assert.Empty(result.RankedCandidates);
        Assert.Equal(2, result.CandidateCount);
    }

    [Fact]
    public void Equivalent_inputs_have_identical_order_components_and_fingerprint()
    {
        var first = Build(
            Profile(7, value: 60),
            Profile(2, value: 90),
            Profile(5, value: 60));
        var repeated = Build(
            Profile(5, value: 60),
            Profile(7, value: 60),
            Profile(2, value: 90));

        Assert.Equal(first.Fingerprint, repeated.Fingerprint);
        Assert.Equal(CandidateIds(first.Candidates), CandidateIds(repeated.Candidates));
        Assert.Equal(
            first.Candidates.Select(item => item.Evaluation.Fingerprint),
            repeated.Candidates.Select(item => item.Evaluation.Fingerprint));
        Assert.Equal(
            first.Candidates.SelectMany(item => item.Evaluation.Components)
                .Select(item => item.Contribution),
            repeated.Candidates.SelectMany(item => item.Evaluation.Components)
                .Select(item => item.Contribution));
    }

    [Fact]
    public void Semantic_fact_or_rule_changes_change_ranking_fingerprint()
    {
        var baseline = Build(Profile(1, value: 70));
        var changedFact = Build(Profile(1, value: 71));
        var changedDefinition = Definition(evaluationRuleVersion: "2");
        var changedRule = Build(
            changedDefinition,
            MartialDiscipline(),
            Profile(1, value: 70));

        Assert.NotEqual(baseline.Fingerprint, changedFact.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, changedRule.Fingerprint);
    }

    [Fact]
    public void Duplicate_candidate_identity_is_rejected_before_evaluation()
    {
        Assert.Throws<ArgumentException>(() => Build(
            Profile(1, value: 70),
            Profile(1, value: 90)));
    }

    [Fact]
    public void Mixed_candidate_source_versions_are_not_comparable()
    {
        Assert.Throws<ArgumentException>(() => Build(
            Profile(1, value: 70),
            Profile(2, value: 90, gameDataVersion: "different-version")));
    }

    [Fact]
    public void Empty_candidate_universe_has_a_stable_empty_ranking()
    {
        var first = Build([]);
        var repeated = Build([]);

        Assert.Empty(first.Candidates);
        Assert.Empty(first.RankedCandidates);
        Assert.Empty(first.UnrankedCandidates);
        Assert.Equal(first.Fingerprint, repeated.Fingerprint);
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string OtherSha =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private static CompanionRoleRanking Build(params CandidateProfile[] profiles) =>
        Build(
            VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude,
            MartialDiscipline(),
            profiles);

    private static CompanionRoleRanking Build(
        CompanionRoleDefinition definition,
        CandidateDisciplineIdentity discipline,
        params CandidateProfile[] profiles) =>
        CompanionRoleShortlistBuilder.EvaluateAndRank(
            definition,
            discipline,
            profiles);

    private static CompanionRoleDefinition Definition(
        string evaluationRuleVersion) => new(
        new CompanionRoleIdentity("MARTIAL_DISCIPLINE_APTITUDE"),
        VerifiedCompanionRoleDefinitions.RoleVersion,
        evaluationRuleVersion,
        [VerifiedCompanionRoleDefinitions.SupportedGameDataVersion],
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion,
        CandidateDisciplineDomain.Martial,
        0,
        13,
        VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude.ScoreDimensions,
        CompanionRoleTiePolicy.ExactTotalRemainsTie);

    private static CandidateProfile Profile(
        int characterId,
        short? value = null,
        CandidateUniverseState state = CandidateUniverseState.Eligible,
        IEnumerable<CandidateProfileFact>? facts = null,
        string gameDataVersion = VerifiedCompanionRoleDefinitions.SupportedGameDataVersion) =>
        Profile(
            characterId,
            state,
            facts ?? (value.HasValue ? [MartialFact(value.Value)] : []),
            gameDataVersion);

    private static CandidateProfile Profile(
        int characterId,
        CandidateUniverseState state,
        IEnumerable<CandidateProfileFact> facts,
        string gameDataVersion = VerifiedCompanionRoleDefinitions.SupportedGameDataVersion) => new(
            new CandidateIdentity(characterId),
            state,
            new CandidateProfileSourceVersions(
                Sha,
                gameDataVersion,
                VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
                "1",
                VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion),
            facts,
            []);

    private static CandidateProfileFact MartialFact(
        short value,
        short type = 0,
        string revision = Sha,
        IEnumerable<CandidateEvidenceReference>? evidence = null) =>
        CandidateProfileFact.Confirmed(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.BaseMartialQualification,
                MartialDiscipline(type)),
            CandidateFactValue.Int16(value),
            SaveProvenance(revision),
            evidence ?? [Evidence(revision)]);

    private static CandidateProfileFact LifeFact(short value, short type) =>
        CandidateProfileFact.Confirmed(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.BaseLifeSkillQualification,
                new CandidateDisciplineIdentity(CandidateDisciplineDomain.LifeSkill, type)),
            CandidateFactValue.Int16(value),
            SaveProvenance(Sha),
            [Evidence()]);

    private static CandidateProfileFact UnsupportedMartialFact() =>
        CandidateProfileFact.Unsupported(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.BaseMartialQualification,
                MartialDiscipline()),
            new CandidateUnavailableReason(
                "BASE_MARTIAL_QUALIFICATION_UNSUPPORTED",
                "Synthetic unsupported evidence for a Domain test."),
            []);

    private static CandidateDisciplineIdentity MartialDiscipline(short type = 0) => new(
        CandidateDisciplineDomain.Martial,
        type);

    private static CandidateFactProvenance SaveProvenance(string revision) => new(
        CandidateEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        revision);

    private static CandidateEvidenceReference Evidence(string revision = Sha) => new(
        "E6-SAVE-001",
        SaveProvenance(revision));

    private static int[] CandidateIds(
        IEnumerable<CompanionRoleCandidateRanking> candidates) =>
        [.. candidates.Select(item => item.Evaluation.Profile.Identity.CharacterId)];
}
