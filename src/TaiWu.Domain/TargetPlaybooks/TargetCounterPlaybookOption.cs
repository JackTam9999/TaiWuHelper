using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatEffects;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public sealed class TargetCounterPlaybookOption
{
    public TargetCounterPlaybookOption(
        CombatCounterRule counterRule,
        IEnumerable<string> conflictGroups)
    {
        CounterRule = counterRule
            ?? throw new ArgumentNullException(nameof(counterRule));
        if (!CounterRule.Effect.HasTypedMechanics)
        {
            throw new ArgumentException(
                "A playbook option requires a typed combat-counter effect.",
                nameof(counterRule));
        }

        ArgumentNullException.ThrowIfNull(conflictGroups);
        var groups = conflictGroups
            .Select(value => TargetProfileText.Code(
                value,
                nameof(conflictGroups)))
            .ToImmutableArray();
        if (groups.Distinct(StringComparer.Ordinal).Count() != groups.Length)
        {
            throw new ArgumentException(
                "Playbook option conflict groups must be unique.",
                nameof(conflictGroups));
        }

        ConflictGroups = [.. groups.Order(StringComparer.Ordinal)];
    }

    public CombatCounterRule CounterRule { get; }

    public string Code => CounterRule.Code;

    public CombatCounterStrength Strength => CounterRule.Strength;

    public CombatCounterActivationTiming ActivationTiming =>
        CounterRule.ActivationTiming;

    public CombatEffectCatalogEntry Effect => CounterRule.Effect;

    public ImmutableArray<CombatRequirement> Requirements =>
        CounterRule.Requirements;

    public ImmutableArray<string> ConflictGroups { get; }

    public ImmutableArray<string> EvidenceReferences =>
        [CounterRule.Effect.SourceReference];

    public string StableKey => Code;

    internal string ContentKey => TargetProfileText.Stable(
        Code,
        ((int)Strength).ToString(CultureInfo.InvariantCulture),
        ((int)ActivationTiming).ToString(CultureInfo.InvariantCulture),
        Effect.SkillId.ToString(CultureInfo.InvariantCulture),
        ((int)Effect.Direction).ToString(CultureInfo.InvariantCulture),
        Effect.RawEffectId.ToString(CultureInfo.InvariantCulture),
        TargetProfileText.StableCollection(ConflictGroups));
}
