using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CombatRecommendations;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class CombatRecommendationsControllerTests
{
    private const string ConfiguredSavePath =
        @"C:\Taiwu\SaveGames\world_1\local.sav";

    [Fact]
    public async Task Post_returns_typed_styles_from_one_snapshot_read()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = GoldenSnapshot();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var controller = Controller(reader);
        var cancellationToken = TestContext.Current.CancellationToken;

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317,
                Objective = RecommendationPolicy.Aggressive
            },
            cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var response = Assert.IsType<CombatRecommendationResponse>(
            ok.Value);
        Assert.Equal(
            RecommendationPolicy.Aggressive,
            response.RequestedStyle);
        Assert.Equal(3, response.Styles.Count);
        Assert.All(
            response.Styles,
            style => Assert.Equal(
                response.SnapshotReference,
                style.SnapshotReference));
        Assert.All(
            response.Threats,
            threat => Assert.StartsWith("threat:", threat.Reference));
        var safe = response.Styles.Single(
            style => style.Style == RecommendationPolicy.Safe);
        Assert.True(safe.HasRecommendation);
        Assert.All(
            safe.Skills.SelectMany(skill => skill.Reasons),
            reason => Assert.False(
                string.IsNullOrWhiteSpace(reason.Reference)));
        Assert.All(
            safe.ManualChanges,
            change =>
            {
                Assert.False(string.IsNullOrWhiteSpace(change.Reference));
                Assert.False(
                    string.IsNullOrWhiteSpace(change.Reason.Reference));
            });
        Assert.All(
            safe.OpeningActions.Concat(safe.SwitchingConditions),
            step =>
            {
                Assert.False(string.IsNullOrWhiteSpace(step.Reference));
                Assert.False(
                    string.IsNullOrWhiteSpace(step.Reason.Reference));
            });
        await reader.Received(1).ReadAsync(
            Arg.Is<CombatSnapshotReadRequest>(request =>
                request != null
                && request.SaveFilePath == ConfiguredSavePath
                && request.TargetCharacterId == 16317),
            cancellationToken);
    }

    [Fact]
    public async Task Optional_screen_observation_is_forwarded_as_analysis_input()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(GoldenSnapshot());
        var controller = Controller(reader);
        var observedAt = DateTimeOffset.Parse("2026-07-30T13:00:00Z");
        var cancellationToken = TestContext.Current.CancellationToken;

        await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317,
                CurrentScreenObservation =
                    new CurrentScreenLoadoutRequest
                    {
                        ObservedAt = observedAt,
                        EvidenceReference = "ui:current-screen",
                        EquippedSkills = new CombatLoadoutRequest
                        {
                            AttackSkillIds = [604]
                        },
                        GenericSlotAllocation =
                            new GenericSlotAllocationRequest()
                    }
            },
            cancellationToken);

        await reader.Received(1).ReadAsync(
            Arg.Is<CombatSnapshotReadRequest>(request =>
                request != null
                && request.CurrentLoadoutObservation != null
                && request.CurrentLoadoutObservation.ObservedAtUtc
                    == observedAt
                && request.CurrentLoadoutObservation.EvidenceReference
                    == "ui:current-screen"
                && request.CurrentLoadoutObservation.EquippedSkills
                    .AttackSkillIds.Contains(604)),
            cancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Invalid_target_returns_problem_without_read(
        int? targetCharacterId)
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var controller = Controller(reader);

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = targetCharacterId
            },
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task Invalid_objective_returns_problem_without_read()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var controller = Controller(reader);

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317,
                Objective = (RecommendationPolicy)999
            },
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task Expected_reader_error_returns_problem()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<CombatSnapshot>(
                    new InvalidDataException("Invalid save.")));
        var controller = Controller(reader);

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317
            },
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(
            "Invalid save.",
            Assert.IsType<ProblemDetails>(problem.Value).Detail);
    }

    [Fact]
    public void Contract_is_one_information_only_post_action()
    {
        var controller = typeof(CombatRecommendationsController);
        var route = controller.GetCustomAttribute<RouteAttribute>();
        Assert.Equal(
            "api/combat-recommendations",
            route?.Template);

        var actions = controller.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly)
            .Where(method =>
                method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();
        var action = Assert.Single(actions);
        Assert.Equal("Recommend", action.Name);
        Assert.NotNull(action.GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal(
            typeof(Task<ActionResult<CombatRecommendationResponse>>),
            action.ReturnType);
        Assert.DoesNotContain(
            action.GetParameters(),
            parameter => parameter.ParameterType == typeof(string));
    }

    private static CombatRecommendationsController Controller(
        ICombatSnapshotReader reader)
    {
        return new CombatRecommendationsController(
            new RecommendCombatLoadout(reader),
            Options.Create(
                new SaveGameOptions
                {
                    DefaultSaveFilePath = ConfiguredSavePath
                }));
    }

    private static CombatSnapshot GoldenSnapshot()
    {
        var playerSkill = Skill(
            604,
            PracticeDirection.Neutral,
            directEffectId: 338,
            reverseEffectId: 1064);
        var targetSkill = Skill(
            719,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                ConfiguredSavePath,
                new string('A', 64),
                DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-07-30T11:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedCombatEffectCatalogs.GoldenGameDataVersion)),
            new PlayerCombatSnapshot(
                characterId: 1,
                SnapshotValue<string>.Available("Taiwu"),
                [playerSkill],
                new CombatLoadoutSnapshot([], [], [], [], []),
                equipment: [],
                new SlotBudgetSet(
                [
                    new SlotBudget(SkillCategory.Neigong, 0, 6),
                    new SlotBudget(SkillCategory.Attack, 0, 2),
                    new SlotBudget(SkillCategory.Agility, 0, 2),
                    new SlotBudget(SkillCategory.Defense, 0, 2),
                    new SlotBudget(SkillCategory.Assistance, 0, 2)
                ]),
                new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
            new TargetCombatSnapshot(
                characterId: 16317,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(52),
                features: [],
                [targetSkill],
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    new CombatLoadoutSnapshot(
                        [],
                        [targetSkill.SkillId],
                        [],
                        [],
                        [])),
                equipment: []),
            [
                new SnapshotWarning(
                    "SOURCE_WARNING",
                    "Preserved source warning.")
            ]);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        PracticeDirection direction,
        int directEffectId,
        int reverseEffectId)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(directEffectId),
            SnapshotValue<int>.Available(reverseEffectId));
    }
}
