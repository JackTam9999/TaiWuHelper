using TaiWu.Application.CombatSkills;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Contracts.CompanionCandidates;

public static class CompanionFinderResponseMapper
{
    public static CompanionRoleDiscoveryResponse MapRoles(TaiwuLanguage language)
    {
        ValidateLanguage(language);
        return new CompanionRoleDiscoveryResponse(
            language,
            [.. VerifiedCompanionRoleDefinitions.All.Select(definition =>
                new CompanionRolePresetResponse(
                    $"companion-role:{definition.Identity.Value}:{definition.RoleVersion}",
                    definition.Identity.Value,
                    definition.RoleVersion,
                    definition.EvaluationRuleVersion,
                    CompanionRolePresetStatus.Supported,
                    definition.DisciplineDomain,
                    definition.MinimumDisciplineType,
                    definition.MaximumDisciplineType,
                    CompanionFinderApiText.RolePurpose(language, definition.Identity),
                    CompanionFinderApiText.ScoreLimitation(language))) ]);
    }

    public static CompanionFinderResponse Map(
        CompanionFinderResult result,
        TaiwuLanguage language)
    {
        ArgumentNullException.ThrowIfNull(result);
        ValidateLanguage(language);
        var failure = result.FailureIdentity is null
            ? null
            : new CompanionFinderFailureResponse(
                result.FailureIdentity,
                CompanionFinderApiText.Failure(language, result.FailureIdentity));
        if (!result.HasAuthoritativeResult)
        {
            return new CompanionFinderResponse(
                result.Status,
                failure,
                Fingerprint: null,
                Source: null,
                Role: null,
                Enrichment: null,
                Counts: null,
                Candidates: [],
                VisibleCandidateReferences: [],
                Comparison: null,
                Diagnostics: []);
        }

        var shortlist = result.Shortlist!;
        var enrichmentByCharacter = result.Enrichment!.Candidates.ToDictionary(
            item => item.Profile.Identity.CharacterId);
        var displayByCharacter = result.Snapshot!.Displays.ToDictionary(
            item => item.Identity.CharacterId);
        var candidates = shortlist.Entries
            .Select(entry => MapCandidate(
                entry,
                enrichmentByCharacter[entry.Evaluation.Profile.Identity.CharacterId],
                displayByCharacter.GetValueOrDefault(
                    entry.Evaluation.Profile.Identity.CharacterId),
                language))
            .ToArray();
        var diagnostics = MapRootDiagnostics(result, language);
        var versions = result.SourceIdentity!.CandidateSourceVersions;
        return new CompanionFinderResponse(
            result.Status,
            failure,
            result.Fingerprint,
            new CompanionFinderSourceResponse(
                result.SourceIdentity.SnapshotCapturedAtUtc,
                versions.SaveSha256,
                versions.GameDataVersion,
                versions.ProfileMappingVersion,
                versions.DisciplineCatalogVersion,
                versions.FingerprintSchemaVersion,
                result.SourceIdentity.CatalogueStatus,
                MapCatalogueSource(result.SourceIdentity.CatalogueSource)),
            MapRole(result, language),
            new CompanionEnrichmentSummaryResponse(
                result.Enrichment.Status,
                result.Enrichment.CatalogueStatus),
            new CompanionShortlistCountsResponse(
                shortlist.Counts.Total,
                shortlist.Counts.Ranked,
                shortlist.Counts.Tied,
                shortlist.Counts.Ineligible,
                shortlist.Counts.Incomplete,
                shortlist.Counts.Unsupported,
                shortlist.Counts.Conflicting,
                result.View!.VisibleCount),
            candidates,
            [.. result.View.Entries.Select(entry => CandidateReference(
                entry.Evaluation.Profile.Identity.CharacterId))],
            result.Comparison is null
                ? null
                : MapComparison(result.Comparison, language),
            diagnostics);
    }

    private static CompanionRoleContextResponse MapRole(
        CompanionFinderResult result,
        TaiwuLanguage language)
    {
        var definition = result.Ranking!.Definition;
        return new CompanionRoleContextResponse(
            $"companion-role:{definition.Identity.Value}:{definition.RoleVersion}",
            definition.Identity.Value,
            definition.RoleVersion,
            definition.EvaluationRuleVersion,
            result.Ranking.Discipline.Domain,
            result.Ranking.Discipline.Type,
            CompanionFinderApiText.RolePurpose(language, definition.Identity),
            CompanionFinderApiText.ScoreLimitation(language));
    }

