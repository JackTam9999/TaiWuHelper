namespace TaiWu.Domain.CompanionRoles;

public static class CompanionRoleMeritComparer
{
    public static CompanionRoleMeritComparison Compare(
        CompanionRoleEvaluation first,
        CompanionRoleEvaluation second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.State != CompanionRoleEvaluationState.Rankable
            || second.State != CompanionRoleEvaluationState.Rankable
            || !string.Equals(
                first.Definition.Fingerprint,
                second.Definition.Fingerprint,
                StringComparison.Ordinal)
            || first.Discipline != second.Discipline)
        {
            return CompanionRoleMeritComparison.NotComparable;
        }

        if (first.TotalScore == second.TotalScore)
        {
            return CompanionRoleMeritComparison.ExactTie;
        }

        return first.TotalScore > second.TotalScore
            ? CompanionRoleMeritComparison.FirstPreferred
            : CompanionRoleMeritComparison.SecondPreferred;
    }
}
