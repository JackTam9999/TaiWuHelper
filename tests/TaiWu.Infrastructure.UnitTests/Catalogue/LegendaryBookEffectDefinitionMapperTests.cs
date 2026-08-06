using TaiWu.Domain.CombatSkills;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class LegendaryBookEffectDefinitionMapperTests
{
    [Fact]
    public void Maps_current_ui_keys_with_field_level_provenance()
    {
        var traditionalChinese = new TaiwuLanguageCatalog(
            new Dictionary<string, string>
            {
                ["Name_83"] = "解破",
                ["Desc_83"] = "打斷敵人的功法施展；",
                ["Unrelated_83"] = "ignored"
            });
        var english = new TaiwuLanguageCatalog(
            new Dictionary<string, string>
            {
                ["Name_83"] = "Counter Break",
                ["Desc_83"] = "Interrupt the enemy skill."
            });

        var result = LegendaryBookEffectDefinitionMapper.Map(
            traditionalChinese,
            english,
            "legendary-book-slot-language-cnh:ABC",
            "legendary-book-slot-language-en:DEF");

        var effect = Assert.Single(result);
        Assert.Equal(83, effect.EffectId);
        var cnh = effect.Find(CatalogueLanguage.TraditionalChinese)!;
        Assert.Equal("解破", cnh.Name);
        Assert.Equal("打斷敵人的功法施展；", cnh.Description);
        Assert.Equal(
            "legendary-book-slot:Name_83",
            cnh.NameSource!.RecordIdentity);
        Assert.Equal(
            "legendary-book-slot:Desc_83",
            cnh.DescriptionSource!.RecordIdentity);
        Assert.Equal(
            "Counter Break",
            effect.Find(CatalogueLanguage.English)!.Name);
    }

    [Fact]
    public void Keeps_partial_localizations_and_orders_effect_ids()
    {
        var traditionalChinese = new TaiwuLanguageCatalog(
            new Dictionary<string, string>
            {
                ["Desc_9"] = "Only a description",
                ["Name_2"] = "Only a name",
                ["Name_bad"] = "ignored",
                ["Desc_-1"] = "ignored"
            });

        var result = LegendaryBookEffectDefinitionMapper.Map(
            traditionalChinese,
            new TaiwuLanguageCatalog(),
            "cnh:test",
            "en:test");

        Assert.Equal([2, 9], result.Select(value => value.EffectId));
        Assert.Null(result[0].Localizations[0].Description);
        Assert.Null(result[1].Localizations[0].Name);
    }
}
