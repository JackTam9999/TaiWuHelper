using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybooks;

namespace TaiWu.Domain.TargetPlaybookComposition;

public static class TargetPlaybookComposer
{
    public const string MatchNotConfirmedCode =
        "ARCHETYPE_MATCH_NOT_CONFIRMED";

    public const string PlaybookUnavailableCode =
        "PLAYBOOK_UNAVAILABLE";

    public static TargetPlaybookComposition Compose(
        TargetArchetypeMatchSet matchSet,
        TargetCounterPlaybookCatalog catalog,
        string observedGameDataVersion)
    {
        ArgumentNullException.ThrowIfNull(matchSet);
        ArgumentNullException.ThrowIfNull(catalog);
        List<TargetCounterPlaybook> playbooks = [];
        List<TargetPlaybookCompositionDiagnostic> diagnostics = [];

        foreach (var match in matchSet.Matches)
        {
            if (match.State != TargetArchetypeMatchState.Matched)
            {
                diagnostics.Add(new TargetPlaybookCompositionDiagnostic(
                    MatchNotConfirmedCode,
                    match.Definition.Identity,
                    match.State));
                continue;
            }

            var resolution = catalog.Resolve(
                observedGameDataVersion,
                match.Definition.Identity);
            if (!resolution.IsResolved)
            {
                diagnostics.Add(new TargetPlaybookCompositionDiagnostic(
                    PlaybookUnavailableCode,
                    match.Definition.Identity,
                    resolutionStatus: resolution.Status));
                continue;
            }

            playbooks.Add(resolution.Playbook!);
        }

        var uniquePlaybooks = playbooks
            .DistinctBy(playbook => playbook.StableKey, StringComparer.Ordinal)
            .OrderBy(playbook => playbook.StableKey, StringComparer.Ordinal)
            .ToArray();
        var goals = ComposeGoals(uniquePlaybooks);
        var conflicts = FindConflicts(goals);
        return new TargetPlaybookComposition(
            matchSet.ProfileFingerprint,
            matchSet.StableKey,
            uniquePlaybooks,
            goals,
            conflicts,
            diagnostics);
    }

