using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWu.API.UnitTests.Controllers;
using TaiWuAPI.Localization;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

[Collection(CompanionCandidatesApiCollection.Name)]
public sealed class CompanionFinderViewModelMapperTests
{
    [Fact]
    public void Role_and_installed_discipline_options_are_bilingual_and_typed()
    {
        var englishRoles = CompanionFinderViewModelMapper.MapRoles(
            TaiwuLanguage.English);
        var chineseRoles = CompanionFinderViewModelMapper.MapRoles(
            TaiwuLanguage.Chinese);
        var englishDisciplines =
            CompanionFinderViewModelMapper.MapDisciplines(
                CompanionFinderTestData.Disciplines(),
                TaiwuLanguage.English);
        var chineseDisciplines =
            CompanionFinderViewModelMapper.MapDisciplines(
                CompanionFinderTestData.Disciplines(),
                TaiwuLanguage.Chinese);

        Assert.Equal(2, englishRoles.Count);
        Assert.Equal(
            englishRoles.Select(value => (value.Identity, value.Version, value.Domain)),
            chineseRoles.Select(value => (value.Identity, value.Version, value.Domain)));
        Assert.Equal("Martial discipline aptitude", englishRoles[0].Label);
        Assert.Equal("武學資質", chineseRoles[0].Label);
        Assert.Contains("not a universal ranking", englishRoles[0].ScoreLimitation);
        Assert.Equal(30, englishDisciplines.Count);
        Assert.Equal(14, englishDisciplines.Count(value =>
            value.Domain == CandidateDisciplineDomain.Martial));
        Assert.Equal(16, englishDisciplines.Count(value =>
            value.Domain == CandidateDisciplineDomain.LifeSkill));
        Assert.Equal("Martial discipline 1", englishDisciplines[0].DisplayName);
        Assert.Equal("武學類別1", chineseDisciplines[0].DisplayName);
        Assert.All(englishDisciplines, value => Assert.True(value.NameAvailable));
    }

