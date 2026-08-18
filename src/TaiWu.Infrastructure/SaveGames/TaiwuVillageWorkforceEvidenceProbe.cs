using GameData.Domains;
using GameData.Domains.Building;
using System.Security.Cryptography;
using System.Text;

namespace TaiWu.Infrastructure.SaveGames;

internal static class TaiwuVillageWorkforceEvidenceProbe
{
    internal static VillageWorkforceProbe Project(
        TaiwuArchiveReadContext context,
        CancellationToken cancellationToken)
    {
        var buildingDomain = DomainManager.Building;
        var taiwuDomain = DomainManager.Taiwu;
        var areas = buildingDomain.GetTaiwuBuildingAreas()
            .OrderBy(location => location.AreaId)
            .ThenBy(location => location.BlockId)
            .ToArray();
        List<(BuildingBlockKey Key, BuildingBlockData Data)> buildings = [];
        foreach (var area in areas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            buildings.AddRange(buildingDomain.GetBuildingBlockList(area)
                .Where(data => data is not null
                    && data.TemplateId >= 0
                    && data.BlockIndex >= 0)
                .Select(data => (
                    new BuildingBlockKey(
                        area.AreaId,
                        area.BlockId,
                        data.BlockIndex),
                    data)));
        }

        var availableWorkers = taiwuDomain
            .GetVillagersForWork(
                includeUnlockedWorkingVillagers: true,
                farmerFirst: false)
            .Where(characterId => characterId > 0)
            .Distinct()
            .Order()
            .ToArray();
        var broadlyAvailableWorkers = taiwuDomain
            .GetAllVillagersAvailableForWork(
                actuallyNotOccupiedOnly: false)
            .Where(characterId => characterId > 0)
            .Distinct()
            .Order()
            .ToArray();
        var currentWork = taiwuDomain.GetVillagerWorkDict();
        var shopTargets = buildings
            .Where(value => value.Data.ConfigData is
            {
                IsShop: true,
                RequireLifeSkillType: >= 0
            })
            .OrderBy(value => value.Key.AreaId)
            .ThenBy(value => value.Key.BlockId)
            .ThenBy(value => value.Key.BuildingBlockIndex)
            .ToArray();

        var availableWorkerSet = availableWorkers.ToHashSet();
        List<string> signatureFacts = [];
        List<short> qualifications = [];
        var evaluatedPairs = 0;
        var failedPairs = 0;
        var targetsWithCurrentManagers = 0;
        var currentManagerCount = 0;
        var comparableTargetCount = 0;
        var currentEfficiencyValues = 0;
        var alternativeEfficiencySentinels = 0;
        Dictionary<sbyte, int> targetSkillTypes = [];
        Dictionary<string, int> failureTypes = new(StringComparer.Ordinal);
        Dictionary<string, int> efficiencyFailureTypes =
            new(StringComparer.Ordinal);
        foreach (var target in shopTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentManagers =
                buildingDomain.TryGetElement_ShopManagerDict(
                    target.Key,
                    out var managerList)
                    ? managerList.GetCollection()
                        .Where(characterId => characterId > 0)
                        .Distinct()
                        .Order()
                        .ToArray()
                    : [];
            if (currentManagers.Length > 0)
            {
                targetsWithCurrentManagers++;
                currentManagerCount += currentManagers.Length;
            }

            var currentManagerSet = currentManagers.ToHashSet();
            var candidates = availableWorkers
                .Concat(currentManagers)
                .Distinct()
                .Order()
                .ToArray();
            List<short> targetQualifications = [];
            var requiredLifeSkillType =
                target.Data.ConfigData.RequireLifeSkillType;
            targetSkillTypes[requiredLifeSkillType] =
                targetSkillTypes.GetValueOrDefault(requiredLifeSkillType) + 1;
            foreach (var characterId in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var character = DomainManager.Character
                        .GetElement_Objects(characterId);
                    var qualification = character
                        .GetBaseLifeSkillQualifications()[
                            requiredLifeSkillType];
                    qualifications.Add(qualification);
                    targetQualifications.Add(qualification);
                    evaluatedPairs++;
                    int? efficiency = null;
                    try
                    {
                        efficiency = buildingDomain
                            .CalcTaiwuVillagerEfficiencyInBuilding(
                                target.Key,
                                characterId);
                        if (currentManagerSet.Contains(characterId)
                            && efficiency >= 0)
                        {
                            currentEfficiencyValues++;
                        }
                        else if (!currentManagerSet.Contains(characterId)
                            && efficiency < 0)
                        {
                            alternativeEfficiencySentinels++;
                        }
                    }
                    catch (Exception exception)
                    {
                        var failureType = exception.GetType().FullName
                            ?? exception.GetType().Name;
                        efficiencyFailureTypes[failureType] =
                            efficiencyFailureTypes.GetValueOrDefault(
                                failureType) + 1;
                    }

                    signatureFacts.Add(
                        $"{target.Key.AreaId}:{target.Key.BlockId}:"
                        + $"{target.Key.BuildingBlockIndex}:{characterId}:"
                        + $"{requiredLifeSkillType}:{qualification}:"
                        + $"{availableWorkerSet.Contains(characterId)}:"
                        + $"{currentManagerSet.Contains(characterId)}:"
                        + $"{efficiency}");
                }
                catch (Exception exception)
                {
                    failedPairs++;
                    var failureType = exception.GetType().FullName
                        ?? exception.GetType().Name;
                    failureTypes[failureType] =
                        failureTypes.GetValueOrDefault(failureType) + 1;
                    signatureFacts.Add(
                        $"{target.Key.AreaId}:{target.Key.BlockId}:"
                        + $"{target.Key.BuildingBlockIndex}:{characterId}:"
                        + exception.GetType().FullName);
                }
            }

            if (targetQualifications.Distinct().Count() > 1)
            {
                comparableTargetCount++;
            }
        }

