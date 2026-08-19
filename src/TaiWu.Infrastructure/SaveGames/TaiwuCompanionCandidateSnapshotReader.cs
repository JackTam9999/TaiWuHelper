using GameData.Domains;
using GameData.Domains.Character;
using System.Diagnostics;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuCompanionCandidateSnapshotReader(
    TaiwuArchiveReadSession readSession,
    ITaiwuSaveFilePathProvider saveFilePathProvider,
    TaiwuGameTextResolver gameTextResolver,
    IReadOnlyFileRevisionProvider revisionProvider,
    TimeProvider timeProvider) : ICompanionCandidateSnapshotReader
{
    private readonly object _cacheGate = new();
    private readonly SemaphoreSlim _readGate = new(1, 1);
    private CachedSnapshot? _cachedSnapshot;

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

        await _readGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var saveFilePath = located.SaveFilePath!;
            var revisionBefore = revisionProvider.Capture(saveFilePath);
            var cached = TryReadCache(
                saveFilePath,
                revisionBefore,
                gameDataVersion);
            if (cached is not null
                && revisionBefore == revisionProvider.Capture(saveFilePath))
            {
                return cached;
            }

            var projection = await readSession.ReadAsync(
                    saveFilePath,
                    (context, token) => Project(
                        context,
                        gameDataVersion,
                        timeProvider.GetUtcNow(),
                        gameTextResolver,
                        token),
                    cancellationToken)
                .ConfigureAwait(false);
            var result = projection.IsPartial
                ? CompanionCandidateSnapshotReadResult.Partial(projection.Snapshot)
                : CompanionCandidateSnapshotReadResult.Complete(projection.Snapshot);
            var revisionAfter = revisionProvider.Capture(saveFilePath);
            if (revisionBefore == revisionAfter)
            {
                StoreCache(saveFilePath, revisionAfter, gameDataVersion, result);
            }

            return result;
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
        finally
        {
            _readGate.Release();
        }
    }

    private CompanionCandidateSnapshotReadResult? TryReadCache(
        string saveFilePath,
        ReadOnlyFileRevision revision,
        string gameDataVersion)
    {
        lock (_cacheGate)
        {
            return _cachedSnapshot is { } cached
                && PathEquals(cached.SaveFilePath, saveFilePath)
                && cached.Revision == revision
                && string.Equals(
                    cached.GameDataVersion,
                    gameDataVersion,
                    StringComparison.Ordinal)
                && cached.ProfileMappingVersion
                    == CompanionCandidateSnapshotMapping.ProfileMappingVersion
                && cached.FingerprintSchemaVersion
                    == CompanionCandidateSnapshotMapping.FingerprintSchemaVersion
                    ? cached.Result
                    : null;
        }
    }

    private void StoreCache(
        string saveFilePath,
        ReadOnlyFileRevision revision,
        string gameDataVersion,
        CompanionCandidateSnapshotReadResult result)
    {
        lock (_cacheGate)
        {
            _cachedSnapshot = new CachedSnapshot(
                saveFilePath,
                revision,
                gameDataVersion,
                CompanionCandidateSnapshotMapping.ProfileMappingVersion,
                CompanionCandidateSnapshotMapping.FingerprintSchemaVersion,
                result);
        }
    }

    private static bool PathEquals(string first, string second) => string.Equals(
        first,
        second,
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal);

    private static CompanionCandidateProjection Project(
        TaiwuArchiveReadContext context,
        string gameDataVersion,
        DateTimeOffset capturedAt,
        TaiwuGameTextResolver textResolver,
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
        var displays = new List<CompanionCandidateDisplay>();
        var partial = false;
        var traditionalChinese = textResolver.CreateContext(
            context.SaveFilePath,
            TaiwuLanguage.Chinese);
        var english = textResolver.CreateContext(
            context.SaveFilePath,
            TaiwuLanguage.English);

        if (context.LoadWarning is not null)
        {
            warnings.Add(MapWarning(context.LoadWarning));
        }

        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        var rosterEntries = DomainManager.Taiwu.GetGroupCharIds().GetCollection()
            .OrderBy(value => value)
            .ToArray();
        var villageEntries = DomainManager.Taiwu.GetVillagersForWork(
                includeUnlockedWorkingVillagers: true,
                farmerFirst: false)
            .OrderBy(value => value)
            .ToArray();
        if (rosterEntries.Length != rosterEntries.Distinct().Count())
        {
            partial = true;
            diagnostics.Add(new CompanionCandidateSnapshotDiagnostic(
                "DUPLICATE_ROSTER_IDENTITY",
                CompanionCandidateSnapshotDiagnosticSeverity.Error,
                "The saved group roster contained a duplicate character identity."));
        }

        if (villageEntries.Length != villageEntries.Distinct().Count())
        {
            partial = true;
            diagnostics.Add(new CompanionCandidateSnapshotDiagnostic(
                "DUPLICATE_VILLAGE_WORK_CANDIDATE_IDENTITY",
                CompanionCandidateSnapshotDiagnosticSeverity.Error,
                "The saved village work-candidate source contained a duplicate character identity."));
        }

        if (rosterEntries.Concat(villageEntries).Any(value => value <= 0))
        {
            partial = true;
            omissions.Add(new CompanionCandidateOmission(
                characterId: null,
                "INVALID_CANDIDATE_IDENTITY",
                "A saved group or village work-candidate entry was omitted because its character identity is invalid."));
        }

        var roster = rosterEntries
            .Where(value => value > 0 && value != taiwuId)
            .ToHashSet();
        var villageCandidates = villageEntries
            .Where(value => value > 0 && value != taiwuId)
            .ToHashSet();
        var candidateIds = roster
            .Union(villageCandidates)
            .Order()
            .ToArray();

        foreach (var characterId in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var raw = ReadCandidate(
                    characterId,
                    roster.Contains(characterId),
                    villageCandidates.Contains(characterId),
                    cancellationToken);
                var mapped = CompanionCandidateSnapshotMapping.Map(raw, versions);
                profiles.Add(mapped.Profile);
                displays.Add(ReadDisplay(
                    mapped.Profile.Identity,
                    traditionalChinese,
                    english));
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
            diagnostics,
            displays);
        return new CompanionCandidateProjection(snapshot, partial);
    }

    private static RawCompanionCandidate ReadCandidate(
        int characterId,
        bool rosterMembership,
        bool villageWorkCandidateMembership,
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
                return MissingCharacter(
                    characterId,
                    rosterMembership,
                    villageWorkCandidateMembership,
                    domainMembership);
            }

            var location = character.GetLocation();
            var mainAttributes = new short[
                CompanionCandidateSnapshotMapping.MainAttributeCount];
            var savedMainAttributes = character.GetBaseMainAttributes();
            for (sbyte type = 0; type < mainAttributes.Length; type++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                mainAttributes[type] = savedMainAttributes[type];
            }

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
                rosterMembership,
                villageWorkCandidateMembership,
                characterPresent: true,
                domainMembership,
                character.IsInTaiwuGroup(),
                DomainManager.Character.IsCharacterAlive(characterId),
                character.GetCurrAge(),
                location.AreaId,
                location.BlockId,
                character.GetFeatureIds().Select(value => (int)value),
                mainAttributes,
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
                rosterMembership,
                villageWorkCandidateMembership,
                characterPresent: true,
                domainMembership,
                characterGroupMembership: null,
                livingState: null,
                currentAge: null,
                locationArea: null,
                locationBlock: null,
                featureIdentities: null,
                baseMainAttributes: null,
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
        bool rosterMembership,
        bool villageWorkCandidateMembership,
        bool? domainMembership) => new(
            characterId,
            rosterMembership,
            villageWorkCandidateMembership,
            characterPresent: false,
            domainMembership,
            characterGroupMembership: null,
            livingState: null,
            currentAge: null,
            locationArea: null,
            locationBlock: null,
            featureIdentities: null,
            baseMainAttributes: null,
            baseMartialQualifications: null,
            learnedMartialSkillIdentities: null,
            equippedMartialSkillIdentities: null,
            baseLifeSkillQualifications: null,
            learnedLifeSkillIdentities: null,
            failureIdentity: "CANDIDATE_CHARACTER_MISSING");

    private static CompanionCandidateDisplay ReadDisplay(
        CandidateIdentity identity,
        TaiwuGameTextContext traditionalChinese,
        TaiwuGameTextContext english)
    {
        try
        {
            if (!DomainManager.Character.TryGetElement_Objects(
                    identity.CharacterId,
                    out Character character))
            {
                return new CompanionCandidateDisplay(
                    identity,
                    traditionalChineseName: null,
                    englishName: null,
                    traditionalChineseLocation: null,
                    englishLocation: null);
            }

            var location = character.GetLocation();
            return new CompanionCandidateDisplay(
                identity,
                SafeName(traditionalChinese.ResolveCharacterName(character)),
                SafeName(english.ResolveCharacterName(character)),
                traditionalChinese.ResolveLocationName(location),
                english.ResolveLocationName(location));
        }
        catch (Exception exception) when (IsCandidateMappingFailure(exception))
        {
            return new CompanionCandidateDisplay(
                identity,
                traditionalChineseName: null,
                englishName: null,
                traditionalChineseLocation: null,
                englishLocation: null);
        }
    }

    private static string? SafeName(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Contains("Name_", StringComparison.Ordinal)
        || value.Contains("SurName_", StringComparison.Ordinal)
            ? null
            : value.Trim();

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

    private sealed record CachedSnapshot(
        string SaveFilePath,
        ReadOnlyFileRevision Revision,
        string GameDataVersion,
        string ProfileMappingVersion,
        string FingerprintSchemaVersion,
        CompanionCandidateSnapshotReadResult Result);
}
