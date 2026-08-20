namespace TaiWu.Application.TacticalCombat;

public interface IRecommendTacticalCombat
{
    Task<TacticalCombatRecommendationResult> ExecuteAsync(
        TacticalCombatRecommendationRequest request,
        CancellationToken cancellationToken = default);
}

public interface ITacticalCombatRecommendationFaultReporter
{
    void Report(Exception exception, string stageIdentity);
}
