using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CombatSnapshots;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Architecture.Tests;

public sealed class CombatSnapshotMappingTests
{
    [Theory]
    [InlineData(0, SkillCategory.Neigong)]
    [InlineData(1, SkillCategory.Attack)]
    [InlineData(2, SkillCategory.Agility)]
    [InlineData(3, SkillCategory.Defense)]
    [InlineData(4, SkillCategory.Assistance)]
    public void Equip_types_map_to_domain_categories(
        int equipType,
        SkillCategory expected)
    {
        var mapped = CombatSnapshotMapping.TryMapSkillCategory(
            equipType,
            out var category);

        Assert.True(mapped);
        Assert.Equal(expected, category);
    }

    [Fact]
    public void Unknown_equip_type_is_rejected()
    {
        Assert.False(
            CombatSnapshotMapping.TryMapSkillCategory(
                equipType: 9,
                out _));
    }

    [Theory]
    [InlineData(-1, PracticeDirection.Reverse)]
    [InlineData(0, PracticeDirection.Neutral)]
    [InlineData(1, PracticeDirection.Direct)]
    public void Practice_direction_preserves_GameData_semantics(
        int source,
        PracticeDirection expected)
    {
        var direction =
            CombatSnapshotMapping.MapPracticeDirection(source);

        Assert.True(direction.IsAvailable);
        Assert.Equal(expected, direction.Value);
    }

    [Fact]
    public void Unknown_practice_direction_remains_unavailable()
    {
        var direction =
            CombatSnapshotMapping.MapPracticeDirection(2);

        Assert.False(direction.IsAvailable);
        Assert.Contains("2", direction.UnavailableReason);
    }

    [Fact]
    public void Inner_skill_grid_bonuses_map_in_display_order()
    {
        var contribution =
            CombatSnapshotMapping.MapSlotContribution(
                [1, 0, 2, -1],
                genericGrid: 3);

        Assert.Equal(1, contribution.Attack);
        Assert.Equal(0, contribution.Agility);
        Assert.Equal(2, contribution.Defense);
        Assert.Equal(-1, contribution.Assistance);
        Assert.Equal(3, contribution.Generic);
    }

    [Theory]
    [InlineData(3, false, 3)]
    [InlineData(3, true, 2)]
    [InlineData(1, true, 1)]
    public void Mastery_adjustment_never_reduces_cost_below_one(
        int configuredCost,
        bool mastered,
        int expected)
    {
        Assert.Equal(
            expected,
            CombatSnapshotMapping.CalculateMasteryAdjustedGridCost(
                configuredCost,
                mastered));
    }

    [Theory]
    [InlineData(0, EquipmentKind.Weapon)]
    [InlineData(1, EquipmentKind.Armor)]
    [InlineData(2, EquipmentKind.Accessory)]
    [InlineData(9, EquipmentKind.Other)]
    public void Equipment_types_map_without_exposing_GameData(
        sbyte itemType,
        EquipmentKind expected)
    {
        Assert.Equal(
            expected,
            CombatSnapshotMapping.MapEquipmentKind(itemType));
    }

    [Fact]
    public void Infrastructure_registers_snapshot_reader_as_singleton()
    {
        ServiceCollection services = [];

        services.AddTaiwuInfrastructure();

        var descriptor = Assert.Single(
            services,
            service =>
                service.ServiceType == typeof(ICombatSnapshotReader));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(
            "TaiwuCombatSnapshotReader",
            descriptor.ImplementationType?.Name);
    }
}
