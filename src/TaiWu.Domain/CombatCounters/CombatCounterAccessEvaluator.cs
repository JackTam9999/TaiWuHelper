using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatCounters;

public static class CombatCounterAccessEvaluator
{
    public static CombatCounterAccessReport Evaluate(
        PlayerCombatSnapshot player,
        CombatRequirementContext requirementContext,
        CombatCounterRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(requirementContext);
        ArgumentNullException.ThrowIfNull(ruleSet);

        return new CombatCounterAccessReport(
            ruleSet.Rules.Select(
                rule => EvaluateRule(
                    player,
                    requirementContext,
                    rule)));
    }

    private static CombatCounterAccessEvaluation EvaluateRule(
        PlayerCombatSnapshot player,
        CombatRequirementContext requirementContext,
        CombatCounterRule rule)
    {
        var candidateValidation = CombatSkillCandidateValidator.Validate(
            player,
            new CombatSkillCandidate(
                rule.Effect.SkillId,
                requiredDirection: rule.RequiredDirection));
        var requirementEvaluation = CombatRequirementEvaluator.Evaluate(
            rule.Requirements,
            requirementContext);
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
