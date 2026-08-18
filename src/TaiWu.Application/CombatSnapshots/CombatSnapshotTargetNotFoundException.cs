namespace TaiWu.Application.CombatSnapshots;

public sealed class CombatSnapshotTargetNotFoundException(int targetCharacterId)
    : Exception($"Target character {targetCharacterId} was not found in the save.")
{
    public int TargetCharacterId { get; } = targetCharacterId;
}
