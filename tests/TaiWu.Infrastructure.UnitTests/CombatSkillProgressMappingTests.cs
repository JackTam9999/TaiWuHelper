using TaiWu.Application.CombatSkills;
using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Infrastructure.Catalogue;
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
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            Labels,
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
        Assert.Equal(15, progress.StudyDetails.Length);
        Assert.Equal(15, progress.StudySummary.ReadCount);
        Assert.Equal(15, progress.StudySummary.AvailableCount);
        Assert.True(progress.StudySummary.IsComplete.Value);
        Assert.Empty(progress.MissingStudyDetails);
        Assert.Equal("Realization", progress.StudyDetails[0].Label.Value);
        Assert.Equal("outline-2", progress.StudyDetails[0].DetailId);
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
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            Labels,
            warnings);

        Assert.False(progress.Proficiency.Current.IsAvailable);
        Assert.False(progress.ActiveDirection.IsAvailable);
        Assert.False(progress.Breakthrough.Value.IsBrokenOut);
        Assert.False(progress.Breakthrough.Value.CanBreakthroughNow);
        Assert.False(progress.Activated.Value);
        Assert.True(progress.Simplified.Value);
        Assert.False(progress.Equipped.Value);
        Assert.Equal(15, progress.MissingStudyDetails.Length);
        Assert.Equal(0, progress.StudySummary.ReadCount);
        Assert.Equal(15, progress.StudySummary.NotReadCount);
        Assert.False(progress.StudySummary.IsComplete.Value);
        Assert.Empty(warnings);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32768)]
    public void Invalid_values_become_unavailable_with_warnings(
        int unsupportedState)
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
                ReadingState: unsupportedState,
                ActivationState: unsupportedState,
                MeetsBreakthroughReadingRequirement: true,
                Simplified: false,
                Equipped: false),
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            Labels,
            warnings);

        Assert.False(progress.Proficiency.Current.IsAvailable);
        Assert.False(progress.Breakthrough.IsAvailable);
        Assert.False(progress.ActiveDirection.IsAvailable);
        Assert.False(progress.Activated.IsAvailable);
        Assert.Equal(15, progress.StudyDetails.Length);
        Assert.Equal(15, progress.StudySummary.UnavailableCount);
        Assert.Equal(0, progress.StudySummary.AvailableCount);
        Assert.False(progress.StudySummary.IsComplete.IsAvailable);
        Assert.Empty(progress.MissingStudyDetails);
        Assert.Equal(15, progress.UnavailableStudyDetails.Length);
        Assert.Contains(
            warnings,
            warning => warning.Code == "PROFICIENCY_OUT_OF_RANGE");
        Assert.Contains(
            warnings,
            warning => warning.Code == "BREAKTHROUGH_STATE_UNAVAILABLE");
        Assert.Contains(
            warnings,
            warning => warning.Code == "ACTIVATION_STATE_UNSUPPORTED");
        Assert.Contains(
            warnings,
            warning => warning.Code
                == "STUDY_DETAIL_READING_STATE_UNSUPPORTED");
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
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            Labels,
            warnings);

        Assert.True(progress.Breakthrough.Value.CanBreakthroughNow);
        Assert.Equal(
            [PracticeDirection.Direct],
            progress.Breakthrough.Value.AvailableDirections);
        Assert.False(progress.ActiveDirection.IsAvailable);
        Assert.True(progress.Activated.Value);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Unsupported_version_has_no_fabricated_detail_map()
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];

        var progress = CombatSkillProgressMapping.Map(
            characterId: 7,
            Snapshot,
            new RawCharacterCombatSkillProgress(
                SkillId: 100,
                Learned: true,
                Proficiency: 0,
                ReadingState: 32767,
                ActivationState: 1 | (31 << 5),
                MeetsBreakthroughReadingRequirement: true,
                Simplified: false,
                Equipped: false),
            gameDataVersion: "2.0.0-unsupported",
            Labels,
            warnings);

        Assert.Empty(progress.StudyDetails);
        Assert.False(progress.StudySummary.IsComplete.IsAvailable);
        Assert.False(progress.Breakthrough.IsAvailable);
        Assert.False(progress.Activated.IsAvailable);
        Assert.Contains(
            warnings,
            warning => warning.Code == "STUDY_DETAIL_VERSION_UNSUPPORTED");
    }

    [Fact]
    public void Invalid_activation_preserves_read_completeness()
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];

        var progress = CombatSkillProgressMapping.Map(
            characterId: 7,
            Snapshot,
            new RawCharacterCombatSkillProgress(
                SkillId: 100,
                Learned: true,
                Proficiency: 0,
                ReadingState: 32767,
                ActivationState: 32768,
                MeetsBreakthroughReadingRequirement: true,
                Simplified: false,
                Equipped: false),
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            Labels,
            warnings);

        Assert.True(progress.StudySummary.IsComplete.Value);
        Assert.Equal(15, progress.StudySummary.ReadCount);
        Assert.All(
            progress.StudyDetails,
            detail => Assert.False(detail.IsActive.IsAvailable));
        Assert.False(progress.Activated.IsAvailable);
        Assert.False(progress.Breakthrough.IsAvailable);
    }

    [Theory]
    [InlineData(996, PracticeDirection.Direct)]
    [InlineData(31745, PracticeDirection.Reverse)]
    public void Completed_direction_uses_the_same_decoded_detail_set(
        int activationState,
        PracticeDirection expectedDirection)
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];

        var progress = CombatSkillProgressMapping.Map(
            characterId: 7,
            Snapshot,
            new RawCharacterCombatSkillProgress(
                SkillId: 100,
                Learned: true,
                Proficiency: 0,
                ReadingState: 32767,
                ActivationState: activationState,
                MeetsBreakthroughReadingRequirement: true,
                Simplified: false,
                Equipped: false),
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            Labels,
            warnings);

        Assert.True(progress.Breakthrough.Value.IsBrokenOut);
        Assert.Equal(expectedDirection, progress.ActiveDirection.Value);
        Assert.True(progress.StudySummary.IsComplete.Value);
        Assert.Equal(
            6,
            progress.StudyDetails.Count(detail => detail.IsActive.Value));
        Assert.Empty(warnings);
    }

    [Fact]
    public void Missing_label_warnings_are_bounded_by_verified_detail_key()
    {
        List<CharacterCombatSkillProgressWarning> warnings = [];
        var labels = new CombatSkillStudyDetailLabelSet(
            CatalogueLanguage.English,
            CatalogueSourceKind.EnglishLanguageResource,
            "language-en:test",
            new TaiwuLanguageCatalog(),
            unavailableReason: null);
        var raw = new RawCharacterCombatSkillProgress(
            SkillId: 100,
            Learned: true,
            Proficiency: 0,
            ReadingState: 0,
            ActivationState: 0,
            MeetsBreakthroughReadingRequirement: false,
            Simplified: false,
            Equipped: false);

        _ = CombatSkillProgressMapping.Map(
            7,
            Snapshot,
            raw,
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            labels,
            warnings);
        _ = CombatSkillProgressMapping.Map(
            7,
            Snapshot,
            raw with { SkillId = 101 },
            CombatSkillStudyDetailDecoder.SupportedGameDataVersion,
            labels,
            warnings);

        Assert.Equal(
            15,
            warnings.Count(warning =>
                warning.Code == "STUDY_DETAIL_LABEL_UNAVAILABLE"));
    }

    private static CombatSkillStudyDetailLabelSet Labels { get; } =
        CreateLabels();

    private static CombatSkillStudyDetailLabelSet CreateLabels()
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        var outline = new[]
        {
            "Resilience", "Unity", "Realization", "Peculiar", "Unique"
        };
        var direct = new[]
        {
            "Might", "Aptitude", "Beginnings", "Integrity", "Possession"
        };
        var reverse = new[]
        {
            "Efficiency", "Eccentricity", "Authenticity", "Persistence",
            "Supreme"
        };
        for (var index = 0; index < 5; index++)
        {
            values[$"LK_CombatSkill_First_Page_Type_{index}"] =
                outline[index];
            values[$"LK_CombatSkill_Direct_Page_{index}"] = direct[index];
            values[$"LK_CombatSkill_Reverse_Page_{index}"] = reverse[index];
        }

        return new CombatSkillStudyDetailLabelSet(
            CatalogueLanguage.English,
            CatalogueSourceKind.EnglishLanguageResource,
            "language-en:test",
            new TaiwuLanguageCatalog(values),
            unavailableReason: null);
    }

    private static SaveSnapshotIdentity Snapshot { get; } = new(
        new string('A', 64),
        DateTimeOffset.Parse("2026-08-02T12:00:00Z"));
}
