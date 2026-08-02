using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class RecommendationWarningPresentationTests
{
    [Theory]
    [InlineData(
        "Snapshot",
        "STALE_SAVE",
        PresentationWarningKind.StaleData,
        false)]
    [InlineData(
        "Snapshot",
        "CURRENT_SCREEN_OBSERVATION_NOT_NEWER",
        PresentationWarningKind.ObservationDifference,
        false)]
    [InlineData(
        "Snapshot",
        "TARGET_LOADOUT_NOT_PERSISTED",
        PresentationWarningKind.UnavailableValue,
        false)]
    [InlineData(
        "ThreatAnalysis",
        "TARGET_EQUIPPED_SKILLS_UNAVAILABLE",
        PresentationWarningKind.UnavailableValue,
        false)]
    [InlineData(
        "ThreatAnalysis",
        "TARGET_GAMEDATA_VERSION_UNSUPPORTED",
        PresentationWarningKind.UnverifiedMechanic,
        true)]
    [InlineData(
        "CandidateGeneration",
        "OptionRejected",
        PresentationWarningKind.CandidateSearch,
        false)]
    [InlineData(
        "CandidateGeneration",
        "NoEligibleOptions",
        PresentationWarningKind.CandidateSearch,
        true)]
    public void Warning_codes_have_distinct_kind_criticality_and_effect(
        string source,
        string code,
        PresentationWarningKind expectedKind,
        bool expectedCritical)
    {
        var result = RecommendationWarningPresentation.Classify(
            source,
            code);

        Assert.Equal(expectedKind, result.Kind);
        Assert.Equal(expectedCritical, result.IsCritical);
        Assert.False(
            string.IsNullOrWhiteSpace(result.EffectOnRecommendation));
    }

    [Fact]
    public void Unavailable_values_are_never_described_as_estimated()
    {
        var result = RecommendationWarningPresentation.Classify(
            "Snapshot",
            "SKILL_COST_UNAVAILABLE");

        Assert.Contains("not replaced", result.EffectOnRecommendation);
        Assert.Contains("estimate", result.EffectOnRecommendation);
    }
}
