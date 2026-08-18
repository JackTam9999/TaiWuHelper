using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Domain.UnitTests.VillageWorkforce;

internal static class VillageWorkforceFixtures
{
    public static readonly WorkforceSourceVersions Versions = new(
        new string('A', 64),
        VerifiedVillageWorkforceRules.SupportedGameDataVersion,
        "1",
        "1",
        VerifiedVillageWorkforceRules.FingerprintSchemaVersion);

    public static readonly WorkforceProvenance SaveProvenance = new(
        WorkforceEvidenceSourceKind.ConfiguredSave,
        "CONFIGURED_SAVE",
        "1",
        new string('A', 64));

    public static readonly WorkforceProvenance GameDataProvenance = new(
        WorkforceEvidenceSourceKind.InstalledGameData,
        "GAMEDATA",
        VerifiedVillageWorkforceRules.SupportedGameDataVersion,
        "ASSEMBLY_A");

    public static WorkforceEvidenceReference SaveEvidence(string identity) =>
        new(identity, SaveProvenance);

    public static WorkforceEvidenceReference GameDataEvidence(
        string identity) => new(identity, GameDataProvenance);

    public static ShopManagerTarget Target(
        short buildingIndex = 7,
        int slotIndex = 0,
        sbyte discipline = 6) => new(
        new ShopManagerTargetIdentity(
            new ShopBuildingIdentity(1, 2, buildingIndex),
            slotIndex),
        new LifeSkillDisciplineIdentity(discipline),
        [GameDataEvidence("SHOP_TARGET")]);

    public static VillageWorkerProfile Worker(
        int characterId,
        short qualification,
        WorkforceWorkerState state = WorkforceWorkerState.Eligible,
        sbyte discipline = 6)
    {
        var candidateIdentity = new WorkforceFactIdentity(
            WorkforceFactKind.CandidateUniverseMembership);
        var qualificationIdentity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(discipline));
        return new VillageWorkerProfile(
            new VillageWorkerIdentity(characterId),
            state,
            Versions,
            [
                WorkforceFact.Confirmed(
                    candidateIdentity,
                    WorkforceFactValue.Boolean(
                        state == WorkforceWorkerState.Eligible),
                    SaveProvenance,
                    [SaveEvidence("WORK_CANDIDATE")]),
                WorkforceFact.Confirmed(
                    qualificationIdentity,
                    WorkforceFactValue.Int16(qualification),
                    SaveProvenance,
                    [SaveEvidence("BASE_LIFE_SKILL_QUALIFICATION")])
            ],
            []);
    }

    public static VillageWorkforceSnapshot Snapshot(
        IEnumerable<VillageWorkerProfile>? workers = null,
        ShopManagerTarget? target = null,
        VillageWorkerIdentity? currentWorker = null,
        DateTimeOffset? capturedAt = null)
    {
        var selectedTarget = target ?? Target();
        var selectedWorkers = (workers ?? [Worker(101, 60), Worker(202, 80)])
            .ToArray();
        var selectedCurrent = currentWorker ?? selectedWorkers[0].Identity;
        return new VillageWorkforceSnapshot(
            new SettlementIdentity(12),
            capturedAt ?? new DateTimeOffset(
                2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
            Versions,
            selectedWorkers,
            [selectedTarget],
            [new CurrentShopManagerAssignment(
                selectedTarget.Identity,
                selectedCurrent,
                SaveProvenance)],
            []);
    }

    public static WorkforceEvaluation RankedEvaluation(
        VillageWorkforceSnapshot snapshot,
        VillageWorkerIdentity worker,
        short qualification,
        WorkforceEvaluationState state = WorkforceEvaluationState.Ranked)
    {
        var target = snapshot.Targets.Single();
        var resultIdentity = new WorkforceResultIdentity(
            snapshot.Fingerprint,
            new WorkforceObjectiveIdentity(
                WorkforceObjectiveKind.ShopManagerBaseLifeSkillQualification,
                "1"),
            new WorkforceRuleVersion("1.0.0"),
            target.Identity);
        var evidence = new[] { SaveEvidence("QUALIFICATION") };
        var requirements = Enum.GetValues<WorkforceRequirementKind>()
            .Select(requirement => new WorkforceRequirementEvaluation(
                requirement,
                WorkforceRequirementOutcome.Passed,
                "PASSED",
                evidence));
        var component = new WorkforceScoreComponent(
            new WorkforceComponentIdentity(
                WorkforceComponentKind.RequiredBaseLifeSkillQualification,
                target.RequiredDiscipline),
            qualification,
            qualification,
            1m,
            qualification,
            "QUALIFICATION_EXACT_VALUE",
            evidence);
        return new WorkforceEvaluation(
            resultIdentity,
            worker,
            WorkforceWorkerState.Eligible,
            state,
            requirements,
            [component],
            new WorkforceResultValue(
                WorkforceUnit.BaseQualificationPoint,
                qualification),
            "QUALIFICATION_AVAILABLE");
    }
}
