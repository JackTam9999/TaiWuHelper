using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatThreats;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class RecommendationSelectionStateTests
{
    [Theory]
    [InlineData(RecommendationPolicy.Safe)]
    [InlineData(RecommendationPolicy.Aggressive)]
    public void Every_user_facing_recommendation_style_can_be_selected(
        RecommendationPolicy style)
    {
        var model = Model();
        var state = new RecommendationSelectionState();
        state.Load(model, RecommendationPolicy.Safe);

        state.ShowStyle(style);

        Assert.Equal(style, state.VisibleStyle);
        Assert.Equal(style, state.VisibleRecommendation?.Style);
        Assert.Same(model, state.Recommendation);
    }

    [Fact]
    public void Style_changes_reuse_the_loaded_snapshot()
    {
        var model = Model();
        var state = new RecommendationSelectionState();
        state.Load(model, RecommendationPolicy.Safe);

        state.ShowStyle(RecommendationPolicy.Aggressive);

        Assert.Same(model, state.Recommendation);
        Assert.Equal(
            RecommendationPolicy.Aggressive,
            state.VisibleRecommendation?.Style);
        Assert.Equal(
            model.SnapshotReference,
            state.VisibleRecommendation?.SnapshotReference);
    }

    [Fact]
    public void Balanced_backend_style_is_not_user_selectable()
    {
        var model = Model();
        var state = new RecommendationSelectionState();

        state.Load(model, RecommendationPolicy.Balanced);

        Assert.Equal(RecommendationPolicy.Safe, state.VisibleStyle);
        Assert.Throws<ArgumentException>(
            () => state.ShowStyle(RecommendationPolicy.Balanced));
        Assert.Contains(
            model.Styles,
            style => style.Style == RecommendationPolicy.Balanced);
    }

    [Fact]
    public void Initial_selection_uses_other_visible_style_when_safe_is_infeasible()
    {
        var state = new RecommendationSelectionState();

        state.Load(
            Model(infeasibleStyle: RecommendationPolicy.Safe),
            RecommendationPolicy.Safe);

        Assert.Equal(RecommendationPolicy.Aggressive, state.VisibleStyle);
        Assert.True(state.VisibleRecommendation?.HasRecommendation);
    }

    [Fact]
    public void Initial_selection_defaults_to_safe_when_every_style_is_infeasible()
    {
        var state = new RecommendationSelectionState();

        state.Load(
            Model(allStylesInfeasible: true),
            RecommendationPolicy.Aggressive);

        Assert.Equal(RecommendationPolicy.Safe, state.VisibleStyle);
        Assert.False(state.VisibleRecommendation?.HasRecommendation);
    }

    [Fact]
    public void Selected_threat_highlights_only_linked_content_and_toggles()
    {
        var state = new RecommendationSelectionState();
        state.Load(Model(), RecommendationPolicy.Safe);

        state.SelectThreat("threat:MAGIC_SOUND");

        Assert.True(
            state.AddressesSelectedThreat(["threat:MAGIC_SOUND"]));
        Assert.False(
            state.AddressesSelectedThreat(["threat:OTHER"]));

        state.SelectThreat("threat:MAGIC_SOUND");

        Assert.Null(state.SelectedThreatReference);
    }

    [Fact]
    public void Unknown_style_and_threat_are_rejected()
    {
        var state = new RecommendationSelectionState();
        state.Load(Model(), RecommendationPolicy.Safe);

        Assert.Throws<ArgumentException>(
            () => state.ShowStyle((RecommendationPolicy)999));
        Assert.Throws<ArgumentException>(
            () => new RecommendationSelectionState().Load(
                Model(),
                (RecommendationPolicy)999));
        Assert.Throws<ArgumentException>(
            () => state.SelectThreat("threat:UNKNOWN"));
    }

    [Fact]
    public void Language_reload_restores_policy_threat_and_filter_mode()
    {
        var selection = new RecommendationSelectionState();
        selection.Load(Model(), RecommendationPolicy.Safe);
        selection.ShowStyle(RecommendationPolicy.Safe);
        selection.SelectThreat("threat:MAGIC_SOUND");
        var filter = new LoadoutComparisonFilterState();
        filter.LoadComparison("comparison:chinese");
        filter.ShowDifferences();

        selection.Load(Model(), RecommendationPolicy.Safe);
        filter.LoadComparison(
            "comparison:english",
            preserveMode: true);
        selection.RestoreInteraction(
            RecommendationPolicy.Safe,
            "threat:MAGIC_SOUND");

        Assert.Equal(RecommendationPolicy.Safe, selection.VisibleStyle);
        Assert.Equal(
            "threat:MAGIC_SOUND",
            selection.SelectedThreatReference);
        Assert.True(filter.DifferencesOnly);
        filter.LoadComparison("comparison:new-target");
        Assert.False(filter.DifferencesOnly);
    }

    private static CombatRecommendationViewModel Model(
        RecommendationPolicy? infeasibleStyle = null,
        bool allStylesInfeasible = false)
    {
        const string snapshotReference = "snapshot:test";
        return new CombatRecommendationViewModel(
            snapshotReference,
            DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
            DateTimeOffset.Parse("2026-07-30T11:59:00Z"),
            "1.0.0",
            RecommendationPolicy.Balanced,
            $"{snapshotReference}:style:Balanced",
            CombatRecommendationViewModelMapper.InformationOnlyNotice,
            [
                new ThreatViewModel(
                    "threat:MAGIC_SOUND",
                    "MAGIC_SOUND",
                    "Magic sound",
                    "Applies mind pressure.",
                    TargetThreatKind.MindDamagePressure,
                    TargetThreatSeverity.Critical,
                    TargetThreatActivationTiming.OnHit,
                    ["evidence:test"])
            ],
            [.. Enum.GetValues<RecommendationPolicy>()
                .Select(policy => new RecommendationStyleViewModel(
                    $"{snapshotReference}:style:{policy}",
                    snapshotReference,
                    policy,
                    policy == RecommendationPolicy.Balanced,
                    HasRecommendation: !allStylesInfeasible
                        && policy != infeasibleStyle,
                    CandidateReference: $"candidate:{policy}",
                    TotalScore: 1,
                    Scores: [],
                    Categories: [],
                    ManualChanges: [],
                    OpeningActions: [],
                    SwitchingConditions: [],
                    Caveats: [],
                    Diagnostic: allStylesInfeasible
                        || policy == infeasibleStyle
                            ? "No feasible scored candidate is available "
                                + "for a manual combat plan."
                            : null))],
            Warnings: []);
    }
}
