using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.CombatSkill;
using System.Diagnostics;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuCharacterCombatSkillProgressReader(
    TaiwuArchiveReadSession readSession,
    ITaiwuSaveFilePathProvider saveFilePathProvider,
    TimeProvider timeProvider) : ICharacterCombatSkillProgressReader
{
    internal const string SupportedGameDataVersion =
        "1.0.0+68032f25c1d54dd4fb8fc65b7156e95bf87ec99a";

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
            return await readSession.ReadAsync(
                    located.SaveFilePath!,
                    (context, token) => Project(
                        context,
                        request.CharacterId,
                        gameDataVersion,
                        token),
                    cancellationToken)
                .ConfigureAwait(false);
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

    private CharacterCombatSkillProgressReadResult Project(
        TaiwuArchiveReadContext context,
        int characterId,
        string gameDataVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!DomainManager.Character.TryGetElement_Objects(
                characterId,
                out Character character))
        {
            throw new KeyNotFoundException(
                $"Character {characterId} is absent from the configured save.");
        }

        var snapshot = new SaveSnapshotIdentity(
            context.SourceFingerprint.Sha256,
            timeProvider.GetUtcNow());
        List<CharacterCombatSkillProgressWarning> warnings = [];
        if (context.LoadWarning is not null)
        {
            warnings.Add(new CharacterCombatSkillProgressWarning(
                context.LoadWarning.Code,
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
        warnings.Add(new CharacterCombatSkillProgressWarning(
            "STUDY_DETAILS_PENDING_DECODER",
            "Individual study details remain unavailable until the "
            + "version-specific E2-010 decoder is applied."));

        HashSet<short> equippedSkillIds = [];
        character.GetCombatSkillEquipment().GetValidSkills(equippedSkillIds);
        var sourceSkills =
            DomainManager.CombatSkill.GetCharCombatSkills(characterId);
        List<CharacterCombatSkillProgress> progress = [];
        foreach (var (skillId, skill) in sourceSkills.OrderBy(pair => pair.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            int? proficiency = DomainManager.Extra
                .TryGetElement_CombatSkillProficiencies(
                    new CombatSkillKey(characterId, skillId),
                    out var storedProficiency)
                ? storedProficiency
                : null;
            var raw = new RawCharacterCombatSkillProgress(
                skillId,
                Learned: true,
                proficiency,
                skill.GetReadingState(),
                skill.GetActivationState(),
                skill.CanBreakout(),
                DomainManager.Extra.IsCombatSkillMasteredByCharacter(
                    characterId,
                    skillId),
                equippedSkillIds.Contains(skillId));
            progress.Add(CombatSkillProgressMapping.Map(
                characterId,
                snapshot,
                raw,
                warnings));
        }

        return CharacterCombatSkillProgressReadResult.Available(
            new CharacterCombatSkillProgressMetadata(
                snapshot,
                gameDataVersion,
                warnings),
            progress);
    }

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
