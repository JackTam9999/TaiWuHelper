using TaiWu.Application.CombatRecommendations;

namespace TaiWu.Application.TargetObservations;

public interface ITargetObservationRecommendationWorkflow
{
    Task<CombatLoadoutRecommendation> ExecuteAsync(
        RecommendCombatLoadoutRequest request,
        CancellationToken cancellationToken = default);
}
