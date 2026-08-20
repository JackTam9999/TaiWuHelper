using System.Collections.Immutable;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalCandidateGateResult
{
    public TacticalCandidateGateResult(
        TacticalCandidateGateKind kind,
        TacticalCandidateGateState state,
        string reasonIdentity,
        IEnumerable<string> evidenceIdentities)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        State = TacticalCombatText.Defined(state, nameof(state));
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
                "A candidate gate requires unique evidence identities.",
                nameof(evidenceIdentities));
        }

        EvidenceIdentities = [.. evidence.Order(StringComparer.Ordinal)];
    }

    public TacticalCandidateGateKind Kind { get; }

    public TacticalCandidateGateState State { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<string> EvidenceIdentities { get; }

    internal string SemanticKey => string.Join('|',
        TacticalCombatText.EnumKey(Kind),
        TacticalCombatText.EnumKey(State),
        ReasonIdentity,
        string.Join("||", EvidenceIdentities));
}
