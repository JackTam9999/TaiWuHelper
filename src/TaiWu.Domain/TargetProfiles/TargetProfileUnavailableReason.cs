namespace TaiWu.Domain.TargetProfiles;

public sealed record TargetProfileUnavailableReason
{
    public TargetProfileUnavailableReason(string code, string? detail = null)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        Detail = TargetProfileText.OptionalDetail(detail, nameof(detail));
    }

    public string Code { get; }

    public string? Detail { get; }
}
