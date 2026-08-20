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

        var gameDataVersion = snapshot.Metadata.GameDataVersion.IsAvailable
            ? snapshot.Metadata.GameDataVersion.Value
            : TacticalContextGameDataVersions.Unavailable;
        var resolution = VerifiedTacticalCombatRuleSets
            .HistoricalMagicSound.Resolve(
                gameDataVersion,
                request.TargetGoalCodes,
                request.Evidence);

        var context = TacticalExecutionContextProjector.Project(
            snapshot,
            resolution,
            request.Proposal,
            cancellationToken);
        var latestObservationAtUtc = snapshot.FieldSources
            .Where(item => item.Source
                == TaiWu.Domain.CombatSnapshots.SnapshotDataSource
                    .CurrentScreenObservation)
            .Select(item => (DateTimeOffset?)item.CapturedAtUtc)
            .Max();
        return new TacticalExecutionContextReadResult(
            context,
            snapshot.Metadata.CapturedAtUtc,
            latestObservationAtUtc);
    }
}
