using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.GameData;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Application.UnitTests.CombatSnapshots;

public sealed class CombatSnapshotReadRequestTests
{
    [Fact]
    public void Request_preserves_valid_source_and_target()
    {
        var request = new CombatSnapshotReadRequest(
            @"C:\Taiwu\SaveGames\world_1\local.sav",
            16317);

        Assert.Equal(
            @"C:\Taiwu\SaveGames\world_1\local.sav",
            request.SaveFilePath);
        Assert.Equal(16317, request.TargetCharacterId);
        Assert.Equal(TaiwuLanguage.English, request.Language);
    }

    [Fact]
    public void Request_preserves_selected_language()
    {
        var request = new CombatSnapshotReadRequest(
            "local.sav",
            16317,
            language: TaiwuLanguage.Chinese);

        Assert.Equal(TaiwuLanguage.Chinese, request.Language);
    }

    [Fact]
    public void Request_preserves_helper_owned_loadout_observation()
    {
        var observation = new PlayerLoadoutObservation(
            DateTimeOffset.UtcNow,
            "sha256:screenshot",
            new CombatLoadoutSnapshot([], [], [], [], []),
            new GenericSlotAllocation(0, 0, 0, 0, 0));
        var request = new CombatSnapshotReadRequest(
            "local.sav",
            16317,
            observation);

        Assert.Same(observation, request.CurrentLoadoutObservation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Request_rejects_a_missing_save_path(string path)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new CombatSnapshotReadRequest(path, 16317));

        Assert.Equal("saveFilePath", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Request_rejects_an_invalid_target_id(int targetCharacterId)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSnapshotReadRequest(
                "local.sav",
                targetCharacterId));

        Assert.Equal("targetCharacterId", exception.ParamName);
    }

    [Fact]
    public void Reader_is_a_query_only_game_data_port()
    {
        Assert.True(
            typeof(IReadOnlyGameDataSource)
                .IsAssignableFrom(typeof(ICombatSnapshotReader)));

        var method = typeof(ICombatSnapshotReader)
            .GetMethod(nameof(ICombatSnapshotReader.ReadAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<CombatSnapshot>), method.ReturnType);
        Assert.Collection(
            method.GetParameters(),
            parameter => Assert.Equal(
                typeof(CombatSnapshotReadRequest),
                parameter.ParameterType),
            parameter => Assert.Equal(
                typeof(CancellationToken),
                parameter.ParameterType));
    }
}
