using System.Collections.Immutable;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetPlaybooks;

namespace TaiWu.Application.CombatRecommendations;

public enum TargetPlaybookCounterAvailabilityState
{
    Feasible,
    Inaccessible,
    Infeasible,
    Unresolved
}

public sealed record TargetPlaybookCounterAvailability
{
    internal TargetPlaybookCounterAvailability(
        ComposedTargetCounterOption option,
        CombatCounterAccessEvaluation access,
        TargetPlaybookCounterAvailabilityState state,
        TargetCounterPlaybookGap? gap,
        IEnumerable<CombatLoadoutGenerationDiagnostic> diagnostics)
    {
        Option = option ?? throw new ArgumentNullException(nameof(option));
        Access = access ?? throw new ArgumentNullException(nameof(access));
        if (!ReferenceEquals(Option.CounterRule, Access.Rule))
        {
            throw new ArgumentException(
                "Counter availability must use the composed option's exact "
                + "verified rule.",
                nameof(access));
        }

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if ((state == TargetPlaybookCounterAvailabilityState.Inaccessible)
            == access.IsAccessible)
        {
            throw new ArgumentException(
                "Only a failed access evaluation can be inaccessible.",
                nameof(state));
        }

        if ((state == TargetPlaybookCounterAvailabilityState.Feasible)
            == (gap is not null))
        {
            throw new ArgumentException(
                "Only unavailable counters can have a playbook gap.",
                nameof(gap));
        }

        ArgumentNullException.ThrowIfNull(diagnostics);
        var diagnosticValues = diagnostics.ToImmutableArray();
        if (diagnosticValues.Any(value => value is null))
        {
            throw new ArgumentException(
                "Counter diagnostics cannot contain null entries.",
                nameof(diagnostics));
        }

        State = state;
        Gap = gap;
        Diagnostics = diagnosticValues;
    }

    public ComposedTargetCounterOption Option { get; }

    public CombatCounterAccessEvaluation Access { get; }

    public TargetPlaybookCounterAvailabilityState State { get; }

    public TargetCounterPlaybookGap? Gap { get; }

    public ImmutableArray<CombatLoadoutGenerationDiagnostic> Diagnostics
    { get; }

    public bool IsFeasible =>
        State == TargetPlaybookCounterAvailabilityState.Feasible;
}
