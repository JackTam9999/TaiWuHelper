using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;

namespace TaiWu.Domain.LegendaryBooks;

public sealed record LocalizedLegendaryBookEffect
{
    public LocalizedLegendaryBookEffect(
        CatalogueLanguage language,
        string? name,
        string? description,
        CatalogueSourceReference? nameSource,
        CatalogueSourceReference? descriptionSource)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.");
        }

        Name = NormalizeOptional(name);
        Description = NormalizeOptional(description);
        if (Name is null && Description is null)
        {
            throw new ArgumentException(
                "A localized legendary-book effect requires a name or description.",
                nameof(name));
        }

        if ((Name is null) != (nameSource is null))
        {
            throw new ArgumentException(
                "A legendary-book effect name and its source must be supplied together.",
                nameof(nameSource));
        }

        if ((Description is null) != (descriptionSource is null))
        {
            throw new ArgumentException(
                "A legendary-book effect description and its source must be supplied together.",
                nameof(descriptionSource));
        }

        Language = language;
        NameSource = nameSource;
        DescriptionSource = descriptionSource;
    }

    public CatalogueLanguage Language { get; }

    public string? Name { get; }

    public string? Description { get; }

    public CatalogueSourceReference? NameSource { get; }

    public CatalogueSourceReference? DescriptionSource { get; }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record LegendaryBookEffectDefinition
{
    public LegendaryBookEffectDefinition(
        int effectId,
        IEnumerable<LocalizedLegendaryBookEffect> localizations)
    {
        if (effectId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(effectId),
                effectId,
                "A legendary-book effect ID cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(localizations);
        var values = localizations.ToImmutableArray();
        if (values.Length == 0)
        {
            throw new ArgumentException(
                "A legendary-book effect requires localized text.",
                nameof(localizations));
        }

        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Legendary-book effect localizations cannot contain null.",
                nameof(localizations));
        }

        if (values.GroupBy(value => value.Language)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "A legendary-book effect cannot contain duplicate languages.",
                nameof(localizations));
        }

        EffectId = effectId;
        Localizations = values
            .OrderBy(value => value.Language)
            .ToImmutableArray();
    }

    public int EffectId { get; }

    public ImmutableArray<LocalizedLegendaryBookEffect> Localizations { get; }

    public LocalizedLegendaryBookEffect? Find(CatalogueLanguage language)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.");
        }

        return Localizations.FirstOrDefault(value => value.Language == language);
    }
}
