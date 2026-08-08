using TaiWu.Application.Localization;
using TaiWuAPI.Localization;
using Xunit;

namespace TaiWu.API.UnitTests.Localization;

public sealed class TaiwuLanguageStateTests
{
    [Fact]
    public void Chinese_is_the_default_website_language()
    {
        var state = new TaiwuLanguageState();

        Assert.Equal(TaiwuLanguage.Chinese, state.Current);
    }
}
