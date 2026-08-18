using GameData.Domains;
using GameData.Domains.Building;
using GameData.Domains.Character;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using TaiWu.Application.Localization;
using TaiWu.Application.SaveGames;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;

namespace TaiWu.Infrastructure.SaveGames;

internal sealed class TaiwuVillageWorkforceSnapshotReader(
    TaiwuArchiveReadSession readSession,
    ITaiwuSaveFilePathProvider saveFilePathProvider,
    TaiwuGameTextResolver gameTextResolver,
    TimeProvider timeProvider,
    ILogger<TaiwuVillageWorkforceSnapshotReader>? logger = null)
    : IVillageWorkforceSnapshotReader
{
    internal const string SupportedGameDataVersion =
        "1.0.0+3918df411fc7c67fdc7f0094ca8619eacfe9da20";
    internal const string MappingVersion = "1";
    internal const string CandidateUniverseVersion = "1";
    internal const string FingerprintSchemaVersion = "2";
    private readonly ILogger<TaiwuVillageWorkforceSnapshotReader> _logger =
        logger ?? NullLogger<TaiwuVillageWorkforceSnapshotReader>.Instance;

    public async Task<VillageWorkforceSnapshotReadResult> ReadAsync(
        VillageWorkforceSnapshotReadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var located = saveFilePathProvider.Resolve();
        if (!located.IsAvailable)
        {
            return Failure(
                VillageWorkforceSnapshotReadStatus.SaveUnavailable,
                "CONFIGURED_SAVE_UNAVAILABLE",
                "The trusted Taiwu save configuration is unavailable.");
        }

        var gameDataVersion = GetGameDataVersion();
        if (!string.Equals(
                gameDataVersion,
                SupportedGameDataVersion,
                StringComparison.Ordinal))
        {
            return Failure(
                VillageWorkforceSnapshotReadStatus.UnsupportedVersion,
                "GAMEDATA_VERSION_UNSUPPORTED",
                "The installed GameData version is not supported by the village-workforce mapping.");
        }

        try
        {
            var projection = await readSession.ReadAsync(
                    located.SaveFilePath!,
                    (context, token) => Project(
                        context,
                        gameDataVersion,
                        timeProvider.GetUtcNow(),
                        gameTextResolver,
                        token),
                    cancellationToken)
                .ConfigureAwait(false);
            if (projection.FailureStatus is { } failureStatus)
            {
                return Failure(
                    failureStatus,
                    projection.FailureIdentity!,
                    projection.FailureMessage!);
            }

            return projection.IsPartial
                ? VillageWorkforceSnapshotReadResult.Partial(
                    projection.Snapshot!,
                    projection.WorkerDisplays,
                    projection.TargetDisplays)
                : VillageWorkforceSnapshotReadResult.Complete(
                    projection.Snapshot!,
                    projection.WorkerDisplays,
                    projection.TargetDisplays);
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            return Failure(
                VillageWorkforceSnapshotReadStatus.SaveUnavailable,
                "CONFIGURED_SAVE_NOT_FOUND",
                "The configured Taiwu save file was not found.");
        }
        catch (TaiwuArchiveChangedException)
        {
            return Failure(
                VillageWorkforceSnapshotReadStatus.ChangedRevision,
                "SAVE_REVISION_CHANGED",
                "The configured save changed during the read; retry after it is stable.");
        }
        catch (Exception exception) when (IsSafeReadFailure(exception))
        {
            _logger.LogWarning(
                exception,
                "The village-workforce snapshot could not be projected safely.");
            return Failure(
                VillageWorkforceSnapshotReadStatus.ReadFailed,
                "CONFIGURED_SAVE_READ_FAILED",
                "The configured Taiwu save could not be read safely.");
        }
    }

    private static VillageWorkforceProjection Project(
        TaiwuArchiveReadContext context,
        string gameDataVersion,
        DateTimeOffset capturedAt,
        TaiwuGameTextResolver textResolver,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var traditionalChinese = textResolver.CreateContext(
            context.SaveFilePath,
            TaiwuLanguage.Chinese);
        var english = textResolver.CreateContext(
            context.SaveFilePath,
            TaiwuLanguage.English);
        var candidateEntries = DomainManager.Taiwu
            .GetVillagersForWork(
                includeUnlockedWorkingVillagers: true,
                farmerFirst: false)
            .ToArray();
        var positiveCandidateEntries = candidateEntries
            .Where(characterId => characterId > 0)
            .ToArray();
        if (positiveCandidateEntries.Length
            != positiveCandidateEntries.Distinct().Count())
        {
            return VillageWorkforceProjection.Conflicting(
                "DUPLICATE_WORK_CANDIDATE_IDENTITY",
                "The saved work-candidate source contained a duplicate identity.");
        }

        var sourceVersions = new WorkforceSourceVersions(
            context.SourceFingerprint.Sha256,
            gameDataVersion,
            MappingVersion,
            CandidateUniverseVersion,
            FingerprintSchemaVersion);
        var saveProvenance = new WorkforceProvenance(
            WorkforceEvidenceSourceKind.ConfiguredSave,
            "CONFIGURED_SAVE_ARCHIVE",
            MappingVersion,
            context.SourceFingerprint.Sha256);
        var gameDataProvenance = new WorkforceProvenance(
            WorkforceEvidenceSourceKind.InstalledGameData,
            "INSTALLED_GAMEDATA",
            gameDataVersion,
            typeof(DomainManager).Assembly.ManifestModule.ModuleVersionId
                .ToString("N"));
        var targets = new List<ShopManagerTarget>();
        var targetDisplays = new List<VillageWorkforceTargetDisplay>();
        var assignments = new List<CurrentShopManagerAssignment>();
        var seenTargets = new HashSet<ShopManagerTargetIdentity>();
        var buildingDomain = DomainManager.Building;
        foreach (var area in buildingDomain.GetTaiwuBuildingAreas()
            .OrderBy(value => value.AreaId)
            .ThenBy(value => value.BlockId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var building in buildingDomain.GetBuildingBlockList(area)
                .Where(value => value is not null
                    && value.BlockIndex >= 0
                    && value.TemplateId >= 0)
                .OrderBy(value => value.BlockIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var config = building.ConfigData;
                if (config is not
                    {
                        IsShop: true,
                        RequireLifeSkillType: >= 0 and <=
                            LifeSkillDisciplineIdentity.MaximumSupportedType
                    })
                {
                    continue;
                }

                var buildingIdentity = new ShopBuildingIdentity(
                    area.AreaId,
                    area.BlockId,
                    building.BlockIndex);
                var buildingKey = new BuildingBlockKey(
                    area.AreaId,
                    area.BlockId,
                    building.BlockIndex);
                if (!buildingDomain.TryGetElement_ShopManagerDict(
                        buildingKey,
                        out var managerList))
                {
                    continue;
                }

                var managerEntries = managerList.GetCollection();
                for (var slotIndex = 0;
                    slotIndex < managerEntries.Count;
                    slotIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var characterId = managerEntries[slotIndex];
                    if (characterId <= 0)
                    {
                        continue;
                    }

                    var targetIdentity = new ShopManagerTargetIdentity(
                        buildingIdentity,
                        slotIndex);
                    if (!seenTargets.Add(targetIdentity))
                    {
                        return VillageWorkforceProjection.Conflicting(
                            "DUPLICATE_SHOP_MANAGER_TARGET",
                            "The saved shop-manager source contained a duplicate target.");
                    }

                    targets.Add(new ShopManagerTarget(
                        targetIdentity,
                        new LifeSkillDisciplineIdentity(
                            config.RequireLifeSkillType),
                        [
                            new WorkforceEvidenceReference(
                                "SHOP_BUILDING_INSTANCE",
                                saveProvenance),
                            new WorkforceEvidenceReference(
                                "SHOP_REQUIRED_LIFE_SKILL",
                                gameDataProvenance),
                            new WorkforceEvidenceReference(
                                "SHOP_MANAGER_POSITION",
                                saveProvenance)
                        ]));
                    targetDisplays.Add(new VillageWorkforceTargetDisplay(
                        targetIdentity,
                        traditionalChinese.ResolveOptional(
                            "BuildingBlock",
                            config.Name),
                        english.ResolveOptional("BuildingBlock", config.Name),
                        traditionalChinese.ResolveLocationName(area),
                        english.ResolveLocationName(area),
                        traditionalChinese.ResolveOptional(
                            "LifeSkillType",
                            $"Name_{config.RequireLifeSkillType}"),
                        english.ResolveOptional(
                            "LifeSkillType",
                            $"Name_{config.RequireLifeSkillType}")));
                    assignments.Add(new CurrentShopManagerAssignment(
                        targetIdentity,
                        new VillageWorkerIdentity(characterId),
                        saveProvenance));
                }
            }
        }

        var candidateSet = positiveCandidateEntries.ToHashSet();
        var currentWorkerSet = assignments
            .Select(item => item.Worker.CharacterId)
            .ToHashSet();
        var requiredDisciplines = targets
            .Select(item => item.RequiredDiscipline)
            .Distinct()
            .OrderBy(item => item.Type)
            .ToArray();
        var projectedWorkers = candidateSet
            .Union(currentWorkerSet)
            .Order()
            .Select(characterId => ProjectWorker(
                characterId,
                candidateSet.Contains(characterId),
                currentWorkerSet.Contains(characterId),
                requiredDisciplines,
                sourceVersions,
                saveProvenance,
                gameDataProvenance,
                cancellationToken))
            .ToArray();
        var workers = projectedWorkers.Select(item => item.Profile).ToArray();
        var workerDisplays = projectedWorkers.Select(item => ProjectWorkerDisplay(
            item.Profile.Identity,
            traditionalChinese,
            english,
            item.Capability)).ToArray();
        var diagnostics = new List<WorkforceDiagnostic>();
        if (candidateEntries.Any(characterId => characterId <= 0))
        {
            diagnostics.Add(new WorkforceDiagnostic(
                "INVALID_WORK_CANDIDATE_IDENTITY_OMITTED",
                WorkforceDiagnosticSeverity.Warning,
                []));
        }

        if (context.LoadWarning is not null)
        {
            diagnostics.Add(new WorkforceDiagnostic(
                "STANDALONE_RUNTIME_BOUNDARY_REACHED",
                WorkforceDiagnosticSeverity.Information,
                []));
        }

        var snapshot = new VillageWorkforceSnapshot(
            new SettlementIdentity(
                DomainManager.Taiwu.GetTaiwuVillageSettlementId()),
            capturedAt,
            sourceVersions,
            workers,
            targets,
            assignments,
            diagnostics);
        var isPartial = workers.Any(item => item.State is
                WorkforceWorkerState.Incomplete
                or WorkforceWorkerState.Unsupported
                or WorkforceWorkerState.Conflicting)
            || candidateEntries.Any(characterId => characterId <= 0);
        return VillageWorkforceProjection.Success(
            snapshot,
            workerDisplays,
            targetDisplays,
            isPartial);
    }

    private static ProjectedWorker ProjectWorker(
        int characterId,
        bool isCandidate,
        bool isCurrent,
        IReadOnlyList<LifeSkillDisciplineIdentity> requiredDisciplines,
        WorkforceSourceVersions sourceVersions,
        WorkforceProvenance saveProvenance,
        WorkforceProvenance gameDataProvenance,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var facts = new List<WorkforceFact>
        {
            WorkforceFact.Confirmed(
                new WorkforceFactIdentity(
                    WorkforceFactKind.CandidateUniverseMembership),
                WorkforceFactValue.Boolean(isCandidate),
                saveProvenance,
                [new WorkforceEvidenceReference(
                    "TAIWU_WORK_CANDIDATE_RESULT",
                    saveProvenance)]),
            WorkforceFact.Confirmed(
                new WorkforceFactIdentity(
                    WorkforceFactKind.CurrentAssignmentMembership),
                WorkforceFactValue.Boolean(isCurrent),
                saveProvenance,
                [new WorkforceEvidenceReference(
                    "SHOP_MANAGER_ASSIGNMENT_RESULT",
                    saveProvenance)])
        };
        if (!DomainManager.Character.TryGetElement_Objects(
                characterId,
                out Character character))
        {
            facts.AddRange(requiredDisciplines.Select(discipline =>
                WorkforceFact.Incomplete(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.BaseLifeSkillQualification,
                        discipline),
                    new WorkforceUnavailableReason(
                        "CHARACTER_PROFILE_MISSING"),
                    [])));
            return new ProjectedWorker(
                new VillageWorkerProfile(
                    new VillageWorkerIdentity(characterId),
                    WorkforceWorkerState.Incomplete,
                    sourceVersions,
                    facts,
                    [new WorkforceDiagnostic(
                        "CHARACTER_PROFILE_MISSING",
                        WorkforceDiagnosticSeverity.Error,
                        [])]),
                null);
        }

        var unsupported = false;
        var baseLifeSkillQualifications =
            character.GetBaseLifeSkillQualifications();
        VillageWorkerCapabilityDisplay? capability = null;
        try
        {
            var mainSource = character.GetBaseMainAttributes();
            var mainAttributes = new short[
                VillageWorkerCapabilityDisplay.MainAttributeCount];
            for (sbyte type = 0; type < mainAttributes.Length; type++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                mainAttributes[type] = mainSource[type];
            }

            var martialSource = character.GetBaseCombatSkillQualifications();
            var martialDisciplines = new short[
                VillageWorkerCapabilityDisplay.MartialDisciplineCount];
            for (sbyte type = 0; type < martialDisciplines.Length; type++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                martialDisciplines[type] = martialSource[type];
            }

            var lifeSkillDisciplines = new short[
                VillageWorkerCapabilityDisplay.LifeSkillDisciplineCount];
            for (sbyte type = 0; type < lifeSkillDisciplines.Length; type++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lifeSkillDisciplines[type] = baseLifeSkillQualifications[type];
            }

            capability = new VillageWorkerCapabilityDisplay(
                new VillageWorkerIdentity(characterId),
                mainAttributes,
                martialDisciplines,
                lifeSkillDisciplines);
        }
        catch (Exception exception) when (exception is
            IndexOutOfRangeException
                or ArgumentOutOfRangeException
                or InvalidOperationException
                or NullReferenceException)
        {
            capability = null;
        }

        foreach (var discipline in requiredDisciplines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var value = baseLifeSkillQualifications[discipline.Type];
                facts.Add(WorkforceFact.Confirmed(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.BaseLifeSkillQualification,
                        discipline),
                    WorkforceFactValue.Int16(value),
                    saveProvenance,
                    [
                        new WorkforceEvidenceReference(
                            "BASE_LIFE_SKILL_QUALIFICATION",
                            saveProvenance),
                        new WorkforceEvidenceReference(
                            "LIFE_SKILL_DISCIPLINE_MAPPING",
                            gameDataProvenance)
                    ]));
            }
            catch (Exception exception) when (
                exception is IndexOutOfRangeException
                    or InvalidOperationException
                    or NullReferenceException)
            {
                unsupported = true;
                facts.Add(WorkforceFact.Unsupported(
                    new WorkforceFactIdentity(
                        WorkforceFactKind.BaseLifeSkillQualification,
                        discipline),
                    new WorkforceUnavailableReason(
                        "BASE_QUALIFICATION_UNSUPPORTED"),
                    []));
            }
        }

        var state = unsupported
            ? WorkforceWorkerState.Unsupported
            : isCandidate
                ? WorkforceWorkerState.Eligible
                : WorkforceWorkerState.CurrentOnly;
        return new ProjectedWorker(
            new VillageWorkerProfile(
                new VillageWorkerIdentity(characterId),
                state,
                sourceVersions,
                facts,
                unsupported
                    ? [new WorkforceDiagnostic(
                        "BASE_QUALIFICATION_UNSUPPORTED",
                        WorkforceDiagnosticSeverity.Error,
                        [])]
                    : []),
            capability);
    }

    private static VillageWorkerDisplay ProjectWorkerDisplay(
        VillageWorkerIdentity identity,
        TaiwuGameTextContext traditionalChinese,
        TaiwuGameTextContext english,
        VillageWorkerCapabilityDisplay? capability)
    {
        try
        {
            if (!DomainManager.Character.TryGetElement_Objects(
                    identity.CharacterId,
                    out Character character))
            {
                return new VillageWorkerDisplay(
                    identity,
                    null,
                    null,
                    null,
                    null,
                    capability);
            }

            var location = character.GetLocation();
            return new VillageWorkerDisplay(
                identity,
                SafeName(traditionalChinese.ResolveCharacterName(character)),
                SafeName(english.ResolveCharacterName(character)),
                traditionalChinese.ResolveLocationName(location),
                english.ResolveLocationName(location),
                capability);
        }
        catch (Exception exception) when (IsSafeReadFailure(exception))
        {
            return new VillageWorkerDisplay(
                identity,
                null,
                null,
                null,
                null,
                capability);
        }
    }

    private static string? SafeName(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Contains("Name_", StringComparison.Ordinal)
        || value.Contains("SurName_", StringComparison.Ordinal)
            ? null
            : value.Trim();

    private static VillageWorkforceSnapshotReadResult Failure(
        VillageWorkforceSnapshotReadStatus status,
        string identity,
        string message) =>
        VillageWorkforceSnapshotReadResult.Failed(
            status,
            identity,
            message);

    private static string GetGameDataVersion()
    {
        var version = FileVersionInfo.GetVersionInfo(
                typeof(DomainManager).Assembly.Location)
            .ProductVersion;
        return string.IsNullOrWhiteSpace(version) ? "unknown" : version;
    }

    private static bool IsSafeReadFailure(Exception exception) =>
        exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException
            or KeyNotFoundException
            or ArgumentException
            or InvalidOperationException;

    private sealed record VillageWorkforceProjection(
        VillageWorkforceSnapshot? Snapshot,
        IReadOnlyList<VillageWorkerDisplay> WorkerDisplays,
        IReadOnlyList<VillageWorkforceTargetDisplay> TargetDisplays,
        bool IsPartial,
        VillageWorkforceSnapshotReadStatus? FailureStatus,
        string? FailureIdentity,
        string? FailureMessage)
    {
        public static VillageWorkforceProjection Success(
            VillageWorkforceSnapshot snapshot,
            IReadOnlyList<VillageWorkerDisplay> workerDisplays,
            IReadOnlyList<VillageWorkforceTargetDisplay> targetDisplays,
            bool isPartial) =>
            new(
                snapshot,
                workerDisplays,
                targetDisplays,
                isPartial,
                null,
                null,
                null);

        public static VillageWorkforceProjection Conflicting(
            string identity,
            string message) =>
            new(
                null,
                [],
                [],
                false,
                VillageWorkforceSnapshotReadStatus.ConflictingSources,
                identity,
                message);
    }

    private sealed record ProjectedWorker(
        VillageWorkerProfile Profile,
        VillageWorkerCapabilityDisplay? Capability);
}
