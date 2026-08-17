namespace TaiWu.Domain.CompanionCandidates;

public sealed record CandidateUnavailableReason
{
    public CandidateUnavailableReason(string code, string detail)
    {
        Code = CandidateProfileText.Stable(code, nameof(code));
        Detail = CandidateProfileText.Detail(detail, nameof(detail));
    }

    public string Code { get; }

    public string Detail { get; }
}
