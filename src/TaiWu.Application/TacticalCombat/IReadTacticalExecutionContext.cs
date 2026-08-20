namespace TaiWu.Application.TacticalCombat;

public interface IReadTacticalExecutionContext
{
    Task<TacticalExecutionContextReadResult> ExecuteAsync(
        TacticalExecutionContextReadRequest request,
        CancellationToken cancellationToken = default);
}
