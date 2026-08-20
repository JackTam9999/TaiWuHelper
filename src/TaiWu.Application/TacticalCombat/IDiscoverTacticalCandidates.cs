namespace TaiWu.Application.TacticalCombat;

public interface IDiscoverTacticalCandidates
{
    Task<TacticalCandidateDiscoveryReadResult> ExecuteAsync(
        TacticalCandidateDiscoveryReadRequest request,
        CancellationToken cancellationToken = default);
}
