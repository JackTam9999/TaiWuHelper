using System.Collections.Immutable;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

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
    CombatSkillDisplayName DisplayName)
{
    public string StableKey => $"combat-skill:{Definition.SkillId}";

    public CombatSkillQueryIssue Issues =>
        !DisplayName.Value.IsAvailable || DisplayName.UsedFallback
            ? CombatSkillQueryIssue.PartialLocalization
            : CombatSkillQueryIssue.None;
}

[Flags]
public enum CombatSkillQueryIssue
{
    None = 0,
    PartialLocalization = 1,
    MissingDefinition = 2,
    UnsupportedStudyMapping = 4,
    ProgressWarnings = 8,
    EffectiveCostUnavailable = 16
}

public sealed record CombatSkillQueryDiagnostic
{
    public CombatSkillQueryDiagnostic(
        string code,
        string reason,
        int? skillId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "A query diagnostic requires a code.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A query diagnostic requires a reason.",
                nameof(reason));
        }

        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A diagnostic skill ID cannot be negative.");
        }

        Code = code.Trim();
        Reason = reason.Trim();
        SkillId = skillId;
    }

    public string Code { get; }

    public string Reason { get; }

    public int? SkillId { get; }
}

public sealed record CombatSkillSearchResult(
    CombatSkillCatalogueStatusResult Catalogue,
    int TotalMatches,
    int Offset,
    int Limit,
    bool CandidateSetMayBeTruncated,
    ImmutableArray<CombatSkillSearchItem> Items)
{
    public CombatSkillQueryIssue Issues => Items.Aggregate(
        CombatSkillQueryIssue.None,
        (issues, item) => issues | item.Issues);
}

public sealed record CombatSkillDetailsRequest
{
    public CombatSkillDetailsRequest(
        int skillId,
        CatalogueLanguage preferredLanguage,
        int? characterId = null)
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

        if (characterId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "A character ID cannot be negative.");
        }

        SkillId = skillId;
        PreferredLanguage = preferredLanguage;
        CharacterId = characterId;
    }

    public int SkillId { get; }

    public CatalogueLanguage PreferredLanguage { get; }

    public int? CharacterId { get; }
}

public sealed record CombatSkillDetailsResult(
    CombatSkillCatalogueStatusResult Catalogue,
    int SkillId,
    CombatSkillDefinition? Definition,
    CombatSkillDisplayName? DisplayName,
    CharacterProgressReadStatus ProgressStatus,
    string? ProgressFailureReason,
    CharacterCombatSkillProgressMetadata? ProgressMetadata,
    CharacterCombatSkillAtlasEntry? CharacterState,
    CombatSkillQueryIssue Issues,
    ImmutableArray<CombatSkillQueryDiagnostic> Diagnostics)
{
    public bool Found => Definition is not null;
}

public sealed record CharacterCombatSkillProgressFilter
{
    public CharacterCombatSkillProgressFilter(
        bool? learned = null,
        bool? hasProficiency = null,
        bool? studyComplete = null,
        bool? breakthroughReady = null,
        bool? brokenThrough = null,
        PracticeDirection? activeDirection = null,
        bool? attainmentMastered = null,
        bool? simplified = null,
        bool? activated = null,
        bool? equipped = null)
    {
        if (activeDirection is { } direction
            && direction is not PracticeDirection.Direct
                and not PracticeDirection.Reverse)
        {
            throw new ArgumentOutOfRangeException(
                nameof(activeDirection),
                direction,
                "A progress direction filter must be Direct or Reverse.");
        }

        Learned = learned;
        HasProficiency = hasProficiency;
        StudyComplete = studyComplete;
        BreakthroughReady = breakthroughReady;
        BrokenThrough = brokenThrough;
        ActiveDirection = activeDirection;
        AttainmentMastered = attainmentMastered;
        Simplified = simplified;
        Activated = activated;
        Equipped = equipped;
    }

    public bool? Learned { get; }

    public bool? HasProficiency { get; }

    public bool? StudyComplete { get; }

    public bool? BreakthroughReady { get; }

    public bool? BrokenThrough { get; }

    public PracticeDirection? ActiveDirection { get; }

    public bool? AttainmentMastered { get; }

    public bool? Simplified { get; }

    public bool? Activated { get; }

    public bool? Equipped { get; }
}

public sealed record CharacterCombatSkillAtlasRequest
{
    public CharacterCombatSkillAtlasRequest(
        int characterId,
        CatalogueLanguage preferredLanguage,
        string? query = null,
        CombatSkillCatalogueFilter? definitionFilter = null,
        CharacterCombatSkillProgressFilter? progressFilter = null,
        int offset = 0,
        int limit = 100)
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

