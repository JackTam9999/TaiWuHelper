using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatCounters;

public sealed record CombatCounterAccessEvaluation
{
    internal CombatCounterAccessEvaluation(
        CombatCounterRule rule,
        CombatSkillCandidateValidationResult candidateValidation,
        CombatRequirementEvaluationResult requirementEvaluation,
        IEnumerable<CombatCounterAccessIssue> issues)
    {
        Rule = rule;
        CandidateValidation = candidateValidation;
        RequirementEvaluation = requirementEvaluation;
        Issues = [.. issues];
    }

    public CombatCounterRule Rule { get; }

    public CombatSkillCandidateValidationResult CandidateValidation { get; }

    public CombatRequirementEvaluationResult RequirementEvaluation { get; }

    public ImmutableArray<CombatCounterAccessIssue> Issues { get; }

    public bool IsAccessible => Issues.IsEmpty;
}
