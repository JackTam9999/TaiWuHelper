using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatLoadoutFeasibilityResult
{
    internal CombatLoadoutFeasibilityResult(
        ProposedCombatLoadout proposal,
        IEnumerable<CombatSkillCandidateValidationResult>
            candidateValidations,
        CombatRequirementEvaluationResult requirementEvaluation,
        SlotBudgetSet? slotBudgets,
        IEnumerable<CombatLoadoutFeasibilityFailure> failures)
    {
        Proposal = proposal
            ?? throw new ArgumentNullException(nameof(proposal));
        RequirementEvaluation = requirementEvaluation
            ?? throw new ArgumentNullException(
                nameof(requirementEvaluation));
        ArgumentNullException.ThrowIfNull(candidateValidations);
        ArgumentNullException.ThrowIfNull(failures);

        CandidateValidations = [.. candidateValidations];
        Failures = [.. failures];
        if (CandidateValidations.Any(value => value is null)
            || Failures.Any(value => value is null))
        {
            throw new ArgumentException(
                "Feasibility result collections cannot contain nulls.");
        }

        SlotBudgets = slotBudgets;
        FeasibleLoadout = Failures.IsEmpty && slotBudgets is not null
            ? new FeasibleCombatLoadout(proposal, slotBudgets)
            : null;
    }

    public ProposedCombatLoadout Proposal { get; }

    public ImmutableArray<CombatSkillCandidateValidationResult>
        CandidateValidations
    {
        get;
    }

    public CombatRequirementEvaluationResult RequirementEvaluation { get; }

    public SlotBudgetSet? SlotBudgets { get; }

    public ImmutableArray<CombatLoadoutFeasibilityFailure> Failures { get; }

    public FeasibleCombatLoadout? FeasibleLoadout { get; }

    public bool IsFeasible => FeasibleLoadout is not null;
}
