using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Presentation;

public sealed record RecommendationSupportingDetailsViewModel(
    IReadOnlyList<AlternativeStyleViewModel> Alternatives,
    IReadOnlyList<RecommendationCaveatViewModel> Assumptions,
    IReadOnlyList<RecommendationCaveatViewModel> UnavailableData,
    IReadOnlyList<SupportingConditionViewModel> ConditionalRequirements,
    IReadOnlyList<RecommendationScoreViewModel> Scores,
    IReadOnlyList<string> EvidenceReferences,
    IReadOnlyList<SupportingEvidenceSummaryViewModel> EvidenceSummaries,
    string UnknownValuePolicy);

public sealed record SupportingEvidenceSummaryViewModel(
    string Name,
    string Kind,
    int SourceCount);

public sealed record AlternativeStyleViewModel(
    string Reference,
    RecommendationPolicy Style,
    bool HasRecommendation,
    decimal? TotalScore,
    int ManualChangeCount,
    int CaveatCount,
    string? Diagnostic);

public sealed record SupportingConditionViewModel(
    string Reference,
    string SkillReference,
    string SkillName,
    SkillConditionViewModel Condition);

public static class RecommendationSupportingDetailsBuilder
{
    public const string UnknownValuePolicy =
        "Unknown values remain unavailable. TaiWu Helper never replaces them "
        + "with estimates.";

    public static RecommendationSupportingDetailsViewModel Build(
        CombatRecommendationViewModel recommendation,
        RecommendationStyleViewModel selectedStyle)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        ArgumentNullException.ThrowIfNull(selectedStyle);
        if (recommendation.Styles.All(
                style => style.Reference != selectedStyle.Reference))
        {
            throw new ArgumentException(
                "The selected style does not belong to the recommendation.",
                nameof(selectedStyle));
        }

        var skills = selectedStyle.Categories
            .SelectMany(category => category.Skills)
            .ToArray();
        var evidence = recommendation.Threats
            .SelectMany(threat => threat.EvidenceReferences)
            .Concat(selectedStyle.Scores.Select(score =>
                score.EvidenceReference))
            .Concat(skills.SelectMany(skill =>
                skill.Cost.EvidenceReferences))
            .Concat(skills
                .Where(skill => skill.Counter.EvidenceReference is not null)
                .Select(skill => skill.Counter.EvidenceReference!))
            .Concat(skills
                .SelectMany(skill => skill.Conditions)
                .Select(condition => condition.EvidenceReference))
            .Concat(skills
                .SelectMany(skill => skill.Reasons)
                .SelectMany(reason => reason.EvidenceReferences))
            .Concat(selectedStyle.Caveats
                .SelectMany(caveat => caveat.EvidenceReferences))
            .Concat(recommendation.Warnings
                .SelectMany(warning => warning.EvidenceReferences))
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var evidenceSummaries = BuildEvidenceSummaries(
            recommendation,
            selectedStyle,
            skills);

        return new RecommendationSupportingDetailsViewModel(
            [.. recommendation.Styles
                .Where(style => style.Reference != selectedStyle.Reference)
                .Select(style => new AlternativeStyleViewModel(
                    style.Reference,
                    style.Style,
                    style.HasRecommendation,
                    style.TotalScore,
                    style.ManualChanges.Count,
                    style.Caveats.Count,
                    style.Diagnostic))],
            [.. selectedStyle.Caveats
                .Where(caveat =>
                    caveat.Kind == RecommendationCaveatKind.Assumption)],
            [.. selectedStyle.Caveats
                .Where(caveat =>
                    caveat.Kind == RecommendationCaveatKind.UnavailableData)],
            [.. skills
                .SelectMany(skill => skill.Conditions
                    .Where(condition =>
                        condition.Criticality
                        == CombatRequirementCriticality.Conditional)
                    .Select(condition => new SupportingConditionViewModel(
                        condition.Reference,
                        skill.Reference,
                        skill.Name ?? "Unnamed skill",
                        condition)))],
            selectedStyle.Scores,
            evidence,
            evidenceSummaries,
            UnknownValuePolicy);
    }

    private static SupportingEvidenceSummaryViewModel[]
        BuildEvidenceSummaries(
            CombatRecommendationViewModel recommendation,
            RecommendationStyleViewModel selectedStyle,
            RecommendedSkillViewModel[] skills)
    {
        IEnumerable<SupportingEvidenceSummaryViewModel> threatEvidence =
            recommendation.Threats
            .Where(threat => threat.EvidenceReferences.Count > 0)
            .Select(threat => new SupportingEvidenceSummaryViewModel(
                threat.Title,
                "Target threat",
                threat.EvidenceReferences.Distinct().Count()));
        IEnumerable<SupportingEvidenceSummaryViewModel> skillEvidence = skills
            .Select(skill => new
            {
                Name = skill.Name ?? "Unnamed skill",
                References = skill.Cost.EvidenceReferences
                    .Concat(skill.Counter.EvidenceReference is null
                        ? []
                        : [skill.Counter.EvidenceReference])
                    .Concat(skill.Conditions.Select(condition =>
                        condition.EvidenceReference))
                    .Concat(skill.Reasons.SelectMany(reason =>
                        reason.EvidenceReferences))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            })
            .Where(skill => skill.References.Length > 0)
            .Select(skill => new SupportingEvidenceSummaryViewModel(
                skill.Name,
                "Recommended skill",
                skill.References.Length));
        var scoreCount = selectedStyle.Scores
            .Select(score => score.EvidenceReference)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .Count();
        var reviewCount = selectedStyle.Caveats
            .SelectMany(caveat => caveat.EvidenceReferences)
            .Concat(recommendation.Warnings.SelectMany(warning =>
                warning.EvidenceReferences))
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .Count();

        return
        [
            .. threatEvidence,
            .. skillEvidence,
            .. scoreCount == 0
                ? []
                : new SupportingEvidenceSummaryViewModel[]
                {
                    new("Recommendation scoring", "Analysis", scoreCount)
                },
            .. reviewCount == 0
                ? []
                : new SupportingEvidenceSummaryViewModel[]
                {
                    new("Warnings and caveats", "Manual review", reviewCount)
                }
        ];
    }
}
