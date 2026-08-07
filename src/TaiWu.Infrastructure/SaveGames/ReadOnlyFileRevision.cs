namespace TaiWu.Infrastructure.SaveGames;

internal readonly record struct ReadOnlyFileRevision(
    long Length,
    DateTimeOffset LastWriteTimeUtc)
{
    public static ReadOnlyFileRevision From(
        ReadOnlyFileFingerprint fingerprint) =>
        new(fingerprint.Length, fingerprint.LastWriteTimeUtc);
}

internal interface IReadOnlyFileRevisionProvider
{
    ReadOnlyFileRevision Capture(string path);
}

internal sealed class ReadOnlyFileRevisionProvider
    : IReadOnlyFileRevisionProvider
{
    public ReadOnlyFileRevision Capture(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var file = new FileInfo(path);
        file.Refresh();
        if (!file.Exists)
        {
            throw new FileNotFoundException(
                "The Taiwu save file was not found.",
                file.FullName);
        }

        return new ReadOnlyFileRevision(
            file.Length,
            file.LastWriteTimeUtc);
    }
}
