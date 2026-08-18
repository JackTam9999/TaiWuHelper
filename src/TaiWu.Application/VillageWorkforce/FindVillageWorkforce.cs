using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Application.VillageWorkforce;

public sealed class FindVillageWorkforce : IFindVillageWorkforce
{
    private readonly IVillageWorkforceSnapshotReader _snapshotReader;
    private readonly BuildVillageWorkforce _builder;

    public FindVillageWorkforce(IVillageWorkforceSnapshotReader snapshotReader)
        : this(snapshotReader, new BuildVillageWorkforce())
    {
    }

    public FindVillageWorkforce(
        IVillageWorkforceSnapshotReader snapshotReader,
        BuildVillageWorkforce builder)
    {
        _snapshotReader = snapshotReader
            ?? throw new ArgumentNullException(nameof(snapshotReader));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    public async Task<VillageWorkforceFinderResult> ExecuteAsync(
        VillageWorkforceFinderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!BuildVillageWorkforce.ValidRequest(request))
        {
            return VillageWorkforceFinderResult.Failed(
                VillageWorkforceFinderStatus.InvalidRequest,
                "VILLAGE_WORKFORCE_REQUEST_INVALID");
        }

        var read = await _snapshotReader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return _builder.Execute(read, request, cancellationToken);
    }
}
