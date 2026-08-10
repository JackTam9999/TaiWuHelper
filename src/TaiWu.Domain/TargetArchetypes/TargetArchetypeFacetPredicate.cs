using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public sealed class TargetArchetypeFacetPredicate
{
    public TargetArchetypeFacetPredicate(
        string code,
        TargetProfileFacetIdentity facet,
        TargetArchetypePredicateOperator predicateOperator,
        TargetProfileFacetValue? expectedValue = null)
    {
        if (!Enum.IsDefined(predicateOperator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(predicateOperator),
                predicateOperator,
                "Unknown target-archetype predicate operator.");
        }

        Facet = facet ?? throw new ArgumentNullException(nameof(facet));
        if (predicateOperator == TargetArchetypePredicateOperator.FacetConfirmed
            && expectedValue is not null
            || predicateOperator == TargetArchetypePredicateOperator.ValueEquals
            && expectedValue is null)
        {
            throw new ArgumentException(
                "Facet-confirmed predicates cannot contain an expected value "
                + "and value-equality predicates require one.",
                nameof(expectedValue));
        }

        if (expectedValue is not null
            && (expectedValue.Dimension != facet.Dimension
                || !string.Equals(
                    expectedValue.Code,
                    facet.Code,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "A predicate expected value must match its facet dimension "
                + "and code.",
                nameof(expectedValue));
        }

        Code = TargetProfileText.Code(code, nameof(code));
        Operator = predicateOperator;
        ExpectedValue = expectedValue;
    }

    public string Code { get; }

    public TargetProfileFacetIdentity Facet { get; }

    public TargetArchetypePredicateOperator Operator { get; }

    public TargetProfileFacetValue? ExpectedValue { get; }

    internal string StableKey => TargetProfileText.Stable(
        Code,
        Facet.StableKey,
        ((int)Operator).ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        ExpectedValue?.StableKey ?? string.Empty);
}
