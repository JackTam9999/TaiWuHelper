using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybooks;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public static class TargetSpecificPlaybookAdjuster
{
    public const string EvidenceMissingCode =
        "ADJUSTMENT_RULE_EVIDENCE_MISSING";

    public const string EvidenceStateMismatchCode =
        "ADJUSTMENT_RULE_EVIDENCE_STATE_MISMATCH";

    public const string ResponseMissingCode =
        "ADJUSTMENT_RULE_RESPONSE_MISSING";

    public const string RuleShadowedCode =
        "ADJUSTMENT_RULE_SHADOWED";

    public static TargetPlaybookAdjustmentSet Apply(
        TargetPlaybookComposition composition,
        TargetCombatProfileAnalysis analysis,
        IEnumerable<TargetPlaybookAdjustmentRule>? reviewedRules = null)
    {
        ArgumentNullException.ThrowIfNull(composition);
        ArgumentNullException.ThrowIfNull(analysis);
        EnsureSameAnalysis(composition, analysis);

        var evidence = CollectEvidence(composition, analysis);
        var automatic = CreateAutomaticAdjustments(
            composition,
            analysis,
            evidence);
        List<TargetPlaybookAdjustmentDiagnostic> diagnostics = [];
        var reviewed = ApplyReviewedRules(
            composition,
            reviewedRules ?? [],
            evidence,
            diagnostics);

        var adjustments = automatic.ToDictionary(
            adjustment => adjustment.TargetKey,
            StringComparer.Ordinal);
        foreach (var group in reviewed
                     .GroupBy(
                         adjustment => adjustment.TargetKey,
                         StringComparer.Ordinal))
        {
            var ordered = group
                .OrderBy(adjustment => ActionOrder(adjustment.Action))
                .ThenBy(adjustment => adjustment.RuleCode,
                    StringComparer.Ordinal)
                .ToArray();
            adjustments[group.Key] = ordered[0];
            foreach (var shadowed in ordered.Skip(1))
            {
                diagnostics.Add(new TargetPlaybookAdjustmentDiagnostic(
                    RuleShadowedCode,
                    shadowed.RuleCode,
                    shadowed.Evidence.Select(value => value.Identity)));
            }
        }

        return new TargetPlaybookAdjustmentSet(
            analysis.Profile.Fingerprint,
            composition.StableKey,
            evidence,
            adjustments.Values,
            diagnostics);
    }

    private static void EnsureSameAnalysis(
        TargetPlaybookComposition composition,
        TargetCombatProfileAnalysis analysis)
    {
        if (!string.Equals(
                composition.ProfileFingerprint,
                analysis.Profile.Fingerprint,
                StringComparison.Ordinal)
            || !string.Equals(
                composition.MatchSetKey,
                analysis.ArchetypeMatches.StableKey,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Target-specific adjustments require the exact profile and "
                + "archetype-match set that produced the composition.",
                nameof(analysis));
        }
    }

    private static TargetPlaybookAdjustmentEvidence[] CollectEvidence(
        TargetPlaybookComposition composition,
        TargetCombatProfileAnalysis analysis)
    {
        List<TargetPlaybookAdjustmentEvidence> evidence = [];
        foreach (var facet in analysis.Profile.Facets)
        {
            evidence.Add(TargetPlaybookAdjustmentEvidence.FromFacet(facet));
            evidence.AddRange(
                TargetPlaybookAdjustmentEvidence.FromFacetSources(facet));
            evidence.AddRange(
                TargetPlaybookAdjustmentEvidence
                    .FromFacetMeasurementRelations(facet));
        }

        foreach (var threat in analysis.ThreatAnalysis.Threats)
        {
            evidence.Add(TargetPlaybookAdjustmentEvidence.FromThreat(threat));
            foreach (var source in threat.Sources)
            {
                evidence.Add(TargetPlaybookAdjustmentEvidence.FromThreatSource(
                    source,
                    TargetPlaybookAdjustmentEvidenceKind.Skill));
                evidence.Add(TargetPlaybookAdjustmentEvidence.FromThreatSource(
                    source,
                    TargetPlaybookAdjustmentEvidenceKind.Effect));
            }
        }

        evidence.AddRange(analysis.ArchetypeMatches.Matches.Select(
            TargetPlaybookAdjustmentEvidence.FromMatch));
        evidence.AddRange(composition.Goals
            .SelectMany(goal => goal.KnownGaps)
            .Select(TargetPlaybookAdjustmentEvidence.FromGap));

        return
        [
            .. evidence
                .DistinctBy(value => value.StableKey, StringComparer.Ordinal)
                .OrderBy(value => value.StableKey, StringComparer.Ordinal)
        ];
    }

    private static TargetPlaybookAdjustment[] CreateAutomaticAdjustments(
        TargetPlaybookComposition composition,
        TargetCombatProfileAnalysis analysis,
        TargetPlaybookAdjustmentEvidence[] allEvidence)
    {
        List<TargetPlaybookAdjustment> adjustments = [];
        var threatsByCode = analysis.ThreatAnalysis.Threats.ToDictionary(
            threat => threat.Threat.Code,
            StringComparer.Ordinal);

        foreach (var goal in composition.Goals)
        {
            var facetIdentities = goal.ProfileFacets
                .Select(TargetPlaybookAdjustmentEvidence.FacetIdentity)
                .ToHashSet(StringComparer.Ordinal);
            var threatIdentities = goal.Threats
                .Select(threat => $"THREAT:{threat.Code}")
                .ToHashSet(StringComparer.Ordinal);
            var exact = allEvidence.Where(value =>
                    facetIdentities.Contains(value.Identity)
                    || threatIdentities.Contains(value.Identity)
                    || value.Kind
                        == TargetPlaybookAdjustmentEvidenceKind.Observation
                    && facetIdentities.Any(facet =>
                        value.Identity.EndsWith(
                            facet,
                            StringComparison.Ordinal)))
                .ToArray();
            var confirmed = exact.Any(value =>
                value.State
                    == TargetPlaybookAdjustmentEvidenceState.Confirmed);
            if (confirmed)
            {
                var observed = exact.Any(value =>
                    value.Kind
                        == TargetPlaybookAdjustmentEvidenceKind.Observation
                    && value.State
                        == TargetPlaybookAdjustmentEvidenceState.Confirmed);
                var action = observed
                    && goal.Priority != TargetResponsePriority.Critical
                        ? TargetPlaybookAdjustmentAction.Elevated
                        : TargetPlaybookAdjustmentAction.Retained;
                adjustments.Add(new TargetPlaybookAdjustment(
                    action == TargetPlaybookAdjustmentAction.Elevated
                        ? "AUTO_CURRENT_OBSERVATION_ELEVATED"
                        : "AUTO_EXACT_TARGET_RETAINED",
                    action,
                    new TargetPlaybookResponseReference(
                        TargetPlaybookResponseReferenceKind.Goal,
                        goal.Code),
                    resultResponse: null,
                    action == TargetPlaybookAdjustmentAction.Elevated
                        ? "CURRENT_OBSERVATION_CONFIRMS_RESPONSE"
                        : "EXACT_TARGET_SUPPORTS_RESPONSE",
                    exact));
            }
            else if (exact.Any(value => value.State
                     == TargetPlaybookAdjustmentEvidenceState.Incomplete))
            {
                adjustments.Add(new TargetPlaybookAdjustment(
                    "AUTO_EXACT_TARGET_UNRESOLVED",
                    TargetPlaybookAdjustmentAction.Unresolved,
                    new TargetPlaybookResponseReference(
                        TargetPlaybookResponseReferenceKind.Goal,
                        goal.Code),
                    resultResponse: null,
                    "EXACT_TARGET_EVIDENCE_INCOMPLETE",
                    exact));
            }

            foreach (var gap in goal.KnownGaps)
            {
                adjustments.Add(new TargetPlaybookAdjustment(
                    "AUTO_PLAYBOOK_GAP_UNRESOLVED",
                    TargetPlaybookAdjustmentAction.Unresolved,
                    new TargetPlaybookResponseReference(
                        TargetPlaybookResponseReferenceKind.Gap,
                        gap.Code),
                    resultResponse: null,
                    "PLAYBOOK_GAP_REMAINS_UNRESOLVED",
                    [TargetPlaybookAdjustmentEvidence.FromGap(gap)]));
            }
        }

        var coveredThreatCodes = composition.Goals
            .SelectMany(goal => goal.Threats)
            .Select(threat => threat.Code)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var threat in threatsByCode.Values
                     .Where(value => !coveredThreatCodes.Contains(
                         value.Threat.Code))
                     .OrderBy(value => value.Threat.Code,
                         StringComparer.Ordinal))
        {
            var threatEvidence = allEvidence.Where(value =>
                string.Equals(
                    value.Identity,
                    $"THREAT:{threat.Threat.Code}",
                    StringComparison.Ordinal)
                || threat.Sources.Any(source =>
                    value.Identity == $"SKILL:{source.SkillId}"
                    || value.Identity ==
                        $"EFFECT:{source.SkillId}:{(int)source.Direction}:"
                        + source.RawEffectId));
            adjustments.Add(new TargetPlaybookAdjustment(
                "AUTO_EXACT_THREAT_ADDED",
                TargetPlaybookAdjustmentAction.Added,
                originalResponse: null,
                new TargetPlaybookResponseReference(
                    TargetPlaybookResponseReferenceKind.Threat,
                    threat.Threat.Code),
                "EXACT_TARGET_THREAT_OUTSIDE_PLAYBOOK",
                threatEvidence));
        }

        return [.. adjustments.OrderBy(
            adjustment => adjustment.TargetKey,
            StringComparer.Ordinal)];
    }

    private static TargetPlaybookAdjustment[] ApplyReviewedRules(
        TargetPlaybookComposition composition,
        IEnumerable<TargetPlaybookAdjustmentRule> rules,
        TargetPlaybookAdjustmentEvidence[] evidence,
        ICollection<TargetPlaybookAdjustmentDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var ruleValues = rules.ToArray();
        if (ruleValues.Any(rule => rule is null))
        {
            throw new ArgumentException(
                "Reviewed adjustment rules cannot contain null entries.",
                nameof(rules));
        }

        if (ruleValues.DistinctBy(rule => rule.Code, StringComparer.Ordinal)
            .Count() != ruleValues.Length)
        {
            throw new ArgumentException(
                "Reviewed adjustment-rule codes must be unique.",
                nameof(rules));
        }

        var responses = ExistingResponses(composition);
        var evidenceByIdentity = evidence
            .GroupBy(value => value.Identity, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.Ordinal);
        List<TargetPlaybookAdjustment> adjustments = [];
        foreach (var rule in ruleValues.OrderBy(
                     rule => rule.Code,
                     StringComparer.Ordinal))
        {
            if (rule.OriginalResponse is not null
                && !responses.Contains(rule.OriginalResponse.StableKey))
            {
                diagnostics.Add(new TargetPlaybookAdjustmentDiagnostic(
                    ResponseMissingCode,
                    rule.Code,
                    rule.RequiredEvidenceIdentities));
                continue;
            }

            var missing = rule.RequiredEvidenceIdentities
                .Where(identity => !evidenceByIdentity.ContainsKey(identity))
                .ToArray();
            if (missing.Length > 0)
            {
                diagnostics.Add(new TargetPlaybookAdjustmentDiagnostic(
                    EvidenceMissingCode,
                    rule.Code,
                    missing));
                continue;
            }

            var exact = rule.RequiredEvidenceIdentities
                .SelectMany(identity => evidenceByIdentity[identity])
                .ToArray();
            var requiredState = RequiredState(rule.Action);
            var wrongState = rule.RequiredEvidenceIdentities.Any(identity =>
                !evidenceByIdentity[identity].Any(value =>
                    value.State == requiredState));
            if (wrongState)
            {
                diagnostics.Add(new TargetPlaybookAdjustmentDiagnostic(
                    EvidenceStateMismatchCode,
                    rule.Code,
                    rule.RequiredEvidenceIdentities));
                continue;
            }

            adjustments.Add(new TargetPlaybookAdjustment(
                rule.Code,
                rule.Action,
                rule.OriginalResponse,
                rule.ResultResponse,
                rule.ReasonCode,
                exact));
        }

        return [.. adjustments];
    }

    private static HashSet<string> ExistingResponses(
        TargetPlaybookComposition composition)
    {
        HashSet<string> responses = new(StringComparer.Ordinal);
        foreach (var goal in composition.Goals)
        {
            responses.Add(new TargetPlaybookResponseReference(
                TargetPlaybookResponseReferenceKind.Goal,
                goal.Code).StableKey);
            foreach (var option in goal.Options)
            {
                responses.Add(new TargetPlaybookResponseReference(
                    TargetPlaybookResponseReferenceKind.Option,
                    option.StableKey).StableKey);
            }

            foreach (var gap in goal.KnownGaps)
            {
                responses.Add(new TargetPlaybookResponseReference(
                    TargetPlaybookResponseReferenceKind.Gap,
                    gap.Code).StableKey);
            }

            foreach (var threat in goal.Threats)
            {
                responses.Add(new TargetPlaybookResponseReference(
                    TargetPlaybookResponseReferenceKind.Threat,
                    threat.Code).StableKey);
            }
        }

        return responses;
    }

    private static TargetPlaybookAdjustmentEvidenceState RequiredState(
        TargetPlaybookAdjustmentAction action) => action switch
        {
            TargetPlaybookAdjustmentAction.Retained
                or TargetPlaybookAdjustmentAction.Elevated
                or TargetPlaybookAdjustmentAction.Added
                or TargetPlaybookAdjustmentAction.Replaced =>
                TargetPlaybookAdjustmentEvidenceState.Confirmed,
            TargetPlaybookAdjustmentAction.Reduced =>
                TargetPlaybookAdjustmentEvidenceState.Contrary,
            TargetPlaybookAdjustmentAction.Unresolved =>
                TargetPlaybookAdjustmentEvidenceState.Incomplete,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

    private static int ActionOrder(TargetPlaybookAdjustmentAction action) =>
        action switch
        {
            TargetPlaybookAdjustmentAction.Replaced => 0,
            TargetPlaybookAdjustmentAction.Reduced => 1,
            TargetPlaybookAdjustmentAction.Elevated => 2,
            TargetPlaybookAdjustmentAction.Added => 3,
            TargetPlaybookAdjustmentAction.Retained => 4,
            TargetPlaybookAdjustmentAction.Unresolved => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
}
