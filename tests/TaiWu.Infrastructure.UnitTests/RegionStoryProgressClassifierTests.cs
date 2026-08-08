using TaiWu.Application.RegionStories;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class RegionStoryProgressClassifierTests
{
    [Theory]
    [InlineData(true, false, false, null, false, RegionStoryProgressStatus.ProsperousEnding)]
    [InlineData(false, true, false, null, false, RegionStoryProgressStatus.FailingEnding)]
    [InlineData(false, false, true, true, false, RegionStoryProgressStatus.ProsperousEnding)]
    [InlineData(false, false, true, false, false, RegionStoryProgressStatus.FailingEnding)]
    [InlineData(false, false, true, null, true, RegionStoryProgressStatus.CompletedEndingUnrecorded)]
    [InlineData(false, false, false, false, true, RegionStoryProgressStatus.InProgress)]
    [InlineData(false, false, false, null, false, RegionStoryProgressStatus.NotCompleted)]
    public void Classify_UsesEndingThenUnlockThenActiveTaskPrecedence(
        bool prosperous,
        bool failing,
        bool mainStoryFunctionUnlocked,
        bool? inferredProsperousEnding,
        bool activeTask,
        RegionStoryProgressStatus expected)
    {
        var actual = TaiwuRegionStoryProgressReader.Classify(
            prosperous,
            failing,
            mainStoryFunctionUnlocked,
            inferredProsperousEnding,
            activeTask);

        Assert.Equal(expected, actual);
    }
}
