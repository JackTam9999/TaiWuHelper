namespace TaiWu.Application.Targets;

public sealed record TargetLookupEntry
{
    public TargetLookupEntry(
        int characterId,
        string displayName,
        int age,
        int areaId,
        int blockId,
        string? locationDisplayName = null,
        TargetLookupKind kind = TargetLookupKind.RegularCharacter,
        int? templateId = null,
        int? consummateLevel = null)
    {
        if (characterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "Character ID must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "A target lookup entry requires a name.",
                nameof(displayName));
        }

        if (age < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(age),
                age,
                "Character age cannot be negative.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Unknown target lookup kind.");
        }

        if (templateId is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(templateId),
                templateId,
                "Character template ID cannot be negative.");
        }

        if (consummateLevel is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(consummateLevel),
                consummateLevel,
                "Consummate level cannot be negative.");
        }

        if (kind == TargetLookupKind.StoryCharacter
            && !templateId.HasValue)
        {
            throw new ArgumentException(
                "A story target requires a character template ID.",
                nameof(templateId));
        }

        CharacterId = characterId;
        DisplayName = displayName.Trim();
        Age = age;
        AreaId = areaId;
        BlockId = blockId;
        LocationDisplayName = string.IsNullOrWhiteSpace(locationDisplayName)
            ? null
            : locationDisplayName.Trim();
        Kind = kind;
        TemplateId = templateId;
        ConsummateLevel = consummateLevel;
    }

    public int CharacterId { get; }

    public string DisplayName { get; }

    public int Age { get; }

    public int AreaId { get; }

    public int BlockId { get; }

    public string? LocationDisplayName { get; }

    public TargetLookupKind Kind { get; }

    public int? TemplateId { get; }

    public int? ConsummateLevel { get; }

    public bool HasValidLocation => AreaId >= 0 && BlockId >= 0;
}
