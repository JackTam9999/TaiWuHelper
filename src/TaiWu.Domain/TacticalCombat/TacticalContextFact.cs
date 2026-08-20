using System.Collections.Immutable;

namespace TaiWu.Domain.TacticalCombat;

public sealed class TacticalContextFact<T>
{
    private readonly T? _value;

    private TacticalContextFact(
        TacticalContextFactState state,
        T? value,
        TacticalContextOrigin origin,
        TacticalContextAvailability availability,
        string reasonIdentity,
        IEnumerable<string> evidenceIdentities)
    {
        State = TacticalCombatText.Defined(state, nameof(state));
        Origin = TacticalCombatText.Defined(origin, nameof(origin));
        Availability = TacticalCombatText.Defined(
            availability,
            nameof(availability));
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        ArgumentNullException.ThrowIfNull(evidenceIdentities);
        var evidence = evidenceIdentities
            .Select(item => TacticalCombatText.Code(
                item,
                nameof(evidenceIdentities)))
            .ToImmutableArray();
        if (evidence.IsEmpty
            || evidence.Distinct(StringComparer.Ordinal).Count()
                != evidence.Length)
        {
            throw new ArgumentException(
                "A tactical context fact requires unique evidence identities.",
                nameof(evidenceIdentities));
        }

        EvidenceIdentities = [.. evidence.Order(StringComparer.Ordinal)];
        if (State == TacticalContextFactState.Available)
        {
            ArgumentNullException.ThrowIfNull(value);
        }

        if (State == TacticalContextFactState.Conflicting
            && EvidenceIdentities.Length < 2)
        {
            throw new ArgumentException(
                "A conflicting tactical context fact requires at least two evidence identities.",
                nameof(evidenceIdentities));
        }

        _value = value;
    }

    public TacticalContextFactState State { get; }

    public bool IsAvailable => State == TacticalContextFactState.Available;

    public T Value => IsAvailable
        ? _value!
        : throw new InvalidOperationException(
            $"Tactical context fact is {State}: {ReasonIdentity}.");

    public TacticalContextOrigin Origin { get; }

    public TacticalContextAvailability Availability { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<string> EvidenceIdentities { get; }

    public static TacticalContextFact<T> Available(
        T value,
        TacticalContextOrigin origin,
        TacticalContextAvailability availability,
        string reasonIdentity,
        params string[] evidenceIdentities) => new(
            TacticalContextFactState.Available,
            value,
            origin,
            availability,
            reasonIdentity,
            evidenceIdentities);

    public static TacticalContextFact<T> Unavailable(
        TacticalContextFactState state,
        TacticalContextOrigin origin,
        TacticalContextAvailability availability,
        string reasonIdentity,
        params string[] evidenceIdentities)
    {
        if (state == TacticalContextFactState.Available)
        {
            throw new ArgumentException(
                "Use Available to construct an available context fact.",
                nameof(state));
        }

        return new TacticalContextFact<T>(
            state,
            default,
            origin,
            availability,
            reasonIdentity,
            evidenceIdentities);
    }

    internal string SemanticKey(string valueKey) => string.Join('|',
        TacticalCombatText.EnumKey(State),
        TacticalCombatText.EnumKey(Origin),
        TacticalCombatText.EnumKey(Availability),
        ReasonIdentity,
        IsAvailable ? valueKey : "NONE",
        string.Join("||", EvidenceIdentities));
}
