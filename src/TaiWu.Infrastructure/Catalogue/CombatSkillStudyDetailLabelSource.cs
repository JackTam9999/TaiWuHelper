using System.Collections.Immutable;
using System.Text;
using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Infrastructure.SaveGames;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed record CombatSkillStudyDetailLabelSet
{
    private readonly TaiwuLanguageCatalog? _catalog;
    private readonly string? _unavailableReason;

    internal CombatSkillStudyDetailLabelSet(
        CatalogueLanguage language,
        CatalogueSourceKind sourceKind,
        string sourceIdentity,
        TaiwuLanguageCatalog? catalog,
        string? unavailableReason,
        IEnumerable<CharacterCombatSkillProgressWarning>? warnings = null)
    {
        Language = language;
        SourceKind = sourceKind;
        SourceIdentity = sourceIdentity;
        _catalog = catalog;
        _unavailableReason = unavailableReason;
        Warnings = (warnings ?? []).ToImmutableArray();
    }

    internal CatalogueLanguage Language { get; }

    internal CatalogueSourceKind SourceKind { get; }

    internal string SourceIdentity { get; }

    internal ImmutableArray<CharacterCombatSkillProgressWarning> Warnings
    { get; }

    internal CatalogueField<string> Resolve(string localizationKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localizationKey);
        var source = new CatalogueSourceReference(
            SourceKind,
            SourceIdentity,
            localizationKey);
        if (_catalog is null)
        {
            return CatalogueField<string>.Unavailable(
                _unavailableReason
                ?? "The selected study-detail language source is unavailable.",
                source);
        }

        var value = _catalog.Find(localizationKey);
        return string.IsNullOrWhiteSpace(value)
            ? CatalogueField<string>.Unavailable(
                $"The selected language source has no value for "
                + $"{localizationKey}.",
                source)
            : CatalogueField<string>.Available(value, source);
    }
}

internal sealed class CombatSkillStudyDetailLabelSource(
    ITaiwuCatalogueSourcePathProvider pathProvider,
    IReadOnlyFileFingerprintProvider fingerprintProvider)
{
    internal async Task<CombatSkillStudyDetailLabelSet> ReadAsync(
        CatalogueLanguage language,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(language))
        {
            throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var sourceKind = SourceKind(language);
        var sourcePrefix = SourcePrefix(language);
        var located = pathProvider.Resolve();
        if (!located.IsAvailable)
        {
            return Unavailable(
                language,
                sourceKind,
                sourcePrefix,
                located.Reason
                ?? "The installed language source could not be located.");
        }

        var path = language == CatalogueLanguage.TraditionalChinese
            ? located.Paths!.TraditionalChineseUiLanguage
            : located.Paths!.EnglishUiLanguage;
        if (!File.Exists(path))
        {
            return Unavailable(
                language,
                sourceKind,
                sourcePrefix,
                "The selected installed study-detail language source is "
                + "missing.");
        }

        try
        {
            var before = await fingerprintProvider.CaptureAsync(
                    path,
                    cancellationToken)
                .ConfigureAwait(false);
            var sourceIdentity = $"{sourcePrefix}:{before.Sha256}";
            var read = await TaiwuLanguageCatalog.ReadAsync(
                    path,
                    sourceIdentity,
                    cancellationToken)
                .ConfigureAwait(false);
            var after = await fingerprintProvider.CaptureAsync(
                    path,
                    cancellationToken)
                .ConfigureAwait(false);
            if (before != after)
            {
                return Unavailable(
                    language,
                    sourceKind,
                    sourcePrefix,
                    "The selected study-detail language source changed "
                    + "while it was being read.");
            }

            var warnings = read.Diagnostics
                .Where(diagnostic => IsStudyDetailDiagnostic(
                    diagnostic.SourceRecordIdentity))
                .Select(diagnostic =>
                    new CharacterCombatSkillProgressWarning(
                        $"STUDY_DETAIL_LABEL_{diagnostic.Code}",
                        diagnostic.Reason));
            return new CombatSkillStudyDetailLabelSet(
                language,
                sourceKind,
                sourceIdentity,
                read.Catalog,
                unavailableReason: null,
                warnings);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is IOException
                  or UnauthorizedAccessException
                  or InvalidDataException
                  or DecoderFallbackException)
        {
            return Unavailable(
                language,
                sourceKind,
                sourcePrefix,
                "The selected installed study-detail language source could "
                + "not be read safely.");
        }
    }

    private static CombatSkillStudyDetailLabelSet Unavailable(
        CatalogueLanguage language,
        CatalogueSourceKind sourceKind,
        string sourcePrefix,
        string reason) => new(
            language,
            sourceKind,
            $"{sourcePrefix}:unavailable",
            catalog: null,
            reason,
            [
                new CharacterCombatSkillProgressWarning(
                    "STUDY_DETAIL_LABEL_SOURCE_UNAVAILABLE",
                    reason)
            ]);

    private static CatalogueSourceKind SourceKind(
        CatalogueLanguage language) => language switch
        {
            CatalogueLanguage.TraditionalChinese =>
                CatalogueSourceKind.TraditionalChineseLanguageResource,
            CatalogueLanguage.English =>
                CatalogueSourceKind.EnglishLanguageResource,
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.")
        };

    private static string SourcePrefix(CatalogueLanguage language) =>
        language switch
        {
            CatalogueLanguage.TraditionalChinese => "language-cnh",
            CatalogueLanguage.English => "language-en",
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown catalogue language.")
        };

    private static bool IsStudyDetailDiagnostic(string recordIdentity) =>
        recordIdentity.Contains(
            "LK_CombatSkill_First_Page_Type_",
            StringComparison.Ordinal)
        || recordIdentity.Contains(
            "LK_CombatSkill_Direct_Page_",
            StringComparison.Ordinal)
        || recordIdentity.Contains(
            "LK_CombatSkill_Reverse_Page_",
            StringComparison.Ordinal);
}
