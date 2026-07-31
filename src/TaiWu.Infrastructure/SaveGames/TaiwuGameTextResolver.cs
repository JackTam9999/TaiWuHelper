using GameData.Domains;
using GameData.Domains.Character;
using System.Collections.Concurrent;
using TaiWu.Application.Localization;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuGameTextResolver
{
    private readonly ConcurrentDictionary<
        string,
        IReadOnlyDictionary<string, string>> _catalogs =
        new(StringComparer.OrdinalIgnoreCase);

    public TaiwuGameTextContext CreateContext(
        string saveFilePath,
        TaiwuLanguage language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(saveFilePath);
        return new TaiwuGameTextContext(
            this,
            FindLanguageDirectory(saveFilePath, language),
            language);
    }

    internal string Resolve(
        string languageDirectory,
        string pack,
        string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var catalogPath = Path.Combine(
            languageDirectory,
            $"{pack}_language.txt");
        var catalog = _catalogs.GetOrAdd(
            catalogPath,
            LoadCatalog);
        return catalog.GetValueOrDefault(key, key);
    }

    private static string FindLanguageDirectory(
        string saveFilePath,
        TaiwuLanguage language)
    {
        var directory = new FileInfo(
            Path.GetFullPath(saveFilePath)).Directory;
        while (directory is not null
               && !string.Equals(
                   directory.Name,
                   "SaveGames",
                   StringComparison.OrdinalIgnoreCase))
        {
            directory = directory.Parent;
        }

        var gameDirectory = directory?.Parent
            ?? throw new InvalidDataException(
                "The configured save must be located below a SaveGames "
                + "directory so the installed game language files can be "
                + "located.");
        var languageFolder = language switch
        {
            TaiwuLanguage.English => "Language_EN",
            TaiwuLanguage.Chinese => "Language_CN",
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.")
        };
        return Path.Combine(
            gameDirectory.FullName,
            "The Scroll of Taiwu_Data",
            "StreamingAssets",
            languageFolder);
    }

    private static IReadOnlyDictionary<string, string> LoadCatalog(
        string catalogPath)
    {
        if (!File.Exists(catalogPath))
        {
            return new Dictionary<string, string>(
                StringComparer.Ordinal);
        }

        Dictionary<string, string> values =
            new(StringComparer.Ordinal);
        using var lines = File.ReadLines(catalogPath).GetEnumerator();
        while (lines.MoveNext())
        {
            var key = lines.Current.TrimStart('\uFEFF');
            if (!lines.MoveNext())
            {
                break;
            }

            var value = lines.Current;
            if (!string.IsNullOrWhiteSpace(key)
                && !string.IsNullOrWhiteSpace(value))
            {
                values.TryAdd(key, value);
            }
        }

        return values;
    }
}

internal sealed class TaiwuGameTextContext(
    TaiwuGameTextResolver resolver,
    string languageDirectory,
    TaiwuLanguage language)
{
    public string Resolve(string pack, string? key) =>
        resolver.Resolve(languageDirectory, pack, key);

    public string ResolveCharacterName(Character character)
    {
        ArgumentNullException.ThrowIfNull(character);

        var fullName = character.GetFullName();
        var customTexts = DomainManager.World.GetCustomTexts();
        var (surname, givenName) = fullName.GetName(
            character.GetGender(),
            customTexts);
        var separator = language == TaiwuLanguage.English ? " " : string.Empty;
        var resolvedSurname = ResolveNameParts(surname, separator);
        var resolvedGivenName = ResolveNameParts(givenName, separator);
        return string.Join(
            separator,
            new[] { resolvedSurname, resolvedGivenName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    internal string ResolveNameParts(string source, string separator)
    {
        var exact = Resolve("Name", source);
        if (!string.Equals(exact, source, StringComparison.Ordinal))
        {
            return exact;
        }

        var starts = NamePartStarts(source);
        if (starts.Count <= 1)
        {
            return source;
        }

        List<string> parts = [];
        for (var index = 0; index < starts.Count; index++)
        {
            var start = starts[index];
            var length = index + 1 < starts.Count
                ? starts[index + 1] - start
                : source.Length - start;
            parts.Add(Resolve("Name", source.Substring(start, length)));
        }

        return string.Join(separator, parts);
    }

    private static List<int> NamePartStarts(string source)
    {
        List<int> starts = [];
        var start = 0;
        while (start < source.Length)
        {
            var match = source.IndexOf(
                "Name_",
                start,
                StringComparison.Ordinal);
            if (match < 0)
            {
                break;
            }

            starts.Add(match);
            start = match + "Name_".Length;
        }

        return starts;
    }
}