    private static CompanionCandidateResponse MapCandidate(
        CompanionRoleShortlistEntry entry,
        CompanionCandidateEnrichment enrichment,
        CompanionCandidateDisplay? display,
        TaiwuLanguage language)
    {
        var evaluation = entry.Evaluation;
        var definition = evaluation.Definition;
        var scoreFacts = definition.ScoreDimensions.Select(dimension =>
        {
            var field = new CandidateProfileFieldIdentity(
                dimension.Field,
                evaluation.Discipline);
            return MapFact(evaluation.Profile.FindFact(field), field, language);
        });
        return new CompanionCandidateResponse(
            CandidateReference(evaluation.Profile.Identity.CharacterId),
            evaluation.Profile.Identity.CharacterId,
            DisplayName(display, language),
            LocationName(display, language),
            entry.Candidate.State,
            CompanionFinderApiText.RankingState(language, entry.Candidate.State),
            entry.Candidate.CompetitionRank,
            evaluation.State,
            evaluation.TotalScore,
            [.. evaluation.Gates.Select(gate => MapGate(gate, language))],
            [.. evaluation.Components.Select(component => MapComponent(component, language))],
            [.. entry.Explanations.Select(explanation =>
                new CompanionExplanationResponse(
                    explanation.Kind,
                    explanation.Identity,
                    CompanionFinderApiText.Explanation(language, explanation.Identity),
                    [.. explanation.Components.Select(item => item.Dimension.Identity)],
                    [.. explanation.Gates.Select(item => item.ReasonIdentity)]))],
            [.. scoreFacts],
            [.. entry.LocationEvidence.Select(fact => MapFact(
                fact,
                fact.Identity,
                language))],
            [.. entry.AvailableLocationFacts.Select(fact => MapFact(
                fact,
                fact.Identity,
                language))],
            MapEnrichment(enrichment, language),
            [.. entry.ProfileDiagnostics.Select(diagnostic =>
                new CompanionApiDiagnosticResponse(
                    "candidate-profile",
                    diagnostic.Code,
                    MapSeverity(diagnostic.Severity),
                    CompanionFinderApiText.Diagnostic(language, diagnostic.Code),
                    CandidateReference(evaluation.Profile.Identity.CharacterId))) ]);
    }

    private static string? DisplayName(
        CompanionCandidateDisplay? display,
        TaiwuLanguage language) => language switch
        {
            TaiwuLanguage.Chinese => display?.TraditionalChineseName,
            TaiwuLanguage.English => display?.EnglishName,
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.")
        };

    private static string? LocationName(
        CompanionCandidateDisplay? display,
        TaiwuLanguage language) => language switch
        {
            TaiwuLanguage.Chinese => display?.TraditionalChineseLocation,
            TaiwuLanguage.English => display?.EnglishLocation,
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown Taiwu language.")
        };

    private static CompanionGateResponse MapGate(
        CompanionRoleGateEvaluation gate,
        TaiwuLanguage language) => new(
            gate.Requirement.Order,
            gate.Requirement.Identity,
            gate.Requirement.Kind,
            gate.Requirement.Field,
            gate.Outcome,
            CompanionFinderApiText.GateOutcome(language, gate.Outcome),
            gate.ReasonIdentity,
            CompanionFinderApiText.GateReason(language, gate.ReasonIdentity),
            [.. gate.Evidence.Select(MapEvidence)]);

    private static CompanionScoreComponentResponse MapComponent(
        CompanionRoleScoreComponent component,
        TaiwuLanguage language) => new(
            component.Dimension.Identity,
            component.Field.Field,
            component.Field.Discipline!.Domain,
            component.Field.Discipline.Type,
            component.Dimension.Unit,
            component.Dimension.Direction,
            component.Dimension.Normalization,
            component.Dimension.NormalizationMinimum,
            component.Dimension.NormalizationMaximum,
            component.RawValue,
            component.NormalizedValue,
            component.Weight,
            component.Contribution,
            component.Dimension.ExplanationIdentity,
            CompanionFinderApiText.Explanation(
                language,
                component.Dimension.ExplanationIdentity),
            [.. component.Evidence.Select(MapEvidence)]);

    private static CompanionRoleFactResponse MapFact(
        CandidateProfileFact? fact,
        CandidateProfileFieldIdentity field,
        TaiwuLanguage language) => new(
            field.Field,
            field.Discipline?.Domain,
            field.Discipline?.Type,
            MapEvidenceState(fact),
            fact is { State: CandidateEvidenceState.Confirmed, Value: { } value }
                ? MapValue(value)
                : null,
            fact?.Provenance is null ? null : MapProvenance(fact.Provenance),
            fact?.UnavailableReason is null
                ? null
                : new CompanionUnavailableResponse(
                    fact.UnavailableReason.Code,
                    CompanionFinderApiText.Unavailable(
                        language,
                        fact.UnavailableReason.Code)),
            fact is null
                ? []
                : [.. fact.Conflicts.Select(MapConflict)],
            fact?.ConflictDecision is null
                ? null
                : new CompanionConflictDecisionResponse(
                    fact.ConflictDecision.Kind,
                    fact.ConflictDecision.RationaleCode,
                    fact.ConflictDecision.SelectedProvenance is null
                        ? null
                        : MapProvenance(fact.ConflictDecision.SelectedProvenance)),
            fact is null ? [] : [.. fact.Evidence.Select(MapEvidence)]);

