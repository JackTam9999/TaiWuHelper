using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.LegendaryBooks;
using Xunit;

namespace TaiWu.Domain.UnitTests.LegendaryBooks;

public sealed class LegendaryBookEffectDefinitionTests
{
    [Fact]
    public void Definition_preserves_localized_name_description_and_sources()
    {
        var nameSource = Source("Name_83");
        var descriptionSource = Source("Desc_83");
        var definition = new LegendaryBookEffectDefinition(
            83,
            [
                new LocalizedLegendaryBookEffect(
                    CatalogueLanguage.TraditionalChinese,
                    " 解破 ",
                    " 現版效果 ",
                    nameSource,
                    descriptionSource)
            ]);

        var text = definition.Find(CatalogueLanguage.TraditionalChinese)!;
        Assert.Equal("解破", text.Name);
        Assert.Equal("現版效果", text.Description);
        Assert.Equal(nameSource, text.NameSource);
        Assert.Equal(descriptionSource, text.DescriptionSource);
    }

    [Fact]
    public void Definition_rejects_duplicate_languages()
    {
        var source = Source("Name_83");
        var text = new LocalizedLegendaryBookEffect(
            CatalogueLanguage.TraditionalChinese,
            "解破",
            description: null,
            source,
            descriptionSource: null);

        Assert.Throws<ArgumentException>(
            () => new LegendaryBookEffectDefinition(83, [text, text]));
    }

    private static CatalogueSourceReference Source(string record) => new(
        CatalogueSourceKind.TraditionalChineseLanguageResource,
        "legendary-book-slot-language-cnh:test",
        $"legendary-book-slot:{record}");
}
