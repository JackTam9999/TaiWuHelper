using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed record TacticalLoadoutSearchReadResult
{
    public TacticalLoadoutSearchReadResult(
        TacticalExecutionContextReadResult context,
        TacticalCandidateDiscoveryResult discovery,
        TacticalLoadoutSearchResult search)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Discovery = discovery
            ?? throw new ArgumentNullException(nameof(discovery));
        Search = search ?? throw new ArgumentNullException(nameof(search));
    }

    public TacticalExecutionContextReadResult Context { get; }

    public TacticalCandidateDiscoveryResult Discovery { get; }

    public TacticalLoadoutSearchResult Search { get; }
}
