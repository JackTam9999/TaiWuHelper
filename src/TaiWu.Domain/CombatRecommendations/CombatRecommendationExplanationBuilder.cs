using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;

namespace TaiWu.Domain.CombatRecommendations;

public static class CombatRecommendationExplanationBuilder
{
    public static CombatRecommendationExplanation Build(
        PlayerCombatSnapshot player,
        IEnumerable<TargetThreat> targetThreats,
        ManualCombatPlan plan)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(targetThreats);
        ArgumentNullException.ThrowIfNull(plan);

        var threats = targetThreats.ToArray();
        if (threats.Any(threat => threat is null))
        {
            throw new ArgumentException(
                "Target threats cannot contain null entries.",
                nameof(targetThreats));
        }

        var duplicateThreat = threats
            .GroupBy(threat => threat.Code, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateThreat is not null)
        {
            throw new ArgumentException(
                $"Duplicate target threat '{duplicateThreat.Key}'.",
                nameof(targetThreats));
        }

        var threatsByCode = threats.ToDictionary(
            threat => threat.Code,
            StringComparer.Ordinal);
        var selected = plan.SelectedRecommendation;
        List<RecommendationCaveat> caveats =
        [
            .. ThreatEvidenceCaveats(threats)
        ];
        AddDamageCaveat(selected, caveats);

        var skills = selected.Candidate.SelectedOptions
            .OrderBy(option =>
                FindSkill(player, option.Candidate.SkillId).Category)
            .ThenBy(option => option.Candidate.SkillId)
            .Select(option => ExplainSkill(
                player,
                plan,
                option,
                threatsByCode,
                caveats))
            .ToArray();

