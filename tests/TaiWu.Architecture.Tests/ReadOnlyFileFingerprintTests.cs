using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class ReadOnlyFileFingerprintTests
{
    [Fact]
    public async Task CaptureAsync_ReadsFileWithoutChangingIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var fixture = await TemporaryFile.CreateAsync(
            [1, 2, 3, 4],
            cancellationToken);
        await using (fixture)
        {
            var bytesBefore = await File.ReadAllBytesAsync(
                fixture.Path,
                cancellationToken);
            var modifiedBefore = File.GetLastWriteTimeUtc(fixture.Path);

            var fingerprint = await ReadOnlyFileFingerprint.CaptureAsync(
                fixture.Path,
                cancellationToken);

            Assert.Equal(bytesBefore.Length, fingerprint.Length);
            Assert.Equal(modifiedBefore, fingerprint.LastWriteTimeUtc);
            Assert.Equal(
                bytesBefore,
                await File.ReadAllBytesAsync(fixture.Path, cancellationToken));
            Assert.Equal(modifiedBefore, File.GetLastWriteTimeUtc(fixture.Path));
        }
    }

    [Fact]
    public async Task CaptureAsync_ReadsAReadOnlyFile()
    {
        var fixture = await TemporaryFile.CreateAsync(
            [5, 6, 7, 8],
            TestContext.Current.CancellationToken);
        await using (fixture)
        {
            File.SetAttributes(fixture.Path, FileAttributes.ReadOnly);

            var fingerprint = await ReadOnlyFileFingerprint.CaptureAsync(
                fixture.Path,
                TestContext.Current.CancellationToken);

            Assert.Equal(4, fingerprint.Length);
        }
    }

    [Fact]
    public async Task CaptureAsync_WhenContentChanges_ProducesDifferentFingerprint()
    {
        var fixture = await TemporaryFile.CreateAsync(
            [9, 10, 11],
            TestContext.Current.CancellationToken);
        await using (fixture)
        {
            var before = await ReadOnlyFileFingerprint.CaptureAsync(
                fixture.Path,
                TestContext.Current.CancellationToken);

            await File.WriteAllBytesAsync(
                fixture.Path,
                [9, 10, 12],
                TestContext.Current.CancellationToken);

            var after = await ReadOnlyFileFingerprint.CaptureAsync(
                fixture.Path,
                TestContext.Current.CancellationToken);

            Assert.NotEqual(before, after);
        }
    }

    private sealed class TemporaryFile : IAsyncDisposable
    {
        private TemporaryFile(string directory, string path)
        {
            Directory = directory;
            Path = path;
        }

        public string Directory { get; }

        public string Path { get; }

        public static async Task<TemporaryFile> CreateAsync(
            byte[] contents,
            CancellationToken cancellationToken)
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "TaiWu.Architecture.Tests",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);

            var path = System.IO.Path.Combine(directory, "local.sav");
            await File.WriteAllBytesAsync(path, contents, cancellationToken);
            return new TemporaryFile(directory, path);
        }

        public ValueTask DisposeAsync()
        {
            if (File.Exists(Path))
            {
                File.SetAttributes(Path, FileAttributes.Normal);
            }

            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
