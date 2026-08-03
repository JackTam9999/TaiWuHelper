using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Application.GameData;

namespace TaiWu.Application.CombatSkills;

public interface ICombatSkillDefinitionSource : IReadOnlyGameDataSource
{
    Task<CombatSkillDefinitionSourceResult> ReadAsync(
        CancellationToken cancellationToken = default);
}

public interface ICombatSkillFactionProfileSource : IReadOnlyGameDataSource
{
    Task<IReadOnlyList<CombatSkillFactionProfile>> ReadAsync(
        CancellationToken cancellationToken = default);
}

public interface ICombatSkillCatalogueRepository
{
    Task<CombatSkillCatalogueRepositorySnapshot> ReadStateAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CombatSkillDefinition>> QueryAsync(
        CombatSkillCatalogueFilter filter,
        CancellationToken cancellationToken = default);

    Task<CombatSkillDefinition?> GetAsync(
        int skillId,
        CancellationToken cancellationToken = default);

    Task<CatalogueReplaceResult> ReplaceAsync(
        CombatSkillCatalogueSourceIdentity sourceIdentity,
        IReadOnlyList<CombatSkillDefinition> definitions,
        IReadOnlyList<CombatSkillImportDiagnostic> diagnostics,
        CancellationToken cancellationToken = default);
}

public interface ICharacterCombatSkillProgressReader
{
    Task<CharacterCombatSkillProgressReadResult> ReadAsync(
        CharacterCombatSkillProgressReadRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CharacterCombatSkillProgressReadRequest
{
    public CharacterCombatSkillProgressReadRequest(
        int? characterId = null,
        CatalogueLanguage preferredLanguage =
            CatalogueLanguage.TraditionalChinese)
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

    public int? CharacterId { get; }

    public CatalogueLanguage PreferredLanguage { get; }
}

public sealed record CombatSkillCatalogueFilter
{
    public const int MaximumCandidateCount = 2000;

    public CombatSkillCatalogueFilter(
        CombatSkillDiscipline? category = null,
        CombatSkillGrade? grade = null,
        CombatSkillFactionId? faction = null,
        CombatSkillElement? element = null,
        CombatSkillEquipmentType? equipmentType = null,
        int candidateLimit = MaximumCandidateCount)
    {
        ValidateOptionalEnum(category, nameof(category));
        ValidateOptionalEnum(element, nameof(element));
        ValidateOptionalEnum(equipmentType, nameof(equipmentType));
        if (candidateLimit is < 1 or > MaximumCandidateCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(candidateLimit),
                candidateLimit,
                $"Candidate limit must be 1..{MaximumCandidateCount}.");
        }

        Category = category;
        Grade = grade;
        Faction = faction;
        Element = element;
        EquipmentType = equipmentType;
        CandidateLimit = candidateLimit;
    }

    public CombatSkillDiscipline? Category { get; }

    public CombatSkillGrade? Grade { get; }

    public CombatSkillFactionId? Faction { get; }

    public CombatSkillElement? Element { get; }

    public CombatSkillEquipmentType? EquipmentType { get; }

    public int CandidateLimit { get; }

    private static void ValidateOptionalEnum<TEnum>(
        TEnum? value,
        string parameterName)
        where TEnum : struct, Enum
    {
        if (value is { } actual && !Enum.IsDefined(actual))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                actual,
                "Unknown filter value.");
        }
    }
}