        var normalizedQuery = query?.Trim();
        if (normalizedQuery?.Length > CombatSkillSearchRequest.MaximumQueryLength)
        {
            throw new ArgumentException(
                $"An atlas query cannot exceed "
                + $"{CombatSkillSearchRequest.MaximumQueryLength} characters.",
                nameof(query));
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "Atlas offset cannot be negative.");
        }

        if (limit is < 1 or > CombatSkillSearchRequest.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                $"Atlas limit must be 1.."
                + $"{CombatSkillSearchRequest.MaximumPageSize}.");
        }

        var actualDefinitionFilter = definitionFilter
            ?? new CombatSkillCatalogueFilter();
        if (offset > actualDefinitionFilter.CandidateLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                offset,
                "Atlas offset cannot exceed the bounded candidate limit.");
        }

        CharacterId = characterId;
        PreferredLanguage = preferredLanguage;
        Query = string.IsNullOrEmpty(normalizedQuery) ? null : normalizedQuery;
        DefinitionFilter = actualDefinitionFilter;
        ProgressFilter = progressFilter ?? new CharacterCombatSkillProgressFilter();
        Offset = offset;
        Limit = limit;
    }

    public int CharacterId { get; }

    public CatalogueLanguage PreferredLanguage { get; }

    public string? Query { get; }

    public CombatSkillCatalogueFilter DefinitionFilter { get; }

    public CharacterCombatSkillProgressFilter ProgressFilter { get; }

    public int Offset { get; }

    public int Limit { get; }
}

public sealed record CharacterCombatSkillAtlasEntry
{
    public CharacterCombatSkillAtlasEntry(
        int skillId,
        CharacterCombatSkillProgress? progress,
        CombatSkillDefinition? definition,
        CombatSkillDisplayName displayName,
        SkillProgressField<bool> learned,
        SkillProgressField<int> currentEffectiveGridCost,
        IEnumerable<CombatSkillQueryDiagnostic>? diagnostics = null)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "A combat-skill ID cannot be negative.");
        }

        if (progress is not null && progress.SkillId != skillId
            || definition is not null && definition.SkillId != skillId)
        {
            throw new ArgumentException(
                "Joined atlas values must use the entry skill ID.",
                nameof(skillId));
        }

        if (progress is null && definition is null)
        {
            throw new ArgumentException(
                "An atlas entry requires progress or a static definition.",
                nameof(progress));
        }

        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(learned);
        ArgumentNullException.ThrowIfNull(currentEffectiveGridCost);
        SkillId = skillId;
        Progress = progress;
        Definition = definition;
        DisplayName = displayName;
        Learned = learned;
        CurrentEffectiveGridCost = currentEffectiveGridCost;
        Diagnostics = (diagnostics ?? []).ToImmutableArray();
    }

    public int SkillId { get; }

    public string StableKey => $"combat-skill:{SkillId}";

    public CharacterCombatSkillProgress? Progress { get; }

    public CombatSkillDefinition? Definition { get; }

    public CombatSkillDisplayName DisplayName { get; }

    public SkillProgressField<bool> Learned { get; }

    public CatalogueField<CombatSkillGridCost> BaseGridCost =>
        Definition?.BaseGridCost
        ?? CatalogueField<CombatSkillGridCost>.Unavailable(
            "The static skill definition is unavailable.");

    public SkillProgressField<int> CurrentEffectiveGridCost { get; }

    public ImmutableArray<CombatSkillQueryDiagnostic> Diagnostics { get; }

    public CombatSkillQueryIssue Issues
    {
        get
        {
            var issues = CombatSkillQueryIssue.None;
            if (!DisplayName.Value.IsAvailable || DisplayName.UsedFallback)
            {
                issues |= CombatSkillQueryIssue.PartialLocalization;
            }

            if (Definition is null)
            {
                issues |= CombatSkillQueryIssue.MissingDefinition;
            }

            if (!CurrentEffectiveGridCost.IsAvailable)
            {
                issues |= CombatSkillQueryIssue.EffectiveCostUnavailable;
            }

            if (Progress is not null
                && (Progress.StudyDetails.Length == 0
                    || Progress.UnavailableStudyDetails.Length > 0))
            {
                issues |= CombatSkillQueryIssue.UnsupportedStudyMapping;
            }

            return issues;
        }
    }
}

public sealed record CharacterCombatSkillAtlasResult(
    CombatSkillCatalogueStatusResult Catalogue,
    CharacterProgressReadStatus ProgressStatus,
    string? ProgressFailureReason,
    CharacterCombatSkillProgressMetadata? ProgressMetadata,
    int TotalMatches,
    int Offset,
    int Limit,
    bool CandidateSetMayBeTruncated,
    CombatSkillQueryIssue Issues,
    ImmutableArray<CombatSkillQueryDiagnostic> Diagnostics,
    ImmutableArray<CharacterCombatSkillAtlasEntry> Entries);
