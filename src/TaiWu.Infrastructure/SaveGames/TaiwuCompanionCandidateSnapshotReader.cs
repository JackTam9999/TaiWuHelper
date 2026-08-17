using GameData.Domains;
using GameData.Domains.Character;
using System.Diagnostics;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuCompanionCandidateSnapshotReader(
    TaiwuArchiveReadSession readSession,
    ITaiwuSaveFilePathProvider saveFilePathProvider,
    TimeProvider timeProvider) : ICompanionCandidateSnapshotReader
{
    public async Task<CompanionCandidateSnapshotReadResult> ReadAsync(
        CompanionCandidateSnapshotReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var located = saveFilePathProvider.Resolve();
        if (!located.IsAvailable)
        {
            return Failure(
                CompanionCandidateSnapshotReadStatus.SaveUnavailable,
                "CONFIGURED_SAVE_UNAVAILABLE",
                "The trusted Taiwu save configuration is unavailable.");
        }

        var gameDataVersion = GetGameDataVersion();
        if (!string.Equals(
                gameDataVersion,
                VerifiedCompanionRoleDefinitions.SupportedGameDataVersion,
                StringComparison.Ordinal))
        {
            return Failure(
                CompanionCandidateSnapshotReadStatus.UnsupportedVersion,
                "GAMEDATA_VERSION_UNSUPPORTED",
                "The installed GameData version is not supported by the verified companion mapping.");
        }

        try
        {
            var projection = await readSession.ReadAsync(
                    located.SaveFilePath!,
                    (context, token) => Project(
                        context,
                        gameDataVersion,
                        timeProvider.GetUtcNow(),
                        token),
                    cancellationToken)
                .ConfigureAwait(false);
            return projection.IsPartial
                ? CompanionCandidateSnapshotReadResult.Partial(projection.Snapshot)
                : CompanionCandidateSnapshotReadResult.Complete(projection.Snapshot);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return Failure(
                CompanionCandidateSnapshotReadStatus.SaveUnavailable,
                "CONFIGURED_SAVE_NOT_FOUND",
                "The configured Taiwu save file was not found.");
        }
        catch (TaiwuArchiveChangedException)
        {
            return Failure(
                CompanionCandidateSnapshotReadStatus.ChangedRevision,
                "SAVE_REVISION_CHANGED",
                "The configured save changed during the read; retry after it is stable.");
        }
        catch (Exception exception) when (IsSafeReadFailure(exception))
        {
            return Failure(
                CompanionCandidateSnapshotReadStatus.ReadFailed,
                "CONFIGURED_SAVE_READ_FAILED",
                "The configured Taiwu save could not be read safely.");
        }
    }

    private static CompanionCandidateProjection Project(
        TaiwuArchiveReadContext context,
        string gameDataVersion,
        DateTimeOffset capturedAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var versions = new CandidateProfileSourceVersions(
            context.SourceFingerprint.Sha256,
            gameDataVersion,
            CompanionCandidateSnapshotMapping.ProfileMappingVersion,
            CompanionCandidateSnapshotMapping.DisciplineCatalogVersion,
            CompanionCandidateSnapshotMapping.FingerprintSchemaVersion);
        var profiles = new List<CandidateProfile>();
        var omissions = new List<CompanionCandidateOmission>();
        var warnings = new List<CompanionCandidateSnapshotWarning>();
        var diagnostics = new List<CompanionCandidateSnapshotDiagnostic>();
        var partial = false;

        if (context.LoadWarning is not null)
        {
            warnings.Add(MapWarning(context.LoadWarning));
        }

        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        var roster = DomainManager.Taiwu.GetGroupCharIds().GetCollection()
            .OrderBy(value => value)
            .ToArray();
        if (roster.Length != roster.Distinct().Count())
        {
            partial = true;
            diagnostics.Add(new CompanionCandidateSnapshotDiagnostic(
                "DUPLICATE_ROSTER_IDENTITY",
                CompanionCandidateSnapshotDiagnosticSeverity.Error,
                "The saved group roster contained a duplicate character identity."));
        }

        foreach (var characterId in roster.Distinct().Order())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (characterId == taiwuId)
            {
                continue;
            }

            if (characterId <= 0)
            {
                partial = true;
                omissions.Add(new CompanionCandidateOmission(
                    characterId: null,
                    "INVALID_ROSTER_IDENTITY",
                    "A saved group entry was omitted because its character identity is invalid."));
                continue;
            }

            try
            {
                var raw = ReadCandidate(characterId, cancellationToken);
                var mapped = CompanionCandidateSnapshotMapping.Map(raw, versions);
                profiles.Add(mapped.Profile);
                diagnostics.AddRange(mapped.Diagnostics);
                partial |= mapped.IsPartial;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (IsCandidateMappingFailure(exception))
            {
                partial = true;
                omissions.Add(new CompanionCandidateOmission(
                    characterId,
                    "CANDIDATE_MAPPING_FAILED",
                    "One saved candidate was omitted because its facts could not be mapped safely."));
            }
        }

        var snapshot = new CompanionCandidateSnapshot(
            capturedAt,
            versions,
            profiles,
            omissions,
            warnings,
            diagnostics);
        return new CompanionCandidateProjection(snapshot, partial);
    }

    private static RawCompanionCandidate ReadCandidate(
        int characterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool? domainMembership = null;
        try
        {
            domainMembership = DomainManager.Taiwu.IsInGroup(characterId);
            if (!DomainManager.Character.TryGetElement_Objects(
                    characterId,
                    out Character character))
            {
                return MissingCharacter(characterId, domainMembership);
            }

            var location = character.GetLocation();
            var martial = new short[
                CompanionCandidateSnapshotMapping.MartialDisciplineCount];
            for (sbyte type = 0; type < martial.Length; type++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                martial[type] = character
                    .GetBaseCombatSkillQualifications()[type];
            }

            var lifeSkills = new short[
                CompanionCandidateSnapshotMapping.LifeSkillDisciplineCount];
            for (sbyte type = 0; type < lifeSkills.Length; type++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lifeSkills[type] = character
                    .GetBaseLifeSkillQualifications()[type];
            }

            return new RawCompanionCandidate(
                characterId,
                characterPresent: true,
                domainMembership,
                character.IsInTaiwuGroup(),
                DomainManager.Character.IsCharacterAlive(characterId),
                character.GetCurrAge(),
                location.AreaId,
                location.BlockId,
                character.GetFeatureIds().Select(value => (int)value),
                martial,
                character.GetLearnedCombatSkills().Select(value => (int)value),
                character.GetEquippedCombatSkills()
                    .Where(value => value >= 0)
                    .Distinct()
                    .Select(value => (int)value),
                lifeSkills,
                character.GetLearnedLifeSkills().Select(item =>
                    (int)item.SkillTemplateId));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsCandidateMappingFailure(exception))
        {
            return new RawCompanionCandidate(
                characterId,
                characterPresent: true,
                domainMembership,
                characterGroupMembership: null,
                livingState: null,
                currentAge: null,
                locationArea: null,
                locationBlock: null,
                featureIdentities: null,
                baseMartialQualifications: null,
                learnedMartialSkillIdentities: null,
                equippedMartialSkillIdentities: null,
                baseLifeSkillQualifications: null,
                learnedLifeSkillIdentities: null,
                failureIdentity: "CANDIDATE_SOURCE_READ_FAILED");
        }
    }

    private static RawCompanionCandidate MissingCharacter(
        int characterId,
        bool? domainMembership) => new(
            characterId,
            characterPresent: false,
            domainMembership,
            characterGroupMembership: null,
            livingState: null,
            currentAge: null,
            locationArea: null,
            locationBlock: null,
            featureIdentities: null,
            baseMartialQualifications: null,
            learnedMartialSkillIdentities: null,
            equippedMartialSkillIdentities: null,
            baseLifeSkillQualifications: null,
            learnedLifeSkillIdentities: null,
            failureIdentity: "CANDIDATE_CHARACTER_MISSING");

    private static CompanionCandidateSnapshotWarning MapWarning(
        TaiwuArchiveLoadWarning warning)
    {
        var kind = warning.Code switch
        {
            TaiwuArchiveLoadWarning.StandaloneEventRuntimeUnavailable =>
                CompanionCandidateSnapshotWarningKind.StandaloneEventRuntimeUnavailable,
            TaiwuArchiveLoadWarning.StandaloneLiveRuntimeUnavailable =>
                CompanionCandidateSnapshotWarningKind.StandaloneLiveRuntimeUnavailable,
            _ => CompanionCandidateSnapshotWarningKind.ArchiveLoadWarning
        };
        return new CompanionCandidateSnapshotWarning(
            kind,
            "The archive reached an expected standalone-runtime boundary after required read-only domains loaded.");
    }

    private static CompanionCandidateSnapshotReadResult Failure(
        CompanionCandidateSnapshotReadStatus status,
        string identity,
        string message) =>
        CompanionCandidateSnapshotReadResult.Failed(status, identity, message);

    private static string GetGameDataVersion()
    {
        var version = FileVersionInfo.GetVersionInfo(
                typeof(DomainManager).Assembly.Location)
            .ProductVersion;
        return string.IsNullOrWhiteSpace(version) ? "unknown" : version;
    }

    private static bool IsSafeReadFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException
            or KeyNotFoundException
            or ArgumentException
            or InvalidOperationException;

    private static bool IsCandidateMappingFailure(Exception exception) =>
        exception is ArgumentException
            or InvalidOperationException
            or IndexOutOfRangeException
            or KeyNotFoundException
            or NullReferenceException;

    private sealed record CompanionCandidateProjection(
        CompanionCandidateSnapshot Snapshot,
        bool IsPartial);
}
