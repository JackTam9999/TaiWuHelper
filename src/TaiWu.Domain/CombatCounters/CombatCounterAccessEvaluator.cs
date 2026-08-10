using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatCounters;

public static class CombatCounterAccessEvaluator
{
    public static CombatCounterAccessReport Evaluate(
        PlayerCombatSnapshot player,
        CombatRequirementContext requirementContext,
        CombatCounterRuleSet ruleSet,
        bool allowBreakthrough = false,
        bool evaluateProposedSelection = false)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(requirementContext);
        ArgumentNullException.ThrowIfNull(ruleSet);

        return new CombatCounterAccessReport(
            ruleSet.Rules.Select(
                rule => EvaluateRule(
                    player,
                    requirementContext,
                    rule,
                    allowBreakthrough,
                    evaluateProposedSelection)));
    }

    private static CombatCounterAccessEvaluation EvaluateRule(
        PlayerCombatSnapshot player,
        CombatRequirementContext requirementContext,
        CombatCounterRule rule,
        bool allowBreakthrough,
        bool evaluateProposedSelection)
    {
        var candidateValidation = CombatSkillCandidateValidator.Validate(
            player,
            new CombatSkillCandidate(
                rule.Effect.SkillId,
                requiredDirection: rule.RequiredDirection,
                allowBreakthrough: allowBreakthrough));
        var evaluatedContext = evaluateProposedSelection
            ? WithProposedSelection(requirementContext, rule)
            : requirementContext;
        var requirementEvaluation = CombatRequirementEvaluator.Evaluate(
            rule.Requirements,
            evaluatedContext);
        List<CombatCounterAccessIssue> issues = [];

        issues.AddRange(
            candidateValidation.Rejections.Select(
                rejection => new CombatCounterAccessIssue(
                    CombatCounterAccessIssueCode.CandidateRejected,
                    rejection.Reason)));
        ValidateEffectIdentity(rule, candidateValidation, issues);
        issues.AddRange(
            requirementEvaluation.Rejections.Select(
                rejection => new CombatCounterAccessIssue(
                    CombatCounterAccessIssueCode.RequirementRejected,
                    rejection.Reason)));

        return new CombatCounterAccessEvaluation(
            rule,
            candidateValidation,
            requirementEvaluation,
            issues);
    }

    private static CombatRequirementContext WithProposedSelection(
        CombatRequirementContext source,
        CombatCounterRule rule)
    {
        var equippedSkillIds = source.EquippedSkillIds
            .Append(rule.Effect.SkillId)
            .Distinct()
            .Order()
            .ToArray();
        int? activeDefenseSkillId = rule.ActivationTiming
            == CombatCounterActivationTiming.ActiveDefense
                ? rule.Effect.SkillId
                : null;
        int? activeAgilitySkillId = rule.ActivationTiming
            == CombatCounterActivationTiming.ActiveAgility
                ? rule.Effect.SkillId
                : null;
        return new CombatRequirementContext(
            source.EquippedWeaponTypeIds,
            source.TrickCounts.Select(value =>
                new CombatTrickCount(value.Key, value.Value)),
            source.Distance,
            source.Resources.Select(value =>
                new CombatResourceAmount(value.Key, value.Value)),
            source.UnlockedWeaponTypeIds,
            equippedSkillIds,
            activeDefenseSkillId,
            activeAgilitySkillId);
    }

    private static void ValidateEffectIdentity(
        CombatCounterRule rule,
        CombatSkillCandidateValidationResult candidateValidation,
        List<CombatCounterAccessIssue> issues)
    {
        if (candidateValidation.Skill is null)
        {
            return;
        }

        var observedEffect = rule.RequiredDirection
            == PracticeDirection.Direct
            ? candidateValidation.Skill.DirectEffectId
            : candidateValidation.Skill.ReverseEffectId;
        if (observedEffect.IsAvailable
            && observedEffect.Value != rule.Effect.RawEffectId)
        {
            issues.Add(
                new CombatCounterAccessIssue(
                    CombatCounterAccessIssueCode.EffectIdMismatch,
                    $"Skill {rule.Effect.SkillId} {rule.RequiredDirection} "
                    + $"effect {observedEffect.Value} does not match "
                    + $"verified effect {rule.Effect.RawEffectId}."));
        }
    }
}
