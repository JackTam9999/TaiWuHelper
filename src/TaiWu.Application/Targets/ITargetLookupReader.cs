using TaiWu.Application.GameData;

namespace TaiWu.Application.Targets;

public interface ITargetLookupReader : IReadOnlyGameDataSource
{
    Task<TargetLookupSnapshot> ReadAsync(
        TargetLookupReadRequest request,
        CancellationToken cancellationToken = default);
}
