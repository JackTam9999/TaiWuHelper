using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatThreats;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class RecommendationSelectionStateTests
{
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
            () => state.SelectThreat("threat:UNKNOWN"));
    }

    private static CombatRecommendationViewModel Model()
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
            Enum.GetValues<RecommendationPolicy>()
                .Select(policy => new RecommendationStyleViewModel(
                    $"{snapshotReference}:style:{policy}",
                    snapshotReference,
                    policy,
                    policy == RecommendationPolicy.Balanced,
                    HasRecommendation: true,
                    CandidateReference: $"candidate:{policy}",
                    TotalScore: 1,
                    Scores: [],
                    Categories: [],
                    ManualChanges: [],
                    OpeningActions: [],
                    SwitchingConditions: [],
                    Caveats: [],
                    Diagnostic: null))
                .ToArray(),
            Warnings: []);
    }
}
