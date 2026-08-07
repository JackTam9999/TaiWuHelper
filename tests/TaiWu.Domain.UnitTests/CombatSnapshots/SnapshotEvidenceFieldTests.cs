using TaiWu.Domain.CombatSkills;
using TaiWu.Domain.CombatSnapshots;
using Xunit;

namespace TaiWu.Domain.UnitTests.CombatSnapshots;

public sealed class SnapshotEvidenceFieldTests
{
    private static readonly DateTimeOffset CapturedAt = new(
        2026,
        8,
        7,
        20,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void Provenance_vocabularies_keep_all_source_kinds_distinct()
    {
        Assert.Equal(
            4,
            Enum.GetValues<SnapshotDataSource>().Distinct().Count());
        Assert.Equal(
            4,
            Enum.GetValues<SkillProgressSourceKind>().Distinct().Count());

        var installed = new SkillProgressSource(
            SkillProgressSourceKind.InstalledConfiguration,
            "gamedata:68032f25",
            "skill:100:category");

        Assert.Equal(
            SkillProgressSourceKind.InstalledConfiguration,
            installed.Kind);
        Assert.NotEqual(
            SnapshotDataSource.GameConfiguration,
            SnapshotDataSource.VerifiedRule);
    }

    [Fact]
    public void Current_screen_source_retains_time_and_opaque_evidence()
    {
        var capturedAt = new DateTimeOffset(
            2026,
            8,
            7,
            21,
            0,
            0,
            TimeSpan.FromHours(1));
        var source = new SnapshotFieldSource(
            "  target.equippedSkills  ",
            SnapshotDataSource.CurrentScreenObservation,
            capturedAt,
            "  E3-000-CAP-002  ");

        Assert.Equal("target.equippedSkills", source.FieldPath);
        Assert.Equal(
            SnapshotDataSource.CurrentScreenObservation,
            source.Source);
        Assert.Equal(TimeSpan.Zero, source.CapturedAtUtc.Offset);
        Assert.Equal(capturedAt.UtcDateTime, source.CapturedAtUtc);
        Assert.Equal("E3-000-CAP-002", source.EvidenceReference);
    }

    [Theory]
    [InlineData("C:\\Users\\Pong\\capture.png")]
    [InlineData("docs/evidence/capture.md")]
    [InlineData("../capture")]
    [InlineData("capture\nexception detail")]
    public void Public_evidence_reference_rejects_paths_and_details(
        string reference)
    {
        Assert.Throws<ArgumentException>(
            () => Source(
                SnapshotDataSource.CurrentScreenObservation,
                reference: reference));
    }

    [Fact]
    public void Source_rejects_invalid_values()
    {
        Assert.Throws<ArgumentException>(
            () => new SnapshotFieldSource(
                " ",
                SnapshotDataSource.Save,
                CapturedAt,
                "save:abc"));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SnapshotFieldSource(
                "target.equippedSkills",
                (SnapshotDataSource)99,
                CapturedAt,
                "save:abc"));
        Assert.Throws<ArgumentException>(
            () => new SnapshotFieldSource(
                "target.equippedSkills",
                SnapshotDataSource.Save,
                CapturedAt,
                " "));
    }

    [Fact]
    public void Available_field_retains_selected_value_and_provenance()
    {
        var source = Source(SnapshotDataSource.CurrentScreenObservation);
        var field = SnapshotEvidenceField<int>.Available(42, source);

        Assert.Equal(SnapshotEvidenceStatus.Available, field.Status);
        Assert.True(field.IsAvailable);
        Assert.Equal(42, field.Value);
        Assert.Same(source, field.Source);
        Assert.Same(source, Assert.Single(field.Observations).Source);
        Assert.Null(field.ReasonCode);
    }

    [Fact]
    public void Unavailable_field_has_no_value_or_fabricated_observation()
    {
        var field = SnapshotEvidenceField<int>.Unavailable(
            "HOSTILE_OR_STORY_TARGET_UNAVAILABLE");

        Assert.Equal(SnapshotEvidenceStatus.Unavailable, field.Status);
        Assert.False(field.IsAvailable);
        Assert.Equal(
            "HOSTILE_OR_STORY_TARGET_UNAVAILABLE",
            field.ReasonCode);
        Assert.Null(field.Source);
        Assert.Empty(field.Observations);
        Assert.Throws<InvalidOperationException>(() => field.Value);
    }

