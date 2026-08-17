using Microsoft.Extensions.DependencyInjection;
using TaiWu.Application.CombatSkills;
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
        var direction = CombatSnapshotMapping.MapActivePracticeDirection(
            activationState: 0,
            skillId: 686);

        Assert.False(direction.IsAvailable);
        Assert.Contains("686", direction.UnavailableReason);
        Assert.Contains("breakthrough", direction.UnavailableReason);
    }

    [Theory]
    [InlineData(40, 14881, PracticeDirection.Reverse)]
    [InlineData(41, 996, PracticeDirection.Direct)]
    public void Golden_broken_out_skill_maps_its_active_direction(
        int skillId,
        int activationState,
        PracticeDirection expected)
    {
        var direction = CombatSnapshotMapping.MapActivePracticeDirection(
            activationState,
            skillId);

        Assert.True(direction.IsAvailable);
        Assert.Equal(expected, direction.Value);
    }

    [Fact]
    public void Reading_pages_map_immediate_direct_breakthrough_only()
    {
        var availability = CombatSnapshotMapping
            .MapBreakthroughDirectionAvailability(
                readingState: 9928,
                activationState: 9920,
                meetsReadingRequirement: true,
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
                activationState: 0,
                meetsReadingRequirement: true,
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
                activationState: 9920,
                meetsReadingRequirement: false,
                skillId: 686);

        Assert.True(availability.IsAvailable);
        Assert.False(availability.Value.CanBreakthroughNow);
        Assert.Empty(availability.Value.AvailableDirections);
    }

    [Fact]
    public void Completed_breakthrough_is_not_reported_as_immediately_ready()
    {
        const int completeReadingState = 32767;
        const int outlineAndDirectPages = 1 | (31 << 5);

        var availability = CombatSnapshotMapping
            .MapBreakthroughDirectionAvailability(
                completeReadingState,
                outlineAndDirectPages,
                meetsReadingRequirement: true,
                skillId: 41);

        Assert.True(availability.IsAvailable);
        Assert.True(availability.Value.IsBrokenOut);
        Assert.False(availability.Value.CanBreakthroughNow);
        Assert.Empty(availability.Value.AvailableDirections);
    }

    [Fact]
    public void Reading_prerequisite_must_match_the_page_bits()
    {
        var availability = CombatSnapshotMapping
            .MapBreakthroughDirectionAvailability(
                readingState: 1,
                activationState: 0,
                meetsReadingRequirement: true,
                skillId: 100);

        Assert.False(availability.IsAvailable);
        Assert.Contains("five normal pages", availability.UnavailableReason);
    }

    [Fact]
    public void Study_details_preserve_read_and_active_bits_independently()
    {
        const int completeReadingState = 32767;
        const int allReversePagesActive = 31 << 10;

        var mapped = CombatSnapshotMapping.MapStudyDetails(
            completeReadingState,
            allReversePagesActive,
            skillId: 456);

        Assert.True(mapped.IsAvailable);
        Assert.Equal(15, mapped.Value.Count);
        Assert.All(mapped.Value, detail => Assert.True(detail.IsRead));
        Assert.All(
            mapped.Value.Where(detail =>
                detail.Group == CombatSkillStudyDetailGroup.Reverse),
            detail => Assert.True(detail.IsActive));
        Assert.All(
            mapped.Value.Where(detail =>
                detail.Group != CombatSkillStudyDetailGroup.Reverse),
            detail => Assert.False(detail.IsActive));
    }

    [Fact]
    public void Study_details_have_stable_groups_keys_and_wheel_order()
    {
        var mapped = CombatSnapshotMapping.MapStudyDetails(
            readingState: 0,
            activationState: 0,
            skillId: 498);

        Assert.True(mapped.IsAvailable);
        Assert.Equal(
            Enumerable.Range(0, 15),
            mapped.Value.Select(detail => detail.InternalIndex));
        Assert.Equal(
            Enumerable.Range(0, 15),
            mapped.Value.OrderBy(detail => detail.WheelOrder)
                .Select(detail => detail.WheelOrder));
        Assert.Equal(
            [
                "outline-2", "outline-3", "outline-4",
                "direct-0", "direct-1", "direct-2", "direct-3", "direct-4",
                "reverse-4", "reverse-3", "reverse-2", "reverse-1",
                "reverse-0", "outline-0", "outline-1"
            ],
            mapped.Value.OrderBy(detail => detail.WheelOrder)
                .Select(detail => detail.StableId));
        Assert.Equal("outline-0", mapped.Value[0].StableId);
        Assert.Equal(
            "LK_CombatSkill_First_Page_Type_0",
            mapped.Value[0].LocalizationKey);
        Assert.Equal("direct-0", mapped.Value[5].StableId);
        Assert.Equal(
            "LK_CombatSkill_Direct_Page_0",
            mapped.Value[5].LocalizationKey);
        Assert.Equal("reverse-4", mapped.Value[14].StableId);
        Assert.Equal(
            "LK_CombatSkill_Reverse_Page_4",
            mapped.Value[14].LocalizationKey);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(32768, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 32768)]
    public void Unsupported_study_state_remains_unavailable(
        int readingState,
        int activationState)
    {
        var mapped = CombatSnapshotMapping.MapStudyDetails(
            readingState,
            activationState,
            skillId: 100);

        Assert.False(mapped.IsAvailable);
    }

    [Fact]
    public void Unsupported_activation_state_has_no_practice_direction()
    {
        var direction = CombatSnapshotMapping.MapActivePracticeDirection(
            activationState: 32768,
            skillId: 100);

        Assert.False(direction.IsAvailable);
        Assert.Contains("activation state", direction.UnavailableReason);
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
    [InlineData(0, CombatSkillElement.Metal)]
    [InlineData(1, CombatSkillElement.Wood)]
    [InlineData(2, CombatSkillElement.Water)]
    [InlineData(3, CombatSkillElement.Fire)]
    [InlineData(4, CombatSkillElement.Earth)]
    [InlineData(5, CombatSkillElement.Mixed)]
    public void Combat_skill_elements_preserve_GameData_order(
        int source,
        CombatSkillElement expected)
    {
        var mapped = CombatSnapshotMapping.MapCombatSkillElement(source);

        Assert.True(mapped.IsAvailable);
        Assert.Equal(expected, mapped.Value);
    }

    [Fact]
    public void Inner_power_adjustments_map_all_five_elements()
    {
        var mapped = CombatSnapshotMapping.MapElementAdjustments(
            [30, -30, 0, 10, -10]);

        Assert.Equal(30, mapped.Metal);
        Assert.Equal(-30, mapped.Wood);
        Assert.Equal(0, mapped.Water);
        Assert.Equal(10, mapped.Fire);
        Assert.Equal(-10, mapped.Earth);
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

    [Fact]
    public void Infrastructure_registers_character_progress_reader_as_singleton()
    {
        ServiceCollection services = [];

        services.AddTaiwuInfrastructure();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType
                == typeof(ICharacterCombatSkillProgressReader));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.NotNull(descriptor.ImplementationFactory);
    }
}
