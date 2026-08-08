using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.SaveGames;
using TaiWu.Application.RegionStories;
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
        services.AddSingleton<
            IReadOnlyFileRevisionProvider,
            ReadOnlyFileRevisionProvider>();
        services.AddSingleton<ITaiwuArchiveLoader, TaiwuArchiveLoader>();
        services.AddSingleton(provider => new TaiwuArchiveReadSession(
            provider.GetRequiredService<IReadOnlyFileRevisionProvider>(),
            provider.GetRequiredService<IReadOnlyFileFingerprintProvider>(),
            provider.GetRequiredService<ITaiwuArchiveLoader>(),
            TimeProvider.System,
            provider.GetService<Microsoft.Extensions.Logging.ILogger<
                TaiwuArchiveReadSession>>()
            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                TaiwuArchiveReadSession>.Instance));
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
        services.AddSingleton(provider =>
            SaveProgressCacheStoragePathProvider.CreateDefault(
                ProtectedGameOwnedDirectories(provider)));
        services.AddSingleton(provider =>
            new SqliteCombatSkillCatalogueStore(
                provider.GetRequiredService<CatalogueStoragePathProvider>()));
        services.AddSingleton<ICombatSkillCatalogueRepository>(provider =>
            provider.GetRequiredService<SqliteCombatSkillCatalogueStore>());
        services.AddSingleton<ILegendaryBookEffectCatalogueRepository>(provider =>
            provider.GetRequiredService<SqliteCombatSkillCatalogueStore>());
        services.AddSingleton<SqliteCharacterCombatSkillProgressCache>();
        services.AddSingleton<
            ICharacterCombatSkillProgressCacheMaintenance>(provider =>
            provider.GetRequiredService<
                SqliteCharacterCombatSkillProgressCache>());
        services.AddSingleton<CombatSkillStudyDetailLabelSource>();
        services.AddSingleton<ICharacterCombatSkillProgressReader>(provider =>
            new TaiwuCharacterCombatSkillProgressReader(
                provider.GetRequiredService<TaiwuArchiveReadSession>(),
                provider.GetRequiredService<ITaiwuSaveFilePathProvider>(),
                provider.GetRequiredService<
                    CombatSkillStudyDetailLabelSource>(),
                provider.GetRequiredService<IReadOnlyFileRevisionProvider>(),
                provider.GetRequiredService<
                    SqliteCharacterCombatSkillProgressCache>(),
                TimeProvider.System,
                provider.GetService<Microsoft.Extensions.Logging.ILogger<
                    TaiwuCharacterCombatSkillProgressReader>>()
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<
                    TaiwuCharacterCombatSkillProgressReader>.Instance));
        services.AddSingleton<ICombatSnapshotReader, TaiwuCombatSnapshotReader>();
        services.AddSingleton<ISaveGameReader, TaiwuSaveGameReader>();
        services.AddSingleton<IRegionStoryProgressReader>(provider =>
            new TaiwuRegionStoryProgressReader(
                provider.GetRequiredService<TaiwuArchiveReadSession>(),
                provider.GetRequiredService<TaiwuGameTextResolver>(),
                TimeProvider.System));
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
