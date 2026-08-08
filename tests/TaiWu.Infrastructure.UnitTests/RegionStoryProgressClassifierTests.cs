using TaiWu.Application.RegionStories;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class RegionStoryProgressClassifierTests
{
    [Theory]
    [InlineData(true, false, false, RegionStoryProgressStatus.ProsperousEnding)]
    [InlineData(false, true, false, RegionStoryProgressStatus.FailingEnding)]
    [InlineData(false, false, true, RegionStoryProgressStatus.InProgress)]
    [InlineData(false, false, false, RegionStoryProgressStatus.NotCompleted)]
    public void Classify_UsesEndingThenActiveTaskPrecedence(
        bool prosperous,
        bool failing,
        bool activeTask,
        RegionStoryProgressStatus expected)
    {
        var actual = TaiwuRegionStoryProgressReader.Classify(
            prosperous,
            failing,
            activeTask);

        Assert.Equal(expected, actual);
    }
}
