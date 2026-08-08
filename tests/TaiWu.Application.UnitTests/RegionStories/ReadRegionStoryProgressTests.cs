using NSubstitute;
using TaiWu.Application.Localization;
using TaiWu.Application.RegionStories;
using Xunit;

namespace TaiWu.Application.UnitTests.RegionStories;

public sealed class ReadRegionStoryProgressTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsValidatedReadRequest()
    {
        var reader = Substitute.For<IRegionStoryProgressReader>();
        var expected = new RegionStoryProgressSnapshot(
            DateTimeOffset.Parse("2026-08-07T21:00:00Z"),
            DateTimeOffset.Parse("2026-08-07T20:00:00Z"),
            new string('A', 64),
            [],
            []);
        reader.ReadAsync(
                Arg.Any<RegionStoryProgressReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);
        var useCase = new ReadRegionStoryProgress(reader);
        var request = new RegionStoryProgressReadRequest(
            "C:\\SaveGames\\world_1\\local.sav",
            TaiwuLanguage.Chinese);

        var actual = await useCase.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        await reader.Received(1).ReadAsync(
            request,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsBlankPathBeforeReading()
    {
        var reader = Substitute.For<IRegionStoryProgressReader>();
        var useCase = new ReadRegionStoryProgress(reader);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(
                new RegionStoryProgressReadRequest(" "),
                TestContext.Current.CancellationToken));

        Assert.Empty(reader.ReceivedCalls());
    }
}
