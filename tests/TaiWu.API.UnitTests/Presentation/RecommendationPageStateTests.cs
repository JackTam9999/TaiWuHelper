using TaiWu.Application.Targets;
using TaiWu.Domain.CombatRecommendations;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class RecommendationPageStateTests
{
    [Fact]
    public void Initial_state_explains_how_to_begin()
    {
        var state = RecommendationPageState.Initial();

        Assert.Equal(RecommendationPageStatus.Initial, state.Status);
        Assert.Contains("Search", state.Message);
        Assert.False(state.CanRetryRead);
        Assert.False(state.IsProblem);
    }

    [Fact]
    public void Loading_state_is_explicit_and_read_only()
    {
        var state = RecommendationPageState.Loading("Reading target");

        Assert.Equal(RecommendationPageStatus.Loading, state.Status);
        Assert.True(state.IsLoading);
        Assert.Contains("No game data is changed", state.Message);
    }

    [Theory]
    [InlineData(
        TargetLookupStatus.NotFound,
        RecommendationPageStatus.Empty)]
    [InlineData(
        TargetLookupStatus.Ambiguous,
        RecommendationPageStatus.AmbiguousTarget)]
    [InlineData(
        TargetLookupStatus.Found,
        RecommendationPageStatus.TargetReady)]
    public void Target_lookup_status_has_a_distinct_page_state(
        TargetLookupStatus lookupStatus,
        RecommendationPageStatus expectedStatus)
    {
        var state = RecommendationPageState.ForTargetLookup(
            lookupStatus,
            matchCount: 2);

        Assert.Equal(expectedStatus, state.Status);
        Assert.False(string.IsNullOrWhiteSpace(state.Message));
    }

    [Fact]
    public void Recommendation_without_warning_is_success()
    {
        var state = RecommendationPageState.ForRecommendation(
            Recommendation());

        Assert.Equal(RecommendationPageStatus.Success, state.Status);
        Assert.True(state.CanRetryRead);
    }

    [Fact]
    public void Recommendation_with_warning_is_success_with_warning()
    {
        var state = RecommendationPageState.ForRecommendation(
            Recommendation(Warning("SKILL_COST_UNAVAILABLE")));

        Assert.Equal(
            RecommendationPageStatus.SuccessWithWarning,
            state.Status);
        Assert.Contains("manual review", state.Message);
    }

    [Fact]
    public void Unsupported_version_never_offers_an_estimate()
    {
        var state = RecommendationPageState.ForRecommendation(
            Recommendation(
                Warning("TARGET_GAMEDATA_VERSION_UNSUPPORTED")));

        Assert.Equal(
            RecommendationPageStatus.UnsupportedVersion,
            state.Status);
        Assert.True(state.IsProblem);
        Assert.Contains("does not estimate", state.Message);
        Assert.DoesNotContain(
            "repair",
            state.Recovery,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Invalid_configuration_requires_a_local_setting_and_restart()
    {
        var state = RecommendationPageState.InvalidConfiguration();

        Assert.Equal(
            RecommendationPageStatus.InvalidConfiguration,
            state.Status);
        Assert.Contains("SaveGames:DefaultSaveFilePath", state.Recovery);
        Assert.False(state.CanRetryRead);
    }

    [Fact]
    public void Failure_recovery_only_retries_the_read()
    {
        var state = RecommendationPageState.Failure("Read failed.");

        Assert.Equal(RecommendationPageStatus.Failure, state.Status);
        Assert.True(state.IsProblem);
        Assert.True(state.CanRetryRead);
        Assert.Contains("Retry the read", state.Recovery);
        Assert.Contains("did not change", state.Recovery);
    }

    private static CombatRecommendationViewModel Recommendation(
        params RecommendationWarningViewModel[] warnings) =>
        new(
            "snapshot:test",
            DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
            DateTimeOffset.Parse("2026-07-30T11:59:00Z"),
            "game-version",
            RecommendationPolicy.Balanced,
            "style:Balanced",
            "Information only.",
            [],
            [],
            warnings);

    private static RecommendationWarningViewModel Warning(string code) =>
        new(
            $"warning:{code}",
            "Test",
            code,
            PresentationWarningKind.General,
            IsCritical: false,
            Occurrences: 1,
            "Test warning.",
            "Review manually.",
            []);
}
