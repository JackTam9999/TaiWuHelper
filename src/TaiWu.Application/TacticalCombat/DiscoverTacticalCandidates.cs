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

        var projection = ReadTacticalExecutionContext.ProjectSnapshot(
            snapshot,
            request.ContextRequest,
            cancellationToken);
        var discovery = TacticalCandidateDiscovery.Discover(
            snapshot.Player,
            projection.Result.Context,
            projection.RuleResolution,
            request.Limits,
            cancellationToken);
        return new TacticalCandidateDiscoveryReadResult(
            projection.Result,
            discovery);
    }
}
