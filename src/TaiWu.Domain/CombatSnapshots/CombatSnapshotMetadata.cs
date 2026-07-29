namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSnapshotMetadata
{
    public CombatSnapshotMetadata(
        string savePath,
        string saveSha256,
        DateTimeOffset capturedAt,
        SnapshotValue<DateTimeOffset> saveLastWriteTimeUtc,
        SnapshotValue<string> gameDataVersion)
    {
        if (string.IsNullOrWhiteSpace(savePath))
        {
            throw new ArgumentException(
                "A snapshot requires its save source path.",
                nameof(savePath));
        }

        if (string.IsNullOrWhiteSpace(saveSha256)
            || saveSha256.Length != 64
            || !saveSha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "Save SHA-256 must contain exactly 64 hexadecimal characters.",
                nameof(saveSha256));
        }

        SavePath = savePath;
        SaveSha256 = saveSha256.ToUpperInvariant();
        CapturedAtUtc = capturedAt.ToUniversalTime();
        SaveLastWriteTimeUtc = saveLastWriteTimeUtc
            ?? throw new ArgumentNullException(nameof(saveLastWriteTimeUtc));
        GameDataVersion = gameDataVersion
            ?? throw new ArgumentNullException(nameof(gameDataVersion));
    }

    public string SavePath { get; }

    public string SaveSha256 { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public SnapshotValue<DateTimeOffset> SaveLastWriteTimeUtc { get; }

    public SnapshotValue<string> GameDataVersion { get; }
}
