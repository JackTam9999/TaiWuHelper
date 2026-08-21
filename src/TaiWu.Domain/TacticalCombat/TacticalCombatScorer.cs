using System.Collections.Immutable;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public static class TacticalCombatScorer
{
    public const string ScoringVersion = "TACTICAL_SCORING@1.0.0";

    public static TacticalCombatScoringResult Score(
        TacticalCombatScoringRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateProofs(request);
        var weights = TacticalScoringPolicyWeights.For(request.Policy);
        var scored = request.SearchResult.FeasibleResults.Select(candidate =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ScoreCandidate(request, candidate, weights);
        }).OrderByDescending(item =>
            item.Candidate.Package.Recovery.State
                == TacticalPackageResolutionState.Unresolved ? 0 : 1)
            .ThenByDescending(item => item.TotalScore)
            .ThenByDescending(item => Value(
                item,
                TacticalScoreComponentKind.CausalValue))
            .ThenByDescending(item => Value(
                item,
                TacticalScoreComponentKind.LayeredProtection))
            .ThenByDescending(item => Value(
                item,
                TacticalScoreComponentKind.TimingOpportunity))
            .ThenBy(item => item.Candidate.StableKey, StringComparer.Ordinal)
            .ToArray();
        return new TacticalCombatScoringResult(
            request.SearchResult.SemanticFingerprint,
            weights,
            scored);
    }

    private static TacticalScoredLoadout ScoreCandidate(
        TacticalCombatScoringRequest request,
        TacticalFeasibleLoadoutResult candidate,
        TacticalScoringPolicyWeights weights)
    {
        var selected = candidate.Package.ScoringEligibleCandidates.ToHashSet();
        var entries = request.SearchRequest.Discovery.Entries
            .Where(item => selected.Contains(item.Consideration.Identity))
            .ToArray();
        var commonEvidence = CommonEvidence(request);
        ComponentDraft[] drafts =
        [
            CausalValue(request, entries, commonEvidence),
            LayeredProtection(request, selected, commonEvidence),
            TimingOpportunity(request, entries, commonEvidence),
            ExecutionReliability(request, candidate, entries, commonEvidence),
            RecoveryCost(request, candidate, entries, commonEvidence),
            FinishPath(request, selected, commonEvidence)
        ];
        var availableWeight = drafts.Where(item => item.IsAvailable)
            .Sum(item => weights.Get(item.Kind));
        var components = drafts.Select(draft => CreateComponent(
            draft,
            weights.Get(draft.Kind),
            availableWeight));
        return new TacticalScoredLoadout(
            candidate,
            request.Policy,
            components,
            UnusedCapacity(candidate));
    }

    private static ComponentDraft CausalValue(
        TacticalCombatScoringRequest request,
        TacticalCandidateDiscoveryEntry[] selectedEntries,
        ImmutableArray<TacticalEvidenceReference> commonEvidence)
    {
        var roleByIdentity = request.SearchRequest.RuleResolution.Roles
            .Where(item => item.Applicability
                == TacticalRuleApplicability.Applicable)
            .ToDictionary(item => item.Rule.Identity);
        var transitionByIdentity = request.SearchRequest.RuleResolution
            .Transitions.Where(item => item.Applicability
                == TacticalRuleApplicability.Applicable)
            .ToDictionary(item => item.Rule.Identity);
        var eligible = request.SearchResult.CandidateDecisions
            .Where(item => item.Decision == TacticalCandidateDecision.Admitted)
            .SelectMany(item => item.Roles)
            .Where(roleByIdentity.ContainsKey)
            .SelectMany(role => roleByIdentity[role].Rule.Transitions)
            .Where(transitionByIdentity.ContainsKey)
            .Distinct()
            .OrderBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        var covered = selectedEntries
            .Where(item => item.Role is not null)
            .SelectMany(item => item.Role!.TransitionIdentities)
            .Where(transitionByIdentity.ContainsKey)
            .ToHashSet();
        List<TacticalScoreRawInput> inputs = [.. eligible.Select(identity =>
        {
            var transition = transitionByIdentity[identity].Rule;
            var isCovered = covered.Contains(identity);
            return AvailableInput(
                isCovered
                    ? TacticalScoreInputKind.CoveredTransition
                    : TacticalScoreInputKind.ApplicableTransition,
                identity.Code,
                TacticalFactValue.Boolean(isCovered),
                isCovered
                    ? "DISTINCT_TRANSITION_COVERED"
                    : "DISTINCT_TRANSITION_NOT_COVERED",
                transition.Evidence);
        })];
        foreach (var group in eligible.Select(identity =>
                     transitionByIdentity[identity].Rule)
                     .SelectMany(transition => transition.TriggerFacts.Select(
                         fact => new { transition, fact }))
                     .GroupBy(item => item.fact))
        {
            var stateCovered = group.Any(item =>
                covered.Contains(item.transition.Identity));
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.CausalTriggerState,
                $"{TacticalCombatText.EnumKey(group.Key.Kind)}:{group.Key.Code}",
                TacticalFactValue.Boolean(stateCovered),
                stateCovered
                    ? "CAUSAL_TRIGGER_STATE_COVERED"
                    : "CAUSAL_TRIGGER_STATE_APPLICABLE",
                group.SelectMany(item => item.transition.Evidence)
                    .DistinctBy(
                        item => item.StableKey,
                        StringComparer.Ordinal)));
        }

        foreach (var group in eligible.Select(identity =>
                     transitionByIdentity[identity].Rule)
                     .SelectMany(transition => transition.ResultingFacts.Select(
                         fact => new { transition, fact }))
                     .GroupBy(item => item.fact))
        {
            var stateCovered = group.Any(item =>
                covered.Contains(item.transition.Identity));
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.CausalResultingState,
                $"{TacticalCombatText.EnumKey(group.Key.Kind)}:{group.Key.Code}",
                TacticalFactValue.Boolean(stateCovered),
                stateCovered
                    ? "CAUSAL_RESULTING_STATE_COVERED"
                    : "CAUSAL_RESULTING_STATE_APPLICABLE",
                group.SelectMany(item => item.transition.Evidence)
                    .DistinctBy(
                        item => item.StableKey,
                        StringComparer.Ordinal)));
        }

        if (inputs.Count == 0)
        {
            inputs =
            [
                AvailableInput(
                    TacticalScoreInputKind.NoTacticalAction,
                    "NO_ADMITTED_CAUSAL_TRANSITION",
                    TacticalFactValue.Boolean(false),
                    "NO_ADMITTED_CAUSAL_TRANSITION",
                    commonEvidence)
            ];
        }

        var normalized = eligible.Length == 0
            ? 0
            : Percent(covered.Count(identity => eligible.Contains(identity)),
                eligible.Length);
        return AvailableDraft(
            TacticalScoreComponentKind.CausalValue,
            inputs,
            "DISTINCT_APPLICABLE_TRANSITION_RATIO_V1",
            normalized,
            inputs.SelectMany(item => item.Evidence),
            eligible.Length == 0 ? ["NO_ADMITTED_CAUSAL_TRANSITION"] : []);
    }

    private static ComponentDraft LayeredProtection(
        TacticalCombatScoringRequest request,
        HashSet<TacticalCandidateIdentity> selected,
        ImmutableArray<TacticalEvidenceReference> commonEvidence)
    {
        var proofs = request.LayeringProofs.Where(item =>
                selected.Contains(item.PrimaryCandidate)
                && selected.Contains(item.LayeredCandidate))
            .ToArray();
        if (proofs.Length == 0)
        {
            var input = AvailableInput(
                TacticalScoreInputKind.LayeringInteraction,
                "NO_DOCUMENTED_LAYERING_RULE",
                TacticalFactValue.Boolean(false),
                "NO_DOCUMENTED_LAYERING_VALUE",
                commonEvidence);
            return AvailableDraft(
                TacticalScoreComponentKind.LayeredProtection,
                [input],
                "DOCUMENTED_LAYER_MARGINAL_UNITS_V1",
                0,
                input.Evidence,
                ["NO_DOCUMENTED_LAYERING_VALUE"]);
        }

        var inputs = proofs.Select(proof => AvailableInput(
            TacticalScoreInputKind.LayeringInteraction,
            LayerIdentity(proof),
            TacticalFactValue.Integer(LayerValue(proof.Kind)),
            "DOCUMENTED_LAYERING_VALUE",
            proof.Evidence)).ToArray();
        return AvailableDraft(
            TacticalScoreComponentKind.LayeredProtection,
            inputs,
            "DOCUMENTED_LAYER_MARGINAL_UNITS_V1",
            Math.Min(100, proofs.Sum(item => LayerValue(item.Kind))),
            inputs.SelectMany(item => item.Evidence),
            proofs.Select(item => item.LimitationIdentity));
    }

    private static ComponentDraft TimingOpportunity(
        TacticalCombatScoringRequest request,
        TacticalCandidateDiscoveryEntry[] entries,
        ImmutableArray<TacticalEvidenceReference> commonEvidence)
    {
        if (entries.Length == 0)
        {
            var input = AvailableInput(
                TacticalScoreInputKind.NoTacticalAction,
                "NO_TACTICAL_TIMING_WINDOW",
                TacticalFactValue.Boolean(false),
                "NO_TACTICAL_ACTION_SELECTED",
                commonEvidence);
            return AvailableDraft(
                TacticalScoreComponentKind.TimingOpportunity,
                [input],
                "SUPPORTED_TIMING_WINDOW_AVERAGE_V1",
                0,
                input.Evidence,
                ["NO_TACTICAL_ACTION_SELECTED"]);
        }

        List<TacticalScoreRawInput> inputs = [];
        List<string> limitations = [];
        var roleScores = new List<int>();
        foreach (var entry in entries)
        {
            var role = entry.Role!;
            roleScores.Add(TimingValue(role.Timing));
            var roleRule = Role(request, role.Identity).Rule;
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.TimingWindow,
                role.Identity.Code,
                TacticalFactValue.Integer(TimingValue(role.Timing)),
                "VERIFIED_ROLE_TIMING",
                roleRule.Evidence));
            if (RequiresPreparation(request.SearchRequest.Player, entry))
            {
                inputs.Add(AvailableInput(
                    TacticalScoreInputKind.PreparationStep,
                    entry.StableKey,
                    TacticalFactValue.Boolean(true),
                    "DIRECTION_PREPARATION_REQUIRED",
                    entry.Consideration.Evidence));
                limitations.Add("DIRECTION_PREPARATION_REQUIRED");
            }
        }

        inputs.AddRange(ObservabilityInputs(request, entries, limitations));
        var unavailable = inputs.Any(item =>
            item.Kind == TacticalScoreInputKind.TriggerObservability
            && item.State != TacticalEvidenceState.Available);
        if (unavailable)
        {
            return UnavailableDraft(
                TacticalScoreComponentKind.TimingOpportunity,
                inputs,
                "SUPPORTED_TIMING_WINDOW_AVERAGE_V1",
                inputs.SelectMany(item => item.Evidence),
                limitations.Append("TRIGGER_OBSERVABILITY_UNAVAILABLE"));
        }

        var preparationPenalty = entries.Count(item =>
            RequiresPreparation(request.SearchRequest.Player, item)) * 10;
        return AvailableDraft(
            TacticalScoreComponentKind.TimingOpportunity,
            inputs,
            "SUPPORTED_TIMING_WINDOW_AVERAGE_V1",
            Clamp((decimal)roleScores.Sum() / roleScores.Count
                - preparationPenalty),
            inputs.SelectMany(item => item.Evidence),
            limitations);
    }

    private static ComponentDraft ExecutionReliability(
        TacticalCombatScoringRequest request,
        TacticalFeasibleLoadoutResult candidate,
        TacticalCandidateDiscoveryEntry[] entries,
        ImmutableArray<TacticalEvidenceReference> commonEvidence)
    {
        if (entries.Length == 0)
        {
            var input = AvailableInput(
                TacticalScoreInputKind.NoTacticalAction,
                "NO_TACTICAL_EXECUTION",
                TacticalFactValue.Boolean(false),
                "NO_TACTICAL_ACTION_SELECTED",
                commonEvidence);
            return AvailableDraft(
                TacticalScoreComponentKind.ExecutionReliability,
                [input],
                "OBSERVABLE_EXECUTION_REQUIREMENTS_V1",
                0,
                input.Evidence,
                ["NO_TACTICAL_ACTION_SELECTED"]);
        }

        List<string> limitations = [];
        var inputs = entries.Select(entry => AvailableInput(
                TacticalScoreInputKind.ExecutionRequirement,
                $"ACTION:{entry.StableKey}",
                TacticalFactValue.Boolean(true),
                "TACTICAL_ACTION_FEASIBLE",
                entry.Consideration.Evidence))
            .Concat(ObservabilityInputs(request, entries, limitations))
            .ToList();
        var proposal = candidate.Loadout.Proposal;
        var evaluations = CombatRequirementEvaluator.Evaluate(
            proposal.Requirements,
            proposal.RequirementContext).Evaluations
            .OrderBy(item => item.Requirement.GetType().Name,
                StringComparer.Ordinal)
            .ThenBy(item => item.Requirement.EvidenceReference,
                StringComparer.Ordinal)
            .ToArray();
        for (var index = 0; index < evaluations.Length; index++)
        {
            var evaluation = evaluations[index];
            var available = evaluation.Status == CombatRequirementStatus.Satisfied;
            var kind = evaluation.Requirement is ResourceRequirement
                ? TacticalScoreInputKind.ResourceRequirement
                : TacticalScoreInputKind.ExecutionRequirement;
            inputs.Add(new TacticalScoreRawInput(
                kind,
                $"REQUIREMENT_{index:000}",
                available
                    ? TacticalEvidenceState.Available
                    : TacticalEvidenceState.Incomplete,
                available ? TacticalFactValue.Boolean(true) : null,
                available
                    ? "EXECUTION_REQUIREMENT_SATISFIED"
                    : "EXECUTION_REQUIREMENT_UNRESOLVED",
                commonEvidence));
            if (!available)
            {
                limitations.Add("EXECUTION_REQUIREMENT_UNRESOLVED");
            }
        }

        foreach (var entry in entries.Where(item =>
                     RequiresPreparation(request.SearchRequest.Player, item)))
        {
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.PreparationStep,
                entry.StableKey,
                TacticalFactValue.Boolean(true),
                "DIRECTION_PREPARATION_REQUIRED",
                entry.Consideration.Evidence));
            limitations.Add("DIRECTION_PREPARATION_REQUIRED");
        }

        if (inputs.Any(item => item.State != TacticalEvidenceState.Available))
        {
            return UnavailableDraft(
                TacticalScoreComponentKind.ExecutionReliability,
                inputs,
                "OBSERVABLE_EXECUTION_REQUIREMENTS_V1",
                inputs.SelectMany(item => item.Evidence),
                limitations);
        }

        var preparation = entries.Count(item =>
            RequiresPreparation(request.SearchRequest.Player, item));
        var activeComplexity = entries.Count(item => item.Role!.Timing is
            TacticalTransitionTiming.DuringCast
            or TacticalTransitionTiming.AfterCast
            or TacticalTransitionTiming.AfterManualAction);
        var resources = evaluations.Count(item =>
            item.Requirement is ResourceRequirement);
        return AvailableDraft(
            TacticalScoreComponentKind.ExecutionReliability,
            inputs,
            "OBSERVABLE_EXECUTION_REQUIREMENTS_V1",
            Clamp(100 - preparation * 15 - activeComplexity * 5
                - resources * 10),
            inputs.SelectMany(item => item.Evidence),
            limitations);
    }

    private static ComponentDraft RecoveryCost(
        TacticalCombatScoringRequest request,
        TacticalFeasibleLoadoutResult candidate,
        TacticalCandidateDiscoveryEntry[] entries,
        ImmutableArray<TacticalEvidenceReference> commonEvidence)
    {
        List<TacticalScoreRawInput> inputs = [];
        List<string> limitations = [];
        var penalty = 0;
        if (candidate.Package.Recovery.State
            == TacticalPackageResolutionState.Unresolved)
        {
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.RecoveryRoute,
                candidate.Package.Recovery.ReasonIdentity,
                TacticalFactValue.Boolean(false),
                "REVERSE_604_RECOVERY_BRANCH_UNRESOLVED",
                commonEvidence));
            limitations.Add(candidate.Package.Recovery.ReasonIdentity);
            penalty += 100;
        }
        else if (candidate.Package.Recovery.State
                 == TacticalPackageResolutionState.Complete)
        {
            foreach (var step in candidate.Package.Recovery.CastSteps)
            {
                inputs.Add(AvailableInput(
                    TacticalScoreInputKind.RecoveryRoute,
                    $"RECOVERY_CAST_{step.Sequence}:{step.Candidate.StableKey}",
                    TacticalFactValue.Integer(step.EffectiveSlotCost),
                    "EXACT_REVERSE_RECOVERY_CAST_RESOLVED",
                    commonEvidence));
            }
        }

        foreach (var entry in entries.Where(item =>
                     RequiresPreparation(request.SearchRequest.Player, item)))
        {
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.PreparationStep,
                entry.StableKey,
                TacticalFactValue.Integer(15),
                "PREPARATION_RECOVERY_BURDEN",
                entry.Consideration.Evidence));
            penalty += 15;
            limitations.Add("DIRECTION_PREPARATION_REQUIRED");
        }

        var selectedTransitions = entries.SelectMany(item =>
                item.Role!.TransitionIdentities)
            .Select(identity => Transition(request, identity))
            .ToArray();
        foreach (var match in selectedTransitions.Where(item =>
                     item.Rule.Purpose
                         == TacticalRulePurpose.DirectPracticeSelfLock))
        {
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.SelfLock,
                match.Rule.Identity.Code,
                TacticalFactValue.Integer(45),
                "VERIFIED_SELF_LOCK_RECOVERY_BURDEN",
                match.Rule.Evidence));
            penalty += 45;
            limitations.Add(match.Rule.LimitationIdentity);
        }

        var hasSelfLock = selectedTransitions.Any(item => item.Rule.Purpose
            == TacticalRulePurpose.DirectPracticeSelfLock);
        if (hasSelfLock)
        {
            var recovery = request.SearchRequest.RuleResolution.Transitions
                .FirstOrDefault(item => item.Applicability
                    == TacticalRuleApplicability.Applicable
                    && item.Rule.Purpose
                        == TacticalRulePurpose.DirectPracticeLockRecovery);
            if (recovery is not null)
            {
                inputs.Add(AvailableInput(
                    TacticalScoreInputKind.RecoveryRoute,
                    recovery.Rule.Identity.Code,
                    TacticalFactValue.Boolean(true),
                    "VERIFIED_RECOVERY_ROUTE_AVAILABLE",
                    recovery.Rule.Evidence));
                limitations.Add(recovery.Rule.LimitationIdentity);
            }
        }

        foreach (var requirement in candidate.Loadout.Proposal.Requirements
                     .OfType<ResourceRequirement>()
                     .OrderBy(item => item.Resource))
        {
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.ResourceRequirement,
                $"RESOURCE_{TacticalCombatText.EnumKey(requirement.Resource)}:"
                + requirement.EvidenceReference,
                TacticalFactValue.Integer(requirement.MinimumAmount),
                "RESOURCE_READINESS_REQUIREMENT",
                commonEvidence));
            limitations.Add("RESOURCE_USE_NOT_INFERRED_FROM_READINESS");
        }

        if (inputs.Count == 0)
        {
            inputs.Add(AvailableInput(
                TacticalScoreInputKind.RecoveryRoute,
                "NO_VERIFIED_RECOVERY_BURDEN",
                TacticalFactValue.Boolean(true),
                "NO_VERIFIED_RECOVERY_BURDEN",
                commonEvidence));
        }

        return AvailableDraft(
            TacticalScoreComponentKind.RecoveryCost,
            inputs,
            "VERIFIED_RECOVERY_BURDEN_DEDUCTION_V1",
            Clamp(100 - penalty),
            inputs.SelectMany(item => item.Evidence),
            limitations);
    }

    private static ComponentDraft FinishPath(
        TacticalCombatScoringRequest request,
        HashSet<TacticalCandidateIdentity> selected,
        ImmutableArray<TacticalEvidenceReference> commonEvidence)
    {
        var applicable = request.FinishProofs.Where(item =>
                selected.Contains(item.ChannelCandidate)
                && selected.Contains(item.FinishCandidate))
            .Select(item => new
            {
                Proof = item,
                Score = FinishScore(item)
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Proof.StableKey, StringComparer.Ordinal)
            .FirstOrDefault();
        if (applicable is null)
        {
            var kinds = Enum.GetValues<TacticalFinishEvidenceKind>();
            var unavailableInputs = kinds.Select(kind =>
                new TacticalScoreRawInput(
                FinishInputKind(kind),
                $"{TacticalCombatText.EnumKey(kind)}_UNAVAILABLE",
                TacticalEvidenceState.Unsupported,
                value: null,
                "FINISH_EVIDENCE_UNAVAILABLE",
                commonEvidence)).ToArray();
            return UnavailableDraft(
                TacticalScoreComponentKind.FinishPath,
                unavailableInputs,
                "SUPPORTED_FINISH_MARGIN_RELIABILITY_V1",
                commonEvidence,
                ["FALLBACK_ONLY_NO_TYPED_FINISH_EVIDENCE"]);
        }

        var proof = applicable.Proof;
        var inputs = proof.Inputs.Select(item => AvailableInput(
            FinishInputKind(item.Kind),
            item.Identity,
            item.Value,
            "TYPED_FINISH_INPUT_AVAILABLE",
            item.Evidence)).ToArray();
        return AvailableDraft(
            TacticalScoreComponentKind.FinishPath,
            inputs,
            "SUPPORTED_FINISH_MARGIN_RELIABILITY_V1",
            applicable.Score,
            inputs.SelectMany(item => item.Evidence),
            [proof.LimitationIdentity]);
    }

    private static IEnumerable<TacticalScoreRawInput> ObservabilityInputs(
        TacticalCombatScoringRequest request,
        TacticalCandidateDiscoveryEntry[] entries,
        List<string> limitations)
    {
        var required = entries.SelectMany(item =>
                item.Role!.TransitionIdentities)
            .Select(identity => Transition(request, identity))
            .Where(item => RequiresTriggerObservation(item.Rule))
            .DistinctBy(item => item.Rule.Identity)
            .OrderBy(item => item.Rule.Identity.StableKey, StringComparer.Ordinal);
        foreach (var transition in required)
        {
            var observation = request.TriggerObservations.SingleOrDefault(
                item => item.Transition == transition.Rule.Identity);
            if (observation is null)
            {
                limitations.Add("TRIGGER_OBSERVABILITY_UNAVAILABLE");
                yield return new TacticalScoreRawInput(
                    TacticalScoreInputKind.TriggerObservability,
                    transition.Rule.Identity.Code,
                    TacticalEvidenceState.Incomplete,
                    value: null,
                    "TRIGGER_OBSERVABILITY_UNAVAILABLE",
                    transition.Rule.Evidence);
                continue;
            }

            if (observation.State != TacticalEvidenceState.Available)
            {
                limitations.Add(observation.LimitationIdentity);
            }

            yield return new TacticalScoreRawInput(
                TacticalScoreInputKind.TriggerObservability,
                transition.Rule.Identity.Code,
                observation.State,
                observation.State == TacticalEvidenceState.Available
                    ? TacticalFactValue.Boolean(true)
                    : null,
                observation.ReasonIdentity,
                observation.Evidence);
        }
    }

    private static bool RequiresTriggerObservation(
        TacticalTransitionRule rule) => (rule.Timing is
            TacticalTransitionTiming.DuringCast
            or TacticalTransitionTiming.OnObservedState)
        && rule.TriggerFacts.Any(item => item.Kind is
            TacticalFactKind.TargetSkillPhase
            or TacticalFactKind.Mark
            or TacticalFactKind.Resonance
            or TacticalFactKind.Resource
            or TacticalFactKind.Other);

    private static bool RequiresPreparation(
        PlayerCombatSnapshot player,
        TacticalCandidateDiscoveryEntry entry)
    {
        if (entry.RequiresBreakthrough)
        {
            return true;
        }

        var skill = player.LearnedSkills.Single(item =>
            item.SkillId == entry.SkillId);
        return skill.Direction.IsAvailable
            && skill.Direction.Value != entry.Direction;
    }

    private static TacticalSkillRoleRuleMatch Role(
        TacticalCombatScoringRequest request,
        TacticalRoleIdentity identity) => request.SearchRequest.RuleResolution
        .Roles.Single(item => item.Rule.Identity == identity);

    private static TacticalTransitionRuleMatch Transition(
        TacticalCombatScoringRequest request,
        TacticalTransitionIdentity identity) => request.SearchRequest
        .RuleResolution.Transitions.Single(item => item.Rule.Identity == identity);

    private static TacticalUnusedCapacityFact UnusedCapacity(
        TacticalFeasibleLoadoutResult candidate) => new(
        candidate.Loadout.SlotBudgets.Values.Select(budget =>
            new TacticalUnusedCapacityEntry(
                budget.Category,
                budget.Remaining.Value,
                budget.Capacity)),
        ["PROPOSED_SLOT_BUDGETS", "UNUSED_CAPACITY_NEUTRAL_V1"]);

    private static void ValidateProofs(TacticalCombatScoringRequest request)
    {
        var contextFingerprint = request.SearchRequest.Context
            .SemanticFingerprint;
        var entries = request.SearchRequest.Discovery.Entries.ToDictionary(
            item => item.Consideration.Identity);
        var decisions = request.SearchResult.CandidateDecisions.ToDictionary(
            item => item.Identity);
        var transitions = request.SearchRequest.RuleResolution.Transitions
            .ToDictionary(item => item.Rule.Identity);
        var roles = request.SearchRequest.RuleResolution.Roles.ToDictionary(
            item => item.Rule.Identity);

        foreach (var observation in request.TriggerObservations)
        {
            if (!transitions.TryGetValue(observation.Transition, out var match)
                || match.Applicability != TacticalRuleApplicability.Applicable)
            {
                throw new ArgumentException(
                    "Trigger observations require an applicable resolved transition.",
                    nameof(request));
            }

            ValidateEvidenceVersions(observation.Evidence, request);
        }

        foreach (var proof in request.LayeringProofs)
        {
            if (!string.Equals(
                    proof.ContextSemanticFingerprint,
                    contextFingerprint,
                    StringComparison.Ordinal)
                || !entries.TryGetValue(proof.PrimaryCandidate, out var primary)
                || !entries.TryGetValue(proof.LayeredCandidate, out var layered)
                || !primary.IsAdmitted
                || !layered.IsAdmitted
                || decisions[proof.PrimaryCandidate].Decision
                    != TacticalCandidateDecision.Admitted
                || decisions[proof.LayeredCandidate].Decision
                    != TacticalCandidateDecision.Admitted
                || !transitions.TryGetValue(
                    proof.MarginalTransition,
                    out var transition)
                || transition.Applicability
                    != TacticalRuleApplicability.Applicable
                || layered.Role is null
                || !layered.Role.TransitionIdentities.Contains(
                    proof.MarginalTransition))
            {
                throw new ArgumentException(
                    "Layering requires two admitted candidates and an applicable transition of the layered candidate in this context.",
                    nameof(request));
            }

            ValidateEvidenceVersions(proof.Evidence, request);
        }

        foreach (var proof in request.FinishProofs)
        {
            if (!string.Equals(
                    proof.ContextSemanticFingerprint,
                    contextFingerprint,
                    StringComparison.Ordinal)
                || !entries.TryGetValue(proof.ChannelCandidate, out var channel)
                || !entries.TryGetValue(proof.FinishCandidate, out var finish)
                || !channel.IsAdmitted
                || !finish.IsAdmitted
                || decisions[proof.ChannelCandidate].Decision
                    != TacticalCandidateDecision.Admitted
                || decisions[proof.FinishCandidate].Decision
                    != TacticalCandidateDecision.Admitted
                || channel.Role?.Identity != proof.ChannelRole
                || finish.Role?.Identity != proof.FinishRole
                || !roles.TryGetValue(proof.ChannelRole, out var channelRole)
                || !roles.TryGetValue(proof.FinishRole, out var finishRole)
                || channelRole.Applicability
                    != TacticalRuleApplicability.Applicable
                || finishRole.Applicability
                    != TacticalRuleApplicability.Applicable
                || channelRole.Rule.Purpose
                    != TacticalRulePurpose.DamageChannelChoice
                || channelRole.Rule.Identity.Kind
                    != TacticalRoleKind.DamageChannel
                || finishRole.Rule.Purpose
                    != TacticalRulePurpose.FinishWindowSupport
                || finishRole.Rule.Identity.Kind != TacticalRoleKind.Finish
                || !finishRole.Rule.Transitions.Contains(proof.FinishTransition)
                || !transitions.TryGetValue(
                    proof.FinishTransition,
                    out var finishTransition)
                || finishTransition.Applicability
                    != TacticalRuleApplicability.Applicable
                || finishTransition.Rule.Purpose
                    != TacticalRulePurpose.FinishWindowSupport)
            {
                throw new ArgumentException(
                    "Finish scoring requires admitted typed channel and finish roles plus an applicable finish transition in this context.",
                    nameof(request));
            }

            ValidateEvidenceVersions(
                proof.Inputs.SelectMany(item => item.Evidence),
                request);
        }
    }

    private static void ValidateEvidenceVersions(
        IEnumerable<TacticalEvidenceReference> evidence,
        TacticalCombatScoringRequest request)
    {
        if (evidence.Any(item => !string.Equals(
                    item.GameDataVersion,
                    request.SearchRequest.RuleResolution.GameDataVersion,
                    StringComparison.Ordinal)
                || !string.Equals(
                    item.RuleVersion,
                    VerifiedTacticalCombatRuleSets.RuleVersion,
                    StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "Scoring evidence must match the resolved GameData and tactical rule versions.",
                nameof(request));
        }
    }

    private static ImmutableArray<TacticalEvidenceReference> CommonEvidence(
        TacticalCombatScoringRequest request)
    {
        var evidence = request.SearchRequest.RuleResolution.Transitions
            .SelectMany(item => item.Rule.Evidence)
            .Concat(request.SearchRequest.RuleResolution.Roles
                .SelectMany(item => item.Rule.Evidence))
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .OrderBy(item => item.StableKey, StringComparer.Ordinal)
            .ToImmutableArray();
        if (!evidence.IsEmpty)
        {
            return evidence;
        }

        return
        [
            new TacticalEvidenceReference(
                TacticalEvidenceSourceKind.SaveSnapshot,
                "TACTICAL_SCORING_CONTEXT",
                request.SearchRequest.RuleResolution.GameDataVersion,
                VerifiedTacticalCombatRuleSets.RuleVersion,
                $"CONTEXT:{request.SearchRequest.Context.SemanticFingerprint}")
        ];
    }

    private static TacticalScoreComponent CreateComponent(
        ComponentDraft draft,
        int baseWeight,
        int availableWeight)
    {
        decimal? applied = draft.IsAvailable
            ? (decimal)baseWeight / availableWeight
            : null;
        decimal? contribution = draft.IsAvailable
            ? decimal.Round(
                draft.NormalizedValue!.Value * applied!.Value,
                4,
                MidpointRounding.AwayFromZero)
            : null;
        return new TacticalScoreComponent(
            draft.Kind,
            draft.IsAvailable
                ? TacticalScoreComponentState.Available
                : TacticalScoreComponentState.Unavailable,
            draft.Inputs,
            draft.NormalizationIdentity,
            draft.NormalizedValue,
            baseWeight,
            applied,
            contribution,
            draft.Evidence.DistinctBy(
                item => item.StableKey,
                StringComparer.Ordinal),
            draft.Limitations);
    }

    private static ComponentDraft AvailableDraft(
        TacticalScoreComponentKind kind,
        IEnumerable<TacticalScoreRawInput> inputs,
        string normalizationIdentity,
        decimal value,
        IEnumerable<TacticalEvidenceReference> evidence,
        IEnumerable<string> limitations) => new(
        kind,
        true,
        inputs,
        normalizationIdentity,
        Clamp(value),
        evidence,
        limitations);

    private static ComponentDraft UnavailableDraft(
        TacticalScoreComponentKind kind,
        IEnumerable<TacticalScoreRawInput> inputs,
        string normalizationIdentity,
        IEnumerable<TacticalEvidenceReference> evidence,
        IEnumerable<string> limitations) => new(
        kind,
        false,
        inputs,
        normalizationIdentity,
        null,
        evidence,
        limitations);

    private static TacticalScoreRawInput AvailableInput(
        TacticalScoreInputKind kind,
        string identity,
        TacticalFactValue value,
        string reason,
        IEnumerable<TacticalEvidenceReference> evidence) => new(
        kind,
        identity,
        TacticalEvidenceState.Available,
        value,
        reason,
        evidence);

    private static decimal FinishScore(TacticalFinishPathProof proof)
    {
        var attack = proof.Integer(
            TacticalFinishEvidenceKind.AttackChannelStrength);
        var reliability = proof.Integer(
            TacticalFinishEvidenceKind.HitOrCastReliabilityPercent);
        var resistance = proof.Integer(
            TacticalFinishEvidenceKind.TargetDefenseOrResistance);
        var margin = Math.Max(0, attack - resistance);
        return decimal.Round(
            Percent(margin, attack) * reliability / 100m,
            4,
            MidpointRounding.AwayFromZero);
    }

    private static TacticalScoreInputKind FinishInputKind(
        TacticalFinishEvidenceKind kind) => kind switch
        {
            TacticalFinishEvidenceKind.AttackChannelStrength =>
                TacticalScoreInputKind.FinishAttackChannel,
            TacticalFinishEvidenceKind.HitOrCastReliabilityPercent =>
                TacticalScoreInputKind.FinishReliabilityPercent,
            TacticalFinishEvidenceKind.TargetDefenseOrResistance =>
                TacticalScoreInputKind.FinishTargetResistance,
            TacticalFinishEvidenceKind.ApplicableCondition =>
                TacticalScoreInputKind.FinishCondition,
            TacticalFinishEvidenceKind.FinishWindow =>
                TacticalScoreInputKind.FinishWindow,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static int LayerValue(TacticalLayeringKind kind) => kind switch
    {
        TacticalLayeringKind.VerifiedInteraction => 100,
        TacticalLayeringKind.FailureFallback => 80,
        TacticalLayeringKind.DifferentTimingWindow => 60,
        TacticalLayeringKind.SeparateMitigation => 50,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string LayerIdentity(TacticalLayeringProof proof) =>
        $"{TacticalCombatText.EnumKey(proof.Kind)}:{proof.MarginalTransition.Code}";

    private static int TimingValue(TacticalTransitionTiming timing) =>
        timing switch
        {
            TacticalTransitionTiming.CombatStart => 100,
            TacticalTransitionTiming.BeforeCombat => 95,
            TacticalTransitionTiming.BeforeFirstUse => 85,
            TacticalTransitionTiming.DuringCast => 70,
            TacticalTransitionTiming.AfterCast => 65,
            TacticalTransitionTiming.OnObservedState => 60,
            TacticalTransitionTiming.AfterManualAction => 50,
            _ => throw new ArgumentOutOfRangeException(nameof(timing))
        };

    private static decimal Value(
        TacticalScoredLoadout candidate,
        TacticalScoreComponentKind kind) =>
        candidate.Get(kind).NormalizedValue ?? -1;

    private static decimal Percent(decimal value, decimal total) =>
        total == 0 ? 0 : Clamp(value * 100 / total);

    private static decimal Clamp(decimal value) =>
        Math.Clamp(value, 0, 100);

    private sealed record ComponentDraft(
        TacticalScoreComponentKind Kind,
        bool IsAvailable,
        IEnumerable<TacticalScoreRawInput> Inputs,
        string NormalizationIdentity,
        decimal? NormalizedValue,
        IEnumerable<TacticalEvidenceReference> Evidence,
        IEnumerable<string> Limitations);
}
