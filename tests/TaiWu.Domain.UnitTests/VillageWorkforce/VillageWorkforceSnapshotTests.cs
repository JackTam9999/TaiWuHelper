using System.Collections.Immutable;
using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Domain.UnitTests.VillageWorkforce;

public sealed class VillageWorkforceSnapshotTests
{
    [Fact]
    public void Snapshot_is_immutable_canonical_and_repeatable()
    {
        var workerA = VillageWorkforceFixtures.Worker(101, 60);
        var workerB = VillageWorkforceFixtures.Worker(202, 80);
        var first = VillageWorkforceFixtures.Snapshot(
            [workerB, workerA],
            currentWorker: workerA.Identity);
        var second = VillageWorkforceFixtures.Snapshot(
            [workerA, workerB],
            currentWorker: workerA.Identity);

        Assert.IsType<ImmutableArray<VillageWorkerProfile>>(first.Workers);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal(
            new[] { 101, 202 },
            first.Workers.Select(item => item.Identity.CharacterId));
    }

    [Fact]
    public void Snapshot_rejects_duplicate_or_dangling_contracts()
    {
        var worker = VillageWorkforceFixtures.Worker(101, 60);
        Assert.Throws<ArgumentException>(
            () => VillageWorkforceFixtures.Snapshot([worker, worker]));

        var target = VillageWorkforceFixtures.Target();
        Assert.Throws<ArgumentException>(() => new VillageWorkforceSnapshot(
            new SettlementIdentity(12),
            DateTimeOffset.UtcNow,
            VillageWorkforceFixtures.Versions,
            [worker],
            [target],
            [new CurrentShopManagerAssignment(
                target.Identity,
                new VillageWorkerIdentity(999),
                VillageWorkforceFixtures.SaveProvenance)],
            []));
    }

    [Fact]
    public void Every_version_one_target_requires_one_current_assignment()
    {
        var worker = VillageWorkforceFixtures.Worker(101, 60);
        var target = VillageWorkforceFixtures.Target();

        Assert.Throws<ArgumentException>(() => new VillageWorkforceSnapshot(
            new SettlementIdentity(12),
            DateTimeOffset.UtcNow,
            VillageWorkforceFixtures.Versions,
            [worker],
            [target],
            [],
            []));
    }

    [Fact]
    public void Snapshot_rejects_mixed_save_revisions()
    {
        var worker = VillageWorkforceFixtures.Worker(101, 60);
        var target = VillageWorkforceFixtures.Target();
        var otherRevision = new WorkforceProvenance(
            WorkforceEvidenceSourceKind.ConfiguredSave,
            "CONFIGURED_SAVE",
            "1",
            new string('B', 64));

        Assert.Throws<ArgumentException>(() => new VillageWorkforceSnapshot(
            new SettlementIdentity(12),
            DateTimeOffset.UtcNow,
            VillageWorkforceFixtures.Versions,
            [worker],
            [target],
            [new CurrentShopManagerAssignment(
                target.Identity,
                worker.Identity,
                otherRevision)],
            []));
    }

    [Fact]
    public void Current_and_proposed_assignments_have_separate_origins()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var resultIdentity = new WorkforceResultIdentity(
            snapshot.Fingerprint,
            new WorkforceObjectiveIdentity(
                WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
                "1"),
            new WorkforceRuleVersion("1.0.0"),
            snapshot.Targets[0].Identity);
        var proposed = new ProposedShopManagerAssignment(
            resultIdentity,
            snapshot.Workers[1].Identity);

        Assert.Equal(
            WorkforceAssignmentOrigin.CurrentSave,
            snapshot.CurrentAssignments[0].Origin);
        Assert.Equal(
            WorkforceAssignmentOrigin.ProposedHelper,
            proposed.Origin);
        Assert.Equal(
            typeof(CurrentShopManagerAssignment),
            typeof(VillageWorkforceSnapshot)
                .GetProperty(nameof(VillageWorkforceSnapshot.CurrentAssignments))!
                .PropertyType
                .GetGenericArguments()
                .Single());
    }

    [Fact]
    public void Snapshot_fingerprint_includes_time_target_and_assignment_facts()
    {
        var baseline = VillageWorkforceFixtures.Snapshot();
        var later = VillageWorkforceFixtures.Snapshot(
            capturedAt: baseline.CapturedAt.AddSeconds(1));
        var differentTarget = VillageWorkforceFixtures.Snapshot(
            target: VillageWorkforceFixtures.Target(buildingIndex: 8));

        Assert.NotEqual(baseline.Fingerprint, later.Fingerprint);
        Assert.NotEqual(baseline.Fingerprint, differentTarget.Fingerprint);
    }
}
