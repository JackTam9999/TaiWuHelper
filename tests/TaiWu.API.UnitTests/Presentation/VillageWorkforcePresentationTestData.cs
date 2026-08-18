using NSubstitute;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.API.UnitTests.Presentation;

internal static class VillageWorkforcePresentationTestData
{
    private const string SaveSha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    public static VillageWorkforceSnapshot Snapshot()
    {
        var workers = new[]
        {
            Worker(41001, 64),
            Worker(41002, 72),
            Worker(41003, 72),
            Worker(41004, null)
        };
        var target = new ShopManagerTarget(
            new ShopManagerTargetIdentity(
                new ShopBuildingIdentity(11, 22, 33),
                0),
            new LifeSkillDisciplineIdentity(6),
            [new WorkforceEvidenceReference(
                "SHOP_TARGET",
                GameDataProvenance())]);
        return new VillageWorkforceSnapshot(
            new SettlementIdentity(44),
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
            Versions(),
            workers,
            [target],
            [new CurrentShopManagerAssignment(
                target.Identity,
                workers[0].Identity,
                SaveProvenance())],
            []);
    }

    public static async Task<VillageWorkforceFinderResult> ResultAsync()
    {
        var snapshot = Snapshot();
        var reader = Substitute.For<IVillageWorkforceSnapshotReader>();
        reader.ReadAsync(
                VillageWorkforceSnapshotReadRequest.Current,
                Arg.Any<CancellationToken>())
            .Returns(VillageWorkforceSnapshotReadResult.Complete(snapshot));
        return await new FindVillageWorkforce(reader).ExecuteAsync(
            new VillageWorkforceFinderRequest(
                snapshot.Targets[0].Identity,
                new WorkforceObjectiveIdentity(
                    WorkforceObjectiveKind
                        .ShopManagerBaseLifeSkillQualification,
                    VerifiedVillageWorkforceRules.ObjectiveVersion)));
    }

    private static VillageWorkerProfile Worker(
        int characterId,
        short? qualification)
    {
        var save = SaveProvenance();
        var qualificationIdentity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));
        var qualificationFact = qualification.HasValue
            ? WorkforceFact.Confirmed(
                qualificationIdentity,
                WorkforceFactValue.Int16(qualification.Value),
                save,
                [new WorkforceEvidenceReference("QUALIFICATION", save)])
            : WorkforceFact.Incomplete(
                qualificationIdentity,
                new WorkforceUnavailableReason("QUALIFICATION_MISSING"),
                [new WorkforceEvidenceReference("QUALIFICATION", save)]);
        return new VillageWorkerProfile(
            new VillageWorkerIdentity(characterId),
            WorkforceWorkerState.Eligible,
            Versions(),
            [
                WorkforceFact.Confirmed(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.CandidateUniverseMembership),
                    WorkforceFactValue.Boolean(true),
                    save,
                    [new WorkforceEvidenceReference("WORK_CANDIDATE", save)]),
                qualificationFact
            ],
            []);
    }

    private static WorkforceSourceVersions Versions() => new(
        SaveSha,
        VerifiedVillageWorkforceRules.SupportedGameDataVersion,
        "1",
        "1",
        "1");

    private static WorkforceProvenance SaveProvenance() => new(
        WorkforceEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        "1",
        SaveSha);

    private static WorkforceProvenance GameDataProvenance() => new(
        WorkforceEvidenceSourceKind.InstalledGameData,
        "GAMEDATA",
        VerifiedVillageWorkforceRules.SupportedGameDataVersion,
        "ASSEMBLY_A");
}
