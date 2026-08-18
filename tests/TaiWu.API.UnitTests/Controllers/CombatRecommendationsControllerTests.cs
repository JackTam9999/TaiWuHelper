using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Reflection;
using System.Text.Json;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;
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
                Objective = RecommendationPolicy.Aggressive,
                Language = TaiwuLanguage.Chinese
            },
            cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var response = Assert.IsType<CombatRecommendationResponse>(
            ok.Value);
        Assert.Null(response.TargetObservation);
        var targetStrategy = Assert.IsType<TargetStrategyResponse>(
            response.TargetStrategy);
        Assert.Contains(
            targetStrategy.Archetypes,
            archetype => archetype.Code
                == "MIND_RESONANCE_BASELINE"
                && archetype.Title == "心神共鳴連鎖");
        var comparison = Assert.IsType<LoadoutComparisonResponse>(
            response.Comparison);
        Assert.Equal(response.SnapshotReference, comparison.SnapshotReference);
        Assert.Equal(4, comparison.Columns.Count);
        Assert.Equal(
            Enum.GetValues<LoadoutComparisonColumnKind>(),
            comparison.Columns.Select(column => column.Kind));
        Assert.All(
            comparison.Columns.Skip(1),
            column => Assert.Equal(
                LoadoutComparisonColumnStatus.Available,
                column.Status));
        var safeComparison = comparison.Columns.Single(column =>
            column.Kind == LoadoutComparisonColumnKind.Safe);
        Assert.Equal(5, safeComparison.Loadout!.Categories.Count);
        Assert.Contains(
            "not win odds",
            safeComparison.TacticalSummary!.ScoreScopeNotice);
        Assert.All(
            safeComparison.TacticalSummary.CoveredThreats,
            threat => Assert.False(string.IsNullOrWhiteSpace(threat.Title)));
        Assert.All(
            safeComparison.Loadout.Categories
                .SelectMany(category => category.Skills),
            skill => Assert.True(skill.Name.IsAvailable));
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
        Assert.All(
            response.Warnings,
            warning => Assert.True(warning.Occurrences >= 1));
        Assert.Equal(
            response.Warnings.Count,
            response.Warnings
                .Select(warning => (
                    warning.Source,
                    warning.Code,
                    warning.Message))
                .Distinct()
                .Count());
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
                && request.TargetCharacterId == 16317
                && request.Language == TaiwuLanguage.Chinese),
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
                            new GenericSlotAllocationRequest(),
                        DisplayedSlotBudgets =
                            new DisplayedSlotBudgetSetRequest
                            {
                                Neigong = Budget(0, 6),
                                Attack = Budget(1, 10),
                                Agility = Budget(0, 8),
                                Defense = Budget(0, 8),
                                Assistance = Budget(0, 2)
                            }
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
                    .AttackSkillIds.Contains(604)
                && request.CurrentLoadoutObservation.DisplayedSlotBudgets!
                    [SkillCategory.Attack].Capacity == 10),
            cancellationToken);
    }

    [Fact]
    public async Task Infeasible_policies_are_diagnostics_not_empty_loadouts()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(EmptySnapshot());
        var controller = Controller(reader);

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317
            },
            TestContext.Current.CancellationToken);

        var comparison = Response(action).Comparison!;
        Assert.All(
            comparison.Columns.Skip(1),
            column =>
            {
                Assert.Equal(
                    LoadoutComparisonColumnStatus.Infeasible,
                    column.Status);
                Assert.Null(column.Loadout);
                Assert.NotNull(column.Diagnostic);
                Assert.False(string.IsNullOrWhiteSpace(
                    column.Diagnostic!.Summary));
            });
    }

    [Fact]
    public async Task Unavailable_cost_and_capacity_keep_public_reasons()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(UnavailableCurrentSnapshot());
        var controller = Controller(reader);

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317
            },
            TestContext.Current.CancellationToken);

        var current = Response(action).Comparison!.Columns.Single(column =>
            column.Kind == LoadoutComparisonColumnKind.Current);
        var attack = current.Loadout!.Categories.Single(category =>
            category.Category == SkillCategory.Attack);
        var skill = Assert.Single(attack.Skills);
        Assert.False(skill.EffectiveCost.IsAvailable);
        Assert.Null(skill.EffectiveCost.Value);
        Assert.Contains(
            "GridCost",
            skill.EffectiveCost.UnavailableReason);
        Assert.False(attack.Capacity.Used.IsAvailable);
        Assert.Null(attack.Capacity.Used.Value);
        Assert.Equal(
            "Used slots were unavailable.",
            attack.Capacity.Used.UnavailableReason);
        Assert.False(attack.Capacity.Remaining.IsAvailable);
    }

    [Fact]
    public async Task Observed_baseline_fields_are_distinct_from_save_fields()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        var snapshot = GoldenSnapshot();
        var observedAt = DateTimeOffset.Parse("2026-08-08T14:00:00Z");
        var observed = new CombatSnapshot(
            snapshot.Metadata,
            snapshot.Player,
            snapshot.Target,
            snapshot.Warnings,
            [
                new SnapshotFieldSource(
                    CombatSnapshotObservationMerger
                        .PlayerEquippedSkillsField,
                    SnapshotDataSource.CurrentScreenObservation,
                    observedAt,
                    "observation:current"),
                new SnapshotFieldSource(
                    CombatSnapshotObservationMerger
                        .PlayerGenericSlotAllocationField,
                    SnapshotDataSource.CurrentScreenObservation,
                    observedAt,
                    "observation:current")
            ]);
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(observed);
        var controller = Controller(reader);

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317
            },
            TestContext.Current.CancellationToken);

        var provenance = Response(action).Comparison!.BaselineProvenance;
        Assert.Equal(
            SnapshotDataSource.CurrentScreenObservation,
            provenance.Single(value => value.Field
                == LoadoutComparisonBaselineField.EquippedSkills).Source);
        Assert.Equal(
            SnapshotDataSource.CurrentScreenObservation,
            provenance.Single(value => value.Field
                == LoadoutComparisonBaselineField.GenericSlotAllocation)
                .Source);
        Assert.Equal(
            SnapshotDataSource.Save,
            provenance.Single(value => value.Field
                == LoadoutComparisonBaselineField.SlotBudgets).Source);
    }

    [Fact]
    public async Task Mixed_and_missing_styles_keep_typed_policy_status()
    {
        var feasible = await Recommendation(GoldenSnapshot());
        var infeasible = await Recommendation(EmptySnapshot());
        var mixed = RecommendationWithStyles(
            feasible,
            [
                feasible.Styles.Single(style =>
                    style.Policy == RecommendationPolicy.Safe),
                infeasible.Styles.Single(style =>
                    style.Policy == RecommendationPolicy.Balanced),
                feasible.Styles.Single(style =>
                    style.Policy == RecommendationPolicy.Aggressive)
            ]);
        var missing = RecommendationWithStyles(
            feasible,
            [
                feasible.Styles.Single(style =>
                    style.Policy == RecommendationPolicy.Safe),
                infeasible.Styles.Single(style =>
                    style.Policy == RecommendationPolicy.Balanced)
            ]);

        var mixedResponse = await Response(mixed);
        var missingResponse = await Response(missing);

        Assert.Equal(
            [
                LoadoutComparisonColumnStatus.Available,
                LoadoutComparisonColumnStatus.Infeasible,
                LoadoutComparisonColumnStatus.Available
            ],
            mixedResponse.Comparison!.Columns.Skip(1)
                .Select(column => column.Status));
        var unavailable = missingResponse.Comparison!.Columns.Single(column =>
            column.Kind == LoadoutComparisonColumnKind.Aggressive);
        Assert.Equal(
            LoadoutComparisonColumnStatus.Unavailable,
            unavailable.Status);
        Assert.Null(unavailable.Loadout);
        Assert.Equal(
            "STYLE_RESULT_UNAVAILABLE",
            unavailable.Diagnostic!.Code);
    }

    [Fact]
    public async Task Comparison_serialization_is_deterministic_and_path_safe()
    {
        var recommendation = await Recommendation(GoldenSnapshot());

        var first = CombatRecommendationResponseMapper.Map(recommendation);
        var second = CombatRecommendationResponseMapper.Map(recommendation);
        var firstJson = JsonSerializer.Serialize(first.Comparison);
        var secondJson = JsonSerializer.Serialize(second.Comparison);

        Assert.Equal(firstJson, secondJson);
        Assert.DoesNotContain(ConfiguredSavePath, firstJson);
        Assert.DoesNotContain(@"C:\Taiwu", firstJson);
        Assert.DoesNotContain("exception", firstJson, StringComparison.OrdinalIgnoreCase);
    }

    private static DisplayedSlotBudgetRequest Budget(
        int used,
        int capacity)
    {
        return new DisplayedSlotBudgetRequest
        {
            Used = used,
            Capacity = capacity
        };
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
    public async Task Missing_target_returns_not_found_problem()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CombatSnapshot>(
                new CombatSnapshotTargetNotFoundException(16317)));
        var controller = Controller(reader);

        var action = await controller.Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317
            },
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal(
            "urn:taiwu-helper:combat-recommendation:target-not-found",
            details.Type);
        Assert.DoesNotContain("16317", details.Detail);
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

    private static CombatRecommendationsController Controller(
        IRecommendCombatLoadout recommender)
    {
        return new CombatRecommendationsController(
            recommender,
            Options.Create(
                new SaveGameOptions
                {
                    DefaultSaveFilePath = ConfiguredSavePath
                }));
    }

    private static CombatRecommendationResponse Response(
        ActionResult<CombatRecommendationResponse> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<CombatRecommendationResponse>(ok.Value);
    }

    private static async Task<CombatRecommendationResponse> Response(
        CombatLoadoutRecommendation recommendation)
    {
        var recommender = Substitute.For<IRecommendCombatLoadout>();
        recommender.ExecuteAsync(
                Arg.Any<RecommendCombatLoadoutRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(recommendation);
        var action = await Controller(recommender).Recommend(
            new CombatRecommendationApiRequest
            {
                TargetCharacterId = 16317,
                Objective = RecommendationPolicy.Safe
            },
            TestContext.Current.CancellationToken);
        return Response(action);
    }

    private static async Task<CombatLoadoutRecommendation> Recommendation(
        CombatSnapshot snapshot)
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return await new RecommendCombatLoadout(reader).ExecuteAsync(
            new RecommendCombatLoadoutRequest(
                ConfiguredSavePath,
                snapshot.Target.CharacterId,
                RecommendationPolicy.Safe),
            TestContext.Current.CancellationToken);
    }

    private static CombatLoadoutRecommendation RecommendationWithStyles(
        CombatLoadoutRecommendation source,
        CombatRecommendationStyleResult[] styles)
    {
        var constructor = typeof(CombatLoadoutRecommendation)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(value => value.GetParameters().Length == 7);
        return (CombatLoadoutRecommendation)constructor.Invoke(
        [
            source.Snapshot,
            source.ThreatAnalysis,
            source.Generation,
            RecommendationPolicy.Safe,
            styles,
            null,
            null
        ]);
    }

    private static CombatSnapshot GoldenSnapshot()
    {
        var playerSkill = Skill(
            604,
            PracticeDirection.Reverse,
            directEffectId: 338,
            reverseEffectId: 1064);
        var targetSkill = Skill(
            719,
            PracticeDirection.Direct,
            directEffectId: 669,
            reverseEffectId: 1669);
        var resetSkill = new CombatSkillSnapshot(
            287,
            SnapshotValue<string>.Available("Skill 287"),
            SkillCategory.Assistance,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(185),
            SnapshotValue<int>.Available(911));
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
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
                [targetSkill, resetSkill],
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    new CombatLoadoutSnapshot(
                        [],
                        [targetSkill.SkillId],
                        [],
                        [],
                        [resetSkill.SkillId])),
                equipment: []),
            [
                new SnapshotWarning(
                    "SOURCE_WARNING",
                    "Preserved source warning.")
            ]);
    }

    private static CombatSnapshot EmptySnapshot()
    {
        return Snapshot(
            playerSkills: [],
            new CombatLoadoutSnapshot([], [], [], [], []),
            new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 0, 6),
                new SlotBudget(SkillCategory.Attack, 0, 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 2),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]));
    }

    private static CombatSnapshot UnavailableCurrentSnapshot()
    {
        var skill = new CombatSkillSnapshot(
            900,
            SnapshotValue<string>.Available("Current skill"),
            SkillCategory.Attack,
            SnapshotValue<int>.Unavailable("Grid cost was not captured."),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(1900),
            SnapshotValue<int>.Available(2900));
        return Snapshot(
            [skill],
            new CombatLoadoutSnapshot([], [skill.SkillId], [], [], []),
            new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 0, 6),
                new SlotBudget(
                    SkillCategory.Attack,
                    SnapshotValue<int>.Unavailable(
                        "Used slots were unavailable."),
                    capacity: 2),
                new SlotBudget(SkillCategory.Agility, 0, 2),
                new SlotBudget(SkillCategory.Defense, 0, 2),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]));
    }

    private static CombatSnapshot Snapshot(
        CombatSkillSnapshot[] playerSkills,
        CombatLoadoutSnapshot playerLoadout,
        SlotBudgetSet slotBudgets)
    {
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('B', 64),
                DateTimeOffset.Parse("2026-08-08T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-08T11:00:00Z")),
                SnapshotValue<string>.Available(
                    VerifiedCombatEffectCatalogs.GoldenGameDataVersion)),
            new PlayerCombatSnapshot(
                characterId: 1,
                SnapshotValue<string>.Available("Taiwu"),
                playerSkills,
                playerLoadout,
                equipment: [],
                slotBudgets,
                new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
            new TargetCombatSnapshot(
                characterId: 16317,
                SnapshotValue<string>.Available("Target"),
                SnapshotValue<int>.Available(52),
                features: [],
                learnedSkills: [],
                SnapshotValue<CombatLoadoutSnapshot>.Available(
                    new CombatLoadoutSnapshot([], [], [], [], [])),
                equipment: []),
            warnings: []);
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
