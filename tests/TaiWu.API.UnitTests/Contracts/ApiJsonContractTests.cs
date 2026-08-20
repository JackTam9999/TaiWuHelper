using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TaiWu.Application.Localization;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWu.Domain.TacticalCombat;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CompanionCandidates;
using TaiWuAPI.Contracts.CombatRecommendations;
using TaiWuAPI.Contracts.VillageWorkforce;
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
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<VillageWorkforceApiStatus>(
                "1",
                Options()));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TacticalRuleEvidenceDisposition>(
                "1",
                Options()));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<TacticalCombatRecommendationStatus>(
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
    [InlineData(VillageWorkforceApiStatus.Partial, "Partial")]
    [InlineData(TacticalRuleEvidenceScope.ExactTarget, "ExactTarget")]
    [InlineData(TacticalEvidenceSourceKind.ConfirmedObservation,
        "ConfirmedObservation")]
    [InlineData(TacticalRuleEvidenceDisposition.Conflicting, "Conflicting")]
    [InlineData(TacticalFinishDisposition.FallbackOnly, "FallbackOnly")]
    [InlineData(VillageWorkforceApiEvaluationState.Tied, "Tied")]
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
                Language = TaiwuLanguage.English,
                TacticalPlanning = new TacticalPlanningApiRequest
                {
                    Observations =
                    [
                        new TacticalRuleObservationApiRequest
                        {
                            Identity = "TARGET_MIND_CHAIN_APPLICABLE",
                            Scope = TacticalRuleEvidenceScope.ExactTarget,
                            Source = TacticalEvidenceSourceKind
                                .ConfirmedObservation,
                            Disposition = TacticalRuleEvidenceDisposition
                                .Confirmed,
                            EvidenceIdentity = "PUBLIC_OBSERVATION",
                            ScopeIdentity = "EXACT_TARGET"
                        }
                    ],
                    Bounds = new TacticalSearchBoundsApiRequest
                    {
                        MaximumOptions = 8,
                        MaximumExploredCombinations = 100,
                        MaximumElapsedMilliseconds = 2_000,
                        MaximumResults = 10
                    }
                }
            },
            Options());
        var workforce = JsonSerializer.SerializeToDocument(
            new VillageWorkforceApiQuery
            {
                AreaId = 1,
                BlockId = 2,
                BuildingBlockIndex = 7,
                ManagerSlotIndex = 0,
                Objective = VillageWorkforceApiTokens.Objective,
                ObjectiveVersion = VillageWorkforceApiTokens.ObjectiveVersion,
                Filter = VillageWorkforceApiTokens.FilterComparable,
                Language = VillageWorkforceApiTokens.TraditionalChinese
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
        var tactical = recommendation.RootElement.GetProperty(
            "tacticalPlanning");
        Assert.Equal(
            "ExactTarget",
            tactical.GetProperty("observations")[0]
                .GetProperty("scope").GetString());
        Assert.Equal(
            100,
            tactical.GetProperty("bounds")
                .GetProperty("maximumExploredCombinations").GetInt32());
        Assert.Equal(
            VillageWorkforceApiTokens.Objective,
            workforce.RootElement.GetProperty("objective").GetString());
        Assert.Equal(
            VillageWorkforceApiTokens.FilterComparable,
            workforce.RootElement.GetProperty("filter").GetString());
        Assert.Equal(
            VillageWorkforceApiTokens.TraditionalChinese,
            workforce.RootElement.GetProperty("language").GetString());
    }

    private static JsonSerializerOptions Options()
    {
        var mvc = new JsonOptions();
        ApiJsonOptions.Configure(mvc);
        return mvc.JsonSerializerOptions;
    }
}
