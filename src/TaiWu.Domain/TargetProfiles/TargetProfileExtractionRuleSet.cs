using System.Collections.Immutable;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Domain.TargetProfiles;

public sealed record TargetProfileThreatFacetRule
{
    public TargetProfileThreatFacetRule(
        TargetThreatKind threatKind,
        TargetProfileFacetIdentity facet)
    {
        if (!Enum.IsDefined(threatKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(threatKind),
                threatKind,
                "Unknown target-threat kind.");
        }

        ThreatKind = threatKind;
        Facet = facet ?? throw new ArgumentNullException(nameof(facet));
    }

    public TargetThreatKind ThreatKind { get; }

    public TargetProfileFacetIdentity Facet { get; }
}

public sealed class TargetProfileExtractionRuleSet
{
    public TargetProfileExtractionRuleSet(
        TargetProfileVersion ruleVersion,
        TargetProfileVersion gameDataVersion,
        TargetProfileFacetIdentity outerDamageFacet,
        TargetProfileFacetIdentity channelResistanceFacet,
        TargetProfileFacetIdentity poisonApplicationFacet,
        string weaponSubtypeFacetPrefix,
        IEnumerable<TargetProfileThreatFacetRule> threatFacetRules)
    {
        RuleVersion = ruleVersion
            ?? throw new ArgumentNullException(nameof(ruleVersion));
        GameDataVersion = gameDataVersion
            ?? throw new ArgumentNullException(nameof(gameDataVersion));
        OuterDamageFacet = RequireDimension(
            outerDamageFacet,
            TargetProfileDimension.Pressure,
            nameof(outerDamageFacet));
        ChannelResistanceFacet = RequireDimension(
            channelResistanceFacet,
            TargetProfileDimension.Resilience,
            nameof(channelResistanceFacet));
        PoisonApplicationFacet = RequireDimension(
            poisonApplicationFacet,
            TargetProfileDimension.Control,
            nameof(poisonApplicationFacet));
        WeaponSubtypeFacetPrefix = TargetProfileText.Code(
            weaponSubtypeFacetPrefix,
            nameof(weaponSubtypeFacetPrefix));

        ArgumentNullException.ThrowIfNull(threatFacetRules);
        var values = threatFacetRules.ToImmutableArray();
        if (values.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "Threat-facet rules cannot contain null entries.",
                nameof(threatFacetRules));
        }

        var duplicateKind = values
            .GroupBy(rule => rule.ThreatKind)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateKind is not null)
        {
            throw new ArgumentException(
                $"Threat kind {duplicateKind.Key} is mapped more than once.",
                nameof(threatFacetRules));
        }

        var duplicateFacet = values
            .GroupBy(rule => rule.Facet.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateFacet is not null)
        {
            throw new ArgumentException(
                "A threat-derived facet cannot be mapped from multiple threat "
                + "kinds in one rule set.",
                nameof(threatFacetRules));
        }

        var allStaticFacets = values.Select(rule => rule.Facet.StableKey)
            .Append(OuterDamageFacet.StableKey)
            .Append(ChannelResistanceFacet.StableKey)
            .Append(PoisonApplicationFacet.StableKey);
        if (allStaticFacets.Distinct(StringComparer.Ordinal).Count()
            != values.Length + 3)
        {
            throw new ArgumentException(
                "Core and threat-derived profile facets must be distinct.",
                nameof(threatFacetRules));
        }

        ThreatFacetRules = [.. values.OrderBy(rule => rule.ThreatKind)];
    }

    public TargetProfileVersion RuleVersion { get; }

    public TargetProfileVersion GameDataVersion { get; }

    public TargetProfileFacetIdentity OuterDamageFacet { get; }

    public TargetProfileFacetIdentity ChannelResistanceFacet { get; }

    public TargetProfileFacetIdentity PoisonApplicationFacet { get; }

    public string WeaponSubtypeFacetPrefix { get; }

    public ImmutableArray<TargetProfileThreatFacetRule> ThreatFacetRules
    { get; }

    public TargetProfileFacetIdentity WeaponSubtypeFacet(int itemSubtype)
    {
        if (itemSubtype <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(itemSubtype),
                itemSubtype,
                "A weapon subtype must be positive.");
        }

        return new TargetProfileFacetIdentity(
            TargetProfileDimension.AttackFamily,
            $"{WeaponSubtypeFacetPrefix}:{itemSubtype}");
    }

    private static TargetProfileFacetIdentity RequireDimension(
        TargetProfileFacetIdentity? value,
        TargetProfileDimension expected,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.Dimension != expected)
        {
            throw new ArgumentException(
                $"The profile facet must use the {expected} dimension.",
                parameterName);
        }

        return value;
    }
}
