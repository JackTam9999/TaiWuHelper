using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Domain.UnitTests.VillageWorkforce;

public sealed class VillageWorkforceShortlistTests
{
    [Fact]
    public void Alternative_can_rank_above_current_without_reordering_facts()
    {
        var current = Profile(101, 60);
        var alternative = Profile(202, 80);
        var shortlist = CreateShortlist(
            [current, alternative],
            current.Identity);

        Assert.Equal(current.Identity, shortlist.CurrentEvaluation.Worker);
        Assert.Equal(
            [202, 101],
            shortlist.Comparable.Select(item =>
                item.Evaluation.Worker.CharacterId));
        Assert.Equal([1, 2], shortlist.Comparable.Select(item =>
            item.CompetitionRank));
    }

    [Fact]
    public void Current_can_remain_best_without_becoming_a_hidden_bonus()
    {
        var current = Profile(101, 100);
        var alternative = Profile(202, 40);
        var shortlist = CreateShortlist(
            [alternative, current],
            current.Identity);

        Assert.Equal(101, shortlist.Comparable[0]
            .Evaluation.Worker.CharacterId);
        Assert.Equal(1, shortlist.Comparable[0].CompetitionRank);
        Assert.Equal(100m, shortlist.Comparable[0]
            .Evaluation.Result?.Value);
    }

    [Fact]
    public void Exact_ties_use_competition_rank_without_identity_tiebreak()
    {
        var first = Profile(101, 80);
        var second = Profile(202, 80);
        var third = Profile(303, 60);
        var shortlist = CreateShortlist(
            [third, second, first],
            first.Identity);

        Assert.Equal(
            [1, 1, 3],
            shortlist.Comparable.Select(item => item.CompetitionRank));
        Assert.Equal(
            [101, 202, 303],
            shortlist.Comparable.Select(item =>
                item.Evaluation.Worker.CharacterId));
        Assert.Equal(
            [
                WorkforceEvaluationState.Tied,
                WorkforceEvaluationState.Tied,
                WorkforceEvaluationState.Ranked
            ],
            shortlist.Comparable.Select(item => item.Evaluation.State));
    }

    [Fact]
    public void Groups_counts_and_filters_preserve_canonical_result()
    {
        var eligible = Profile(101, 60);
        var currentOnly = Profile(
            202,
            90,
            WorkforceWorkerState.CurrentOnly,
            candidate: false);
        var ineligible = Profile(
            303,
            300,
            WorkforceWorkerState.Ineligible,
            candidate: false);
        var incomplete = Profile(
            404,
            null,
            qualificationState: WorkforceEvidenceState.Incomplete);
        var unsupported = Profile(
            505,
            null,
            qualificationState: WorkforceEvidenceState.Unsupported);
        var conflicting = Profile(
            606,
            null,
            qualificationState: WorkforceEvidenceState.Conflicting);
        var shortlist = CreateShortlist(
            [
                conflicting,
                unsupported,
                incomplete,
                ineligible,
                currentOnly,
                eligible
            ],
            currentOnly.Identity);

        Assert.Equal(6, shortlist.Counts.Total);
        Assert.Equal(1, shortlist.Counts.Comparable);
        Assert.Equal(1, shortlist.Counts.CurrentOnly);
        Assert.Equal(1, shortlist.Counts.Ineligible);
        Assert.Equal(1, shortlist.Counts.Incomplete);
        Assert.Equal(1, shortlist.Counts.Unsupported);
        Assert.Equal(1, shortlist.Counts.Conflicting);
        Assert.Single(shortlist.CurrentOnly);
        Assert.Single(shortlist.Ineligible);
        Assert.Single(shortlist.Incomplete);
        Assert.Single(shortlist.Unsupported);
        Assert.Single(shortlist.Conflicting);

        var all = shortlist.ApplyFilter(WorkforceShortlistFilter.All);
        var comparable = shortlist.ApplyFilter(
            WorkforceShortlistFilter.Comparable);
        var needsReview = shortlist.ApplyFilter(
            WorkforceShortlistFilter.NeedsReview);
        var filteredIneligible = shortlist.ApplyFilter(
            WorkforceShortlistFilter.Ineligible);
        Assert.Equal(6, all.VisibleEvaluations.Length);
        Assert.Single(comparable.VisibleEvaluations);
        Assert.Equal(3, needsReview.VisibleEvaluations.Length);
        Assert.Single(filteredIneligible.VisibleEvaluations);
        Assert.All(
            new[] { all, comparable, needsReview, filteredIneligible },
            view =>
            {
                Assert.Equal(shortlist.Fingerprint, view.ResultFingerprint);
                Assert.Same(shortlist.Counts, view.UnfilteredCounts);
            });
    }

