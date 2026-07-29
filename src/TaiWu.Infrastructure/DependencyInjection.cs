using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.SaveGames;
using TaiWu.Infrastructure.SaveGames;

namespace TaiWu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTaiwuInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<ISaveGameReader, TaiwuSaveGameReader>();
        return services;
    }
}
