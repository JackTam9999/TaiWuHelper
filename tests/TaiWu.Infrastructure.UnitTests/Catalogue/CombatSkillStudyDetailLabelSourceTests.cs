using TaiWu.Domain.CombatSkills;
using TaiWu.Infrastructure.Catalogue;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class CombatSkillStudyDetailLabelSourceTests
{
    [Fact]
    public async Task Reads_selected_language_with_fingerprint_provenance()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteLanguage(
            "Language_EN",
            "LK_CombatSkill_Direct_Page_0\nMight\n");
        var source = new CombatSkillStudyDetailLabelSource(
            new TaiwuCatalogueSourcePathProvider(directory.Path),
            new ReadOnlyFileFingerprintProvider());

        var result = await source.ReadAsync(
            CatalogueLanguage.English,
            TestContext.Current.CancellationToken);
        var label = result.Resolve("LK_CombatSkill_Direct_Page_0");

        Assert.True(label.IsAvailable);
        Assert.Equal("Might", label.Value);
        Assert.Equal(
            CatalogueSourceKind.EnglishLanguageResource,
            label.Source!.Kind);
        Assert.StartsWith("language-en:", label.Source.SourceIdentity);
        Assert.Equal(
            "LK_CombatSkill_Direct_Page_0",
            label.Source.RecordIdentity);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Missing_selected_language_is_explicit_and_path_free()
    {
        using var directory = new TemporaryDirectory();
        var source = new CombatSkillStudyDetailLabelSource(
            new TaiwuCatalogueSourcePathProvider(directory.Path),
            new ReadOnlyFileFingerprintProvider());

        var result = await source.ReadAsync(
            CatalogueLanguage.TraditionalChinese,
            TestContext.Current.CancellationToken);
        var label = result.Resolve("LK_CombatSkill_First_Page_Type_0");

        Assert.False(label.IsAvailable);
        Assert.DoesNotContain(directory.Path, label.Reason);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("STUDY_DETAIL_LABEL_SOURCE_UNAVAILABLE", warning.Code);
        Assert.DoesNotContain(directory.Path, warning.Reason);
    }

    [Fact]
    public async Task Cancellation_is_propagated_before_source_resolution()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new CombatSkillStudyDetailLabelSource(
            new ThrowingPathProvider(),
            new ThrowingFingerprintProvider());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ReadAsync(
                CatalogueLanguage.English,
                cancellation.Token));
    }

    private sealed class ThrowingPathProvider
        : ITaiwuCatalogueSourcePathProvider
    {
        public TaiwuCatalogueSourcePathResult Resolve() =>
            throw new InvalidOperationException("Should not be called.");
    }

    private sealed class ThrowingFingerprintProvider
        : IReadOnlyFileFingerprintProvider
    {
        public Task<ReadOnlyFileFingerprint> CaptureAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Should not be called.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"taiwu-study-label-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        internal void WriteLanguage(string languageDirectory, string content)
        {
            var directory = System.IO.Path.Combine(
                Path,
                "The Scroll of Taiwu_Data",
                "StreamingAssets",
                languageDirectory);
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                System.IO.Path.Combine(directory, "ui_language.txt"),
                content);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
