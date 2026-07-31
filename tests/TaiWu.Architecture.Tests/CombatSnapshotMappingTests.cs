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
    [InlineData(-1, PracticeDirection.Neutral)]
    [InlineData(0, PracticeDirection.Direct)]
    [InlineData(1, PracticeDirection.Reverse)]
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
    public void Skill_without_completed_breakthrough_has_no_active_direction()
    {
        var direction = CombatSnapshotMapping.MapPracticeDirection(
            direction: -1,
            isBrokenOut: false,
            skillId: 686);

        Assert.False(direction.IsAvailable);
        Assert.Contains("686", direction.UnavailableReason);
        Assert.Contains("breakthrough", direction.UnavailableReason);
    }

    [Fact]
    public void Broken_out_reverse_skill_maps_as_reverse()
    {
        var direction = CombatSnapshotMapping.MapPracticeDirection(
            direction: 1,
            isBrokenOut: true,
            skillId: 686);

        Assert.True(direction.IsAvailable);
        Assert.Equal(PracticeDirection.Reverse, direction.Value);
    }

    [Fact]
    public void Reading_pages_map_immediate_direct_breakthrough_only()
    {
        var availability = CombatSnapshotMapping
            .MapBreakthroughDirectionAvailability(
                readingState: 9928,
                isBrokenOut: false,
                canBreakthroughNow: true,
                skillId: 686);

        Assert.True(availability.IsAvailable);
        Assert.True(availability.Value.CanBreakthroughNow);
        Assert.Equal(
            [PracticeDirection.Direct],
            availability.Value.AvailableDirections);
    }

    [Fact]
    public void Flexible_reading_pages_map_both_breakthrough_directions()
    {
        const int outlineAndAllNormalPages =
            1 | (31 << 5) | (31 << 10);

        var availability = CombatSnapshotMapping
            .MapBreakthroughDirectionAvailability(
                outlineAndAllNormalPages,
                isBrokenOut: false,
                canBreakthroughNow: true,
                skillId: 100);

        Assert.True(availability.IsAvailable);
        Assert.Equal(
            [PracticeDirection.Direct, PracticeDirection.Reverse],
            availability.Value.AvailableDirections);
    }

    [Fact]
    public void Unready_skill_exposes_no_immediate_breakthrough_direction()
    {
        var availability = CombatSnapshotMapping
            .MapBreakthroughDirectionAvailability(
                readingState: 9928,
                isBrokenOut: false,
                canBreakthroughNow: false,
                skillId: 686);

        Assert.True(availability.IsAvailable);
        Assert.False(availability.Value.CanBreakthroughNow);
        Assert.Empty(availability.Value.AvailableDirections);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(2)]
    public void Unknown_practice_direction_remains_unavailable(int source)
    {
        var direction =
            CombatSnapshotMapping.MapPracticeDirection(source);

        Assert.False(direction.IsAvailable);
        Assert.Contains(
            source.ToString(System.Globalization.CultureInfo.InvariantCulture),
            direction.UnavailableReason);
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
