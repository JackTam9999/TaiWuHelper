using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public sealed record TargetCounterPlaybookIdentity
{
    public TargetCounterPlaybookIdentity(
        TargetArchetypeIdentity archetype,
        TargetProfileVersion version)
    {
        Archetype = archetype
            ?? throw new ArgumentNullException(nameof(archetype));
        Version = version
            ?? throw new ArgumentNullException(nameof(version));
    }

    public TargetArchetypeIdentity Archetype { get; }

    public TargetProfileVersion Version { get; }

    public string StableKey => $"{Archetype.StableKey}/PLAYBOOK@{Version.Value}";
}
