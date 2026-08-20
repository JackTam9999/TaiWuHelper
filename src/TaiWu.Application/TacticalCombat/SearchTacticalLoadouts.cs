using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed class SearchTacticalLoadouts(
    ICombatSnapshotReader reader,
    TimeProvider timeProvider) : ISearchTacticalLoadouts
{
    public async Task<TacticalLoadoutSearchReadResult> ExecuteAsync(
        TacticalLoadoutSearchReadRequest request,
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
            request.DiscoveryLimits,
            cancellationToken);
        var search = TacticalLoadoutSearch.Search(
            new TacticalLoadoutSearchRequest(
                snapshot.Player,
                projection.Result.Context,
                projection.RuleResolution,
                discovery,
                request.Bounds,
                request.IrrelevanceProofs,
                request.DominanceProofs),
            timeProvider,
            cancellationToken);
        return new TacticalLoadoutSearchReadResult(
            projection.Result,
            discovery,
            search);
    }
}
