namespace TaiWu.Application.CombatSkills;

public sealed class CombatSkillCatalogueMaintenanceCoordinator
{
    private readonly SemaphoreSlim _ensureGate = new(1, 1);
    private int _isRebuilding;

    public bool IsRebuilding => Volatile.Read(ref _isRebuilding) != 0;

    internal Task WaitAsync(CancellationToken cancellationToken) =>
        _ensureGate.WaitAsync(cancellationToken);

    internal void Release() => _ensureGate.Release();

    internal void BeginRebuild() =>
        Interlocked.Exchange(ref _isRebuilding, 1);

    internal void EndRebuild() =>
        Interlocked.Exchange(ref _isRebuilding, 0);
}
