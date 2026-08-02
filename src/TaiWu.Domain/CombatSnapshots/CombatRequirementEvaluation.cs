namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatRequirementEvaluation
{
    internal CombatRequirementEvaluation(
        CombatRequirement requirement,
        CombatRequirementStatus status,
        string reason)
    {
        Requirement = requirement
            ?? throw new ArgumentNullException(nameof(requirement));

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown combat-requirement status.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A requirement evaluation requires a reason.",
                nameof(reason));
        }

        Status = status;
        Reason = reason.Trim();
    }

    public CombatRequirement Requirement { get; }

    public CombatRequirementStatus Status { get; }

    public string Reason { get; }
}
