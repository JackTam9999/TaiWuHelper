using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSkills;

public enum CatalogueLanguage
{
    TraditionalChinese = 0,
    English = 1
}

public sealed record LocalizedCombatSkillName
{
    public LocalizedCombatSkillName(
        CatalogueLanguage language,
        string text,
        CatalogueSourceReference source)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "A localized combat-skill name cannot be blank.",
                nameof(text));
        }

        ArgumentNullException.ThrowIfNull(source);
        Language = language;
        Text = text.Trim();
        Source = source;
    }

    public CatalogueLanguage Language { get; }

    public string Text { get; }

    public CatalogueSourceReference Source { get; }
}

public sealed record CombatSkillLocalizedNames
{
    public CombatSkillLocalizedNames(
        IEnumerable<LocalizedCombatSkillName>? names = null)
    {
        var values = (names ?? []).ToImmutableArray();
        if (values.Any(name => name is null))
        {
            throw new ArgumentException(
                "Localized name entries cannot contain null.",
                nameof(names));
        }

        var duplicate = values
            .GroupBy(name => name.Language)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"More than one {duplicate.Key} name was supplied.",
                nameof(names));
        }

        Values = values;
    }

    public ImmutableArray<LocalizedCombatSkillName> Values { get; }

    public CatalogueField<LocalizedCombatSkillName> Get(
        CatalogueLanguage language)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.");
        }

        var value = Values.FirstOrDefault(name => name.Language == language);
        return value is null
            ? CatalogueField<LocalizedCombatSkillName>.Unavailable(
                $"No {language} name is available.")
            : CatalogueField<LocalizedCombatSkillName>.Available(
                value,
                value.Source);
    }

    public CatalogueField<LocalizedCombatSkillName> Resolve(
        CatalogueLanguage preferredLanguage)
    {
        var preferred = Get(preferredLanguage);
        if (preferred.IsAvailable)
        {
            return preferred;
        }

        var fallback = preferredLanguage == CatalogueLanguage.TraditionalChinese
            ? CatalogueLanguage.English
            : CatalogueLanguage.TraditionalChinese;
        var resolved = Get(fallback);
        return resolved.IsAvailable
            ? resolved
            : CatalogueField<LocalizedCombatSkillName>.Unavailable(
                "No Traditional Chinese or English name is available.");
    }
}
