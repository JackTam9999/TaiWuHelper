using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Reflection;
using TaiWu.Application.SaveGames;
using TaiWu.Domain.SaveGames;
using TaiWuAPI.Configuration;
using TaiWuAPI.Controllers;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class SaveGamesControllerTests
{
    private const string ConfiguredSavePath =
        @"C:\Taiwu\SaveGames\world_1\local.sav";

    [Fact]
    public async Task ReadConfigured_UsesConfiguredPathTargetAndCancellation()
    {
        var reader = Substitute.For<ISaveGameReader>();
        var expected = new SaveGameReport(["TAIWU|21396"]);
        reader.ReadAsync(
                Arg.Any<SaveGameReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);
        var controller = Controller(reader);
        var cancellationToken = TestContext.Current.CancellationToken;

        var action = await controller.ReadConfigured(
            targetCharacterId: 16317,
            cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        await reader.Received(1).ReadAsync(
            Arg.Is<SaveGameReadRequest>(request =>
                request != null
                && request.SaveFilePath == ConfiguredSavePath
                && request.TargetCharacterId == 16317),
            cancellationToken);
    }

    [Fact]
    public async Task ReadConfigured_ExpectedReaderFailureReturnsProblem()
    {
        var reader = Substitute.For<ISaveGameReader>();
        reader.ReadAsync(
                Arg.Any<SaveGameReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<SaveGameReport>(
                    new InvalidDataException("The save changed.")));
        var controller = Controller(reader);

        var action = await controller.ReadConfigured(
            targetCharacterId: null,
            TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ObjectResult>(action.Result);
        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(
            "The save changed.",
            Assert.IsType<ProblemDetails>(problem.Value).Detail);
    }

    [Fact]
    public async Task ReadConfigured_RequestCancellationPropagates()
    {
        using var cancellation = CancellationTokenSource
            .CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();
        var reader = Substitute.For<ISaveGameReader>();
        reader.ReadAsync(
                Arg.Any<SaveGameReadRequest>(),
                cancellation.Token)
            .Returns(Task.FromCanceled<SaveGameReport>(cancellation.Token));
        var controller = Controller(reader);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => controller.ReadConfigured(
                targetCharacterId: null,
                cancellation.Token));
    }

    [Fact]
    public void Contract_IsOneReadOnlyGetAction()
    {
        var controller = typeof(SaveGamesController);
        Assert.Equal(
            "api/save-games",
            controller.GetCustomAttribute<RouteAttribute>()?.Template);
        var actions = controller.GetMethods(
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.DeclaredOnly)
            .Where(method =>
                method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();

        var action = Assert.Single(actions);
        var get = Assert.IsType<HttpGetAttribute>(
            action.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal("read", get.Template);
        Assert.Equal(
            typeof(Task<ActionResult<SaveGameReport>>),
            action.ReturnType);
    }

    private static SaveGamesController Controller(ISaveGameReader reader)
    {
        return new SaveGamesController(
            new ReadSaveGame(reader),
            Options.Create(
                new SaveGameOptions
                {
                    DefaultSaveFilePath = ConfiguredSavePath
                }));
    }
}
