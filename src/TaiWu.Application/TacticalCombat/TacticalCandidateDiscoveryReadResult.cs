using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed record TacticalCandidateDiscoveryReadResult
{
    public TacticalCandidateDiscoveryReadResult(
        TacticalExecutionContextReadResult context,
        TacticalCandidateDiscoveryResult discovery)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Discovery = discovery
            ?? throw new ArgumentNullException(nameof(discovery));
    }

    public TacticalExecutionContextReadResult Context { get; }

    public TacticalCandidateDiscoveryResult Discovery { get; }
}