    [Fact]
    public async Task Maps_every_rank_and_evidence_state_without_zero_fallback()
    {
        var result = await CompanionFinderTestData.ResultAsync();

        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");

        Assert.Equal(9, model.Counts.Total);
        Assert.Equal(8, model.Counts.Eligible);
        Assert.Equal(1, model.Counts.Ranked);
        Assert.Equal(2, model.Counts.Tied);
        Assert.Equal(5, model.Counts.NeedsReview);
        Assert.Equal(1, model.Counts.Ineligible);
        Assert.Equal(3, model.Counts.Incomplete);
        Assert.Equal(1, model.Counts.Unsupported);
        Assert.Equal(1, model.Counts.Conflicting);
        Assert.Contains("not current attainment", model.ScoreLimitation);
        Assert.Equal(
            CompanionCandidateSnapshotReadStatus.Complete,
            model.SnapshotReadStatus);
        Assert.Equal(
            CompanionCandidateEnrichmentStatus.Complete,
            model.Enrichment.Status);
        Assert.Equal(
            CombatSkillCatalogueStatus.Current,
            model.Enrichment.CatalogueStatus);
        Assert.False(model.Enrichment.NeedsAttention);

        var first = Assert.Single(model.Candidates, value =>
            value.DisplayName == "Synthetic Person A");
        Assert.Equal("Rank 1", first.RankLabel);
        Assert.Equal("90", first.ScoreLabel);
        Assert.Equal("Saved base value confirmed", first.EvidenceLabel);
        Assert.Equal("Synthetic Place A", first.LocationName);
        Assert.Equal(CompanionRoleEvaluationState.Rankable, first.EvaluationState);
        Assert.Equal("Rankable", first.EvaluationStateLabel);
        Assert.NotEmpty(first.Strengths);
        Assert.NotEmpty(first.Limitations);
        Assert.All(first.Gates, gate => Assert.True(gate.Passed));
        Assert.Equal(
            Enum.GetValues<CompanionRoleEvaluationState>()
                .OrderBy(value => value),
            model.Candidates.Select(value => value.EvaluationState)
                .Distinct()
                .OrderBy(value => value));
        var universeGate = Assert.Single(first.Gates, gate =>
            gate.Kind == CompanionRoleRequirementKind.CandidateUniverseEligible);
        Assert.Equal(1, universeGate.Order);
        Assert.Equal("CANDIDATE_UNIVERSE_ELIGIBLE", universeGate.RequirementIdentity);
        Assert.Null(universeGate.Field);
        Assert.Equal("Candidate-universe eligibility", universeGate.RequirementLabel);
        Assert.Equal(CompanionRoleGateOutcome.Passed, universeGate.Outcome);
        Assert.Equal("Passed", universeGate.OutcomeLabel);
        Assert.False(string.IsNullOrWhiteSpace(universeGate.ReasonIdentity));
        var qualificationGates = first.Gates.Where(gate =>
            gate.Field == CandidateProfileField.BaseMartialQualification).ToArray();
        Assert.Equal(2, qualificationGates.Length);
        Assert.Equal(2, qualificationGates.Select(gate =>
            gate.RequirementIdentity).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(qualificationGates, gate =>
            gate.Kind == CompanionRoleRequirementKind.RequiredFactConfirmed);
        Assert.Contains(qualificationGates, gate =>
            gate.Kind == CompanionRoleRequirementKind.FactProvenanceCompatible);

        var tied = model.Candidates.Where(value =>
            value.RankingState == CompanionRoleCandidateRankingState.Tied)
            .ToArray();
        Assert.Equal(2, tied.Length);
        Assert.All(tied, value => Assert.Equal("Tied at rank 2", value.RankLabel));

        var unranked = model.Candidates.Where(value =>
            value.Section != CompanionCandidateSection.Ranked).ToArray();
        Assert.All(unranked, value => Assert.Equal("Not ranked", value.RankLabel));
        Assert.All(unranked, value =>
            Assert.NotEqual("0", value.ScoreLabel));
        Assert.Contains(unranked, value =>
            value.EvidenceLabel == "Required evidence missing");
        Assert.Contains(unranked, value =>
            value.EvidenceLabel == "Evidence incomplete");
        Assert.Contains(unranked, value =>
            value.EvidenceLabel == "Evidence unsupported");
        Assert.Contains(unranked, value =>
            value.EvidenceLabel == "Evidence no longer current");
        Assert.Contains(unranked, value =>
            value.EvidenceLabel == "Evidence conflicts");
    }

    [Fact]
    public async Task Partial_snapshot_state_remains_typed_separately_from_current_enrichment()
    {
        var result = await CompanionFinderTestData.ResultAsync(
            partialSnapshot: true);

        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");

        Assert.True(model.IsPartial);
        Assert.Equal(
            CompanionCandidateSnapshotReadStatus.Partial,
            model.SnapshotReadStatus);
        Assert.Equal(
            CompanionCandidateEnrichmentStatus.Complete,
            model.Enrichment.Status);
        Assert.False(model.Enrichment.NeedsAttention);
    }

    [Theory]
    [InlineData(
        CompanionCandidateEnrichmentStatus.Complete,
        CombatSkillCatalogueStatus.Current,
        false,
        "Catalogue evidence current")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.Partial,
        CombatSkillCatalogueStatus.Current,
        true,
        "Some candidate evidence is incomplete")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueMissing,
        CombatSkillCatalogueStatus.Missing,
        true,
        "Local catalogue missing")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueMissing,
        CombatSkillCatalogueStatus.MissingSources,
        true,
        "Installed catalogue sources missing")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueStale,
        CombatSkillCatalogueStatus.Stale,
        true,
        "Local catalogue is stale")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueRebuilding,
        CombatSkillCatalogueStatus.Rebuilding,
        true,
        "Catalogue rebuild in progress")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueUnsupported,
        CombatSkillCatalogueStatus.UnsupportedVersion,
        true,
        "Catalogue version unsupported")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueFailed,
        CombatSkillCatalogueStatus.SourceReadFailed,
        true,
        "Could not read catalogue sources")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueFailed,
        CombatSkillCatalogueStatus.RepositoryFailed,
        true,
        "Local catalogue unavailable")]
    [InlineData(
        CompanionCandidateEnrichmentStatus.CatalogueFailed,
        CombatSkillCatalogueStatus.Corrupt,
        true,
        "Local catalogue is corrupt")]
    public void Enrichment_and_catalogue_states_remain_typed_and_actionable(
        CompanionCandidateEnrichmentStatus status,
        CombatSkillCatalogueStatus catalogueStatus,
        bool needsAttention,
        string expectedEnglishTitle)
    {
        var english = CompanionFinderViewModelMapper.MapEnrichment(
            status,
            catalogueStatus,
            TaiwuLanguage.English);
        var chinese = CompanionFinderViewModelMapper.MapEnrichment(
            status,
            catalogueStatus,
            TaiwuLanguage.Chinese);

        Assert.Equal(status, english.Status);
        Assert.Equal(catalogueStatus, english.CatalogueStatus);
        Assert.Equal(needsAttention, english.NeedsAttention);
        Assert.Equal(expectedEnglishTitle, english.Title);
        Assert.False(string.IsNullOrWhiteSpace(english.Message));
        Assert.Equal(status, chinese.Status);
        Assert.Equal(catalogueStatus, chinese.CatalogueStatus);
        Assert.Equal(needsAttention, chinese.NeedsAttention);
        Assert.NotEqual(english.Title, chinese.Title);
        Assert.NotEqual(english.Message, chinese.Message);
    }

    [Fact]
    public async Task Language_changes_display_only_and_preserves_exact_facts()
    {
        var result = await CompanionFinderTestData.ResultAsync();

        var english = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");
        var chinese = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.Chinese,
            "範例武學類別");

        Assert.Equal(
            english.Candidates.Select(value => (
                value.CharacterId,
                value.RankingState,
                value.EvaluationState,
                value.CompetitionRank,
                value.RoleLocalScore)),
            chinese.Candidates.Select(value => (
                value.CharacterId,
                value.RankingState,
                value.EvaluationState,
                value.CompetitionRank,
                value.RoleLocalScore)));
        Assert.Equal("Synthetic Person A", english.Candidates[0].DisplayName);
        Assert.Equal("範例人物甲", chinese.Candidates[0].DisplayName);
        Assert.Equal("Synthetic Place A", english.Candidates[0].LocationName);
        Assert.Equal("範例地點甲", chinese.Candidates[0].LocationName);
        Assert.NotEqual(
            english.Candidates[0].EvaluationStateLabel,
            chinese.Candidates[0].EvaluationStateLabel);
        Assert.Equal(
            english.Candidates[0].Gates.Select(gate => (
                gate.Order,
                gate.RequirementIdentity,
                gate.Kind,
                gate.Field,
                gate.Outcome,
                gate.ReasonIdentity)),
            chinese.Candidates[0].Gates.Select(gate => (
                gate.Order,
                gate.RequirementIdentity,
                gate.Kind,
                gate.Field,
                gate.Outcome,
                gate.ReasonIdentity)));
        Assert.NotEqual(
            english.Candidates[0].Gates[0].RequirementLabel,
            chinese.Candidates[0].Gates[0].RequirementLabel);
        Assert.Contains("整體價值", chinese.ScoreLimitation);
    }

    [Fact]
    public async Task Comparison_uses_the_same_evaluations_and_reports_equal_tie()
    {
        var result = await CompanionFinderTestData.ResultAsync();
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");

        var comparison = CompanionFinderViewModelMapper.MapComparison(
            result,
            model,
            31002,
            31003,
            TaiwuLanguage.English);

        Assert.Equal("Synthetic Person B", comparison.FirstCandidateName);
        Assert.Equal("Synthetic Person C", comparison.SecondCandidateName);
        Assert.Equal("Equal confirmed evidence", comparison.Outcome);
        Assert.Contains(comparison.Facts, value =>
            value.Label == "Evaluation state"
            && value.FirstValue == "Rankable"
            && value.SecondValue == "Rankable");
        Assert.DoesNotContain(comparison.Facts, value =>
            value.Label == "Evaluation state"
            && (value.FirstValue == "Tied" || value.SecondValue == "Tied"));
        Assert.Contains(comparison.Facts, value =>
            value.Label == "Saved base qualification"
            && value.FirstValue == "75"
            && value.SecondValue == "75");
        Assert.Contains(comparison.Facts, value =>
            value.Label == "Competition rank"
            && value.FirstValue == "Tied at rank 2"
            && value.SecondValue == "Tied at rank 2");
        Assert.Contains(comparison.Facts, value =>
            value.Label == "Hard gates"
            && value.FirstValue.Contains(
                "Candidate-universe eligibility — Passed",
                StringComparison.Ordinal)
            && value.FirstValue.Contains(
                "Required saved base martial qualification evidence — Passed",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task Filters_and_comparison_mutate_only_session_presentation_state()
    {
        var result = await CompanionFinderTestData.ResultAsync();
        var model = CompanionFinderViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            "Synthetic martial discipline");
        var state = new CompanionFinderInteractionState();

        state.SetFilter(CompanionRoleShortlistFilter.Ranked);
        Assert.Equal(3, state.VisibleCandidates(model).Count);
        state.SetNameQuery("person b");
        var filtered = Assert.Single(state.VisibleCandidates(model));
        Assert.Equal("Synthetic Person B", filtered.DisplayName);
        Assert.Equal(2, filtered.CompetitionRank);
        Assert.Equal(75, filtered.RoleLocalScore);

        state.ToggleComparison(model, 31002);
        state.ToggleComparison(model, 31003);
        state.SetFilter(CompanionRoleShortlistFilter.Ineligible);
        state.SetNameQuery(null);
        Assert.True(state.ComparisonReady);
        Assert.Equal([31002, 31003], state.SelectedCharacterIds);
        Assert.False(state.CanSelect(31004));
        Assert.Single(state.VisibleCandidates(model));
        Assert.Throws<InvalidOperationException>(() =>
            state.ToggleComparison(model, 31004));

        var cleared = state.ClearComparison();
        Assert.Equal([31002, 31003], cleared);
        Assert.Empty(state.SelectedCharacterIds);
        Assert.True(state.CanSelect(31004));
        Assert.Equal(9, model.Counts.Total);
    }

    [Theory]
    [InlineData(CompanionFinderStatus.SaveUnavailable, true)]
    [InlineData(CompanionFinderStatus.ChangedRevision, true)]
    [InlineData(CompanionFinderStatus.UnsupportedSourceVersion, false)]
    [InlineData(CompanionFinderStatus.ReadFailed, true)]
    [InlineData(CompanionFinderStatus.Failed, true)]
    public void Failure_notices_are_safe_and_actionable(
        CompanionFinderStatus status,
        bool canRetry)
    {
        var notice = CompanionFinderViewModelMapper.MapFailure(
            status,
            TaiwuLanguage.English);

        Assert.Equal(CompanionFinderNoticeStatus.Failure, notice.Status);
        Assert.Equal(canRetry, notice.CanRetry);
        Assert.False(string.IsNullOrWhiteSpace(notice.Title));
        Assert.False(string.IsNullOrWhiteSpace(notice.Message));
        Assert.DoesNotContain("\\", notice.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", notice.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_companion_ui_identity_has_distinct_complete_bilingual_copy()
    {
        foreach (var key in Enum.GetValues<CompanionFinderUiTextKey>())
        {
            var english = CompanionFinderUiText.Get(TaiwuLanguage.English, key);
            var chinese = CompanionFinderUiText.Get(TaiwuLanguage.Chinese, key);

            Assert.False(string.IsNullOrWhiteSpace(english));
            Assert.False(string.IsNullOrWhiteSpace(chinese));
            Assert.NotEqual(english, chinese);
        }
    }
}
