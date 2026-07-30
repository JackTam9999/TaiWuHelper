using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Application.SaveGames;
using TaiWu.Application.Targets;
using TaiWu.Infrastructure.SaveGames;

namespace TaiWu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaiwuInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<ICombatSnapshotReader, TaiwuCombatSnapshotReader>();
        services.AddSingleton<ISaveGameReader, TaiwuSaveGameReader>();
        services.AddSingleton<ITargetLookupReader, TaiwuTargetLookupReader>();
        return services;
    }
}
