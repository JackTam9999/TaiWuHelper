using TaiWu.Application.Localization;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class TaiwuGameTextResolverTests
{
    [Fact]
    public async Task Resolve_UsesRequestedInstalledGameLanguage()
    {
        await using var fixture = await LanguageFixture.CreateAsync();
        var resolver = new TaiwuGameTextResolver();

        var english = resolver.CreateContext(
            fixture.SavePath,
            TaiwuLanguage.English);
        var chinese = resolver.CreateContext(
            fixture.SavePath,
            TaiwuLanguage.Chinese);

        Assert.Equal(
            "Chant of Abundance",
            english.Resolve("CombatSkill", "Name_0"));
        Assert.Equal(
            "沛然訣",
            chinese.Resolve("CombatSkill", "Name_0"));
    }

    [Fact]
    public async Task ResolveNameParts_ResolvesConcatenatedGameNameKeys()
    {
        await using var fixture = await LanguageFixture.CreateAsync();
        var resolver = new TaiwuGameTextResolver();
        var english = resolver.CreateContext(
            fixture.SavePath,
            TaiwuLanguage.English);
        var chinese = resolver.CreateContext(
            fixture.SavePath,
            TaiwuLanguage.Chinese);

        Assert.Equal(
            "Gui Chan",
            english.ResolveNameParts(
                "Name_714Name_126_Woman_Apart_12",
                " "));
        Assert.Equal(
            "貴嬋",
            chinese.ResolveNameParts(
                "Name_714Name_126_Woman_Apart_12",
                string.Empty));
        Assert.Equal(
            "葛",
            chinese.ResolveNameParts("SurName_602", string.Empty));
    }

    [Fact]
    public async Task Resolve_WhenKeyIsUnavailable_ReturnsOriginalKey()
    {
        await using var fixture = await LanguageFixture.CreateAsync();
        var resolver = new TaiwuGameTextResolver();
        var context = resolver.CreateContext(
            fixture.SavePath,
            TaiwuLanguage.English);

        Assert.Equal(
            "Name_999",
            context.Resolve("CombatSkill", "Name_999"));
    }

    private sealed class LanguageFixture : IAsyncDisposable
    {
        private LanguageFixture(string rootPath, string savePath)
        {
            RootPath = rootPath;
            SavePath = savePath;
        }

        public string RootPath { get; }

        public string SavePath { get; }

        public static async Task<LanguageFixture> CreateAsync()
        {
            var rootPath = Path.Combine(
                Path.GetTempPath(),
                "TaiWu.Infrastructure.UnitTests",
                Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(
                rootPath,
                "SaveGames",
                "world_1",
                "local.sav");
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await File.WriteAllBytesAsync(
                savePath,
                [],
                TestContext.Current.CancellationToken);

            foreach (var (folder, skillName, surname, firstName, suffix)
                     in new[]
                     {
                         (
                             "Language_EN",
                             "Chant of Abundance",
                             "Ge",
                             "Gui",
                             "Chan"),
                         (
                             "Language_CNH",
                             "沛然訣",
                             "葛",
                             "貴",
                             "嬋")
                     })
            {
                var languagePath = Path.Combine(
                    rootPath,
                    "The Scroll of Taiwu_Data",
                    "StreamingAssets",
                    folder);
                Directory.CreateDirectory(languagePath);
                await File.WriteAllLinesAsync(
                    Path.Combine(
                        languagePath,
                        "CombatSkill_language.txt"),
                    ["Name_0", skillName],
                    TestContext.Current.CancellationToken);
                await File.WriteAllLinesAsync(
                    Path.Combine(languagePath, "Name_language.txt"),
                    [
                        folder == "Language_CNH"
                            ? "Surname_602"
                            : "SurName_602",
                        surname,
                        "Name_714",
                        firstName,
                        "Name_126_Woman_Apart_12",
                        suffix
                    ],
                    TestContext.Current.CancellationToken);
            }

            return new LanguageFixture(rootPath, savePath);
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
