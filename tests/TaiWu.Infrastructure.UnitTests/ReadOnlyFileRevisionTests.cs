using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class ReadOnlyFileRevisionTests
{
    private readonly ReadOnlyFileRevisionProvider _provider = new();

    [Fact]
    public async Task Capture_ReturnsLengthAndLastWriteTime()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"TaiWu.Infrastructure.UnitTests-{Guid.NewGuid():N}.sav");
        var expectedWriteTime = new DateTime(
            2026,
            8,
            3,
            12,
            34,
            56,
            DateTimeKind.Utc);

        try
        {
            await File.WriteAllBytesAsync(
                path,
                [1, 2, 3, 4, 5],
                TestContext.Current.CancellationToken);
            File.SetLastWriteTimeUtc(path, expectedWriteTime);

            var revision = _provider.Capture(path);

            Assert.Equal(5, revision.Length);
            Assert.Equal(expectedWriteTime, revision.LastWriteTimeUtc);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Capture_WhenFileIsMissing_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"TaiWu.Infrastructure.UnitTests-{Guid.NewGuid():N}.sav");

        var exception = Assert.Throws<FileNotFoundException>(
            () => _provider.Capture(path));

        Assert.Equal(Path.GetFullPath(path), exception.FileName);
    }
}
