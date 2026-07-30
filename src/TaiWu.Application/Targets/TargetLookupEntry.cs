namespace TaiWu.Application.Targets;

public sealed record TargetLookupEntry
{
    public TargetLookupEntry(
        int characterId,
        string displayName,
        int age,
        int areaId,
        int blockId)
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

        CharacterId = characterId;
        DisplayName = displayName.Trim();
        Age = age;
        AreaId = areaId;
        BlockId = blockId;
    }

    public int CharacterId { get; }

    public string DisplayName { get; }

    public int Age { get; }

    public int AreaId { get; }

    public int BlockId { get; }
}
