using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using System.Net;
using TaiWu.Application.CombatSkills;
using Xunit;

namespace TaiWu.API.UnitTests.Controllers;

public sealed class CombatSkillsAntiforgeryTests
{
    [Theory]
    [InlineData("/api/combat-skills/catalogue-cache/rebuild")]
    [InlineData("/api/combat-skills/progress-cache/clear")]
    public async Task Maintenance_post_without_token_returns_bad_request(
        string path)
    {
        using var factory = CreateFactory(out _);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            path,
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Maintenance_post_with_valid_token_executes_action()
    {
        using var factory = CreateFactory(out var maintenance);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var antiforgery = scope.ServiceProvider.GetRequiredService<IAntiforgery>();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        var tokens = antiforgery.GetAndStoreTokens(context);
        var setCookie = Assert.Single(context.Response.Headers.SetCookie);
        Assert.NotNull(setCookie);
        var cookie = setCookie.Split(';', 2)[0];
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/combat-skills/progress-cache/clear");
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add(tokens.HeaderName!, tokens.RequestToken);

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await maintenance.Received(1).ClearAsync(
            Arg.Any<CancellationToken>());
    }

    private static WebApplicationFactory<Program> CreateFactory(
        out ICharacterCombatSkillProgressCacheMaintenance maintenance)
    {
        maintenance =
            Substitute.For<ICharacterCombatSkillProgressCacheMaintenance>();
        maintenance.ClearAsync(Arg.Any<CancellationToken>()).Returns(3);
        var replacement = maintenance;

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<
                    ICharacterCombatSkillProgressCacheMaintenance>();
                services.AddSingleton(replacement);
            }));
    }
}
