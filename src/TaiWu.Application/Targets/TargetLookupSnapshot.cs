using System.Collections.Immutable;

namespace TaiWu.Application.Targets;

public sealed record TargetLookupSnapshot
{
    public TargetLookupSnapshot(
        DateTimeOffset capturedAt,
        string? gameDataVersion,
        IEnumerable<TargetLookupEntry> entries,
        IEnumerable<TargetLookupWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(warnings);
        CapturedAtUtc = capturedAt.ToUniversalTime();
        GameDataVersion = string.IsNullOrWhiteSpace(gameDataVersion)
            ? null
            : gameDataVersion.Trim();
        Entries = [.. entries];
        Warnings = [.. warnings];
        if (Entries.Any(entry => entry is null)
            || Warnings.Any(warning => warning is null))
        {
            throw new ArgumentException(
                "Target lookup collections cannot contain null entries.");
        }

        var duplicate = Entries
            .GroupBy(entry => entry.CharacterId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate target character {duplicate.Key}.",
                nameof(entries));
        }
    }

    public DateTimeOffset CapturedAtUtc { get; }

    public string? GameDataVersion { get; }

    public ImmutableArray<TargetLookupEntry> Entries { get; }

    public ImmutableArray<TargetLookupWarning> Warnings { get; }
}
