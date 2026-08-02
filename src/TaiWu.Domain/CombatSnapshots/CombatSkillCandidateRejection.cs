namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSkillCandidateRejection
{
    public CombatSkillCandidateRejection(
        CombatSkillCandidateRejectionCode code,
        string reason)
    {
        if (!Enum.IsDefined(code))
        {
            throw new ArgumentOutOfRangeException(
                nameof(code),
                code,
                "Unknown candidate rejection code.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A candidate rejection requires a reason.",
                nameof(reason));
        }

        Code = code;
        Reason = reason.Trim();
    }

    public CombatSkillCandidateRejectionCode Code { get; }

    public string Reason { get; }
}
