using TaiWu.Application.CombatRecommendations;
using TaiWu.Domain.CombatRecommendations;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public static class CombatRecommendationResponseMapper
{
    public static CombatRecommendationResponse Map(
        CombatLoadoutRecommendation recommendation)
    {
        ArgumentNullException.ThrowIfNull(recommendation);

        var snapshotReference =
            $"snapshot:{recommendation.Snapshot.Metadata.CapturedAtUtc:O}";
        var threats = recommendation.ThreatAnalysis.Threats
            .Select(value => new CombatThreatResponse(
                ThreatReference(value.Threat.Code),
                value.Threat.Code,
                value.Threat.Title,
                value.Threat.Severity,
                value.Threat.ActivationTiming,
                [.. value.Threat.Evidence.Select(evidence => evidence.Reference)]))
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
            MapInnerPowerState(recommendation.Snapshot.Player));
    }

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
