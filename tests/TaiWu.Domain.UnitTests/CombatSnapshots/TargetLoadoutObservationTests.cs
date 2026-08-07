using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class TargetLoadoutObservationTests
{
    private static readonly DateTimeOffset ObservedAt = new(
        2026,
        8,
        7,
        21,
        30,
        0,
        TimeSpan.FromHours(1));

    [Fact]
    public void Observation_normalizes_values_and_copies_skills()
    {
        List<ObservedTargetCombatSkill> source =
        [
            new(
                100,
                SkillCategory.Attack,
                PracticeDirection.Direct,
                slotIndex: 0)
        ];

        var observation = CreateObservation(
            TargetLoadoutCoverage.PartialLoadout,
            source,
            evidenceReference: "  E3-000-CAP-002  ");
        source.Clear();

        Assert.Equal(16317, observation.TargetCharacterId);
        Assert.Equal(
            TargetObservationContext.Sparring,
            observation.ObservationContext);
        Assert.Equal(TimeSpan.Zero, observation.ObservedAtUtc.Offset);
        Assert.Equal(ObservedAt.UtcDateTime, observation.ObservedAtUtc);
        Assert.Equal("E3-000-CAP-002", observation.EvidenceReference);
        Assert.Single(observation.ObservedSkills);
        Assert.Equal(100, observation.ObservedSkills[0].SkillId);
    }

    [Fact]
    public void Equivalent_observations_have_value_equality()
    {
        var first = CreateObservation(
            TargetLoadoutCoverage.PartialLoadout,
            [new ObservedTargetCombatSkill(0, SkillCategory.Neigong)]);
        var second = CreateObservation(
            TargetLoadoutCoverage.PartialLoadout,
            [new ObservedTargetCombatSkill(0, SkillCategory.Neigong)]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(
            first,
            CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [new ObservedTargetCombatSkill(1, SkillCategory.Neigong)]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(PracticeDirection.Direct)]
    [InlineData(PracticeDirection.Reverse)]
    public void Direction_is_optional_and_limited_to_visible_values(
        PracticeDirection? direction)
    {
        var skill = new ObservedTargetCombatSkill(
            100,
            SkillCategory.Attack,
            direction);

        Assert.Equal(direction, skill.Direction);
    }

    [Theory]
    [InlineData(PracticeDirection.Neutral)]
    [InlineData((PracticeDirection)99)]
    public void Unsupported_direction_is_rejected(
        PracticeDirection direction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ObservedTargetCombatSkill(
                100,
                SkillCategory.Attack,
                direction));
    }

    [Fact]
    public void Invalid_skill_identity_category_and_slot_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ObservedTargetCombatSkill(
                -1,
                SkillCategory.Attack));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ObservedTargetCombatSkill(
                1,
                (SkillCategory)99));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ObservedTargetCombatSkill(
                1,
                SkillCategory.Attack,
                slotIndex: -1));
    }

    [Fact]
    public void Duplicate_skills_are_rejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [
                    new ObservedTargetCombatSkill(
                        100,
                        SkillCategory.Attack),
                    new ObservedTargetCombatSkill(
                        100,
                        SkillCategory.Defense)
                ]));

        Assert.Contains("100", exception.Message);
    }

    [Fact]
    public void Duplicate_category_relative_slots_are_rejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [
                    new ObservedTargetCombatSkill(
                        100,
                        SkillCategory.Attack,
                        slotIndex: 0),
                    new ObservedTargetCombatSkill(
                        101,
                        SkillCategory.Attack,
                        slotIndex: 0)
                ]));

        Assert.Contains("Attack:0", exception.Message);
    }

    [Fact]
    public void Same_slot_index_in_different_categories_is_valid()
    {
        var observation = CreateObservation(
            TargetLoadoutCoverage.PartialLoadout,
            [
                new ObservedTargetCombatSkill(
                    100,
                    SkillCategory.Attack,
                    slotIndex: 0),
                new ObservedTargetCombatSkill(
                    101,
                    SkillCategory.Defense,
                    slotIndex: 0)
            ]);

        Assert.Equal(2, observation.ObservedSkills.Length);
    }

    [Fact]
    public void Partial_coverage_cannot_establish_absence()
    {
        var observation = CreateObservation(
            TargetLoadoutCoverage.PartialLoadout,
            [new ObservedTargetCombatSkill(100, SkillCategory.Attack)]);

        Assert.Equal(
            TargetLoadoutCoverageKind.PartialLoadout,
            observation.Coverage.Kind);
        Assert.False(observation.Coverage.CanEstablishAbsence);
        Assert.False(observation.EstablishesAbsenceOf(100));
        Assert.False(observation.EstablishesAbsenceOf(101));
    }

    [Fact]
    public void Complete_coverage_can_establish_omitted_skill_absence()
    {
        var completeness = TargetLoadoutCompletenessEvidence.FromE3000(
            TargetLoadoutCompletenessEvidence.E3000GameDataVersion);
        var coverage = TargetLoadoutCoverage.CompleteCurrentLoadout(
            completeness);
        var observation = CreateObservation(
            coverage,
            [new ObservedTargetCombatSkill(100, SkillCategory.Attack)]);

        Assert.Equal(
            TargetLoadoutCoverageKind.CompleteCurrentLoadout,
            observation.Coverage.Kind);
        Assert.True(observation.Coverage.CanEstablishAbsence);
        Assert.False(observation.EstablishesAbsenceOf(100));
        Assert.True(observation.EstablishesAbsenceOf(101));
        Assert.Equal(
            TargetObservationContext.Sparring,
            completeness.ObservationContext);
        Assert.Equal(
            TargetLoadoutCompletenessEvidence.E3000RuleId,
            completeness.RuleId);
        Assert.Equal(
            TargetLoadoutCompletenessEvidence.E3000EvidenceReference,
            completeness.EvidenceReference);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1.0.0+different")]
    public void Complete_coverage_rejects_unsupported_detected_version(
        string version)
    {
        Assert.Throws<ArgumentException>(
            () => TargetLoadoutCompletenessEvidence.FromE3000(version));
    }

    [Fact]
    public void Complete_coverage_requires_typed_e3000_evidence()
    {
        Assert.Empty(
            typeof(TargetLoadoutCompletenessEvidence).GetConstructors());
        Assert.Throws<ArgumentNullException>(
            () => TargetLoadoutCoverage.CompleteCurrentLoadout(null!));
    }

    [Theory]
    [InlineData(TargetObservationContext.Hostile)]
    [InlineData(TargetObservationContext.Story)]
    public void Hostile_and_story_contexts_cannot_create_observations(
        TargetObservationContext context)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [],
                observationContext: context));

        Assert.Contains("unavailable", exception.Message);
    }

    [Fact]
    public void Invalid_observation_values_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [],
                targetCharacterId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [],
                observationContext: (TargetObservationContext)99));
        Assert.Throws<ArgumentException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [],
                evidenceReference: " "));
        Assert.Throws<ArgumentNullException>(
            () => CreateObservation(null!, []));
        Assert.Throws<ArgumentNullException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                null!));
        Assert.Throws<ArgumentException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                [null!]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateObservation(
                TargetLoadoutCoverage.PartialLoadout,
                []).EstablishesAbsenceOf(-1));
    }

    private static TargetLoadoutObservation CreateObservation(
        TargetLoadoutCoverage coverage,
        IEnumerable<ObservedTargetCombatSkill> observedSkills,
        int targetCharacterId = 16317,
        TargetObservationContext observationContext =
            TargetObservationContext.Sparring,
        string evidenceReference = "E3-000-CAP-002") => new(
            targetCharacterId,
            observationContext,
            ObservedAt,
            evidenceReference,
            coverage,
            observedSkills);
}