    [Fact]
    public void One_current_only_worker_yields_empty_comparable_replacement_set()
    {
        var current = Profile(
            101,
            90,
            WorkforceWorkerState.CurrentOnly,
            candidate: false);
        var shortlist = CreateShortlist([current], current.Identity);

        Assert.Empty(shortlist.Comparable);
        Assert.Equal(1, shortlist.Counts.Total);
        Assert.Equal(0, shortlist.Counts.Comparable);
        Assert.Equal(
            WorkforceVacancyState.NoExplicitVacancy,
            shortlist.VacancyState);
        Assert.Throws<ArgumentException>(
            () => shortlist.CreateManualPlan(current.Identity));
    }

    [Fact]
    public void One_eligible_worker_remains_a_single_ranked_current_result()
    {
        var current = Profile(101, 60);
        var shortlist = CreateShortlist([current], current.Identity);

        var ranked = Assert.Single(shortlist.Comparable);
        Assert.Equal(1, ranked.CompetitionRank);
        Assert.Equal(current.Identity, ranked.Evaluation.Worker);
    }

    [Fact]
    public void Manual_plan_is_factual_and_has_no_completion_state()
    {
        var current = Profile(101, 60);
        var alternative = Profile(202, 80);
        var shortlist = CreateShortlist(
            [current, alternative],
            current.Identity);

        var plan = shortlist.CreateManualPlan(alternative.Identity);

        Assert.Equal(current.Identity, plan.CurrentWorker);
        Assert.Equal(alternative.Identity, plan.ProposedAssignment.Worker);
        Assert.Equal(
            WorkforceAssignmentOrigin.ProposedHelper,
            plan.ProposedAssignment.Origin);
        Assert.Equal(
            Enum.GetValues<WorkforceChecklistItemKind>(),
            plan.Checklist.Select(item => item.Kind));
        Assert.Contains(plan.Checklist, item =>
            item.Category == WorkforceChecklistCategory.Prerequisite);
        Assert.Contains(plan.Checklist, item =>
            item.Category == WorkforceChecklistCategory.FactToVerify);
        Assert.Contains(plan.Checklist, item =>
            item.Category == WorkforceChecklistCategory.Caution);
        Assert.DoesNotContain(
            typeof(WorkforceManualChecklistItem).GetProperties(),
            property => property.PropertyType == typeof(bool));
    }

    [Fact]
    public void Comparison_preserves_available_unavailable_and_not_comparable()
    {
        var higher = Profile(101, 80);
        var lower = Profile(202, 60);
        var incomplete = Profile(
            303,
            null,
            qualificationState: WorkforceEvidenceState.Incomplete);
        var ineligible = Profile(
            404,
            300,
            WorkforceWorkerState.Ineligible,
            candidate: false);
        var shortlist = CreateShortlist(
            [higher, lower, incomplete, ineligible],
            higher.Identity);

        Assert.Equal(
            WorkforceComparisonOutcome.Higher,
            shortlist.Compare(higher.Identity, lower.Identity).Outcome);
        Assert.Equal(
            WorkforceComparisonOutcome.Lower,
            shortlist.Compare(lower.Identity, higher.Identity).Outcome);
        Assert.Equal(
            WorkforceComparisonOutcome.Unavailable,
            shortlist.Compare(higher.Identity, incomplete.Identity).Outcome);
        Assert.Equal(
            WorkforceComparisonOutcome.NotComparable,
            shortlist.Compare(higher.Identity, ineligible.Identity).Outcome);
    }

