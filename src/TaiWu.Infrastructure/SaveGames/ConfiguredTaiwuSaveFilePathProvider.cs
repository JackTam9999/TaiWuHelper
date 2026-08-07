using Microsoft.Extensions.Configuration;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed record TaiwuSaveFilePathResult(
    string? SaveFilePath,
    string? Reason)
{
    internal bool IsAvailable => SaveFilePath is not null;
}

internal interface ITaiwuSaveFilePathProvider
{
    TaiwuSaveFilePathResult Resolve();
}

internal sealed class ConfiguredTaiwuSaveFilePathProvider(
    IConfiguration? configuration) : ITaiwuSaveFilePathProvider
{
    internal const string ConfigurationKey =
        "SaveGames:DefaultSaveFilePath";

    public TaiwuSaveFilePathResult Resolve()
    {
        var configured = configuration?[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(configured))
        {
            return Missing(
                $"Trusted configuration '{ConfigurationKey}' is not set.");
        }

        if (!Path.IsPathFullyQualified(configured)
            || !string.Equals(
                Path.GetExtension(configured),
                ".sav",
                StringComparison.OrdinalIgnoreCase))
        {
            return Missing(
                $"Trusted configuration '{ConfigurationKey}' must identify "
                + "an absolute Taiwu save file.");
        }

        return new TaiwuSaveFilePathResult(
            Path.GetFullPath(configured),
            Reason: null);
    }

    private static TaiwuSaveFilePathResult Missing(string reason) =>
        new(SaveFilePath: null, reason);
}