    private static CompanionFactValueResponse MapValue(CandidateFactValue value) =>
        value.Kind switch
        {
            CandidateFactValueKind.Boolean => new(
                value.Kind,
                value.BooleanValue,
                Int16: null,
                Int32: null,
                Identities: []),
            CandidateFactValueKind.Int16 => new(
                value.Kind,
                Boolean: null,
                value.Int16Value,
                Int32: null,
                Identities: []),
            CandidateFactValueKind.Int32 => new(
                value.Kind,
                Boolean: null,
                Int16: null,
                value.Int32Value,
                Identities: []),
            CandidateFactValueKind.Int32Set => new(
                value.Kind,
                Boolean: null,
                Int16: null,
                Int32: null,
                value.Identities),
            _ => throw new ArgumentOutOfRangeException(nameof(value), value.Kind, "Unknown candidate fact value kind.")
        };

    private static CompanionFactEvidenceState MapEvidenceState(
        CandidateProfileFact? fact) => fact?.State switch
        {
            null => CompanionFactEvidenceState.Missing,
            CandidateEvidenceState.Confirmed => CompanionFactEvidenceState.Confirmed,
            CandidateEvidenceState.Incomplete => CompanionFactEvidenceState.Incomplete,
            CandidateEvidenceState.Unsupported => CompanionFactEvidenceState.Unsupported,
            CandidateEvidenceState.Stale => CompanionFactEvidenceState.Stale,
            CandidateEvidenceState.Conflicting => CompanionFactEvidenceState.Conflicting,
            _ => throw new ArgumentOutOfRangeException(
                nameof(fact),
                fact.State,
                "Unknown candidate evidence state.")
        };

    private static CompanionConflictValueResponse MapConflict(
        CandidateConflictValue conflict) => new(
            MapValue(conflict.Value),
            MapProvenance(conflict.Provenance),
            [.. conflict.Evidence.Select(MapEvidence)]);

    private static CompanionProvenanceResponse MapProvenance(
        CandidateFactProvenance provenance) => new(
            provenance.SourceKind,
            provenance.SourceIdentity,
            provenance.SourceVersion,
            provenance.RevisionIdentity);

    private static CompanionEvidenceResponse MapEvidence(
        CandidateEvidenceReference evidence) => new(
            evidence.Reference,
            MapProvenance(evidence.Provenance));

    private static CompanionCandidateEnrichmentResponse MapEnrichment(
        CompanionCandidateEnrichment enrichment,
        TaiwuLanguage language) => new(
            enrichment.State,
            enrichment.LearnedMartialState,
            enrichment.EquippedMartialState,
            enrichment.LearnedLifeSkillState,
            [.. enrichment.CombatSkills.Select(skill =>
                new CompanionSkillEnrichmentResponse(
                    skill.SkillId,
                    skill.DefinitionState,
                    skill.DetailedProgressState,
                    new CompanionMembershipResponse(
                        skill.Learned.State,
                        skill.Learned.Value),
                    new CompanionMembershipResponse(
                        skill.Equipped.State,
                        skill.Equipped.Value)))],
            [.. enrichment.Diagnostics.Select(diagnostic =>
                MapSnapshotDiagnostic(
                    "candidate-enrichment",
                    diagnostic,
                    language))]);

    private static CompanionComparisonResponse MapComparison(
        CompanionRoleComparison comparison,
        TaiwuLanguage language) => new(
            CandidateReference(comparison.First.Evaluation.Profile.Identity.CharacterId),
            CandidateReference(comparison.Second.Evaluation.Profile.Identity.CharacterId),
            comparison.Outcome,
            CompanionFinderApiText.ComparisonOutcome(language, comparison.Outcome),
            [.. comparison.Rows.Select(row =>
                new CompanionComparisonRowResponse(
                    row.Dimension.Identity,
                    row.Field.Field,
                    row.Outcome,
                    CompanionFinderApiText.ComparisonOutcome(language, row.Outcome),
                    MapComparisonValue(row.First, row.Field, language),
                    MapComparisonValue(row.Second, row.Field, language))) ]);

    private static CompanionComparisonValueResponse MapComparisonValue(
        CompanionRoleComparisonValue value,
        CandidateProfileFieldIdentity field,
        TaiwuLanguage language) => new(
            value.State,
            value.Value,
            value.Fact is null ? null : MapFact(value.Fact, field, language));

