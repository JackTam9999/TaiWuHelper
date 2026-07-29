using System.Security.Cryptography;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record ReadOnlyFileFingerprint(long Length, string Sha256)
{
    public static async Task<ReadOnlyFileFingerprint> CaptureAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var options = new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Share = FileShare.ReadWrite | FileShare.Delete,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        };

        await using var stream = new FileStream(path, options);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);

        return new ReadOnlyFileFingerprint(
            stream.Length,
            Convert.ToHexString(hash));
    }
}
