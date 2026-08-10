using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetArchetypes;

public sealed record TargetArchetypeIdentity
{
    public TargetArchetypeIdentity(
        string code,
        TargetProfileVersion version)
    {
        Code = TargetProfileText.Code(code, nameof(code));
        Version = version ?? throw new ArgumentNullException(nameof(version));
    }

    public string Code { get; }

    public TargetProfileVersion Version { get; }

    public string StableKey => $"{Code}@{Version.Value}";
}
