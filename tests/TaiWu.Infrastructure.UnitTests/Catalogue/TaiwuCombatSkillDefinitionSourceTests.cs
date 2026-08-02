using System.Collections.Immutable;
using System.Diagnostics;
using TaiWu.Application.CombatSkills;
using TaiWu.Infrastructure.Catalogue;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class TaiwuCombatSkillDefinitionSourceTests
{
    [Fact]
    public async Task Imports_valid_records_and_diagnoses_unrepresentable_record()
    {
        using var directory = new TemporaryDirectory();
        var gameData = typeof(TaiwuCombatSkillDefinitionSourceTests)
            .Assembly.Location;
        var traditionalChinese = directory.Write(
            "cnh.txt",
            "Name_1\n一\nName_2\n二\n");
        var english = directory.Write(
            "en.txt",
            "Name_1\nOne\nName_2\nTwo\n");
        var version = FileVersionInfo.GetVersionInfo(gameData).ProductVersion!;
        var records = new[]
        {
            Record(2),
            Record(-1),
            Record(1)
        };
        var source = new TaiwuCombatSkillDefinitionSource(
            new FixedPathProvider(
                gameData,
                traditionalChinese,
                english),
            new ReadOnlyFileFingerprintProvider(),
            new FixedConfigurationReader(version, gameData, records));

        var result = await source.ReadAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(DefinitionSourceReadStatus.Available, result.Status);
        Assert.Equal(
            TaiwuCombatSkillDefinitionSource.ImporterVersion,
            result.SourceIdentity!.ImporterVersion);
        Assert.Equal([1, 2], result.Definitions.Select(value => value.SkillId));
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(
            CombatSkillImportDiagnosticSeverity.Error,
            diagnostic.Severity);
        Assert.Equal(
            "CONFIGURATION_RECORD_IMPORT_FAILED",
            diagnostic.Code);
        Assert.Equal("combat-skill:-1", diagnostic.SourceRecordIdentity);
        Assert.Equal(
            "One",
            result.Definitions[0].Names
                .Get(Domain.CombatSkills.CatalogueLanguage.English)
                .Value.Text);
    }

    [Fact]
    public async Task Missing_sources_return_typed_status_before_any_read()
    {
        using var directory = new TemporaryDirectory();
        var source = new TaiwuCombatSkillDefinitionSource(
            new FixedPathProvider(
                Path.Combine(directory.Path, "missing.dll"),
                Path.Combine(directory.Path, "missing-cnh.txt"),
                Path.Combine(directory.Path, "missing-en.txt")),
            new ThrowingFingerprintProvider(),
            new ThrowingConfigurationReader());

        var result = await source.ReadAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(DefinitionSourceReadStatus.MissingSources, result.Status);
        Assert.Contains("GameData.Shared.dll", result.Reason);
        Assert.Empty(result.Definitions);
    }

    [Fact]
    public async Task Cancellation_is_propagated_before_source_resolution()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var source = new TaiwuCombatSkillDefinitionSource(
            new ThrowingPathProvider(),
            new ThrowingFingerprintProvider(),
            new ThrowingConfigurationReader());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.ReadAsync(cancellation.Token));
    }

    private static CombatSkillSourceRecord Record(int skillId) => new(
        skillId,
        $"Name_{skillId}",
        DescriptionKey: null,
        Category: 0,
        Grade: 0,
        Faction: 0,
        Element: 0,
        EquipmentType: 0,
        BaseGridCost: 1,
        SpecificGrids: [0, 0, 0, 0],
        GenericGrid: 0,
        PreparationProgress: 0,
        BreathStanceCost: 0,
        CastSpeed: 0,
        DirectEffectId: 1,
        ReverseEffectId: 2,
        Requirements: []);

    private sealed class FixedPathProvider(
        string gameData,
        string traditionalChinese,
        string english) : ITaiwuCatalogueSourcePathProvider
    {
        public TaiwuCatalogueSourcePathResult Resolve() => new(
            new TaiwuCatalogueSourcePaths(
                gameData,
                traditionalChinese,
                english),
            Reason: null);
    }

    private sealed class FixedConfigurationReader(
        string version,
        string assemblyPath,
        IEnumerable<CombatSkillSourceRecord> records)
        : ICombatSkillConfigurationReader
    {
        public string CompatibleGameDataVersion => version;

        public string LoadedConfigurationAssemblyPath => assemblyPath;

        public CombatSkillConfigurationReadResult ReadAll(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new CombatSkillConfigurationReadResult(
                records.ToImmutableArray(),
                Diagnostics: []);
        }
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

    private sealed class ThrowingConfigurationReader
        : ICombatSkillConfigurationReader
    {
        public string CompatibleGameDataVersion =>
            throw new InvalidOperationException("Should not be called.");

        public string LoadedConfigurationAssemblyPath =>
            throw new InvalidOperationException("Should not be called.");

        public CombatSkillConfigurationReadResult ReadAll(
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Should not be called.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"taiwu-source-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        internal string Write(string fileName, string content)
        {
            var path = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(path, content);
            return path;
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
