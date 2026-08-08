using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using System.Text.RegularExpressions;
using TaiWu.Application.Localization;
using TaiWu.Application.RegionStories;
using TaiWuAPI.Components.Pages;
using TaiWuAPI.Configuration;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class RegionStoriesRenderingTests
{
    [Fact]
    public async Task Chinese_page_shows_rewards_and_correct_save_statuses()
    {
        var reader = Substitute.For<IRegionStoryProgressReader>();
        reader.ReadAsync(
                Arg.Any<RegionStoryProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(CurrentSnapshot());

        var html = await RenderPageAsync(reader, TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("地區故事、獎勵與下一步", text);
        Assert.Contains("已完成結局 2", text);
        Assert.Contains("進行中 2", text);
        Assert.Contains("目前未進行 11", text);
        Assert.Contains("少林派 《禪武之道》 進行中", text);
        Assert.Contains("老僧傳授", text);
        Assert.Contains("武當派 《龜蛇蟠扶》 進行中", text);
        Assert.Contains("逆練功法", text);
        Assert.Contains("鑄劍山莊 《銅生試劍》 昌盛結局", text);
        Assert.Contains("世界 71 年 8 月", text);
        Assert.Contains("五仙教 《五聖心毒》 昌盛結局", text);
        Assert.Contains("世界 89 年 3 月", text);
        Assert.Contains("煉製王蠱", text);
        Assert.Contains("驅動王蠱", text);
        Assert.Contains("後傳強化", text);
        Assert.Contains("不會推測後傳是否完成", text);
        Assert.Contains("data-organization-id=\"12\"", html);
        Assert.Contains("data-story-status=\"ProsperousEnding\"", html);
        await reader.Received(1).ReadAsync(
            Arg.Is<RegionStoryProgressReadRequest>(request =>
                request != null
                && request.SaveFilePath
                    == "C:\\SaveGames\\world_1\\local.sav"
                && request.Language == TaiwuLanguage.Chinese),
            Arg.Any<CancellationToken>());
    }

    private static async Task<string> RenderPageAsync(
        IRegionStoryProgressReader reader,
        TaiwuLanguage language)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(reader);
        services.AddSingleton(new ReadRegionStoryProgress(reader));
        services.AddSingleton<IOptions<SaveGameOptions>>(
            Options.Create(new SaveGameOptions
            {
                DefaultSaveFilePath =
                    "C:\\SaveGames\\world_1\\local.sav"
            }));
        using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<
                CascadingValue<TaiwuLanguage>>(
                ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        [nameof(CascadingValue<TaiwuLanguage>.Value)] = language,
                        [nameof(CascadingValue<TaiwuLanguage>.ChildContent)] =
                            (RenderFragment)(builder =>
                            {
                                builder.OpenComponent<RegionStories>(0);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static RegionStoryProgressSnapshot CurrentSnapshot()
    {
        var entries = Enumerable.Range(1, 15)
            .Select(organizationId => new RegionStoryProgressEntry(
                organizationId,
                RegionStoryProgressStatus.NotCompleted,
                CompletionDate: null,
                ActiveTaskChainId: null,
                CurrentTaskId: null,
                CurrentTaskTitle: null,
                CurrentTaskDescription: null))
            .ToArray();
        entries[0] = entries[0] with
        {
            Status = RegionStoryProgressStatus.InProgress,
            ActiveTaskChainId = 27,
            CurrentTaskId = 142,
            CurrentTaskTitle = "老僧傳授",
            CurrentTaskDescription = "持有雕像，等待老僧入夢再戰。"
        };
        entries[3] = entries[3] with
        {
            Status = RegionStoryProgressStatus.InProgress,
            ActiveTaskChainId = 29,
            CurrentTaskId = 167,
            CurrentTaskTitle = "逆練功法",
            CurrentTaskDescription = "以逆練參悟任意武當派功法。"
        };
        entries[8] = entries[8] with
        {
            Status = RegionStoryProgressStatus.ProsperousEnding,
            CompletionDate = 860
        };
        entries[11] = entries[11] with
        {
            Status = RegionStoryProgressStatus.ProsperousEnding,
            CompletionDate = 1071
        };
        return new RegionStoryProgressSnapshot(
            DateTimeOffset.Parse("2026-08-07T21:30:00Z"),
            DateTimeOffset.Parse("2026-08-07T21:00:00Z"),
            new string('A', 64),
            entries,
            []);
    }

    private static string VisibleText(string html)
    {
        var withoutTags = HtmlTagPattern().Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespacePattern().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();
}
