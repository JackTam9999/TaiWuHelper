using TaiWu.Application.CompanionCandidates;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed class TaiwuCompanionDisciplineDisplaySource(
    ITaiwuCatalogueSourcePathProvider sourcePathProvider)
    : ICompanionDisciplineDisplaySource
{
    public async Task<CompanionDisciplineDisplayResult> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var located = sourcePathProvider.Resolve();
        if (located.Paths is null)
        {
            return Unavailable("DISCIPLINE_LANGUAGE_SOURCE_UNAVAILABLE");
        }

        try
        {
            var traditionalDirectory = Path.GetDirectoryName(
                located.Paths.TraditionalChineseCombatSkillLanguage)!;
            var englishDirectory = Path.GetDirectoryName(
                located.Paths.EnglishCombatSkillLanguage)!;
            var traditionalMartial = await ReadPackAsync(
                Path.Combine(
                    traditionalDirectory,
                    "CombatSkillType_language.txt"),
                "discipline-martial-zh-hant",
                cancellationToken).ConfigureAwait(false);
            var englishMartial = await ReadPackAsync(
                Path.Combine(
                    englishDirectory,
                    "CombatSkillType_language.txt"),
                "discipline-martial-en",
                cancellationToken).ConfigureAwait(false);
            var traditionalLifeSkill = await ReadPackAsync(
                Path.Combine(
                    traditionalDirectory,
                    "LifeSkillType_language.txt"),
                "discipline-life-skill-zh-hant",
                cancellationToken).ConfigureAwait(false);
            var englishLifeSkill = await ReadPackAsync(
                Path.Combine(
                    englishDirectory,
                    "LifeSkillType_language.txt"),
                "discipline-life-skill-en",
                cancellationToken).ConfigureAwait(false);
            var values = new List<CompanionDisciplineDisplayName>(30);
            AddDomain(
                values,
                CandidateDisciplineDomain.Martial,
                count: 14,
                traditionalMartial.Catalog,
                englishMartial.Catalog);
            AddDomain(
                values,
                CandidateDisciplineDomain.LifeSkill,
                count: 16,
                traditionalLifeSkill.Catalog,
                englishLifeSkill.Catalog);

            var status = values.All(value =>
                value.TraditionalChineseName is not null
                && value.EnglishName is not null)
                && new[]
                {
                    traditionalMartial,
                    englishMartial,
                    traditionalLifeSkill,
                    englishLifeSkill
                }.All(result => result.Diagnostics.IsEmpty)
                ? CompanionDisciplineDisplayStatus.Complete
                : CompanionDisciplineDisplayStatus.Partial;
            return new CompanionDisciplineDisplayResult(
                status,
                values);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return Unavailable("DISCIPLINE_LANGUAGE_READ_FAILED");
        }
    }

    private static void AddDomain(
        ICollection<CompanionDisciplineDisplayName> destination,
        CandidateDisciplineDomain domain,
        short count,
        TaiwuLanguageCatalog traditionalChinese,
        TaiwuLanguageCatalog english)
    {
        for (short type = 0; type < count; type++)
        {
            var key = $"Name_{type}";
            destination.Add(new CompanionDisciplineDisplayName(
                new CandidateDisciplineIdentity(domain, type),
                traditionalChinese.Find(key),
                english.Find(key)));
        }
    }

    private static Task<TaiwuLanguageCatalogReadResult> ReadPackAsync(
        string path,
        string sourceIdentity,
        CancellationToken cancellationToken) =>
        TaiwuLanguageCatalog.ReadAsync(
            path,
            sourceIdentity,
            cancellationToken);

    private static CompanionDisciplineDisplayResult Unavailable(
        string failureIdentity) => new(
        CompanionDisciplineDisplayStatus.Unavailable,
        disciplines: [],
        failureIdentity);
}
