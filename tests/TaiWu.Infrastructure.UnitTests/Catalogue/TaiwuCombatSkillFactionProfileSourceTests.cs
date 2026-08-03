using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure.Catalogue;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests.Catalogue;

public sealed class TaiwuCombatSkillFactionProfileSourceTests
{
    [Theory]
    [InlineData(0, CombatSkillElement.Metal)]
    [InlineData(1, CombatSkillElement.Wood)]
    [InlineData(2, CombatSkillElement.Water)]
    [InlineData(3, CombatSkillElement.Fire)]
    [InlineData(4, CombatSkillElement.Earth)]
    [InlineData(5, CombatSkillElement.Mixed)]
    public void Elements_preserve_installed_configuration_order(
        int raw,
        CombatSkillElement expected)
    {
        Assert.Equal(
            expected,
            TaiwuCombatSkillFactionProfileSource.MapElement(raw));
    }

    [Theory]
    [InlineData(0, CombatSkillFactionAlignment.Just)]
    [InlineData(1, CombatSkillFactionAlignment.Kind)]
    [InlineData(2, CombatSkillFactionAlignment.Even)]
    [InlineData(3, CombatSkillFactionAlignment.Rebel)]
    [InlineData(4, CombatSkillFactionAlignment.Egoistic)]
    public void Alignments_preserve_installed_behavior_order(
        int raw,
        CombatSkillFactionAlignment expected)
    {
        Assert.Equal(
            expected,
            TaiwuCombatSkillFactionProfileSource.MapAlignment(raw));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void Unknown_profile_values_remain_unavailable(int raw)
    {
        Assert.Null(TaiwuCombatSkillFactionProfileSource.MapElement(raw));
        Assert.Null(TaiwuCombatSkillFactionProfileSource.MapAlignment(raw));
    }

    [Fact]
    public void Missing_morality_sentinel_remains_unavailable()
    {
        Assert.Null(
            TaiwuCombatSkillFactionProfileSource.MapMorality(short.MinValue));
    }
}
