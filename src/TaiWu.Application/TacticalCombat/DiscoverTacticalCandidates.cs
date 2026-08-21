using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed class DiscoverTacticalCandidates(ICombatSnapshotReader reader)
    : IDiscoverTacticalCandidates
{
    public async Task<TacticalCandidateDiscoveryReadResult> ExecuteAsync(
        TacticalCandidateDiscoveryReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await reader.ReadAsync(
            request.ContextRequest.SnapshotRequest,
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var resolution = TacticalExecutionContextProjection.ResolveRules(
            snapshot,
            request.ContextRequest,
            cancellationToken);
        var contextRead = TacticalExecutionContextProjection.Project(
            snapshot,
            request.ContextRequest,
            resolution,
            cancellationToken);
        var discovery = TacticalCandidateDiscovery.Discover(
            snapshot.Player,
            contextRead.Context,
            resolution,
            request.Limits,
            cancellationToken);
        return new TacticalCandidateDiscoveryReadResult(
            contextRead,
            discovery);
    }
}
