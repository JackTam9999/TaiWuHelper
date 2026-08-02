namespace TaiWu.Application.Targets;

public interface IFindTargets
{
    Task<FindTargetsResult> ExecuteAsync(
        FindTargetsRequest request,
        CancellationToken cancellationToken = default);
}
