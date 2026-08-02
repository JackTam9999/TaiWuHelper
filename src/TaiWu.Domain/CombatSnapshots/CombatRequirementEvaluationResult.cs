using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatRequirementEvaluationResult
{
    internal CombatRequirementEvaluationResult(
        IEnumerable<CombatRequirementEvaluation> evaluations)
    {
        ArgumentNullException.ThrowIfNull(evaluations);

        Evaluations = [.. evaluations];
        if (Evaluations.Any(evaluation => evaluation is null))
        {
            throw new ArgumentException(
                "Requirement evaluations cannot contain null entries.",
                nameof(evaluations));
        }

        Rejections =
        [
            .. Evaluations.Where(
                evaluation =>
                    evaluation.Requirement.Criticality
                        == CombatRequirementCriticality.Hard
                    && evaluation.Status
                        != CombatRequirementStatus.Satisfied)
        ];
        Warnings =
        [
            .. Evaluations.Where(
                evaluation =>
                    evaluation.Requirement.Criticality
                        == CombatRequirementCriticality.Conditional
                    && evaluation.Status
                        != CombatRequirementStatus.Satisfied)
        ];
    }

    public ImmutableArray<CombatRequirementEvaluation> Evaluations { get; }

    public ImmutableArray<CombatRequirementEvaluation> Rejections { get; }

    public ImmutableArray<CombatRequirementEvaluation> Warnings { get; }

    public bool IsAccepted => Rejections.IsEmpty;
}
