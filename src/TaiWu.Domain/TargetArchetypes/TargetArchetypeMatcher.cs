using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public static class TargetArchetypeMatcher
{
    public const string UnsupportedProfileRuleVersionCode =
        "PROFILE_RULE_VERSION_UNSUPPORTED";

    public const string RequiredFacetMissingCode =
        "REQUIRED_FACET_MISSING";

    public const string RequiredFacetIncompleteCode =
        "REQUIRED_FACET_INCOMPLETE";

    public const string RequiredFacetUnsupportedCode =
        "REQUIRED_FACET_UNSUPPORTED";

    public const string RequiredValueContradictedCode =
        "REQUIRED_VALUE_CONTRADICTED";

    public const string ExclusionConfirmedCode =
        "EXCLUSION_CONFIRMED";

    public const string ExclusionUnresolvedCode =
        "EXCLUSION_UNRESOLVED";

    public const string PredicateEvidenceConflictingCode =
        "PREDICATE_EVIDENCE_CONFLICTING";

    public const string NoRequiredFacetAvailableCode =
        "NO_REQUIRED_FACET_AVAILABLE";

    public static TargetArchetypeMatchSet Match(
        TargetCombatProfile profile,
        IEnumerable<TargetArchetypeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(definitions);
        var values = definitions.ToArray();
        if (values.Any(definition => definition is null))
        {
            throw new ArgumentException(
                "Archetype definitions cannot contain null entries.",
                nameof(definitions));
        }

        var duplicate = values
            .GroupBy(definition => definition.StableKey,
                StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Archetype definition {duplicate.Key} is duplicated.",
                nameof(definitions));
        }

        var facets = profile.Facets.ToDictionary(
            facet => facet.Identity.StableKey,
            StringComparer.Ordinal);
        var matches = values
            .OrderBy(definition => definition.Identity.Code,
                StringComparer.Ordinal)
            .ThenBy(definition => definition.Identity.Version.Value,
                StringComparer.Ordinal)
            .Select(definition => Evaluate(profile, definition, facets));
        return new TargetArchetypeMatchSet(profile.Fingerprint, matches);
    }

    private static TargetArchetypeMatch Evaluate(
        TargetCombatProfile profile,
        TargetArchetypeDefinition definition,
        IReadOnlyDictionary<string, TargetProfileFacet> facets)
    {
        if (!string.Equals(
                profile.RuleVersion.Value,
                definition.ApplicableProfileRuleVersion.Value,
                StringComparison.Ordinal))
        {
            return new TargetArchetypeMatch(
                definition,
                profile.Fingerprint,
                TargetArchetypeMatchState.Unsupported,
                supportingFacets: [],
                missingFacets: [],
                excludingFacets: [],
                conflictingFacets: [],
                diagnostics:
                [
                    new TargetArchetypeMatchDiagnostic(
                        UnsupportedProfileRuleVersionCode)
                ]);
        }

        List<TargetProfileFacetIdentity> supporting = [];
        List<TargetProfileFacetIdentity> missing = [];
        List<TargetProfileFacetIdentity> excluding = [];
        List<TargetProfileFacetIdentity> conflicting = [];
        List<TargetArchetypeMatchDiagnostic> diagnostics = [];
        var requiredSatisfied = 0;

        foreach (var predicate in definition.RequiredPredicates)
        {
            var evaluation = EvaluatePredicate(predicate, facets);
            switch (evaluation)
            {
                case PredicateEvaluation.Satisfied:
                    requiredSatisfied++;
                    supporting.Add(predicate.Facet);
                    break;
                case PredicateEvaluation.Missing:
                    missing.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        RequiredFacetMissingCode,
                        predicate));
                    break;
                case PredicateEvaluation.Incomplete:
                    missing.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        RequiredFacetIncompleteCode,
                        predicate));
                    break;
                case PredicateEvaluation.Unsupported:
                    missing.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        RequiredFacetUnsupportedCode,
                        predicate));
                    break;
                case PredicateEvaluation.Contradicted:
                    excluding.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        RequiredValueContradictedCode,
                        predicate));
                    break;
                case PredicateEvaluation.Conflicting:
                    conflicting.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        PredicateEvidenceConflictingCode,
                        predicate));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        foreach (var predicate in definition.SupportingPredicates)
        {
            var evaluation = EvaluatePredicate(predicate, facets);
            if (evaluation == PredicateEvaluation.Satisfied)
            {
                supporting.Add(predicate.Facet);
            }
            else if (evaluation == PredicateEvaluation.Conflicting)
            {
                conflicting.Add(predicate.Facet);
                diagnostics.Add(Diagnostic(
                    PredicateEvidenceConflictingCode,
                    predicate));
            }
        }

        foreach (var predicate in definition.Exclusions)
        {
            var evaluation = EvaluatePredicate(predicate, facets);
            switch (evaluation)
            {
                case PredicateEvaluation.Satisfied:
                    excluding.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        ExclusionConfirmedCode,
                        predicate));
                    break;
                case PredicateEvaluation.Missing:
                case PredicateEvaluation.Incomplete:
                case PredicateEvaluation.Unsupported:
                    missing.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        ExclusionUnresolvedCode,
                        predicate));
                    break;
                case PredicateEvaluation.Conflicting:
                    conflicting.Add(predicate.Facet);
                    diagnostics.Add(Diagnostic(
                        PredicateEvidenceConflictingCode,
                        predicate));
                    break;
                case PredicateEvaluation.Contradicted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        TargetArchetypeMatchState state;
        if (conflicting.Count > 0)
        {
            state = TargetArchetypeMatchState.Conflicting;
        }
        else if (excluding.Count > 0)
        {
            state = TargetArchetypeMatchState.NotMatched;
        }
        else if (missing.Count == 0
                 && requiredSatisfied == definition.RequiredPredicates.Length)
        {
            state = TargetArchetypeMatchState.Matched;
        }
        else if (requiredSatisfied > 0)
        {
            state = TargetArchetypeMatchState.Partial;
        }
        else
        {
            state = TargetArchetypeMatchState.Unsupported;
            diagnostics.Add(new TargetArchetypeMatchDiagnostic(
                NoRequiredFacetAvailableCode));
        }

        return new TargetArchetypeMatch(
            definition,
            profile.Fingerprint,
            state,
            supporting,
            missing,
            excluding,
            conflicting,
            diagnostics);
    }

    private static PredicateEvaluation EvaluatePredicate(
        TargetArchetypeFacetPredicate predicate,
        IReadOnlyDictionary<string, TargetProfileFacet> facets)
    {
        if (!facets.TryGetValue(predicate.Facet.StableKey, out var facet))
        {
            return PredicateEvaluation.Missing;
        }

        return facet.State switch
        {
            TargetProfileEvidenceState.Incomplete =>
                PredicateEvaluation.Incomplete,
            TargetProfileEvidenceState.Unsupported =>
                PredicateEvaluation.Unsupported,
            TargetProfileEvidenceState.Conflicting =>
                PredicateEvaluation.Conflicting,
            TargetProfileEvidenceState.Confirmed => predicate.Operator switch
            {
                TargetArchetypePredicateOperator.FacetConfirmed =>
                    PredicateEvaluation.Satisfied,
                TargetArchetypePredicateOperator.ValueEquals =>
                    facet.Value!.Equals(predicate.ExpectedValue)
                        ? PredicateEvaluation.Satisfied
                        : PredicateEvaluation.Contradicted,
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static TargetArchetypeMatchDiagnostic Diagnostic(
        string code,
        TargetArchetypeFacetPredicate predicate) => new(
            code,
            predicate.Code,
            predicate.Facet);

    private enum PredicateEvaluation
    {
        Satisfied,
        Missing,
        Incomplete,
        Unsupported,
        Contradicted,
        Conflicting
    }
}
