using TaiWu.Domain.VillageWorkforce;
using Xunit;

namespace TaiWu.Domain.UnitTests.VillageWorkforce;

public sealed class VillageWorkforceEvaluatorTests
{
    [Fact]
    public void Evaluation_is_deterministic_and_marks_exact_ties()
    {
        var firstWorker = Profile(101, 80);
        var secondWorker = Profile(202, 80);
        var lowerWorker = Profile(303, 60);
        var firstSnapshot = VillageWorkforceFixtures.Snapshot(
            [lowerWorker, secondWorker, firstWorker],
            currentWorker: firstWorker.Identity);
        var secondSnapshot = VillageWorkforceFixtures.Snapshot(
            [firstWorker, lowerWorker, secondWorker],
            currentWorker: firstWorker.Identity,
            capturedAt: firstSnapshot.CapturedAt);
        var rule = ResolveRule(firstSnapshot.Targets[0].RequiredDiscipline);

        var first = VillageWorkforceEvaluator.Evaluate(
            firstSnapshot,
            firstSnapshot.Targets[0].Identity,
            rule);
        var second = VillageWorkforceEvaluator.Evaluate(
            secondSnapshot,
            secondSnapshot.Targets[0].Identity,
            rule);

        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal(
            [101, 202, 303],
            first.Evaluations.Select(item => item.Worker.CharacterId));
        Assert.Equal(
            WorkforceEvaluationState.Tied,
            Evaluation(first, 101).State);
        Assert.Equal(
            WorkforceEvaluationState.Tied,
            Evaluation(first, 202).State);
        Assert.Equal(
            WorkforceEvaluationState.Ranked,
            Evaluation(first, 303).State);
        Assert.Equal(
            "EXACT_QUALIFICATION_TIE",
            Evaluation(first, 101).OutcomeIdentity);
    }

    [Fact]
    public void High_qualification_cannot_override_failed_candidate_gate()
    {
        var ineligible = Profile(
            101,
            300,
            WorkforceWorkerState.Ineligible,
            candidate: false);
        var eligible = Profile(202, 40);
        var snapshot = VillageWorkforceFixtures.Snapshot(
            [ineligible, eligible],
            currentWorker: eligible.Identity);

        var result = VillageWorkforceEvaluator.Evaluate(
            snapshot,
            snapshot.Targets[0].Identity,
            ResolveRule(snapshot.Targets[0].RequiredDiscipline));
        var evaluation = Evaluation(result, ineligible.Identity.CharacterId);

        Assert.Equal(WorkforceEvaluationState.Ineligible, evaluation.State);
        Assert.Empty(evaluation.Components);
        Assert.Null(evaluation.Result);
        Assert.Equal(
            WorkforceRequirementOutcome.Failed,
            evaluation.Requirements.Single(item =>
                item.Requirement
                    == WorkforceRequirementKind.AlternativeWorkCandidate)
                .Outcome);
    }

    [Fact]
    public void Missing_required_fact_is_incomplete_without_zero_component()
    {
        var missing = Profile(
            101,
            qualification: null,
            qualificationState: WorkforceEvidenceState.Incomplete);
        var snapshot = VillageWorkforceFixtures.Snapshot(
            [missing],
            currentWorker: missing.Identity);

        var result = VillageWorkforceEvaluator.Evaluate(
            snapshot,
            snapshot.Targets[0].Identity,
            ResolveRule(snapshot.Targets[0].RequiredDiscipline));
        var evaluation = Assert.Single(result.Evaluations);

        Assert.Equal(WorkforceEvaluationState.Incomplete, evaluation.State);
        Assert.Empty(evaluation.Components);
        Assert.Null(evaluation.Result);
        Assert.Equal(
            WorkforceRequirementOutcome.Incomplete,
            evaluation.Requirements.Single(item =>
                item.Requirement
                    == WorkforceRequirementKind.CharacterProfileAvailable)
                .Outcome);
    }

    [Theory]
    [InlineData(
        WorkforceEvidenceState.Unsupported,
        WorkforceEvaluationState.Unsupported)]
    [InlineData(
        WorkforceEvidenceState.Conflicting,
        WorkforceEvaluationState.Conflicting)]
    public void Fact_state_remains_typed_and_unrankable(
        WorkforceEvidenceState factState,
        WorkforceEvaluationState expected)
    {
        var worker = Profile(
            101,
            qualification: null,
            qualificationState: factState);
        var snapshot = VillageWorkforceFixtures.Snapshot(
            [worker],
            currentWorker: worker.Identity);

        var evaluation = Assert.Single(VillageWorkforceEvaluator.Evaluate(
            snapshot,
            snapshot.Targets[0].Identity,
            ResolveRule(snapshot.Targets[0].RequiredDiscipline)).Evaluations);

        Assert.Equal(expected, evaluation.State);
        Assert.Empty(evaluation.Components);
        Assert.Null(evaluation.Result);
        if (factState == WorkforceEvidenceState.Conflicting)
        {
            Assert.Equal(
                2,
                evaluation.Requirements.Single(item =>
                    item.Requirement
                        == WorkforceRequirementKind.CharacterProfileAvailable)
                    .Conflicts.Length);
        }
    }