    private static CompanionCatalogueSourceResponse? MapCatalogueSource(
        CombatSkillCatalogueSourceIdentity? source) => source is null
        ? null
        : new CompanionCatalogueSourceResponse(
            source.GameDataVersion,
            source.ImporterVersion,
            source.GameDataFingerprint,
            source.TraditionalChineseFingerprint,
            source.EnglishFingerprint,
            source.TraditionalChineseSpecialEffectFingerprint,
            source.EnglishSpecialEffectFingerprint);

    private static IReadOnlyList<CompanionApiDiagnosticResponse> MapRootDiagnostics(
        CompanionFinderResult result,
        TaiwuLanguage language)
    {
        var diagnostics = new List<CompanionApiDiagnosticResponse>();
        diagnostics.AddRange(result.Snapshot!.Warnings.Select(warning =>
            new CompanionApiDiagnosticResponse(
                "candidate-snapshot",
                $"SNAPSHOT_WARNING_{warning.Kind.ToString().ToUpperInvariant()}",
                CompanionApiDiagnosticSeverity.Warning,
                CompanionFinderApiText.Diagnostic(
                    language,
                    $"SNAPSHOT_WARNING_{warning.Kind.ToString().ToUpperInvariant()}"),
                CandidateReference: null)));
        diagnostics.AddRange(result.Snapshot.Omissions.Select(omission =>
            new CompanionApiDiagnosticResponse(
                "candidate-snapshot",
                omission.ReasonIdentity,
                CompanionApiDiagnosticSeverity.Warning,
                CompanionFinderApiText.Diagnostic(language, omission.ReasonIdentity),
                omission.CharacterId.HasValue
                    ? CandidateReference(omission.CharacterId.Value)
                    : null)));
        diagnostics.AddRange(result.Snapshot.Diagnostics.Select(diagnostic =>
            MapSnapshotDiagnostic("candidate-snapshot", diagnostic, language)));
        diagnostics.AddRange(result.Enrichment!.Diagnostics.Select(diagnostic =>
            MapSnapshotDiagnostic("candidate-enrichment", diagnostic, language)));
        diagnostics.AddRange(result.Shortlist!.Diagnostics.Select(diagnostic =>
            new CompanionApiDiagnosticResponse(
                "shortlist",
                diagnostic.Identity,
                diagnostic.Severity == CompanionRoleShortlistDiagnosticSeverity.Information
                    ? CompanionApiDiagnosticSeverity.Information
                    : CompanionApiDiagnosticSeverity.Warning,
                CompanionFinderApiText.Diagnostic(language, diagnostic.Identity),
                CandidateReference: null)));
        return diagnostics
            .OrderBy(item => item.Scope, StringComparer.Ordinal)
            .ThenBy(item => item.Identity, StringComparer.Ordinal)
            .ThenBy(item => item.CandidateReference, StringComparer.Ordinal)
            .ToArray();
    }

    private static CompanionApiDiagnosticResponse MapSnapshotDiagnostic(
        string scope,
        CompanionCandidateSnapshotDiagnostic diagnostic,
        TaiwuLanguage language) => new(
            scope,
            diagnostic.Identity,
            diagnostic.Severity switch
            {
                CompanionCandidateSnapshotDiagnosticSeverity.Information =>
                    CompanionApiDiagnosticSeverity.Information,
                CompanionCandidateSnapshotDiagnosticSeverity.Warning =>
                    CompanionApiDiagnosticSeverity.Warning,
                CompanionCandidateSnapshotDiagnosticSeverity.Error =>
                    CompanionApiDiagnosticSeverity.Error,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(diagnostic),
                    diagnostic.Severity,
                    "Unknown snapshot diagnostic severity.")
            },
            CompanionFinderApiText.Diagnostic(language, diagnostic.Identity),
            diagnostic.Candidate is null
                ? null
                : CandidateReference(diagnostic.Candidate.CharacterId));

    private static CompanionApiDiagnosticSeverity MapSeverity(
        CandidateProfileDiagnosticSeverity severity) => severity switch
        {
            CandidateProfileDiagnosticSeverity.Information => CompanionApiDiagnosticSeverity.Information,
            CandidateProfileDiagnosticSeverity.Warning => CompanionApiDiagnosticSeverity.Warning,
            CandidateProfileDiagnosticSeverity.Error => CompanionApiDiagnosticSeverity.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown profile diagnostic severity.")
        };

    private static string CandidateReference(int characterId) =>
        $"companion-candidate:{characterId.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    private static void ValidateLanguage(TaiwuLanguage language)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown UI language.");
        }
    }
}
