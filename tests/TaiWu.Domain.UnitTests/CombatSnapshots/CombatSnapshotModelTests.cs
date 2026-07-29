using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class CombatSnapshotModelTests
{
    [Fact]
    public void Combat_skill_uses_typed_direction_and_explicit_unavailable_values()
    {
        var skill = CreateSkill(604, PracticeDirection.Neutral);

        Assert.Equal(SkillCategory.Attack, skill.Category);
        Assert.Equal(PracticeDirection.Neutral, skill.Direction.Value);
        Assert.False(skill.ReverseEffectId.IsAvailable);
    }

    [Fact]
    public void Combat_skill_rejects_an_invalid_available_grid_cost()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CombatSkillSnapshot(
                skillId: 604,
                SnapshotValue<string>.Available("金猊鎮魔刀"),
                SkillCategory.Attack,
                SnapshotValue<int>.Available(0),
                SnapshotValue<bool>.Available(false),
                SnapshotValue<PracticeDirection>.Available(
                    PracticeDirection.Neutral),
                SkillSlotContribution.None,
                SnapshotValue<int>.Unavailable("Not initialized."),
                SnapshotValue<int>.Unavailable("Not initialized.")));
    }

    [Fact]
    public void Loadout_copies_input_collections()
    {
        var attacks = new List<int> { 604 };
        var loadout = new CombatLoadoutSnapshot(
            neigongSkillIds: [],
            attackSkillIds: attacks,
            agilitySkillIds: [],
            defenseSkillIds: [],
            assistanceSkillIds: []);

        attacks.Add(603);

        Assert.Equal([604], loadout.AttackSkillIds);
    }

    [Fact]
    public void Target_can_explicitly_report_unavailable_equipped_skills()
    {
        var target = new TargetCombatSnapshot(
            characterId: 16317,
            SnapshotValue<string>.Available("樂器奇書（52歲）"),
            SnapshotValue<int>.Available(52),
            learnedSkills: [],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "The current save contains no equipped target skills."),
            equipment: []);

        Assert.False(target.EquippedSkills.IsAvailable);
        Assert.Contains(
            "no equipped target skills",
            target.EquippedSkills.UnavailableReason);
    }

    [Fact]
    public void Metadata_normalizes_hash_and_time_to_utc()
    {
        var metadata = new CombatSnapshotMetadata(
            "local.sav",
            new string('a', 64),
            new DateTimeOffset(2026, 7, 29, 23, 0, 0, TimeSpan.FromHours(1)),
            SnapshotValue<DateTimeOffset>.Unavailable(
                "File timestamp was not available."),
            SnapshotValue<string>.Available("1.0.0+test"));

        Assert.Equal(new string('A', 64), metadata.SaveSha256);
        Assert.Equal(TimeSpan.Zero, metadata.CapturedAtUtc.Offset);
    }

    [Fact]
    public void Snapshot_copies_warning_collection()
    {
        var warnings = new List<SnapshotWarning>
        {
            new("TARGET_LOADOUT_UNAVAILABLE", "Target loadout is unavailable.")
        };
        var snapshot = CreateSnapshot(warnings);

        warnings.Add(new SnapshotWarning("STALE_SAVE", "Save is older."));

        Assert.Single(snapshot.Warnings);
        Assert.Equal("TARGET_LOADOUT_UNAVAILABLE", snapshot.Warnings[0].Code);
    }

    [Fact]
    public void Player_rejects_duplicate_learned_skill_ids()
    {
        var duplicateSkills = new[]
        {
            CreateSkill(604, PracticeDirection.Neutral),
            CreateSkill(604, PracticeDirection.Reverse)
        };

        Assert.Throws<ArgumentException>(
            () => CreatePlayer(duplicateSkills));
    }

    private static CombatSnapshot CreateSnapshot(
        IEnumerable<SnapshotWarning> warnings)
    {
        var metadata = new CombatSnapshotMetadata(
            "local.sav",
            new string('B', 64),
            DateTimeOffset.UtcNow,
            SnapshotValue<DateTimeOffset>.Available(DateTimeOffset.UtcNow),
            SnapshotValue<string>.Available("1.0.0+test"));
        var player = CreatePlayer([CreateSkill(604, PracticeDirection.Neutral)]);
        var target = new TargetCombatSnapshot(
            16317,
            SnapshotValue<string>.Available("樂器奇書（52歲）"),
            SnapshotValue<int>.Available(52),
            learnedSkills: [],
            SnapshotValue<CombatLoadoutSnapshot>.Unavailable(
                "No equipped target skills in this snapshot."),
            equipment: []);

        return new CombatSnapshot(metadata, player, target, warnings);
    }

    private static PlayerCombatSnapshot CreatePlayer(
        IEnumerable<CombatSkillSnapshot> skills)
    {
        return new PlayerCombatSnapshot(
            21396,
            SnapshotValue<string>.Unavailable(
                "Localized player name was not initialized."),
            skills,
            new CombatLoadoutSnapshot(
                neigongSkillIds: [],
                attackSkillIds: [604],
                agilitySkillIds: [],
                defenseSkillIds: [],
                assistanceSkillIds: []),
            equipment: [],
            new SlotBudgetSet(
            [
                new SlotBudget(SkillCategory.Neigong, 6, 6),
                new SlotBudget(SkillCategory.Attack, 3, 10),
                new SlotBudget(SkillCategory.Agility, 0, 8),
                new SlotBudget(SkillCategory.Defense, 0, 8),
                new SlotBudget(SkillCategory.Assistance, 0, 2)
            ]),
            new GenericSlotAllocation(6, 4, 2, 0, 0),
            legendaryBookModifiers: []);
    }

    private static CombatSkillSnapshot CreateSkill(
        int skillId,
        PracticeDirection direction)
    {
        return new CombatSkillSnapshot(
            skillId,
            SnapshotValue<string>.Available("金猊鎮魔刀"),
            SkillCategory.Attack,
            SnapshotValue<int>.Available(3),
            SnapshotValue<bool>.Available(false),
            SnapshotValue<PracticeDirection>.Available(direction),
            SkillSlotContribution.None,
            SnapshotValue<int>.Available(338),
            SnapshotValue<int>.Unavailable(
                "Reverse effect runtime was not initialized."));
    }
}
