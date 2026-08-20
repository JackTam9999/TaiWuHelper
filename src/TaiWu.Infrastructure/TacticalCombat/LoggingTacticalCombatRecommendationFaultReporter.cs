using Microsoft.Extensions.Logging;
using TaiWu.Application.TacticalCombat;

namespace TaiWu.Infrastructure.TacticalCombat;

internal sealed class LoggingTacticalCombatRecommendationFaultReporter(
    ILogger<RecommendTacticalCombat> logger)
    : ITacticalCombatRecommendationFaultReporter
{
    public void Report(Exception exception, string stageIdentity)
    {
        logger.LogError(
            exception,
            "Unexpected tactical recommendation failure at {StageIdentity}.",
            stageIdentity);
    }
}