    [Fact]
    public void Evaluation_set_cannot_lose_the_selected_current_worker()
    {
        var current = Profile(101, 60);
        var alternative = Profile(202, 80);
        var snapshot = VillageWorkforceFixtures.Snapshot(
            [current, alternative],
            currentWorker: current.Identity);
        var rule = ResolveRule(snapshot.Targets[0].RequiredDiscipline);
        var result = VillageWorkforceEvaluator.Evaluate(
            snapshot,
            snapshot.Targets[0].Identity,
            rule);

        Assert.Throws<ArgumentException>(() =>
            new VillageWorkforceEvaluationSet(
                result.ResultIdentity,
                result.Rule,
                new VillageWorkerIdentity(999),
                result.Evaluations));
    }

    private static VillageWorkforceShortlist CreateShortlist(
        IEnumerable<VillageWorkerProfile> workers,
        VillageWorkerIdentity currentWorker)
    {
        var snapshot = VillageWorkforceFixtures.Snapshot(
            workers,
            currentWorker: currentWorker);
        var rule = ResolveRule(snapshot.Targets[0].RequiredDiscipline);
        return new VillageWorkforceShortlist(
            VillageWorkforceEvaluator.Evaluate(
                snapshot,
                snapshot.Targets[0].Identity,
                rule));
    }

    private static WorkforceRuleDefinition ResolveRule(
        LifeSkillDisciplineIdentity discipline) =>
        Assert.IsType<WorkforceRuleDefinition>(
            VerifiedVillageWorkforceRules.Resolve(
                new WorkforceObjectiveIdentity(
                    WorkforceObjectiveKind
                        .ShopManagerBaseLifeSkillQualification,
                    "1"),
                VillageWorkforceFixtures.Versions,
                WorkforceTargetKind.ShopManagerSlot,
                discipline).Rule);

    private static VillageWorkerProfile Profile(
        int characterId,
        short? qualification,
        WorkforceWorkerState state = WorkforceWorkerState.Eligible,
        bool candidate = true,
        WorkforceEvidenceState qualificationState =
            WorkforceEvidenceState.Confirmed)
    {
        var qualificationIdentity = new WorkforceFactIdentity(
            WorkforceFactKind.BaseLifeSkillQualification,
            new LifeSkillDisciplineIdentity(6));
        var facts = new List<WorkforceFact>
        {
            WorkforceFact.Confirmed(
                new WorkforceFactIdentity(
                    WorkforceFactKind.CandidateUniverseMembership),
                WorkforceFactValue.Boolean(candidate),
                VillageWorkforceFixtures.SaveProvenance,
                [VillageWorkforceFixtures.SaveEvidence("WORK_CANDIDATE")])
        };
        facts.Add(qualificationState switch
        {
            WorkforceEvidenceState.Confirmed => WorkforceFact.Confirmed(
                qualificationIdentity,
                WorkforceFactValue.Int16(qualification
                    ?? throw new ArgumentNullException(nameof(qualification))),
                VillageWorkforceFixtures.SaveProvenance,
                [VillageWorkforceFixtures.SaveEvidence("QUALIFICATION")]),
            WorkforceEvidenceState.Incomplete => WorkforceFact.Incomplete(
                qualificationIdentity,
                new WorkforceUnavailableReason("QUALIFICATION_MISSING"),
                [VillageWorkforceFixtures.SaveEvidence("QUALIFICATION")]),
            WorkforceEvidenceState.Unsupported => WorkforceFact.Unsupported(
                qualificationIdentity,
                new WorkforceUnavailableReason("QUALIFICATION_UNSUPPORTED"),
                []),
            WorkforceEvidenceState.Conflicting => WorkforceFact.Conflicting(
                qualificationIdentity,
                [
                    new WorkforceConflictValue(
                        WorkforceFactValue.Int16(50),
                        VillageWorkforceFixtures.SaveProvenance),
                    new WorkforceConflictValue(
                        WorkforceFactValue.Int16(60),
                        VillageWorkforceFixtures.GameDataProvenance)
                ],
                []),
            _ => throw new ArgumentOutOfRangeException(
                nameof(qualificationState))
        });
        return new VillageWorkerProfile(
            new VillageWorkerIdentity(characterId),
            state,
            VillageWorkforceFixtures.Versions,
            facts,
            []);
    }
}
