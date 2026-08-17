using System.Globalization;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

public static partial class CompanionFinderViewModelMapper
{
    public static CompanionComparisonViewModel MapComparison(
        CompanionFinderResult result,
        CompanionFinderViewModel model,
        int firstCharacterId,
        int secondCharacterId,
        TaiwuLanguage language)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(model);
        if (!result.HasAuthoritativeResult)
        {
            throw new ArgumentException(
                "An authoritative companion-finder result is required.",
                nameof(result));
        }

        var comparison = CompanionRoleComparisonBuilder.Compare(
            result.Shortlist!,
            firstCharacterId,
            secondCharacterId);
        var first = model.Candidates.Single(value =>
            value.CharacterId == firstCharacterId);
        var second = model.Candidates.Single(value =>
            value.CharacterId == secondCharacterId);
        var facts = new List<CompanionComparisonFactViewModel>
        {
            new(
                Text(language, CompanionFinderUiTextKey.EvaluationState),
                first.EvaluationStateLabel,
                second.EvaluationStateLabel),
            new(
                Text(language, CompanionFinderUiTextKey.HardGates),
                GateSummary(first, language),
                GateSummary(second, language))
        };
        foreach (var row in comparison.Rows)
        {
            if (row.Dimension.Field
                == CandidateProfileField.CapabilityBreadthIndex)
            {
                continue;
            }

            facts.Add(new CompanionComparisonFactViewModel(
                Text(
                    language,
                    CompanionFinderUiTextKey.SavedBaseQualification),
                ComparisonValue(row.First, language),
                ComparisonValue(row.Second, language)));
        }

        if (model.RequiresDisciplineSelection)
        {
            facts.Add(new CompanionComparisonFactViewModel(
                Text(language, CompanionFinderUiTextKey.Evidence),
                first.EvidenceLabel,
                second.EvidenceLabel));
            facts.Add(new CompanionComparisonFactViewModel(
                Text(language, CompanionFinderUiTextKey.RoleLocalScore),
                first.ScoreLabel,
                second.ScoreLabel));
        }

        facts.Add(new CompanionComparisonFactViewModel(
            Text(language, CompanionFinderUiTextKey.CompetitionRank),
            first.RankLabel,
            second.RankLabel));
        var capability = new CompanionCapabilityComparisonViewModel(
            Text(language, CompanionFinderUiTextKey.CapabilityOverview),
            model.RequiresDisciplineSelection
                ? Text(language, CompanionFinderUiTextKey.CapabilityLimitation)
                : model.ScoreLimitation,
            [
                new CompanionCapabilityComparisonFactViewModel(
                    Text(language, CompanionFinderUiTextKey.BreadthIndex),
                    first.CapabilitySummary.BreadthIndexLabel,
                    string.Empty,
                    second.CapabilitySummary.BreadthIndexLabel,
                    string.Empty),
                CapabilityFact(
                    first.CapabilitySummary.MainAttributes,
                    second.CapabilitySummary.MainAttributes,
                    language),
                CapabilityFact(
                    first.CapabilitySummary.MartialDisciplines,
                    second.CapabilitySummary.MartialDisciplines,
                    language),
                CapabilityFact(
                    first.CapabilitySummary.LifeSkillDisciplines,
                    second.CapabilitySummary.LifeSkillDisciplines,
                    language)
            ]);
        return new CompanionComparisonViewModel(
            first.DisplayName,
            second.DisplayName,
            CompanionFinderApiText.ComparisonOutcome(
                language,
                comparison.Outcome),
            capability,
            facts);
    }

    private static CompanionCapabilityComparisonFactViewModel CapabilityFact(
        CompanionCapabilityCategoryViewModel first,
        CompanionCapabilityCategoryViewModel second,
        TaiwuLanguage language) => new(
        first.Label,
        first.ScoreLabel,
        CapabilityDetail(first, language),
        second.ScoreLabel,
        CapabilityDetail(second, language));

    private static string CapabilityDetail(
        CompanionCapabilityCategoryViewModel category,
        TaiwuLanguage language)
    {
        var coverage = $"{Text(language, CompanionFinderUiTextKey.ConfirmedCoverage)}: "
            + category.CoverageLabel;
        if (category.TopValues.Count == 0)
        {
            return coverage;
        }

        return coverage + "; "
            + Text(language, CompanionFinderUiTextKey.TopValues)
            + ": "
            + string.Join(
                ", ",
                category.TopValues.Select(value =>
                    $"{value.Label} {value.Value.ToString(CultureInfo.InvariantCulture)}"));
    }

    private static string GateSummary(
        CompanionCandidateViewModel candidate,
        TaiwuLanguage language) => candidate.Gates.Count == 0
        ? Text(language, CompanionFinderUiTextKey.Unavailable)
        : string.Join(
            "; ",
            candidate.Gates.Select(value =>
                $"{value.RequirementLabel} — {value.OutcomeLabel}: "
                + value.Explanation));

    private static string ComparisonValue(
        CompanionRoleComparisonValue value,
        TaiwuLanguage language) => value.Value?.ToString(
        CultureInfo.InvariantCulture) ?? Text(
        language,
        value.State switch
        {
            CompanionRoleComparisonEvidenceState.Missing =>
                CompanionFinderUiTextKey.MissingEvidence,
            CompanionRoleComparisonEvidenceState.Incomplete =>
                CompanionFinderUiTextKey.IncompleteEvidence,
            CompanionRoleComparisonEvidenceState.Unsupported =>
                CompanionFinderUiTextKey.UnsupportedEvidence,
            CompanionRoleComparisonEvidenceState.Stale =>
                CompanionFinderUiTextKey.StaleEvidence,
            CompanionRoleComparisonEvidenceState.Conflicting =>
                CompanionFinderUiTextKey.ConflictingEvidence,
            CompanionRoleComparisonEvidenceState.Confirmed =>
                CompanionFinderUiTextKey.Unavailable,
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                value.State,
                "Unknown comparison evidence state.")
        });
}
