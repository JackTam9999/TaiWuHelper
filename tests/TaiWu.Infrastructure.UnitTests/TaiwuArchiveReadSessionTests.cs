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
        var revisions = new StubRevisionProvider(Revision(fingerprint));
        var warning = new TaiwuArchiveLoadWarning(
            TaiwuArchiveLoadWarning.StandaloneEventRuntimeUnavailable,
            "Void InitRuntimeEnvironment()");
        var loader = new StubArchiveLoader(warning);
        var session = new TaiwuArchiveReadSession(
            revisions,
            fingerprints,
            loader);
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
            Assert.Equal(1, revisions.CaptureCount);
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
        var revisions = new StubRevisionProvider(
            Revision(Fingerprint("BEFORE")));
        var loader = new StubArchiveLoader();
        var session = new TaiwuArchiveReadSession(
            revisions,
            fingerprints,
            loader);
        var path = await CreateSaveAsync();

        try
        {
            var exception = await Assert.ThrowsAsync<TaiwuArchiveChangedException>(
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
        var revisions = new StubRevisionProvider(
            Revision(Fingerprint("A")));
        var loader = new StubArchiveLoader();
        var session = new TaiwuArchiveReadSession(
            revisions,
            fingerprints,
            loader);
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

    [Fact]
    public async Task ReadAsync_WhenRevisionIsUnchanged_ReusesLoadedArchive()
    {
        var fingerprint = Fingerprint("A");
        var revision = Revision(fingerprint);
        var revisions = new StubRevisionProvider(
            revision,
            revision,
            revision);
        var fingerprints = new StubFingerprintProvider(
            fingerprint,
            fingerprint,
            fingerprint,
            fingerprint);
        var warning = new TaiwuArchiveLoadWarning(
            TaiwuArchiveLoadWarning.StandaloneEventRuntimeUnavailable,
            "Void InitRuntimeEnvironment()");
        var loader = new StubArchiveLoader(warning);
        var session = new TaiwuArchiveReadSession(
            revisions,
            fingerprints,
            loader);
        var path = await CreateSaveAsync();

        try
        {
            var first = await session.ReadAsync(
                path,
                static (_, _) => 1,
                TestContext.Current.CancellationToken);
            var second = await session.ReadAsync(
                path,
                (context, _) =>
                {
                    Assert.Same(fingerprint, context.SourceFingerprint);
                    Assert.Same(warning, context.LoadWarning);
                    return 2;
                },
                TestContext.Current.CancellationToken);

            Assert.Equal(1, first);
            Assert.Equal(2, second);
            Assert.Equal(3, revisions.CaptureCount);
            Assert.Equal(3, fingerprints.CaptureCount);
            Assert.Equal(1, loader.LoadCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenRevisionChanges_ReloadsArchive()
    {
        var firstFingerprint = Fingerprint("A");
        var secondFingerprint = Fingerprint(
            "B",
            DateTimeOffset.Parse("2026-07-31T00:01:00Z"));
        var revisions = new StubRevisionProvider(
            Revision(firstFingerprint),
            Revision(secondFingerprint));
        var fingerprints = new StubFingerprintProvider(
            firstFingerprint,
            firstFingerprint,
            secondFingerprint,
            secondFingerprint);
        var loader = new StubArchiveLoader();
        var session = new TaiwuArchiveReadSession(
            revisions,
            fingerprints,
            loader);
        var path = await CreateSaveAsync();

        try
        {
            await session.ReadAsync(
                path,
                static (_, _) => 1,
                TestContext.Current.CancellationToken);
            var secondHash = await session.ReadAsync(
                path,
                static (context, _) => context.SourceFingerprint.Sha256,
                TestContext.Current.CancellationToken);

            Assert.Equal("B", secondHash);
            Assert.Equal(2, revisions.CaptureCount);
            Assert.Equal(4, fingerprints.CaptureCount);
            Assert.Equal(2, loader.LoadCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenContentChangesWithPreservedMetadata_ReloadsArchive()
    {
        var firstFingerprint = Fingerprint("A");
        var secondFingerprint = Fingerprint("B");
        var revision = Revision(firstFingerprint);
        var revisions = new StubRevisionProvider(revision, revision);
        var fingerprints = new StubFingerprintProvider(
            firstFingerprint,
            firstFingerprint,
            secondFingerprint,
            secondFingerprint);
        var loader = new StubArchiveLoader();
        var session = new TaiwuArchiveReadSession(
            revisions,
            fingerprints,
            loader);
        var path = await CreateSaveAsync();

        try
        {
            await session.ReadAsync(
                path,
                static (_, _) => "A",
                TestContext.Current.CancellationToken);
            var secondHash = await session.ReadAsync(
                path,
                static (context, _) => context.SourceFingerprint.Sha256,
                TestContext.Current.CancellationToken);

            Assert.Equal("B", secondHash);
            Assert.Equal(2, revisions.CaptureCount);
            Assert.Equal(4, fingerprints.CaptureCount);
            Assert.Equal(2, loader.LoadCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenCachedSourceChanges_DiscardsProjection()
    {
        var fingerprint = Fingerprint("A");
        var changed = new ReadOnlyFileRevision(
            fingerprint.Length + 1,
            fingerprint.LastWriteTimeUtc.AddMinutes(1));
        var revisions = new StubRevisionProvider(
            Revision(fingerprint),
            Revision(fingerprint),
            changed);
        var fingerprints = new StubFingerprintProvider(
            fingerprint,
            fingerprint,
            fingerprint);
        var loader = new StubArchiveLoader();
        var session = new TaiwuArchiveReadSession(
            revisions,
            fingerprints,
            loader);
        var path = await CreateSaveAsync();

        try
        {
            await session.ReadAsync(
                path,
                static (_, _) => 1,
                TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<TaiwuArchiveChangedException>(
                () => session.ReadAsync(
                    path,
                    static (_, _) => "discard me",
                    TestContext.Current.CancellationToken));

            Assert.Contains("changed while it was being read", exception.Message);
            Assert.Equal(3, fingerprints.CaptureCount);
            Assert.Equal(1, loader.LoadCount);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ReadOnlyFileFingerprint Fingerprint(
        string hash,
        DateTimeOffset? lastWriteTimeUtc = null)
    {
        return new ReadOnlyFileFingerprint(
            Length: 4,
            Sha256: hash,
            LastWriteTimeUtc: lastWriteTimeUtc
                ?? DateTimeOffset.Parse("2026-07-31T00:00:00Z"));
    }

    private static ReadOnlyFileRevision Revision(
        ReadOnlyFileFingerprint fingerprint) =>
        ReadOnlyFileRevision.From(fingerprint);

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

    private sealed class StubRevisionProvider(
        params ReadOnlyFileRevision[] revisions)
        : IReadOnlyFileRevisionProvider
    {
        private readonly Queue<ReadOnlyFileRevision> _revisions =
            new(revisions);

        public int CaptureCount { get; private set; }

        public ReadOnlyFileRevision Capture(string path)
        {
            CaptureCount++;
            return _revisions.Dequeue();
        }
    }

    private sealed class StubArchiveLoader(
        TaiwuArchiveLoadWarning? warning = null) : ITaiwuArchiveLoader
    {
        public string? LoadedPath { get; private set; }

        public int LoadCount { get; private set; }

        public TaiwuArchiveLoadWarning? Load(string saveFilePath)
        {
            LoadCount++;
            LoadedPath = saveFilePath;
            return warning;
        }
    }
}
