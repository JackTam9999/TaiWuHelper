namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatLoadoutFeasibilityFailure
{
    public CombatLoadoutFeasibilityFailure(
        CombatLoadoutFeasibilityFailureCode code,
        string reason,
        int? skillId = null)
    {
        if (!Enum.IsDefined(code))
        {
            throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                "Unknown feasibility failure code.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A feasibility failure requires a reason.",
                nameof(reason));
        }

        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        Code = code;
        Reason = reason.Trim();
        SkillId = skillId;
    }

    public CombatLoadoutFeasibilityFailureCode Code { get; }

    public string Reason { get; }

    public int? SkillId { get; }
}
