using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<TaiwuGameTextResolver>();
        services.AddSingleton<ITaiwuCatalogueSourcePathProvider>(
            _ => new TaiwuCatalogueSourcePathProvider());
        services.AddSingleton<
            ICombatSkillConfigurationReader,
            CombatSkillConfigurationReader>();
        services.AddSingleton<
            ICombatSkillDefinitionSource,
            TaiwuCombatSkillDefinitionSource>();
        services.AddSingleton<ICombatSnapshotReader, TaiwuCombatSnapshotReader>();
        services.AddSingleton<ISaveGameReader, TaiwuSaveGameReader>();
        services.AddSingleton<ITargetLookupReader, TaiwuTargetLookupReader>();
        return services;
    }
}
