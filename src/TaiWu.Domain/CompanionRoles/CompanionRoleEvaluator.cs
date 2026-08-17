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
            disciplineSupported ? "DISCIPLINE_SUPPORTED" : "DISCIPLINE_UNSUPPORTED",
            []));
        if (!disciplineSupported)
        {
            return Unranked(definition, profile, discipline, gates, disciplineOutcome);
        }

        foreach (var dimension in definition.ScoreDimensions)
        {
            var field = new CandidateProfileFieldIdentity(dimension.Field, discipline);
            var fact = profile.FindFact(field);
            var factOutcome = FactOutcome(fact, dimension);
            gates.Add(Gate(
                definition.HardRequirements[requirementIndex++],
                factOutcome,
                FactReason(fact, factOutcome),
                fact?.Evidence ?? []));
            if (factOutcome != CompanionRoleGateOutcome.Passed)
            {
                return Unranked(definition, profile, discipline, gates, factOutcome);
            }

            var provenanceMatches = fact!.Provenance!.SourceKind
                    == CandidateEvidenceSourceKind.ConfiguredSave
                && string.Equals(
                    fact.Provenance.RevisionIdentity,
                    profile.SourceVersions.SaveSha256,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    fact.Provenance.SourceVersion,
                    profile.SourceVersions.ProfileMappingVersion,
                    StringComparison.Ordinal);
            var provenanceOutcome = provenanceMatches
                ? CompanionRoleGateOutcome.Passed
                : CompanionRoleGateOutcome.Conflicting;
            gates.Add(Gate(
                definition.HardRequirements[requirementIndex++],
                provenanceOutcome,
                provenanceMatches ? "FACT_PROVENANCE_MATCHES_PROFILE" : "FACT_PROVENANCE_CONFLICTS_WITH_PROFILE",
                fact.Evidence));
            if (!provenanceMatches)
            {
                return Unranked(definition, profile, discipline, gates, provenanceOutcome);
            }

            var rawValue = fact.Value!.Int16Value;
            var normalized = Normalize(dimension, rawValue);
            if (normalized is null)
            {
                gates[^1] = Gate(
                    gates[^1].Requirement,
                    CompanionRoleGateOutcome.Conflicting,
                    "FACT_OUTSIDE_NORMALIZATION_RANGE",
                    fact.Evidence);
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
                _ => throw new ArgumentOutOfRangeException(
                    nameof(dimension),
                    dimension.Direction,
                    "Unknown score direction.")
            };
            var contribution = checked(directionalValue * dimension.Weight);
            components.Add(new CompanionRoleScoreComponent(
                dimension,
                field,
                rawValue,
                normalized.Value,
                contribution,
                fact.Evidence));
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
