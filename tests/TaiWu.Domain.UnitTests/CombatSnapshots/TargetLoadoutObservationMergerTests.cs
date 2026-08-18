using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class TargetLoadoutObservationMergerTests
{
    private static readonly DateTimeOffset SaveTime = new(
        2026,
        8,
        7,
        20,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void Observation_target_must_match_snapshot_target()
    {
        var snapshot = CreateSnapshot([Skill(100, SkillCategory.Attack)]);
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [Observed(100, SkillCategory.Attack)],
            targetId: 999);

        Assert.Throws<ArgumentException>(
            () => TargetLoadoutObservationMerger.Merge(
                snapshot,
                observation));
    }

    [Fact]
    public void Fresh_complete_observation_replaces_membership_immutably()
    {
        var savedSkill = Skill(
            100,
            SkillCategory.Attack,
            PracticeDirection.Direct);
        var resolvedSkill = Skill(
            101,
            SkillCategory.Defense,
            direction: null);
        var snapshot = CreateSnapshot(
            [savedSkill],
            equipped: Loadout((SkillCategory.Attack, 100)),
            warnings:
            [
                new SnapshotWarning(
                    CombatSnapshotWarningCodes.TargetLoadoutNotPersisted,
                    "Target loadout was not persisted.")
            ]);
        var observation = Observation(
            CompleteCoverage(),
            [
                Observed(
                    101,
                    SkillCategory.Defense,
                    PracticeDirection.Reverse,
                    slotIndex: 0)
            ]);

        var result = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation,
            [resolvedSkill]);

        Assert.Equal(TargetLoadoutMergeStatus.Applied, result.Status);
        Assert.Equal(
            [101],
            result.Snapshot.Target.EquippedSkills.Value.DefenseSkillIds);
        Assert.Empty(
            result.Snapshot.Target.EquippedSkills.Value.AttackSkillIds);
        Assert.Equal(
            [100, 101],
            result.Snapshot.Target.LearnedSkills.Select(skill => skill.SkillId));
        Assert.Equal(
            PracticeDirection.Reverse,
            result.Snapshot.Target.LearnedSkills
                .Single(skill => skill.SkillId == 101)
                .Direction.Value);
        Assert.Same(observation, result.Snapshot.Target.LoadoutObservation);
        Assert.DoesNotContain(
            result.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes.TargetLoadoutNotPersisted);
        Assert.Contains(
            result.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes.TargetObservationSaveConflict);
        Assert.Equal(
            SnapshotEvidenceStatus.Conflicting,
            result.LoadoutEvidence.Status);
        Assert.Equal(2, result.LoadoutEvidence.Observations.Length);

        Assert.Equal(
            [100],
            snapshot.Target.EquippedSkills.Value.AttackSkillIds);
        Assert.Single(snapshot.Target.LearnedSkills);
        Assert.Null(snapshot.Target.LoadoutObservation);
        Assert.Single(observation.ObservedSkills);
    }

    [Fact]
    public void Partial_observation_unions_with_available_saved_membership()
    {
        var snapshot = CreateSnapshot(
            [
                Skill(100, SkillCategory.Attack),
                Skill(101, SkillCategory.Attack)
            ],
            equipped: Loadout((SkillCategory.Attack, 100)));
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [Observed(101, SkillCategory.Attack)]);

        var result = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Equal(
            [100, 101],
            result.Snapshot.Target.EquippedSkills.Value.AttackSkillIds);
        Assert.Contains(
            result.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes.TargetObservationPartial);
        Assert.False(observation.EstablishesAbsenceOf(102));
        Assert.Equal(
            SnapshotEvidenceStatus.Available,
            result.LoadoutEvidence.Status);
    }

    [Fact]
    public void Partial_observation_does_not_turn_unknown_loadout_into_complete()
    {
        var snapshot = CreateSnapshot(
            [],
            equipped: null,
            warnings:
            [
                new SnapshotWarning(
                    CombatSnapshotWarningCodes.TargetLoadoutNotPersisted,
                    "Target loadout was not persisted.")
            ]);
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [Observed(101, SkillCategory.Defense)]);
        var resolved = Skill(101, SkillCategory.Defense);

        var result = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation,
            [resolved]);

        Assert.False(result.Snapshot.Target.EquippedSkills.IsAvailable);
        Assert.Same(observation, result.Snapshot.Target.LoadoutObservation);
        Assert.Contains(
            result.Snapshot.Target.LearnedSkills,
            skill => skill.SkillId == 101);
        Assert.Contains(
            result.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes.TargetLoadoutNotPersisted);
    }

    [Fact]
    public void Stale_observation_is_retained_but_not_applied()
    {
        var snapshot = CreateSnapshot(
            [Skill(100, SkillCategory.Attack)],
            equipped: Loadout((SkillCategory.Attack, 100)));
        var observation = Observation(
            CompleteCoverage(),
            [Observed(101, SkillCategory.Defense)],
            observedAt: SaveTime);

        var result = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Equal(TargetLoadoutMergeStatus.Stale, result.Status);
        Assert.Same(snapshot.Target, result.Snapshot.Target);
        Assert.Equal(
            SnapshotEvidenceStatus.Stale,
            result.LoadoutEvidence.Status);
        Assert.Equal(2, result.LoadoutEvidence.Observations.Length);
        Assert.Contains(
            result.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes.TargetObservationNotNewer);
    }

    [Fact]
    public void Missing_save_time_requires_explicit_precedence_confirmation()
    {
        var snapshot = CreateSnapshot(
            [Skill(100, SkillCategory.Attack)],
            equipped: Loadout((SkillCategory.Attack, 100)),
            saveTimeAvailable: false);
        var observation = Observation(
            CompleteCoverage(),
            [Observed(100, SkillCategory.Attack)]);

        var pending = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);
        var applied = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation,
            confirmPrecedenceWhenSaveTimeUnavailable: true);

        Assert.Equal(
            TargetLoadoutMergeStatus.PrecedenceConfirmationRequired,
            pending.Status);
        Assert.Same(snapshot.Target, pending.Snapshot.Target);
        Assert.Equal(TargetLoadoutMergeStatus.Applied, applied.Status);
        Assert.Contains(
            applied.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes
                    .TargetObservationSaveTimeUnavailable);
    }

    [Fact]
    public void Unsupported_version_does_not_apply_observation()
    {
        var snapshot = CreateSnapshot(
            [Skill(100, SkillCategory.Attack)],
            equipped: Loadout((SkillCategory.Attack, 100)),
            gameDataVersion: "1.0.0+different");
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [Observed(100, SkillCategory.Attack)]);

        var result = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Equal(
            TargetLoadoutMergeStatus.UnsupportedVersion,
            result.Status);
        Assert.Same(snapshot.Target, result.Snapshot.Target);
        Assert.Equal(
            SnapshotEvidenceStatus.Unavailable,
            result.LoadoutEvidence.Status);
        Assert.Contains(
            result.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes
                    .TargetObservationUnsupportedVersion);
    }

    [Fact]
    public void Observed_direction_overrides_no_unrelated_skill_field()
    {
        var saved = Skill(
            100,
            SkillCategory.Attack,
            PracticeDirection.Direct);
        var snapshot = CreateSnapshot(
            [saved],
            equipped: Loadout((SkillCategory.Attack, 100)));
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [
                Observed(
                    100,
                    SkillCategory.Attack,
                    PracticeDirection.Reverse)
            ]);

        var result = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);
        var merged = Assert.Single(result.Snapshot.Target.LearnedSkills);
        var evidence = Assert.Single(result.DirectionEvidence);

        Assert.Equal(PracticeDirection.Reverse, merged.Direction.Value);
        Assert.Same(saved.DisplayName, merged.DisplayName);
        Assert.Same(saved.GridCost, merged.GridCost);
        Assert.Same(saved.Mastered, merged.Mastered);
        Assert.Same(saved.SlotContribution, merged.SlotContribution);
        Assert.Same(saved.DirectEffectId, merged.DirectEffectId);
        Assert.Same(saved.ReverseEffectId, merged.ReverseEffectId);
        Assert.Equal(
            SnapshotEvidenceStatus.Conflicting,
            evidence.Evidence.Status);
        Assert.Contains(
            result.Snapshot.Warnings,
            warning => warning.Code
                == CombatSnapshotWarningCodes.TargetObservationSaveConflict);
        Assert.Equal(
            [PracticeDirection.Direct, PracticeDirection.Reverse],
            evidence.Evidence.Observations.Select(value => value.Value));
        Assert.Equal(PracticeDirection.Direct, saved.Direction.Value);
    }

    [Fact]
    public void Missing_or_invalid_resolved_static_facts_are_rejected()
    {
        var snapshot = CreateSnapshot([], equipped: null);
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [Observed(101, SkillCategory.Defense)]);

        Assert.Throws<ArgumentException>(
            () => TargetLoadoutObservationMerger.Merge(
                snapshot,
                observation));
        Assert.Throws<ArgumentException>(
            () => TargetLoadoutObservationMerger.Merge(
                snapshot,
                observation,
                [Skill(101, SkillCategory.Attack)]));
        Assert.Throws<ArgumentException>(
            () => TargetLoadoutObservationMerger.Merge(
                snapshot,
                observation,
                [
                    Skill(101, SkillCategory.Defense),
                    Skill(999, SkillCategory.Attack)
                ]));
    }

    [Fact]
    public void Identical_inputs_produce_deterministic_ordering_and_warnings()
    {
        var snapshot = CreateSnapshot(
            [
                Skill(100, SkillCategory.Attack),
                Skill(102, SkillCategory.Defense)
            ],
            equipped: Loadout((SkillCategory.Attack, 100)));
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [
                Observed(102, SkillCategory.Defense, slotIndex: 2),
                Observed(100, SkillCategory.Attack, slotIndex: 1)
            ]);

        var first = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);
        var second = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Equal(
            first.Snapshot.Target.LearnedSkills.Select(skill => skill.SkillId),
            second.Snapshot.Target.LearnedSkills.Select(skill => skill.SkillId));
        Assert.Equal(
            first.Snapshot.Warnings.Select(warning => warning.Code),
            second.Snapshot.Warnings.Select(warning => warning.Code));
        Assert.Equal(
            first.Snapshot.FieldSources.Select(source => source.FieldPath),
            second.Snapshot.FieldSources.Select(source => source.FieldPath));
        Assert.Equal(
            first.DirectionEvidence.Select(value => value.SkillId),
            second.DirectionEvidence.Select(value => value.SkillId));
    }

    [Theory]
    [InlineData(TargetObservationContext.Hostile)]
    [InlineData(TargetObservationContext.Story)]
    public void Battle_visible_effects_do_not_change_equipped_membership(
        TargetObservationContext context)
    {
        var snapshot = CreateSnapshot(
            [Skill(100, SkillCategory.Attack, PracticeDirection.Direct)],
            equipped: Loadout((SkillCategory.Attack, 100)));
        var observation = Observation(
            TargetLoadoutCoverage.PartialLoadout,
            [
                Observed(
                    101,
                    SkillCategory.Defense,
                    PracticeDirection.Reverse,
                    visiblePowerPercent: 142)
            ],
            observationContext: context,
            evidenceReference: "E3-012-CAP-001");

        var result = TargetLoadoutObservationMerger.Merge(
            snapshot,
            observation,
            [Skill(101, SkillCategory.Defense)]);

        Assert.Equal(TargetLoadoutMergeStatus.Applied, result.Status);
        Assert.Equal(
            [100],
            result.Snapshot.Target.EquippedSkills.Value.AttackSkillIds);
        Assert.Empty(
            result.Snapshot.Target.EquippedSkills.Value.DefenseSkillIds);
        Assert.Contains(
            result.Snapshot.Target.LearnedSkills,
            skill => skill.SkillId == 101);
        Assert.Contains(
            result.Snapshot.FieldSources,
            source => source.FieldPath
                == TargetLoadoutObservationMerger
                    .TargetVisibleActiveEffectsField);
        Assert.DoesNotContain(
            result.Snapshot.FieldSources,
            source => source.FieldPath
                == TargetLoadoutObservationMerger.TargetEquippedSkillsField
                && source.Source
                    == SnapshotDataSource.CurrentScreenObservation);
        Assert.False(observation.EstablishesAbsenceOf(999));
    }

    private static TargetLoadoutCoverage CompleteCoverage() =>
        TargetLoadoutCoverage.CompleteCurrentLoadout(
            TargetLoadoutCompletenessEvidence.FromE3000(
                TargetLoadoutCompletenessEvidence.E3000GameDataVersion));

    private static TargetLoadoutObservation Observation(
        TargetLoadoutCoverage coverage,
        IEnumerable<ObservedTargetCombatSkill> skills,
        int targetId = 16317,
        DateTimeOffset? observedAt = null,
        TargetObservationContext observationContext =
            TargetObservationContext.Sparring,
        string evidenceReference = "E3-000-CAP-002") => new(
            targetId,
            observationContext,
            observedAt ?? SaveTime.AddMinutes(1),
            evidenceReference,
            coverage,
            skills);

    private static ObservedTargetCombatSkill Observed(
        int skillId,
        SkillCategory category,
        PracticeDirection? direction = null,
        int? slotIndex = null,
        int? visiblePowerPercent = null) => new(
            skillId,
            category,
            direction,
            slotIndex,
            visiblePowerPercent);

    private static CombatSnapshot CreateSnapshot(
        IEnumerable<CombatSkillSnapshot> learnedSkills,
        CombatLoadoutSnapshot? equipped = null,
        bool saveTimeAvailable = true,
        string gameDataVersion =
            TargetLoadoutCompletenessEvidence.E3000GameDataVersion,
        IEnumerable<SnapshotWarning>? warnings = null)
    {
        var metadata = new CombatSnapshotMetadata(
            new string('A', 64),
            SaveTime.AddSeconds(1),
            saveTimeAvailable
                ? SnapshotValue<DateTimeOffset>.Available(SaveTime)
                : SnapshotValue<DateTimeOffset>.Unavailable(
                    "Save time is unavailable."),
            SnapshotValue<string>.Available(gameDataVersion));
        var player = new PlayerCombatSnapshot(
            21396,
            SnapshotValue<string>.Available("Taiwu"),
            learnedSkills: [],
            new CombatLoadoutSnapshot([], [], [], [], []),
            equipment: [],
            new SlotBudgetSet(Enum.GetValues<SkillCategory>().Select(
                category => new SlotBudget(category, 0, 10))),
            new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookCostSlots: [],
            legendaryBookCostAssignments: []);
        var target = new TargetCombatSnapshot(
            16317,
            SnapshotValue<string>.Available("Target"),
            SnapshotValue<int>.Available(52),
            features: [],
            learnedSkills,
            equipped is null
                ? SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                    "Target loadout is unavailable.")
                : SnapshotValue<CombatLoadoutSnapshot>.Available(equipped),
            equipment: []);
        return new CombatSnapshot(
            metadata,
            player,
            target,
            warnings ?? [],
            fieldSources: []);
    }

    private static CombatSkillSnapshot Skill(
        int skillId,
        SkillCategory category,
        PracticeDirection? direction = null) => new(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(true),
            direction is null
                ? SnapshotValue<PracticeDirection>.Unavailable(
                    "Direction is unavailable.")
                : SnapshotValue<PracticeDirection>.Available(direction.Value),
            new SkillSlotContribution(1, 0, 0, 0, 0),
            SnapshotValue<int>.Available(1000 + skillId),
            SnapshotValue<int>.Available(2000 + skillId));

    private static CombatLoadoutSnapshot Loadout(
        params (SkillCategory Category, int SkillId)[] values) => new(
            Ids(values, SkillCategory.Neigong),
            Ids(values, SkillCategory.Attack),
            Ids(values, SkillCategory.Agility),
            Ids(values, SkillCategory.Defense),
            Ids(values, SkillCategory.Assistance));

    private static IEnumerable<int> Ids(
        IEnumerable<(SkillCategory Category, int SkillId)> values,
        SkillCategory category) => values
        .Where(value => value.Category == category)
        .Select(value => value.SkillId);
}