        var workTypeProfile = string.Join(
            ',',
            currentWork.Values
                .GroupBy(value => value.WorkType)
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Key}:{group.Count()}"));
        var targetSkillProfile = string.Join(
            ',',
            targetSkillTypes
                .OrderBy(value => value.Key)
                .Select(value => $"{value.Key}:{value.Value}"));
        var failureTypeProfile = string.Join(
            ',',
            failureTypes
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => $"{value.Key}:{value.Value}"));
        var efficiencyFailureTypeProfile = string.Join(
            ',',
            efficiencyFailureTypes
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => $"{value.Key}:{value.Value}"));
        signatureFacts.Add($"workTypes:{workTypeProfile}");
        signatureFacts.Add($"targetSkills:{targetSkillProfile}");
        signatureFacts.AddRange(currentWork
            .OrderBy(value => value.Key)
            .Select(value =>
                $"work:{value.Key}:{value.Value.WorkType}:"
                + $"{value.Value.AreaId}:{value.Value.BlockId}:"
                + $"{value.Value.BuildingBlockIndex}:"
                + $"{value.Value.WorkerIndex}"));
        signatureFacts.AddRange(broadlyAvailableWorkers.Select(value =>
            $"available:{value}"));
        var signature = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(string.Join('\n', signatureFacts))));

        return new VillageWorkforceProbe(
            typeof(DomainManager).Assembly.GetName().Version?.ToString()
                ?? "unknown",
            typeof(BuildingBlockData).Assembly.GetName().Version?.ToString()
                ?? "unknown",
            context.SourceFingerprint.Sha256,
            areas.Length,
            buildings.Count,
            availableWorkers.Length,
            broadlyAvailableWorkers.Length,
            currentWork.Count,
            workTypeProfile,
            shopTargets.Length,
            targetSkillProfile,
            targetsWithCurrentManagers,
            currentManagerCount,
            evaluatedPairs,
            failedPairs,
            failureTypeProfile,
            comparableTargetCount,
            currentEfficiencyValues,
            alternativeEfficiencySentinels,
            efficiencyFailureTypes.Values.Sum(),
            efficiencyFailureTypeProfile,
            qualifications.Count == 0 ? null : qualifications.Min(),
            qualifications.Count == 0 ? null : qualifications.Max(),
            qualifications.Distinct().Count(),
            signature);
    }
}

internal sealed record VillageWorkforceProbe(
    string GameDataVersion,
    string SharedVersion,
    string SaveSha256,
    int AreaCount,
    int NonEmptyBuildingCount,
    int AvailableWorkerCount,
    int BroadlyAvailableWorkerCount,
    int CurrentWorkRecordCount,
    string WorkTypeProfile,
    int ShopTargetCount,
    string TargetSkillProfile,
    int ShopTargetsWithCurrentManagers,
    int CurrentManagerCount,
    int EvaluatedPairCount,
    int EvaluationFailureCount,
    string FailureTypeProfile,
    int ComparableTargetCount,
    int CurrentEfficiencyValueCount,
    int AlternativeEfficiencySentinelCount,
    int EfficiencyFailureCount,
    string EfficiencyFailureTypeProfile,
    short? MinimumQualification,
    short? MaximumQualification,
    int DistinctQualificationCount,
    string Signature);
