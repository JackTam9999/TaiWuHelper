using Microsoft.Extensions.Configuration;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class ConfiguredTaiwuSaveFilePathProviderTests
{
    [Fact]
    public void Missing_configuration_is_explicit_and_path_free()
    {
        var result = new ConfiguredTaiwuSaveFilePathProvider(
                configuration: null)
            .Resolve();

        Assert.False(result.IsAvailable);
        Assert.Contains(
            ConfiguredTaiwuSaveFilePathProvider.ConfigurationKey,
            result.Reason);
        Assert.DoesNotContain("\\", result.Reason);
    }

    [Theory]
    [InlineData("relative.sav")]
    [InlineData("relative.txt")]
    public void Invalid_configuration_is_rejected(string configured)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfiguredTaiwuSaveFilePathProvider.ConfigurationKey] =
                    configured
            })
            .Build();

        var result = new ConfiguredTaiwuSaveFilePathProvider(configuration)
            .Resolve();

        Assert.False(result.IsAvailable);
        Assert.DoesNotContain(configured, result.Reason);
    }

    [Fact]
    public void Absolute_save_configuration_is_normalized_without_io()
    {
        var configured = Path.Combine(
            Path.GetTempPath(),
            "trusted",
            "current.sav");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ConfiguredTaiwuSaveFilePathProvider.ConfigurationKey] =
                    configured
            })
            .Build();

        var result = new ConfiguredTaiwuSaveFilePathProvider(configuration)
            .Resolve();

        Assert.True(result.IsAvailable);
        Assert.Equal(Path.GetFullPath(configured), result.SaveFilePath);
        Assert.Null(result.Reason);
    }
}
