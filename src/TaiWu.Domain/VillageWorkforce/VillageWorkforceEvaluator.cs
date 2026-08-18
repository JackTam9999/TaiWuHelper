using System.Collections.Immutable;
using System.Text;

namespace TaiWu.Domain.VillageWorkforce;

public sealed class VillageWorkforceEvaluationSet
{
    public VillageWorkforceEvaluationSet(
        WorkforceResultIdentity resultIdentity,
        WorkforceRuleDefinition rule,
        VillageWorkerIdentity currentWorker,
        IEnumerable<WorkforceEvaluation> evaluations)
    {
        ResultIdentity = resultIdentity
            ?? throw new ArgumentNullException(nameof(resultIdentity));
        Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        if (resultIdentity.Objective != rule.Objective
            || resultIdentity.RuleVersion != rule.Version
            || resultIdentity.Target.Kind != rule.TargetKind)
        {
            throw new ArgumentException(
                "The evaluation result identity must use the exact resolved rule.",
                nameof(rule));
        }
        CurrentWorker = currentWorker
            ?? throw new ArgumentNullException(nameof(currentWorker));
        ArgumentNullException.ThrowIfNull(evaluations);
        var copied = evaluations.ToImmutableArray();
        if (copied.IsEmpty || copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "An evaluation set requires non-null worker evaluations.",
                nameof(evaluations));
        }

        if (copied.Any(item => item.ResultIdentity != resultIdentity))
        {
            throw new ArgumentException(
                "Every worker evaluation must belong to the same immutable result.",
                nameof(evaluations));
        }

        if (copied.GroupBy(item => item.Worker).Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "An evaluation set cannot contain a worker twice.",
                nameof(evaluations));
        }

        if (copied.All(item => item.Worker != currentWorker))
        {
            throw new ArgumentException(
                "The selected target's current worker must remain in the evaluation set.",
                nameof(evaluations));
        }

        var invalidTieGroup = copied
            .Where(item => item.IsRankable && item.Result is not null)
            .GroupBy(item => item.Result!.Value)
            .FirstOrDefault(group => group.Count() > 1
                ? group.Any(item =>
                    item.State != WorkforceEvaluationState.Tied)
                : group.Any(item =>
                    item.State != WorkforceEvaluationState.Ranked));
        if (invalidTieGroup is not null)
        {
            throw new ArgumentException(
                "Exact equal results must be ties, and unique results must be ranked.",
                nameof(evaluations));
        }

        Evaluations = [.. copied.OrderBy(
            item => item.Worker.CharacterId)];
        Fingerprint = CreateFingerprint();
    }

    public WorkforceResultIdentity ResultIdentity { get; }

    public WorkforceRuleDefinition Rule { get; }

    public string RuleFingerprint => Rule.Fingerprint;

    public VillageWorkerIdentity CurrentWorker { get; }

    public ImmutableArray<WorkforceEvaluation> Evaluations { get; }

    public string Fingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("VILLAGE_WORKFORCE_EVALUATION_SET_V1\n")
            .Append(ResultIdentity.StableKey).Append('|')
            .Append(Rule.Fingerprint).Append('|')
            .Append(CurrentWorker.StableKey).Append('\n');
        foreach (var evaluation in Evaluations)
        {
            canonical.Append("EVALUATION|")
                .Append(evaluation.Fingerprint).Append('\n');
        }

        return WorkforceText.Fingerprint(canonical.ToString());
    }
}

public static class VillageWorkforceEvaluator
{
    public static VillageWorkforceEvaluationSet Evaluate(
        VillageWorkforceSnapshot snapshot,
        ShopManagerTargetIdentity targetIdentity,
        WorkforceRuleDefinition rule)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(targetIdentity);
        ArgumentNullException.ThrowIfNull(rule);

        var target = snapshot.Targets.SingleOrDefault(item =>
            item.Identity == targetIdentity)
            ?? throw new ArgumentException(
                "The requested workforce target is not present in the snapshot.",
                nameof(targetIdentity));
        var currentAssignment = snapshot.CurrentAssignments.Single(item =>
            item.Target == targetIdentity);
        var resultIdentity = new WorkforceResultIdentity(
            snapshot.Fingerprint,
            rule.Objective,
            rule.Version,
            targetIdentity);
        var ruleEvidence = new WorkforceEvidenceReference(
            "WORKFORCE_RULE_DEFINITION",
            new WorkforceProvenance(
                WorkforceEvidenceSourceKind.DerivedRule,
                rule.Identity.Value,
                rule.Version.Value,
                rule.Fingerprint));
        var provisional = snapshot.Workers
            .Select(worker => EvaluateWorker(
                snapshot,
                target,
                currentAssignment,
                rule,
                resultIdentity,
                ruleEvidence,
                worker))
            .ToImmutableArray();
        var tiedValues = provisional
            .Where(item => item.IsRankable && item.Result is not null)
            .GroupBy(item => item.Result!.Value)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        var evaluations = provisional.Select(item =>
            item.IsRankable
                && item.Result is not null
                && tiedValues.Contains(item.Result.Value)
                ? ReclassifyTie(item)
                : item);

