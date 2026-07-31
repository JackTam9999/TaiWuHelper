using GameData.ArchiveData;
using GameData.Domains;
using GameData.Domains.Character;
using System.Reflection;
using TaiWu.Application.Targets;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuTargetLookupReader(
    TaiwuArchiveReadSession readSession,
    TaiwuGameTextResolver textResolver) : ITargetLookupReader
{
    public Task<TargetLookupSnapshot> ReadAsync(
        TargetLookupReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return readSession.ReadAsync(
            request.SaveFilePath,
            (context, token) => ProjectTargets(
                context,
                textResolver.CreateContext(
                    request.SaveFilePath,
                    request.Language),
                token),
            cancellationToken);
    }

    private static TargetLookupSnapshot ProjectTargets(
        TaiwuArchiveReadContext readContext,
        TaiwuGameTextContext text,
        CancellationToken cancellationToken)
    {
        List<TargetLookupWarning> warnings = [];
        if (readContext.LoadWarning is not null)
        {
            warnings.Add(
                new TargetLookupWarning(
                    readContext.LoadWarning.Code,
                    "The archive reached the expected standalone "
                    + $"event-runtime boundary: {readContext.LoadWarning.Detail}"));
        }

        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        List<TargetLookupEntry> entries = [];
        var index = 0;
        foreach (var (characterId, character) in
                 DomainManager.Character.Characters.OrderBy(pair => pair.Key))
        {
            if (index++ % 128 == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (characterId == taiwuId)
            {
                continue;
            }

            TryMapCharacter(
                characterId,
                character,
                text,
                entries,
                warnings);
        }

        return new TargetLookupSnapshot(
            DateTimeOffset.UtcNow,
            GetGameDataVersion(),
            entries,
            warnings);
    }

    private static void TryMapCharacter(
        int characterId,
        Character character,
        TaiwuGameTextContext text,
        List<TargetLookupEntry> entries,
        List<TargetLookupWarning> warnings)
    {
        try
        {
            var displayName = text.ResolveCharacterName(character);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                warnings.Add(
                    new TargetLookupWarning(
                        "TARGET_NAME_UNAVAILABLE",
                        $"Character {characterId} was omitted because its "
                        + "display name is unavailable."));
                return;
            }

            var location = character.GetLocation();
            entries.Add(
                new TargetLookupEntry(
                    characterId,
                    displayName,
                    character.GetCurrAge(),
                    location.AreaId,
                    location.BlockId,
                    text.ResolveLocationName(location)));
        }
        catch (Exception exception)
            when (exception is ArgumentException
                or InvalidOperationException
                or NullReferenceException)
        {
            warnings.Add(
                new TargetLookupWarning(
                    "TARGET_CONTEXT_UNAVAILABLE",
                    $"Character {characterId} was omitted because its "
                    + $"lookup context is unavailable: {exception.Message}"));
        }
    }

    private static string? GetGameDataVersion()
    {
        var assembly = typeof(LocalArchiveFile).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        return string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString()
            : informationalVersion;
    }
}
