using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalPlanBranch
{
    public TacticalPlanBranch(
        string conditionIdentity,
        TacticalBranchOutcome outcome,
        TacticalPlanStepIdentity? targetStep = null)
    {
        ConditionIdentity = TacticalCombatText.Code(
            conditionIdentity,
            nameof(conditionIdentity));
        Outcome = TacticalCombatText.Defined(outcome, nameof(outcome));
        TargetStep = targetStep;

        var requiresTarget = Outcome is TacticalBranchOutcome.Continue
            or TacticalBranchOutcome.Fallback;
        if (requiresTarget != (TargetStep is not null))
        {
            throw new ArgumentException(
                "Continue and fallback branches require a target; unresolved and stop branches forbid one.",
                nameof(targetStep));
        }
    }

    public string ConditionIdentity { get; }

    public TacticalBranchOutcome Outcome { get; }

    public TacticalPlanStepIdentity? TargetStep { get; }

    internal string StableKey => string.Join('|',
        ConditionIdentity,
        TacticalCombatText.EnumKey(Outcome),
        TargetStep?.StableKey ?? "NONE");
}

public sealed class TacticalPlanStep
{
    public TacticalPlanStep(
        TacticalPlanStepIdentity identity,
        TacticalPlanStage stage,
        int order,
        TacticalStepBranchKind branchKind,
        IEnumerable<TacticalFactIdentity> observedFacts,
        IEnumerable<TacticalRequirementEvaluation> requirements,
        IEnumerable<TacticalTransitionIdentity> transitions,
        string manualActionIdentity,
        string expectedPurposeIdentity,
        string limitationIdentity,
        IEnumerable<TacticalPlanBranch> branches,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Stage = TacticalCombatText.Defined(stage, nameof(stage));
        if (order <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order));
        }

        Order = order;
        BranchKind = TacticalCombatText.Defined(branchKind, nameof(branchKind));
        ObservedFacts = TacticalCombatText.CopyUnique(
            observedFacts,
            item => item.StableKey,
            "step observed fact",
            nameof(observedFacts));
        Requirements = TacticalCombatText.CopyUnique(
            requirements,
            item => item.StableKey,
            "step requirement",
            nameof(requirements));
        Transitions = TacticalCombatText.CopyUnique(
            transitions,
            item => item.StableKey,
            "step transition",
            nameof(transitions));
        ManualActionIdentity = TacticalCombatText.Code(
            manualActionIdentity,
            nameof(manualActionIdentity));
        ExpectedPurposeIdentity = TacticalCombatText.Code(
            expectedPurposeIdentity,
            nameof(expectedPurposeIdentity));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        Branches = TacticalCombatText.CopyUnique(
            branches,
            item => item.StableKey,
            "step branch",
            nameof(branches));
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "step evidence",
            nameof(evidence));
        if (ObservedFacts.IsEmpty
            || Transitions.IsEmpty
            || Branches.IsEmpty
            || Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical step requires observed facts, transitions, branches, and evidence.");
        }
    }

    public TacticalPlanStepIdentity Identity { get; }

    public TacticalPlanStage Stage { get; }

    public int Order { get; }

    public TacticalStepBranchKind BranchKind { get; }

    public ImmutableArray<TacticalFactIdentity> ObservedFacts { get; }

    public ImmutableArray<TacticalRequirementEvaluation> Requirements { get; }

    public ImmutableArray<TacticalTransitionIdentity> Transitions { get; }

    public string ManualActionIdentity { get; }

    public string ExpectedPurposeIdentity { get; }

    public string LimitationIdentity { get; }

    public ImmutableArray<TacticalPlanBranch> Branches { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => Identity.StableKey;

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(Stage),
        Order.ToString(CultureInfo.InvariantCulture),
        TacticalCombatText.EnumKey(BranchKind),
        ManualActionIdentity,
        ExpectedPurposeIdentity,
        LimitationIdentity,
        string.Join("||", ObservedFacts.Select(item => item.StableKey)),
        string.Join("||", Requirements.Select(item => item.ContentKey)),
        string.Join("||", Transitions.Select(item => item.StableKey)),
        string.Join("||", Branches.Select(item => item.StableKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));

    internal IEnumerable<TacticalEvidenceReference> AllEvidence =>
        Evidence.Concat(Requirements.SelectMany(item => item.Evidence));
}

public sealed class TacticalPlanStageDefinition
{
    public TacticalPlanStageDefinition(
        TacticalPlanStage stage,
        TacticalPlanStageState state,
        string limitationIdentity,
        IEnumerable<TacticalPlanStep> steps,
        IEnumerable<TacticalEvidenceReference> evidence)
    {
        Stage = TacticalCombatText.Defined(stage, nameof(stage));
        State = TacticalCombatText.Defined(state, nameof(state));
        LimitationIdentity = TacticalCombatText.Code(
            limitationIdentity,
            nameof(limitationIdentity));
        ArgumentNullException.ThrowIfNull(steps);
        var copied = steps.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException(
                "A tactical stage cannot contain a null step.",
                nameof(steps));
        }

