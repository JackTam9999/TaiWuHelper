namespace TaiWu.Application.CombatSnapshots;

public sealed record CombatSnapshotReadRequest
{
    public CombatSnapshotReadRequest(
        string saveFilePath,
        int targetCharacterId)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath))
        {
            throw new ArgumentException(
                "A save-file path is required.",
                nameof(saveFilePath));
        }

        if (targetCharacterId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetCharacterId),
                targetCharacterId,
                "Target character ID must be greater than zero.");
        }

        SaveFilePath = saveFilePath;
        TargetCharacterId = targetCharacterId;
    }

    public string SaveFilePath { get; }

    public int TargetCharacterId { get; }
}
