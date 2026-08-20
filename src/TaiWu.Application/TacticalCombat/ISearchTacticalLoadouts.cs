namespace TaiWu.Application.TacticalCombat;

public interface ISearchTacticalLoadouts
{
    Task<TacticalLoadoutSearchReadResult> ExecuteAsync(
        TacticalLoadoutSearchReadRequest request,
        CancellationToken cancellationToken = default);
}
