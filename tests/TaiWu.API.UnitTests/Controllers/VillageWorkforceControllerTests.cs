using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NSubstitute;
using System.Reflection;
using System.Text.Json;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.VillageWorkforce;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class VillageWorkforceControllerTests
{
    [Fact]
    public void Routes_are_get_only_and_expose_no_mutation_action()
    {
        var methods = typeof(VillageWorkforceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.DeclaringType
                == typeof(VillageWorkforceController))
            .Select(method => new
            {
                method.Name,
                Attributes = method.GetCustomAttributes<HttpMethodAttribute>()
                    .ToArray()
            })
            .Where(item => item.Attributes.Length > 0)
            .ToArray();

        Assert.Equal(["Options", "Result"], methods.Select(item => item.Name));
        Assert.All(methods, item => Assert.All(
            item.Attributes,
            attribute => Assert.IsType<HttpGetAttribute>(attribute)));
    }

    [Fact]
    public async Task Discovery_localizes_stable_objective_and_targets()
    {
        var snapshot = Snapshot([Worker(101, 60), Worker(202, 80)]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var controller = Controller(reader);

        var englishAction = await controller.Options(
            VillageWorkforceApiTokens.English,
            TestContext.Current.CancellationToken);
        var chineseAction = await controller.Options(
            VillageWorkforceApiTokens.TraditionalChinese,
            TestContext.Current.CancellationToken);
        var english = OkValue(englishAction);
        var chinese = OkValue(chineseAction);

        Assert.Equal(VillageWorkforceApiStatus.Complete, english.Status);
        Assert.Single(english.Objectives);
        Assert.Single(english.Targets);
        Assert.Equal(
            english.Objectives[0].Identity,
            chinese.Objectives[0].Identity);
        Assert.Equal(
            english.Targets[0].Reference,
            chinese.Targets[0].Reference);
        Assert.NotEqual(
            english.Objectives[0].Label,
            chinese.Objectives[0].Label);
        Assert.NotEqual(english.Targets[0].Label, chinese.Targets[0].Label);
        await reader.Received(2).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_result_maps_full_typed_chain_without_source_secrets()
    {
        var snapshot = Snapshot([
            Worker(101, 60),
            Worker(202, 80)
        ], diagnostics: [new WorkforceDiagnostic(
            "STANDALONE_RUNTIME_BOUNDARY",
            WorkforceDiagnosticSeverity.Information,
            [])]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var controller = Controller(reader);
        var query = Query(snapshot) with
        {
            FirstComparisonCharacterId = 202,
            SecondComparisonCharacterId = 101,
            ProposedCharacterId = 202
        };

        var action = await controller.Result(
            query,
            TestContext.Current.CancellationToken);
        var response = OkValue(action);

        Assert.Equal(VillageWorkforceApiStatus.Complete, response.Status);
        Assert.Equal(
            VillageWorkforceApiSnapshotStatus.Complete,
            response.Source?.SnapshotStatus);
        Assert.Equal(
            VillageWorkforceApiVacancyState.NoExplicitVacancy,
            response.Target?.VacancyState);
        Assert.Equal(101, response.CurrentAssignment?.CharacterId);
        Assert.Equal(2, response.Counts?.Total);
        Assert.Equal(2, response.Counts?.Visible);
        Assert.Equal(
            [202, 101],
            response.Candidates.Select(item => item.CharacterId));
        var first = response.Candidates[0];
        Assert.Equal(1, first.CompetitionRank);
        Assert.Equal(80m, first.Total);
        Assert.Equal(
            VillageWorkforceApiEvaluationState.Ranked,
            first.EvaluationState);
        Assert.Equal(5, first.Requirements.Count);
        Assert.All(first.Requirements, item => Assert.Equal(
            VillageWorkforceApiRequirementOutcome.Passed,
            item.Outcome));
        var component = Assert.Single(first.Components);
        Assert.Equal((short)80, component.RawValue);
        Assert.Equal(80m, component.Contribution);
        Assert.NotEmpty(component.Evidence);
        Assert.Equal(
            VillageWorkforceApiComparisonOutcome.Higher,
            response.Comparison?.Outcome);
        Assert.Equal(
            "village-worker:202",
            response.ManualPlan?.ProposedWorkerReference);
        Assert.Equal(5, response.ManualPlan?.Checklist.Count);
        Assert.Equal(3, response.Limitations.Count);
        Assert.Single(response.Diagnostics);

        var json = JsonSerializer.Serialize(response, JsonOptions());
        Assert.DoesNotContain(ShaA, json, StringComparison.Ordinal);
        Assert.DoesNotContain("RevisionIdentity", json, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceIdentity", json, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveSha256", json, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", json, StringComparison.Ordinal);
        await reader.Received(1).ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Partial_result_uses_206_and_retains_incomplete_worker()
    {
        var snapshot = Snapshot([Worker(
            101,
            qualification: null,
            qualificationState: WorkforceEvidenceState.Incomplete)]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));

        var action = await Controller(reader).Result(
            Query(snapshot),
            TestContext.Current.CancellationToken);
        var objectResult = Assert.IsType<ObjectResult>(action.Result);
        var response = Assert.IsType<VillageWorkforceResultResponse>(
            objectResult.Value);

        Assert.Equal(StatusCodes.Status206PartialContent, objectResult.StatusCode);
        Assert.Equal(VillageWorkforceApiStatus.Partial, response.Status);
        Assert.Equal(1, response.Counts?.Incomplete);
        Assert.Null(Assert.Single(response.Candidates).Total);
    }

    [Theory]
    [InlineData("0", VillageWorkforceApiTokens.FilterAll, VillageWorkforceApiTokens.English)]
    [InlineData(VillageWorkforceApiTokens.Objective, "0", VillageWorkforceApiTokens.English)]
    [InlineData(VillageWorkforceApiTokens.Objective, VillageWorkforceApiTokens.FilterAll, "0")]
    public async Task Unknown_or_numeric_tokens_are_safe_400_before_workflow(
        string objective,
        string filter,
        string language)
    {
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        var finder = Substitute.For<IFindVillageWorkforce>();
        var query = Query(snapshot) with
        {
            Objective = objective,
            Filter = filter,
            Language = language
        };

        var action = await new VillageWorkforceController(reader, finder)
            .Result(query, TestContext.Current.CancellationToken);
        var result = Assert.IsType<ObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(
            "VILLAGE_WORKFORCE_REQUEST_INVALID",
            problem.Extensions["code"]);
        await finder.DidNotReceive().ExecuteAsync(
            Arg.Any<VillageWorkforceFinderRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(VillageWorkforceSnapshotReadStatus.SaveUnavailable, 404)]
    [InlineData(VillageWorkforceSnapshotReadStatus.UnsupportedVersion, 422)]
    [InlineData(VillageWorkforceSnapshotReadStatus.ConflictingSources, 409)]
    [InlineData(VillageWorkforceSnapshotReadStatus.ChangedRevision, 409)]
    [InlineData(VillageWorkforceSnapshotReadStatus.ReadFailed, 500)]
    public async Task Source_failures_map_to_safe_http_problem(
        VillageWorkforceSnapshotReadStatus status,
        int expectedStatus)
    {
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Reader(VillageWorkforceSnapshotReadResult.Failed(
            status,
            "ADAPTER_INTERNAL_IDENTITY",
            "C:\\private\\save.twV1: synthetic exception text"));

        var action = await Controller(reader).Result(
            Query(snapshot),
            TestContext.Current.CancellationToken);
        var result = Assert.IsType<ObjectResult>(action.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        var serialized = JsonSerializer.Serialize(problem, JsonOptions());

        Assert.Equal(expectedStatus, result.StatusCode);
        Assert.DoesNotContain("private", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("synthetic exception", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_target_is_404_and_invalid_comparison_is_typed_400()
    {
        var snapshot = Snapshot([Worker(101, 60), Worker(202, 80)]);
        var reader = Reader(
            VillageWorkforceSnapshotReadResult.Complete(snapshot));
        var controller = Controller(reader);
        var missingTarget = Query(snapshot) with { BuildingBlockIndex = 99 };
        var invalidComparison = Query(snapshot) with
        {
            FirstComparisonCharacterId = 101,
            SecondComparisonCharacterId = 999
        };

        var missingAction = await controller.Result(
            missingTarget,
            TestContext.Current.CancellationToken);
        var comparisonAction = await controller.Result(
            invalidComparison,
            TestContext.Current.CancellationToken);
        var missing = Assert.IsType<ObjectResult>(missingAction.Result);
        var comparison = Assert.IsType<ObjectResult>(comparisonAction.Result);

        Assert.Equal(StatusCodes.Status404NotFound, missing.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, comparison.StatusCode);
        Assert.Equal(
            VillageWorkforceApiStatus.InvalidComparison,
            Assert.IsType<VillageWorkforceResultResponse>(comparison.Value)
                .Status);
    }

    [Fact]
    public async Task Cancellation_and_unexpected_fault_propagate_to_host()
    {
        var snapshot = Snapshot([Worker(101, 60)]);
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        var finder = Substitute.For<IFindVillageWorkforce>();
        using var cancellation = new CancellationTokenSource();
        finder.ExecuteAsync(
                Arg.Any<VillageWorkforceFinderRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<VillageWorkforceFinderResult>(
                    call.ArgAt<CancellationToken>(1));
            });
        var controller = new VillageWorkforceController(reader, finder);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            controller.Result(Query(snapshot), cancellation.Token));

        finder.ExecuteAsync(
                Arg.Any<VillageWorkforceFinderRequest>(),
                Arg.Any<CancellationToken>())
            .Returns<VillageWorkforceFinderResult>(_ =>
                throw new InvalidOperationException("programmer fault"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.Result(
                Query(snapshot),
                TestContext.Current.CancellationToken));
    }

    private const string ShaA =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private static VillageWorkforceController Controller(
        IVillageWorkforceSnapshotReader reader) =>
        new(reader, new FindVillageWorkforce(reader));

    private static IVillageWorkforceSnapshotReader Reader(
        VillageWorkforceSnapshotReadResult result)
    {
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(result);
        return reader;
    }

    private static VillageWorkforceApiQuery Query(
        VillageWorkforceSnapshot snapshot)
    {
        var target = snapshot.Targets[0].Identity;
        return new VillageWorkforceApiQuery
        {
            AreaId = target.Building.AreaId,
            BlockId = target.Building.BlockId,
            BuildingBlockIndex = target.Building.BuildingBlockIndex,
            ManagerSlotIndex = target.ManagerSlotIndex
        };
    }

    private static T OkValue<T>(ActionResult<T> action)
    {
        var result = Assert.IsType<OkObjectResult>(action.Result);
        return Assert.IsType<T>(result.Value);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonOptions();
        ApiJsonOptions.Configure(options);
        return options.JsonSerializerOptions;
    }

    private static VillageWorkforceSnapshot Snapshot(
        IEnumerable<VillageWorkerProfile> workers,
        IEnumerable<WorkforceDiagnostic>? diagnostics = null)
    {
        var copied = workers.ToArray();
        var versions = Versions();
        var save = SaveProvenance();
        var target = new ShopManagerTarget(
            new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(1, 2, 7),
                0),
            new LifeSkillDisciplineIdentity(6),
            [new WorkforceEvidenceReference(
                "SHOP_TARGET",
                GameDataProvenance())]);
        return new VillageWorkforceSnapshot(
            new SettlementIdentity(12),
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
            versions,
            copied,
            [target],
            [new CurrentShopManagerAssignment(
                target.Identity,
                copied[0].Identity,
                save)],
            diagnostics ?? []);
    }

    private static VillageWorkerProfile Worker(
        int characterId,
        short? qualification,
        WorkforceEvidenceState qualificationState =
            WorkforceEvidenceState.Confirmed)
    {
        var save = SaveProvenance();
        var qualificationIdentity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));
        var qualificationFact = qualificationState switch
        {
            WorkforceEvidenceState.Confirmed => WorkforceFact.Confirmed(
                qualificationIdentity,
                WorkforceFactValue.Int16(qualification
                    ?? throw new ArgumentNullException(nameof(qualification))),
                save,
                [new WorkforceEvidenceReference("QUALIFICATION", save)]),
            WorkforceEvidenceState.Incomplete => WorkforceFact.Incomplete(
                qualificationIdentity,
                new WorkforceUnavailableReason("QUALIFICATION_MISSING"),
                [new WorkforceEvidenceReference("QUALIFICATION", save)]),
            _ => throw new ArgumentOutOfRangeException(
                nameof(qualificationState))
        };
        return new VillageWorkerProfile(
            new VillageWorkerIdentity(characterId),
            WorkforceWorkerState.Eligible,
            Versions(),
            [
                WorkforceFact.Confirmed(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.CandidateUniverseMembership),
                    WorkforceFactValue.Boolean(true),
                    save,
                    [new WorkforceEvidenceReference(
                        "WORK_CANDIDATE",
                        save)]),
                qualificationFact
            ],
            []);
    }

    private static WorkforceSourceVersions Versions() => new(
        ShaA,
        VerifiedVillageWorkforceRules.SupportedGameDataVersion,
        "1",
        "1",
        "1");

    private static WorkforceProvenance SaveProvenance() => new(
        WorkforceEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        "1",
        ShaA);

    private static WorkforceProvenance GameDataProvenance() => new(
        WorkforceEvidenceSourceKind.InstalledGameData,
        "GAMEDATA",
        VerifiedVillageWorkforceRules.SupportedGameDataVersion,
        "ASSEMBLY_A");
}
