using System.Collections.Immutable;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalRequirementDefinition
{
    public TacticalRequirementDefinition(
        TacticalRequirementIdentity identity,
        TacticalFactIdentity fact,
        TacticalRequirementOperator @operator,
        TacticalFactValue? expectedValue)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Fact = fact ?? throw new ArgumentNullException(nameof(fact));
        Operator = TacticalCombatText.Defined(@operator, nameof(@operator));
        ExpectedValue = expectedValue;

        var requiresValue = Operator is TacticalRequirementOperator.Equal
            or TacticalRequirementOperator.AtLeast
            or TacticalRequirementOperator.AtMost;
        if (requiresValue != (ExpectedValue is not null))
        {
            throw new ArgumentException(
                "Equal and range requirements require a value; presence requirements forbid one.",
                nameof(expectedValue));
        }

        if (Operator is TacticalRequirementOperator.AtLeast
                or TacticalRequirementOperator.AtMost
            && ExpectedValue?.Kind != TacticalFactValueKind.Integer)
        {
            throw new ArgumentException(
                "Range requirements require an integer value.",
                nameof(expectedValue));
        }
    }

    public TacticalRequirementIdentity Identity { get; }

    public TacticalFactIdentity Fact { get; }

    public TacticalRequirementOperator Operator { get; }

    public TacticalFactValue? ExpectedValue { get; }

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        Fact.StableKey,
        TacticalCombatText.EnumKey(Operator),
        ExpectedValue?.StableKey ?? "NONE");
}

public sealed class TacticalRequirementEvaluation
{
    public TacticalRequirementEvaluation(
        TacticalRequirementIdentity requirement,
        TacticalRequirementOutcome outcome,
        string reasonIdentity,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Requirement = requirement
            ?? throw new ArgumentNullException(nameof(requirement));
        Outcome = TacticalCombatText.Defined(outcome, nameof(outcome));
        ReasonIdentity = TacticalCombatText.Code(
            reasonIdentity,
            nameof(reasonIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "requirement evidence",
            nameof(evidence));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical requirement evaluation requires evidence.",
                nameof(evidence));
        }

        if (Outcome == TacticalRequirementOutcome.Conflicting
            && Evidence.Length < 2)
        {
            throw new ArgumentException(
                "A conflicting requirement requires at least two evidence sources.",
                nameof(evidence));
        }
    }

    public TacticalRequirementIdentity Requirement { get; }

    public TacticalRequirementOutcome Outcome { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => Requirement.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(Outcome),
        ReasonIdentity,
        string.Join("||", Evidence.Select(item => item.StableKey)));
}

public sealed class TacticalTransition
{
    public TacticalTransition(
        TacticalTransitionIdentity identity,
        IEnumerable<TacticalRequirementIdentity> preconditions,
        IEnumerable<TacticalFactIdentity> resultingFacts,
        TacticalTransitionTiming timing,
        string expectedPurposeIdentity,
        string limitationIdentity,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Preconditions = TacticalCombatText.CopyUnique(
            preconditions,
            item => item.StableKey,
            "transition precondition",
            nameof(preconditions));
        ResultingFacts = TacticalCombatText.CopyUnique(
            resultingFacts,
            item => item.StableKey,
            "transition result",
            nameof(resultingFacts));
        Timing = TacticalCombatText.Defined(timing, nameof(timing));
        ExpectedPurposeIdentity = TacticalCombatText.Code(
            expectedPurposeIdentity,
            nameof(expectedPurposeIdentity));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "transition evidence",
            nameof(evidence));
        if (Preconditions.IsEmpty
            || ResultingFacts.IsEmpty
            || Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical transition requires preconditions, resulting facts, and evidence.");
        }
    }

    public TacticalTransitionIdentity Identity { get; }

    public ImmutableArray<TacticalRequirementIdentity> Preconditions { get; }

    public ImmutableArray<TacticalFactIdentity> ResultingFacts { get; }

    public TacticalTransitionTiming Timing { get; }

    public string ExpectedPurposeIdentity { get; }

    public string LimitationIdentity { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(Timing),
        ExpectedPurposeIdentity,
        LimitationIdentity,
        string.Join("||", Preconditions.Select(item => item.StableKey)),
        string.Join("||", ResultingFacts.Select(item => item.StableKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));
}
