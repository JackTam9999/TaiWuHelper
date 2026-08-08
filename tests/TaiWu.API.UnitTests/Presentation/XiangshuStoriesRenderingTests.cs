using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;
using TaiWu.Application.Localization;
using TaiWuAPI.Components.Pages;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed partial class XiangshuStoriesRenderingTests
{
    [Fact]
    public async Task Chinese_page_lists_all_choices_and_consequences()
    {
        var html = await RenderPageAsync(TaiwuLanguage.Chinese);
        var text = VisibleText(html);

        Assert.Contains("相樞故事選擇與後果", text);
        Assert.Contains("已核對目前安裝版本的事件選項", text);
        Assert.Contains("選項 [2] 導向解之篇，選項 [1] 導向劫之篇", text);
        Assert.Equal(9, StoryCardPattern().Matches(html).Count);
        Assert.Contains("莫女衣 莫女", text);
        Assert.Contains("題目 「那劍我已捨身取回……你可拿到了嗎？」", text);
        Assert.Contains("選擇 [2] 我已取得了劍，將妖魔驅趕了……", text);
        Assert.Contains("選擇 [1] 鳥兒力氣太小，未能將劍送來……", text);
        Assert.Contains("「你道那嬰孩……最後被聖人救走了嗎？」", text);
        Assert.Contains("選擇 [2] 雖無聖人，嬰孩亦可得救……", text);
        Assert.Contains("選擇 [1] 天威難犯，世上更無聖人……", text);
        Assert.Contains("「醜狐……定在那片火海裡面等我……對嗎？」", text);
        Assert.Contains("選擇 [2] （點頭……）", text);
        Assert.Contains("選擇 [1] （搖頭……）", text);
        Assert.Contains("「義為何物……道在何方……？」", text);
        Assert.Contains("大道至簡，和光同塵，無心而成", text);
        Assert.Contains("心有所執，只求高義，道所不容", text);
        Assert.Contains("囚魔木 血楓 故事主角 蚩尤", text);
        Assert.Contains("敗於軒轅，促成一統，便是天命", text);
        Assert.Contains("怪力亂神，妖言惑眾，豈能有信", text);
        Assert.Contains("救命之恩、朋友之義兩全", text);
        Assert.Contains("恩與朋友之義兩者皆失", text);
        Assert.Contains("data-resolution-note=\"34\"", html);
        Assert.Contains("data-calamity-note=\"35\"", html);
        Assert.Contains("不推測未經資料驗證的數值或獎勵效果", text);
    }

    [Fact]
    public async Task English_page_uses_installed_English_story_wording()
    {
        var html = await RenderPageAsync(TaiwuLanguage.English);
        var text = VisibleText(html);

        Assert.Contains("Xiangshu story choices and outcomes", text);
        Assert.Contains("Simplicity resides within the Tao", text);
        Assert.Contains("Verified against the installed event choices", text);
        Assert.Contains("choice [2] leads to Resolution", text);
        Assert.Contains("Though there may be no sage", text);
        Assert.Equal(9, StoryCardPattern().Matches(html).Count);
    }

    private static async Task<string> RenderPageAsync(
        TaiwuLanguage language)
    {
        var services = new ServiceCollection();
        services.AddLogging();
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
                                builder.OpenComponent<XiangshuStories>(0);
                                builder.CloseComponent();
                            })
                    }));
            return output.ToHtmlString();
        });
    }

    private static string VisibleText(string html)
    {
        var withoutTags = HtmlTagPattern().Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespacePattern().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("data-xiangshu-story=\\\"")]
    private static partial Regex StoryCardPattern();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagPattern();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespacePattern();
}
