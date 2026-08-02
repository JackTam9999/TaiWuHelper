using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;

namespace TaiWu.Application.CombatSkills;

public sealed record CombatSkillSearchRequest
{
    public const int MaximumQueryLength = 100;
    public const int MaximumPageSize = 100;

    public CombatSkillSearchRequest(
        CatalogueLanguage preferredLanguage,
        string? query = null,
        CombatSkillCatalogueFilter? filter = null,
        int offset = 0,
        int limit = 50)
    {
        if (!Enum.IsDefined(preferredLanguage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preferredLanguage),
                preferredLanguage,
                "Unknown catalogue language.");
        }

        var normalizedQuery = query?.Trim();
        if (normalizedQuery?.Length > MaximumQueryLength)
        {
            throw new ArgumentException(
                $"A search query cannot exceed {MaximumQueryLength} characters.",
                nameof(query));
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "Search offset cannot be negative.");
        }

        if (limit is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                $"Search limit must be 1..{MaximumPageSize}.");
        }

        var actualFilter = filter ?? new CombatSkillCatalogueFilter();
        if (offset > actualFilter.CandidateLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "Search offset cannot exceed the bounded candidate limit.");
        }

        PreferredLanguage = preferredLanguage;
        Query = string.IsNullOrEmpty(normalizedQuery) ? null : normalizedQuery;
        Filter = actualFilter;
        Offset = offset;
        Limit = limit;
    }

    public CatalogueLanguage PreferredLanguage { get; }

    public string? Query { get; }

    public CombatSkillCatalogueFilter Filter { get; }

    public int Offset { get; }

    public int Limit { get; }
}

public sealed record CombatSkillDisplayName(
    CatalogueLanguage PreferredLanguage,
    CatalogueField<LocalizedCombatSkillName> Value,
    bool UsedFallback);

public sealed record CombatSkillSearchItem(
    CombatSkillDefinition Definition,
    CombatSkillDisplayName DisplayName);

public sealed record CombatSkillSearchResult(
    CombatSkillCatalogueStatusResult Catalogue,
    int TotalMatches,
    int Offset,
    int Limit,
    bool CandidateSetMayBeTruncated,
    ImmutableArray<CombatSkillSearchItem> Items);

public sealed record CombatSkillDetailsRequest
{
    public CombatSkillDetailsRequest(
        int skillId,
        CatalogueLanguage preferredLanguage)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A combat-skill ID cannot be negative.");
        }

        if (!Enum.IsDefined(preferredLanguage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preferredLanguage),
                preferredLanguage,
                "Unknown catalogue language.");
        }

        SkillId = skillId;
        PreferredLanguage = preferredLanguage;
    }

    public int SkillId { get; }

    public CatalogueLanguage PreferredLanguage { get; }
}

public sealed record CombatSkillDetailsResult(
    CombatSkillCatalogueStatusResult Catalogue,
    int SkillId,
    CombatSkillDefinition? Definition,
    CombatSkillDisplayName? DisplayName)
{
    public bool Found => Definition is not null;
}

public sealed record CharacterCombatSkillAtlasRequest
{
    public CharacterCombatSkillAtlasRequest(
        int characterId,
        CatalogueLanguage preferredLanguage)
    {
        if (characterId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "A character ID cannot be negative.");
        }

        if (!Enum.IsDefined(preferredLanguage))
        {
            throw new ArgumentOutOfRangeException(
                nameof(preferredLanguage),
                preferredLanguage,
                "Unknown catalogue language.");
        }

        CharacterId = characterId;
        PreferredLanguage = preferredLanguage;
    }

    public int CharacterId { get; }

    public CatalogueLanguage PreferredLanguage { get; }
}

public sealed record CharacterCombatSkillAtlasEntry(
    CharacterCombatSkillProgress Progress,
    CombatSkillDefinition? Definition,
    CombatSkillDisplayName DisplayName);

public sealed record CharacterCombatSkillAtlasResult(
    CombatSkillCatalogueStatusResult Catalogue,
    CharacterProgressReadStatus ProgressStatus,
    string? ProgressFailureReason,
    ImmutableArray<CharacterCombatSkillAtlasEntry> Entries);
