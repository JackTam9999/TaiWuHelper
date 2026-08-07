using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.LegendaryBooks;

namespace TaiWu.Infrastructure.Catalogue;

internal static class LegendaryBookEffectDefinitionMapper
{
    internal static IReadOnlyList<LegendaryBookEffectDefinition> Map(
        TaiwuLanguageCatalog traditionalChinese,
        TaiwuLanguageCatalog english,
        string traditionalChineseSourceIdentity,
        string englishSourceIdentity)
    {
        ArgumentNullException.ThrowIfNull(traditionalChinese);
        ArgumentNullException.ThrowIfNull(english);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            traditionalChineseSourceIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(englishSourceIdentity);

        var effectIds = traditionalChinese.Keys
            .Concat(english.Keys)
            .Select(TryReadEffectId)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .Order()
            .ToArray();

        List<LegendaryBookEffectDefinition> definitions = [];
        foreach (var effectId in effectIds)
        {
            List<LocalizedLegendaryBookEffect> localizations = [];
            AddLocalization(
                localizations,
                effectId,
                CatalogueLanguage.TraditionalChinese,
                traditionalChinese,
                traditionalChineseSourceIdentity,
                CatalogueSourceKind.TraditionalChineseLanguageResource);
            AddLocalization(
                localizations,
                effectId,
                CatalogueLanguage.English,
                english,
                englishSourceIdentity,
                CatalogueSourceKind.EnglishLanguageResource);
            if (localizations.Count > 0)
            {
                definitions.Add(new LegendaryBookEffectDefinition(
                    effectId,
                    localizations));
            }
        }

        return definitions;
    }

    private static int? TryReadEffectId(string key)
    {
        const string namePrefix = "Name_";
        const string descriptionPrefix = "Desc_";
        var value = key.StartsWith(namePrefix, StringComparison.Ordinal)
            ? key[namePrefix.Length..]
            : key.StartsWith(descriptionPrefix, StringComparison.Ordinal)
                ? key[descriptionPrefix.Length..]
                : null;
        return int.TryParse(
            value,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out var effectId)
            && effectId >= 0
                ? effectId
                : null;
    }

    private static void AddLocalization(
        ICollection<LocalizedLegendaryBookEffect> localizations,
        int effectId,
        CatalogueLanguage language,
        TaiwuLanguageCatalog catalog,
        string sourceIdentity,
        CatalogueSourceKind sourceKind)
    {
        var nameKey = $"Name_{effectId}";
        var descriptionKey = $"Desc_{effectId}";
        var name = catalog.Find(nameKey);
        var description = catalog.Find(descriptionKey);
        if (name is null && description is null)
        {
            return;
        }

        localizations.Add(new LocalizedLegendaryBookEffect(
            language,
            name,
            description,
            name is null
                ? null
                : Source(sourceKind, sourceIdentity, nameKey),
            description is null
                ? null
                : Source(sourceKind, sourceIdentity, descriptionKey)));
    }

    private static CatalogueSourceReference Source(
        CatalogueSourceKind kind,
        string sourceIdentity,
        string key) => new(
            kind,
            sourceIdentity,
            $"legendary-book-slot:{key}");
}
