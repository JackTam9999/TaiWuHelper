using System.Collections.Immutable;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public sealed class TargetArchetypeDefinition
{
    public TargetArchetypeDefinition(
        TargetArchetypeIdentity identity,
        TargetProfileVersion applicableProfileRuleVersion,
        string localizedTitleKey,
        IEnumerable<TargetArchetypeFacetPredicate> requiredPredicates,
        IEnumerable<TargetArchetypeFacetPredicate> supportingPredicates,
        IEnumerable<TargetArchetypeFacetPredicate> exclusions,
        IEnumerable<string> evidenceReferences)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        ApplicableProfileRuleVersion = applicableProfileRuleVersion
            ?? throw new ArgumentNullException(
                nameof(applicableProfileRuleVersion));
        LocalizedTitleKey = TargetProfileText.ResourceKey(
            localizedTitleKey,
            nameof(localizedTitleKey));

        RequiredPredicates = CopyPredicates(
            requiredPredicates,
            nameof(requiredPredicates),
            requireValue: true);
        SupportingPredicates = CopyPredicates(
            supportingPredicates,
            nameof(supportingPredicates),
            requireValue: false);
        Exclusions = CopyPredicates(
            exclusions,
            nameof(exclusions),
            requireValue: false);

        var predicates = RequiredPredicates
            .Concat(SupportingPredicates)
            .Concat(Exclusions)
            .ToImmutableArray();
        var duplicateCode = predicates
            .GroupBy(predicate => predicate.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateCode is not null)
        {
            throw new ArgumentException(
                $"Archetype predicate {duplicateCode.Key} is duplicated.",
                nameof(requiredPredicates));
        }

        var duplicateFacet = predicates
            .GroupBy(predicate => predicate.Facet.StableKey,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFacet is not null)
        {
            throw new ArgumentException(
                "An archetype cannot evaluate the same facet in more than one "
                + "predicate role.",
                nameof(requiredPredicates));
        }

        ArgumentNullException.ThrowIfNull(evidenceReferences);
        var references = evidenceReferences
            .Select(reference => TargetProfileText.Code(
                reference,
                nameof(evidenceReferences)))
            .ToImmutableArray();
        if (references.Length == 0)
        {
            throw new ArgumentException(
                "An archetype definition requires evidence.",
                nameof(evidenceReferences));
        }

        if (references.Distinct(StringComparer.Ordinal).Count()
            != references.Length)
        {
            throw new ArgumentException(
                "Archetype evidence references must be unique.",
                nameof(evidenceReferences));
        }

        EvidenceReferences = [.. references.Order(StringComparer.Ordinal)];
    }

    public TargetArchetypeIdentity Identity { get; }

    public TargetProfileVersion ApplicableProfileRuleVersion { get; }

    public string LocalizedTitleKey { get; }

    public ImmutableArray<TargetArchetypeFacetPredicate> RequiredPredicates
    { get; }

    public ImmutableArray<TargetArchetypeFacetPredicate> SupportingPredicates
    { get; }

    public ImmutableArray<TargetArchetypeFacetPredicate> Exclusions { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    public string StableKey => Identity.StableKey;

    private static ImmutableArray<TargetArchetypeFacetPredicate> CopyPredicates(
        IEnumerable<TargetArchetypeFacetPredicate> values,
        string parameterName,
        bool requireValue)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var predicates = values.ToImmutableArray();
        if (requireValue && predicates.Length == 0)
        {
            throw new ArgumentException(
                "An archetype definition requires at least one required "
                + "predicate.",
                parameterName);
        }

        if (predicates.Any(predicate => predicate is null))
        {
            throw new ArgumentException(
                "Archetype predicate collections cannot contain null entries.",
                parameterName);
        }

        return [.. predicates.OrderBy(
            predicate => predicate.StableKey,
            StringComparer.Ordinal)];
    }
}
