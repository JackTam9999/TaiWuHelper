using System.Collections.Immutable;
using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Application.VillageWorkforce;

public enum VillageWorkforceSnapshotReadStatus
{
    Complete = 0,
    Partial = 1,
    SaveUnavailable = 2,
    UnsupportedVersion = 3,
    ConflictingSources = 4,
    ChangedRevision = 5,
    ReadFailed = 6
}

public sealed class VillageWorkforceSnapshotReadResult
{
    private VillageWorkforceSnapshotReadResult(
        VillageWorkforceSnapshotReadStatus status,
        VillageWorkforceSnapshot? snapshot,
        IEnumerable<VillageWorkerDisplay>? workerDisplays,
        IEnumerable<VillageWorkforceTargetDisplay>? targetDisplays,
        string? failureIdentity,
        string? failureMessage)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown village-workforce snapshot status.");
        }

        var isSuccess = status is VillageWorkforceSnapshotReadStatus.Complete
            or VillageWorkforceSnapshotReadStatus.Partial;
        if (isSuccess != (snapshot is not null)
            || isSuccess == (failureIdentity is not null
                || failureMessage is not null))
        {
            throw new ArgumentException(
                "Village-workforce read status and payload are incompatible.");
        }

        Status = status;
        Snapshot = snapshot;
        WorkerDisplays = CopyWorkerDisplays(snapshot, workerDisplays ?? []);
        TargetDisplays = CopyTargetDisplays(snapshot, targetDisplays ?? []);
        FailureIdentity = failureIdentity;
        FailureMessage = failureMessage;
    }

    public VillageWorkforceSnapshotReadStatus Status { get; }

    public VillageWorkforceSnapshot? Snapshot { get; }

    public ImmutableArray<VillageWorkerDisplay> WorkerDisplays { get; }

    public ImmutableArray<VillageWorkforceTargetDisplay> TargetDisplays { get; }

    public string? FailureIdentity { get; }

    public string? FailureMessage { get; }

    public static VillageWorkforceSnapshotReadResult Complete(
        VillageWorkforceSnapshot snapshot,
        IEnumerable<VillageWorkerDisplay>? workerDisplays = null,
        IEnumerable<VillageWorkforceTargetDisplay>? targetDisplays = null) =>
        new(VillageWorkforceSnapshotReadStatus.Complete,
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)),
            workerDisplays,
            targetDisplays,
            null,
            null);

    public static VillageWorkforceSnapshotReadResult Partial(
        VillageWorkforceSnapshot snapshot,
        IEnumerable<VillageWorkerDisplay>? workerDisplays = null,
        IEnumerable<VillageWorkforceTargetDisplay>? targetDisplays = null) =>
        new(VillageWorkforceSnapshotReadStatus.Partial,
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)),
            workerDisplays,
            targetDisplays,
            null,
            null);

    public static VillageWorkforceSnapshotReadResult Failed(
        VillageWorkforceSnapshotReadStatus status,
        string failureIdentity,
        string failureMessage)
    {
        if (status is VillageWorkforceSnapshotReadStatus.Complete
            or VillageWorkforceSnapshotReadStatus.Partial)
        {
            throw new ArgumentException(
                "A success status cannot create a failed read result.",
                nameof(status));
        }

        return new VillageWorkforceSnapshotReadResult(
            status,
            null,
            null,
            null,
            Stable(failureIdentity, nameof(failureIdentity)),
            Required(failureMessage, nameof(failureMessage)));
    }

    private static string Stable(string value, string parameterName)
    {
        var normalized = Required(value, parameterName);
        return normalized.IndexOfAny(['|', '/', '\\', '\r', '\n']) >= 0
            ? throw new ArgumentException(
                "A read failure identity cannot contain delimiters or paths.",
                parameterName)
            : normalized;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException(
                "A read failure requires safe nonblank text.",
                parameterName)
            : value.Trim();

    private static ImmutableArray<VillageWorkerDisplay> CopyWorkerDisplays(
        VillageWorkforceSnapshot? snapshot,
        IEnumerable<VillageWorkerDisplay> values)
    {
        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.GroupBy(item => item.Identity).Any(group => group.Count() > 1)
            || snapshot is null && !copied.IsEmpty
            || snapshot is not null && copied.Any(item => snapshot.Workers.All(
                worker => worker.Identity != item.Identity)))
        {
            throw new ArgumentException(
                "Worker displays must be unique and belong to the snapshot.",
                nameof(values));
        }

        return [.. copied.OrderBy(item => item.Identity.CharacterId)];
    }

    private static ImmutableArray<VillageWorkforceTargetDisplay> CopyTargetDisplays(
        VillageWorkforceSnapshot? snapshot,
        IEnumerable<VillageWorkforceTargetDisplay> values)
    {
        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.GroupBy(item => item.Identity).Any(group => group.Count() > 1)
            || snapshot is null && !copied.IsEmpty
            || snapshot is not null && copied.Any(item => snapshot.Targets.All(
                target => target.Identity != item.Identity)))
        {
            throw new ArgumentException(
                "Target displays must be unique and belong to the snapshot.",
                nameof(values));
        }

        return [.. copied.OrderBy(item => item.Identity.Building.AreaId)
            .ThenBy(item => item.Identity.Building.BlockId)
            .ThenBy(item => item.Identity.Building.BuildingBlockIndex)
            .ThenBy(item => item.Identity.ManagerSlotIndex)];
    }
}
