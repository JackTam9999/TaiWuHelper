using System.Collections.Immutable;
using System.Text;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public static class TacticalCombatPlanCompiler
{
    public const string CompilerVersion = "TACTICAL_COMBAT_PLAN_COMPILER@1.0.0";

    public static TacticalCompiledCombatPlan Compile(
        TacticalPlanCompilationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var scoringRequest = request.ScoringRequest;
        var searchRequest = scoringRequest.SearchRequest;
        var context = searchRequest.Context;
        var resolution = searchRequest.RuleResolution;
        ValidateCoherence(request);

        var selected = request.ScoringResult.RankedCandidates.Single(item =>
            string.Equals(
                item.Candidate.StableKey,
                request.SelectedLoadoutStableKey,
                StringComparison.Ordinal));
        ValidateSelection(searchRequest, selected);
        cancellationToken.ThrowIfCancellationRequested();

        var snapshotEvidence = SnapshotEvidence(resolution, context);
        var projection = ProjectRules(
            scoringRequest,
            snapshotEvidence,
            cancellationToken);
        var selectedRoles = SelectedRoles(
            selected,
            resolution,
            cancellationToken);
        var preparationChecks = BuildPreparationChecks(
            searchRequest,
            selected,
            selectedRoles);
        AddPreparationContracts(
            preparationChecks,
            projection,
            snapshotEvidence);
        AddCandidateRequirementContracts(
            scoringRequest.SearchResult.CandidateDecisions,
            projection,
            snapshotEvidence);

        var drafts = BuildStepDrafts(
            scoringRequest,
            selected,
            selectedRoles,
            preparationChecks,
            projection,
            snapshotEvidence,
            cancellationToken);
        var stages = MaterializeStages(drafts, projection.SharedEvidence);
        var plan = new TacticalCombatPlan(
            resolution.GameDataVersion,
            VerifiedTacticalCombatRuleSets.RuleVersion,
            projection.SharedEvidence,
            projection.Facts.Values,
            projection.Requirements.Values,
            projection.Transitions.Values,
            projection.Roles.Values,
            scoringRequest.SearchResult.CandidateDecisions,
            scoringRequest.SearchResult.Coverage,
            stages);
        var finish = stages.Single(item =>
            item.Stage == TacticalPlanStage.Finish);
        var fallback = stages.Single(item =>
            item.Stage == TacticalPlanStage.Fallback);
        var disposition = finish.State == TacticalPlanStageState.Supported
            ? TacticalFinishDisposition.Supported
            : fallback.State == TacticalPlanStageState.Supported
                ? TacticalFinishDisposition.FallbackOnly
                : TacticalFinishDisposition.Unsupported;

        return new TacticalCompiledCombatPlan(
            selected,
            plan,
            disposition,
            preparationChecks,
            context.SemanticFingerprint,
            scoringRequest.SearchResult.SemanticFingerprint,
            request.ScoringResult.SemanticFingerprint,
            ObservationFingerprint(
                context,
                scoringRequest.TriggerObservations));
    }

    private static string ObservationFingerprint(
        TacticalExecutionContext context,
        IEnumerable<TacticalTriggerObservability> observations)
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_PLAN_OBSERVATION_V1\n")
            .Append(context.ObservationRevisionFingerprint).Append('\n');
        foreach (var observation in observations.OrderBy(
            item => item.StableKey,
            StringComparer.Ordinal))
        {
            canonical.Append(observation.StableKey).Append('|')
                .Append(TacticalCombatText.EnumKey(observation.State))
                .Append('|').Append(observation.ReasonIdentity)
                .Append('|').Append(observation.LimitationIdentity)
                .Append('|').AppendJoin(
                    "||",
                    observation.Evidence.Select(item => item.StableKey))
                .Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }

    private static void ValidateCoherence(TacticalPlanCompilationRequest request)
    {
        var scoringRequest = request.ScoringRequest;
        var searchRequest = scoringRequest.SearchRequest;
        if (!string.Equals(
                searchRequest.Context.SemanticFingerprint,
                scoringRequest.SearchResult.ContextSemanticFingerprint,
                StringComparison.Ordinal)
            || !string.Equals(
                scoringRequest.SearchResult.SemanticFingerprint,
                request.ScoringResult.SearchSemanticFingerprint,
                StringComparison.Ordinal)
            || !string.Equals(
                searchRequest.Context.RuleSetFingerprint,
                searchRequest.RuleResolution.RuleSetFingerprint,
                StringComparison.Ordinal)
            || !searchRequest.Context.GameDataVersion.IsAvailable
            || !string.Equals(
                searchRequest.Context.GameDataVersion.Value,
                searchRequest.RuleResolution.GameDataVersion,
                StringComparison.Ordinal)
            || !searchRequest.RuleResolution.IsResolved)
        {
            throw new ArgumentException(
                "Plan compilation requires one resolved coherent context, search, and score result.",
                nameof(request));
        }
    }

    private static void ValidateSelection(
        TacticalLoadoutSearchRequest request,
        TacticalScoredLoadout selected)
    {
        var admitted = request.Discovery.Entries
            .Where(item => item.Consideration.Decision
                == TacticalCandidateDecision.Admitted)
            .Select(item => item.Consideration.Identity.StableKey)
            .ToHashSet(StringComparer.Ordinal);
        if (selected.Candidate.SelectedCandidates.IsEmpty
            || selected.Candidate.SelectedCandidates.Any(item =>
                !admitted.Contains(item.StableKey)))
        {
            throw new ArgumentException(
                "A compiled plan can select only admitted tactical candidates.",
                nameof(selected));
        }

        var proposal = selected.Candidate.Loadout.Proposal;
        var proposalCandidates = proposal.SkillCandidates.ToDictionary(
            item => item.SkillId);
        foreach (var identity in selected.Candidate.SelectedCandidates)
        {
            if (!proposalCandidates.TryGetValue(identity.SkillId, out var candidate)
                || candidate.RequiredDirection != identity.Direction)
            {
                throw new ArgumentException(
                    "The feasible proposal must retain every selected skill direction exactly.",
                    nameof(selected));
            }

            var validation = CombatSkillCandidateValidator.Validate(
                request.Player,
                candidate);
            if (!validation.IsAccepted)
            {
                throw new InvalidOperationException(
                    "A selected tactical candidate no longer passes the existing feasibility gates.");
            }
        }
    }

    private static TacticalEvidenceReference SnapshotEvidence(
        TacticalCombatRuleResolution resolution,
        TacticalExecutionContext context) => new(
        TacticalEvidenceSourceKind.SaveSnapshot,
        $"TACTICAL_CONTEXT_{context.SourceRevisionFingerprint[..24]}",
        resolution.GameDataVersion,
        VerifiedTacticalCombatRuleSets.RuleVersion,
        "SELECTED_LOADOUT");

    private static Projection ProjectRules(
        TacticalCombatScoringRequest request,
        TacticalEvidenceReference snapshotEvidence,
        CancellationToken cancellationToken)
    {
        var resolution = request.SearchRequest.RuleResolution;
        var projection = new Projection();
        projection.AddShared(snapshotEvidence);
        foreach (var match in resolution.Transitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            projection.AddShared(match.Rule.Evidence);
        }

        foreach (var match in resolution.Roles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            projection.AddShared(match.Rule.Evidence);
        }

        projection.AddShared(request.TriggerObservations.SelectMany(item =>
            item.Evidence));
        projection.AddShared(request.LayeringProofs.SelectMany(item =>
            item.Evidence));
        projection.AddShared(request.FinishProofs.SelectMany(item =>
            item.Inputs.SelectMany(input => input.Evidence)));

        var triggerObservations = request.TriggerObservations.ToDictionary(
            item => item.Transition.StableKey,
            StringComparer.Ordinal);
        var factRules = resolution.Transitions
            .SelectMany(match => match.Rule.TriggerFacts
                .Concat(match.Rule.ResultingFacts)
                .Select(fact => (Fact: fact, Match: match)))
            .GroupBy(item => item.Fact.StableKey, StringComparer.Ordinal);
        foreach (var group in factRules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var identity = group.First().Fact;
            var triggerMatches = group.Where(item =>
                item.Match.Rule.TriggerFacts.Any(fact =>
                    fact.StableKey == identity.StableKey)).ToArray();
            var observations = triggerMatches
                .Select(item => triggerObservations.GetValueOrDefault(
                    item.Match.Rule.Identity.StableKey))
                .Where(item => item is not null)
                .Cast<TacticalTriggerObservability>()
                .ToArray();
            var evidence = group.SelectMany(item => item.Match.Rule.Evidence)
                .Concat(observations.SelectMany(item => item.Evidence))
                .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
                .ToArray();
            projection.AddFact(Fact(identity, observations, evidence));
        }

        foreach (var match in resolution.Transitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var requirementIds = match.Rule.TriggerFacts
                .Select((fact, index) => new TacticalRequirementIdentity(
                    $"REQ_{match.Rule.Identity.Code}_{index + 1:00}"))
                .ToArray();
            projection.TransitionRequirements.Add(
                match.Rule.Identity.StableKey,
                requirementIds);
            foreach (var value in match.Rule.TriggerFacts.Zip(requirementIds))
            {
                projection.AddRequirement(new TacticalRequirementDefinition(
                    value.Second,
                    value.First,
                    TacticalRequirementOperator.Equal,
                    TacticalFactValue.Boolean(true)));
            }

            projection.AddTransition(new TacticalTransition(
                match.Rule.Identity,
                requirementIds,
                match.Rule.ResultingFacts,
                match.Rule.Timing,
                $"RULE_{TacticalCombatText.EnumKey(match.Rule.Purpose)}",
                match.Rule.LimitationIdentity,
                match.Rule.Evidence));
        }

        foreach (var match in resolution.Roles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var requirements = match.Rule.Transitions.SelectMany(identity =>
                projection.TransitionRequirements[identity.StableKey]);
            projection.AddRole(new TacticalSkillRole(
                match.Rule.Identity,
                checked((short)match.Rule.SkillId),
                match.Rule.Direction,
                checked((short)match.Rule.RawEffectId),
                match.Rule.Timing,
                match.Rule.Transitions,
                requirements,
                match.Rule.LimitationIdentity,
                match.Rule.Evidence));
        }

        return projection;
    }

    private static TacticalStateFact Fact(
        TacticalFactIdentity identity,
        IReadOnlyCollection<TacticalTriggerObservability> observations,
        IReadOnlyCollection<TacticalEvidenceReference> evidence)
    {
        if (observations.Any(item =>
                item.State == TacticalEvidenceState.Conflicting))
        {
            var source = evidence.First();
            return new TacticalStateFact(
                identity,
                TacticalEvidenceState.Conflicting,
                value: null,
                "TRIGGER_EVIDENCE_CONFLICTING",
                evidence,
                [
                    new TacticalConflictValue(
                        TacticalFactValue.Boolean(true),
                        source),
                    new TacticalConflictValue(
                        TacticalFactValue.Boolean(false),
                        source)
                ]);
        }

        if (observations.Any(item =>
                item.State == TacticalEvidenceState.Available))
        {
            return new TacticalStateFact(
                identity,
                TacticalEvidenceState.Available,
                TacticalFactValue.Boolean(true),
                "TRIGGER_OBSERVABLE_OR_CONFIRMED",
                evidence);
        }

        if (!observations.Any())
        {
            return new TacticalStateFact(
                identity,
                TacticalEvidenceState.Incomplete,
                value: null,
                "MANUAL_CONFIRMATION_REQUIRED",
                evidence);
        }

        var state = observations.All(item =>
            item.State == TacticalEvidenceState.Unsupported)
                ? TacticalEvidenceState.Unsupported
                : TacticalEvidenceState.Incomplete;
        return new TacticalStateFact(
            identity,
            state,
            value: null,
            observations.Select(item => item.ReasonIdentity)
                .Order(StringComparer.Ordinal).First(),
            evidence);
    }

    private static ImmutableArray<SelectedRole> SelectedRoles(
        TacticalScoredLoadout selected,
        TacticalCombatRuleResolution resolution,
        CancellationToken cancellationToken)
    {
        List<SelectedRole> values = [];
        foreach (var candidate in selected.Candidate.SelectedCandidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matches = resolution.Roles.Where(item =>
                item.Rule.SkillId == candidate.SkillId
                && item.Rule.Direction == candidate.Direction
                && item.Applicability == TacticalRuleApplicability.Applicable);
            values.AddRange(matches.Select(item => new SelectedRole(
                candidate,
                item.Rule)));
        }

        return
        [
            .. values.OrderBy(item => item.Rule.Timing)
                .ThenBy(item => item.Candidate.StableKey, StringComparer.Ordinal)
                .ThenBy(item => item.Rule.Identity.StableKey, StringComparer.Ordinal)
        ];
    }

    private static ImmutableArray<TacticalPreparationCheck> BuildPreparationChecks(
        TacticalLoadoutSearchRequest request,
        TacticalScoredLoadout selected,
        ImmutableArray<SelectedRole> selectedRoles)
    {
        var proposal = selected.Candidate.Loadout.Proposal;
        List<PreparationSeed> seeds = [];
        foreach (var category in Enum.GetValues<SkillCategory>())
        {
            var current = request.Player.EquippedSkills.Get(category)
                .ToHashSet();
            var proposed = proposal.Skills.Get(category).ToHashSet();
            seeds.AddRange(current.Except(proposed).Order().Select(skillId =>
                new PreparationSeed(
                    TacticalPreparationCheckKind.RemoveSkill,
                    $"REMOVE_SKILL_{skillId}",
                    category,
                    skillId,
                    null,
                    null)));
            seeds.AddRange(proposed.Except(current).Order().Select(skillId =>
                new PreparationSeed(
                    TacticalPreparationCheckKind.AddSkill,
                    $"ADD_SKILL_{skillId}",
                    category,
                    skillId,
                    null,
                    null)));
        }

        foreach (var candidate in proposal.SkillCandidates.OrderBy(item =>
            item.SkillId))
        {
            var validation = CombatSkillCandidateValidator.Validate(
                request.Player,
                candidate);
            if (validation.RequiredBreakthroughDirection.HasValue)
            {
                seeds.Add(new PreparationSeed(
                    TacticalPreparationCheckKind.CompleteBreakthrough,
                    $"COMPLETE_BREAKTHROUGH_SKILL_{candidate.SkillId}_{validation.RequiredBreakthroughDirection.Value.ToString().ToUpperInvariant()}",
                    validation.Skill!.Category,
                    candidate.SkillId,
                    validation.RequiredBreakthroughDirection,
                    null));
            }
            else if (validation.RequiredDirectionChange.HasValue)
            {
                seeds.Add(new PreparationSeed(
                    TacticalPreparationCheckKind.ChangeDirection,
                    $"CHANGE_DIRECTION_SKILL_{candidate.SkillId}_{validation.RequiredDirectionChange.Value.ToString().ToUpperInvariant()}",
                    validation.Skill!.Category,
                    candidate.SkillId,
                    validation.RequiredDirectionChange,
                    null));
            }
        }

        seeds.AddRange(
        [
            new PreparationSeed(
                TacticalPreparationCheckKind.Capacity,
                "CONFIRM_SELECTED_CAPACITY",
                null,
                null,
                null,
                null),
            new PreparationSeed(
                TacticalPreparationCheckKind.UniversalSlotAllocation,
                "CONFIRM_UNIVERSAL_SLOT_ALLOCATION",
                null,
                null,
                null,
                null),
            new PreparationSeed(
                TacticalPreparationCheckKind.LegendaryCostAssignment,
                "CONFIRM_LEGENDARY_COST_ASSIGNMENTS",
                null,
                null,
                null,
                null),
            new PreparationSeed(
                TacticalPreparationCheckKind.Equipment,
                "CONFIRM_SELECTED_EQUIPMENT_REQUIREMENTS",
                null,
                null,
                null,
                null),
            new PreparationSeed(
                TacticalPreparationCheckKind.Weapon,
                "CONFIRM_SELECTED_WEAPON_REQUIREMENTS",
                null,
                null,
                null,
                null),
            new PreparationSeed(
                TacticalPreparationCheckKind.ExecutionContext,
                "CONFIRM_SELECTED_EXECUTION_CONTEXT",
                null,
                null,
                null,
                null)
        ]);
        foreach (var selectedRole in selectedRoles.Where(item =>
            item.Rule.Timing == TacticalTransitionTiming.BeforeCombat))
        {
            var category = request.Player.LearnedSkills.Single(item =>
                item.SkillId == selectedRole.Candidate.SkillId).Category;
            seeds.Add(new PreparationSeed(
                TacticalPreparationCheckKind.BeforeCombatRole,
                $"CONSIDER_SKILL_{selectedRole.Candidate.SkillId}_{selectedRole.Candidate.Direction.ToString().ToUpperInvariant()}_BEFORE_COMBAT",
                category,
                selectedRole.Candidate.SkillId,
                selectedRole.Candidate.Direction,
                selectedRole.Rule.Identity.StableKey));
        }

        var ordered = seeds.OrderBy(item => item.Kind)
            .ThenBy(item => item.Category)
            .ThenBy(item => item.SkillId)
            .ThenBy(item => item.Action, StringComparer.Ordinal)
            .ToArray();
        return
        [
            .. ordered.Select((item, index) => new TacticalPreparationCheck(
                $"PREP_{index + 1:000}_{item.Kind.ToString().ToUpperInvariant()}",
                item.Kind,
                item.Action,
                item.Category,
                item.SkillId,
                item.Direction))
        ];
    }

    private static void AddPreparationContracts(
        IEnumerable<TacticalPreparationCheck> checks,
        Projection projection,
        TacticalEvidenceReference evidence)
    {
        foreach (var check in checks.Where(item =>
            item.Kind != TacticalPreparationCheckKind.BeforeCombatRole))
        {
            var fact = new TacticalFactIdentity(
                TacticalFactKind.PlayerReadiness,
                check.Identity);
            var requirement = new TacticalRequirementIdentity(
                $"REQ_{check.Identity}");
            var transition = new TacticalTransitionIdentity(
                $"VERIFY_{check.Identity}");
            projection.AddFact(new TacticalStateFact(
                fact,
                TacticalEvidenceState.Available,
                TacticalFactValue.Boolean(true),
                "EXISTING_FEASIBILITY_VALIDATOR_ACCEPTED",
                [evidence]));
            projection.AddRequirement(new TacticalRequirementDefinition(
                requirement,
                fact,
                TacticalRequirementOperator.Equal,
                TacticalFactValue.Boolean(true)));
            projection.AddTransition(new TacticalTransition(
                transition,
                [requirement],
                [fact],
                TacticalTransitionTiming.BeforeCombat,
                "CONFIRM_SELECTED_LOADOUT_MANUALLY",
                "INFORMATION_ONLY_NO_GAME_ACTION",
                [evidence]));
            projection.PreparationContracts.Add(
                check.StableKey,
                new PreparationContract(fact, requirement, transition));
        }
    }

    private static void AddCandidateRequirementContracts(
        IEnumerable<TacticalCandidateConsideration> candidates,
        Projection projection,
        TacticalEvidenceReference snapshotEvidence)
    {
        foreach (var group in candidates.SelectMany(item => item.Requirements)
            .GroupBy(item => item.Requirement.StableKey, StringComparer.Ordinal))
        {
            if (projection.Requirements.ContainsKey(group.Key))
            {
                continue;
            }

            var identity = group.First().Requirement;
            var fact = new TacticalFactIdentity(
                TacticalFactKind.Other,
                identity.Code);
            var evidence = group.SelectMany(item => item.Evidence)
                .Append(snapshotEvidence)
                .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
                .ToArray();
            projection.AddFact(new TacticalStateFact(
                fact,
                TacticalEvidenceState.Incomplete,
                value: null,
                "CANDIDATE_SPECIFIC_REQUIREMENT",
                evidence));
            projection.AddRequirement(new TacticalRequirementDefinition(
                identity,
                fact,
                TacticalRequirementOperator.Present,
                expectedValue: null));
        }
    }

    private static DraftSet BuildStepDrafts(
        TacticalCombatScoringRequest request,
        TacticalScoredLoadout selected,
        ImmutableArray<SelectedRole> selectedRoles,
        ImmutableArray<TacticalPreparationCheck> preparationChecks,
        Projection projection,
        TacticalEvidenceReference snapshotEvidence,
        CancellationToken cancellationToken)
    {
        DraftSet result = new();
        foreach (var check in preparationChecks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (check.Kind == TacticalPreparationCheckKind.BeforeCombatRole)
            {
                var role = selectedRoles.Single(item =>
                    item.Candidate.SkillId == check.SkillId
                    && item.Candidate.Direction == check.Direction
                    && item.Rule.Timing == TacticalTransitionTiming.BeforeCombat);
                result.Preparation.Add(RoleDraft(
                    role,
                    TacticalPlanStage.Preparation,
                    result.Preparation.Count + 1,
                    request,
                    projection,
                    check.ManualActionIdentity,
                    $"STEP_{check.Identity}"));
                continue;
            }

            var contract = projection.PreparationContracts[check.StableKey];
            result.Preparation.Add(new StepDraft(
                new TacticalPlanStepIdentity($"STEP_{check.Identity}"),
                TacticalPlanStage.Preparation,
                result.Preparation.Count + 1,
                TacticalStepBranchKind.Primary,
                [contract.Fact],
                [new TacticalRequirementEvaluation(
                    contract.Requirement,
                    TacticalRequirementOutcome.Satisfied,
                    "EXISTING_FEASIBILITY_VALIDATOR_ACCEPTED",
                    [snapshotEvidence])],
                [contract.Transition],
                check.SkillId,
                check.ManualActionIdentity,
                "CONFIRM_SELECTED_LOADOUT_MANUALLY",
                "INFORMATION_ONLY_NO_GAME_ACTION",
                [snapshotEvidence]));
        }

        foreach (var role in selectedRoles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (role.Rule.Timing == TacticalTransitionTiming.BeforeCombat)
            {
                continue;
            }

            if (role.Rule.Identity.Kind == TacticalRoleKind.Finish)
            {
                continue;
            }

            if (role.Rule.Identity.Kind == TacticalRoleKind.Recovery
                || role.Rule.Purpose
                    == TacticalRulePurpose.DirectPracticeLockRecovery)
            {
                continue;
            }

            var stage = role.Rule.Timing is TacticalTransitionTiming.CombatStart
                    or TacticalTransitionTiming.BeforeFirstUse
                ? TacticalPlanStage.Opening
                : TacticalPlanStage.TargetStateResponse;
            var collection = stage == TacticalPlanStage.Opening
                ? result.Opening
                : result.Response;
            collection.Add(RoleDraft(
                role,
                stage,
                collection.Count + 1,
                request,
                projection));
        }

        BuildRecoveryDrafts(
            request,
            selected,
            selectedRoles,
            projection,
            result,
            cancellationToken);
        BuildFinishDraft(
            request,
            selected,
            selectedRoles,
            projection,
            result);
        BuildFallbackDraft(
            request,
            selectedRoles,
            projection,
            result);
        return result;
    }

    private static StepDraft RoleDraft(
        SelectedRole role,
        TacticalPlanStage stage,
        int order,
        TacticalCombatScoringRequest request,
        Projection projection,
        string? manualActionIdentity = null,
        string? stepIdentity = null)
    {
        var transitionIds = role.Rule.Transitions;
        var candidate = request.SearchResult.CandidateDecisions.Single(item =>
            item.Identity == role.Candidate);
        var transitionFacts = transitionIds.SelectMany(identity =>
                projection.Transitions[identity.StableKey].Preconditions)
            .Select(identity =>
                projection.Requirements[identity.StableKey].Fact)
            .ToArray();
        var candidateFacts = candidate.Requirements.Select(item =>
            projection.Requirements[item.Requirement.StableKey].Fact);
        var facts = transitionFacts.Concat(candidateFacts)
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        var transitionEvaluations = transitionIds.SelectMany(identity =>
                projection.Transitions[identity.StableKey].Preconditions)
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .Select(identity => EvaluateRequirement(
                identity,
                projection,
                request.TriggerObservations))
            .ToArray();
        var evaluations = transitionEvaluations
            .Concat(candidate.Requirements)
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        var evidence = role.Rule.Evidence
            .Concat(transitionIds.SelectMany(identity =>
                projection.Transitions[identity.StableKey].Evidence))
            .Concat(candidate.Evidence)
            .Concat(candidate.Requirements.SelectMany(item => item.Evidence))
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        var code = stepIdentity
            ?? $"STEP_{stage.ToString().ToUpperInvariant()}_{role.Candidate.SkillId}_{role.Candidate.Direction.ToString().ToUpperInvariant()}";
        return new StepDraft(
            new TacticalPlanStepIdentity(code),
            stage,
            order,
            stage == TacticalPlanStage.Preparation
                ? TacticalStepBranchKind.Primary
                : TacticalStepBranchKind.Conditional,
            facts,
            evaluations,
            transitionIds,
            role.Candidate.SkillId,
            manualActionIdentity
                ?? $"CONSIDER_SKILL_{role.Candidate.SkillId}_{role.Candidate.Direction.ToString().ToUpperInvariant()}",
            $"APPLY_{TacticalCombatText.EnumKey(role.Rule.Purpose)}",
            role.Rule.LimitationIdentity,
            evidence);
    }

    private static TacticalRequirementEvaluation EvaluateRequirement(
        TacticalRequirementIdentity identity,
        Projection projection,
        IEnumerable<TacticalTriggerObservability> observations)
    {
        var definition = projection.Requirements[identity.StableKey];
        var fact = projection.Facts[definition.Fact.StableKey];
        var relevant = observations.Where(item =>
                projection.TransitionRequirements[item.Transition.StableKey]
                    .Any(value => value.StableKey == identity.StableKey))
            .ToArray();
        var evidence = relevant.SelectMany(item => item.Evidence)
            .Concat(fact.Evidence)
            .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
            .ToArray();
        var outcome = fact.State switch
        {
            TacticalEvidenceState.Available =>
                TacticalRequirementOutcome.Satisfied,
            TacticalEvidenceState.Unsupported =>
                TacticalRequirementOutcome.Unsupported,
            TacticalEvidenceState.Conflicting =>
                TacticalRequirementOutcome.Conflicting,
            _ => TacticalRequirementOutcome.Unknown
        };
        return new TacticalRequirementEvaluation(
            identity,
            outcome,
            relevant.Select(item => item.ReasonIdentity)
                .DefaultIfEmpty(fact.ReasonIdentity)
                .Order(StringComparer.Ordinal).First(),
            evidence);
    }

    private static void BuildRecoveryDrafts(
        TacticalCombatScoringRequest request,
        TacticalScoredLoadout selected,
        ImmutableArray<SelectedRole> selectedRoles,
        Projection projection,
        DraftSet result,
        CancellationToken cancellationToken)
    {
        var selfLock = selectedRoles.Any(item => item.Rule.Transitions.Any(
            transition => request.SearchRequest.RuleResolution.Transitions
                .Single(match => match.Rule.Identity == transition).Rule.Purpose
                == TacticalRulePurpose.DirectPracticeSelfLock));
        if (!selfLock)
        {
            result.RecoveryState = TacticalPlanStageState.Omitted;
            result.RecoveryLimitation = "NO_VERIFIED_RECOVERY_COST";
            return;
        }

        var recovery = request.SearchRequest.RuleResolution.Transitions
            .Where(item =>
                item.Applicability == TacticalRuleApplicability.Applicable
                && item.Rule.Purpose
                    == TacticalRulePurpose.DirectPracticeLockRecovery)
            .Select(item => item.Rule)
            .SingleOrDefault();
        var activeReverse = selectedRoles.Where(item =>
                item.Candidate.Direction == PracticeDirection.Reverse
                && item.Rule.Timing is TacticalTransitionTiming.DuringCast
                    or TacticalTransitionTiming.AfterCast
                    or TacticalTransitionTiming.AfterManualAction)
            .DistinctBy(item => item.Candidate.StableKey, StringComparer.Ordinal)
            .Take(3)
            .ToArray();
        if (recovery is null || activeReverse.Length < 3)
        {
            result.RecoveryState = TacticalPlanStageState.Unsupported;
            result.RecoveryLimitation =
                "THREE_EXACT_EXECUTABLE_CASTS_NOT_PRESELECTED";
            return;
        }

        result.RecoveryState = TacticalPlanStageState.Supported;
        result.RecoveryLimitation = recovery.LimitationIdentity;
        var transition = projection.Transitions[recovery.Identity.StableKey];
        foreach (var role in activeReverse)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evaluations = transition.Preconditions.Select(identity =>
                EvaluateRequirement(
                    identity,
                    projection,
                    request.TriggerObservations));
            result.Recovery.Add(new StepDraft(
                new TacticalPlanStepIdentity(
                    $"STEP_RECOVERY_LAYER_{result.Recovery.Count + 1:00}"),
                TacticalPlanStage.Recovery,
                result.Recovery.Count + 1,
                TacticalStepBranchKind.Conditional,
                transition.Preconditions.Select(identity =>
                    projection.Requirements[identity.StableKey].Fact),
                evaluations,
                [transition.Identity],
                role.Candidate.SkillId,
                $"CONSIDER_REVERSE_CAST_SKILL_{role.Candidate.SkillId}",
                transition.ExpectedPurposeIdentity,
                transition.LimitationIdentity,
                transition.Evidence));
        }
    }

    private static void BuildFinishDraft(
        TacticalCombatScoringRequest request,
        TacticalScoredLoadout selected,
        ImmutableArray<SelectedRole> selectedRoles,
        Projection projection,
        DraftSet result)
    {
        var finishComponent = selected.Get(TacticalScoreComponentKind.FinishPath);
        var selectedKeys = selected.Candidate.SelectedCandidates
            .Select(item => item.StableKey).ToHashSet(StringComparer.Ordinal);
        var proof = request.FinishProofs.FirstOrDefault(item =>
            selectedKeys.Contains(item.ChannelCandidate.StableKey)
            && selectedKeys.Contains(item.FinishCandidate.StableKey));
        var role = proof is null
            ? null
            : selectedRoles.SingleOrDefault(item =>
                item.Rule.Identity == proof.FinishRole);
        if (!finishComponent.IsAvailable || proof is null || role is null)
        {
            result.FinishState = TacticalPlanStageState.Unsupported;
            result.FinishLimitation = finishComponent.Limitations
                .DefaultIfEmpty("FINISH_EVIDENCE_UNAVAILABLE")
                .Order(StringComparer.Ordinal).First();
            return;
        }

        result.FinishState = TacticalPlanStageState.Supported;
        result.FinishLimitation = proof.LimitationIdentity;
        result.Finish.Add(RoleDraft(
            role,
            TacticalPlanStage.Finish,
            1,
            request,
            projection,
            $"CONSIDER_FINISH_SKILL_{proof.FinishCandidate.SkillId}"));
    }

    private static void BuildFallbackDraft(
        TacticalCombatScoringRequest request,
        ImmutableArray<SelectedRole> selectedRoles,
        Projection projection,
        DraftSet result)
    {
        var role = selectedRoles.Where(item =>
                item.Rule.Identity.Kind is TacticalRoleKind.Mitigation
                    or TacticalRoleKind.Recovery
                    or TacticalRoleKind.Fallback)
            .OrderBy(item => item.Rule.Identity.Kind)
            .ThenBy(item => item.Candidate.StableKey, StringComparer.Ordinal)
            .FirstOrDefault();
        if (role is null)
        {
            result.FallbackState = TacticalPlanStageState.Unsupported;
            result.FallbackLimitation = "NO_SEPARATELY_VERIFIED_FEASIBLE_FALLBACK";
            return;
        }

        result.FallbackState = TacticalPlanStageState.Supported;
        result.FallbackLimitation = role.Rule.LimitationIdentity;
        var draft = RoleDraft(
            role,
            TacticalPlanStage.Fallback,
            1,
            request,
            projection,
            $"FALLBACK_CONSIDER_SKILL_{role.Candidate.SkillId}_{role.Candidate.Direction.ToString().ToUpperInvariant()}",
            "STEP_VERIFIED_FEASIBLE_FALLBACK");
        result.Fallback.Add(draft with
        {
            BranchKind = TacticalStepBranchKind.Fallback
        });
    }

    private static TacticalPlanStageDefinition[] MaterializeStages(
        DraftSet drafts,
        IEnumerable<TacticalEvidenceReference> sharedEvidence)
    {
        var fallbackTarget = drafts.Fallback.FirstOrDefault()?.Identity;
        var orderedPrimary = drafts.Preparation
            .Concat(drafts.Opening)
            .Concat(drafts.Response)
            .Concat(drafts.Recovery)
            .Concat(drafts.Finish)
            .ToArray();
        Dictionary<string, TacticalPlanStep> materialized =
            new(StringComparer.Ordinal);
        for (var index = orderedPrimary.Length - 1; index >= 0; index--)
        {
            var draft = orderedPrimary[index];
            var next = draft.Stage == TacticalPlanStage.Finish
                ? null
                : index + 1 < orderedPrimary.Length
                    ? orderedPrimary[index + 1].Identity
                    : fallbackTarget;
            materialized.Add(
                draft.Identity.Code,
                Materialize(draft, next, fallbackTarget));
        }

        foreach (var draft in drafts.Fallback)
        {
            materialized.Add(
                draft.Identity.Code,
                new TacticalPlanStep(
                    draft.Identity,
                    draft.Stage,
                    draft.Order,
                    TacticalStepBranchKind.Fallback,
                    draft.Facts,
                    draft.Requirements,
                    draft.Transitions,
                    draft.SkillId,
                    draft.ManualActionIdentity,
                    draft.ExpectedPurposeIdentity,
                    draft.LimitationIdentity,
                    [
                        new TacticalPlanBranch(
                            "FALLBACK_ACTION_COMPLETE",
                            TacticalBranchOutcome.Stop),
                        new TacticalPlanBranch(
                            "FALLBACK_CONDITION_UNMET",
                            TacticalBranchOutcome.Unresolved)
                    ],
                    draft.Evidence));
        }

        var evidence = sharedEvidence.First();
        return
        [
            Stage(
                TacticalPlanStage.Preparation,
                TacticalPlanStageState.Supported,
                "MANUAL_PREPARATION_REQUIRED",
                drafts.Preparation,
                materialized,
                evidence),
            Stage(
                TacticalPlanStage.Opening,
                drafts.Opening.Count == 0
                    ? TacticalPlanStageState.Omitted
                    : TacticalPlanStageState.Supported,
                drafts.Opening.Count == 0
                    ? "NO_SELECTED_VERIFIED_OPENING_ACTION"
                    : "SELECTED_VERIFIED_OPENING_ACTIONS",
                drafts.Opening,
                materialized,
                evidence),
            Stage(
                TacticalPlanStage.TargetStateResponse,
                drafts.Response.Count == 0
                    ? TacticalPlanStageState.Omitted
                    : TacticalPlanStageState.Supported,
                drafts.Response.Count == 0
                    ? "NO_SELECTED_TARGET_STATE_RESPONSE"
                    : "CONDITIONAL_TARGET_STATE_RESPONSE",
                drafts.Response,
                materialized,
                evidence),
            Stage(
                TacticalPlanStage.Recovery,
                drafts.RecoveryState,
                drafts.RecoveryLimitation,
                drafts.Recovery,
                materialized,
                evidence),
            Stage(
                TacticalPlanStage.Finish,
                drafts.FinishState,
                drafts.FinishLimitation,
                drafts.Finish,
                materialized,
                evidence),
            Stage(
                TacticalPlanStage.Fallback,
                drafts.FallbackState,
                drafts.FallbackLimitation,
                drafts.Fallback,
                materialized,
                evidence)
        ];
    }

    private static TacticalPlanStep Materialize(
        StepDraft draft,
        TacticalPlanStepIdentity? next,
        TacticalPlanStepIdentity? fallback)
    {
        List<TacticalPlanBranch> branches = [];
        if (next is null)
        {
            branches.Add(new TacticalPlanBranch(
                $"{draft.Identity.Code}_COMPLETE",
                TacticalBranchOutcome.Stop));
        }
        else
        {
            branches.Add(new TacticalPlanBranch(
                $"{draft.Identity.Code}_CONDITION_SATISFIED",
                next == fallback
                    ? TacticalBranchOutcome.Fallback
                    : TacticalBranchOutcome.Continue,
                next));
        }

        if (fallback is not null && fallback != next)
        {
            branches.Add(new TacticalPlanBranch(
                $"{draft.Identity.Code}_CONDITION_FAILED_OR_UNKNOWN",
                TacticalBranchOutcome.Fallback,
                fallback));
        }
        else
        {
            branches.Add(new TacticalPlanBranch(
                $"{draft.Identity.Code}_CONDITION_FAILED_OR_UNKNOWN",
                TacticalBranchOutcome.Unresolved));
        }

        return new TacticalPlanStep(
            draft.Identity,
            draft.Stage,
            draft.Order,
            draft.BranchKind,
            draft.Facts,
            draft.Requirements,
            draft.Transitions,
            draft.SkillId,
            draft.ManualActionIdentity,
            draft.ExpectedPurposeIdentity,
            draft.LimitationIdentity,
            branches,
            draft.Evidence);
    }

    private static TacticalPlanStageDefinition Stage(
        TacticalPlanStage stage,
        TacticalPlanStageState state,
        string limitation,
        IEnumerable<StepDraft> drafts,
        IReadOnlyDictionary<string, TacticalPlanStep> materialized,
        TacticalEvidenceReference evidence) => new(
        stage,
        state,
        limitation,
        drafts.Select(item => materialized[item.Identity.Code]),
        [evidence]);

    private sealed class Projection
    {
        private readonly Dictionary<string, TacticalEvidenceReference>
            _sharedEvidence = new(StringComparer.Ordinal);

        internal Dictionary<string, TacticalStateFact> Facts { get; } =
            new(StringComparer.Ordinal);

        internal Dictionary<string, TacticalRequirementDefinition>
            Requirements
        { get; } = new(StringComparer.Ordinal);

        internal Dictionary<string, TacticalTransition> Transitions { get; } =
            new(StringComparer.Ordinal);

        internal Dictionary<string, TacticalSkillRole> Roles { get; } =
            new(StringComparer.Ordinal);

        internal Dictionary<string, TacticalRequirementIdentity[]>
            TransitionRequirements
        { get; } = new(StringComparer.Ordinal);

        internal Dictionary<string, PreparationContract> PreparationContracts
        { get; } = new(StringComparer.Ordinal);

        internal IEnumerable<TacticalEvidenceReference> SharedEvidence =>
            _sharedEvidence.Values.OrderBy(item => item.StableKey,
                StringComparer.Ordinal);

        internal void AddShared(TacticalEvidenceReference evidence) =>
            _sharedEvidence.TryAdd(evidence.StableKey, evidence);

        internal void AddShared(IEnumerable<TacticalEvidenceReference> evidence)
        {
            foreach (var item in evidence)
            {
                AddShared(item);
            }
        }

        internal void AddFact(TacticalStateFact value) =>
            Facts.Add(value.StableKey, value);

        internal void AddRequirement(TacticalRequirementDefinition value) =>
            Requirements.Add(value.StableKey, value);

        internal void AddTransition(TacticalTransition value) =>
            Transitions.Add(value.StableKey, value);

        internal void AddRole(TacticalSkillRole value) =>
            Roles.Add(value.StableKey, value);
    }

    private sealed class DraftSet
    {
        internal List<StepDraft> Preparation { get; } = [];

        internal List<StepDraft> Opening { get; } = [];

        internal List<StepDraft> Response { get; } = [];

        internal List<StepDraft> Recovery { get; } = [];

        internal List<StepDraft> Finish { get; } = [];

        internal List<StepDraft> Fallback { get; } = [];

        internal TacticalPlanStageState RecoveryState { get; set; } =
            TacticalPlanStageState.Omitted;

        internal string RecoveryLimitation { get; set; } =
            "NO_VERIFIED_RECOVERY_COST";

        internal TacticalPlanStageState FinishState { get; set; } =
            TacticalPlanStageState.Unsupported;

        internal string FinishLimitation { get; set; } =
            "FINISH_EVIDENCE_UNAVAILABLE";

        internal TacticalPlanStageState FallbackState { get; set; } =
            TacticalPlanStageState.Unsupported;

        internal string FallbackLimitation { get; set; } =
            "NO_SEPARATELY_VERIFIED_FEASIBLE_FALLBACK";
    }

    private sealed record SelectedRole(
        TacticalCandidateIdentity Candidate,
        TacticalSkillRoleRule Rule);

    private sealed record PreparationSeed(
        TacticalPreparationCheckKind Kind,
        string Action,
        SkillCategory? Category,
        int? SkillId,
        PracticeDirection? Direction,
        string? RoleIdentity);

    private sealed record PreparationContract(
        TacticalFactIdentity Fact,
        TacticalRequirementIdentity Requirement,
        TacticalTransitionIdentity Transition);

    private sealed record StepDraft(
        TacticalPlanStepIdentity Identity,
        TacticalPlanStage Stage,
        int Order,
        TacticalStepBranchKind BranchKind,
        IEnumerable<TacticalFactIdentity> Facts,
        IEnumerable<TacticalRequirementEvaluation> Requirements,
        IEnumerable<TacticalTransitionIdentity> Transitions,
        int? SkillId,
        string ManualActionIdentity,
        string ExpectedPurposeIdentity,
        string LimitationIdentity,
        IEnumerable<TacticalEvidenceReference> Evidence);
}
