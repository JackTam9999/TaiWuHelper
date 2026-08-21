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
            request.DiscoveryLimits,
            cancellationToken);
        var search = TacticalLoadoutSearch.Search(
            new TacticalLoadoutSearchRequest(
                snapshot.Player,
                contextRead.Context,
                resolution,
                discovery,
                request.Bounds,
                request.IrrelevanceProofs,
                request.DominanceProofs),
            timeProvider,
            cancellationToken);
        return new TacticalLoadoutSearchReadResult(
            contextRead,
            discovery,
            search);
    }
}
