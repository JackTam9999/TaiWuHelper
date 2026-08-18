using NSubstitute;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.API.UnitTests.Presentation;

internal static class VillageWorkforcePresentationTestData
{
    private const string SaveSha =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    public static VillageWorkforceSnapshot Snapshot() =>
        Snapshot(
        [
            Worker(41001, 64),
            Worker(41002, 72),
            Worker(41003, 72),
            Worker(
                41004,
                null,
                WorkforceEvidenceState.Incomplete)
        ],
        currentCharacterId: 41001);

    public static VillageWorkforceSnapshot Snapshot(
        IEnumerable<VillageWorkerProfile> workers,
        int currentCharacterId)
    {
        var copiedWorkers = workers.ToArray();
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
            copiedWorkers,
            [target],
            [new CurrentShopManagerAssignment(
                target.Identity,
                new VillageWorkerIdentity(currentCharacterId),
                SaveProvenance())],
            []);
    }

    public static async Task<VillageWorkforceFinderResult> ResultAsync(
        VillageWorkforceSnapshot? snapshot = null)
    {
        snapshot ??= Snapshot();
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

    public static VillageWorkerProfile Worker(
        int characterId,
        short? qualification,
        WorkforceEvidenceState qualificationState =
            WorkforceEvidenceState.Confirmed,
        WorkforceWorkerState workerState = WorkforceWorkerState.Eligible,
        bool candidate = true)
    {
        var save = SaveProvenance();
        var qualificationIdentity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));
        var qualificationFact = qualificationState switch
        {
            WorkforceEvidenceState.Confirmed => WorkforceFact.Confirmed(
                qualificationIdentity,
                WorkforceFactValue.Int16(qualification
                    ?? throw new ArgumentNullException(nameof(qualification))),
                save,
                [new WorkforceEvidenceReference("QUALIFICATION", save)]),
            WorkforceEvidenceState.Incomplete => WorkforceFact.Incomplete(
                qualificationIdentity,
                new WorkforceUnavailableReason("QUALIFICATION_MISSING"),
                [new WorkforceEvidenceReference("QUALIFICATION", save)]),
            WorkforceEvidenceState.Unsupported => WorkforceFact.Unsupported(
                qualificationIdentity,
                new WorkforceUnavailableReason("QUALIFICATION_UNSUPPORTED"),
                [new WorkforceEvidenceReference("QUALIFICATION", save)]),
            WorkforceEvidenceState.Conflicting => WorkforceFact.Conflicting(
                qualificationIdentity,
                [
                    new WorkforceConflictValue(
                        WorkforceFactValue.Int16(41),
                        save),
                    new WorkforceConflictValue(
                        WorkforceFactValue.Int16(42),
                        save)
                ],
                [new WorkforceEvidenceReference("QUALIFICATION", save)]),
            _ => throw new ArgumentOutOfRangeException(
                nameof(qualificationState))
        };
        return new VillageWorkerProfile(
            new VillageWorkerIdentity(characterId),
            workerState,
            Versions(),
            [
                WorkforceFact.Confirmed(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.CandidateUniverseMembership),
                    WorkforceFactValue.Boolean(candidate),
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
        VerifiedVillageWorkforceRules.FingerprintSchemaVersion);

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
