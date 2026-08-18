using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Domain.UnitTests.VillageWorkforce;

public sealed class WorkforceIdentityAndEvidenceTests
{
    [Fact]
    public void Stable_identities_reject_invalid_source_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new VillageWorkerIdentity(0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SettlementIdentity(-1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShopBuildingIdentity(0, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(0, 0, 0),
                128));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LifeSkillDisciplineIdentity(16));
        Assert.Throws<ArgumentException>(
            () => new WorkforceSourceVersions(
                "not-a-hash",
                "1",
                "1",
                "1",
                "1"));
    }

    [Fact]
    public void Stable_identities_use_value_equality()
    {
        Assert.Equal(
            new VillageWorkerIdentity(101),
            new VillageWorkerIdentity(101));
        Assert.Equal(
            new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(1, 2, 3),
                4),
            new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(1, 2, 3),
                4));
        Assert.NotEqual(
            new VillageWorkerIdentity(101),
            new VillageWorkerIdentity(102));
    }

    [Fact]
    public void Qualification_fact_requires_a_discipline_and_matching_value()
    {
        Assert.Throws<ArgumentException>(
            () => new WorkforceFactIdentity(
                WorkforceFactKind.BaseLifeSkillQualification));
        var identity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));

        Assert.Throws<ArgumentException>(() => WorkforceFact.Confirmed(
            identity,
            WorkforceFactValue.Boolean(true),
            VillageWorkforceFixtures.SaveProvenance,
            []));
    }

    [Fact]
    public void Unavailable_and_conflicting_facts_preserve_typed_state()
    {
        var identity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));
        var missing = WorkforceFact.Incomplete(
            identity,
            new WorkforceUnavailableReason("FACT_MISSING"),
            [VillageWorkforceFixtures.SaveEvidence("MISSING")]);
        var unsupported = WorkforceFact.Unsupported(
            identity,
            new WorkforceUnavailableReason("VERSION_UNSUPPORTED"),
            []);
        var conflicting = WorkforceFact.Conflicting(
            identity,
            [
                new WorkforceConflictValue(
                    WorkforceFactValue.Int16(50),
                    VillageWorkforceFixtures.SaveProvenance),
                new WorkforceConflictValue(
                    WorkforceFactValue.Int16(60),
                    VillageWorkforceFixtures.GameDataProvenance)
            ],
            []);

        Assert.Equal(WorkforceEvidenceState.Incomplete, missing.State);
        Assert.Null(missing.Value);
        Assert.Equal("FACT_MISSING", missing.UnavailableReason?.Code);
        Assert.Equal(WorkforceEvidenceState.Unsupported, unsupported.State);
        Assert.Equal(WorkforceEvidenceState.Conflicting, conflicting.State);
        Assert.Equal(2, conflicting.Conflicts.Length);
        Assert.All(conflicting.Conflicts,
            item => Assert.NotNull(item.Provenance));
    }

    [Fact]
    public void Profile_canonicalizes_fact_order_and_rejects_duplicates()
    {
        var first = VillageWorkforceFixtures.Worker(101, 70);
        var reversed = new VillageWorkerProfile(
            first.Identity,
            first.State,
            first.SourceVersions,
            first.Facts.Reverse(),
            first.Diagnostics.Reverse());

        Assert.Equal(first.Fingerprint, reversed.Fingerprint);
        Assert.Equal(first.Facts, reversed.Facts);
        Assert.Throws<ArgumentException>(() => new VillageWorkerProfile(
            first.Identity,
            first.State,
            first.SourceVersions,
            [first.Facts[0], first.Facts[0]],
            []));
    }

    [Fact]
    public void Fingerprint_changes_when_an_evaluation_fact_changes()
    {
        var lower = VillageWorkforceFixtures.Worker(101, 70);
        var higher = VillageWorkforceFixtures.Worker(101, 71);

        Assert.NotEqual(lower.Fingerprint, higher.Fingerprint);
    }
}
