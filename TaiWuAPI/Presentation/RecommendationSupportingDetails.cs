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
    string UnknownValuePolicy);

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

        return new RecommendationSupportingDetailsViewModel(
            recommendation.Styles
                .Where(style => style.Reference != selectedStyle.Reference)
                .Select(style => new AlternativeStyleViewModel(
                    style.Reference,
                    style.Style,
                    style.HasRecommendation,
                    style.TotalScore,
                    style.ManualChanges.Count,
                    style.Caveats.Count,
                    style.Diagnostic))
                .ToArray(),
            selectedStyle.Caveats
                .Where(caveat =>
                    caveat.Kind == RecommendationCaveatKind.Assumption)
                .ToArray(),
            selectedStyle.Caveats
                .Where(caveat =>
                    caveat.Kind == RecommendationCaveatKind.UnavailableData)
                .ToArray(),
            skills
                .SelectMany(skill => skill.Conditions
                    .Where(condition =>
                        condition.Criticality
                        == CombatRequirementCriticality.Conditional)
                    .Select(condition => new SupportingConditionViewModel(
                        condition.Reference,
                        skill.Reference,
                        skill.Name ?? $"Skill {skill.SkillId}",
                        condition)))
                .ToArray(),
            selectedStyle.Scores,
            evidence,
            UnknownValuePolicy);
    }
}
