using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.CombatSkill;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Infrastructure.Catalogue;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuCharacterCombatSkillProgressReader(
    TaiwuArchiveReadSession readSession,
    ITaiwuSaveFilePathProvider saveFilePathProvider,
    CombatSkillStudyDetailLabelSource labelSource,
    IReadOnlyFileRevisionProvider revisionProvider,
    SqliteCharacterCombatSkillProgressCache cache,
    TimeProvider timeProvider,
    ILogger<TaiwuCharacterCombatSkillProgressReader> logger)
    : ICharacterCombatSkillProgressReader
{
    internal const string SupportedGameDataVersion =
        CombatSkillStudyDetailDecoder.SupportedGameDataVersion;

    public async Task<CharacterCombatSkillProgressReadResult> ReadAsync(
        CharacterCombatSkillProgressReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var located = saveFilePathProvider.Resolve();
        if (!located.IsAvailable)
        {
            return CharacterCombatSkillProgressReadResult.SaveMissing(
                located.Reason
                ?? "The trusted Taiwu save configuration is unavailable.");
        }

        var gameDataVersion = GetGameDataVersion();
        if (!string.Equals(
                gameDataVersion,
                SupportedGameDataVersion,
                StringComparison.Ordinal))
        {
            return CharacterCombatSkillProgressReadResult.UnsupportedVersion(
                $"GameData version {gameDataVersion} is not supported by the "
                + "verified character-progress mapping.");
        }

        try
        {
            var totalStarted = timeProvider.GetTimestamp();
            var labelsStarted = timeProvider.GetTimestamp();
            var labels = await labelSource.ReadAsync(
                    request.PreferredLanguage,
                    cancellationToken)
                .ConfigureAwait(false);
            var labelsElapsed = timeProvider.GetElapsedTime(labelsStarted);

            var saveFilePath = located.SaveFilePath!;
            var revisionBefore = revisionProvider.Capture(saveFilePath);
            var cacheStarted = timeProvider.GetTimestamp();
            RawCharacterCombatSkillSnapshot? cached = null;
            try
            {
                cached = await cache.TryReadAsync(
                        saveFilePath,
                        revisionBefore,
                        request.CharacterId,
                        gameDataVersion,
                        CombatSkillProgressMapping.CacheMappingVersion,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (IsRecoverableCacheFailure(exception))
            {
                logger.LogWarning(
                    exception,
                    "The structured save-progress cache could not be read; "
                    + "the source save will be used.");
            }

            var cacheElapsed = timeProvider.GetElapsedTime(cacheStarted);
            var revisionAfter = revisionProvider.Capture(saveFilePath);
            if (cached is not null && revisionBefore == revisionAfter)
            {
                var result = Map(cached, gameDataVersion, labels);
                LogTiming(
                    cacheHit: true,
                    labelsElapsed,
                    cacheElapsed,
                    archiveElapsed: TimeSpan.Zero,
                    cacheStoreElapsed: TimeSpan.Zero,
                    timeProvider.GetElapsedTime(totalStarted));
                return result;
            }

            var archiveStarted = timeProvider.GetTimestamp();
            var projected = await readSession.ReadAsync(
                    located.SaveFilePath!,
                    (context, token) => ProjectRaw(
                        context,
                        request.CharacterId,
                        token),
                    cancellationToken)
                .ConfigureAwait(false);
            var archiveElapsed = timeProvider.GetElapsedTime(archiveStarted);
            var mapped = Map(projected, gameDataVersion, labels);

            var cacheStoreStarted = timeProvider.GetTimestamp();
            try
            {
                await cache.StoreAsync(
                        saveFilePath,
                        gameDataVersion,
                        CombatSkillProgressMapping.CacheMappingVersion,
                        projected,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (IsRecoverableCacheFailure(exception))
            {
                logger.LogWarning(
                    exception,
                    "The structured save-progress cache could not be updated.");
            }

            var cacheStoreElapsed = timeProvider.GetElapsedTime(
                cacheStoreStarted);
            LogTiming(
                cacheHit: false,
                labelsElapsed,
                cacheElapsed,
                archiveElapsed,
                cacheStoreElapsed,
                timeProvider.GetElapsedTime(totalStarted));
            return mapped;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return CharacterCombatSkillProgressReadResult.SaveMissing(
                "The configured Taiwu save file was not found.");
        }
        catch (Exception exception)
            when (exception is IOException
                  or UnauthorizedAccessException
                  or InvalidDataException
                  or KeyNotFoundException)
        {
            return CharacterCombatSkillProgressReadResult.SaveReadFailed(
                "The configured Taiwu save could not be read safely.");
        }
    }

    private RawCharacterCombatSkillSnapshot ProjectRaw(
        TaiwuArchiveReadContext context,
        int? requestedCharacterId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var taiwuCharacterId = DomainManager.Taiwu.GetTaiwuCharId();
        var characterId = requestedCharacterId
            ?? taiwuCharacterId;
        if (!DomainManager.Character.TryGetElement_Objects(
                characterId,
                out Character character))
        {
            throw new KeyNotFoundException(
                $"Character {characterId} is absent from the configured save.");
        }

        HashSet<short> equippedSkillIds = [];
        character.GetCombatSkillEquipment().GetValidSkills(equippedSkillIds);
        var sourceSkills =
            DomainManager.CombatSkill.GetCharCombatSkills(characterId);
        var progress = System.Collections.Immutable.ImmutableArray
            .CreateBuilder<RawCharacterCombatSkillProgress>();
        foreach (var (skillId, skill) in sourceSkills.OrderBy(pair => pair.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            int? proficiency = DomainManager.Extra
                .TryGetElement_CombatSkillProficiencies(
                    new CombatSkillKey(characterId, skillId),
                    out var storedProficiency)
                ? storedProficiency
                : null;
            progress.Add(new RawCharacterCombatSkillProgress(
                skillId,
                Learned: true,
                proficiency,
                skill.GetReadingState(),
                skill.GetActivationState(),
                skill.CanBreakout(),
                DomainManager.Extra.IsCombatSkillMasteredByCharacter(
                    characterId,
                    skillId),
                equippedSkillIds.Contains(skillId)));
        }

        return new RawCharacterCombatSkillSnapshot(
            context.SourceFingerprint,
            timeProvider.GetUtcNow(),
            taiwuCharacterId,
            characterId,
            context.LoadWarning,
            progress.ToImmutable());
    }

    private static CharacterCombatSkillProgressReadResult Map(
        RawCharacterCombatSkillSnapshot rawSnapshot,
        string gameDataVersion,
        CombatSkillStudyDetailLabelSet labels)
    {
        var snapshot = new SaveSnapshotIdentity(
            rawSnapshot.SourceFingerprint.Sha256,
            rawSnapshot.ReadAtUtc);
        List<CharacterCombatSkillProgressWarning> warnings = [];
        warnings.AddRange(labels.Warnings);
        if (rawSnapshot.LoadWarning is not null)
        {
            warnings.Add(new CharacterCombatSkillProgressWarning(
                rawSnapshot.LoadWarning.Code,
                "The archive reached the expected standalone event-runtime "
                + "boundary while loading read-only progress."));
        }

        warnings.Add(new CharacterCombatSkillProgressWarning(
            "ATTAINMENT_MASTERY_UNAVAILABLE",
            "The persisted rule for the player-facing attainment mastery "
            + "label is not verified for this version."));
        warnings.Add(new CharacterCombatSkillProgressWarning(
            "PROFICIENCY_PERCENTAGE_UNAVAILABLE",
            "The displayed proficiency percentage conversion is not verified."));
        var progress = rawSnapshot.Progress.Select(raw =>
            CombatSkillProgressMapping.Map(
                rawSnapshot.CharacterId,
                snapshot,
                raw,
                gameDataVersion,
                labels,
                warnings))
            .ToArray();
        return CharacterCombatSkillProgressReadResult.Available(
            new CharacterCombatSkillProgressMetadata(
                snapshot,
                gameDataVersion,
                warnings),
            progress);
    }

    private void LogTiming(
        bool cacheHit,
        TimeSpan labelsElapsed,
        TimeSpan cacheElapsed,
        TimeSpan archiveElapsed,
        TimeSpan cacheStoreElapsed,
        TimeSpan totalElapsed)
    {
        logger.LogInformation(
            "Combat-skill progress read: cacheHit={CacheHit}; labelsMs={LabelsMs:F0}; "
            + "cacheLookupMs={CacheLookupMs:F0}; archiveMs={ArchiveMs:F0}; "
            + "cacheStoreMs={CacheStoreMs:F0}; totalMs={TotalMs:F0}.",
            cacheHit,
            labelsElapsed.TotalMilliseconds,
            cacheElapsed.TotalMilliseconds,
            archiveElapsed.TotalMilliseconds,
            cacheStoreElapsed.TotalMilliseconds,
            totalElapsed.TotalMilliseconds);
    }

    private static bool IsRecoverableCacheFailure(Exception exception) =>
        exception is SqliteException
            or IOException
            or UnauthorizedAccessException
            or InvalidDataException;

    private static string GetGameDataVersion()
    {
        var version = FileVersionInfo.GetVersionInfo(
                typeof(DomainManager).Assembly.Location)
            .ProductVersion;
        return string.IsNullOrWhiteSpace(version)
            ? "unknown"
            : version;
    }
}