    [Fact]
    public void Current_only_worker_keeps_descriptive_value_without_rank()
    {
        var current = Profile(
            101,
            100,
            WorkforceWorkerState.CurrentOnly,
            candidate: false);
        var eligible = Profile(202, 40);
        var snapshot = VillageWorkforceFixtures.Snapshot(
            [current, eligible],
            currentWorker: current.Identity);

        var result = VillageWorkforceEvaluator.Evaluate(
            snapshot,
            snapshot.Targets[0].Identity,
            ResolveRule(snapshot.Targets[0].RequiredDiscipline));
        var currentEvaluation = Evaluation(result, 101);
        var eligibleEvaluation = Evaluation(result, 202);

        Assert.Equal(
            WorkforceEvaluationState.CurrentOnly,
            currentEvaluation.State);
        Assert.False(currentEvaluation.IsRankable);
        Assert.Equal(100m, currentEvaluation.Result?.Value);
        Assert.Equal(WorkforceEvaluationState.Ranked, eligibleEvaluation.State);
        Assert.Equal(40m, eligibleEvaluation.Result?.Value);
    }

    [Fact]
    public void Every_available_component_preserves_formula_and_evidence()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();

        var evaluation = Evaluation(
            VillageWorkforceEvaluator.Evaluate(
                snapshot,
                snapshot.Targets[0].Identity,
                ResolveRule(snapshot.Targets[0].RequiredDiscipline)),
            snapshot.Workers[0].Identity.CharacterId);
        var component = Assert.Single(evaluation.Components);

        Assert.Equal(60, component.RawValue);
        Assert.Equal(60m, component.NormalizedValue);
        Assert.Equal(1m, component.Weight);
        Assert.Equal(60m, component.Contribution);
        Assert.Equal(WorkforceUnit.BaseQualificationPoint, component.Unit);
        Assert.Equal(
            "REQUIRED_BASE_LIFE_SKILL_QUALIFICATION_EXACT_VALUE",
            component.ExplanationIdentity);
        Assert.NotEmpty(component.Evidence);
        Assert.Equal(
            component.Contribution,
            evaluation.Result?.Value);
    }

    [Fact]
    public void Source_or_target_rule_mismatch_is_unsupported_without_score()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var resolved = ResolveRule(snapshot.Targets[0].RequiredDiscipline);
        var sourceMismatch = CopyRule(
            resolved,
            new WorkforceSupportedSourceVersion(
                "1.0.0+different",
                resolved.SupportedSource.MappingVersion,
                resolved.SupportedSource.CandidateUniverseVersion,
                resolved.SupportedSource.FingerprintSchemaVersion));
        var targetMismatch = ResolveRule(new LifeSkillDisciplineIdentity(7));

        var sourceEvaluation = Evaluation(
            VillageWorkforceEvaluator.Evaluate(
                snapshot,
                snapshot.Targets[0].Identity,
                sourceMismatch),
            snapshot.Workers[0].Identity.CharacterId);
        var targetEvaluation = Evaluation(
            VillageWorkforceEvaluator.Evaluate(
                snapshot,
                snapshot.Targets[0].Identity,
                targetMismatch),
            snapshot.Workers[0].Identity.CharacterId);

        Assert.Equal(
            WorkforceEvaluationState.Unsupported,
            sourceEvaluation.State);
        Assert.Equal(
            WorkforceEvaluationState.Unsupported,
            targetEvaluation.State);
        Assert.Empty(sourceEvaluation.Components);
        Assert.Empty(targetEvaluation.Components);
    }

    [Fact]
    public void Evaluation_set_rejects_duplicate_or_cross_result_workers()
    {
        var snapshot = VillageWorkforceFixtures.Snapshot();
        var result = VillageWorkforceEvaluator.Evaluate(
            snapshot,
            snapshot.Targets[0].Identity,
            ResolveRule(snapshot.Targets[0].RequiredDiscipline));

        Assert.Throws<ArgumentException>(() =>
            new VillageWorkforceEvaluationSet(
                result.ResultIdentity,
                result.Rule,
                result.CurrentWorker,
                [result.Evaluations[0], result.Evaluations[0]]));
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

    private static WorkforceEvaluation Evaluation(
        VillageWorkforceEvaluationSet result,
        int characterId) =>
        result.Evaluations.Single(item =>
            item.Worker.CharacterId == characterId);

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

    private static WorkforceRuleDefinition CopyRule(
        WorkforceRuleDefinition rule,
        WorkforceSupportedSourceVersion supportedSource) =>
        new(
            rule.Identity,
            rule.Version,
            rule.Objective,
            supportedSource,
            rule.TargetKind,
            rule.Requirements,
            rule.Components,
            rule.Limitations);
}
