namespace TaiWu.Domain.CombatCounters;

public sealed record CombatCounterAccessIssue
{
    internal CombatCounterAccessIssue(
        CombatCounterAccessIssueCode code,
        string reason)
    {
        Code = code;
        Reason = reason;
    }

    public CombatCounterAccessIssueCode Code { get; }

    public string Reason { get; }
}
