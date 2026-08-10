using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetProfiles;

namespace TaiWu.Domain.TargetPlaybooks;

public sealed record TargetCounterPlaybookResolution
{
    internal TargetCounterPlaybookResolution(
        TargetProfileVersion observedGameDataVersion,
        TargetArchetypeIdentity archetype,
        TargetCounterPlaybookResolutionStatus status,
        TargetCounterPlaybook? playbook)
    {
        ObservedGameDataVersion = observedGameDataVersion;
        Archetype = archetype;
        Status = status;
        Playbook = playbook;
    }

    public TargetProfileVersion ObservedGameDataVersion { get; }

    public TargetArchetypeIdentity Archetype { get; }

    public TargetCounterPlaybookResolutionStatus Status { get; }

    public TargetCounterPlaybook? Playbook { get; }

    public bool IsResolved =>
        Status == TargetCounterPlaybookResolutionStatus.Resolved;
}
