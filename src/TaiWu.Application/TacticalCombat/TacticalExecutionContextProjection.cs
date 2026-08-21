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
        return VerifiedTacticalCombatRuleSets.ResolveExact(
            gameDataVersion,
            request.TargetGoalCodes,
            request.Evidence);
    }

    internal static TacticalExecutionContextReadResult Project(
        CombatSnapshot snapshot,
        TacticalExecutionContextReadRequest request,
        TacticalCombatRuleResolution resolution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(resolution);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateObservationPrecedence(snapshot, request);

        var context = (request.Proposal, request.CurrentObservation) switch
        {
            (null, null) =>
                TacticalExecutionContextProjector.ProjectCurrentLoadout(
                    snapshot,
                    resolution,
                    cancellationToken),
            (null, { } observation) =>
                TacticalExecutionContextProjector.ProjectCurrentLoadout(
                    snapshot,
                    resolution,
                    observation,
                    cancellationToken),
            ({ } proposal, null) =>
                TacticalExecutionContextProjector.Project(
                    snapshot,
                    resolution,
                    proposal,
                    cancellationToken),
            ({ } proposal, { } observation) =>
                TacticalExecutionContextProjector.ProjectObserved(
                    snapshot,
                    resolution,
                    observation,
                    proposal,
                    cancellationToken)
        };
        var snapshotObservationAtUtc = snapshot.FieldSources
            .Where(item => item.Source
                == SnapshotDataSource.CurrentScreenObservation)
            .Select(item => (DateTimeOffset?)item.CapturedAtUtc)
            .Max();
        var latestObservationAtUtc = new[]
            {
                snapshotObservationAtUtc,
                request.CurrentObservationAtUtc
            }
            .Max();
        return new TacticalExecutionContextReadResult(
            context,
            snapshot.Metadata.CapturedAtUtc,
            latestObservationAtUtc);
    }

    private static void ValidateObservationPrecedence(
        CombatSnapshot snapshot,
        TacticalExecutionContextReadRequest request)
    {
        if (request.CurrentObservation is null
            || !request.CurrentObservationAtUtc.HasValue
            || !snapshot.Metadata.SaveLastWriteTimeUtc.IsAvailable)
        {
            return;
        }

        if (request.CurrentObservationAtUtc.Value
            <= snapshot.Metadata.SaveLastWriteTimeUtc.Value)
        {
            throw new ArgumentException(
                "A tactical execution observation must be newer than the save.",
                nameof(request));
        }
    }
}
