namespace TaiWu.Domain.CompanionCandidates;

public sealed record CandidateIdentity
{
    public CandidateIdentity(int characterId)
    {
        if (characterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "A candidate character ID must be greater than zero.");
        }

        CharacterId = characterId;
    }

    public int CharacterId { get; }

    internal string StableKey => CharacterId.ToString(
        System.Globalization.CultureInfo.InvariantCulture);
}
