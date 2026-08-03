using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.SaveGames;
using TaiWu.Application.Targets;
using TaiWu.Infrastructure.Catalogue;
using TaiWu.Infrastructure.SaveGames;

namespace TaiWu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaiwuInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<
            IReadOnlyFileFingerprintProvider,
            ReadOnlyFileFingerprintProvider>();
        services.AddSingleton<ITaiwuArchiveLoader, TaiwuArchiveLoader>();
        services.AddSingleton<TaiwuArchiveReadSession>();
        services.AddSingleton<ITaiwuSaveFilePathProvider>(provider =>
            new ConfiguredTaiwuSaveFilePathProvider(
                provider.GetService<IConfiguration>()));
        services.AddSingleton<TaiwuGameTextResolver>();
        services.AddSingleton<ITaiwuCatalogueSourcePathProvider>(
            _ => new TaiwuCatalogueSourcePathProvider());
        services.AddSingleton<
            ICombatSkillConfigurationReader,
            CombatSkillConfigurationReader>();
        services.AddSingleton<
            ICombatSkillDefinitionSource,
            TaiwuCombatSkillDefinitionSource>();
        services.AddSingleton<
            ICombatSkillFactionProfileSource,
            TaiwuCombatSkillFactionProfileSource>();
        services.AddSingleton(provider =>
            CatalogueStoragePathProvider.CreateDefault(
                ProtectedGameOwnedDirectories(provider)));
        services.AddSingleton<ICombatSkillCatalogueRepository>(provider =>
            new SqliteCombatSkillCatalogueStore(
                provider.GetRequiredService<CatalogueStoragePathProvider>()));
        services.AddSingleton<CombatSkillStudyDetailLabelSource>();
        services.AddSingleton<ICharacterCombatSkillProgressReader>(provider =>
            new TaiwuCharacterCombatSkillProgressReader(
                provider.GetRequiredService<TaiwuArchiveReadSession>(),
                provider.GetRequiredService<ITaiwuSaveFilePathProvider>(),
                provider.GetRequiredService<
                    CombatSkillStudyDetailLabelSource>(),
                TimeProvider.System));
        services.AddSingleton<ICombatSnapshotReader, TaiwuCombatSnapshotReader>();
        services.AddSingleton<ISaveGameReader, TaiwuSaveGameReader>();
        services.AddSingleton<ITargetLookupReader, TaiwuTargetLookupReader>();
        return services;
    }

    private static IReadOnlyList<string> ProtectedGameOwnedDirectories(
        IServiceProvider provider)
    {
        List<string> directories = [];
        var catalogueSources = provider
            .GetRequiredService<ITaiwuCatalogueSourcePathProvider>()
            .Resolve();
        if (catalogueSources.Paths is { } sources)
        {
            var backendDirectory = Path.GetDirectoryName(
                sources.GameDataConfigurationAssembly);
            var gameDirectory = backendDirectory is null
                ? null
                : Directory.GetParent(backendDirectory)?.FullName;
            if (gameDirectory is not null)
            {
                directories.Add(gameDirectory);
            }
        }

        var save = provider
            .GetRequiredService<ITaiwuSaveFilePathProvider>()
            .Resolve();
        var saveDirectory = save.SaveFilePath is null
            ? null
            : Path.GetDirectoryName(save.SaveFilePath);
        if (saveDirectory is not null)
        {
            directories.Add(saveDirectory);
        }

        if (directories.Count == 0)
        {
            var runtimeDirectory = Path.GetDirectoryName(
                typeof(Config.CombatSkill).Assembly.Location);
            if (runtimeDirectory is null)
            {
                throw new InvalidOperationException(
                    "A protected Taiwu runtime directory is unavailable.");
            }

            directories.Add(runtimeDirectory);
        }

        return directories;
    }
}