        if (copied.Any(item => item.Stage != Stage)
            || copied.Select(item => item.Identity.StableKey)
                .Distinct(StringComparer.Ordinal)
                .Count() != copied.Length
            || copied.Select(item => item.Order).Distinct().Count()
                != copied.Length)
        {
            throw new ArgumentException(
                "Stage steps require the same stage and unique identities and orders.",
                nameof(steps));
        }

        Steps =
        [
            .. copied
                .OrderBy(item => item.Order)
                .ThenBy(item => item.StableKey, StringComparer.Ordinal)
        ];
        Evidence = TacticalCombatText.CopyUnique(
            evidence,
            item => item.StableKey,
            "stage evidence",
            nameof(evidence));
        if (Evidence.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical stage requires evidence for its state.",
                nameof(evidence));
        }

        if ((State == TacticalPlanStageState.Supported) != !Steps.IsEmpty)
        {
            throw new ArgumentException(
                "Only a supported tactical stage can contain steps, and it requires at least one.",
                nameof(steps));
        }
    }

    public TacticalPlanStage Stage { get; }

    public TacticalPlanStageState State { get; }

    public string LimitationIdentity { get; }

    public ImmutableArray<TacticalPlanStep> Steps { get; }

    public ImmutableArray<TacticalEvidenceReference> Evidence { get; }

    internal string StableKey => TacticalCombatText.EnumKey(Stage);

    internal string ContentKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(State),
        LimitationIdentity,
        string.Join("||", Steps.Select(item => item.ContentKey)),
        string.Join("||", Evidence.Select(item => item.StableKey)));

    internal IEnumerable<TacticalEvidenceReference> AllEvidence =>
        Evidence.Concat(Steps.SelectMany(item => item.AllEvidence));
}

public sealed class TacticalCombatPlan
{
    public TacticalCombatPlan(
        string gameDataVersion,
        string ruleVersion,
        IEnumerable<TacticalEvidenceReference> sharedEvidence,
        IEnumerable<TacticalStateFact> facts,
        IEnumerable<TacticalRequirementDefinition> requirements,
        IEnumerable<TacticalTransition> transitions,
        IEnumerable<TacticalSkillRole> roles,
        IEnumerable<TacticalCandidateConsideration> candidates,
        TacticalSearchCoverage searchCoverage,
        IEnumerable<TacticalPlanStageDefinition> stages)
    {
        GameDataVersion = TacticalCombatText.Stable(
            gameDataVersion,
            nameof(gameDataVersion));
        RuleVersion = TacticalCombatText.Stable(
            ruleVersion,
            nameof(ruleVersion));
        SharedEvidence = TacticalCombatText.CopyUnique(
            sharedEvidence,
            item => item.StableKey,
            "shared evidence",
            nameof(sharedEvidence));
        Facts = TacticalCombatText.CopyUnique(
            facts,
            item => item.StableKey,
            "fact",
            nameof(facts));
        Requirements = TacticalCombatText.CopyUnique(
            requirements,
            item => item.StableKey,
            "requirement definition",
            nameof(requirements));
        Transitions = TacticalCombatText.CopyUnique(
            transitions,
            item => item.StableKey,
            "transition",
            nameof(transitions));
        Roles = TacticalCombatText.CopyUnique(
            roles,
            item => item.StableKey,
            "role",
            nameof(roles));
        Candidates = TacticalCombatText.CopyUnique(
            candidates,
            item => item.StableKey,
            "candidate",
            nameof(candidates));
        SearchCoverage = searchCoverage
            ?? throw new ArgumentNullException(nameof(searchCoverage));
        Stages = CopyStages(stages);
        if (SharedEvidence.IsEmpty || Facts.IsEmpty)
        {
            throw new ArgumentException(
                "A tactical combat plan requires shared evidence and at least one state fact.");
        }

        ValidateReferencesAndVersions();
        ValidateCandidateAccounting();
        ValidatePlanGraph();
        Fingerprint = CreateFingerprint();
    }

    public string GameDataVersion { get; }

    public string RuleVersion { get; }

    public ImmutableArray<TacticalEvidenceReference> SharedEvidence { get; }

    public ImmutableArray<TacticalStateFact> Facts { get; }

    public ImmutableArray<TacticalRequirementDefinition> Requirements { get; }

    public ImmutableArray<TacticalTransition> Transitions { get; }

    public ImmutableArray<TacticalSkillRole> Roles { get; }

