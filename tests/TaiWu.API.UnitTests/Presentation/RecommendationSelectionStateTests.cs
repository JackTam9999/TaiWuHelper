using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatThreats;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class RecommendationSelectionStateTests
{
    [Theory]
    [InlineData(RecommendationPolicy.Safe)]
    [InlineData(RecommendationPolicy.Balanced)]
    [InlineData(RecommendationPolicy.Aggressive)]
    public void Every_recommendation_style_can_be_selected(
        RecommendationPolicy style)
    {
        var model = Model();
        var state = new RecommendationSelectionState();
        state.Load(model, RecommendationPolicy.Balanced);

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
    public void Initial_selection_uses_first_feasible_style_when_requested_is_infeasible()
    {
        var state = new RecommendationSelectionState();

        state.Load(
            Model(infeasibleStyle: RecommendationPolicy.Balanced),
            RecommendationPolicy.Balanced);

        Assert.Equal(RecommendationPolicy.Safe, state.VisibleStyle);
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
        state.Load(Model(), RecommendationPolicy.Balanced);

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
        state.Load(Model(), RecommendationPolicy.Balanced);

        Assert.Throws<ArgumentException>(
            () => state.ShowStyle((RecommendationPolicy)999));
        Assert.Throws<ArgumentException>(
            () => new RecommendationSelectionState().Load(
                Model(),
                (RecommendationPolicy)999));
        Assert.Throws<ArgumentException>(
            () => state.SelectThreat("threat:UNKNOWN"));
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
