using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public static class CompanionRoleEvaluator
{
    public static CompanionRoleEvaluation Evaluate(
        CompanionRoleDefinition definition,
        CandidateProfile profile,
        CandidateDisciplineIdentity discipline)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(discipline);

        var gates = new List<CompanionRoleGateEvaluation>();
        var components = new List<CompanionRoleScoreComponent>();
        var requirementIndex = 0;

        var universeOutcome = profile.UniverseState switch
        {
            CandidateUniverseState.Eligible => CompanionRoleGateOutcome.Passed,
            CandidateUniverseState.Ineligible => CompanionRoleGateOutcome.Failed,
            CandidateUniverseState.Incomplete => CompanionRoleGateOutcome.Incomplete,
            CandidateUniverseState.Unsupported => CompanionRoleGateOutcome.Unsupported,
            CandidateUniverseState.Conflicting => CompanionRoleGateOutcome.Conflicting,
            _ => throw new ArgumentOutOfRangeException(
                nameof(profile),
                profile.UniverseState,
                "Unknown candidate-universe state.")
        };
        gates.Add(Gate(
            definition.HardRequirements[requirementIndex++],
            universeOutcome,
            $"CANDIDATE_UNIVERSE_{profile.UniverseState.ToString().ToUpperInvariant()}",
            []));
        if (universeOutcome != CompanionRoleGateOutcome.Passed)
        {
            return Unranked(definition, profile, discipline, gates, universeOutcome);
        }

        var sourceSupported = definition.SupportedGameDataVersions.Contains(
                profile.SourceVersions.GameDataVersion,
                StringComparer.Ordinal)
            && string.Equals(
                definition.SupportedProfileMappingVersion,
                profile.SourceVersions.ProfileMappingVersion,
                StringComparison.Ordinal)
            && string.Equals(
                definition.SupportedFingerprintSchemaVersion,
                profile.SourceVersions.FingerprintSchemaVersion,
                StringComparison.Ordinal);
        var sourceOutcome = sourceSupported
            ? CompanionRoleGateOutcome.Passed
            : CompanionRoleGateOutcome.Unsupported;
        gates.Add(Gate(
            definition.HardRequirements[requirementIndex++],
            sourceOutcome,
            sourceSupported ? "SOURCE_VERSIONS_MATCH" : "SOURCE_VERSIONS_UNSUPPORTED",
            []));
        if (!sourceSupported)
        {
            return Unranked(definition, profile, discipline, gates, sourceOutcome);
        }

        var disciplineSupported = discipline.Domain == definition.DisciplineDomain
            && discipline.Type >= definition.MinimumDisciplineType
            && discipline.Type <= definition.MaximumDisciplineType;
        var disciplineOutcome = disciplineSupported
            ? CompanionRoleGateOutcome.Passed
            : CompanionRoleGateOutcome.Unsupported;
        gates.Add(Gate(
            definition.HardRequirements[requirementIndex++],
            disciplineOutcome,
            definition.RequiresDisciplineSelection
                ? disciplineSupported
                    ? "DISCIPLINE_SUPPORTED"
                    : "DISCIPLINE_UNSUPPORTED"
                : disciplineSupported
                    ? "OBJECTIVE_SUPPORTED"
                    : "OBJECTIVE_UNSUPPORTED",
            []));
        if (!disciplineSupported)
        {
            return Unranked(definition, profile, discipline, gates, disciplineOutcome);
        }

        foreach (var dimension in definition.ScoreDimensions)
        {
            var field = CandidateProfileFieldIdentity.ForRole(
                dimension.Field,
                discipline);
            short rawValue;
            IReadOnlyList<CandidateEvidenceReference> componentEvidence;
            if (dimension.Field == CandidateProfileField.CapabilityBreadthIndex)
            {
                var summary = CompanionCapabilitySummaryBuilder.Build(profile);
                var capabilityFacts = CapabilityFacts(profile, summary);
                var capabilityEvidence = capabilityFacts
                    .SelectMany(fact => fact.Evidence)
                    .Distinct()
                    .OrderBy(item => item.StableKey, StringComparer.Ordinal)
                    .ToArray();
                var factOutcome = CapabilityOutcome(summary.State);
                gates.Add(Gate(
                    definition.HardRequirements[requirementIndex++],
                    factOutcome,
                    $"CAPABILITY_SUMMARY_{summary.State.ToString().ToUpperInvariant()}",
                    capabilityEvidence));
                if (factOutcome != CompanionRoleGateOutcome.Passed)
                {
                    return Unranked(
                        definition,
                        profile,
                        discipline,
                        gates,
                        factOutcome);
                }

                var provenanceMatches = capabilityFacts.Count
                        == CompanionCapabilitySummary.MainAttributeCount
                        + CompanionCapabilitySummary.MartialDisciplineCount
                        + CompanionCapabilitySummary.LifeSkillDisciplineCount
                    && capabilityFacts.All(fact =>
                        ProvenanceMatchesProfile(fact, profile));
                var provenanceOutcome = provenanceMatches
                    ? CompanionRoleGateOutcome.Passed
                    : CompanionRoleGateOutcome.Conflicting;
                gates.Add(Gate(
                    definition.HardRequirements[requirementIndex++],
                    provenanceOutcome,
                    provenanceMatches
                        ? "CAPABILITY_PROVENANCE_MATCHES_PROFILE"
                        : "CAPABILITY_PROVENANCE_CONFLICTS_WITH_PROFILE",
                    capabilityEvidence));
                if (!provenanceMatches)
                {
                    return Unranked(
                        definition,
                        profile,
                        discipline,
                        gates,
                        provenanceOutcome);
                }

                var scaledBreadth = summary.BreadthIndex!.Value * 100m;
                if (scaledBreadth < short.MinValue
                    || scaledBreadth > short.MaxValue)
                {
                    gates[^1] = Gate(
                        gates[^1].Requirement,
                        CompanionRoleGateOutcome.Conflicting,
                        "FACT_OUTSIDE_NORMALIZATION_RANGE",
                        capabilityEvidence);
                    return Unranked(
                        definition,
                        profile,
                        discipline,
                        gates,
                        CompanionRoleGateOutcome.Conflicting);
                }

                rawValue = decimal.ToInt16(scaledBreadth);
                componentEvidence = capabilityEvidence;
            }
            else
            {
                var fact = profile.FindFact(field);
                var factOutcome = FactOutcome(fact, dimension);
                gates.Add(Gate(
                    definition.HardRequirements[requirementIndex++],
                    factOutcome,
                    FactReason(fact, factOutcome),
                    fact?.Evidence ?? []));
                if (factOutcome != CompanionRoleGateOutcome.Passed)
                {
                    return Unranked(
                        definition,
                        profile,
                        discipline,
                        gates,
                        factOutcome);
                }

                var provenanceMatches = ProvenanceMatchesProfile(fact!, profile);
                var provenanceOutcome = provenanceMatches
                    ? CompanionRoleGateOutcome.Passed
                    : CompanionRoleGateOutcome.Conflicting;
                gates.Add(Gate(
                    definition.HardRequirements[requirementIndex++],
                    provenanceOutcome,
                    provenanceMatches
                        ? "FACT_PROVENANCE_MATCHES_PROFILE"
                        : "FACT_PROVENANCE_CONFLICTS_WITH_PROFILE",
                    fact!.Evidence));
                if (!provenanceMatches)
                {
                    return Unranked(
                        definition,
                        profile,
                        discipline,
                        gates,
                        provenanceOutcome);
                }

                rawValue = fact.Value!.Int16Value;
                componentEvidence = fact.Evidence;
            }

            var normalized = Normalize(dimension, rawValue);
            if (normalized is null)
            {
                gates[^1] = Gate(
                    gates[^1].Requirement,
                    CompanionRoleGateOutcome.Conflicting,
                    "FACT_OUTSIDE_NORMALIZATION_RANGE",
                    componentEvidence);
                return Unranked(
                    definition,
                    profile,
                    discipline,
                    gates,
                    CompanionRoleGateOutcome.Conflicting);
            }

            var directionalValue = dimension.Direction switch
            {
                CompanionRoleScoreDirection.HigherIsBetter => normalized.Value,
                CompanionRoleScoreDirection.LowerIsBetter => -normalized.Value,
                _ => throw new InvalidOperationException(
                    $"Unknown score direction '{dimension.Direction}'.")
            };
            var contribution = checked(directionalValue * dimension.Weight);
            components.Add(new CompanionRoleScoreComponent(
                dimension,
                field,
                rawValue,
                normalized.Value,
                contribution,
                componentEvidence));
        }

        var total = components.Sum(item => item.Contribution);
        return new CompanionRoleEvaluation(
            definition,
            profile,
            discipline,
            CompanionRoleEvaluationState.Rankable,
            gates,
            components,
            total,
            "ROLE_REQUIREMENTS_PASSED");
    }

    private static CompanionRoleGateEvaluation Gate(
        CompanionRoleHardRequirement requirement,
        CompanionRoleGateOutcome outcome,
        string reason,
        IEnumerable<CandidateEvidenceReference> evidence) =>
        new(requirement, outcome, reason, evidence);

    private static IReadOnlyList<CandidateProfileFact> CapabilityFacts(
        CandidateProfile profile,
        CompanionCapabilitySummary summary) =>
        [.. summary.MainAttributes.Components
            .Concat(summary.MartialDisciplines.Components)
            .Concat(summary.LifeSkillDisciplines.Components)
            .Select(component => profile.FindFact(component.Field))
            .OfType<CandidateProfileFact>()];

    private static CompanionRoleGateOutcome CapabilityOutcome(
        CompanionCapabilitySummaryState state) => state switch
        {
            CompanionCapabilitySummaryState.Complete =>
                CompanionRoleGateOutcome.Passed,
            CompanionCapabilitySummaryState.Incomplete or
                CompanionCapabilitySummaryState.Stale =>
                CompanionRoleGateOutcome.Incomplete,
            CompanionCapabilitySummaryState.Unsupported =>
                CompanionRoleGateOutcome.Unsupported,
            CompanionCapabilitySummaryState.Conflicting =>
                CompanionRoleGateOutcome.Conflicting,
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown capability summary state.")
        };

    private static bool ProvenanceMatchesProfile(
        CandidateProfileFact fact,
        CandidateProfile profile) =>
        fact.Provenance is { } provenance
        && provenance.SourceKind == CandidateEvidenceSourceKind.ConfiguredSave
        && string.Equals(
            provenance.RevisionIdentity,
            profile.SourceVersions.SaveSha256,
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            provenance.SourceVersion,
            profile.SourceVersions.ProfileMappingVersion,
            StringComparison.Ordinal);

    private static CompanionRoleGateOutcome FactOutcome(
        CandidateProfileFact? fact,
        CompanionRoleScoreDimension dimension)
    {
        if (fact is null)
        {
            return dimension.MissingEvidenceBehavior == CompanionRoleMissingEvidenceBehavior.EvaluationUnsupported
                ? CompanionRoleGateOutcome.Unsupported
                : CompanionRoleGateOutcome.Incomplete;
        }

        return fact.State switch
        {
            CandidateEvidenceState.Confirmed => CompanionRoleGateOutcome.Passed,
            CandidateEvidenceState.Incomplete => CompanionRoleGateOutcome.Incomplete,
            CandidateEvidenceState.Unsupported => CompanionRoleGateOutcome.Unsupported,
            CandidateEvidenceState.Stale => CompanionRoleGateOutcome.Incomplete,
            CandidateEvidenceState.Conflicting => CompanionRoleGateOutcome.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact.State, "Unknown candidate evidence state.")
        };
    }

    private static string FactReason(
        CandidateProfileFact? fact,
        CompanionRoleGateOutcome outcome) => fact is null
        ? outcome == CompanionRoleGateOutcome.Unsupported
            ? "REQUIRED_FACT_NOT_SUPPORTED"
            : "REQUIRED_FACT_MISSING"
        : fact.State switch
        {
            CandidateEvidenceState.Confirmed => "REQUIRED_FACT_CONFIRMED",
            CandidateEvidenceState.Incomplete => "REQUIRED_FACT_INCOMPLETE",
            CandidateEvidenceState.Unsupported => "REQUIRED_FACT_UNSUPPORTED",
            CandidateEvidenceState.Stale => "REQUIRED_FACT_STALE",
            CandidateEvidenceState.Conflicting => "REQUIRED_FACT_CONFLICTING",
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact.State, "Unknown candidate evidence state.")
        };

    private static decimal? Normalize(
        CompanionRoleScoreDimension dimension,
        short rawValue)
    {
        if (rawValue < dimension.NormalizationMinimum
            || rawValue > dimension.NormalizationMaximum)
        {
            return null;
        }

        return dimension.Normalization switch
        {
            CompanionRoleNormalizationKind.Identity => rawValue,
            CompanionRoleNormalizationKind.Hundredth => rawValue / 100m,
            _ => throw new ArgumentOutOfRangeException(
                nameof(dimension),
                dimension.Normalization,
                "Unknown normalization rule.")
        };
    }

    private static CompanionRoleEvaluation Unranked(
        CompanionRoleDefinition definition,
        CandidateProfile profile,
        CandidateDisciplineIdentity discipline,
        IEnumerable<CompanionRoleGateEvaluation> gates,
        CompanionRoleGateOutcome outcome)
    {
        var state = outcome switch
        {
            CompanionRoleGateOutcome.Failed => CompanionRoleEvaluationState.Ineligible,
            CompanionRoleGateOutcome.Incomplete => CompanionRoleEvaluationState.Incomplete,
            CompanionRoleGateOutcome.Unsupported => CompanionRoleEvaluationState.Unsupported,
            CompanionRoleGateOutcome.Conflicting => CompanionRoleEvaluationState.Conflicting,
            _ => throw new ArgumentException("A passing gate cannot create an unranked evaluation.", nameof(outcome))
        };
        return new CompanionRoleEvaluation(
            definition,
            profile,
            discipline,
            state,
            gates,
            [],
            totalScore: null,
            $"ROLE_EVALUATION_{state.ToString().ToUpperInvariant()}");
    }
}
