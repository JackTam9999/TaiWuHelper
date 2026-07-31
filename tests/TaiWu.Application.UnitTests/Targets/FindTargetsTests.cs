using NSubstitute;
using TaiWu.Application.GameData;
using TaiWu.Application.Localization;
using TaiWu.Application.Targets;
using Xunit;

namespace TaiWu.Application.UnitTests.Targets;

public sealed class FindTargetsTests
{
    [Fact]
    public async Task Numeric_query_finds_exact_character_id()
    {
        var reader = Reader(Snapshot());
        var useCase = new FindTargets(reader);
        var cancellationToken = TestContext.Current.CancellationToken;

        var result = await useCase.ExecuteAsync(
            new FindTargetsRequest(
                "local.sav",
                "16317",
                language: TaiwuLanguage.Chinese),
            cancellationToken);

        Assert.Equal(TargetLookupStatus.Found, result.Status);
        Assert.Equal(1, result.TotalMatches);
        Assert.Equal(16317, Assert.Single(result.Matches).CharacterId);
        await reader.Received(1).ReadAsync(
            Arg.Is<TargetLookupReadRequest>(request =>
                request != null
                && request.SaveFilePath == "local.sav"
                && request.Language == TaiwuLanguage.Chinese),
            cancellationToken);
    }

    [Fact]
    public async Task Name_query_reports_ambiguity_with_location_context()
    {
        var reader = Reader(Snapshot());
        var useCase = new FindTargets(reader);

        var result = await useCase.ExecuteAsync(
            new FindTargetsRequest("local.sav", "何"),
            TestContext.Current.CancellationToken);

        Assert.Equal(TargetLookupStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.TotalMatches);
        Assert.Collection(
            result.Matches,
            first =>
            {
                Assert.Equal("何春石", first.DisplayName);
                Assert.Equal(10, first.AreaId);
                Assert.Equal(20, first.BlockId);
            },
            second =>
            {
                Assert.Equal("何春石", second.DisplayName);
                Assert.Equal(11, second.AreaId);
                Assert.Equal(21, second.BlockId);
            });
    }

    [Fact]
    public async Task Missing_query_is_an_explicit_not_found_result()
    {
        var useCase = new FindTargets(Reader(Snapshot()));

        var result = await useCase.ExecuteAsync(
            new FindTargetsRequest("local.sav", "不存在"),
            TestContext.Current.CancellationToken);

        Assert.Equal(TargetLookupStatus.NotFound, result.Status);
        Assert.Equal(0, result.TotalMatches);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task Result_limit_does_not_hide_total_ambiguity()
    {
        var useCase = new FindTargets(Reader(Snapshot()));

        var result = await useCase.ExecuteAsync(
            new FindTargetsRequest(
                "local.sav",
                "何",
                maxResults: 1),
            TestContext.Current.CancellationToken);

        Assert.Equal(TargetLookupStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.TotalMatches);
        Assert.Single(result.Matches);
    }

    [Fact]
    public async Task Reader_failure_is_propagated()
    {
        var reader = Substitute.For<ITargetLookupReader>();
        var failure = new InvalidDataException("Invalid save.");
        reader.ReadAsync(
                Arg.Any<TargetLookupReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TargetLookupSnapshot>(failure));
        var useCase = new FindTargets(reader);

        var actual = await Assert.ThrowsAsync<InvalidDataException>(
            () => useCase.ExecuteAsync(
                new FindTargetsRequest("local.sav", "何"),
                TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task Cancellation_is_propagated_to_reader()
    {
        var reader = Substitute.For<ITargetLookupReader>();
        using var cancellation = new CancellationTokenSource();
        reader.ReadAsync(
                Arg.Any<TargetLookupReadRequest>(),
                cancellation.Token)
            .Returns(_ =>
            {
                cancellation.Cancel();
                return Task.FromCanceled<TargetLookupSnapshot>(
                    cancellation.Token);
            });
        var useCase = new FindTargets(reader);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => useCase.ExecuteAsync(
                new FindTargetsRequest("local.sav", "何"),
                cancellation.Token));

        await reader.Received(1).ReadAsync(
            Arg.Any<TargetLookupReadRequest>(),
            cancellation.Token);
    }

    [Fact]
    public void Request_validates_query_and_result_limit()
    {
        Assert.Throws<ArgumentException>(
            () => new FindTargetsRequest("local.sav", " "));
        Assert.Throws<ArgumentException>(
            () => new FindTargetsRequest(" ", "何"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FindTargetsRequest(
                "local.sav",
                "何",
                FindTargetsRequest.MaximumResults + 1));
    }

    [Fact]
    public void Reader_is_a_query_only_game_data_port()
    {
        Assert.True(
            typeof(IReadOnlyGameDataSource)
                .IsAssignableFrom(typeof(ITargetLookupReader)));
        Assert.Equal(
            nameof(ITargetLookupReader.ReadAsync),
            Assert.Single(typeof(ITargetLookupReader).GetMethods()).Name);
    }

    private static ITargetLookupReader Reader(
        TargetLookupSnapshot snapshot)
    {
        var reader = Substitute.For<ITargetLookupReader>();
        reader.ReadAsync(
                Arg.Any<TargetLookupReadRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(snapshot);
        return reader;
    }

    private static TargetLookupSnapshot Snapshot()
    {
        return new TargetLookupSnapshot(
            DateTimeOffset.Parse("2026-07-30T12:00:00Z"),
            "game-version",
            [
                new TargetLookupEntry(
                    16317,
                    "何春石",
                    age: 52,
                    areaId: 10,
                    blockId: 20),
                new TargetLookupEntry(
                    20000,
                    "何春石",
                    age: 41,
                    areaId: 11,
                    blockId: 21),
                new TargetLookupEntry(
                    30000,
                    "太吾賢鑒",
                    age: 45,
                    areaId: 12,
                    blockId: 22)
            ],
            [
                new TargetLookupWarning(
                    "SOURCE_WARNING",
                    "Preserved warning.")
            ]);
    }
}
