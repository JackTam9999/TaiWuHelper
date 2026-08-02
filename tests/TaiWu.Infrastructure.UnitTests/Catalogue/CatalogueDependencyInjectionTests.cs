using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CombatSkills;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class CatalogueDependencyInjectionTests
{
    [Fact]
    public void Production_registration_resolves_one_guarded_catalogue_repository()
    {
        var savePath = Path.Combine(
            Path.GetTempPath(),
            "TaiwuTestGameOwned",
            "SaveGames",
            "world_1",
            "local.sav");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SaveGames:DefaultSaveFilePath"] = savePath
            })
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddTaiwuInfrastructure()
            .BuildServiceProvider();

        var first = services.GetRequiredService<
            ICombatSkillCatalogueRepository>();
        var second = services.GetRequiredService<
            ICombatSkillCatalogueRepository>();

        Assert.IsType<SqliteCombatSkillCatalogueStore>(first);
        Assert.Same(first, second);
    }
}