    public ImmutableArray<TacticalCandidateConsideration> Candidates { get; }

    public TacticalSearchCoverage SearchCoverage { get; }

    public ImmutableArray<TacticalPlanStageDefinition> Stages { get; }

    public string Fingerprint { get; }

    private static ImmutableArray<TacticalPlanStageDefinition> CopyStages(
        IEnumerable<TacticalPlanStageDefinition> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);
        var copied = stages.ToImmutableArray();
        var expected = Enum.GetValues<TacticalPlanStage>();
        if (copied.Any(item => item is null)
            || copied.Length != expected.Length
            || copied.Select(item => item.Stage).Distinct().Count()
                != expected.Length
            || expected.Any(stage => copied.All(item => item.Stage != stage)))
        {
            throw new ArgumentException(
                "A tactical plan requires every stage exactly once.",
                nameof(stages));
        }

        return [.. copied.OrderBy(item => (int)item.Stage)];
    }

    private void ValidateReferencesAndVersions()
    {
        var facts = Facts.ToDictionary(item => item.StableKey);
        var requirements = Requirements.ToDictionary(item => item.StableKey);
        var transitions = Transitions.ToDictionary(item => item.StableKey);
        var roles = Roles.ToDictionary(item => item.StableKey);
        var candidates = Candidates.ToDictionary(item => item.StableKey);

        foreach (var requirement in Requirements)
        {
            RequireReference(facts, requirement.Fact.StableKey, "fact");
        }

        foreach (var transition in Transitions)
        {
            foreach (var requirement in transition.Preconditions)
            {
                RequireReference(
                    requirements,
                    requirement.StableKey,
                    "transition precondition");
            }

            foreach (var fact in transition.ResultingFacts)
            {
                RequireReference(facts, fact.StableKey, "transition result");
            }
        }

        foreach (var role in Roles)
        {
            foreach (var transition in role.Transitions)
            {
                RequireReference(
                    transitions,
                    transition.StableKey,
                    "role transition");
            }

            foreach (var requirement in role.Requirements)
            {
                RequireReference(
                    requirements,
                    requirement.StableKey,
                    "role requirement");
            }
        }

        foreach (var candidate in Candidates)
        {
            foreach (var roleIdentity in candidate.Roles)
            {
                RequireReference(roles, roleIdentity.StableKey, "candidate role");
                var role = roles[roleIdentity.StableKey];
                if (role.SkillId != candidate.Identity.SkillId
                    || role.Direction != candidate.Identity.Direction)
                {
                    throw new ArgumentException(
                        "A candidate can reference only roles for the same skill and direction.");
                }
            }

            foreach (var evaluation in candidate.Requirements)
            {
                RequireReference(
                    requirements,
                    evaluation.Requirement.StableKey,
                    "candidate requirement");
            }

            if (candidate.DominatedBy is not null)
            {
                RequireReference(
                    candidates,
                    candidate.DominatedBy.StableKey,
                    "candidate dominator");
                if (candidates[candidate.DominatedBy.StableKey].Decision
                    != TacticalCandidateDecision.Admitted)
                {
                    throw new ArgumentException(
                        "A dominated candidate must reference an admitted dominator.");
                }
            }
        }

        foreach (var step in Stages.SelectMany(item => item.Steps))
        {
            foreach (var fact in step.ObservedFacts)
            {
                RequireReference(facts, fact.StableKey, "step fact");
            }

            foreach (var evaluation in step.Requirements)
            {
                RequireReference(
                    requirements,
                    evaluation.Requirement.StableKey,
                    "step requirement");
            }

            foreach (var transition in step.Transitions)
            {
                RequireReference(
                    transitions,
                    transition.StableKey,
                    "step transition");
            }
        }

        foreach (var evidence in AllEvidence())
        {
            if (!string.Equals(
                    evidence.GameDataVersion,
                    GameDataVersion,
                    StringComparison.Ordinal)
                || !string.Equals(
                    evidence.RuleVersion,
                    RuleVersion,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Every tactical evidence reference must match the plan GameData and rule versions.");
            }
        }
    }

    private void ValidateCandidateAccounting()
    {
        if (SearchCoverage.CandidateUniverseCount != Candidates.Length
            || SearchCoverage.AdmittedCount != Count(
                TacticalCandidateDecision.Admitted)
            || SearchCoverage.RejectedCount != Count(
                TacticalCandidateDecision.Rejected)
            || SearchCoverage.UnsupportedCount != Count(
                TacticalCandidateDecision.Unsupported)
            || SearchCoverage.IrrelevantCount != Count(
                TacticalCandidateDecision.Irrelevant)
            || SearchCoverage.DominatedCount != Count(
                TacticalCandidateDecision.Dominated)
            || SearchCoverage.RoleSupportedCount
                != Candidates.Count(item => !item.Roles.IsEmpty))
        {
            throw new ArgumentException(
                "Search coverage must exactly account for candidate considerations.",
                nameof(SearchCoverage));
        }
    }

    private int Count(TacticalCandidateDecision decision) =>
        Candidates.Count(item => item.Decision == decision);

    private void ValidatePlanGraph()
    {
        var steps = Stages
            .SelectMany(item => item.Steps)
            .ToDictionary(item => item.StableKey, StringComparer.Ordinal);

        foreach (var stage in Stages)
        {
            foreach (var step in stage.Steps)
            {
                if ((stage.Stage == TacticalPlanStage.Fallback)
                    != (step.BranchKind == TacticalStepBranchKind.Fallback))
                {
                    throw new ArgumentException(
                        "Only fallback-stage steps can use the fallback branch kind.");
                }

                foreach (var branch in step.Branches)
                {
                    if (branch.TargetStep is null)
                    {
                        continue;
                    }

                    RequireReference(
                        steps,
                        branch.TargetStep.StableKey,
                        "plan branch target");
                    var target = steps[branch.TargetStep.StableKey];
                    if (branch.Outcome == TacticalBranchOutcome.Fallback
                        && target.Stage != TacticalPlanStage.Fallback)
                    {
                        throw new ArgumentException(
                            "A fallback branch must target the fallback stage.");
                    }

                    if (branch.Outcome == TacticalBranchOutcome.Continue
                        && target.Stage == TacticalPlanStage.Fallback)
                    {
                        throw new ArgumentException(
                            "A fallback-stage target requires a fallback branch outcome.");
                    }

                    if ((int)target.Stage < (int)step.Stage
                        || (target.Stage == step.Stage
                            && target.Order <= step.Order))
                    {
                        throw new ArgumentException(
                            "A plan branch cannot target itself, an earlier step, or an earlier stage.");
                    }
                }
            }
        }

        HashSet<string> visiting = new(StringComparer.Ordinal);
        HashSet<string> visited = new(StringComparer.Ordinal);
        foreach (var step in steps.Values)
        {
            Visit(step, steps, visiting, visited);
        }
    }

    private static void Visit(
        TacticalPlanStep step,
        IReadOnlyDictionary<string, TacticalPlanStep> steps,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visited.Contains(step.StableKey))
        {
            return;
        }

        if (!visiting.Add(step.StableKey))
        {
            throw new ArgumentException("A tactical plan branch cycle is invalid.");
        }

        foreach (var target in step.Branches
            .Where(item => item.TargetStep is not null)
            .Select(item => steps[item.TargetStep!.StableKey]))
        {
            Visit(target, steps, visiting, visited);
        }

        visiting.Remove(step.StableKey);
        visited.Add(step.StableKey);
    }

    private IEnumerable<TacticalEvidenceReference> AllEvidence() =>
        SharedEvidence
            .Concat(Facts.SelectMany(item => item.AllEvidence))
            .Concat(Transitions.SelectMany(item => item.Evidence))
            .Concat(Roles.SelectMany(item => item.Evidence))
            .Concat(Candidates.SelectMany(item => item.AllEvidence))
            .Concat(Stages.SelectMany(item => item.AllEvidence));

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_COMBAT_PLAN_V1\n")
            .Append(GameDataVersion).Append('\n')
            .Append(RuleVersion).Append('\n')
            .Append("SEARCH|").Append(SearchCoverage.SemanticKey).Append('\n');
        Append(canonical, "EVIDENCE", SharedEvidence, item => item.StableKey);
        Append(canonical, "FACT", Facts, item => item.ContentKey);
        Append(
            canonical,
            "REQUIREMENT",
            Requirements,
            item => item.ContentKey);
        Append(
            canonical,
            "TRANSITION",
            Transitions,
            item => item.ContentKey);
        Append(canonical, "ROLE", Roles, item => item.ContentKey);
        Append(
            canonical,
            "CANDIDATE",
            Candidates,
            item => item.ContentKey);
        Append(canonical, "STAGE", Stages, item => item.ContentKey);
        return TacticalCombatText.Fingerprint(canonical.ToString());
    }

    private static void Append<T>(
        StringBuilder builder,
        string prefix,
        IEnumerable<T> values,
        Func<T, string> selector)
    {
        foreach (var value in values)
        {
            builder.Append(prefix).Append('|')
                .Append(selector(value)).Append('\n');
        }
    }

    private static void RequireReference<T>(
        IReadOnlyDictionary<string, T> values,
        string key,
        string referenceKind)
    {
        if (!values.ContainsKey(key))
        {
            throw new ArgumentException(
                $"A tactical {referenceKind} reference is dangling: {key}.");
        }
    }
}
