using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed record TacticalCandidateDiscoveryReadRequest
{
    public TacticalCandidateDiscoveryReadRequest(
        TacticalExecutionContextReadRequest contextRequest,
        TacticalCandidateDiscoveryLimits? limits = null)
    {
        ContextRequest = contextRequest
            ?? throw new ArgumentNullException(nameof(contextRequest));
        Limits = limits ?? TacticalCandidateDiscoveryLimits.Default;
    }

    public TacticalExecutionContextReadRequest ContextRequest { get; }

    public TacticalCandidateDiscoveryLimits Limits { get; }
}
