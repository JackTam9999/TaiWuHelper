using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Application.CompanionCandidates;

public sealed class EnrichCompanionCandidateProfiles(
    ICombatSkillDefinitionSource definitionSource,
    ICombatSkillCatalogueRepository catalogueRepository,
    CombatSkillCatalogueMaintenanceCoordinator? coordinator = null)
{
    public async Task<CompanionCandidateEnrichmentResult> ExecuteAsync(
        CompanionCandidateSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        cancellationToken.ThrowIfCancellationRequested();

        var catalogue = await new ReadCombatSkillCatalogueStatus(
                definitionSource,
                catalogueRepository,
                coordinator)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (catalogue.Status != CombatSkillCatalogueStatus.Current)
        {
            return Build(
                snapshot,
                MapStatus(catalogue.Status),
                catalogue,
                definitions: null,
                [Diagnostic(
                    CatalogueDiagnosticIdentity(catalogue.Status),
                    CompanionCandidateSnapshotDiagnosticSeverity.Warning,
                    "Combat-skill catalogue enrichment is unavailable; saved membership evidence remains unchanged.")]);
        }

        if (!CatalogueMatchesSnapshot(catalogue, snapshot))
        {
            var unsupported = catalogue with
            {
                Status = CombatSkillCatalogueStatus.UnsupportedVersion,
                Reason = "The catalogue GameData version does not match the candidate snapshot."
            };
            return Build(
                snapshot,
                CompanionCandidateEnrichmentStatus.CatalogueUnsupported,
                unsupported,
                definitions: null,
                [Diagnostic(
                    "CATALOGUE_SNAPSHOT_VERSION_MISMATCH",
                    CompanionCandidateSnapshotDiagnosticSeverity.Error,
                    "Combat-skill catalogue and candidate snapshot versions do not match.")]);
        }

        var definitions = await catalogueRepository.QueryAsync(
                new CombatSkillCatalogueFilter(),
                cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (definitions is null
            || definitions.Any(item => item is null)
            || definitions.GroupBy(item => item.SkillId).Any(group => group.Count() > 1))
        {
            var failed = catalogue with
            {
                Status = CombatSkillCatalogueStatus.RepositoryFailed,
                Reason = "The current combat-skill catalogue query failed validation."
            };
            return Build(
                snapshot,
                CompanionCandidateEnrichmentStatus.CatalogueFailed,
                failed,
                definitions: null,
                [Diagnostic(
                    "CATALOGUE_QUERY_FAILED",
                    CompanionCandidateSnapshotDiagnosticSeverity.Error,
                    "The current combat-skill catalogue could not be queried safely.")]);
        }

        return Build(
            snapshot,
            CompanionCandidateEnrichmentStatus.Complete,
            catalogue,
            definitions,
            []);
    }

    private static CompanionCandidateEnrichmentResult Build(
        CompanionCandidateSnapshot snapshot,
        CompanionCandidateEnrichmentStatus requestedStatus,
        CombatSkillCatalogueStatusResult catalogue,
        IReadOnlyList<CombatSkillDefinition>? definitions,
        IEnumerable<CompanionCandidateSnapshotDiagnostic> resultDiagnostics)
    {
        var bySkillId = definitions?.ToDictionary(item => item.SkillId);
        var catalogueAvailable = bySkillId is not null;
        var candidates = snapshot.Profiles
            .Select(profile => BuildCandidate(
                profile,
                snapshot.SourceVersions,
                bySkillId,
                catalogueAvailable,
                requestedStatus))
            .OrderBy(item => item.Profile.Identity.CharacterId)
            .ToArray();
        var status = requestedStatus == CompanionCandidateEnrichmentStatus.Complete
            && (snapshot.Omissions.Length > 0
                || candidates.Any(item => item.State != CompanionCandidateEnrichmentState.Complete))
            ? CompanionCandidateEnrichmentStatus.Partial
            : requestedStatus;
        return new CompanionCandidateEnrichmentResult(
            snapshot,
            status,
            catalogue.Status,
            catalogue.InstalledSource,
            candidates,
            resultDiagnostics);
    }

    private static CompanionCandidateEnrichment BuildCandidate(
        CandidateProfile profile,
        CandidateProfileSourceVersions versions,
        IReadOnlyDictionary<int, CombatSkillDefinition>? definitions,
        bool catalogueAvailable,
        CompanionCandidateEnrichmentStatus requestedStatus)
    {
        var learnedFact = profile.FindFact(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.LearnedMartialSkillIdentities));
        var equippedFact = profile.FindFact(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.EquippedMartialSkillIdentities));
        var lifeFact = profile.FindFact(
            new CandidateProfileFieldIdentity(
                CandidateProfileField.LearnedLifeSkillIdentities));
        var learned = ReadMembershipSet(learnedFact, versions);
        var equipped = ReadMembershipSet(equippedFact, versions);
        var life = ReadMembershipSet(lifeFact, versions);
        var skillIds = learned.Values
            .Concat(equipped.Values)
            .Distinct()
            .Order()
            .ToArray();
        var skills = new List<CompanionCombatSkillEnrichment>();
        var missingDefinition = false;
        foreach (var skillId in skillIds)
        {
            CombatSkillDefinition? definition = null;
            definitions?.TryGetValue(skillId, out definition);
            var definitionState = !catalogueAvailable
                ? CompanionSkillDefinitionState.CatalogueUnavailable
                : definition is null
                    ? CompanionSkillDefinitionState.Missing
                    : CompanionSkillDefinitionState.Available;
            missingDefinition |= definitionState == CompanionSkillDefinitionState.Missing;
            skills.Add(new CompanionCombatSkillEnrichment(
                skillId,
                MembershipForSkill(learned, learnedFact, skillId),
                MembershipForSkill(equipped, equippedFact, skillId),
                definitionState,
                definition));
        }

        var diagnostics = new List<CompanionCandidateSnapshotDiagnostic>();
        if (missingDefinition)
        {
            diagnostics.Add(new CompanionCandidateSnapshotDiagnostic(
                "CANDIDATE_SKILL_DEFINITION_MISSING",
                CompanionCandidateSnapshotDiagnosticSeverity.Warning,
                "One or more saved combat-skill identities have no matching current catalogue definition.",
                profile.Identity));
        }

        var evidenceComplete = learned.State == CompanionMembershipEvidenceState.Available
            && equipped.State == CompanionMembershipEvidenceState.Available
            && life.State == CompanionMembershipEvidenceState.Available;
        var state = !catalogueAvailable
            ? MapCandidateUnavailableState(requestedStatus)
            : evidenceComplete && !missingDefinition
                ? CompanionCandidateEnrichmentState.Complete
                : CompanionCandidateEnrichmentState.Partial;
        return new CompanionCandidateEnrichment(
            profile,
            state,
            learned.State,
            equipped.State,
            life.State,
            skills,
            diagnostics);
    }

    private static MembershipSet ReadMembershipSet(
        CandidateProfileFact? fact,
        CandidateProfileSourceVersions versions)
    {
        if (fact is null)
        {
            return new MembershipSet(
                CompanionMembershipEvidenceState.Incomplete,
                []);
        }

        var state = fact.State switch
        {
            CandidateEvidenceState.Confirmed => ProvenanceMatches(fact, versions)
                ? CompanionMembershipEvidenceState.Available
                : CompanionMembershipEvidenceState.Conflicting,
            CandidateEvidenceState.Incomplete => CompanionMembershipEvidenceState.Incomplete,
            CandidateEvidenceState.Unsupported => CompanionMembershipEvidenceState.Unsupported,
            CandidateEvidenceState.Stale => CompanionMembershipEvidenceState.Stale,
            CandidateEvidenceState.Conflicting => CompanionMembershipEvidenceState.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact.State, "Unknown profile evidence state.")
        };
        return state == CompanionMembershipEvidenceState.Available
            ? new MembershipSet(state, fact.Value!.Identities)
            : new MembershipSet(state, []);
    }

    private static bool ProvenanceMatches(
        CandidateProfileFact fact,
        CandidateProfileSourceVersions versions) =>
        fact.Provenance is { } provenance
        && provenance.SourceKind == CandidateEvidenceSourceKind.ConfiguredSave
        && string.Equals(
            provenance.RevisionIdentity,
            versions.SaveSha256,
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            provenance.SourceVersion,
            versions.ProfileMappingVersion,
            StringComparison.Ordinal);

    private static CompanionSkillMembershipFact MembershipForSkill(
        MembershipSet set,
        CandidateProfileFact? sourceFact,
        int skillId) => new(
            set.State,
            set.State == CompanionMembershipEvidenceState.Available
                ? set.Values.Contains(skillId)
                : null,
            sourceFact);

    private static bool CatalogueMatchesSnapshot(
        CombatSkillCatalogueStatusResult catalogue,
        CompanionCandidateSnapshot snapshot) =>
        catalogue.InstalledSource is { } installed
        && catalogue.StoredSource is { } stored
        && string.Equals(
            installed.GameDataVersion,
            snapshot.SourceVersions.GameDataVersion,
            StringComparison.Ordinal)
        && string.Equals(
            stored.GameDataVersion,
            snapshot.SourceVersions.GameDataVersion,
            StringComparison.Ordinal);

    private static CompanionCandidateEnrichmentStatus MapStatus(
        CombatSkillCatalogueStatus status) =>
        status switch
        {
            CombatSkillCatalogueStatus.Missing
                or CombatSkillCatalogueStatus.MissingSources =>
                CompanionCandidateEnrichmentStatus.CatalogueMissing,
            CombatSkillCatalogueStatus.Stale =>
                CompanionCandidateEnrichmentStatus.CatalogueStale,
            CombatSkillCatalogueStatus.Rebuilding =>
                CompanionCandidateEnrichmentStatus.CatalogueRebuilding,
            CombatSkillCatalogueStatus.UnsupportedVersion =>
                CompanionCandidateEnrichmentStatus.CatalogueUnsupported,
            CombatSkillCatalogueStatus.SourceReadFailed
                or CombatSkillCatalogueStatus.RepositoryFailed
                or CombatSkillCatalogueStatus.Corrupt =>
                CompanionCandidateEnrichmentStatus.CatalogueFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown non-current catalogue state.")
        };

    private static CompanionCandidateEnrichmentState MapCandidateUnavailableState(
        CompanionCandidateEnrichmentStatus status) =>
        status switch
        {
            CompanionCandidateEnrichmentStatus.CatalogueMissing =>
                CompanionCandidateEnrichmentState.CatalogueMissing,
            CompanionCandidateEnrichmentStatus.CatalogueStale =>
                CompanionCandidateEnrichmentState.CatalogueStale,
            CompanionCandidateEnrichmentStatus.CatalogueRebuilding =>
                CompanionCandidateEnrichmentState.CatalogueRebuilding,
            CompanionCandidateEnrichmentStatus.CatalogueUnsupported =>
                CompanionCandidateEnrichmentState.CatalogueUnsupported,
            CompanionCandidateEnrichmentStatus.CatalogueFailed =>
                CompanionCandidateEnrichmentState.CatalogueFailed,
            _ => throw new ArgumentException(
                "An available enrichment status cannot create a catalogue-unavailable candidate.",
                nameof(status))
        };

    private static string CatalogueDiagnosticIdentity(
        CombatSkillCatalogueStatus status) =>
        status switch
        {
            CombatSkillCatalogueStatus.Missing => "CATALOGUE_MISSING",
            CombatSkillCatalogueStatus.Stale => "CATALOGUE_STALE",
            CombatSkillCatalogueStatus.MissingSources => "CATALOGUE_SOURCES_MISSING",
            CombatSkillCatalogueStatus.UnsupportedVersion => "CATALOGUE_VERSION_UNSUPPORTED",
            CombatSkillCatalogueStatus.SourceReadFailed => "CATALOGUE_SOURCE_READ_FAILED",
            CombatSkillCatalogueStatus.RepositoryFailed => "CATALOGUE_REPOSITORY_FAILED",
            CombatSkillCatalogueStatus.Corrupt => "CATALOGUE_CORRUPT",
            CombatSkillCatalogueStatus.Rebuilding => "CATALOGUE_REBUILDING",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown non-current catalogue state.")
        };

    private static CompanionCandidateSnapshotDiagnostic Diagnostic(
        string identity,
        CompanionCandidateSnapshotDiagnosticSeverity severity,
        string message) => new(identity, severity, message);

    private sealed record MembershipSet(
        CompanionMembershipEvidenceState State,
        IReadOnlySet<int> Values)
    {
        public MembershipSet(
            CompanionMembershipEvidenceState state,
            IEnumerable<int> values)
            : this(state, values.ToHashSet())
        {
        }
    }
}
