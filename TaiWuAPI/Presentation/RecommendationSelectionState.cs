using TaiWu.Domain.CombatRecommendations;

namespace TaiWuAPI.Presentation;

public sealed class RecommendationSelectionState
{
    public CombatRecommendationViewModel? Recommendation { get; private set; }

    public RecommendationPolicy VisibleStyle { get; private set; } =
        RecommendationPolicy.Safe;

    public string? SelectedThreatReference { get; private set; }

    public RecommendationStyleViewModel? VisibleRecommendation =>
        Recommendation?.Styles.Single(
            style => style.Style == VisibleStyle);

    public void Load(
        CombatRecommendationViewModel recommendation,
        RecommendationPolicy visibleStyle)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        Recommendation = recommendation;
        SelectedThreatReference = null;
        if (!Enum.IsDefined(visibleStyle))
        {
            throw new ArgumentException(
                "The requested style is unknown.",
                nameof(visibleStyle));
        }

        var visibleStyles = RecommendationPolicyDisplay.VisiblePolicies
            .Select(policy => recommendation.Styles.SingleOrDefault(
                style => style.Style == policy))
            .Where(style => style is not null)
            .Cast<RecommendationStyleViewModel>()
            .ToArray();
        if (visibleStyles.Length == 0)
        {
            throw new ArgumentException(
                "The recommendation has no user-facing style.",
                nameof(recommendation));
        }

        var requested = visibleStyles.SingleOrDefault(
            style => style.Style == visibleStyle);
        var initial = requested?.HasRecommendation == true
            ? requested.Style
            : visibleStyles.FirstOrDefault(
                style => style.HasRecommendation)?.Style
                ?? visibleStyles[0].Style;
        ShowStyle(initial);
    }

    public void Clear()
    {
        Recommendation = null;
        SelectedThreatReference = null;
        VisibleStyle = RecommendationPolicy.Safe;
    }

    public void ShowStyle(RecommendationPolicy style)
    {
        if (Recommendation is null
            || !RecommendationPolicyDisplay.IsVisible(style)
            || Recommendation.Styles.All(value => value.Style != style))
        {
            throw new ArgumentException(
                "The requested style is not present in the recommendation.",
                nameof(style));
        }

        VisibleStyle = style;
    }

    public void RestoreInteraction(
        RecommendationPolicy visibleStyle,
        string? selectedThreatReference)
    {
        ShowStyle(visibleStyle);
        SelectedThreatReference = selectedThreatReference is not null
            && Recommendation!.Threats.Any(threat =>
                threat.Reference == selectedThreatReference)
                ? selectedThreatReference
                : null;
    }

    public void SelectThreat(string? threatReference)
    {
        if (threatReference is null)
        {
            SelectedThreatReference = null;
            return;
        }

        if (Recommendation is null
            || Recommendation.Threats.All(
                threat => threat.Reference != threatReference))
        {
            throw new ArgumentException(
                "The selected threat is not present in the recommendation.",
                nameof(threatReference));
        }

        SelectedThreatReference =
            SelectedThreatReference == threatReference
                ? null
                : threatReference;
    }

    public bool AddressesSelectedThreat(
        IEnumerable<string> threatReferences)
    {
        ArgumentNullException.ThrowIfNull(threatReferences);
        return SelectedThreatReference is not null
            && threatReferences.Contains(
                SelectedThreatReference,
                StringComparer.Ordinal);
    }
}