        return new VillageWorkforceEvaluationSet(
            resultIdentity,
            rule,
            currentAssignment.Worker,
            evaluations);
    }

    private static WorkforceEvaluation EvaluateWorker(
        VillageWorkforceSnapshot snapshot,
        ShopManagerTarget target,
        CurrentShopManagerAssignment currentAssignment,
        WorkforceRuleDefinition rule,
        WorkforceResultIdentity resultIdentity,
        WorkforceEvidenceReference ruleEvidence,
        VillageWorkerProfile worker)
    {
        var componentRule = rule.Components.Single();
        var sourceRequirement = rule.Requirements.Single(item =>
            item.Requirement == WorkforceRequirementKind.SupportedSourceVersion);
        var targetRequirement = rule.Requirements.Single(item =>
            item.Requirement == WorkforceRequirementKind.SupportedShopTarget);
        var candidateRequirement = rule.Requirements.Single(item =>
            item.Requirement == WorkforceRequirementKind.AlternativeWorkCandidate);
        var profileRequirement = rule.Requirements.Single(item =>
            item.Requirement == WorkforceRequirementKind.CharacterProfileAvailable);
        var provenanceRequirement = rule.Requirements.Single(item =>
            item.Requirement
                == WorkforceRequirementKind.QualificationProvenanceMatch);

        var qualification = worker.FindFact(componentRule.SourceFact);
        var requirements = new[]
        {
            EvaluateSourceRequirement(
                snapshot.SourceVersions,
                rule.SupportedSource,
                sourceRequirement,
                ruleEvidence),
            EvaluateTargetRequirement(
                target,
                currentAssignment,
                rule,
                targetRequirement,
                ruleEvidence),
            EvaluateCandidateRequirement(
                worker,
                candidateRequirement,
                ruleEvidence),
            EvaluateFactRequirement(
                qualification,
                profileRequirement,
                "CHARACTER_PROFILE"),
            EvaluateProvenanceRequirement(
                qualification,
                snapshot.SourceVersions,
                provenanceRequirement)
        };
        var state = DetermineState(requirements, worker.State);
        var mayCarryValue = state is WorkforceEvaluationState.Ranked
            or WorkforceEvaluationState.CurrentOnly;
        var component = mayCarryValue
            ? CreateComponent(qualification, componentRule)
            : null;
        var result = component is null
            ? null
            : new WorkforceResultValue(component.Unit, component.Contribution);

        return new WorkforceEvaluation(
            resultIdentity,
            worker.Identity,
            ToWorkerState(state),
            state,
            requirements,
            component is null ? [] : [component],
            result,
            OutcomeIdentity(state));
    }

    private static WorkforceRequirementEvaluation EvaluateSourceRequirement(
        WorkforceSourceVersions observed,
        WorkforceSupportedSourceVersion supported,
        WorkforceRequirementDefinition definition,
        WorkforceEvidenceReference ruleEvidence)
    {
        var matches = string.Equals(
                observed.GameDataVersion,
                supported.GameDataVersion,
                StringComparison.Ordinal)
            && string.Equals(
                observed.MappingVersion,
                supported.MappingVersion,
                StringComparison.Ordinal)
            && string.Equals(
                observed.CandidateUniverseVersion,
                supported.CandidateUniverseVersion,
                StringComparison.Ordinal)
            && string.Equals(
                observed.FingerprintSchemaVersion,
                supported.FingerprintSchemaVersion,
                StringComparison.Ordinal);
        return new WorkforceRequirementEvaluation(
            definition.Requirement,
            matches
                ? WorkforceRequirementOutcome.Passed
                : WorkforceRequirementOutcome.Unsupported,
            matches
                ? "SOURCE_VERSIONS_SUPPORTED"
                : "SOURCE_VERSIONS_UNSUPPORTED",
            [ruleEvidence]);
    }

    private static WorkforceRequirementEvaluation EvaluateTargetRequirement(
        ShopManagerTarget target,
        CurrentShopManagerAssignment currentAssignment,
        WorkforceRuleDefinition rule,
        WorkforceRequirementDefinition definition,
        WorkforceEvidenceReference ruleEvidence)
    {
        var component = rule.Components.Single();
        var matches = target.Identity.Kind == rule.TargetKind
            && target.RequiredDiscipline == component.Identity.Discipline
            && currentAssignment.Target == target.Identity;
        var assignmentEvidence = new WorkforceEvidenceReference(
            "CURRENT_SHOP_MANAGER_ASSIGNMENT",
            currentAssignment.Provenance);
        return new WorkforceRequirementEvaluation(
            definition.Requirement,
            matches
                ? WorkforceRequirementOutcome.Passed
                : WorkforceRequirementOutcome.Unsupported,
            matches
                ? "SHOP_MANAGER_TARGET_SUPPORTED"
                : "SHOP_MANAGER_TARGET_UNSUPPORTED",
            UniqueEvidence(
                target.Evidence,
                [assignmentEvidence, ruleEvidence]));
    }

    private static WorkforceRequirementEvaluation EvaluateCandidateRequirement(
        VillageWorkerProfile worker,
        WorkforceRequirementDefinition definition,
        WorkforceEvidenceReference ruleEvidence)
    {
        var fact = worker.FindFact(definition.SourceFact!);
        if (fact?.State == WorkforceEvidenceState.Confirmed
            && fact.Value!.BooleanValue)
        {
            return new WorkforceRequirementEvaluation(
                definition.Requirement,
                WorkforceRequirementOutcome.Passed,
                "ALTERNATIVE_WORK_CANDIDATE_CONFIRMED",
                UniqueEvidence(FactEvidence(fact), [ruleEvidence]));
        }

        if (fact?.State == WorkforceEvidenceState.Confirmed)
        {
            return new WorkforceRequirementEvaluation(
                definition.Requirement,
                WorkforceRequirementOutcome.Failed,
                worker.State == WorkforceWorkerState.CurrentOnly
                    ? "CURRENT_ASSIGNMENT_OUTSIDE_ALTERNATIVE_UNIVERSE"
                    : "ALTERNATIVE_WORK_CANDIDATE_NOT_CONFIRMED",
                UniqueEvidence(FactEvidence(fact), [ruleEvidence]));
        }

        return FactUnavailable(
            definition.Requirement,
            fact,
            "ALTERNATIVE_WORK_CANDIDATE");
    }

    private static WorkforceRequirementEvaluation EvaluateFactRequirement(
        WorkforceFact? fact,
        WorkforceRequirementDefinition definition,
        string reasonPrefix)
    {
        if (fact?.State == WorkforceEvidenceState.Confirmed)
        {
            return new WorkforceRequirementEvaluation(
                definition.Requirement,
                WorkforceRequirementOutcome.Passed,
                $"{reasonPrefix}_AVAILABLE",
                FactEvidence(fact));
        }

        return FactUnavailable(definition.Requirement, fact, reasonPrefix);
    }

    private static WorkforceRequirementEvaluation
        EvaluateProvenanceRequirement(
            WorkforceFact? fact,
            WorkforceSourceVersions sourceVersions,
            WorkforceRequirementDefinition definition)
    {
        if (fact?.State != WorkforceEvidenceState.Confirmed)
        {
            return FactUnavailable(
                definition.Requirement,
                fact,
                "QUALIFICATION_PROVENANCE");
        }

        var provenance = fact.Provenance!;
        var matches = provenance.SourceKind
                == WorkforceEvidenceSourceKind.ConfiguredSave
            && string.Equals(
                provenance.SourceVersion,
                sourceVersions.MappingVersion,
                StringComparison.Ordinal)
            && string.Equals(
                provenance.RevisionIdentity,
                sourceVersions.SaveSha256,
                StringComparison.Ordinal);
        return new WorkforceRequirementEvaluation(
            definition.Requirement,
            matches
                ? WorkforceRequirementOutcome.Passed
                : WorkforceRequirementOutcome.Conflicting,
            matches
                ? "QUALIFICATION_PROVENANCE_MATCHED"
                : "QUALIFICATION_PROVENANCE_CONFLICTED",
            FactEvidence(fact));
    }

    private static WorkforceRequirementEvaluation FactUnavailable(
        WorkforceRequirementKind requirement,
        WorkforceFact? fact,
        string reasonPrefix)
    {
        var outcome = fact?.State switch
        {
            null or WorkforceEvidenceState.Incomplete
                or WorkforceEvidenceState.Stale =>
                WorkforceRequirementOutcome.Incomplete,
            WorkforceEvidenceState.Unsupported =>
                WorkforceRequirementOutcome.Unsupported,
            WorkforceEvidenceState.Conflicting =>
                WorkforceRequirementOutcome.Conflicting,
            WorkforceEvidenceState.Confirmed =>
                throw new InvalidOperationException(
                    "Confirmed evidence must be evaluated by its typed gate."),
            _ => throw new ArgumentOutOfRangeException(nameof(fact))
        };
        return new WorkforceRequirementEvaluation(
            requirement,
            outcome,
            fact?.UnavailableReason?.Code
                ?? $"{reasonPrefix}_{outcome.ToString().ToUpperInvariant()}",
            fact is null ? [] : FactEvidence(fact),
            fact?.Conflicts ?? []);
    }

    private static WorkforceScoreComponent? CreateComponent(
        WorkforceFact? qualification,
        WorkforceComponentDefinition definition)
    {
        if (qualification?.State != WorkforceEvidenceState.Confirmed)
        {
            return null;
        }

        var raw = qualification.Value!.Int16Value;
        return new WorkforceScoreComponent(
            definition.Identity,
            raw,
            raw,
            definition.Weight,
            raw,
            definition.ExplanationIdentity,
            FactEvidence(qualification));
    }

    private static WorkforceEvaluationState DetermineState(
        IReadOnlyCollection<WorkforceRequirementEvaluation> requirements,
        WorkforceWorkerState profileState)
    {
        if (requirements.Any(item =>
            item.Outcome == WorkforceRequirementOutcome.Conflicting))
        {
            return WorkforceEvaluationState.Conflicting;
        }

        if (requirements.Any(item =>
            item.Outcome == WorkforceRequirementOutcome.Unsupported))
        {
            return WorkforceEvaluationState.Unsupported;
        }

        if (requirements.Any(item =>
            item.Outcome == WorkforceRequirementOutcome.Incomplete))
        {
            return WorkforceEvaluationState.Incomplete;
        }

        if (requirements.Any(item =>
            item.Outcome == WorkforceRequirementOutcome.Failed))
        {
            return profileState == WorkforceWorkerState.CurrentOnly
                ? WorkforceEvaluationState.CurrentOnly
                : WorkforceEvaluationState.Ineligible;
        }

        return WorkforceEvaluationState.Ranked;
    }

    private static WorkforceWorkerState ToWorkerState(
        WorkforceEvaluationState state) => state switch
        {
            WorkforceEvaluationState.Ranked or WorkforceEvaluationState.Tied =>
                WorkforceWorkerState.Eligible,
            WorkforceEvaluationState.CurrentOnly =>
                WorkforceWorkerState.CurrentOnly,
            WorkforceEvaluationState.Ineligible =>
                WorkforceWorkerState.Ineligible,
            WorkforceEvaluationState.Incomplete =>
                WorkforceWorkerState.Incomplete,
            WorkforceEvaluationState.Unsupported =>
                WorkforceWorkerState.Unsupported,
            WorkforceEvaluationState.Conflicting =>
                WorkforceWorkerState.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    private static WorkforceEvaluation ReclassifyTie(
        WorkforceEvaluation evaluation) =>
        new(
            evaluation.ResultIdentity,
            evaluation.Worker,
            WorkforceWorkerState.Eligible,
            WorkforceEvaluationState.Tied,
            evaluation.Requirements,
            evaluation.Components,
            evaluation.Result,
            "EXACT_QUALIFICATION_TIE");

    private static string OutcomeIdentity(WorkforceEvaluationState state) =>
        state switch
        {
            WorkforceEvaluationState.Ranked => "QUALIFICATION_RANKABLE",
            WorkforceEvaluationState.Tied => "EXACT_QUALIFICATION_TIE",
            WorkforceEvaluationState.CurrentOnly =>
                "CURRENT_ASSIGNMENT_OUTSIDE_ALTERNATIVE_UNIVERSE",
            WorkforceEvaluationState.Ineligible =>
                "WORKER_INELIGIBLE_FOR_ALTERNATIVE_ASSIGNMENT",
            WorkforceEvaluationState.Incomplete =>
                "WORKFORCE_EVIDENCE_INCOMPLETE",
            WorkforceEvaluationState.Unsupported =>
                "WORKFORCE_EVIDENCE_UNSUPPORTED",
            WorkforceEvaluationState.Conflicting =>
                "WORKFORCE_EVIDENCE_CONFLICTING",
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    private static ImmutableArray<WorkforceEvidenceReference> FactEvidence(
        WorkforceFact fact)
    {
        var direct = new List<WorkforceEvidenceReference>();
        if (fact.Provenance is not null)
        {
            direct.Add(new WorkforceEvidenceReference(
                "WORKFORCE_FACT_VALUE",
                fact.Provenance));
        }

        direct.AddRange(fact.Conflicts.Select(conflict =>
            new WorkforceEvidenceReference(
                "WORKFORCE_FACT_CONFLICT",
                conflict.Provenance)));
        return UniqueEvidence(fact.Evidence, direct);
    }

    private static ImmutableArray<WorkforceEvidenceReference> UniqueEvidence(
        IEnumerable<WorkforceEvidenceReference> first,
        IEnumerable<WorkforceEvidenceReference> second) =>
        [.. first.Concat(second)
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .OrderBy(item => item.StableKey, StringComparer.Ordinal)];
}
