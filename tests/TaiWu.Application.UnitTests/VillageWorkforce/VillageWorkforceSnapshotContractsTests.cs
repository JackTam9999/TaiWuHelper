using TaiWu.Application.GameData;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Application.UnitTests.VillageWorkforce;

public sealed class VillageWorkforceSnapshotContractsTests
{
    [Fact]
    public void Request_and_port_expose_no_path_or_mutation_contract()
    {
        Assert.Empty(typeof(VillageWorkforceSnapshotReadRequest).GetProperties(
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.Instance));
        Assert.True(typeof(IReadOnlyGameDataSource).IsAssignableFrom(
            typeof(IVillageWorkforceSnapshotReader)));
        var method = Assert.Single(
            typeof(IVillageWorkforceSnapshotReader).GetMethods());
        Assert.Equal("ReadAsync", method.Name);
        Assert.DoesNotContain(method.GetParameters(), parameter =>
            parameter.ParameterType == typeof(string)
            || parameter.Name?.Contains(
                "path",
                StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Read_result_enforces_success_and_failure_payloads()
    {
        var snapshot = Snapshot();
        var complete = VillageWorkforceSnapshotReadResult.Complete(snapshot);
        var partial = VillageWorkforceSnapshotReadResult.Partial(snapshot);
        var failed = VillageWorkforceSnapshotReadResult.Failed(
            VillageWorkforceSnapshotReadStatus.ChangedRevision,
            "SAVE_REVISION_CHANGED",
            "Retry after the save is stable.");

        Assert.Equal(
            VillageWorkforceSnapshotReadStatus.Complete,
            complete.Status);
        Assert.Same(snapshot, complete.Snapshot);
        Assert.Equal(
            VillageWorkforceSnapshotReadStatus.Partial,
            partial.Status);
        Assert.Equal(
            VillageWorkforceSnapshotReadStatus.ChangedRevision,
            failed.Status);
        Assert.Null(failed.Snapshot);
        Assert.Equal("SAVE_REVISION_CHANGED", failed.FailureIdentity);
        Assert.Throws<ArgumentException>(() =>
            VillageWorkforceSnapshotReadResult.Failed(
                VillageWorkforceSnapshotReadStatus.Complete,
                "INVALID",
                "Invalid result."));
        Assert.Throws<ArgumentException>(() =>
            VillageWorkforceSnapshotReadResult.Failed(
                VillageWorkforceSnapshotReadStatus.ReadFailed,
                @"C:\unsafe\save.sav",
                "Unsafe identity."));
    }

    private static VillageWorkforceSnapshot Snapshot()
    {
        var sha = new string('A', 64);
        var versions = new WorkforceSourceVersions(
            sha,
            "1.0.0+supported",
            "1",
            "1",
            "1");
        var save = new WorkforceProvenance(
            WorkforceEvidenceSourceKind.ConfiguredSave,
            "CONFIGURED_SAVE",
            "1",
            sha);
        var gameData = new WorkforceProvenance(
            WorkforceEvidenceSourceKind.InstalledGameData,
            "GAMEDATA",
            "1.0.0+supported",
            "ASSEMBLY_A");
        var discipline = new LifeSkillDisciplineIdentity(6);
        var worker = new VillageWorkerProfile(
            new VillageWorkerIdentity(101),
            WorkforceWorkerState.Eligible,
            versions,
            [
                WorkforceFact.Confirmed(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.CandidateUniverseMembership),
                    WorkforceFactValue.Boolean(true),
                    save,
                    []),
                WorkforceFact.Confirmed(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.BaseLifeSkillQualification,
                        discipline),
                    WorkforceFactValue.Int16(60),
                    save,
                    [])
            ],
            []);
        var target = new ShopManagerTarget(
            new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(1, 2, 3),
                0),
            discipline,
            [new WorkforceEvidenceReference("TARGET", gameData)]);
        return new VillageWorkforceSnapshot(
            new SettlementIdentity(12),
            DateTimeOffset.UnixEpoch,
            versions,
            [worker],
            [target],
            [new CurrentShopManagerAssignment(
                target.Identity,
                worker.Identity,
                save)],
            []);
    }
}
