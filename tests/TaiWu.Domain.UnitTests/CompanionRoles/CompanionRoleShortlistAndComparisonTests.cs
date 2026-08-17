using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using Xunit;

namespace TaiWu.Domain.UnitTests.CompanionRoles;

public sealed class CompanionRoleShortlistAndComparisonTests
{
    [Fact]
    public void Shortlist_retains_identity_counts_ties_exclusions_and_diagnostics()
    {
        var ranking = Rank(
            Profile(7, facts: [ConflictingMartialFact()]),
            Profile(6, facts: [UnsupportedMartialFact()]),
            Profile(5),
            Profile(4, CandidateUniverseState.Ineligible, [MartialFact(short.MaxValue)]),
            Profile(3, value: 90),
            Profile(2, value: 90),
            Profile(1, value: 100));

        var shortlist = CompanionRoleShortlistFactory.Create(ranking);

        Assert.Same(ranking, shortlist.Ranking);
        Assert.Same(ranking.Definition, shortlist.Definition);
        Assert.Same(ranking.SourceVersions, shortlist.SourceVersions);
        Assert.Equal(7, shortlist.Counts.Total);
        Assert.Equal(1, shortlist.Counts.Ranked);
        Assert.Equal(2, shortlist.Counts.Tied);
        Assert.Equal(1, shortlist.Counts.Ineligible);
        Assert.Equal(1, shortlist.Counts.Incomplete);
        Assert.Equal(1, shortlist.Counts.Unsupported);
        Assert.Equal(1, shortlist.Counts.Conflicting);
        Assert.Equal(3, shortlist.RankedEntries.Length);
        Assert.Equal(4, shortlist.ExcludedEntries.Length);
        Assert.Equal(3, shortlist.Diagnostics.Length);
        Assert.Equal(
            ranking.Candidates.Select(item => item.Evaluation.Profile.Identity.CharacterId),
            shortlist.Entries.Select(item => item.Evaluation.Profile.Identity.CharacterId));
        Assert.All(
            shortlist.Entries.Select((entry, index) => (entry, index)),
            item => Assert.Same(ranking.Candidates[item.index], item.entry.Candidate));
    }

