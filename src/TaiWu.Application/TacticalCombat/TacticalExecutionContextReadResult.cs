using TaiWu.Domain.TacticalCombat;

namespace TaiWu.Application.TacticalCombat;

public sealed record TacticalExecutionContextReadResult
{
    public TacticalExecutionContextReadResult(
        TacticalExecutionContext context,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset? latestObservationAtUtc)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        CapturedAtUtc = capturedAtUtc.ToUniversalTime();
        LatestObservationAtUtc = latestObservationAtUtc?.ToUniversalTime();
    }

    public TacticalExecutionContext Context { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public DateTimeOffset? LatestObservationAtUtc { get; }
}
