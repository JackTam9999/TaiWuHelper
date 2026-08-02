using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class TaiwuArchiveReadSessionTests
{
    [Fact]
    public async Task ReadAsync_ProjectsAStableReadAndPreservesLoadWarning()
    {
        var fingerprint = Fingerprint("A");
        var fingerprints = new StubFingerprintProvider(
            fingerprint,
            fingerprint);
        var warning = new TaiwuArchiveLoadWarning(
            TaiwuArchiveLoadWarning.StandaloneEventRuntimeUnavailable,
            "Void InitRuntimeEnvironment()");
        var loader = new StubArchiveLoader(warning);
        var session = new TaiwuArchiveReadSession(fingerprints, loader);
        var path = await CreateSaveAsync();

        try
        {
            var result = await session.ReadAsync(
                path,
                (context, cancellationToken) =>
                {
                    Assert.Equal(Path.GetFullPath(path), context.SaveFilePath);
                    Assert.Same(fingerprint, context.SourceFingerprint);
                    Assert.Same(warning, context.LoadWarning);
                    Assert.Equal(
                        TestContext.Current.CancellationToken,
                        cancellationToken);
                    return 42;
                },
                TestContext.Current.CancellationToken);

            Assert.Equal(42, result);
            Assert.Equal(2, fingerprints.CaptureCount);
            Assert.Equal(Path.GetFullPath(path), loader.LoadedPath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenSourceChanges_DiscardsProjectedResult()
    {
        var fingerprints = new StubFingerprintProvider(
            Fingerprint("BEFORE"),
            Fingerprint("AFTER"));
        var loader = new StubArchiveLoader();
        var session = new TaiwuArchiveReadSession(fingerprints, loader);
        var path = await CreateSaveAsync();

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => session.ReadAsync(
                    path,
                    static (_, _) => "discard me",
                    TestContext.Current.CancellationToken));

            Assert.Contains("changed while it was being read", exception.Message);
            Assert.Equal(2, fingerprints.CaptureCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenProjectionCancels_SkipsFinalFingerprint()
    {
        using var cancellation = CancellationTokenSource
            .CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var fingerprints = new StubFingerprintProvider(Fingerprint("A"));
        var loader = new StubArchiveLoader();
        var session = new TaiwuArchiveReadSession(fingerprints, loader);
        var path = await CreateSaveAsync();

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => session.ReadAsync(
                    path,
                    (_, _) =>
                    {
                        cancellation.Cancel();
                        return "cancelled";
                    },
                    cancellation.Token));

            Assert.Equal(1, fingerprints.CaptureCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ReadOnlyFileFingerprint Fingerprint(string hash)
    {
        return new ReadOnlyFileFingerprint(
            Length: 4,
            Sha256: hash,
            LastWriteTimeUtc:
                DateTimeOffset.Parse("2026-07-31T00:00:00Z"));
    }

    private static async Task<string> CreateSaveAsync()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"TaiWu.Infrastructure.UnitTests-{Guid.NewGuid():N}.sav");
        await File.WriteAllBytesAsync(
            path,
            [1, 2, 3, 4],
            TestContext.Current.CancellationToken);
        return path;
    }

    private sealed class StubFingerprintProvider
        : IReadOnlyFileFingerprintProvider
    {
        private readonly Queue<ReadOnlyFileFingerprint> _fingerprints;

        public StubFingerprintProvider(
            params ReadOnlyFileFingerprint[] fingerprints)
        {
            _fingerprints = new Queue<ReadOnlyFileFingerprint>(fingerprints);
        }

        public int CaptureCount { get; private set; }

        public Task<ReadOnlyFileFingerprint> CaptureAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CaptureCount++;
            return Task.FromResult(_fingerprints.Dequeue());
        }
    }

    private sealed class StubArchiveLoader(
        TaiwuArchiveLoadWarning? warning = null) : ITaiwuArchiveLoader
    {
        public string? LoadedPath { get; private set; }

        public TaiwuArchiveLoadWarning? Load(string saveFilePath)
        {
            LoadedPath = saveFilePath;
            return warning;
        }
    }
}
