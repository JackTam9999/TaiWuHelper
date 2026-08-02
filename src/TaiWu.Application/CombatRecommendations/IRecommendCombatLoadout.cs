namespace TaiWu.Application.CombatRecommendations;

public interface IRecommendCombatLoadout
{
    Task<CombatLoadoutRecommendation> ExecuteAsync(
        RecommendCombatLoadoutRequest request,
        CancellationToken cancellationToken = default);
}
