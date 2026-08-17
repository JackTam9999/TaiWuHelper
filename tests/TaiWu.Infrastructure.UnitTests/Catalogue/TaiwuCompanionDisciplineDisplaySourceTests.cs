using System.Text;
using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Infrastructure;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class TaiwuCompanionDisciplineDisplaySourceTests
{
    [Fact]
    public async Task Reads_exact_bilingual_14_and_16_entry_installed_packs()
    {
        using var installation = new TemporaryInstallation();
        installation.WritePack(
            "Language_CNH",
            "CombatSkillType_language.txt",
            14,
            "武學");
        installation.WritePack(
            "Language_EN",
            "CombatSkillType_language.txt",
            14,
            "Martial");
        installation.WritePack(
            "Language_CNH",
            "LifeSkillType_language.txt",
            16,
            "技藝");
        installation.WritePack(
            "Language_EN",
            "LifeSkillType_language.txt",
            16,
            "Life");
        var source = new TaiwuCompanionDisciplineDisplaySource(
            new TaiwuCatalogueSourcePathProvider(installation.Root));

        var result = await source.ReadAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(CompanionDisciplineDisplayStatus.Complete, result.Status);
        Assert.Equal(30, result.Disciplines.Length);
        Assert.Equal(14, result.Disciplines.Count(value =>
            value.Discipline.Domain == CandidateDisciplineDomain.Martial));
        Assert.Equal(16, result.Disciplines.Count(value =>
            value.Discipline.Domain == CandidateDisciplineDomain.LifeSkill));
        Assert.Equal("武學0", result.Disciplines[0].TraditionalChineseName);
        Assert.Equal("Martial0", result.Disciplines[0].EnglishName);
        Assert.Equal((short)13, result.Disciplines[13].Discipline.Type);
        Assert.Equal((short)15, result.Disciplines[^1].Discipline.Type);
        Assert.Null(result.FailureIdentity);
    }

    [Fact]
    public async Task Missing_one_name_is_partial_without_cross_language_fallback()
    {
        using var installation = new TemporaryInstallation();
        installation.WritePack(
            "Language_CNH",
            "CombatSkillType_language.txt",
            14,
            "武學");
        installation.WritePack(
            "Language_EN",
            "CombatSkillType_language.txt",
            14,
            "Martial");
        installation.WritePack(
            "Language_CNH",
            "LifeSkillType_language.txt",
            16,
            "技藝");
        installation.WritePack(
            "Language_EN",
            "LifeSkillType_language.txt",
            15,
            "Life");
        var source = new TaiwuCompanionDisciplineDisplaySource(
            new TaiwuCatalogueSourcePathProvider(installation.Root));

        var result = await source.ReadAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(CompanionDisciplineDisplayStatus.Partial, result.Status);
        Assert.Equal(30, result.Disciplines.Length);
        var missing = result.Disciplines.Single(value =>
            value.Discipline.Domain == CandidateDisciplineDomain.LifeSkill
            && value.Discipline.Type == 15);
        Assert.Equal("技藝15", missing.TraditionalChineseName);
        Assert.Null(missing.EnglishName);
        Assert.Null(result.FailureIdentity);
    }

    [Fact]
    public async Task Missing_installed_pack_returns_typed_path_free_failure()
    {
        using var installation = new TemporaryInstallation();
        var source = new TaiwuCompanionDisciplineDisplaySource(
            new TaiwuCatalogueSourcePathProvider(installation.Root));

        var result = await source.ReadAsync(
            TestContext.Current.CancellationToken);

        Assert.Equal(
            CompanionDisciplineDisplayStatus.Unavailable,
            result.Status);
        Assert.Empty(result.Disciplines);
        Assert.Equal(
            "DISCIPLINE_LANGUAGE_READ_FAILED",
            result.FailureIdentity);
        Assert.DoesNotContain(installation.Root, result.FailureIdentity);
    }

    [Fact]
    public void Production_registration_exposes_read_only_display_source()
    {
        var services = new ServiceCollection();
        services.AddTaiwuInfrastructure();
        using var provider = services.BuildServiceProvider();

        var source = provider.GetRequiredService<
            ICompanionDisciplineDisplaySource>();

        Assert.IsType<TaiwuCompanionDisciplineDisplaySource>(source);
    }

    private sealed class TemporaryInstallation : IDisposable
    {
        internal TemporaryInstallation()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                $"taiwu-discipline-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Root);
        }

        internal string Root { get; }

        internal void WritePack(
            string languageDirectory,
            string fileName,
            int count,
            string prefix)
        {
            var directory = Path.Combine(
                Root,
                "The Scroll of Taiwu_Data",
                "StreamingAssets",
                languageDirectory);
            Directory.CreateDirectory(directory);
            var content = new StringBuilder();
            for (var index = 0; index < count; index++)
            {
                content.Append("Name_").Append(index).Append('\n')
                    .Append(prefix).Append(index).Append('\n');
            }

            File.WriteAllText(
                Path.Combine(directory, fileName),
                content.ToString());
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
