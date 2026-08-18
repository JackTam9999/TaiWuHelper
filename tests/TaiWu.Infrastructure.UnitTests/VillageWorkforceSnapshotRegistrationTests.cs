using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.VillageWorkforce;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class VillageWorkforceSnapshotRegistrationTests
{
    [Fact]
    public async Task Reader_is_registered_and_missing_save_is_typed()
    {
        var services = new ServiceCollection();
        services.AddTaiwuInfrastructure();
        using var provider = services.BuildServiceProvider();
        var reader = provider
            .GetRequiredService<IVillageWorkforceSnapshotReader>();

        var result = await reader.ReadAsync(
            VillageWorkforceSnapshotReadRequest.Current,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            VillageWorkforceSnapshotReadStatus.SaveUnavailable,
            result.Status);
        Assert.Null(result.Snapshot);
        Assert.Equal(
            "CONFIGURED_SAVE_UNAVAILABLE",
            result.FailureIdentity);
    }

    [Fact]
    public async Task Reader_honors_pre_cancelled_request()
    {
        var services = new ServiceCollection();
        services.AddTaiwuInfrastructure();
        using var provider = services.BuildServiceProvider();
        var reader = provider
            .GetRequiredService<IVillageWorkforceSnapshotReader>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                cancellation.Token));
    }
}
