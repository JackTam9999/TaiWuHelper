namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSnapshotMetadata
{
    public CombatSnapshotMetadata(
        string saveSha256,
        DateTimeOffset capturedAt,
        SnapshotValue<DateTimeOffset> saveLastWriteTimeUtc,
        SnapshotValue<string> gameDataVersion)
    {
        if (string.IsNullOrWhiteSpace(saveSha256)
            || saveSha256.Length != 64
            || !saveSha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "Save SHA-256 must contain exactly 64 hexadecimal characters.",
                nameof(saveSha256));
        }

        SaveSha256 = saveSha256.ToUpperInvariant();
        CapturedAtUtc = capturedAt.ToUniversalTime();
        SaveLastWriteTimeUtc = saveLastWriteTimeUtc
            ?? throw new ArgumentNullException(nameof(saveLastWriteTimeUtc));
        GameDataVersion = gameDataVersion
            ?? throw new ArgumentNullException(nameof(gameDataVersion));
    }

    public string SaveSha256 { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public SnapshotValue<DateTimeOffset> SaveLastWriteTimeUtc { get; }

    public SnapshotValue<string> GameDataVersion { get; }
}
