using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.CombatRecommendations;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class TacticalCombatApiTests
{
    [Fact]
    public async Task Complete_result_preserves_typed_pipeline_and_fallback_plan()
    {
        var fixture = Fixture();

        var action = await fixture.Controller.Recommend(
            Request(ConfirmedObservations()),
            TestContext.Current.CancellationToken);

        var response = Response(action);
        var tactical = Assert.IsType<TacticalCombatResponse>(
            response.TacticalPlanning);
        Assert.Equal(TacticalCombatRecommendationStatus.Success, tactical.Status);
        Assert.NotNull(tactical.Snapshot);
        Assert.NotNull(tactical.TargetChain);
        Assert.NotNull(tactical.ExecutionContext);
        Assert.NotNull(tactical.CandidateDiscovery);
        Assert.NotNull(tactical.Search);
        Assert.NotNull(tactical.Scoring);
        Assert.NotNull(tactical.SelectedLoadout);
        Assert.NotNull(tactical.Plan);
        Assert.Equal(
            TacticalFinishDisposition.Unsupported,
            tactical.Plan.FinishDisposition);
        Assert.Equal(
            Enum.GetValues<TacticalPlanStage>(),
            tactical.Plan.Stages.Select(item => item.Stage));
        Assert.All(
            tactical.CandidateDiscovery.Candidates,
            candidate => Assert.Equal(
                Enum.GetValues<TacticalCandidateGateKind>()
                    .OrderBy(item => item.ToString(), StringComparer.Ordinal),
                candidate.Gates.Select(gate => gate.Kind)));
        Assert.Equal(1, tactical.Diagnostics.WorkCounts.SnapshotReads);
        await fixture.Reader.Received(1).ReadAsync(
            Arg.Any<CombatSnapshotReadRequest>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Partial_and_conflicting_evidence_survive_mapping()
    {
        var partial = await Fixture().Controller.Recommend(
            Request([]),
            TestContext.Current.CancellationToken);
        var observations = ConfirmedObservations();
        observations[0] = observations[0] with
        {
            Disposition = TacticalRuleEvidenceDisposition.Conflicting
        };
        var conflicting = await Fixture().Controller.Recommend(
            Request(observations),
            TestContext.Current.CancellationToken);

        var partialResult = PartialResponse(partial).TacticalPlanning!;
        var conflictingResult = PartialResponse(conflicting).TacticalPlanning!;
        Assert.Equal(
            TacticalCombatRecommendationStatus.PartialEvidence,
            partialResult.Status);
        Assert.Contains(
            partialResult.TargetChain!.Transitions,
            item => item.Applicability == TacticalRuleApplicability.Incomplete);
        Assert.Equal(
            TacticalCombatRecommendationStatus.PartialEvidence,
            conflictingResult.Status);
        Assert.Contains(
            conflictingResult.TargetChain!.Transitions,
            item => item.Applicability == TacticalRuleApplicability.Conflicting);
    }

    [Fact]
    public async Task Unsupported_and_truncated_states_survive_mapping()
    {
        var unsupported = await Fixture("UNSUPPORTED-GAME-DATA")
            .Controller.Recommend(
                Request(ConfirmedObservations()),
                TestContext.Current.CancellationToken);
        var truncatedRequest = Request(
            ConfirmedObservations(),
            new TacticalSearchBoundsApiRequest
            {
                MaximumOptions = 8,
                MaximumExploredCombinations = 1,
                MaximumElapsedMilliseconds = 30_000,
                MaximumResults = 100
            });
        var truncated = await Fixture().Controller.Recommend(
            truncatedRequest,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            TacticalCombatRecommendationStatus.UnsupportedChain,
            Response(unsupported).TacticalPlanning!.Status);
        var truncatedResult = PartialResponse(truncated).TacticalPlanning!;
        Assert.Equal(
            TacticalCombatRecommendationStatus.SearchTruncated,
            truncatedResult.Status);
        Assert.Equal(
            TacticalSearchTerminator.ExplorationLimit,
            truncatedResult.Search!.Coverage.FirstTerminator);
    }

    [Fact]
    public async Task Rejected_candidate_and_failed_gate_survive_mapping()
    {
        var action = await Fixture(reverseEffectId: 9999).Controller.Recommend(
            Request(ConfirmedObservations()),
            TestContext.Current.CancellationToken);

        var tactical = Response(action).TacticalPlanning!;
        Assert.Equal(TacticalCombatRecommendationStatus.NoCandidate,
            tactical.Status);
        var rejected = Assert.Single(
            tactical.CandidateDiscovery!.Candidates,
            item => item.Direction == PracticeDirection.Reverse);
        Assert.Equal(TacticalCandidateAdmissionState.Infeasible,
            rejected.AdmissionState);
        Assert.Equal(TacticalCandidateDecision.Rejected, rejected.Decision);
        Assert.Contains(
            rejected.Gates,
            gate => gate.Kind == TacticalCandidateGateKind.RawEffect
                && gate.State == TacticalCandidateGateState.Failed);
    }

    [Fact]
    public async Task Invalid_bound_and_unknown_identity_return_safe_problem()
    {
        var invalidBounds = Request(
            ConfirmedObservations(),
            new TacticalSearchBoundsApiRequest
            {
                MaximumOptions = 25
            });
        var invalidIdentity = Request(
        [
            new TacticalRuleObservationApiRequest
            {
                Identity = "UNKNOWN_RULE_EVIDENCE",
                Scope = TacticalRuleEvidenceScope.ExactTarget,
                Source = TacticalEvidenceSourceKind.ConfirmedObservation,
                Disposition = TacticalRuleEvidenceDisposition.Confirmed,
                EvidenceIdentity = "PUBLIC_OBSERVATION",
                ScopeIdentity = "EXACT_TARGET"
            }
        ]);

        var boundsAction = await Fixture().Controller.Recommend(
            invalidBounds,
            TestContext.Current.CancellationToken);
        var identityAction = await Fixture().Controller.Recommend(
            invalidIdentity,
            TestContext.Current.CancellationToken);

        AssertSafeBadRequest(boundsAction);
        AssertSafeBadRequest(identityAction);
    }

    [Fact]
    public async Task Observation_repeat_replace_and_clear_change_whole_identity()
    {
        var fixture = Fixture();
        var applied = Request(ConfirmedObservations());
        var replacedValues = ConfirmedObservations();
        replacedValues[0] = replacedValues[0] with
        {
            Disposition = TacticalRuleEvidenceDisposition.Contrary
        };
        var replaced = Request(replacedValues);
        var clear = Request([]);

        var first = Response(await fixture.Controller.Recommend(
            applied,
            TestContext.Current.CancellationToken)).TacticalPlanning!;
        var repeat = Response(await fixture.Controller.Recommend(
            applied,
            TestContext.Current.CancellationToken)).TacticalPlanning!;
        var replacement = SuccessResponse(await fixture.Controller.Recommend(
            replaced,
            TestContext.Current.CancellationToken)).TacticalPlanning!;
        var cleared = PartialResponse(await fixture.Controller.Recommend(
            clear,
            TestContext.Current.CancellationToken)).TacticalPlanning!;

        Assert.Equal(
            first.Identity!.SemanticFingerprint,
            repeat.Identity!.SemanticFingerprint);
        Assert.NotEqual(
            first.Identity.SemanticFingerprint,
            replacement.Identity!.SemanticFingerprint);
        Assert.NotEqual(
            replacement.Identity.SemanticFingerprint,
            cleared.Identity!.SemanticFingerprint);
    }

    [Fact]
    public async Task Public_json_has_no_path_payload_or_numeric_enum()
    {
        var response = Response(await Fixture().Controller.Recommend(
            Request(ConfirmedObservations()),
            TestContext.Current.CancellationToken));
        var json = JsonSerializer.Serialize(response, JsonOptions());

        Assert.DoesNotContain("local.sav", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("saveFilePath", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rawSourceText", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status\":\"Success\"", json);
        Assert.Contains("\"finishDisposition\":\"Unsupported\"", json);
    }

    [Fact]
    public async Task Cancellation_propagates_without_problem_response()
    {
        var fixture = Fixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Controller.Recommend(
                Request(ConfirmedObservations()),
                cancellation.Token));
    }

    private static ApiFixture Fixture(
        string gameDataVersion = VerifiedTacticalCombatRuleSets
            .HistoricalGameDataVersion,
        bool isMastered = true,
        int reverseEffectId = 1064)
    {
        var reader = Substitute.For<ICombatSnapshotReader>();
        reader.ReadAsync(
                Arg.Any<CombatSnapshotReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Snapshot(gameDataVersion, isMastered, reverseEffectId));
        var reporter = Substitute.For<
            ITacticalCombatRecommendationFaultReporter>();
        var workflow = new RecommendTacticalCombat(
            reader,
            new ZeroElapsedTimeProvider(),
            reporter);
        var controller = new CombatRecommendationsController(
            Substitute.For<IRecommendCombatLoadout>(),
            Options.Create(new SaveGameOptions
            {
                DefaultSaveFilePath = "local.sav"
            }),
            targetObservationWorkflow: null,
            workflow);
        return new ApiFixture(reader, controller);
    }

    private static CombatRecommendationApiRequest Request(
        IReadOnlyList<TacticalRuleObservationApiRequest> observations,
        TacticalSearchBoundsApiRequest? bounds = null) => new()
        {
            TargetCharacterId = 2,
            Objective = RecommendationPolicy.Balanced,
            TacticalPlanning = new TacticalPlanningApiRequest
            {
                Observations = observations,
                Bounds = bounds ?? new TacticalSearchBoundsApiRequest
                {
                    MaximumOptions = 8,
                    MaximumExploredCombinations = 100,
                    MaximumElapsedMilliseconds = 30_000,
                    MaximumResults = 100
                }
            }
        };

    private static TacticalRuleObservationApiRequest[] ConfirmedObservations() =>
        VerifiedTacticalCombatRuleSets.HistoricalMagicSound.Transitions
            .SelectMany(item => item.EvidenceRequirements)
            .Concat(VerifiedTacticalCombatRuleSets.HistoricalMagicSound.Roles
                .SelectMany(item => item.EvidenceRequirements))
            .DistinctBy(item => (item.Identity.Code, item.Scope, item.Source))
            .Select((item, index) => new TacticalRuleObservationApiRequest
            {
                Identity = item.Identity.Code,
                Scope = item.Scope,
                Source = item.Source,
                Disposition = TacticalRuleEvidenceDisposition.Confirmed,
                EvidenceIdentity = $"PUBLIC_OBSERVATION_{index:000}",
                ScopeIdentity = item.Scope == TacticalRuleEvidenceScope.ExactTarget
                    ? "EXACT_TARGET"
                    : "BROAD_RULE"
            })
            .ToArray();

    private static CombatSnapshot Snapshot(
        string gameDataVersion,
        bool isMastered,
        int reverseEffectId)
    {
        var skill = new CombatSkillSnapshot(
            604,
            SnapshotValue<string>.Available("display-only"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(3),
            SnapshotValue<bool>.Available(isMastered),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Reverse),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(338),
            SnapshotValue<int>.Available(reverseEffectId),
            element: SnapshotValue<CombatSkillElement>.Available(
                CombatSkillElement.Water));
        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                new string('D', 64),
                DateTimeOffset.Parse("2026-08-20T12:00:00Z"),
                SnapshotValue<DateTimeOffset>.Available(
                    DateTimeOffset.Parse("2026-08-20T11:00:00Z")),
                SnapshotValue<string>.Available(gameDataVersion)),
            new PlayerCombatSnapshot(
                1,
                SnapshotValue<string>.Unavailable("Not required."),
                [skill],
                new CombatLoadoutSnapshot([], [], [], [], []),
                equipment: [],
                Budgets(),
                new GenericSlotAllocation(2, 1, 1, 0, 0),
                legendaryBookCostSlots: [],
                legendaryBookCostAssignments: [],
                SnapshotValue<InnerPowerStateSnapshot>.Available(
                    new InnerPowerStateSnapshot(
                        1,
                        SnapshotValue<string>.Unavailable("Not required."),
                        SnapshotValue<string>.Unavailable("Not required."),
                        ElementAdjustmentSet.None,
                        ElementAdjustmentSet.None,
                        CombatSkillElement.Fire))),
            new TargetCombatSnapshot(
                2,
                SnapshotValue<string>.Unavailable("Not required."),
                SnapshotValue<int>.Unavailable("Not required."),
                features: [],
                learnedSkills: [],
                SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "Not required."),
                equipment: []),
            warnings: []);
    }

    private static SlotBudgetSet Budgets() => new(
    [
        new SlotBudget(SkillCategory.Neigong, 0, 6),
        new SlotBudget(SkillCategory.Attack, 0, 10),
        new SlotBudget(SkillCategory.Agility, 0, 8),
        new SlotBudget(SkillCategory.Defense, 0, 8),
        new SlotBudget(SkillCategory.Assistance, 0, 2)
    ]);

    private static CombatRecommendationResponse Response(
        ActionResult<CombatRecommendationResponse> action) =>
        Assert.IsType<CombatRecommendationResponse>(
            Assert.IsType<OkObjectResult>(action.Result).Value);

    private static CombatRecommendationResponse PartialResponse(
        ActionResult<CombatRecommendationResponse> action)
    {
        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status206PartialContent, result.StatusCode);
        return Assert.IsType<CombatRecommendationResponse>(result.Value);
    }

    private static CombatRecommendationResponse SuccessResponse(
        ActionResult<CombatRecommendationResponse> action)
    {
        var result = Assert.IsAssignableFrom<ObjectResult>(action.Result);
        Assert.Contains(
            result.StatusCode ?? StatusCodes.Status200OK,
            new[]
            {
                StatusCodes.Status200OK,
                StatusCodes.Status206PartialContent
            });
        return Assert.IsType<CombatRecommendationResponse>(result.Value);
    }

    private static void AssertSafeBadRequest(
        ActionResult<CombatRecommendationResponse> action)
    {
        var result = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("INVALID_TACTICAL_REQUEST", problem.Extensions["code"]);
        Assert.DoesNotContain("maximum", problem.Detail!,
            StringComparison.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonOptions();
        ApiJsonOptions.Configure(options);
        return options.JsonSerializerOptions;
    }

    private sealed record ApiFixture(
        ICombatSnapshotReader Reader,
        CombatRecommendationsController Controller);

    private sealed class ZeroElapsedTimeProvider : TimeProvider
    {
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => 0;
    }
}
