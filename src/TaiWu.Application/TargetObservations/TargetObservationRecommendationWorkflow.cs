using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Application.TargetObservations;

public sealed class TargetObservationRecommendationWorkflow(
    ICombatSnapshotReader reader,
    IResolveTargetSkillSelection resolver)
    : ITargetObservationRecommendationWorkflow
{
    public async Task<CombatLoadoutRecommendation> ExecuteAsync(
        RecommendCombatLoadoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var targetRequest = request.TargetObservation
            ?? throw new ArgumentException(
                "The target-observation workflow requires an observation.",
                nameof(request));
        cancellationToken.ThrowIfCancellationRequested();

        var snapshot = await reader.ReadAsync(
            new CombatSnapshotReadRequest(
                request.SaveFilePath,
                request.TargetCharacterId,
                request.CurrentLoadoutObservation,
                request.Language),
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var processing = await ProcessAsync(
            snapshot,
            targetRequest,
            request.Language,
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var baseline = RecommendCombatLoadout.Build(
            snapshot,
            request.Policy,
            targetObservation: null,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var observed = RecommendCombatLoadout.Build(
            processing.Merge.Snapshot,
            request.Policy,
            processing,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var impact = TargetObservationRecommendationImpactAnalyzer.Compare(
            baseline,
            observed,
            processing.Merge);
        return observed.WithTargetObservationImpact(impact);
    }

    private async Task<TargetObservationProcessingResult> ProcessAsync(
        CombatSnapshot snapshot,
        TargetObservationRequest request,
        TaiwuLanguage language,
        CancellationToken cancellationToken)
    {
        var targetSkillIds = snapshot.Target.LearnedSkills
            .Select(skill => skill.SkillId)
            .ToArray();
        List<ResolvedTargetSkillSelection> resolved = [];
        for (var index = 0; index < request.SelectedSkills.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var selection = request.SelectedSkills[index];
            var result = await resolver.ExecuteAsync(
                new TargetSkillSelectionRequest(
                    request.Context,
                    language == TaiwuLanguage.Chinese
                        ? CatalogueLanguage.TraditionalChinese
                        : CatalogueLanguage.English,
                    selection.VisibleName,
                    selection.Category,
                    selection.ConfirmedSkillId,
                    selection.Direction,
                    selection.SlotIndex,
                    targetSkillIds),
                cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status != TargetSkillSelectionStatus.Resolved)
            {
                throw new TargetObservationResolutionException(
                    result.Status,
                    index,
                    result.Candidates);
            }

            resolved.Add(result.ResolvedSelection!);
        }

        TargetLoadoutObservation observation;
        try
        {
            observation = new TargetLoadoutObservation(
                snapshot.Target.CharacterId,
                request.Context,
                request.ObservedAtUtc,
                request.EvidenceReference,
                CreateCoverage(request.Coverage),
                resolved.Select(value => value.Observation));
        }
        catch (ArgumentException)
        {
            throw new TargetObservationResolutionException(
                TargetSkillSelectionStatus.ConfirmationInvalid,
                selectionIndex: 0);
        }

        var staticSnapshots = resolved
            .Where(value => value.SnapshotPresence
                != TargetSkillSnapshotPresence.Present)
            .Select(value => value.StaticFacts.CreateSnapshot(
                value.Observation))
            .ToArray();
        var merge = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation,
            staticSnapshots,
            request.ConfirmPrecedenceWhenSaveTimeUnavailable);
        return new TargetObservationProcessingResult(
            snapshot,
            merge,
            resolved);
    }

    private static TargetLoadoutCoverage CreateCoverage(
        TargetLoadoutCoverageKind coverage) => coverage switch
        {
            TargetLoadoutCoverageKind.PartialLoadout =>
                TargetLoadoutCoverage.PartialLoadout,
            TargetLoadoutCoverageKind.CompleteCurrentLoadout =>
                TargetLoadoutCoverage.CompleteCurrentLoadout(
                    TargetLoadoutCompletenessEvidence.FromE3000(
                        TargetLoadoutCompletenessEvidence
                            .E3000GameDataVersion)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(coverage),
                coverage,
                "Unknown target-observation coverage.")
        };
}
