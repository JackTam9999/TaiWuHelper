using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWuAPI.Localization;
using Xunit;

namespace TaiWu.API.UnitTests.Localization;

public sealed class CombatSkillUiTextTests
{
    [Theory]
    [InlineData(CombatSkillElement.Metal, "金剛", "Metal Qi")]
    [InlineData(CombatSkillElement.Wood, "紫霞", "Wood Qi")]
    [InlineData(CombatSkillElement.Water, "玄陰", "Water Qi")]
    [InlineData(CombatSkillElement.Fire, "純陽", "Fire Qi")]
    [InlineData(CombatSkillElement.Earth, "歸元", "Earth Qi")]
    [InlineData(CombatSkillElement.Mixed, "混元", "Hunyuan Qi")]
    public void Element_labels_follow_the_active_language(
        CombatSkillElement element,
        string chinese,
        string english)
    {
        Assert.Equal(
            chinese,
            CombatSkillUiText.Element(TaiwuLanguage.Chinese, element));
        Assert.Equal(
            english,
            CombatSkillUiText.Element(TaiwuLanguage.English, element));
    }

    [Theory]
    [InlineData(CombatSkillFactionAlignment.Just, "剛正", "Principled")]
    [InlineData(CombatSkillFactionAlignment.Kind, "仁善", "Benevolent")]
    [InlineData(CombatSkillFactionAlignment.Even, "中庸", "Moderate")]
    [InlineData(CombatSkillFactionAlignment.Rebel, "叛逆", "Rebellious")]
    [InlineData(CombatSkillFactionAlignment.Egoistic, "唯我", "Egocentric")]
    public void Alignment_labels_follow_the_active_language(
        CombatSkillFactionAlignment alignment,
        string chinese,
        string english)
    {
        Assert.Equal(
            chinese,
            CombatSkillUiText.Alignment(TaiwuLanguage.Chinese, alignment));
        Assert.Equal(
            english,
            CombatSkillUiText.Alignment(TaiwuLanguage.English, alignment));
    }
}
