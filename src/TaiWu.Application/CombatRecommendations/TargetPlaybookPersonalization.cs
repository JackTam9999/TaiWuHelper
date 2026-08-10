using System.Collections.Immutable;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;

namespace TaiWu.Application.CombatRecommendations;

public sealed record TargetPlaybookPersonalization
{
    internal TargetPlaybookPersonalization(
        TargetCombatProfileAnalysis analysis,
        TargetPlaybookComposition composition,
        TargetPlaybookAdjustmentSet adjustments,
        IEnumerable<ComposedTargetResponseGoal> eligibleGoals,
        IEnumerable<TargetPlaybookCounterAvailability> counters)
    {
        Analysis = analysis
            ?? throw new ArgumentNullException(nameof(analysis));
        Composition = composition
            ?? throw new ArgumentNullException(nameof(composition));
        Adjustments = adjustments
            ?? throw new ArgumentNullException(nameof(adjustments));
        if (!string.Equals(
                Analysis.Profile.Fingerprint,
                Composition.ProfileFingerprint,
                StringComparison.Ordinal)
            || !string.Equals(
                Analysis.ArchetypeMatches.StableKey,
                Composition.MatchSetKey,
                StringComparison.Ordinal)
            || !string.Equals(
                Analysis.Profile.Fingerprint,
                Adjustments.ProfileFingerprint,
                StringComparison.Ordinal)
            || !string.Equals(
                Composition.StableKey,
                Adjustments.CompositionKey,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A personalization result requires one exact profile, "
                + "match set, composition, and adjustment chain.");
        }

        ArgumentNullException.ThrowIfNull(eligibleGoals);
        ArgumentNullException.ThrowIfNull(counters);

        var goalValues = eligibleGoals.ToArray();
        var counterValues = counters.ToArray();
        var composedGoalKeys = Composition.Goals
            .Select(value => value.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        var composedOptionKeys = Composition.Options
            .Select(value => value.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        if (goalValues.Any(value => value is null
                || !composedGoalKeys.Contains(value.StableKey))
            || counterValues.Any(value => value is null
                || !composedOptionKeys.Contains(value.Option.StableKey)))
        {
            throw new ArgumentException(
                "Eligible goals and counters must belong to the exact "
                + "composition.");
        }

        EligibleGoals =
        [
            .. goalValues
                .DistinctBy(value => value.StableKey, StringComparer.Ordinal)
                .OrderBy(value => value.Sequence)
                .ThenBy(value => value.Priority)
                .ThenBy(value => value.StableKey, StringComparer.Ordinal)
        ];
        Counters =
        [
            .. counterValues
                .OrderByDescending(value => value.Option.Strength)
                .ThenBy(value => value.Option.StableKey,
                    StringComparer.Ordinal)
        ];
        Gaps =
        [
            .. Composition.KnownGaps
                .Concat(Counters
                    .Where(value => value.Gap is not null)
                    .Select(value => value.Gap!))
                .DistinctBy(value => value.StableKey, StringComparer.Ordinal)
                .OrderBy(value => value.StableKey, StringComparer.Ordinal)
        ];
    }

    public TargetCombatProfileAnalysis Analysis { get; }

    public TargetPlaybookComposition Composition { get; }

    public TargetPlaybookAdjustmentSet Adjustments { get; }

    public ImmutableArray<ComposedTargetResponseGoal> EligibleGoals { get; }

    public ImmutableArray<TargetPlaybookCounterAvailability> Counters
    { get; }

    public ImmutableArray<TargetCounterPlaybookGap> Gaps { get; }
}
