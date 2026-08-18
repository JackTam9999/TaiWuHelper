using TaiWu.Application.Localization;
using TaiWu.Domain.VillageWorkforce;
using TaiWuAPI.Contracts.VillageWorkforce;
using TaiWuAPI.Presentation;
using Xunit;

namespace TaiWu.API.UnitTests.Presentation;

public sealed class VillageWorkforceCrossLayerParityTests
{
    [Fact]
    public async Task Api_and_presentation_preserve_every_unavailable_and_conflict_state()
    {
        var snapshot = VillageWorkforcePresentationTestData.Snapshot(
        [
            VillageWorkforcePresentationTestData.Worker(51001, 60),
            VillageWorkforcePresentationTestData.Worker(
                51002,
                null,
                WorkforceEvidenceState.Incomplete),
            VillageWorkforcePresentationTestData.Worker(
                51003,
                null,
                WorkforceEvidenceState.Unsupported),
            VillageWorkforcePresentationTestData.Worker(
                51004,
                null,
                WorkforceEvidenceState.Conflicting),
            VillageWorkforcePresentationTestData.Worker(
                51005,
                300,
                workerState: WorkforceWorkerState.Ineligible,
                candidate: false)
        ],
        currentCharacterId: 51001);
        var result = await VillageWorkforcePresentationTestData.ResultAsync(
            snapshot);

        var api = VillageWorkforceResponseMapper.Map(
            result,
            VillageWorkforceApiLanguage.English);
        var presentation = VillageWorkforceViewModelMapper.Map(
            result,
            TaiwuLanguage.English,
            targetOrdinal: 1);

        Assert.Equal(VillageWorkforceApiStatus.Partial, api.Status);
        Assert.True(presentation.IsPartial);
        Assert.Equal(api.Counts, presentation.Counts);
        var expected = new Dictionary<int, VillageWorkforceApiEvaluationState>
        {
            [51001] = VillageWorkforceApiEvaluationState.Ranked,
            [51002] = VillageWorkforceApiEvaluationState.Incomplete,
            [51003] = VillageWorkforceApiEvaluationState.Unsupported,
            [51004] = VillageWorkforceApiEvaluationState.Conflicting,
            [51005] = VillageWorkforceApiEvaluationState.Ineligible
        };
        foreach (var (characterId, state) in expected)
        {
            var apiCandidate = api.Candidates.Single(item =>
                item.CharacterId == characterId);
            var presented = presentation.Candidates.Single(item =>
                item.CharacterId == characterId);
            Assert.Equal(state, apiCandidate.EvaluationState);
            Assert.Equal(state, presented.State);
            Assert.Equal(apiCandidate.Total, presented.Total);
            Assert.False(string.IsNullOrWhiteSpace(presented.StateLabel));
        }

        var conflictApi = api.Candidates.Single(item =>
            item.CharacterId == 51004);
        var conflictView = presentation.Candidates.Single(item =>
            item.CharacterId == 51004);
        var apiConflictCount = conflictApi.Requirements.Sum(item =>
            item.Conflicts.Count);
        Assert.True(apiConflictCount >= 2);
        Assert.Equal(
            apiConflictCount,
            conflictView.Requirements.Sum(item => item.ConflictCount));
        Assert.All(
            api.Candidates.Where(item => item.CharacterId != 51001),
            item => Assert.Null(item.Total));
    }

    [Fact]
    public async Task Current_only_state_and_value_remain_descriptive_in_both_layers()
    {
        var snapshot = VillageWorkforcePresentationTestData.Snapshot(
        [
            VillageWorkforcePresentationTestData.Worker(
                52001,
                100,
                workerState: WorkforceWorkerState.CurrentOnly,
                candidate: false),
            VillageWorkforcePresentationTestData.Worker(52002, 40)
        ],
        currentCharacterId: 52001);
        var result = await VillageWorkforcePresentationTestData.ResultAsync(
            snapshot);

        var api = VillageWorkforceResponseMapper.Map(
            result,
            VillageWorkforceApiLanguage.TraditionalChinese);
        var presentation = VillageWorkforceViewModelMapper.Map(
            result,
            TaiwuLanguage.Chinese,
            targetOrdinal: 1);
        var apiCurrent = api.Candidates.Single(item => item.IsCurrent);
        var viewCurrent = presentation.Current;

        Assert.Equal(
            VillageWorkforceApiEvaluationState.CurrentOnly,
            apiCurrent.EvaluationState);
        Assert.Equal(apiCurrent.EvaluationState, viewCurrent.State);
        Assert.Equal(100m, apiCurrent.Total);
        Assert.Equal(apiCurrent.Total, viewCurrent.Total);
        Assert.False(string.IsNullOrWhiteSpace(viewCurrent.StateLabel));
        Assert.Equal(1, api.Counts?.CurrentOnly);
        Assert.Equal(1, presentation.Counts.CurrentOnly);
    }
}
