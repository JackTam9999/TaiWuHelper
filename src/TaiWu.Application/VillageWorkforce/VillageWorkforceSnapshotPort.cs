using TaiWu.Application.GameData;

namespace TaiWu.Application.VillageWorkforce;

public sealed record VillageWorkforceSnapshotReadRequest
{
    private VillageWorkforceSnapshotReadRequest()
    {
    }

    public static VillageWorkforceSnapshotReadRequest Current { get; } = new();
}

public interface IVillageWorkforceSnapshotReader : IReadOnlyGameDataSource
{
    Task<VillageWorkforceSnapshotReadResult> ReadAsync(
        VillageWorkforceSnapshotReadRequest request,
        CancellationToken cancellationToken = default);
}