    [Fact]
    public void Ranked_explanations_reference_existing_components_only()
    {
        var shortlist = Shortlist(Profile(1, value: 73));

        var entry = Assert.Single(shortlist.Entries);
        var component = Assert.Single(entry.Evaluation.Components);
        var strongest = entry.Explanations.Single(item =>
            item.Kind == CompanionRoleExplanationKind.StrongestContribution);
        var limitation = entry.Explanations.Single(item =>
            item.Kind == CompanionRoleExplanationKind.MaterialLimitation);

        Assert.Same(component, Assert.Single(strongest.Components));
        Assert.Same(component, Assert.Single(limitation.Components));
        Assert.Empty(strongest.Gates);
        Assert.Empty(limitation.Gates);
        Assert.Equal("STRONGEST_APPROVED_SCORE_CONTRIBUTION", strongest.Identity);
        Assert.Equal("ROLE_SCORE_LIMITED_TO_APPROVED_COMPONENTS", limitation.Identity);
        Assert.DoesNotContain(
            entry.Explanations,
            item => item.Identity.Contains("RECRUIT", StringComparison.OrdinalIgnoreCase)
                || item.Identity.Contains("TRAIN", StringComparison.OrdinalIgnoreCase)
                || item.Identity.Contains("TRAVEL", StringComparison.OrdinalIgnoreCase)
                || item.Identity.Contains("EQUIP", StringComparison.OrdinalIgnoreCase)
                || item.Identity.Contains("ASSIGN", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Tied_entry_retains_exact_tie_explanation_and_shared_rank()
    {
        var shortlist = Shortlist(
            Profile(8, value: 73),
            Profile(2, value: 73));

        Assert.All(shortlist.Entries, entry =>
        {
            Assert.Equal(CompanionRoleCandidateRankingState.Tied, entry.Candidate.State);
            Assert.Equal(1, entry.Candidate.CompetitionRank);
            var tie = entry.Explanations.Single(item =>
                item.Kind == CompanionRoleExplanationKind.ExactTie);
            Assert.Equal("EXACT_ROLE_TOTAL_TIE", tie.Identity);
            Assert.Same(
                Assert.Single(entry.Evaluation.Components),
                Assert.Single(tie.Components));
        });
    }

    [Fact]
    public void Exclusion_explanation_preserves_exact_nonpassing_gate()
    {
        var shortlist = Shortlist(Profile(
            4,
            CandidateUniverseState.Ineligible,
            [MartialFact(short.MaxValue)]));

        var entry = Assert.Single(shortlist.ExcludedEntries);
        var explanation = Assert.Single(entry.Explanations);
        var evaluationGate = Assert.Single(entry.Evaluation.Gates);
        Assert.Equal(CompanionRoleExplanationKind.Exclusion, explanation.Kind);
        Assert.Equal(entry.Evaluation.OutcomeIdentity, explanation.Identity);
        Assert.Same(evaluationGate, Assert.Single(explanation.Gates));
        Assert.Equal(CompanionRoleGateOutcome.Failed, evaluationGate.Outcome);
        Assert.Empty(explanation.Components);
    }

    [Fact]
    public void Comparison_uses_existing_evaluations_components_values_and_evidence()
    {
        var evidence = Evidence();
        var lowFact = MartialFact(70, evidence: [evidence]);
        var highFact = MartialFact(90, evidence: [evidence]);
        var shortlist = Shortlist(
            Profile(1, facts: [lowFact]),
            Profile(2, facts: [highFact]));

        var comparison = CompanionRoleComparisonBuilder.Compare(shortlist, 2, 1);

        Assert.Same(shortlist, comparison.Shortlist);
        Assert.Equal(CompanionRoleComparisonOutcome.FirstAdvantage, comparison.Outcome);
        Assert.Equal(2, comparison.First.Evaluation.Profile.Identity.CharacterId);
        Assert.Equal(1, comparison.Second.Evaluation.Profile.Identity.CharacterId);
        var row = Assert.Single(comparison.Rows);
        Assert.Same(shortlist.Definition.ScoreDimensions[0], row.Dimension);
        Assert.Equal(CandidateProfileField.BaseMartialQualification, row.Field.Field);
        Assert.Equal(CompanionRoleComparisonEvidenceState.Confirmed, row.First.State);
        Assert.Equal(CompanionRoleComparisonEvidenceState.Confirmed, row.Second.State);
        Assert.Equal((short)90, row.First.Value);
        Assert.Equal((short)70, row.Second.Value);
        Assert.Same(highFact, row.First.Fact);
        Assert.Same(lowFact, row.Second.Fact);
        Assert.Equal([evidence], row.First.Evidence);
        Assert.Equal(CompanionRoleComparisonOutcome.FirstAdvantage, row.Outcome);
    }

    [Fact]
    public void Comparison_reuses_direction_aware_contributions_instead_of_raw_order()
    {
        var definition = LowerIsBetterDefinition();
        var shortlist = Shortlist(
            definition,
            Profile(1, value: 10),
            Profile(2, value: 20));

        var comparison = CompanionRoleComparisonBuilder.Compare(shortlist, 1, 2);

        Assert.Equal(CompanionRoleComparisonOutcome.FirstAdvantage, comparison.Outcome);
        var row = Assert.Single(comparison.Rows);
        Assert.Equal((short)10, row.First.Value);
        Assert.Equal((short)20, row.Second.Value);
        Assert.Equal(CompanionRoleComparisonOutcome.FirstAdvantage, row.Outcome);
        Assert.Equal(
            -10m,
            Assert.Single(comparison.First.Evaluation.Components).Contribution);
        Assert.Equal(
            -20m,
            Assert.Single(comparison.Second.Evaluation.Components).Contribution);
    }

    [Fact]
    public void Equal_confirmed_values_remain_an_equal_tied_comparison()
    {
        var shortlist = Shortlist(
            Profile(1, value: 73),
            Profile(2, value: 73));

        var comparison = CompanionRoleComparisonBuilder.Compare(shortlist, 1, 2);

        Assert.Equal(CompanionRoleComparisonOutcome.Equal, comparison.Outcome);
        Assert.Equal(CompanionRoleComparisonOutcome.Equal, Assert.Single(comparison.Rows).Outcome);
        Assert.All(
            [comparison.First, comparison.Second],
            entry => Assert.Equal(CompanionRoleCandidateRankingState.Tied, entry.Candidate.State));
    }

    [Fact]
    public void Ranked_to_incomplete_comparison_is_unavailable_not_a_score_difference()
    {
        var shortlist = Shortlist(
            Profile(1, value: 73),
            Profile(2));

        var comparison = CompanionRoleComparisonBuilder.Compare(shortlist, 1, 2);

        Assert.Equal(CompanionRoleComparisonOutcome.Unavailable, comparison.Outcome);
        var row = Assert.Single(comparison.Rows);
        Assert.Equal(CompanionRoleComparisonOutcome.Unavailable, row.Outcome);
        Assert.Equal(CompanionRoleComparisonEvidenceState.Confirmed, row.First.State);
        Assert.Equal(CompanionRoleComparisonEvidenceState.Missing, row.Second.State);
        Assert.Null(row.Second.Value);
        Assert.Null(row.Second.Fact);
    }

    [Fact]
    public void Conflicting_evidence_is_distinct_from_unavailable_evidence()
    {
        var conflict = ConflictingMartialFact();
        var shortlist = Shortlist(
            Profile(1, value: 73),
            Profile(2, facts: [conflict]));

        var comparison = CompanionRoleComparisonBuilder.Compare(shortlist, 1, 2);

        Assert.Equal(CompanionRoleComparisonOutcome.Conflicting, comparison.Outcome);
        var row = Assert.Single(comparison.Rows);
        Assert.Equal(CompanionRoleComparisonOutcome.Conflicting, row.Outcome);
        Assert.Equal(CompanionRoleComparisonEvidenceState.Conflicting, row.Second.State);
        Assert.Same(conflict, row.Second.Fact);
        Assert.Null(row.Second.Value);
    }

    [Theory]
    [InlineData(CompanionRoleShortlistFilter.All, 5)]
    [InlineData(CompanionRoleShortlistFilter.Ranked, 2)]
    [InlineData(CompanionRoleShortlistFilter.NeedsReview, 2)]
    [InlineData(CompanionRoleShortlistFilter.Ineligible, 1)]
    public void Filtered_views_preserve_source_counts_order_and_entry_identity(
        CompanionRoleShortlistFilter filter,
        int expectedCount)
    {
        var shortlist = Shortlist(
            Profile(1, value: 90),
            Profile(2, value: 80),
            Profile(3),
            Profile(4, facts: [UnsupportedMartialFact()]),
            Profile(5, CandidateUniverseState.Ineligible, [MartialFact(100)]));
        var originalFingerprint = shortlist.Fingerprint;

        var view = CompanionRoleShortlistFilterer.CreateView(shortlist, filter);

        Assert.Same(shortlist, view.Source);
        Assert.Same(shortlist.Counts, view.UnfilteredCounts);
        Assert.Equal(expectedCount, view.VisibleCount);
        Assert.Equal(originalFingerprint, shortlist.Fingerprint);
        Assert.All(
            view.Entries,
            entry => Assert.Contains(shortlist.Entries, candidate => ReferenceEquals(candidate, entry)));
        Assert.Equal(
            shortlist.Entries.Where(entry => view.Entries.Contains(entry)),
            view.Entries);
    }

    [Fact]
    public void Location_is_available_only_for_current_confirmed_save_evidence()
    {
        var currentLocation = LocationFact(11, CandidateEvidenceState.Confirmed);
        var staleLocation = LocationFact(12, CandidateEvidenceState.Stale);
        var unavailableLocation = LocationFact(13, CandidateEvidenceState.Incomplete);
        var shortlist = Shortlist(
            Profile(1, facts: [MartialFact(90), currentLocation]),
            Profile(2, facts: [MartialFact(80), staleLocation]),
            Profile(3, facts: [MartialFact(70), unavailableLocation]));

        var first = shortlist.Entries.Single(item =>
            item.Evaluation.Profile.Identity.CharacterId == 1);
        var second = shortlist.Entries.Single(item =>
            item.Evaluation.Profile.Identity.CharacterId == 2);
        var third = shortlist.Entries.Single(item =>
            item.Evaluation.Profile.Identity.CharacterId == 3);

        Assert.Same(currentLocation, Assert.Single(first.LocationEvidence));
        Assert.Same(currentLocation, Assert.Single(first.AvailableLocationFacts));
        Assert.Same(staleLocation, Assert.Single(second.LocationEvidence));
        Assert.Empty(second.AvailableLocationFacts);
        Assert.Same(unavailableLocation, Assert.Single(third.LocationEvidence));
        Assert.Empty(third.AvailableLocationFacts);
    }

    [Fact]
    public void Comparison_rejects_same_or_unknown_candidate_selection()
    {
        var shortlist = Shortlist(
            Profile(1, value: 90),
            Profile(2, value: 80));

        Assert.Throws<ArgumentException>(() =>
            CompanionRoleComparisonBuilder.Compare(shortlist, 1, 1));
        Assert.Throws<ArgumentException>(() =>
            CompanionRoleComparisonBuilder.Compare(shortlist, 1, 999));
        Assert.Throws<ArgumentException>(() =>
            CompanionRoleComparisonBuilder.Compare(shortlist, 999, 1));
    }

    [Fact]
    public void Equivalent_reruns_preserve_shortlist_and_comparison_fingerprints()
    {
        var first = Shortlist(
            Profile(7, value: 70),
            Profile(2, value: 90));
        var repeated = Shortlist(
            Profile(2, value: 90),
            Profile(7, value: 70));
        var firstComparison = CompanionRoleComparisonBuilder.Compare(first, 2, 7);
        var repeatedComparison = CompanionRoleComparisonBuilder.Compare(repeated, 2, 7);

        Assert.Equal(first.Fingerprint, repeated.Fingerprint);
        Assert.Equal(firstComparison.Fingerprint, repeatedComparison.Fingerprint);
        Assert.Equal(
            first.Entries.Select(item => item.Explanations.Select(value => value.Identity)),
            repeated.Entries.Select(item => item.Explanations.Select(value => value.Identity)));
    }

    [Fact]
    public void Empty_ranking_produces_stable_empty_shortlist_and_views()
    {
        var first = Shortlist([]);
        var repeated = Shortlist([]);
        var view = CompanionRoleShortlistFilterer.CreateView(
            first,
            CompanionRoleShortlistFilter.All);

        Assert.Equal(0, first.Counts.Total);
        Assert.Empty(first.Entries);
        Assert.Empty(view.Entries);
        Assert.Equal(first.Fingerprint, repeated.Fingerprint);
        Assert.Equal(2, first.Diagnostics.Length);
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string OtherSha =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private static CompanionRoleShortlist Shortlist(params CandidateProfile[] profiles) =>
        CompanionRoleShortlistFactory.Create(Rank(profiles));

    private static CompanionRoleShortlist Shortlist(
        CompanionRoleDefinition definition,
        params CandidateProfile[] profiles) =>
        CompanionRoleShortlistFactory.Create(
            CompanionRoleShortlistBuilder.EvaluateAndRank(
                definition,
                MartialDiscipline(),
                profiles));

    private static CompanionRoleRanking Rank(params CandidateProfile[] profiles) =>
        CompanionRoleShortlistBuilder.EvaluateAndRank(
            VerifiedCompanionRoleDefinitions.MartialDisciplineAptitude,
            MartialDiscipline(),
            profiles);

    private static CompanionRoleDefinition LowerIsBetterDefinition() => new(
        new CompanionRoleIdentity("LOWER_MARTIAL_APTITUDE"),
        "1",
        "1",
        [VerifiedCompanionRoleDefinitions.SupportedGameDataVersion],
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion,
        CandidateDisciplineDomain.Martial,
        0,
        13,
        [new CompanionRoleScoreDimension(
            "BASE_MARTIAL_QUALIFICATION",
            CandidateProfileField.BaseMartialQualification,
            "BASE_QUALIFICATION_POINT",
            CompanionRoleScoreDirection.LowerIsBetter,
            CompanionRoleNormalizationKind.Identity,
            short.MinValue,
            short.MaxValue,
            1m,
            CompanionRoleMissingEvidenceBehavior.EvaluationIncomplete,
            "LOWER_BASE_MARTIAL_QUALIFICATION")],
        CompanionRoleTiePolicy.ExactTotalRemainsTie);

    private static CandidateProfile Profile(
        int characterId,
        short? value = null,
        IEnumerable<CandidateProfileFact>? facts = null) =>
        Profile(
            characterId,
            CandidateUniverseState.Eligible,
            facts ?? (value.HasValue ? [MartialFact(value.Value)] : []));

    private static CandidateProfile Profile(
        int characterId,
        CandidateUniverseState state,
        IEnumerable<CandidateProfileFact> facts) => new(
            new CandidateIdentity(characterId),
            state,
            Versions(),
            facts,
            []);

    private static CandidateProfileFact MartialFact(
        short value,
        IEnumerable<CandidateEvidenceReference>? evidence = null) =>
        CandidateProfileFact.Confirmed(
            MartialField(),
            CandidateFactValue.Int16(value),
            SaveProvenance(Sha),
            evidence ?? [Evidence()]);

    private static CandidateProfileFact UnsupportedMartialFact() =>
        CandidateProfileFact.Unsupported(
            MartialField(),
            Reason("BASE_MARTIAL_QUALIFICATION_UNSUPPORTED"),
            []);

    private static CandidateProfileFact ConflictingMartialFact()
    {
        var first = SaveProvenance(Sha);
        var second = SaveProvenance(OtherSha);
        return CandidateProfileFact.Conflicting(
            MartialField(),
            [
                new CandidateConflictValue(
                    CandidateFactValue.Int16(70),
                    first,
                    [new CandidateEvidenceReference("E6-SAVE-001", first)]),
                new CandidateConflictValue(
                    CandidateFactValue.Int16(80),
                    second,
                    [new CandidateEvidenceReference("E6-SAVE-002", second)])
            ],
            new CandidateConflictDecision(
                CandidateConflictDecisionKind.Unresolved,
                "NO_SAFE_PRECEDENCE"),
            []);
    }

    private static CandidateProfileFact LocationFact(
        int value,
        CandidateEvidenceState state) =>
        state switch
        {
            CandidateEvidenceState.Confirmed => CandidateProfileFact.Confirmed(
                LocationField(),
                CandidateFactValue.Int32(value),
                SaveProvenance(Sha),
                [Evidence()]),
            CandidateEvidenceState.Stale => CandidateProfileFact.Stale(
                LocationField(),
                CandidateFactValue.Int32(value),
                SaveProvenance(OtherSha),
                Reason("LOCATION_STALE"),
                [Evidence(OtherSha)]),
            CandidateEvidenceState.Incomplete => CandidateProfileFact.Incomplete(
                LocationField(),
                Reason("LOCATION_INCOMPLETE"),
                []),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported test location state.")
        };

    private static CandidateProfileFieldIdentity MartialField() => new(
        CandidateProfileField.BaseMartialQualification,
        MartialDiscipline());

    private static CandidateProfileFieldIdentity LocationField() => new(
        CandidateProfileField.CurrentLocationArea);

    private static CandidateDisciplineIdentity MartialDiscipline() => new(
        CandidateDisciplineDomain.Martial,
        0);

    private static CandidateProfileSourceVersions Versions() => new(
        Sha,
        VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        "1",
        VerifiedCompanionRoleDefinitions.FingerprintSchemaVersion);

    private static CandidateFactProvenance SaveProvenance(string revision) => new(
        CandidateEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        VerifiedCompanionRoleDefinitions.ProfileMappingVersion,
        revision);

    private static CandidateEvidenceReference Evidence(string revision = Sha) => new(
        "E6-SAVE-001",
        SaveProvenance(revision));

    private static CandidateUnavailableReason Reason(string code) => new(
        code,
        "Synthetic unavailable evidence for a Domain test.");
}
