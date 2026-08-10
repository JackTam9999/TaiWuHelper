namespace TaiWu.Domain.TargetArchetypes;

public enum TargetArchetypePredicateOperator
{
    FacetConfirmed,
    ValueEquals
}

public enum TargetArchetypeMatchState
{
    Matched,
    Partial,
    NotMatched,
    Unsupported,
    Conflicting
}
