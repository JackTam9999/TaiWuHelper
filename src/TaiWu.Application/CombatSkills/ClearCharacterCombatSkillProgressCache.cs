namespace TaiWu.Application.CombatSkills;

public sealed class ClearCharacterCombatSkillProgressCache(
    ICharacterCombatSkillProgressCacheMaintenance maintenance)
{
    public async Task<ClearCharacterCombatSkillProgressCacheResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var clearedSnapshotCount = await maintenance
                .ClearAsync(cancellationToken)
                .ConfigureAwait(false);
            return new ClearCharacterCombatSkillProgressCacheResult(
                ClearCharacterCombatSkillProgressCacheStatus.Cleared,
                clearedSnapshotCount,
                Reason: null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new ClearCharacterCombatSkillProgressCacheResult(
                ClearCharacterCombatSkillProgressCacheStatus.Failed,
                ClearedSnapshotCount: 0,
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "The derived progress cache could not be cleared."
                    : exception.Message.Trim());
        }
    }
}
