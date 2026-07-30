using System.Collections.Immutable;

namespace TaiWu.Domain.CombatCounters;

public sealed class CombatCounterRuleSet
{
    public CombatCounterRuleSet(
        string gameDataVersion,
        IEnumerable<CombatCounterRule> rules)
    {
        if (string.IsNullOrWhiteSpace(gameDataVersion))
        {
            throw new ArgumentException(
                "GameData version cannot be blank.",
                nameof(gameDataVersion));
        }

        ArgumentNullException.ThrowIfNull(rules);
        Rules = [.. rules];
        if (Rules.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "Counter rules cannot contain null entries.",
                nameof(rules));
        }

        var duplicate = Rules
            .GroupBy(rule => rule.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate combat-counter rule "
                + $"'{duplicate.Key}'.",
                nameof(rules));
        }

        GameDataVersion = gameDataVersion.Trim();
    }

    public string GameDataVersion { get; }

    public ImmutableArray<CombatCounterRule> Rules { get; }
}
