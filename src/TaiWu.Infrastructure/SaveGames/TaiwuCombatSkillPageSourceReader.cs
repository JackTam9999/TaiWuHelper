using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Item;
using System.Collections.Immutable;
using System.Diagnostics;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSkills;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuCombatSkillPageSourceReader(
    TaiwuArchiveReadSession readSession,
    ITaiwuSaveFilePathProvider saveFilePathProvider,
    TaiwuGameTextResolver textResolver,
    TimeProvider? timeProvider = null) : ICombatSkillPageSourceReader
{
    internal const string SupportedGameDataVersion =
        CombatSkillStudyDetailDecoder.SupportedGameDataVersion;

    private static readonly IReadOnlyDictionary<string, ushort> DetailMasks =
        Enumerable.Range(0, 5)
            .SelectMany(index => new[]
            {
                KeyValuePair.Create($"outline-{index}", (ushort)(1 << index)),
                KeyValuePair.Create($"direct-{index}",
                    (ushort)(1 << (index + 5))),
                KeyValuePair.Create($"reverse-{index}",
                    (ushort)(1 << (index + 10)))
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value,
                StringComparer.Ordinal);

    private readonly TimeProvider _timeProvider = timeProvider
        ?? TimeProvider.System;

    public async Task<CombatSkillPageSourceReadResult> ReadAsync(
        CombatSkillPageSourceReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var located = saveFilePathProvider.Resolve();
        if (!located.IsAvailable)
        {
            return CombatSkillPageSourceReadResult.Unavailable(
                CombatSkillPageSourceReadStatus.SaveMissing,
                request,
                located.Reason
                ?? "The trusted Taiwu save configuration is unavailable.");
        }

        var gameDataVersion = GetGameDataVersion();
        if (!string.Equals(
                gameDataVersion,
                SupportedGameDataVersion,
                StringComparison.Ordinal))
        {
            return CombatSkillPageSourceReadResult.Unavailable(
                CombatSkillPageSourceReadStatus.UnsupportedVersion,
                request,
                $"GameData version {gameDataVersion} has no verified "
                + "skill-page source mapping.");
        }

        try
        {
            return await readSession.ReadAsync(
                    located.SaveFilePath!,
                    (context, token) => Project(
                        context,
                        request,
                        gameDataVersion,
                        token),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return CombatSkillPageSourceReadResult.Unavailable(
                CombatSkillPageSourceReadStatus.SaveMissing,
                request,
                "The configured Taiwu save file was not found.");
        }
        catch (Exception exception)
            when (exception is IOException
                or UnauthorizedAccessException
                or InvalidDataException
                or KeyNotFoundException)
        {
            return CombatSkillPageSourceReadResult.Unavailable(
                CombatSkillPageSourceReadStatus.SaveReadFailed,
                request,
                "The configured Taiwu save could not be searched safely.");
        }
    }

    private CombatSkillPageSourceReadResult Project(
        TaiwuArchiveReadContext context,
        CombatSkillPageSourceReadRequest request,
        string gameDataVersion,
        CancellationToken cancellationToken)
    {
        var language = request.PreferredLanguage switch
        {
            CatalogueLanguage.TraditionalChinese => TaiwuLanguage.Chinese,
            CatalogueLanguage.English => TaiwuLanguage.English,
            _ => throw new ArgumentOutOfRangeException(
                nameof(request),
                request.PreferredLanguage,
                "Unknown catalogue language.")
        };
        var text = textResolver.CreateContext(
            context.SaveFilePath,
            language);
        var requested = request.DetailIds
            .ToDictionary(
                detailId => detailId,
                detailId => DetailMasks[detailId],
                StringComparer.Ordinal);
        List<CombatSkillPageSourceCandidate> candidates = [];
        List<CombatSkillPageSourceWarning> warnings = [];
        if (context.LoadWarning is not null)
        {
            warnings.Add(new CombatSkillPageSourceWarning(
                context.LoadWarning.Code,
                "The archive reached the expected standalone event-runtime "
                + "boundary while locating read-only page sources."));
        }

        var taiwuId = DomainManager.Taiwu.GetTaiwuCharId();
        var unsupportedReadingStates = 0;
        var unsupportedBookLayouts = 0;
        var unavailableCharacters = 0;
        var index = 0;
        foreach (var (characterId, character) in
                 DomainManager.Character.Characters.OrderBy(pair => pair.Key))
        {
            if (index++ % 128 == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            try
            {
                var characterContext = DescribeCharacter(character, text);
                if (characterId != taiwuId)
                {
                    var skills = DomainManager.CombatSkill
                        .GetCharCombatSkills(characterId);
                    if (skills.TryGetValue((short)request.SkillId, out var skill))
                    {
                        var readingState = skill.GetReadingState();
                        if ((readingState & 0x8000) != 0)
                        {
                            unsupportedReadingStates++;
                        }
                        else
                        {
                            var matchingDetails = requested
                                .Where(pair => (readingState & pair.Value) != 0)
                                .Select(pair => pair.Key)
                                .ToImmutableArray();
                            if (matchingDetails.Length > 0)
                            {
                                candidates.Add(CreateCandidate(
                                    CombatSkillPageSourceKind.CharacterKnowledge,
                                    characterId,
                                    characterContext,
                                    BookItemId: null,
                                    BookTemplateId: null,
                                    Quantity: 1,
                                    matchingDetails,
                                    isTaiwuInventory: false));
                            }
                        }
                    }
                }

                foreach (var (itemKey, quantity) in
                         character.GetInventory().Items)
                {
                    if (DomainManager.Item.TryGetBaseItem(itemKey)
                        is not SkillBook book
                        || book.GetCombatSkillTemplateId() != request.SkillId)
                    {
                        continue;
                    }

                    var display = DomainManager.Item
                        .GetSkillBookPagesInfo(itemKey);
                    var bookDetails = DecodeBookDetailIds(display.Type);
                    if (bookDetails is null)
                    {
                        unsupportedBookLayouts++;
                        continue;
                    }

                    var matchingDetails = request.DetailIds
                        .Where(bookDetails.Contains)
                        .ToImmutableArray();
                    if (matchingDetails.Length == 0)
                    {
                        continue;
                    }

                    candidates.Add(CreateCandidate(
                        CombatSkillPageSourceKind.InventoryBook,
                        characterId,
                        characterContext,
                        itemKey.Id,
                        itemKey.TemplateId,
                        quantity,
                        matchingDetails,
                        isTaiwuInventory: characterId == taiwuId));
                }
            }
            catch (Exception exception)
                when (exception is ArgumentException
                    or InvalidOperationException
                    or NullReferenceException
                    or KeyNotFoundException)
            {
                unavailableCharacters++;
            }
        }

        AddAggregateWarning(
            warnings,
            "PAGE_SOURCE_READING_STATE_UNSUPPORTED",
            unsupportedReadingStates,
            "character reading states were unsupported and omitted");
        AddAggregateWarning(
            warnings,
            "PAGE_SOURCE_BOOK_LAYOUT_UNSUPPORTED",
            unsupportedBookLayouts,
            "skill books had unsupported page layouts and were omitted");
        AddAggregateWarning(
            warnings,
            "PAGE_SOURCE_CHARACTER_UNAVAILABLE",
            unavailableCharacters,
            "character records could not be inspected and were omitted");

        var ordered = candidates
            .OrderBy(candidate => candidate.IsActionable ? 0 : 1)
            .ThenBy(candidate => candidate.Kind)
            .ThenBy(candidate => candidate.CharacterName)
            .ThenBy(candidate => candidate.CharacterId)
            .ThenBy(candidate => candidate.BookItemId)
            .ToImmutableArray();
        return new CombatSkillPageSourceReadResult(
            CombatSkillPageSourceReadStatus.Available,
            request.SkillId,
            request.DetailIds,
            new CombatSkillPageSourceMetadata(
                new SaveSnapshotIdentity(
                    context.SourceFingerprint.Sha256,
                    _timeProvider.GetUtcNow()),
                gameDataVersion,
                warnings.ToImmutableArray()),
            ordered,
            Reason: null);
    }

    private static CombatSkillPageSourceCandidate CreateCandidate(
        CombatSkillPageSourceKind kind,
        int characterId,
        CharacterContext character,
        int? BookItemId,
        int? BookTemplateId,
        int Quantity,
        ImmutableArray<string> detailIds,
        bool isTaiwuInventory)
    {
        var availability = isTaiwuInventory
            ? CombatSkillPageSourceAvailability.TaiwuInventory
            : !string.IsNullOrWhiteSpace(character.Name)
                && !string.IsNullOrWhiteSpace(character.LocationName)
                ? CombatSkillPageSourceAvailability.Locatable
                : CombatSkillPageSourceAvailability.Unlocated;
        return new CombatSkillPageSourceCandidate(
            kind,
            availability,
            characterId,
            character.Name,
            character.Age,
            character.AreaId,
            character.BlockId,
            character.LocationName,
            BookItemId,
            BookTemplateId,
            Math.Max(1, Quantity),
            detailIds);
    }

    private static CharacterContext DescribeCharacter(
        Character character,
        TaiwuGameTextContext text)
    {
        var name = text.ResolveCharacterName(character);
        var location = character.GetLocation();
        return new CharacterContext(
            string.IsNullOrWhiteSpace(name) ? null : name,
            character.GetCurrAge(),
            location.IsValid() ? location.AreaId : null,
            location.IsValid() ? location.BlockId : null,
            text.ResolveLocationName(location));
    }

    internal static ImmutableHashSet<string>? DecodeBookDetailIds(
        IReadOnlyList<sbyte>? pageTypes)
    {
        if (pageTypes is null
            || pageTypes.Count != 6
            || pageTypes[0] is < 0 or > 4)
        {
            return null;
        }

        var details = ImmutableHashSet.CreateBuilder<string>(
            StringComparer.Ordinal);
        details.Add($"outline-{pageTypes[0]}");
        for (var index = 0; index < 5; index++)
        {
            var pageType = pageTypes[index + 1];
            if (pageType is < 0 or > 1)
            {
                return null;
            }

            details.Add(pageType == 0
                ? $"direct-{index}"
                : $"reverse-{index}");
        }

        return details.ToImmutable();
    }

    internal static ushort GetDetailMask(string detailId) =>
        DetailMasks.TryGetValue(detailId, out var mask)
            ? mask
            : throw new ArgumentOutOfRangeException(
                nameof(detailId),
                detailId,
                "Unknown study-detail ID.");

    private static void AddAggregateWarning(
        ICollection<CombatSkillPageSourceWarning> warnings,
        string code,
        int count,
        string message)
    {
        if (count > 0)
        {
            warnings.Add(new CombatSkillPageSourceWarning(
                code,
                $"{count} {message}."));
        }
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

    private sealed record CharacterContext(
        string? Name,
        int? Age,
        int? AreaId,
        int? BlockId,
        string? LocationName);
}
