using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.GameData;
using TaiWu.Domain.CompanionCandidates;
using Xunit;

namespace TaiWu.Application.UnitTests.CompanionCandidates;

public sealed class CompanionCandidateSnapshotContractsTests
{
    [Fact]
    public void Read_request_and_port_expose_no_caller_path_or_mutation()
    {
        Assert.Empty(typeof(CompanionCandidateSnapshotReadRequest).GetProperties(
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance));
        Assert.True(typeof(IReadOnlyGameDataSource).IsAssignableFrom(
            typeof(ICompanionCandidateSnapshotReader)));
        var method = Assert.Single(typeof(ICompanionCandidateSnapshotReader).GetMethods());
        Assert.Equal("ReadAsync", method.Name);
        Assert.DoesNotContain(
            method.GetParameters(),
            parameter => parameter.ParameterType == typeof(string)
                || parameter.Name?.Contains("path", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Snapshot_copies_sorts_and_requires_one_source_revision()
    {
        var versions = Versions();
        var profiles = new List<CandidateProfile>
        {
            Profile(9, versions),
            Profile(2, versions)
        };
        var snapshot = new CompanionCandidateSnapshot(
            DateTimeOffset.Parse("2026-08-17T12:00:00+02:00"),
            versions,
            profiles,
            [],
            [],
            []);
        profiles.Clear();

        Assert.Equal(DateTimeOffset.Parse("2026-08-17T10:00:00Z"), snapshot.CapturedAtUtc);
        Assert.Equal([2, 9], snapshot.Profiles.Select(item => item.Identity.CharacterId));
        Assert.Throws<ArgumentException>(() => new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            versions,
            [Profile(2, versions), Profile(2, versions)],
            [],
            [],
            []));
        Assert.Throws<ArgumentException>(() => new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            versions,
            [Profile(2, Versions(saveHash: OtherSha))],
            [],
            [],
            []));
    }

    [Fact]
    public void Snapshot_nested_collections_are_typed_sorted_and_unique()
    {
        var versions = Versions();
        var later = new CompanionCandidateOmission(
            9,
            "MAPPING_FAILED",
            "Candidate mapping failed.");
        var earlier = new CompanionCandidateOmission(
            2,
            "CHARACTER_MISSING",
            "Candidate character was missing.");
        var warning = new CompanionCandidateSnapshotWarning(
            CompanionCandidateSnapshotWarningKind.StandaloneEventRuntimeUnavailable,
            "Expected standalone boundary.");
        var diagnostic = new CompanionCandidateSnapshotDiagnostic(
            "PARTIAL_SNAPSHOT",
            CompanionCandidateSnapshotDiagnosticSeverity.Warning,
            "One candidate is incomplete.",
            new CandidateIdentity(2));
        var snapshot = new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            versions,
            [],
            [later, earlier],
            [warning],
            [diagnostic]);

        Assert.Equal([2, 9], snapshot.Omissions.Select(item => item.CharacterId));
        Assert.Equal(
            CompanionCandidateSnapshotWarningKind.StandaloneEventRuntimeUnavailable,
            Assert.Single(snapshot.Warnings).Kind);
        Assert.Equal("PARTIAL_SNAPSHOT", Assert.Single(snapshot.Diagnostics).Identity);
        Assert.Throws<ArgumentException>(() => new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            versions,
            [],
            [earlier, earlier],
            [],
            []));
    }

    [Fact]
    public void Snapshot_keeps_bilingual_display_context_outside_profile_identity()
    {
        var versions = Versions();
        var profile = Profile(2, versions);
        var display = new CompanionCandidateDisplay(
            profile.Identity,
            "範例人物",
            "Synthetic Person",
            "範例地點",
            "Synthetic Place");
        var snapshot = new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            versions,
            [profile],
            [],
            [],
            [],
            [display]);

        Assert.Same(display, Assert.Single(snapshot.Displays));
        Assert.Equal("Synthetic Person", display.EnglishName);
        Assert.Equal("範例地點", display.TraditionalChineseLocation);
        Assert.DoesNotContain(
            typeof(CompanionCandidateDisplay).GetProperties(),
            property => property.Name.Contains("Score", StringComparison.Ordinal)
                || property.Name.Contains("Rank", StringComparison.Ordinal)
                || property.Name.Contains("Eligible", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            versions,
            [profile],
            [],
            [],
            [],
            [display, display]));
        Assert.Throws<ArgumentException>(() => new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            versions,
            [profile],
            [],
            [],
            [],
            [new CompanionCandidateDisplay(
                new CandidateIdentity(3),
                "其他人物",
                "Other Person",
                null,
                null)]));
    }

    [Fact]
    public void Discipline_display_contracts_preserve_typed_order_and_state()
    {
        var life = new CompanionDisciplineDisplayName(
            new CandidateDisciplineIdentity(
                CandidateDisciplineDomain.LifeSkill,
                1),
            "弈棋",
            "Strategy Games");
        var martial = new CompanionDisciplineDisplayName(
            new CandidateDisciplineIdentity(
                CandidateDisciplineDomain.Martial,
                0),
            "內功",
            "Internal Arts");
        var result = new CompanionDisciplineDisplayResult(
            CompanionDisciplineDisplayStatus.Complete,
            [life, martial]);

        Assert.Equal([martial, life], result.Disciplines);
        Assert.Null(result.FailureIdentity);
        Assert.Throws<ArgumentException>(() =>
            new CompanionDisciplineDisplayResult(
                CompanionDisciplineDisplayStatus.Complete,
                [new CompanionDisciplineDisplayName(
                    martial.Discipline,
                    "內功",
                    englishName: null)]));
        Assert.Throws<ArgumentException>(() =>
            new CompanionDisciplineDisplayResult(
                CompanionDisciplineDisplayStatus.Unavailable,
                disciplines: []));
    }

    [Fact]
    public void Read_result_enforces_success_and_failure_payloads()
    {
        var snapshot = new CompanionCandidateSnapshot(
            DateTimeOffset.UtcNow,
            Versions(),
            [],
            [],
            [],
            []);
        var complete = CompanionCandidateSnapshotReadResult.Complete(snapshot);
        var partial = CompanionCandidateSnapshotReadResult.Partial(snapshot);
        var failed = CompanionCandidateSnapshotReadResult.Failed(
            CompanionCandidateSnapshotReadStatus.ChangedRevision,
            "SAVE_REVISION_CHANGED",
            "Retry after the save is stable.");

        Assert.Equal(CompanionCandidateSnapshotReadStatus.Complete, complete.Status);
        Assert.Same(snapshot, complete.Snapshot);
        Assert.Equal(CompanionCandidateSnapshotReadStatus.Partial, partial.Status);
        Assert.Equal(CompanionCandidateSnapshotReadStatus.ChangedRevision, failed.Status);
        Assert.Null(failed.Snapshot);
        Assert.Equal("SAVE_REVISION_CHANGED", failed.FailureIdentity);
        Assert.Throws<ArgumentException>(() => CompanionCandidateSnapshotReadResult.Failed(
            CompanionCandidateSnapshotReadStatus.Complete,
            "INVALID",
            "Invalid result."));
    }

    [Fact]
    public void Public_snapshot_contracts_reject_invalid_enums_blank_ids_and_paths()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CompanionCandidateSnapshotWarning(
                (CompanionCandidateSnapshotWarningKind)99,
                "Unknown warning."));
        Assert.Throws<ArgumentException>(() => new CompanionCandidateOmission(
            2,
            @"C:\local\save.sav",
            "Unsafe identity."));
        Assert.Throws<ArgumentException>(() => new CompanionCandidateSnapshotDiagnostic(
            " ",
            CompanionCandidateSnapshotDiagnosticSeverity.Warning,
            "Missing identity."));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompanionCandidateOmission(
            0,
            "INVALID_ID",
            "Invalid character ID."));
    }

    private const string Sha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string OtherSha =
        "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";

    private static CandidateProfileSourceVersions Versions(
        string saveHash = Sha) => new(
            saveHash,
            "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20",
            "1",
            "1",
            "1");

    private static CandidateProfile Profile(
        int id,
        CandidateProfileSourceVersions versions) => new(
            new CandidateIdentity(id),
            CandidateUniverseState.Eligible,
            versions,
            [],
            []);
}
