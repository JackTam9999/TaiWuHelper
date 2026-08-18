using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CompanionCandidates;
using TaiWuAPI.Contracts.CombatRecommendations;
using Xunit;

namespace TaiWu.API.UnitTests.Contracts;

public sealed class ApiJsonContractTests
{
    [Fact]
    public void Numeric_enum_tokens_are_rejected()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<RecommendationPolicy>(
                "1",
                Options()));
    }

    [Theory]
    [InlineData(RecommendationPolicy.Balanced, "Balanced")]
    [InlineData(TaiwuLanguage.Chinese, "Chinese")]
    [InlineData(TargetObservationContext.Sparring, "Sparring")]
    [InlineData(TargetLoadoutCoverageKind.PartialLoadout, "PartialLoadout")]
    [InlineData(SkillCategory.Attack, "Attack")]
    [InlineData(PracticeDirection.Reverse, "Reverse")]
    [InlineData(CandidateDisciplineDomain.Capability, "Capability")]
    [InlineData(CompanionRoleShortlistFilter.NeedsReview, "NeedsReview")]
    public void Request_enum_tokens_are_pinned(object value, string token)
    {
        Assert.Equal(
            $"\"{token}\"",
            JsonSerializer.Serialize(value, value.GetType(), Options()));
    }

    [Fact]
    public void Loopback_request_property_names_and_tokens_are_pinned()
    {
        var companion = JsonSerializer.SerializeToDocument(
            new CompanionFinderApiRequest
            {
                RoleIdentity = "COMPREHENSIVE_BASE_CAPABILITY",
                RoleVersion = "1",
                DisciplineDomain = CandidateDisciplineDomain.Capability,
                DisciplineType = 0,
                Filter = CompanionRoleShortlistFilter.Ranked,
                Language = TaiwuLanguage.Chinese
            },
            Options());
        var recommendation = JsonSerializer.SerializeToDocument(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 42,
                Objective = RecommendationPolicy.Balanced,
                Language = TaiwuLanguage.English
            },
            Options());

        Assert.Equal(
            "Capability",
            companion.RootElement.GetProperty("disciplineDomain").GetString());
        Assert.Equal(
            "Ranked",
            companion.RootElement.GetProperty("filter").GetString());
        Assert.Equal(
            "Chinese",
            companion.RootElement.GetProperty("language").GetString());
        Assert.Equal(
            "Balanced",
            recommendation.RootElement.GetProperty("objective").GetString());
        Assert.Equal(
            "English",
            recommendation.RootElement.GetProperty("language").GetString());
    }

    private static JsonSerializerOptions Options()
    {
        var mvc = new JsonOptions();
        ApiJsonOptions.Configure(mvc);
        return mvc.JsonSerializerOptions;
    }
}
