using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

internal static class TacticalExecutionContextProjection
{
    internal static TacticalCombatRuleResolution ResolveRules(
        CombatSnapshot snapshot,
        TacticalExecutionContextReadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var gameDataVersion = snapshot.Metadata.GameDataVersion.IsAvailable
            ? snapshot.Metadata.GameDataVersion.Value
            : TacticalContextGameDataVersions.Unavailable;
        return VerifiedTacticalCombatRuleSets.HistoricalMagicSound.Resolve(
            gameDataVersion,
            request.TargetGoalCodes,
            request.Evidence);
    }

    internal static TacticalExecutionContextReadResult Project(
        CombatSnapshot snapshot,
        TacticalExecutionContextReadRequest request,
        TacticalCombatRuleResolution resolution,
        CancellationToken cancellationToken,
        TacticalExecutionProposal? proposal = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(resolution);
        cancellationToken.ThrowIfCancellationRequested();

        var context = TacticalExecutionContextProjector.Project(
            snapshot,
            resolution,
            proposal ?? request.Proposal,
            cancellationToken);
        var latestObservationAtUtc = snapshot.FieldSources
            .Where(item => item.Source
                == SnapshotDataSource.CurrentScreenObservation)
            .Select(item => (DateTimeOffset?)item.CapturedAtUtc)
            .Max();
        return new TacticalExecutionContextReadResult(
            context,
            snapshot.Metadata.CapturedAtUtc,
            latestObservationAtUtc);
    }
}
