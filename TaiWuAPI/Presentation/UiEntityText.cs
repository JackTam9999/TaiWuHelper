using System.Text.RegularExpressions;

namespace TaiWuAPI.Presentation;

internal static partial class UiEntityText
{
    public static string UseNames(
        string text,
        IReadOnlyDictionary<int, string> skillNames)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(skillNames);

        var result = SkillPattern().Replace(
            text,
            match => skillNames.TryGetValue(
                int.Parse(match.Groups["id"].Value),
                out var name)
                && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : "the unnamed skill");
        result = TargetPattern().Replace(result, "the selected target");
        result = WeaponTypePattern().Replace(
            result,
            "the required weapon type");
        result = TrickTypePattern().Replace(
            result,
            "the required trick type");
        result = EffectPattern().Replace(result, "the expected effect");
        result = EquipTypePattern().Replace(result, "equip type");
        return result;
    }

    [GeneratedRegex(
        @"\bskill (?<id>\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SkillPattern();

    [GeneratedRegex(
        @"\btarget \d+\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TargetPattern();

    [GeneratedRegex(
        @"\bweapon type \d+\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WeaponTypePattern();

    [GeneratedRegex(
        @"\btrick type \d+\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TrickTypePattern();

    [GeneratedRegex(
        @"\b(?:expected )?effect \d+\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EffectPattern();

    [GeneratedRegex(
        @"\bequip type \d+\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EquipTypePattern();
}
