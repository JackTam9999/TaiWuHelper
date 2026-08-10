using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public sealed class TargetArchetypeMatchDiagnostic
{
    public TargetArchetypeMatchDiagnostic(
        string code,
        string? predicateCode = null,
        TargetProfileFacetIdentity? facet = null)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        PredicateCode = predicateCode is null
            ? null
            : TargetProfileText.Code(predicateCode, nameof(predicateCode));
        Facet = facet;
        if ((PredicateCode is null) != (Facet is null))
        {
            throw new ArgumentException(
                "A predicate diagnostic must identify both its predicate and "
                + "facet, or neither for a definition-level diagnostic.");
        }
    }

    public string Code { get; }

    public string? PredicateCode { get; }

    public TargetProfileFacetIdentity? Facet { get; }

    internal string StableKey => TargetProfileText.Stable(
        Code,
        PredicateCode ?? string.Empty,
        Facet?.StableKey ?? string.Empty);
}
