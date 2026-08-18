using TaiWu.Application.Localization;
using TaiWuAPI.Localization;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class VillageWorkforceUiTextTests
{
    [Fact]
    public void Every_typed_key_has_nonblank_English_and_Chinese_text()
    {
        foreach (var key in Enum.GetValues<VillageWorkforceUiTextKey>())
        {
            Assert.False(string.IsNullOrWhiteSpace(
                VillageWorkforceUiText.Get(TaiwuLanguage.English, key)));
            Assert.False(string.IsNullOrWhiteSpace(
                VillageWorkforceUiText.Get(TaiwuLanguage.Chinese, key)));
        }
    }

    [Fact]
    public void Dynamic_labels_are_bilingual_and_reject_unknown_languages()
    {
        Assert.Equal(
            "Shop manager position 2",
            VillageWorkforceUiText.TargetLabel(
                TaiwuLanguage.English,
                2));
        Assert.Equal(
            "商鋪管理位置 2",
            VillageWorkforceUiText.TargetLabel(
                TaiwuLanguage.Chinese,
                2));
        Assert.Contains(
            "2 of 4",
            VillageWorkforceUiText.VisibleCount(
                TaiwuLanguage.English,
                2,
                4));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VillageWorkforceUiText.Get(
                (TaiwuLanguage)(-1),
                VillageWorkforceUiTextKey.PageTitle));
    }
}
