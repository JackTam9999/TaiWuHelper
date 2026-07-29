using NSubstitute;
using TaiWu.Application.SaveGames;
using TaiWu.Domain.SaveGames;
using Xunit;

namespace TaiWu.Application.UnitTests.SaveGames;

public sealed class ReadSaveGameTests
{
    [Fact]
    public async Task ExecuteAsync_DelegatesRequestToReader()
    {
        var reader = Substitute.For<ISaveGameReader>();
        var expected = new SaveGameReport(["TAIWU|123|測試"]);
        var request = new SaveGameReadRequest("local.sav", 456);
        var cancellationToken = TestContext.Current.CancellationToken;
        reader.ReadAsync(request, cancellationToken).Returns(expected);
        var useCase = new ReadSaveGame(reader);

        var actual = await useCase.ExecuteAsync(request, cancellationToken);

        Assert.Same(expected, actual);
        await reader.Received(1).ReadAsync(request, cancellationToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task ExecuteAsync_WithMissingPath_RejectsRequest(string path)
    {
        var reader = Substitute.For<ISaveGameReader>();
        var useCase = new ReadSaveGame(reader);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(
                new SaveGameReadRequest(path),
                TestContext.Current.CancellationToken));

        Assert.Equal("request", exception.ParamName);
        Assert.Empty(reader.ReceivedCalls());
    }
}
