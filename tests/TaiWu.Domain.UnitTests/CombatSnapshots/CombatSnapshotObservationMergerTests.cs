using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatSnapshotObservationMergerTests
{
    private static readonly DateTimeOffset SaveModifiedAt =
        new(2026, 7, 29, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Newer_observation_replaces_only_reported_player_fields()
    {
        var snapshot = CreateSnapshot();
        var observation = CreateObservation(
            observedAt: SaveModifiedAt.AddHours(1),
            includeDisplayedBudgets: true);

        var result = CombatSnapshotObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Equal([21], result.Player.EquippedSkills.AttackSkillIds);
        Assert.Equal(4, result.Player.GenericSlotAllocation.Attack);
        Assert.Equal(10, result.Player.SlotBudgets[SkillCategory.Attack].Capacity);
        Assert.Equal([20], snapshot.Player.EquippedSkills.AttackSkillIds);
        Assert.Equal(9, snapshot.Player.SlotBudgets[SkillCategory.Attack].Capacity);
        Assert.Same(snapshot.Metadata, result.Metadata);
        Assert.Equal(new string('A', 64), result.Metadata.SaveSha256);
        Assert.Equal(3, result.FieldSources.Length);
        Assert.All(
            result.FieldSources,
            source =>
            {
                Assert.Equal(
                    SnapshotDataSource.CurrentScreenObservation,
                    source.Source);
                Assert.Equal(
                    "sha256:screenshot",
                    source.EvidenceReference);
                Assert.Equal(
                    observation.ObservedAtUtc,
                    source.CapturedAtUtc);
            });
    }

    [Fact]
    public void Observation_without_budgets_retains_disk_capacity()
    {
        var snapshot = CreateSnapshot();
        var observation = CreateObservation(
            observedAt: SaveModifiedAt.AddMinutes(1),
            includeDisplayedBudgets: false);

        var result = CombatSnapshotObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Equal(9, result.Player.SlotBudgets[SkillCategory.Attack].Capacity);
        Assert.DoesNotContain(
            result.FieldSources,
            source =>
                source.FieldPath
                == CombatSnapshotObservationMerger.PlayerSlotBudgetsField);
        Assert.Equal(2, result.FieldSources.Length);
    }

    [Fact]
    public void Observation_not_newer_than_save_is_not_applied()
    {
        var snapshot = CreateSnapshot();
        var observation = CreateObservation(
            observedAt: SaveModifiedAt,
            includeDisplayedBudgets: true);

        var result = CombatSnapshotObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Same(snapshot.Player, result.Player);
        Assert.Empty(result.FieldSources);
        Assert.Contains(
            result.Warnings,
            warning =>
                warning.Code == "CURRENT_SCREEN_OBSERVATION_NOT_NEWER");
    }

    [Fact]
    public void Observation_uses_source_precedence_when_save_time_is_unavailable()
    {
        var snapshot = CreateSnapshot(saveTimestampAvailable: false);
        var observation = CreateObservation(
            observedAt: SaveModifiedAt.AddMinutes(1),
            includeDisplayedBudgets: false);

        var result = CombatSnapshotObservationMerger.Merge(
            snapshot,
            observation);

        Assert.Equal([21], result.Player.EquippedSkills.AttackSkillIds);
        Assert.Contains(
            result.Warnings,
            warning => warning.Code == "SAVE_TIMESTAMP_UNAVAILABLE");
    }

    [Fact]
    public void Observation_rejects_a_skill_not_learned_by_player()
    {
        var observation = new PlayerLoadoutObservation(
            SaveModifiedAt.AddHours(1),
            "sha256:screenshot",
            CreateLoadout(999),
            new GenericSlotAllocation(0, 0, 0, 0, 0));

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSnapshotObservationMerger.Merge(
                CreateSnapshot(),
                observation));

        Assert.Contains("not learned", exception.Message);
    }

    [Fact]
    public void Observation_rejects_a_skill_in_the_wrong_category()
    {
        var loadout = new CombatLoadoutSnapshot(
            neigongSkillIds: [21],
            attackSkillIds: [],
            agilitySkillIds: [],
            defenseSkillIds: [],
            assistanceSkillIds: []);
        var observation = new PlayerLoadoutObservation(
            SaveModifiedAt.AddHours(1),
            "sha256:screenshot",
            loadout,
            new GenericSlotAllocation(0, 0, 0, 0, 0));

        var exception = Assert.Throws<ArgumentException>(
            () => CombatSnapshotObservationMerger.Merge(
                CreateSnapshot(),
                observation));

        Assert.Contains("not Neigong", exception.Message);
    }

    [Fact]
    public void Observation_requires_evidence_and_normalizes_time()
    {
        var observedAt = new DateTimeOffset(
            2026,
            7,
            29,
            22,
            0,
            0,
            TimeSpan.FromHours(1));
        var observation = new PlayerLoadoutObservation(
            observedAt,
            "  sha256:screenshot  ",
            CreateLoadout(21),
            new GenericSlotAllocation(0, 0, 0, 0, 0));

        Assert.Equal(TimeSpan.Zero, observation.ObservedAtUtc.Offset);
        Assert.Equal("sha256:screenshot", observation.EvidenceReference);
        Assert.Throws<ArgumentException>(
            () => new PlayerLoadoutObservation(
                observedAt,
                " ",
                CreateLoadout(21),
                new GenericSlotAllocation(0, 0, 0, 0, 0)));
    }

    private static CombatSnapshot CreateSnapshot(
        bool saveTimestampAvailable = true)
    {
        var player = new PlayerCombatSnapshot(
            characterId: 21396,
            SnapshotValue<string>.Available("太吾"),
            learnedSkills:
            [
                CreateSkill(20, SkillCategory.Attack),
                CreateSkill(21, SkillCategory.Attack)
            ],
            equippedSkills: CreateLoadout(20),
            equipment: [],
            slotBudgets: CreateBudgets(attackCapacity: 9),
            genericSlotAllocation:
                new GenericSlotAllocation(0, 0, 0, 0, 0),
            legendaryBookModifiers: []);
        var target = new TargetCombatSnapshot(
            characterId: 16317,
            SnapshotValue<string>.Available("樂器奇書（52歲）"),
            SnapshotValue<int>.Available(52),
            features: [],
            learnedSkills: [],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "No equipped target skills in this snapshot."),
            equipment: []);

        return new CombatSnapshot(
            new CombatSnapshotMetadata(
                "local.sav",
                new string('A', 64),
                SaveModifiedAt.AddMinutes(1),
                saveTimestampAvailable
                    ? SnapshotValue<DateTimeOffset>.Available(SaveModifiedAt)
                    : SnapshotValue<DateTimeOffset>.Unavailable(
                        "Save timestamp was not available."),
                SnapshotValue<string>.Available("1.0.0+test")),
            player,
            target,
            warnings: []);
    }

    private static PlayerLoadoutObservation CreateObservation(
        DateTimeOffset observedAt,
        bool includeDisplayedBudgets)
    {
        return new PlayerLoadoutObservation(
            observedAt,
            "sha256:screenshot",
            CreateLoadout(21),
            new GenericSlotAllocation(
                totalSlots: 6,
                attack: 4,
                agility: 2,
                defense: 0,
                assistance: 0),
            includeDisplayedBudgets
                ? CreateBudgets(attackCapacity: 10)
                : null);
    }

    private static CombatLoadoutSnapshot CreateLoadout(int attackSkillId)
    {
        return new CombatLoadoutSnapshot(
            neigongSkillIds: [],
            attackSkillIds: [attackSkillId],
            agilitySkillIds: [],
            defenseSkillIds: [],
            assistanceSkillIds: []);
    }

    private static SlotBudgetSet CreateBudgets(int attackCapacity)
    {
        return new SlotBudgetSet(
        [
            new SlotBudget(SkillCategory.Neigong, 0, 6),
            new SlotBudget(SkillCategory.Attack, 1, attackCapacity),
            new SlotBudget(SkillCategory.Agility, 0, 8),
            new SlotBudget(SkillCategory.Defense, 0, 8),
            new SlotBudget(SkillCategory.Assistance, 0, 2)
        ]);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId,
        SkillCategory category)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available($"Skill {skillId}"),
            category,
            SnapshotValue<int>.Available(1),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(
                PracticeDirection.Neutral),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(0),
            SnapshotValue<int>.Available(0));
    }
}
