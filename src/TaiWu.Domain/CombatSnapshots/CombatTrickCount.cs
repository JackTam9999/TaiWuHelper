namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatTrickCount
{
    public CombatTrickCount(int trickTypeId, int count)
    {
        if (trickTypeId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(trickTypeId),
                trickTypeId,
                "Trick type ID cannot be negative.");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "Trick count cannot be negative.");
        }

        TrickTypeId = trickTypeId;
        Count = count;
    }

    public int TrickTypeId { get; }

    public int Count { get; }
}
