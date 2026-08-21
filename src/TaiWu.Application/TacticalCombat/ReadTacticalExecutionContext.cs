using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed class ReadTacticalExecutionContext(ICombatSnapshotReader reader)
    : IReadTacticalExecutionContext
{
    public async Task<TacticalExecutionContextReadResult> ExecuteAsync(
        TacticalExecutionContextReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await reader.ReadAsync(
            request.SnapshotRequest,
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var resolution = TacticalExecutionContextProjection.ResolveRules(
            snapshot,
            request,
            cancellationToken);
        return TacticalExecutionContextProjection.Project(
            snapshot,
            request,
            resolution,
            cancellationToken);
    }
}
