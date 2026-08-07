using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSkills;

public sealed class CharacterCombatSkillProgressTests
{
    private const string GoldenHash =
        "C9EB00A368A6CE25B2D816DAE941AFAC67B6217ED561FF7563F613C3B297CECA";

    [Fact]
    public void Identity_uses_character_snapshot_and_skill()
    {
        var first = CreateProgress();
        var same = CreateProgress(
            power: Power(140, 120));
        var otherSkill = CreateProgress(skillId: 498);
        var otherCharacter = CreateProgress(characterId: 9);
        var otherSnapshot = CreateProgress(
            snapshot: new SaveSnapshotIdentity(
                GoldenHash,
                new DateTimeOffset(2026, 8, 2, 13, 0, 0, TimeSpan.Zero)));

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, otherSkill);
        Assert.NotEqual(first, otherCharacter);
        Assert.NotEqual(first, otherSnapshot);
    }

    [Fact]
    public void Learned_uses_verified_terminology_and_is_independent()
    {
        var progress = CreateProgress(
            learned: Available(true, "learned"),
            equipped: Available(false, "equipped"));

        Assert.True(progress.Learned.Value);
        Assert.False(progress.Equipped.Value);
        Assert.DoesNotContain(
            typeof(CharacterCombatSkillProgress).GetProperties(),
            property => property.Name.Contains(
                "Obtained",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_proficiency_remains_unavailable()
    {
        var proficiency = new CombatSkillProficiencyProgress(
            SkillProgressField<int>.Unavailable(
                "The save contains no proficiency key."),
            Available(CombatSkillProficiencyProgress.MaximumSupportedValue, "max"));
        var progress = CreateProgress(proficiency: proficiency);

        Assert.Equal(
            SkillProgressFieldStatus.Unavailable,
            progress.Proficiency.Current.Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000000000)]
    public void Invalid_current_proficiency_is_rejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillProficiencyProgress(
                Available(value, "current"),
                Available(
                    CombatSkillProficiencyProgress.MaximumSupportedValue,
                    "maximum")));
    }

    [Fact]
    public void Runtime_power_can_exceed_its_requirements_cap()
    {
        var progress = CreateProgress(power: Power(140, 120));

        Assert.Equal(140, progress.Power.Current.Value);
        Assert.Equal(120, progress.Power.Maximum.Value);
        Assert.Equal(CombatSkillPowerContext.OutOfCombat, progress.Power.Context);
    }

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(100, 0)]
    public void Invalid_runtime_power_is_rejected(int current, int maximum)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Power(current, maximum));
    }

    [Fact]
    public void Available_attainment_must_match_current_breakthrough()
    {
        Assert.Throws<ArgumentException>(() => CreateProgress(
            attainmentMastered: Available(true, "attainment-mastered")));
    }

    [Fact]
    public void Known_missing_detail_proves_incomplete_without_counting_unknown()
    {
        var progress = CreateProgress(
            details:
            [
                Detail("outline-0", 0, CombatSkillStudyState.Read),
                Detail("direct-0", 1, CombatSkillStudyState.NotRead),
                DetailUnavailable("reverse-0", 2)
            ],
            activated: SkillProgressField<bool>.Unavailable(
                "Activation is partial."));

        Assert.Equal(3, progress.StudySummary.TotalCount);
        Assert.Equal(2, progress.StudySummary.AvailableCount);
        Assert.Equal(1, progress.StudySummary.ReadCount);
        Assert.Equal(1, progress.StudySummary.NotReadCount);
        Assert.Equal(1, progress.StudySummary.UnavailableCount);
        Assert.True(progress.StudySummary.IsComplete.IsAvailable);
        Assert.False(progress.StudySummary.IsComplete.Value);
        Assert.Equal(
            ["direct-0"],
            progress.MissingStudyDetails.Select(detail => detail.DetailId));
        Assert.Equal(
            ["reverse-0"],
            progress.UnavailableStudyDetails.Select(detail => detail.DetailId));
    }

    [Fact]
    public void Unknown_detail_does_not_become_incomplete()
    {
        var progress = CreateProgress(
            details:
            [
                Detail("outline-0", 0, CombatSkillStudyState.Read),
                DetailUnavailable("direct-0", 1)
            ],
            activated: SkillProgressField<bool>.Unavailable(
                "Activation is partial."));

        Assert.Equal(0, progress.StudySummary.NotReadCount);
        Assert.Equal(1, progress.StudySummary.AvailableCount);
        Assert.Equal(1, progress.StudySummary.UnavailableCount);
        Assert.False(progress.StudySummary.IsComplete.IsAvailable);
        Assert.Contains(
            "unavailable or conflicting",
            progress.StudySummary.IsComplete.Reason);
    }

    [Fact]
    public void All_known_read_details_are_complete()
    {
        var progress = CreateProgress(
            details:
            [
                Detail("outline-0", 0, CombatSkillStudyState.Read),
                Detail("direct-0", 1, CombatSkillStudyState.Read)
            ]);

        Assert.True(progress.StudySummary.IsComplete.Value);
        Assert.Equal(2, progress.StudySummary.ReadCount);
        Assert.Equal(0, progress.StudySummary.UnavailableCount);
    }

    [Fact]
    public void No_details_has_unavailable_completeness()
    {
        var progress = CreateProgress(details: []);

        Assert.False(progress.StudySummary.IsComplete.IsAvailable);
        Assert.Contains("No study details", progress.StudySummary.IsComplete.Reason);
    }

    [Fact]
    public void Duplicate_detail_id_or_order_is_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => CreateProgress(
                details:
                [
                    Detail("direct-0", 0, CombatSkillStudyState.Read),
                    Detail("direct-0", 1, CombatSkillStudyState.Read)
                ]));
        Assert.Throws<ArgumentException>(
            () => CreateProgress(
                details:
                [
                    Detail("direct-0", 0, CombatSkillStudyState.Read),
                    Detail("direct-1", 0, CombatSkillStudyState.Read)
                ]));
    }

    [Fact]
    public void Detail_order_is_normalized_and_input_list_is_copied()
    {
        List<CombatSkillStudyDetailProgress> details =
        [
            Detail("direct-1", 2, CombatSkillStudyState.Read),
            Detail("outline-0", 0, CombatSkillStudyState.Read)
        ];
        var progress = CreateProgress(details: details);

        details.Clear();

        Assert.Equal(
            ["outline-0", "direct-1"],
            progress.StudyDetails.Select(detail => detail.DetailId));
    }

    [Fact]
    public void Active_direction_requires_completed_breakthrough()
    {
        var notCompleted = Available(
            new BreakthroughDirectionAvailability(
                isBrokenOut: false,
                canBreakthroughNow: true,
                [PracticeDirection.Direct]),
            "breakthrough");

        Assert.Throws<ArgumentException>(
            () => CreateProgress(
                breakthrough: notCompleted,
                activeDirection: Available(
                    PracticeDirection.Direct,
                    "direction")));
    }

    [Fact]
    public void Completed_breakthrough_reuses_verified_direction_model()
    {
        var completed = Available(
            new BreakthroughDirectionAvailability(
                isBrokenOut: true,
                canBreakthroughNow: false,
                []),
            "breakthrough");
        var progress = CreateProgress(
            breakthrough: completed,
            activeDirection: Available(
                PracticeDirection.Reverse,
                "direction"));

        Assert.True(progress.Breakthrough.Value.IsBrokenOut);
        Assert.Equal(
            PracticeDirection.Reverse,
            progress.ActiveDirection.Value);
    }

    [Fact]
    public void Unknown_direction_remains_representable_after_breakthrough()
    {
        var completed = Available(
            new BreakthroughDirectionAvailability(
                isBrokenOut: true,
                canBreakthroughNow: false,
                []),
            "breakthrough");
        var progress = CreateProgress(
            breakthrough: completed,
            activeDirection: SkillProgressField<PracticeDirection>.Unavailable(
                "Activation direction is unsupported."));

        Assert.False(progress.ActiveDirection.IsAvailable);
    }

    [Fact]
    public void Mastery_simplification_activation_and_equipment_are_independent()
    {
        var progress = CreateProgress(
            breakthrough: Available(
                new BreakthroughDirectionAvailability(
                    isBrokenOut: true,
                    canBreakthroughNow: false,
                    []),
                "breakthrough"),
            attainmentMastered: Available(true, "attainment-mastered"),
            simplified: Available(false, "simplified"),
            activated: Available(true, "activated"),
            equipped: Available(false, "equipped"));

        Assert.True(progress.AttainmentMastered.Value);
        Assert.False(progress.Simplified.Value);
        Assert.True(progress.Activated.Value);
        Assert.False(progress.Equipped.Value);
    }

    [Fact]
    public void Aggregate_activation_must_match_available_detail_states()
    {
        Assert.Throws<ArgumentException>(
            () => CreateProgress(
                details:
                [
                    Detail(
                        "direct-0",
                        0,
                        CombatSkillStudyState.Read,
                        isActive: false)
                ],
                activated: Available(true, "activated")));
    }

    [Fact]
    public void Conflicting_observations_preserve_both_sources()
    {
        var conflict = SkillProgressField<bool>.Conflicting(
            "Save and newer screen disagree.",
            [
                new SkillProgressObservation<bool>(
                    false,
                    SaveSource("attainment-mastered")),
                new SkillProgressObservation<bool>(
                    true,
                    ScreenSource("attainment-mastered"))
            ]);
        var progress = CreateProgress(attainmentMastered: conflict);

        Assert.Equal(
            SkillProgressFieldStatus.Conflicting,
            progress.AttainmentMastered.Status);
        Assert.Equal(2, progress.AttainmentMastered.Observations.Length);
        Assert.Throws<InvalidOperationException>(
            () => progress.AttainmentMastered.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("Z9EB00A368A6CE25B2D816DAE941AFAC67B6217ED561FF7563F613C3B297CECA")]
    public void Invalid_snapshot_hash_is_rejected(string hash)
    {
        Assert.Throws<ArgumentException>(
            () => new SaveSnapshotIdentity(hash, DateTimeOffset.UtcNow));
    }

    private static CharacterCombatSkillProgress CreateProgress(
        int characterId = 21396,
        SaveSnapshotIdentity? snapshot = null,
        int skillId = 456,
        SkillProgressField<bool>? learned = null,
        CombatSkillProficiencyProgress? proficiency = null,
        CombatSkillPowerProgress? power = null,
        IEnumerable<CombatSkillStudyDetailProgress>? details = null,
        SkillProgressField<BreakthroughDirectionAvailability>? breakthrough = null,
        SkillProgressField<PracticeDirection>? activeDirection = null,
        SkillProgressField<bool>? attainmentMastered = null,
        SkillProgressField<bool>? simplified = null,
        SkillProgressField<bool>? activated = null,
        SkillProgressField<bool>? equipped = null)
    {
        var actualDetails = details ??
        [
            Detail(
                "reverse-0",
                0,
                CombatSkillStudyState.Read,
                isActive: true)
        ];
        return new CharacterCombatSkillProgress(
            characterId,
            snapshot ?? new SaveSnapshotIdentity(
                GoldenHash,
                new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero)),
            skillId,
            learned ?? Available(true, "learned"),
            proficiency ?? new CombatSkillProficiencyProgress(
                SkillProgressField<int>.Unavailable(
                    "No persisted proficiency key."),
                Available(
                    CombatSkillProficiencyProgress.MaximumSupportedValue,
                    "maximum")),
            power ?? Power(113, 100),
            actualDetails,
            breakthrough ?? Available(
                new BreakthroughDirectionAvailability(
                    isBrokenOut: false,
                    canBreakthroughNow: true,
                    [PracticeDirection.Direct, PracticeDirection.Reverse]),
                "breakthrough"),
            activeDirection ?? SkillProgressField<PracticeDirection>.Unavailable(
                "No direction is active before breakthrough."),
            attainmentMastered ?? SkillProgressField<bool>.Unavailable(
                "The attainment mastery rule is unavailable."),
            simplified ?? Available(false, "simplified"),
            activated ?? Available(true, "activated"),
            equipped ?? Available(false, "equipped"));
    }

    private static CombatSkillStudyDetailProgress Detail(
        string id,
        int order,
        CombatSkillStudyState state,
        bool isActive = true)
    {
        return new CombatSkillStudyDetailProgress(
            id,
            order,
            id.StartsWith("outline", StringComparison.Ordinal)
                ? CombatSkillStudyDetailGroup.Outline
                : id.StartsWith("direct", StringComparison.Ordinal)
                    ? CombatSkillStudyDetailGroup.Direct
                    : CombatSkillStudyDetailGroup.Reverse,
            Label(id),
            Available(state, $"{id}:read"),
            Available(isActive, $"{id}:active"));
    }

    private static CombatSkillStudyDetailProgress DetailUnavailable(
        string id,
        int order)
    {
        return new CombatSkillStudyDetailProgress(
            id,
            order,
            CombatSkillStudyDetailGroup.Reverse,
            Label(id),
            SkillProgressField<CombatSkillStudyState>.Unavailable(
                "Reading state is unavailable."),
            SkillProgressField<bool>.Unavailable(
                "Activation state is unavailable."));
    }

    private static CatalogueField<string> Label(string id) =>
        CatalogueField<string>.Available(
            id,
            new CatalogueSourceReference(
                CatalogueSourceKind.EnglishLanguageResource,
                "language-en:f89c3b8a",
                $"detail-label:{id}"));

    private static SkillProgressField<T> Available<T>(T value, string field) =>
        SkillProgressField<T>.Available(value, SaveSource(field));

    private static CombatSkillPowerProgress Power(int current, int maximum) =>
        new(
            Available(current, "power"),
            Available(maximum, "maximum-power"),
            CombatSkillPowerContext.OutOfCombat);

    private static SkillProgressSource SaveSource(string field) =>
        new(
            SkillProgressSourceKind.SaveSnapshot,
            $"save:{GoldenHash}",
            field);

    private static SkillProgressSource ScreenSource(string field) =>
        new(
            SkillProgressSourceKind.CurrentScreenObservation,
            "screen:e2-001-character-skill-list",
            field);
}
