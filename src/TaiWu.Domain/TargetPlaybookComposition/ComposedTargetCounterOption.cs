using System.Collections.Immutable;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed class ComposedTargetCounterOption
{
    internal ComposedTargetCounterOption(
        CombatCounterRule counterRule,
        IEnumerable<string> sourcePlaybookKeys,
        IEnumerable<string> sourceGoalCodes,
        IEnumerable<string> conflictGroups)
    {
        CounterRule = counterRule
            ?? throw new ArgumentNullException(nameof(counterRule));
        SourcePlaybookKeys = CopyReferences(
            sourcePlaybookKeys,
            nameof(sourcePlaybookKeys));
        SourceGoalCodes = CopyCodes(
            sourceGoalCodes,
            nameof(sourceGoalCodes));
        ConflictGroups = CopyCodes(
            conflictGroups,
            nameof(conflictGroups),
            requireValue: false);
    }

    public CombatCounterRule CounterRule { get; }

    public string StableKey => CounterRule.Code;

    public CombatCounterStrength Strength => CounterRule.Strength;

    public CombatCounterActivationTiming ActivationTiming =>
        CounterRule.ActivationTiming;

    public CombatEffectCatalogEntry Effect => CounterRule.Effect;

    public ImmutableArray<CombatRequirement> Requirements =>
        CounterRule.Requirements;

    public ImmutableArray<string> SourcePlaybookKeys { get; }

    public ImmutableArray<string> SourceGoalCodes { get; }

    public ImmutableArray<string> ConflictGroups { get; }

    internal string ContentKey => TargetProfileText.Stable(
        StableKey,
        TargetProfileText.StableCollection(SourcePlaybookKeys),
        TargetProfileText.StableCollection(SourceGoalCodes),
        TargetProfileText.StableCollection(ConflictGroups));

    private static ImmutableArray<string> CopyCodes(
        IEnumerable<string> values,
        string parameterName,
        bool requireValue = true)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var codes = values
            .Select(value => TargetProfileText.Code(value, parameterName))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (requireValue && codes.Length == 0)
        {
            throw new ArgumentException(
                "A composed option requires source identity.",
                parameterName);
        }

        return codes;
    }

    private static ImmutableArray<string> CopyReferences(
        IEnumerable<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var references = values
            .Select(value =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    value,
                    parameterName);
                return value.Trim();
            })
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToImmutableArray();
        if (references.Length == 0)
        {
            throw new ArgumentException(
                "A composed option requires a source playbook.",
                parameterName);
        }

        return references;
    }
}