    internal static int TimingOrder(CombatCounterActivationTiming timing) =>
        timing switch
        {
            CombatCounterActivationTiming.CombatStartPassive => 0,
            CombatCounterActivationTiming.EquippedPassive => 1,
            CombatCounterActivationTiming.ActiveDefense => 2,
            CombatCounterActivationTiming.ActiveAgility => 3,
            CombatCounterActivationTiming.ActiveAttack => 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(timing),
                timing,
                "Unknown response timing.")
        };

    private static ComposedTargetResponseGoal[] ComposeGoals(
        TargetCounterPlaybook[] playbooks)
    {
        return
        [
            .. playbooks
                .SelectMany(playbook => playbook.Goals.Select(goal =>
                    new GoalContribution(playbook, goal)))
                .GroupBy(
                    contribution => contribution.Goal.StableKey,
                    StringComparer.Ordinal)
                .Select(ComposeGoal)
                .OrderBy(goal => goal.Sequence)
                .ThenBy(goal => goal.Priority)
                .ThenBy(goal => goal.Code, StringComparer.Ordinal)
        ];
    }

    private static ComposedTargetResponseGoal ComposeGoal(
        IGrouping<string, GoalContribution> contributions)
    {
        var values = contributions.ToArray();
        var options = values
            .SelectMany(value => value.Goal.Options.Select(option =>
                new OptionContribution(
                    value.Playbook.StableKey,
                    value.Goal.Code,
                    option)))
            .GroupBy(value => value.Option.StableKey, StringComparer.Ordinal)
            .Select(group => new ComposedTargetCounterOption(
                group.First().Option.CounterRule,
                group.Select(value => value.PlaybookKey),
                group.Select(value => value.GoalCode),
                group.SelectMany(value => value.Option.ConflictGroups)))
            .ToArray();

        return new ComposedTargetResponseGoal(
            contributions.Key,
            values.Min(value => value.Goal.Sequence),
            values.Min(value => value.Goal.Priority),
            values
                .Select(value => value.Goal.ResponseTiming)
                .MinBy(TimingOrder),
            values.Select(value => value.Playbook.StableKey),
            values.SelectMany(value => value.Goal.ProfileFacets),
            values.SelectMany(value => value.Goal.Threats),
            options,
            values.SelectMany(value => value.Goal.ConflictGroups),
            values.SelectMany(value => value.Goal.EvidenceReferences),
            values.SelectMany(value => value.Goal.KnownGaps));
    }

    private static TargetPlaybookCompositionConflict[] FindConflicts(
        ComposedTargetResponseGoal[] goals)
    {
        List<TargetPlaybookCompositionConflict> conflicts = [];
        foreach (var group in goals
                     .SelectMany(goal => goal.ConflictGroups.Select(
                         conflictGroup => (Goal: goal, Group: conflictGroup)))
                     .GroupBy(value => value.Group, StringComparer.Ordinal))
        {
            var groupedGoals = group
                .Select(value => value.Goal)
                .DistinctBy(goal => goal.StableKey, StringComparer.Ordinal)
                .ToArray();
            if (groupedGoals.Length > 1)
            {
                conflicts.Add(new TargetPlaybookCompositionConflict(
                    ConflictKind(group.Key),
                    group.Key,
                    groupedGoals.Select(goal => goal.Code),
                    optionCodes: []));
            }
        }

        for (var firstIndex = 0; firstIndex < goals.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1;
                 secondIndex < goals.Length;
                 secondIndex++)
            {
                AddOptionConflicts(
                    goals[firstIndex],
                    goals[secondIndex],
                    conflicts);
            }
        }

        return
        [
            .. conflicts
                .DistinctBy(
                    conflict => conflict.StableKey,
                    StringComparer.Ordinal)
                .OrderBy(
                    conflict => conflict.StableKey,
                    StringComparer.Ordinal)
        ];
    }

    private static void AddOptionConflicts(
        ComposedTargetResponseGoal first,
        ComposedTargetResponseGoal second,
        ICollection<TargetPlaybookCompositionConflict> conflicts)
    {
        var firstGroups = OptionsByConflictGroup(first);
        var secondGroups = OptionsByConflictGroup(second);
        foreach (var group in firstGroups.Keys
                     .Intersect(secondGroups.Keys, StringComparer.Ordinal)
                     .Order(StringComparer.Ordinal))
        {
            var firstOptions = firstGroups[group];
            var secondOptions = secondGroups[group];
            if (firstOptions.Overlaps(secondOptions))
            {
                continue;
            }

            conflicts.Add(new TargetPlaybookCompositionConflict(
                ConflictKind(group),
                group,
                [first.Code, second.Code],
                firstOptions.Concat(secondOptions)));
        }
    }

    private static Dictionary<string, HashSet<string>> OptionsByConflictGroup(
        ComposedTargetResponseGoal goal)
    {
        Dictionary<string, HashSet<string>> result =
            new(StringComparer.Ordinal);
        foreach (var option in goal.Options)
        {
            foreach (var group in option.ConflictGroups)
            {
                if (!result.TryGetValue(group, out var options))
                {
                    options = new HashSet<string>(StringComparer.Ordinal);
                    result[group] = options;
                }

                options.Add(option.StableKey);
            }
        }

        return result;
    }

    private static TargetPlaybookCompositionConflictKind ConflictKind(
        string group)
    {
        if (group.StartsWith("ACTIVE_", StringComparison.Ordinal))
        {
            return TargetPlaybookCompositionConflictKind.ActiveRole;
        }

        if (group.StartsWith("TIMING_", StringComparison.Ordinal))
        {
            return TargetPlaybookCompositionConflictKind.Timing;
        }

        if (group.StartsWith("CAPACITY_", StringComparison.Ordinal)
            || group.StartsWith("SLOT_", StringComparison.Ordinal))
        {
            return TargetPlaybookCompositionConflictKind.Capacity;
        }

        return TargetPlaybookCompositionConflictKind.Requirement;
    }

    private sealed record GoalContribution(
        TargetCounterPlaybook Playbook,
        TargetCounterPlaybookGoal Goal);

    private sealed record OptionContribution(
        string PlaybookKey,
        string GoalCode,
        TargetCounterPlaybookOption Option);
}