    [Fact]
    public void Stale_field_copies_and_orders_retained_observations()
    {
        List<SnapshotFieldObservation<int>> source =
        [
            Observation(
                2,
                SnapshotDataSource.CurrentScreenObservation,
                CapturedAt.AddMinutes(2),
                "screen:newer"),
            Observation(
                1,
                SnapshotDataSource.Save,
                CapturedAt,
                "save:older")
        ];

        var field = SnapshotEvidenceField<int>.Stale(
            "OBSERVATION_OLDER_THAN_SNAPSHOT",
            source);
        source.Clear();

        Assert.Equal(SnapshotEvidenceStatus.Stale, field.Status);
        Assert.Equal([1, 2], field.Observations.Select(value => value.Value));
        Assert.Throws<InvalidOperationException>(() => field.Value);
    }

    [Fact]
    public void Conflict_retains_distinct_values_in_deterministic_order()
    {
        var field = SnapshotEvidenceField<string>.Conflicting(
            "SAVE_SCREEN_CONFLICT",
            [
                Observation(
                    "screen",
                    SnapshotDataSource.CurrentScreenObservation,
                    CapturedAt.AddMinutes(1),
                    "screen:E3-000-CAP-002"),
                Observation(
                    "rule",
                    SnapshotDataSource.VerifiedRule,
                    CapturedAt,
                    "rule:E3-000"),
                Observation(
                    "configuration",
                    SnapshotDataSource.GameConfiguration,
                    CapturedAt,
                    "gamedata:68032f25"),
                Observation(
                    "save",
                    SnapshotDataSource.Save,
                    CapturedAt,
                    "save:abc")
            ]);

        Assert.Equal(SnapshotEvidenceStatus.Conflicting, field.Status);
        Assert.Equal(
            ["save", "configuration", "rule", "screen"],
            field.Observations.Select(value => value.Value));
        Assert.Null(field.Source);
        Assert.Throws<InvalidOperationException>(() => field.Value);
    }

    [Fact]
    public void Conflict_requires_distinct_values_and_sources()
    {
        var save = Observation(
            1,
            SnapshotDataSource.Save,
            CapturedAt,
            "save:abc");
        var screen = Observation(
            1,
            SnapshotDataSource.CurrentScreenObservation,
            CapturedAt.AddMinutes(1),
            "screen:abc");

        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Conflicting(
                "SOURCE_CONFLICT",
                [save]));
        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Conflicting(
                "SOURCE_CONFLICT",
                [save, screen]));
        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Conflicting(
                "SOURCE_CONFLICT",
                [save, save]));
    }

    [Fact]
    public void Retained_observations_must_describe_one_field()
    {
        var otherField = new SnapshotFieldObservation<int>(
            2,
            new SnapshotFieldSource(
                "target.age",
                SnapshotDataSource.Save,
                CapturedAt.AddMinutes(1),
                "save:abc"));

        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Conflicting(
                "SOURCE_CONFLICT",
                [
                    Observation(
                        1,
                        SnapshotDataSource.Save,
                        CapturedAt,
                        "save:older"),
                    otherField
                ]));
    }

    [Fact]
    public void Invalid_status_inputs_are_rejected()
    {
        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Unavailable(" "));
        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Unavailable(
                "Exception message or C:\\secret"));
        Assert.Throws<ArgumentNullException>(
            () => SnapshotEvidenceField<string>.Available(
                null!,
                Source(SnapshotDataSource.Save)));
        Assert.Throws<ArgumentNullException>(
            () => SnapshotEvidenceField<int>.Available(1, null!));
        Assert.Throws<ArgumentNullException>(
            () => SnapshotEvidenceField<int>.Stale(
                "STALE_EVIDENCE",
                null!));
        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Stale("STALE_EVIDENCE", []));
        Assert.Throws<ArgumentException>(
            () => SnapshotEvidenceField<int>.Stale(
                "STALE_EVIDENCE",
                [null!]));
    }

    [Fact]
    public void Existing_player_loadout_observation_remains_compatible()
    {
        var observation = new PlayerLoadoutObservation(
            CapturedAt,
            "screen:player-loadout",
            new CombatLoadoutSnapshot([], [], [], [], []),
            new GenericSlotAllocation(0, 0, 0, 0, 0));

        Assert.Equal("screen:player-loadout", observation.EvidenceReference);
        Assert.Empty(observation.EquippedSkills.AttackSkillIds);
    }

    private static SnapshotFieldObservation<T> Observation<T>(
        T value,
        SnapshotDataSource source,
        DateTimeOffset capturedAt,
        string reference) => new(
            value,
            Source(source, capturedAt, reference));

    private static SnapshotFieldSource Source(
        SnapshotDataSource source,
        DateTimeOffset? capturedAt = null,
        string reference = "evidence:abc") => new(
            "target.equippedSkills",
            source,
            capturedAt ?? CapturedAt,
            reference);
}
