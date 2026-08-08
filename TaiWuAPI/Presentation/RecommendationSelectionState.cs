using TaiWu.Domain.CombatRecommendations;

namespace TaiWuAPI.Presentation;

public sealed class RecommendationSelectionState
{
    public CombatRecommendationViewModel? Recommendation { get; private set; }

    public RecommendationPolicy VisibleStyle { get; private set; } =
        RecommendationPolicy.Balanced;

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
        var requested = recommendation.Styles.SingleOrDefault(
            style => style.Style == visibleStyle);
        if (requested is null)
        {
            throw new ArgumentException(
                "The requested style is not present in the recommendation.",
                nameof(visibleStyle));
        }

        var initial = requested.HasRecommendation
            ? visibleStyle
            : recommendation.Styles.FirstOrDefault(
                style => style.HasRecommendation)?.Style
                ?? recommendation.Styles.FirstOrDefault(
                    style => style.Style == RecommendationPolicy.Safe)?.Style
                ?? requested.Style;
        ShowStyle(initial);
    }

    public void Clear()
    {
        Recommendation = null;
        SelectedThreatReference = null;
        VisibleStyle = RecommendationPolicy.Balanced;
    }

    public void ShowStyle(RecommendationPolicy style)
    {
        if (Recommendation is null
            || Recommendation.Styles.All(value => value.Style != style))
        {
            throw new ArgumentException(
                "The requested style is not present in the recommendation.",
                nameof(style));
        }

        VisibleStyle = style;
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
