using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Domain.UnitTests.VillageWorkforce;

public sealed class VillageWorkforceEvaluationTests
{
    [Fact]
    public void Ranked_evaluation_requires_passed_gates_component_and_result()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var valid = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            60);

        Assert.True(valid.IsRankable);
        Assert.Equal(WorkforceEvaluationState.Ranked, valid.State);
        Assert.Equal(60m, valid.Result?.Value);
        Assert.Single(valid.Components);

        Assert.Throws<ArgumentException>(() => new WorkforceEvaluation(
            valid.ResultIdentity,
            valid.Worker,
            WorkforceWorkerState.Eligible,
            WorkforceEvaluationState.Ranked,
            valid.Requirements,
            [],
            null,
            "INVALID"));
    }

    [Fact]
    public void Evaluation_rejects_duplicate_requirements_and_components()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var valid = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            60);

        Assert.Throws<ArgumentException>(() => new WorkforceEvaluation(
            valid.ResultIdentity,
            valid.Worker,
            valid.WorkerState,
            valid.State,
            [valid.Requirements[0], valid.Requirements[0]],
            valid.Components,
            valid.Result,
            valid.OutcomeIdentity));
        Assert.Throws<ArgumentException>(() => new WorkforceEvaluation(
            valid.ResultIdentity,
            valid.Worker,
            valid.WorkerState,
            valid.State,
            valid.Requirements,
            [valid.Components[0], valid.Components[0]],
            valid.Result,
            valid.OutcomeIdentity));
    }

    [Fact]
    public void Score_component_rejects_hidden_normalization_or_weighting()
    {
        var identity = new WorkforceComponentIdentity(
            WorkforceComponentKind.RequiredBaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));

        Assert.Throws<ArgumentException>(() => new WorkforceScoreComponent(
            identity,
            60,
            0.6m,
            1m,
            0.6m,
            "QUALIFICATION_EXACT_VALUE",
            []));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new WorkforceScoreComponent(
                identity,
                60,
                60m,
                2m,
                120m,
                "QUALIFICATION_EXACT_VALUE",
                []));
    }

    [Fact]
    public void Comparison_uses_exact_result_values()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var lower = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            60);
        var higher = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[1].Identity,
            80);
        var tied = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[1].Identity,
            60,
            WorkforceEvaluationState.Tied);

        Assert.Equal(
            WorkforceComparisonOutcome.Lower,
            new WorkforceComparison(lower, higher).Outcome);
        Assert.Equal(
            WorkforceComparisonOutcome.Higher,
            new WorkforceComparison(higher, lower).Outcome);
        Assert.Equal(
            WorkforceComparisonOutcome.Equal,
            new WorkforceComparison(lower, tied).Outcome);
    }

    [Theory]
    [InlineData(
        WorkforceEvaluationState.Incomplete,
        WorkforceWorkerState.Incomplete,
        WorkforceComparisonOutcome.Unavailable)]
    [InlineData(
        WorkforceEvaluationState.Unsupported,
        WorkforceWorkerState.Unsupported,
        WorkforceComparisonOutcome.Unavailable)]
    [InlineData(
        WorkforceEvaluationState.Conflicting,
        WorkforceWorkerState.Conflicting,
        WorkforceComparisonOutcome.Conflicting)]
    public void Comparison_preserves_unavailable_and_conflicting_states(
        WorkforceEvaluationState state,
        WorkforceWorkerState workerState,
        WorkforceComparisonOutcome expected)
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var ranked = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            60);
        var unavailable = new WorkforceEvaluation(
            ranked.ResultIdentity,
            snapshot.Workers[1].Identity,
            workerState,
            state,
            [new WorkforceRequirementEvaluation(
                WorkforceRequirementKind.CharacterProfileAvailable,
                state == WorkforceEvaluationState.Conflicting
                    ? WorkforceRequirementOutcome.Conflicting
                    : WorkforceRequirementOutcome.Incomplete,
                "VALUE_UNAVAILABLE",
                [])],
            [],
            null,
            "VALUE_UNAVAILABLE");

        Assert.Equal(
            expected,
            new WorkforceComparison(ranked, unavailable).Outcome);
    }

    [Fact]
    public void Comparison_rejects_different_results_or_same_worker()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var first = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            60);
        var sameWorker = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            61);
        var differentSnapshot = VillageWorkforceFixtures.Snapshot(
            capturedAt: snapshot.CapturedAt.AddSeconds(1));
        var differentResult = VillageWorkforceFixtures.RankedEvaluation(
            differentSnapshot,
            differentSnapshot.Workers[1].Identity,
            80);

        Assert.Throws<ArgumentException>(
            () => new WorkforceComparison(first, sameWorker));
        Assert.Throws<ArgumentException>(
            () => new WorkforceComparison(first, differentResult));
    }

    [Fact]
    public void Evaluation_fingerprint_is_order_independent_and_fact_sensitive()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var first = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            60);
        var reordered = new WorkforceEvaluation(
            first.ResultIdentity,
            first.Worker,
            first.WorkerState,
            first.State,
            first.Requirements.Reverse(),
            first.Components.Reverse(),
            first.Result,
            first.OutcomeIdentity);
        var changed = VillageWorkforceFixtures.RankedEvaluation(
            snapshot,
            snapshot.Workers[0].Identity,
            61);

        Assert.Equal(first.Fingerprint, reordered.Fingerprint);
        Assert.NotEqual(first.Fingerprint, changed.Fingerprint);
    }
}
