using System.Collections.Immutable;

namespace TaiWu.Application.Targets;

public sealed record FindTargetsResult
{
    internal FindTargetsResult(
        string query,
        TargetLookupStatus status,
        int totalMatches,
        IEnumerable<TargetLookupEntry> matches,
        TargetLookupSnapshot snapshot)
    {
        Query = query;
        Status = status;
        TotalMatches = totalMatches;
        Matches = [.. matches];
        CapturedAtUtc = snapshot.CapturedAtUtc;
        GameDataVersion = snapshot.GameDataVersion;
        Warnings = snapshot.Warnings;
    }

    public string Query { get; }

    public TargetLookupStatus Status { get; }

    public int TotalMatches { get; }

    public ImmutableArray<TargetLookupEntry> Matches { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public string? GameDataVersion { get; }

    public ImmutableArray<TargetLookupWarning> Warnings { get; }
}
