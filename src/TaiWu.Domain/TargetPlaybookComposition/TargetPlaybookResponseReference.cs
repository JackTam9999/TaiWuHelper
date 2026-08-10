using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybookComposition;

public sealed record TargetPlaybookResponseReference
{
    public TargetPlaybookResponseReference(
        TargetPlaybookResponseReferenceKind kind,
        string stableCode)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
        StableCode = TargetProfileText.Code(
            stableCode,
            nameof(stableCode));
    }

    public TargetPlaybookResponseReferenceKind Kind { get; }

    public string StableCode { get; }

    public string StableKey => $"{Kind}:{StableCode}";
}
