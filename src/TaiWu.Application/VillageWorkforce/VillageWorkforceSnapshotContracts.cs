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
        FailureIdentity = failureIdentity;
        FailureMessage = failureMessage;
    }

    public VillageWorkforceSnapshotReadStatus Status { get; }

    public VillageWorkforceSnapshot? Snapshot { get; }

    public string? FailureIdentity { get; }

    public string? FailureMessage { get; }

    public static VillageWorkforceSnapshotReadResult Complete(
        VillageWorkforceSnapshot snapshot) =>
        new(VillageWorkforceSnapshotReadStatus.Complete,
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)),
            null,
            null);

    public static VillageWorkforceSnapshotReadResult Partial(
        VillageWorkforceSnapshot snapshot) =>
        new(VillageWorkforceSnapshotReadStatus.Partial,
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)),
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
}
