using TaiWu.Application.GameData;

namespace TaiWu.Application.CompanionCandidates;

public sealed record CompanionCandidateSnapshotReadRequest
{
    private CompanionCandidateSnapshotReadRequest()
    {
    }

    public static CompanionCandidateSnapshotReadRequest Current { get; } = new();
}

public interface ICompanionCandidateSnapshotReader : IReadOnlyGameDataSource
{
    Task<CompanionCandidateSnapshotReadResult> ReadAsync(
        CompanionCandidateSnapshotReadRequest request,
        CancellationToken cancellationToken = default);
}
