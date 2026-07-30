using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Reflection;
using TaiWu.Application.Targets;
using TaiWuAPI.Configuration;
using TaiWuAPI.Contracts.Targets;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class TargetsControllerTests
{
    private const string ConfiguredSavePath =
        @"C:\Taiwu\SaveGames\world_1\local.sav";

    [Fact]
    public async Task Get_returns_structured_ambiguous_matches()
    {
        var reader = Reader();
        var controller = Controller(reader);
        var cancellationToken = TestContext.Current.CancellationToken;

        var action = await controller.Find(
            "何",
            maxResults: 25,
            cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var response = Assert.IsType<TargetLookupResponse>(ok.Value);
        Assert.Equal(TargetLookupStatus.Ambiguous, response.Status);
        Assert.Equal(2, response.TotalMatches);
        Assert.Collection(
            response.Matches,
            first =>
            {
                Assert.Equal("target:16317", first.Reference);
                Assert.Equal("何春石", first.DisplayName);
                Assert.Equal("location:10:20", first.Location.Reference);
            },
            second =>
            {
                Assert.Equal("target:20000", second.Reference);
                Assert.Equal("何春石", second.DisplayName);
                Assert.Equal("location:11:21", second.Location.Reference);
            });
        await reader.Received(1).ReadAsync(
            Arg.Is<TargetLookupReadRequest>(request =>
                request != null
                && request.SaveFilePath == ConfiguredSavePath),
            cancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Missing_query_returns_problem_without_read(string? query)
    {
        var reader = Substitute.For<ITargetLookupReader>();
        var controller = Controller(reader);

        var action = await controller.Find(
            query,
            maxResults: 25,
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task Invalid_limit_returns_problem()
    {
        var reader = Substitute.For<ITargetLookupReader>();
        var controller = Controller(reader);

        var action = await controller.Find(
            "何",
            maxResults: 0,
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task Expected_reader_failure_returns_problem()
    {
        var reader = Substitute.For<ITargetLookupReader>();
        reader.ReadAsync(
                Arg.Any<TargetLookupReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<TargetLookupSnapshot>(
                    new FileNotFoundException("Missing save.")));
        var controller = Controller(reader);

        var action = await controller.Find(
            "何",
            maxResults: 25,
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(
            "Missing save.",
            Assert.IsType<ProblemDetails>(problem.Value).Detail);
    }

    [Fact]
    public void Contract_is_one_query_only_get_action()
    {
        var controller = typeof(TargetsController);
        Assert.Equal(
            "api/targets",
            controller.GetCustomAttribute<RouteAttribute>()?.Template);
        var actions = controller.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly)
            .Where(method =>
                method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();

        var action = Assert.Single(actions);
        Assert.Equal("Find", action.Name);
        Assert.NotNull(action.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal(
            typeof(Task<ActionResult<TargetLookupResponse>>),
            action.ReturnType);
    }

    private static TargetsController Controller(
        ITargetLookupReader reader)
    {
        return new TargetsController(
            new FindTargets(reader),
            Options.Create(
                new SaveGameOptions
                {
                    DefaultSaveFilePath = ConfiguredSavePath
                }));
    }

    private static ITargetLookupReader Reader()
    {
        var reader = Substitute.For<ITargetLookupReader>();
        reader.ReadAsync(
                Arg.Any<TargetLookupReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(
                new TargetLookupSnapshot(
                    DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
                    "game-version",
                    [
                        new TargetLookupEntry(
                            16317,
                            "何春石",
                            age: 52,
                            areaId: 10,
                            blockId: 20),
                        new TargetLookupEntry(
                            20000,
                            "何春石",
                            age: 41,
                            areaId: 11,
                            blockId: 21)
                    ],
                    [
                        new TargetLookupWarning(
                            "SOURCE_WARNING",
                            "Preserved warning.")
                    ]));
        return reader;
    }
}
