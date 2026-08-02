using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using TaiWu.Application.CombatSkills;
using TaiWu.Infrastructure.SaveGames;

namespace TaiWu.Infrastructure.Catalogue;

internal sealed class TaiwuCombatSkillDefinitionSource(
    ITaiwuCatalogueSourcePathProvider pathProvider,
    IReadOnlyFileFingerprintProvider fingerprintProvider,
    ICombatSkillConfigurationReader configurationReader)
    : ICombatSkillDefinitionSource
{
    public async Task<CombatSkillDefinitionSourceResult> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var located = pathProvider.Resolve();
        if (!located.IsAvailable)
        {
            return CombatSkillDefinitionSourceResult.MissingSources(
                located.Reason
                ?? "The installed catalogue sources could not be located.");
        }

        var paths = located.Paths!;
        var missing = MissingSourceNames(paths);
        if (missing.Length > 0)
        {
            return CombatSkillDefinitionSourceResult.MissingSources(
                "Required installed catalogue sources are missing: "
                + string.Join(", ", missing));
        }

        try
        {
            var before = await CaptureAsync(paths, cancellationToken)
                .ConfigureAwait(false);
            var loadedFingerprint = await fingerprintProvider.CaptureAsync(
                    configurationReader.LoadedConfigurationAssemblyPath,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(
                    before.GameData.Sha256,
                    loadedFingerprint.Sha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                return CombatSkillDefinitionSourceResult.UnsupportedVersion(
                    "The installed combat-skill configuration assembly does "
                    + "not match the loaded read-only GameData runtime. "
                    + "Rebuild and restart the helper.");
            }

            var installedVersion = FileVersionInfo.GetVersionInfo(
                    paths.GameDataConfigurationAssembly)
                .ProductVersion;
            if (string.IsNullOrWhiteSpace(installedVersion))
            {
                return CombatSkillDefinitionSourceResult.UnsupportedVersion(
                    "The installed GameData configuration assembly has no "
                    + "product-version metadata.");
            }

            if (!string.Equals(
                    installedVersion,
                    configurationReader.CompatibleGameDataVersion,
                    StringComparison.Ordinal))
            {
                return CombatSkillDefinitionSourceResult.UnsupportedVersion(
                    $"Installed GameData version {installedVersion} does not "
                    + "match the loaded read-only adapter version "
                    + $"{configurationReader.CompatibleGameDataVersion}.");
            }

            var traditionalChinese = await TaiwuLanguageCatalog.ReadAsync(
                    paths.TraditionalChineseCombatSkillLanguage,
                    "language-cnh",
                    cancellationToken)
                .ConfigureAwait(false);
            var english = await TaiwuLanguageCatalog.ReadAsync(
                    paths.EnglishCombatSkillLanguage,
                    "language-en",
                    cancellationToken)
                .ConfigureAwait(false);
            var configuration = configurationReader.ReadAll(cancellationToken);
            var sources = new CombatSkillCatalogueMappingSources(
                $"gamedata:{installedVersion}:{before.GameData.Sha256}",
                $"language-cnh:{before.TraditionalChinese.Sha256}",
                $"language-en:{before.English.Sha256}");
            var definitions = new List<Domain.CombatSkills.CombatSkillDefinition>();
            var diagnostics = new List<CombatSkillImportDiagnostic>();
            diagnostics.AddRange(configuration.Diagnostics);
            diagnostics.AddRange(traditionalChinese.Diagnostics);
            diagnostics.AddRange(english.Diagnostics);
            foreach (var record in configuration.Records.OrderBy(value => value.SkillId))
            {
                try
                {
                    definitions.Add(CombatSkillDefinitionMapper.Map(
                        record,
                        traditionalChinese.Catalog,
                        english.Catalog,
                        sources));
                }
                catch (Exception exception)
                {
                    diagnostics.Add(new CombatSkillImportDiagnostic(
                        CombatSkillImportDiagnosticSeverity.Error,
                        "CONFIGURATION_RECORD_IMPORT_FAILED",
                        $"combat-skill:{record.SkillId}",
                        exception.Message));
                }
            }

            var after = await CaptureAsync(paths, cancellationToken)
                .ConfigureAwait(false);
            if (before != after)
            {
                return CombatSkillDefinitionSourceResult.Failed(
                    "An installed catalogue source changed while it was being "
                    + "read; no import result was accepted.");
            }

            return CombatSkillDefinitionSourceResult.Available(
                new CombatSkillCatalogueSourceIdentity(
                    installedVersion,
                    before.GameData.Sha256,
                    before.TraditionalChinese.Sha256,
                    before.English.Sha256),
                definitions,
                diagnostics);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is IOException
                  or UnauthorizedAccessException
                  or InvalidDataException
                  or DecoderFallbackException)
        {
            return CombatSkillDefinitionSourceResult.Failed(
                $"The installed catalogue sources could not be read: "
                + exception.Message);
        }
    }

    private async Task<CapturedCatalogueSources> CaptureAsync(
        TaiwuCatalogueSourcePaths paths,
        CancellationToken cancellationToken)
    {
        var gameData = await fingerprintProvider.CaptureAsync(
                paths.GameDataConfigurationAssembly,
                cancellationToken)
            .ConfigureAwait(false);
        var traditionalChinese = await fingerprintProvider.CaptureAsync(
                paths.TraditionalChineseCombatSkillLanguage,
                cancellationToken)
            .ConfigureAwait(false);
        var english = await fingerprintProvider.CaptureAsync(
                paths.EnglishCombatSkillLanguage,
                cancellationToken)
            .ConfigureAwait(false);
        return new CapturedCatalogueSources(
            gameData,
            traditionalChinese,
            english);
    }

    private static ImmutableArray<string> MissingSourceNames(
        TaiwuCatalogueSourcePaths paths)
    {
        List<string> missing = [];
        if (!File.Exists(paths.GameDataConfigurationAssembly))
        {
            missing.Add("GameData.Shared.dll");
        }

        if (!File.Exists(paths.TraditionalChineseCombatSkillLanguage))
        {
            missing.Add("Language_CNH/CombatSkill_language.txt");
        }

        if (!File.Exists(paths.EnglishCombatSkillLanguage))
        {
            missing.Add("Language_EN/CombatSkill_language.txt");
        }

        return missing.Order(StringComparer.Ordinal).ToImmutableArray();
    }

    private sealed record CapturedCatalogueSources(
        ReadOnlyFileFingerprint GameData,
        ReadOnlyFileFingerprint TraditionalChinese,
        ReadOnlyFileFingerprint English);
}
