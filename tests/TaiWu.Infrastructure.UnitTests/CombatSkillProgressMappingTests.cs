using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure.SaveGames;
using Xunit;

namespace TaiWu.Infrastructure.UnitTests;

public sealed class CombatSkillProgressMappingTests
{
    [Fact]
    public void Maps_independent_verified_progress_facts()
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];

        var progress = CombatSkillProgressMapping.Map(
            characterId: 7,
            Snapshot,
            new RawCharacterCombatSkillProgress(
                SkillId: 40,
                Learned: true,
                Proficiency: 125,
                ReadingState: 32767,
                ActivationState: 14881,
                MeetsBreakthroughReadingRequirement: true,
                Simplified: false,
                Equipped: true),
            warnings);

        Assert.True(progress.Learned.Value);
        Assert.Equal(125, progress.Proficiency.Current.Value);
        Assert.Equal(
            CombatSkillProficiencyProgress.MaximumSupportedValue,
            progress.Proficiency.Maximum.Value);
        Assert.False(progress.Proficiency.Percentage.IsAvailable);
        Assert.True(progress.Breakthrough.Value.IsBrokenOut);
        Assert.False(progress.Breakthrough.Value.CanBreakthroughNow);
        Assert.Equal(PracticeDirection.Reverse, progress.ActiveDirection.Value);
        Assert.False(progress.AttainmentMastered.IsAvailable);
        Assert.False(progress.Simplified.Value);
        Assert.True(progress.Activated.Value);
        Assert.True(progress.Equipped.Value);
        Assert.Empty(progress.StudyDetails);
        Assert.False(progress.StudySummary.IsComplete.IsAvailable);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Missing_proficiency_and_unbroken_direction_are_explicit()
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];

        var progress = CombatSkillProgressMapping.Map(
            characterId: 7,
            Snapshot,
            new RawCharacterCombatSkillProgress(
                SkillId: 498,
                Learned: true,
                Proficiency: null,
                ReadingState: 0,
                ActivationState: 0,
                MeetsBreakthroughReadingRequirement: false,
                Simplified: true,
                Equipped: false),
            warnings);

        Assert.False(progress.Proficiency.Current.IsAvailable);
        Assert.False(progress.ActiveDirection.IsAvailable);
        Assert.False(progress.Breakthrough.Value.IsBrokenOut);
        Assert.False(progress.Breakthrough.Value.CanBreakthroughNow);
        Assert.False(progress.Activated.Value);
        Assert.True(progress.Simplified.Value);
        Assert.False(progress.Equipped.Value);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Invalid_values_become_unavailable_with_warnings()
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];

        var progress = CombatSkillProgressMapping.Map(
            characterId: 7,
            Snapshot,
            new RawCharacterCombatSkillProgress(
                SkillId: 100,
                Learned: true,
                Proficiency:
                    CombatSkillProficiencyProgress.MaximumSupportedValue + 1,
                ReadingState: 32768,
                ActivationState: 32768,
                MeetsBreakthroughReadingRequirement: true,
                Simplified: false,
                Equipped: false),
            warnings);

        Assert.False(progress.Proficiency.Current.IsAvailable);
        Assert.False(progress.Breakthrough.IsAvailable);
        Assert.False(progress.ActiveDirection.IsAvailable);
        Assert.False(progress.Activated.IsAvailable);
        Assert.Contains(
            warnings,
            warning => warning.Code == "PROFICIENCY_OUT_OF_RANGE");
        Assert.Contains(
            warnings,
            warning => warning.Code == "BREAKTHROUGH_STATE_UNAVAILABLE");
        Assert.Contains(
            warnings,
            warning => warning.Code == "ACTIVATION_STATE_UNSUPPORTED");
    }

    [Fact]
    public void Immediate_breakthrough_preserves_only_verified_direction()
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];

        var progress = CombatSkillProgressMapping.Map(
            characterId: 7,
            Snapshot,
            new RawCharacterCombatSkillProgress(
                SkillId: 686,
                Learned: true,
                Proficiency: 0,
                ReadingState: 9928,
                ActivationState: 9920,
                MeetsBreakthroughReadingRequirement: true,
                Simplified: false,
                Equipped: false),
            warnings);

        Assert.True(progress.Breakthrough.Value.CanBreakthroughNow);
        Assert.Equal(
            [PracticeDirection.Direct],
            progress.Breakthrough.Value.AvailableDirections);
        Assert.False(progress.ActiveDirection.IsAvailable);
        Assert.True(progress.Activated.Value);
        Assert.Empty(warnings);
    }

    private static SaveSnapshotIdentity Snapshot { get; } = new(
        new string('A', 64),
        DateTimeOffset.Parse("2026-08-02T12:00:00Z"));
}
