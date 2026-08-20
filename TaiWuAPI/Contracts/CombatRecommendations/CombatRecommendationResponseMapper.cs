using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.LoadoutComparisons;
using TaiWu.Application.Localization;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public static class CombatRecommendationResponseMapper
{
    public static CombatRecommendationResponse Map(
        TacticalCombatRecommendationResult result,
        TaiwuLanguage language = TaiwuLanguage.English)
    {
        ArgumentNullException.ThrowIfNull(result);
        var recommendation = result.LegacyRecommendation
            ?? throw new ArgumentException(
                "A tactical API response requires its coherent legacy recommendation.",
                nameof(result));
        return Map(recommendation, language) with
        {
            TacticalPlanning = TacticalCombatResponseMapper.Map(result)
        };
    }

    public static CombatRecommendationResponse Map(
        CombatLoadoutRecommendation recommendation,
        TaiwuLanguage language = TaiwuLanguage.English)
    {
        ArgumentNullException.ThrowIfNull(recommendation);
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(nameof(language));
        }

        var comparison = CombatLoadoutComparisonBuilder.Build(recommendation);
        var snapshotReference = comparison.SnapshotReference.Value;
        var threats = recommendation.RecommendationThreats
            .Select(value => new CombatThreatResponse(
                ThreatReference(value.Code),
                value.Code,
                value.Title,
                value.Severity,
                value.ActivationTiming,
                [.. value.Evidence.Select(evidence => evidence.Reference)]))
            .ToArray();
        var styles = recommendation.Styles
            .Select(style => MapStyle(snapshotReference, style))
            .ToArray();

        return new CombatRecommendationResponse(
            snapshotReference,
            recommendation.Snapshot.Metadata.CapturedAtUtc,
            recommendation.Snapshot.Metadata.GameDataVersion.IsAvailable
                ? recommendation.Snapshot.Metadata.GameDataVersion.Value
                : null,
            recommendation.RequestedPolicy,
            threats,
            styles,
            MapWarnings(recommendation),
            MapInnerPowerState(recommendation.Snapshot.Player),
            MapTargetObservation(recommendation),
            LoadoutComparisonResponseMapper.Map(
                comparison,
                recommendation),
            recommendation.TargetPlaybook is null
                ? null
                : TargetStrategyResponseMapper.Map(
                    recommendation.TargetPlaybook,
                    recommendation.Snapshot.Player,
                    language));
    }

    private static TargetObservationResponse? MapTargetObservation(
        CombatLoadoutRecommendation recommendation)
    {
        var processing = recommendation.TargetObservation;
        if (processing is null)
        {
            return null;
        }

        var merge = processing.Merge;
        var observation = merge.Observation;
        var sources = merge.LoadoutEvidence.Observations
            .Select(value => new TargetObservationSourceResponse(
                value.Source.FieldPath,
                value.Source.Source,
                value.Source.CapturedAtUtc,
                value.Source.EvidenceReference,
                merge.LoadoutEvidence.Status))
            .Concat(merge.DirectionEvidence.SelectMany(value =>
                value.Evidence.Observations.Select(observationValue =>
                    new TargetObservationSourceResponse(
                        observationValue.Source.FieldPath,
                        observationValue.Source.Source,
                        observationValue.Source.CapturedAtUtc,
                        observationValue.Source.EvidenceReference,
                        value.Evidence.Status))))
            .Concat(merge.Snapshot.FieldSources
                .Where(value => value.FieldPath
                    == TargetLoadoutObservationMerger
                        .TargetLoadoutObservationField)
                .Select(value => new TargetObservationSourceResponse(
                    value.FieldPath,
                    value.Source,
                    value.CapturedAtUtc,
                    value.EvidenceReference,
                    SnapshotEvidenceStatus.Available)))
            .Distinct()
            .OrderBy(value => value.CapturedAtUtc)
            .ThenBy(value => value.Field, StringComparer.Ordinal)
            .ThenBy(value => value.EvidenceReference, StringComparer.Ordinal)
            .ToArray();

        return new TargetObservationResponse(
            observation.TargetCharacterId,
            observation.ObservationContext,
            observation.ObservedAtUtc,
            observation.EvidenceReference,
            observation.Coverage.Kind,
            merge.Status,
            merge.LoadoutEvidence.Status,
            [.. processing.ResolvedSkills.Select(value =>
                new TargetObservedSkillResponse(
                    value.Observation.SkillId,
                    AvailableName(value.StaticFacts.DisplayName),
                    value.Observation.Category,
                    value.Observation.Direction,
                    value.Observation.SlotIndex,
                    value.Observation.VisiblePowerPercent,
                    value.SnapshotPresence))],
            sources,
            MapTargetObservationImpact(processing.OriginalSnapshot, merge),
            MapTargetObservationRecommendationImpact(
                recommendation.TargetObservationImpact));
    }

    private static TargetObservationRecommendationImpactResponse?
        MapTargetObservationRecommendationImpact(
            TargetObservationRecommendationImpact? impact)
    {
        if (impact is null)
        {
            return null;
        }

        return new TargetObservationRecommendationImpactResponse(
            [.. impact.Threats.Select(value => new TargetThreatImpactResponse(
                value.ThreatCode,
                value.Title,
                value.Kind,
                value.Severity,
                value.SourceKinds,
                value.EvidenceReferences))],
            [.. impact.FeasibilityChanges.Select(MapRecommendationImpact)],
            [.. impact.ScoringChanges.Select(MapRecommendationImpact)],
            [.. impact.UnsupportedEvidence.Select(value =>
                new TargetUnresolvedEvidenceImpactResponse(
                    value.Code,
                    value.WasPresentBefore,
                    value.EvidenceReference,
                    value.SkillId,
                    value.RawEffectId))],
            impact.PartialCoverageLeavesUnknown,
            [.. impact.Conflicts.Select(value =>
                new TargetObservationConflictImpactResponse(
                    value.Field,
                    value.ReasonCode,
                    value.PrecedenceRule,
                    [.. value.Sources.Select(source =>
                        new TargetObservationConflictSourceResponse(
                            source.Source,
                            source.CapturedAtUtc,
                            source.EvidenceReference))]))],
            "Evidence provenance only; not a win probability.");
    }

    private static TargetRecommendationImpactResponse MapRecommendationImpact(
        TargetRecommendationImpact value) => new(
            value.Policy,
            value.Kind,
            value.Cause,
            value.SkillId,
            value.Category,
            value.RequiredDirection,
            value.ThreatCodes,
            value.ThreatTitles,
            value.EvidenceReferences);

    private static TargetObservationImpactResponse MapTargetObservationImpact(
        CombatSnapshot original,
        TargetLoadoutObservationMergeResult merge)
    {
        if (merge.Status != TargetLoadoutMergeStatus.Applied)
        {
            return new TargetObservationImpactResponse(
                Applied: false,
                AddedTargetSkillIds: [],
                AddedEquippedSkillIds: [],
                RemovedEquippedSkillIds: [],
                ChangedDirectionSkillIds: []);
        }

        var merged = merge.Snapshot;
        var originalLearned = original.Target.LearnedSkills
            .Select(value => value.SkillId)
            .ToHashSet();
        var originalEquipped = EquippedIds(original.Target.EquippedSkills);
        var mergedEquipped = EquippedIds(merged.Target.EquippedSkills);
        var originalDirections = original.Target.LearnedSkills
            .ToDictionary(value => value.SkillId, value => value.Direction);
        var changedDirections = merge.DirectionEvidence
            .Select(value => value.SkillId)
            .Where(skillId =>
            {
                var mergedSkill = merged.Target.LearnedSkills.Single(
                    value => value.SkillId == skillId);
                return mergedSkill.Direction.IsAvailable
                    && (!originalDirections.TryGetValue(
                            skillId,
                            out var originalDirection)
                        || !originalDirection.IsAvailable
                        || originalDirection.Value
                            != mergedSkill.Direction.Value);
            })
            .Order()
            .ToArray();

        return new TargetObservationImpactResponse(
            Applied: true,
            [.. merged.Target.LearnedSkills
                .Select(value => value.SkillId)
                .Where(skillId => !originalLearned.Contains(skillId))
                .Order()],
            [.. mergedEquipped.Except(originalEquipped).Order()],
            [.. originalEquipped.Except(mergedEquipped).Order()],
            changedDirections);
    }

    private static HashSet<int> EquippedIds(
        SnapshotValue<CombatLoadoutSnapshot> loadout) =>
        loadout.IsAvailable
            ? Enum.GetValues<SkillCategory>()
                .SelectMany(category => loadout.Value.Get(category))
                .ToHashSet()
            : [];

    internal static string? AvailableName(CombatSkillDisplayName displayName) =>
        displayName.Value.IsAvailable
            ? displayName.Value.Value.Text
            : null;

    internal static TargetObservationProblemCandidateResponse MapCandidate(
        TargetSkillResolutionCandidate candidate) => new(
            candidate.SkillId,
            AvailableName(candidate.DisplayName),
            candidate.StaticFacts?.Category,
            candidate.MatchKind,
            candidate.SnapshotPresence);

    private static InnerPowerStateResponse? MapInnerPowerState(
        TaiWu.Domain.CombatSnapshots.PlayerCombatSnapshot player)
    {
        if (!player.InnerPowerState.IsAvailable)
        {
            return null;
        }

        var state = player.InnerPowerState.Value;
        return new InnerPowerStateResponse(
            state.DisplayName.IsAvailable ? state.DisplayName.Value : null,
            state.EffectDescription.IsAvailable
                ? state.EffectDescription.Value
                : null,
            state.BacklashOnUseElement);
    }

    private static CombatRecommendationStyleResponse MapStyle(
        string snapshotReference,
        CombatRecommendationStyleResult style)
    {
        var plan = style.ManualPlan.Plan;
        if (plan is null)
        {
            return new CombatRecommendationStyleResponse(
                snapshotReference,
                style.Policy,
                HasRecommendation: false,
                CandidateReference: null,
                TotalScore: null,
                Scores: [],
                Skills: [],
                ManualChanges: [],
                OpeningActions: [],
                SwitchingConditions: [],
                Caveats: [],
                style.ManualPlan.Diagnostic,
                GenericSlots: null);
        }

        var candidateReference =
            $"candidate:{plan.SelectedRecommendation.Candidate.StableKey}";
        var explanation = style.Explanation!;
        return new CombatRecommendationStyleResponse(
            snapshotReference,
            style.Policy,
            HasRecommendation: true,
            candidateReference,
            plan.SelectedRecommendation.TotalScore,
            [.. plan.SelectedRecommendation.Components
                .Select(component => new RecommendationScoreResponse(
                    component.Kind,
                    component.Weight,
                    component.Score,
                    component.WeightedPoints,
                    component.Explanation,
                    component.EvidenceReference))],
            [.. explanation.Skills.Select(skill => MapSkill(candidateReference, skill))],
            [.. plan.LoadoutChanges.Select(change => MapChange(candidateReference, change))],
            [.. plan.OpeningActions
                .Select(action => MapStep(
                    candidateReference,
                    "opening",
                    action))],
            [.. plan.SwitchingConditions
                .Select(action => MapStep(
                    candidateReference,
                    "switch",
                    action))],
            [.. explanation.Caveats
                .Select((caveat, index) => new RecommendationCaveatResponse(
                    $"{candidateReference}:caveat:{caveat.Code}:{index + 1}",
                    caveat.Kind,
                    caveat.Code,
                    caveat.Explanation,
                    caveat.SkillId,
                    caveat.EvidenceReferences))],
            Diagnostic: null,
            GenericSlots: MapGenericSlots(
                plan.SelectedRecommendation.Candidate.FeasibleLoadout
                    .Proposal.GenericSlotAllocation));
    }

    private static GenericSlotPlanResponse MapGenericSlots(
        TaiWu.Domain.CombatSnapshots.GenericSlotAllocation allocation)
    {
        return new GenericSlotPlanResponse(
            allocation.TotalSlots,
            allocation.Attack,
            allocation.Agility,
            allocation.Defense,
            allocation.Assistance);
    }

    private static RecommendedSkillResponse MapSkill(
        string candidateReference,
        SkillRecommendationExplanation skill)
    {
        return new RecommendedSkillResponse(
            SkillReference(candidateReference, skill.SkillId),
            skill.SkillId,
            skill.DisplayName.IsAvailable
                ? skill.DisplayName.Value
                : null,
            skill.Category,
            skill.Direction.CurrentDirection.IsAvailable
                ? skill.Direction.CurrentDirection.Value
                : null,
            skill.Direction.RequiredDirection,
            skill.Direction.RequiresManualDirectionChange,
            skill.Cost.EffectiveCost.IsAvailable
                ? skill.Cost.EffectiveCost.Value
                : null,
            skill.Counter.Strength,
            skill.Counter.ActivationTiming,
            [.. skill.Reasons.Select(reason => MapReason(
                    candidateReference,
                    skill.SkillId,
                    reason))],
            skill.Direction.RequiresBreakthrough);
    }

    private static ManualLoadoutChangeResponse MapChange(
        string candidateReference,
        ManualLoadoutChange change)
    {
        return new ManualLoadoutChangeResponse(
            $"{candidateReference}:change:{change.Kind}:"
            + $"{change.Category}:{change.SkillId}",
            change.Kind,
            change.Category,
            change.SkillId,
            change.RequiredDirection,
            MapReason(
                candidateReference,
                change.SkillId,
                change.Reason));
    }

    private static CombatPlanStepResponse MapStep(
        string candidateReference,
        string phase,
        BattlePlanInstruction instruction)
    {
        var reasonSkillId = instruction.AlternativeSkillId
            ?? instruction.SkillId;
        return new CombatPlanStepResponse(
            $"{candidateReference}:plan:{phase}:{instruction.Sequence}",
            instruction.Kind,
            instruction.SkillId,
            instruction.AlternativeSkillId,
            instruction.Condition,
            MapReason(
                candidateReference,
                reasonSkillId,
                instruction.Reason));
    }

    private static RecommendationReasonResponse MapReason(
        string candidateReference,
        int skillId,
        RecommendationReason reason)
    {
        return new RecommendationReasonResponse(
            ReasonReference(candidateReference, skillId, reason.Code),
            reason.Code,
            reason.Summary,
            reason.EvidenceReferences,
            [.. reason.ThreatCodes.Select(ThreatReference)]);
    }

    private static CombatRecommendationWarningResponse[] MapWarnings(
        CombatLoadoutRecommendation recommendation)
    {
        var snapshotWarnings = recommendation.SnapshotWarnings
            .Select((warning, index) =>
                new CombatRecommendationWarningResponse(
                    $"warning:snapshot:{warning.Code}:{index + 1}",
                    "Snapshot",
                    warning.Code,
                    Occurrences: 1,
                    warning.Message,
                    EvidenceReferences: Array.Empty<string>()));
        var threatWarnings = recommendation.ThreatAnalysis.Warnings
            .Select((warning, index) =>
                new CombatRecommendationWarningResponse(
                    $"warning:threat:{warning.Code}:{index + 1}",
                    "ThreatAnalysis",
                    warning.Code,
                    Occurrences: 1,
                    warning.Message,
                    [warning.Mechanic.EvidenceReference]));
        var generationWarnings = recommendation.Generation.Diagnostics
            .Select((warning, index) =>
                new CombatRecommendationWarningResponse(
                    $"warning:generation:{warning.Code}:{index + 1}",
                    "CandidateGeneration",
                    warning.Code.ToString(),
                    warning.Occurrences,
                    GenerationWarningMessage(warning),
                    EvidenceReferences: Array.Empty<string>()));

        return
        [
            .. snapshotWarnings,
            .. threatWarnings,
            .. generationWarnings
        ];
    }

    private static string GenerationWarningMessage(
        CombatLoadoutGenerationDiagnostic warning) =>
        warning.Occurrences == 1
            ? warning.Reason
            : $"{warning.Reason} Occurred in {warning.Occurrences} explored "
              + "combinations.";

    private static string ThreatReference(string code) =>
        $"threat:{code}";

    private static string SkillReference(
        string candidateReference,
        int skillId) =>
        $"{candidateReference}:skill:{skillId}";

    private static string ReasonReference(
        string candidateReference,
        int skillId,
        string code) =>
        $"{SkillReference(candidateReference, skillId)}:reason:{code}";
}
