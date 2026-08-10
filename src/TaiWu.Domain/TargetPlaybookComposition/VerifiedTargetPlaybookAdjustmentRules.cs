using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public static class VerifiedTargetPlaybookAdjustmentRules
{
    internal const string OuterResistanceHigherEvidence =
        "FACET_RELATION:CHANNEL_RESISTANCE:OUTER_HIGHER";

    internal const string InnerResistanceHigherEvidence =
        "FACET_RELATION:CHANNEL_RESISTANCE:INNER_HIGHER";

    public static IReadOnlyList<TargetPlaybookAdjustmentRule> For(
        TargetCombatProfileAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var facet = analysis.Profile.Facets.SingleOrDefault(value =>
            value.Identity.Dimension == TargetProfileDimension.Resilience
            && string.Equals(
                value.Identity.Code,
                "CHANNEL_RESISTANCE_ASYMMETRY",
                StringComparison.Ordinal)
            && value.State == TargetProfileEvidenceState.Confirmed);
        if (facet?.Value?.Kind
                != TargetProfileFacetValueKind.Measurements)
        {
            return [];
        }

        var measurements = facet.Value.Measurements.ToDictionary(
            value => value.Code,
            StringComparer.Ordinal);
        if (!measurements.TryGetValue("OUTER", out var outer)
            || !measurements.TryGetValue("INNER", out var inner)
            || outer.Value == inner.Value)
        {
            return [];
        }

        return outer.Value > inner.Value
            ? [Replace(
                "REPLACE_INNER_ROUTE_WITH_OUTER_TO_INNER",
                "REVERSE_YINYANG_ROUTE_INNER_TO_OUTER",
                "DIRECT_YINYANG_ROUTE_OUTER_TO_INNER",
                "LOWER_INNER_RESISTANCE_SELECTS_OUTER_ROUTE",
                OuterResistanceHigherEvidence)]
            : [Replace(
                "REPLACE_OUTER_ROUTE_WITH_INNER_TO_OUTER",
                "DIRECT_YINYANG_ROUTE_OUTER_TO_INNER",
                "REVERSE_YINYANG_ROUTE_INNER_TO_OUTER",
                "LOWER_OUTER_RESISTANCE_SELECTS_INNER_ROUTE",
                InnerResistanceHigherEvidence)];
    }

    private static TargetPlaybookAdjustmentRule Replace(
        string code,
        string originalOption,
        string replacementOption,
        string reasonCode,
        string evidenceIdentity) => new(
            code,
            TargetPlaybookAdjustmentAction.Replaced,
            new TargetPlaybookResponseReference(
                TargetPlaybookResponseReferenceKind.Option,
                originalOption),
            new TargetPlaybookResponseReference(
                TargetPlaybookResponseReferenceKind.Option,
                replacementOption),
            reasonCode,
            [evidenceIdentity]);
}
