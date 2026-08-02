using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class TaiwuLanguageCatalogTests
{
    [Fact]
    public async Task Reads_key_value_pairs_without_language_fallback()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Write(
            "language.txt",
            "\uFEFFName_2\nSecond\nName_1\nFirst\n");

        var result = await TaiwuLanguageCatalog.ReadAsync(
            path,
            "language-test",
            TestContext.Current.CancellationToken);

        Assert.Equal("First", result.Catalog.Find("Name_1"));
        Assert.Equal("Second", result.Catalog.Find("Name_2"));
        Assert.Null(result.Catalog.Find("Missing"));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task Duplicate_and_dangling_keys_produce_stable_diagnostics()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.Write(
            "language.txt",
            "Name_1\nFirst\nName_1\nIgnored\nDangling\n");

        var first = await TaiwuLanguageCatalog.ReadAsync(
            path,
            "language-test",
            TestContext.Current.CancellationToken);
        var second = await TaiwuLanguageCatalog.ReadAsync(
            path,
            "language-test",
            TestContext.Current.CancellationToken);

        Assert.Equal("First", first.Catalog.Find("Name_1"));
        Assert.Equal(first.Diagnostics, second.Diagnostics);
        Assert.Collection(
            first.Diagnostics,
            duplicate => Assert.Equal(
                "LANGUAGE_KEY_DUPLICATE",
                duplicate.Code),
            dangling => Assert.Equal(
                "LANGUAGE_VALUE_MISSING",
                dangling.Code));
    }

    [Fact]
    public void Fixed_path_provider_derives_only_known_source_files()
    {
        var root = Path.GetFullPath(Path.Combine("test-data", "taiwu"));
        var result = new TaiwuCatalogueSourcePathProvider(root).Resolve();

        Assert.True(result.IsAvailable);
        Assert.Equal(
            Path.Combine(root, "Backend", "GameData.Shared.dll"),
            result.Paths!.GameDataConfigurationAssembly);
        Assert.EndsWith(
            Path.Combine(
                "Language_CNH",
                "CombatSkill_language.txt"),
            result.Paths.TraditionalChineseCombatSkillLanguage,
            StringComparison.Ordinal);
        Assert.EndsWith(
            Path.Combine(
                "Language_EN",
                "CombatSkill_language.txt"),
            result.Paths.EnglishCombatSkillLanguage,
            StringComparison.Ordinal);
        Assert.EndsWith(
            Path.Combine("Language_CNH", "ui_language.txt"),
            result.Paths.TraditionalChineseUiLanguage,
            StringComparison.Ordinal);
        Assert.EndsWith(
            Path.Combine("Language_EN", "ui_language.txt"),
            result.Paths.EnglishUiLanguage,
            StringComparison.Ordinal);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(
            Path.GetTempPath(),
            $"taiwu-catalogue-tests-{Guid.NewGuid():N}");

        internal TemporaryDirectory()
        {
            Directory.CreateDirectory(_path);
        }

        internal string Write(string fileName, string content)
        {
            var path = Path.Combine(_path, fileName);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        }
    }
}
