using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TargetObservations;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CombatRecommendations;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class CombatRecommendationTargetObservationControllerTests
{
    private const string ConfiguredSavePath =
        @"C:\Taiwu\SaveGames\world_1\local.sav";

    private static readonly DateTimeOffset SaveTime = DateTimeOffset.Parse(
        "2026-08-07T20:00:00Z");

    [Fact]
    public async Task Valid_request_returns_sanitized_typed_observation_result()
    {
        var controller = Controller(Snapshot());

        var action = await controller.Recommend(
            Request(
                TargetLoadoutCoverageKind.PartialLoadout,
                SaveTime.AddMinutes(1),
                [SkillRequest(confirmedSkillId: 719)]),
            TestContext.Current.CancellationToken);

        var response = Response(action);
        var observation = Assert.IsType<TargetObservationResponse>(
            response.TargetObservation);
        Assert.Equal(16317, observation.TargetCharacterId);
        Assert.Equal(
            TargetLoadoutMergeStatus.Applied,
            observation.MergeStatus);
        Assert.Equal(
            SnapshotEvidenceStatus.Available,
            observation.LoadoutEvidenceStatus);
        Assert.Equal(
            TargetLoadoutCoverageKind.PartialLoadout,
            observation.Coverage);
        var skill = Assert.Single(observation.ResolvedSkills);
        Assert.Equal(719, skill.SkillId);
        Assert.Equal("Target Art", skill.Name);
        Assert.Equal(
            TargetSkillSnapshotPresence.Present,
            skill.SnapshotPresence);
        Assert.True(observation.Impact.Applied);
        var recommendationImpact = Assert.IsType<
            TargetObservationRecommendationImpactResponse>(
            observation.RecommendationImpact);
        Assert.True(recommendationImpact.PartialCoverageLeavesUnknown);
        Assert.Empty(recommendationImpact.Threats);
        Assert.Empty(recommendationImpact.FeasibilityChanges);
        Assert.Empty(recommendationImpact.ScoringChanges);
        var unsupported = Assert.Single(
            recommendationImpact.UnsupportedEvidence);
        Assert.Equal(
            "UNRECOGNIZED_TARGET_EFFECT",
            unsupported.Code);
        Assert.False(unsupported.WasPresentBefore);
        Assert.Contains(
            "not a win probability",
            recommendationImpact.ConfidenceNotice);
        Assert.All(
            observation.Sources,
            source =>
            {
                Assert.DoesNotContain("\\", source.EvidenceReference);
                Assert.DoesNotContain("/", source.EvidenceReference);
            });
    }

    [Theory]
    [InlineData(TargetObservationContext.Hostile)]
    [InlineData(TargetObservationContext.Story)]
    public async Task Battle_visible_request_retains_context_and_power_as_partial_evidence(
        TargetObservationContext context)
    {
        var action = await Controller(Snapshot()).Recommend(
            Request(
                TargetLoadoutCoverageKind.PartialLoadout,
                SaveTime.AddMinutes(1),
                [SkillRequest(
                    confirmedSkillId: 719,
                    visiblePowerPercent: 146,
                    slotIndex: null)],
                context: context,
                evidenceReference: "E3-012-CAP-001"),
            TestContext.Current.CancellationToken);

        var observation = Assert.IsType<TargetObservationResponse>(
            Response(action).TargetObservation);
        Assert.Equal(context, observation.Context);
        var skill = Assert.Single(observation.ResolvedSkills);
        Assert.Equal(146, skill.VisiblePowerPercent);
        Assert.Null(skill.SlotIndex);
        Assert.Empty(observation.Impact.AddedEquippedSkillIds);
        Assert.Contains(
            observation.Sources,
            source => source.Field
                == TargetLoadoutObservationMerger
                    .TargetVisibleActiveEffectsField);
        Assert.DoesNotContain(
            observation.Sources,
            source => source.Field
                    == TargetLoadoutObservationMerger.TargetEquippedSkillsField
                && source.Source
                    == SnapshotDataSource.CurrentScreenObservation);
    }

    [Fact]
    public async Task Battle_visible_request_rejects_complete_loadout_claim()
    {
        var action = await Controller(Snapshot()).Recommend(
            Request(
                TargetLoadoutCoverageKind.CompleteCurrentLoadout,
                SaveTime.AddMinutes(1),
                [SkillRequest(confirmedSkillId: 719)],
                context: TargetObservationContext.Story,
                evidenceReference: "E3-012-CAP-001"),
            TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("InvalidObservation", problem.Extensions["code"]);
    }

    [Fact]
    public async Task Ambiguous_selection_returns_stable_problem_candidates()
    {
        var controller = Controller(
            Snapshot(),
            Definition(719, "Target Art"),
            Definition(720, "Target Art Advanced"));

        var action = await controller.Recommend(
            Request(
                TargetLoadoutCoverageKind.PartialLoadout,
                SaveTime.AddMinutes(1),
                [SkillRequest(confirmedSkillId: null)]),
            TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Ambiguous", problem.Extensions["code"]);
        Assert.Equal(0, problem.Extensions["selectionIndex"]);
        var candidates = Assert.IsType<
            TargetObservationProblemCandidateResponse[]>(
            problem.Extensions["candidates"]);
        Assert.Equal([719, 720], candidates.Select(value => value.SkillId));
        var json = JsonSerializer.Serialize(problem);
        Assert.DoesNotContain(ConfiguredSavePath, json);
        Assert.DoesNotContain("local.sav", json);
    }

    [Fact]
    public async Task Invalid_local_evidence_returns_stable_problem()
    {
        var controller = Controller(Snapshot());
        var request = Request(
            TargetLoadoutCoverageKind.PartialLoadout,
            SaveTime.AddMinutes(1),
            [SkillRequest(confirmedSkillId: 719)],
            evidenceReference: @"C:\captures\target.png");

        var action = await controller.Recommend(
            request,
            TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("InvalidObservation", problem.Extensions["code"]);
        Assert.DoesNotContain("captures", JsonSerializer.Serialize(problem));
    }

    [Fact]
    public async Task Observation_snapshot_failure_does_not_expose_local_detail()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CombatSnapshot>(
                new InvalidDataException(ConfiguredSavePath)));
        var controller = Controller(reader, [Definition(719, "Target Art")]);

        var action = await controller.Recommend(
            Request(
                TargetLoadoutCoverageKind.PartialLoadout,
                SaveTime.AddMinutes(1),
                [SkillRequest(confirmedSkillId: 719)]),
            TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("SnapshotUnavailable", problem.Extensions["code"]);
        Assert.DoesNotContain(
            ConfiguredSavePath,
            JsonSerializer.Serialize(problem));
    }

    [Fact]
    public async Task Observation_missing_target_returns_not_found_problem()
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CombatSnapshot>(
                new CombatSnapshotTargetNotFoundException(16317)));
        var controller = Controller(reader, [Definition(719, "Target Art")]);

        var action = await controller.Recommend(
            Request(
                TargetLoadoutCoverageKind.PartialLoadout,
                SaveTime.AddMinutes(1),
                [SkillRequest(confirmedSkillId: 719)]),
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal(
            "urn:taiwu-helper:combat-recommendation:target-not-found",
            Assert.IsType<ProblemDetails>(problem.Value).Type);
    }

    [Fact]
    public async Task Evidence_states_are_successful_and_json_safe()
    {
        var stale = await Recommend(
            Snapshot(),
            Request(
                TargetLoadoutCoverageKind.PartialLoadout,
                SaveTime,
                [SkillRequest(confirmedSkillId: 719)]));
        var unsupported = await Recommend(
            Snapshot(gameDataVersion: "1.0.0+different"),
            Request(
                TargetLoadoutCoverageKind.PartialLoadout,
                SaveTime.AddMinutes(1),
                [SkillRequest(confirmedSkillId: 719)]));
        var conflicting = await Recommend(
            Snapshot(),
            Request(
                TargetLoadoutCoverageKind.CompleteCurrentLoadout,
                SaveTime.AddMinutes(1),
                selectedSkills: []));

        Assert.Equal(
            TargetLoadoutMergeStatus.Stale,
            stale.TargetObservation!.MergeStatus);
        Assert.Equal(
            SnapshotEvidenceStatus.Stale,
            stale.TargetObservation.LoadoutEvidenceStatus);
        Assert.Equal(
            TargetLoadoutMergeStatus.UnsupportedVersion,
            unsupported.TargetObservation!.MergeStatus);
        Assert.Equal(
            SnapshotEvidenceStatus.Unavailable,
            unsupported.TargetObservation.LoadoutEvidenceStatus);
        Assert.Equal(
            TargetLoadoutMergeStatus.Applied,
            conflicting.TargetObservation!.MergeStatus);
        Assert.Equal(
            SnapshotEvidenceStatus.Conflicting,
            conflicting.TargetObservation.LoadoutEvidenceStatus);
        Assert.Equal(
            [719],
            conflicting.TargetObservation.Impact.RemovedEquippedSkillIds);
        var conflict = Assert.Single(
            conflicting.TargetObservation.RecommendationImpact!.Conflicts);
        Assert.Equal(
            "target.equippedSkills",
            conflict.Field);
        Assert.Equal(
            [SnapshotDataSource.Save, SnapshotDataSource.CurrentScreenObservation],
            conflict.Sources.Select(source => source.Source));
        Assert.All(
            conflict.Sources,
            source => Assert.NotEqual(default, source.CapturedAtUtc));

        _ = JsonSerializer.Serialize(stale);
        _ = JsonSerializer.Serialize(unsupported);
        _ = JsonSerializer.Serialize(conflicting);
    }

    private static async Task<CombatRecommendationResponse> Recommend(
        CombatSnapshot snapshot,
        CombatRecommendationApiRequest request)
    {
        return Response(await Controller(snapshot).Recommend(
            request,
            TestContext.Current.CancellationToken));
    }

    private static CombatRecommendationResponse Response(
        ActionResult<CombatRecommendationResponse> action)
    {
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<CombatRecommendationResponse>(ok.Value);
    }

    private static CombatRecommendationApiRequest Request(
        TargetLoadoutCoverageKind coverage,
        DateTimeOffset observedAt,
        IReadOnlyList<TargetObservedSkillApiRequest> selectedSkills,
        string evidenceReference = "E3-000-CAP-002",
        TargetObservationContext context =
            TargetObservationContext.Sparring) => new()
            {
                TargetCharacterId = 16317,
                Objective = RecommendationPolicy.Balanced,
                TargetObservation = new TargetObservationApiRequest
                {
                    Context = context,
                    ObservedAt = observedAt,
                    EvidenceReference = evidenceReference,
                    Coverage = coverage,
                    SelectedSkills = selectedSkills
                }
            };

    private static TargetObservedSkillApiRequest SkillRequest(
        int? confirmedSkillId,
        int? visiblePowerPercent = null,
        int? slotIndex = 0) => new()
        {
            VisibleName = "Target Art",
            Category = SkillCategory.Attack,
            ConfirmedSkillId = confirmedSkillId,
            Direction = PracticeDirection.Reverse,
            SlotIndex = slotIndex,
            VisiblePowerPercent = visiblePowerPercent
        };

    private static CombatRecommendationsController Controller(
        CombatSnapshot snapshot,
        params CombatSkillDefinition[] definitions)
    {
        if (definitions.Length == 0)
        {
            definitions = [Definition(719, "Target Art")];
        }

        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return Controller(reader, definitions);
    }

    private static CombatRecommendationsController Controller(
        ICombatSnapshotReader reader,
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        return new CombatRecommendationsController(
            new RecommendCombatLoadout(reader),
            Options.Create(new SaveGameOptions
            {
                DefaultSaveFilePath = ConfiguredSavePath
            }),
            new TargetObservationRecommendationWorkflow(
                reader,
                new ResolveTargetSkillSelection(
                    Source(definitions),
                    Repository(definitions))));
    }

    private static CombatSnapshot Snapshot(
        string gameDataVersion =
            TargetLoadoutCompletenessEvidence.E3000GameDataVersion)
    {
        var targetSkill = new CombatSkillSnapshot(
            719,
            SnapshotValue<string>.Available("Target Art"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(2),
            SnapshotValue<bool>.Available(true),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Direct),
            new SkillSlotContribution(2, 0, 0, 0, 1),
            SnapshotValue<int>.Available(1719),
            SnapshotValue<int>.Available(2719));
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('A', 64),
                SaveTime,
                SnapshotValue<DateTimeOffset>.Available(SaveTime),
                SnapshotValue<string>.Available(gameDataVersion)),
            new PlayerCombatSnapshot(
                1,
                SnapshotValue<string>.Available("Taiwu"),
                learnedSkills: [],
                new CombatLoadoutSnapshot([], [], [], [], []),
                equipment: [],
                new SlotBudgetSet(Enum.GetValues<SkillCategory>().Select(
                    category => new SlotBudget(category, 0, 10))),
                new GenericSlotAllocation(0, 0, 0, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: []),
            new TargetCombatSnapshot(
                16317,
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
            warnings: []);
    }

    private static ICombatSkillDefinitionSource Source(
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var source = Substitute.For<ICombatSkillDefinitionSource>();
        source.ReadAsync(Arg.Any<CancellationToken>()).Returns(
            CombatSkillDefinitionSourceResult.Available(
                Identity,
                definitions));
        return source;
    }

    private static ICombatSkillCatalogueRepository Repository(
        IReadOnlyList<CombatSkillDefinition> definitions)
    {
        var repository = Substitute.For<ICombatSkillCatalogueRepository>();
        repository.ReadStateAsync(Arg.Any<CancellationToken>()).Returns(
            new CombatSkillCatalogueRepositorySnapshot(
                CatalogueRepositoryState.Ready,
                Identity,
                definitions.Count,
                SaveTime));
        repository.QueryAsync(
                Arg.Any<CombatSkillCatalogueFilter>(),
                Arg.Any<CancellationToken>())
            .Returns(definitions);
        return repository;
    }

    private static CombatSkillDefinition Definition(
        int skillId,
        string englishName)
    {
        var source = new CatalogueSourceReference(
            CatalogueSourceKind.GameData,
            "gamedata:test",
            $"combat-skill:{skillId}");
        return new CombatSkillDefinition(
            skillId,
            new CombatSkillLocalizedNames(
            [
                new LocalizedCombatSkillName(
                    CatalogueLanguage.English,
                    englishName,
                    new CatalogueSourceReference(
                        CatalogueSourceKind.EnglishLanguageResource,
                        "language-en:test",
                        $"combat-skill-name:{skillId}"))
            ]),
            CatalogueField<CombatSkillDiscipline>.Available(
                CombatSkillDiscipline.Finger,
                source),
            CatalogueField<CombatSkillGrade>.Available(
                new CombatSkillGrade(5),
                source),
            CatalogueField<CombatSkillFactionId>.Available(
                new CombatSkillFactionId(1),
                source),
            CatalogueField<CombatSkillElement>.Available(
                CombatSkillElement.Wood,
                source),
            CatalogueField<CombatSkillEquipmentType>.Available(
                CombatSkillEquipmentType.Attack,
                source),
            CatalogueField<CombatSkillGridCost>.Available(
                new CombatSkillGridCost(2),
                source),
            CatalogueField<SkillSlotContribution>.Available(
                new SkillSlotContribution(2, 0, 0, 0, 1),
                source),
            requirements: null,
            new CombatSkillTimingDefinition(
                CatalogueField<int>.Available(100, source),
                CatalogueField<int>.Available(100, source),
                CatalogueField<int>.Available(100, source)),
            new CombatSkillEffectReferences(
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(1000 + skillId),
                    source),
                CatalogueField<CombatSkillEffectId>.Available(
                    new CombatSkillEffectId(2000 + skillId),
                    source),
                CatalogueField<CombatSkillEffectId>.Unavailable("unused")),
            rawDescriptions: [],
            source);
    }

    private static CombatSkillCatalogueSourceIdentity Identity { get; } = new(
        "1.0.0-current",
        importerVersion: 1,
        new string('A', 64),
        new string('B', 64),
        new string('C', 64));
}
