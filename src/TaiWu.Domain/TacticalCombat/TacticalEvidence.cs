using System.Collections.Immutable;
using System.Globalization;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalEvidenceReference
{
    public TacticalEvidenceReference(
        TacticalEvidenceSourceKind source,
        string evidenceIdentity,
        string gameDataVersion,
        string ruleVersion,
        string scopeIdentity)
    {
        Source = TacticalCombatText.Defined(source, nameof(source));
        EvidenceIdentity = TacticalCombatText.Code(
            evidenceIdentity,
            nameof(evidenceIdentity));
        GameDataVersion = TacticalCombatText.Stable(
            gameDataVersion,
            nameof(gameDataVersion));
        RuleVersion = TacticalCombatText.Stable(
            ruleVersion,
            nameof(ruleVersion));
        ScopeIdentity = TacticalCombatText.Code(
            scopeIdentity,
            nameof(scopeIdentity));
    }

    public TacticalEvidenceSourceKind Source { get; }

    public string EvidenceIdentity { get; }

    public string GameDataVersion { get; }

    public string RuleVersion { get; }

    public string ScopeIdentity { get; }

    internal string StableKey => string.Join('|',
        TacticalCombatText.EnumKey(Source),
        EvidenceIdentity,
        GameDataVersion,
        RuleVersion,
        ScopeIdentity);
}

public sealed record TacticalFactValue
{
    private TacticalFactValue(
        TacticalFactValueKind kind,
        string canonicalValue)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        CanonicalValue = canonicalValue;
    }

    public TacticalFactValueKind Kind { get; }

    public string CanonicalValue { get; }

    public static TacticalFactValue Boolean(bool value) =>
        new(TacticalFactValueKind.Boolean, value ? "TRUE" : "FALSE");

    public static TacticalFactValue Integer(long value) =>
        new(
            TacticalFactValueKind.Integer,
            value.ToString(CultureInfo.InvariantCulture));

    public static TacticalFactValue Code(string value) =>
        new(
            TacticalFactValueKind.Code,
            TacticalCombatText.Code(value, nameof(value)));

    internal string StableKey =>
        $"{TacticalCombatText.EnumKey(Kind)}:{CanonicalValue}";
}

public sealed record TacticalConflictValue
{
    public TacticalConflictValue(
        TacticalFactValue value,
        TacticalEvidenceReference evidence)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Evidence = evidence
            ?? throw new ArgumentNullException(nameof(evidence));
    }

    public TacticalFactValue Value { get; }

    public TacticalEvidenceReference Evidence { get; }

    internal string StableKey => $"{Value.StableKey}|{Evidence.StableKey}";
}

public sealed class TacticalStateFact
{
    public TacticalStateFact(
        TacticalFactIdentity identity,
        TacticalEvidenceState state,
        TacticalFactValue? value,
        string reasonIdentity,
        IEnumerable<TacticalEvidenceReference> evidence,
        IEnumerable<TacticalConflictValue>? conflicts = null)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        State = TacticalCombatText.Defined(state, nameof(state));
        Value = value;
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "fact evidence",
            nameof(evidence));
        Conflicts = TacticalCombatText.CopyUnique(
            conflicts ?? [],
            item => item.StableKey,
            "fact conflict",
            nameof(conflicts));
        ValidateInvariant();
    }

    public TacticalFactIdentity Identity { get; }

    public TacticalEvidenceState State { get; }

    public TacticalFactValue? Value { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    public ImmutableArray<TacticalConflictValue> Conflicts { get; }

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(State),
        Value?.StableKey ?? "NONE",
        ReasonIdentity,
        string.Join("||", Evidence.Select(item => item.StableKey)),
        string.Join("||", Conflicts.Select(item => item.StableKey)));

    internal IEnumerable<TacticalEvidenceReference> AllEvidence =>
        Evidence.Concat(Conflicts.Select(item => item.Evidence));

    private void ValidateInvariant()
    {
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical state fact requires evidence.",
                nameof(Evidence));
        }

        if (State == TacticalEvidenceState.Available)
        {
            if (Value is null || !Conflicts.IsEmpty)
            {
                throw new ArgumentException(
                    "An available tactical fact requires one value and no conflicts.");
            }

            return;
        }

        if (Value is not null)
        {
            throw new ArgumentException(
                "A non-available tactical fact cannot select a value.",
                nameof(Value));
        }

        if (State == TacticalEvidenceState.Conflicting)
        {
            if (Conflicts.Length < 2
                || Conflicts.Select(item => item.Value.StableKey)
                    .Distinct(StringComparer.Ordinal)
                    .Count() < 2)
            {
                throw new ArgumentException(
                    "A conflicting tactical fact requires at least two distinct values.",
                    nameof(Conflicts));
            }
        }
        else if (!Conflicts.IsEmpty)
        {
            throw new ArgumentException(
                "Only a conflicting tactical fact can retain conflict values.",
                nameof(Conflicts));
        }
    }
}