        return new CombatRecommendationExplanation(
            selected.Candidate.StableKey,
            skills,
            caveats
                .OrderBy(caveat => caveat.Kind)
                .ThenBy(caveat => caveat.SkillId)
                .ThenBy(caveat => caveat.Code, StringComparer.Ordinal));
    }

    private static SkillRecommendationExplanation ExplainSkill(
        PlayerCombatSnapshot player,
        ManualCombatPlan plan,
        CombatLoadoutOption option,
        Dictionary<string, TargetThreat> threatsByCode,
        List<RecommendationCaveat> caveats)
    {
        var skill = FindSkill(player, option.Candidate.SkillId);
        AddSkillDataCaveats(skill, caveats);

        var threatLinks = new List<SkillThreatExplanation>();
        foreach (var threatCode in option.ThreatCodes)
        {
            if (threatsByCode.TryGetValue(threatCode, out var threat))
            {
                threatLinks.Add(new SkillThreatExplanation(threat));
            }
            else
            {
                caveats.Add(
                    Gap(
                        "THREAT_DETAILS_UNAVAILABLE",
                        $"Skill {skill.SkillId} references threat "
                        + $"'{threatCode}', but its structured threat "
                        + "details were not supplied.",
                        skill.SkillId,
                        [option.EvidenceReference]));
            }
        }

        var validation = CombatSkillCandidateValidator.Validate(
            player,
            option.Candidate);
        var cost = CombatSkillCostCalculator.Calculate(
            player,
            skill.SkillId);
        AddCostCaveats(cost, skill.SkillId, caveats);

        var requirementEvaluation = CombatRequirementEvaluator.Evaluate(
            option.Requirements,
            plan.SelectedRecommendation.Candidate.FeasibleLoadout
                .Proposal.RequirementContext);
        foreach (var evaluation in requirementEvaluation.Evaluations.Where(
                     value =>
                         value.Status == CombatRequirementStatus.Unknown))
        {
            caveats.Add(
                Gap(
                    "CONDITION_STATUS_UNAVAILABLE",
                    evaluation.Reason,
                    skill.SkillId,
                    [evaluation.Requirement.EvidenceReference]));
        }

        return new SkillRecommendationExplanation(
            skill,
            ReasonsForSkill(plan, option),
            threatLinks,
            CounterFor(option),
            new SkillDirectionExplanation(
                skill.Direction,
                option.Candidate.RequiredDirection,
                validation.RequiredDirectionChange.HasValue,
                option.ExpectedEffectId,
                option.EvidenceReference),
            new SkillCostExplanation(
                cost,
                plan.SelectedRecommendation.Candidate.FeasibleLoadout
                    .SlotBudgets[skill.Category],
                CostEvidence(cost, skill.SkillId)),
            requirementEvaluation.Evaluations.Select(
                evaluation => new SkillConditionExplanation(
                    ConditionKind(evaluation.Requirement),
                    evaluation.Requirement.Criticality,
                    evaluation.Status,
                    evaluation.Reason,
                    evaluation.Requirement.EvidenceReference)));
    }

    private static RecommendationReason[] ReasonsForSkill(
        ManualCombatPlan plan,
        CombatLoadoutOption option)
    {
        var skillId = option.Candidate.SkillId;
        var reasons = plan.LoadoutChanges
            .Where(change => change.SkillId == skillId)
            .Select(change => change.Reason)
            .Concat(
                plan.OpeningActions
                    .Where(action => action.SkillId == skillId)
                    .Select(action => action.Reason))
            .DistinctBy(reason => (
                reason.Code,
                string.Join("|", reason.EvidenceReferences)))
            .ToArray();

        return reasons.Length > 0
            ? reasons
            :
            [
                new RecommendationReason(
                    "SELECTED_FEASIBLE_SKILL",
                    "The skill is part of the highest-ranked feasible "
                    + "loadout.",
                    [option.EvidenceReference],
                    option.ThreatCodes)
            ];
    }

    private static SkillCounterExplanation CounterFor(
        CombatLoadoutOption option)
    {
        return option.CounterStrength.HasValue
            ? new SkillCounterExplanation(
                isAvailable: true,
                option.CounterStrength,
                option.ActivationTiming,
                option.EvidenceReference,
                unavailableReason: null)
            : new SkillCounterExplanation(
                isAvailable: false,
                strength: null,
                activationTiming: null,
                evidenceReference: null,
                "No verified counter mapping is attached; this skill was "
                + "selected for another stated reason.");
    }

    private static RecommendationConditionKind ConditionKind(
        CombatRequirement requirement) => requirement switch
        {
            WeaponRequirement => RecommendationConditionKind.Weapon,
            TrickRequirement => RecommendationConditionKind.Trick,
            RangeRequirement => RecommendationConditionKind.Range,
            ResourceRequirement => RecommendationConditionKind.Resource,
            WeaponUnlockRequirement =>
                RecommendationConditionKind.WeaponUnlock,
            SkillActivationRequirement =>
                RecommendationConditionKind.SkillActivation,
            _ => throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement.GetType().Name,
                "Unknown combat requirement.")
        };

    private static string[] CostEvidence(
        CombatSkillCostBreakdown cost,
        int skillId)
    {
        var assignmentEvidence = cost
            .AppliedLegendaryBookCostAssignments
            .Select(assignment => assignment.AssignmentEvidenceReference);
        return
        [
            $"snapshot:player:learned-skill:{skillId}:cost",
            .. assignmentEvidence
        ];
    }

    private static void AddSkillDataCaveats(
        CombatSkillSnapshot skill,
        List<RecommendationCaveat> caveats)
    {
        AddUnavailable(
            skill.DisplayName,
            "SKILL_NAME_UNAVAILABLE",
            skill.SkillId,
            caveats);
        AddUnavailable(
            skill.Direction,
            "SKILL_DIRECTION_UNAVAILABLE",
            skill.SkillId,
            caveats);
    }

    private static void AddCostCaveats(
        CombatSkillCostBreakdown cost,
        int skillId,
        List<RecommendationCaveat> caveats)
    {
        AddUnavailable(
            cost.BaseCost,
            "BASE_COST_UNAVAILABLE",
            skillId,
            caveats);
        AddUnavailable(
            cost.MasteryReduction,
            "MASTERY_REDUCTION_UNAVAILABLE",
            skillId,
            caveats);
        AddUnavailable(
            cost.LegendaryBookReduction,
            "LEGENDARY_BOOK_REDUCTION_UNAVAILABLE",
            skillId,
            caveats);
        AddUnavailable(
            cost.EffectiveCost,
            "EFFECTIVE_COST_UNAVAILABLE",
            skillId,
            caveats);
    }

    private static void AddUnavailable<T>(
        SnapshotValue<T> value,
        string code,
        int skillId,
        List<RecommendationCaveat> caveats)
    {
        if (value.IsAvailable)
        {
            return;
        }

        caveats.Add(
            Gap(
                code,
                value.UnavailableReason!,
                skillId,
                evidenceReferences: []));
    }

    private static IEnumerable<RecommendationCaveat>
        ThreatEvidenceCaveats(IEnumerable<TargetThreat> threats)
    {
        return threats.SelectMany(threat => threat.Evidence)
            .Where(evidence =>
                evidence.Confidence
                    is TargetThreatEvidenceConfidence.Hypothesis
                    or TargetThreatEvidenceConfidence.PlayerObservation
                    or TargetThreatEvidenceConfidence
                        .CurrentScreenObservation)
            .Select(evidence => new RecommendationCaveat(
                RecommendationCaveatKind.Assumption,
                AssumptionCode(evidence.Confidence),
                "This threat input is observational or hypothetical and "
                + "may not represent stable game data.",
                skillId: null,
                [evidence.Reference]));
    }

    private static string AssumptionCode(
        TargetThreatEvidenceConfidence confidence) => confidence switch
        {
            TargetThreatEvidenceConfidence.CurrentScreenObservation =>
                "THREAT_CURRENT_SCREEN_OBSERVATION",
            TargetThreatEvidenceConfidence.PlayerObservation =>
                "THREAT_PLAYER_OBSERVATION",
            TargetThreatEvidenceConfidence.Hypothesis =>
                "THREAT_HYPOTHESIS",
            _ => throw new ArgumentOutOfRangeException(
                nameof(confidence),
                confidence,
                "This evidence confidence is not an assumption.")
        };

    private static void AddDamageCaveat(
        ScoredCombatLoadout selected,
        List<RecommendationCaveat> caveats)
    {
        var damage = selected.Get(
            RecommendationScoreComponentKind.DamagePotential);
        if (damage.IsAvailable)
        {
            return;
        }

        caveats.Add(
            Gap(
                "DAMAGE_EVIDENCE_UNAVAILABLE",
                damage.Explanation,
                skillId: null,
                [damage.EvidenceReference]));
    }

    private static RecommendationCaveat Gap(
        string code,
        string explanation,
        int? skillId,
        IEnumerable<string> evidenceReferences)
    {
        return new RecommendationCaveat(
            RecommendationCaveatKind.UnavailableData,
            code,
            explanation,
            skillId,
            evidenceReferences);
    }

    private static CombatSkillSnapshot FindSkill(
        PlayerCombatSnapshot player,
        int skillId)
    {
        return player.LearnedSkills.Single(
            skill => skill.SkillId == skillId);
    }
}
