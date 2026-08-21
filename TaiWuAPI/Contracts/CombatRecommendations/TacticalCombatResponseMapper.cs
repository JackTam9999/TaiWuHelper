using System.Collections.Immutable;
using TaiWu.Application.TacticalCombat;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.TacticalCombat;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public static class TacticalCombatResponseMapper
{
    public static TacticalCombatResponse Map(
        TacticalCombatRecommendationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var compiled = result.CompiledPlan;
        return new TacticalCombatResponse(
            result.Status,
            result.ReasonIdentity,
            result.HasTacticalPlan,
            Map(result.Identity),
            MapSnapshot(result.Context),
            Map(result.RuleResolution),
            MapContext(result.Context),
            Map(result.Discovery),
            Map(result.Search),
            Map(result.Scoring),
            MapSelected(compiled),
            MapPlan(compiled),
            new TacticalDiagnosticsResponse(
                Map(result.WorkCounts),
                result.Search is null
                    ? null
                    : checked((long)result.Search.Coverage.Elapsed.TotalMilliseconds),
                result.Context?.CapturedAtUtc,
                result.Context?.LatestObservationAtUtc));
    }

    private static TacticalRecommendationIdentityResponse? Map(
        TacticalCombatRecommendationIdentity? value) => value is null
        ? null
        : new(
            value.SnapshotFingerprint,
            value.ObservationFingerprint,
            value.TargetChainFingerprint,
            value.RuleFingerprint,
            value.CandidateFingerprint,
            value.BoundFingerprint,
            value.PolicyFingerprint,
            value.SelectedLoadoutFingerprint,
            value.PlanFingerprint,
            value.SemanticFingerprint);

    private static TacticalSnapshotSummaryResponse? MapSnapshot(
        TacticalExecutionContextReadResult? value) => value is null
        ? null
        : new(
            value.CapturedAtUtc,
            value.LatestObservationAtUtc,
            value.Context.SourceRevisionFingerprint,
            value.Context.ObservationRevisionFingerprint,
            value.Context.GameDataVersion.IsAvailable
                ? value.Context.GameDataVersion.Value
                : null);

    private static TacticalTargetChainResponse? Map(
        TacticalCombatRuleResolution? value) => value is null
        ? null
        : new(
            value.GameDataVersion,
            value.RuleSetFingerprint,
            value.Status,
            [.. value.Transitions.Select(item =>
                new TacticalTransitionRuleResponse(
                    item.Rule.Identity.Code,
                    item.Rule.Purpose,
                    item.Rule.Timing,
                    item.Applicability,
                    [.. item.Rule.TriggerFacts.Select(Fact)],
                    [.. item.Rule.ResultingFacts.Select(Fact)],
                    item.Rule.TargetGoalCodes,
                    [.. item.UnmetEvidence.Select(identity => identity.Code)],
                    item.Rule.LimitationIdentity,
                    MapEvidence(item.Rule.Evidence)))],
            [.. value.Roles.Select(item => new TacticalRoleRuleResponse(
                Role(item.Rule.Identity),
                item.Rule.Purpose,
                item.Rule.Timing,
                item.Applicability,
                item.Rule.SkillId,
                item.Rule.Direction,
                item.Rule.RawEffectId,
                item.Rule.RequiredMechanics,
                item.Rule.TargetGoalCodes,
                [.. item.Rule.Transitions.Select(identity => identity.Code)],
                [.. item.UnmetEvidence.Select(identity => identity.Code)],
                item.Rule.LimitationIdentity,
                MapEvidence(item.Rule.Evidence))) ]);

    private static TacticalExecutionContextResponse? MapContext(
        TacticalExecutionContextReadResult? value)
    {
        if (value is null)
        {
            return null;
        }

        var context = value.Context;
        return new TacticalExecutionContextResponse(
            context.SemanticFingerprint,
            context.RuleResolutionStatus,
            [.. context.ResolvedRules.Select(item =>
                new TacticalResolvedRuleResponse(
                    item.Kind,
                    item.RuleIdentity,
                    item.Applicability,
                    [.. item.UnmetEvidence.Select(identity =>
                        identity.Code)]))],
            MapFacts(context.Current),
            MapFacts(context.Proposed));
    }

    private static IReadOnlyList<TacticalContextFactResponse> MapFacts(
        CurrentTacticalExecutionFacts facts) =>
    [
        Map("EQUIPPED_WEAPON_TYPE_IDS", facts.EquippedWeaponTypeIds),
        Map("UNLOCKED_WEAPON_TYPE_IDS", facts.UnlockedWeaponTypeIds),
        Map("USABLE_COMBAT_STYLE_IDS", facts.UsableCombatStyleIds),
        Map("DISTANCE", facts.Distance),
        Map("STANCE", facts.Stance),
        Map("BREATH", facts.Breath),
        Map("RESOURCES", facts.Resources),
        Map("ACTIVE_DEFENSE_SKILL_ID", facts.ActiveDefenseSkillId),
        Map("ACTIVE_AGILITY_SKILL_ID", facts.ActiveAgilitySkillId),
        Map("INNER_POWER", facts.InnerPower),
        Map("SLOT_BUDGETS", facts.SlotBudgets),
        Map("UNIVERSAL_SLOT_ALLOCATION", facts.UniversalSlotAllocation),
        Map("LEGENDARY_COST_SLOTS", facts.LegendaryCostSlots),
        Map("LEGENDARY_COST_ASSIGNMENTS", facts.LegendaryCostAssignments),
        Map("EQUIPPED_SKILL_IDS", facts.EquippedSkillIds)
    ];

    private static IReadOnlyList<TacticalContextFactResponse> MapFacts(
        ProposedTacticalExecutionFacts facts) =>
    [
        Map("EQUIPPED_WEAPON_TYPE_IDS", facts.EquippedWeaponTypeIds),
        Map("UNLOCKED_WEAPON_TYPE_IDS", facts.UnlockedWeaponTypeIds),
        Map("USABLE_COMBAT_STYLE_IDS", facts.UsableCombatStyleIds),
        Map("DISTANCE", facts.Distance),
        Map("STANCE", facts.Stance),
        Map("BREATH", facts.Breath),
        Map("RESOURCES", facts.Resources),
        Map("ACTIVE_DEFENSE_SKILL_ID", facts.ActiveDefenseSkillId),
        Map("ACTIVE_AGILITY_SKILL_ID", facts.ActiveAgilitySkillId),
        Map("INNER_POWER", facts.InnerPower),
        Map("SLOT_BUDGETS", facts.SlotBudgets),
        Map("UNIVERSAL_SLOT_ALLOCATION", facts.UniversalSlotAllocation),
        Map("LEGENDARY_COST_SLOTS", facts.LegendaryCostSlots),
        Map("LEGENDARY_COST_ASSIGNMENTS", facts.LegendaryCostAssignments),
        Map("EQUIPPED_SKILL_IDS", facts.EquippedSkillIds)
    ];

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<int> fact) => ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse("INTEGER", fact.Value)
                : null);

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<ImmutableArray<int>> fact) => ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse(
                    "INTEGER_LIST",
                    Integers: fact.Value)
                : null);

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<ImmutableArray<CombatResourceAmount>> fact) =>
        ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse(
                    "RESOURCES",
                    Resources: [.. fact.Value.Select(item =>
                        new TacticalResourceResponse(
                            item.Resource,
                            item.Amount.IsAvailable,
                            item.Amount.IsAvailable ? item.Amount.Value : null,
                            item.Amount.UnavailableReason))])
                : null);

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<TacticalInnerPowerContext> fact) => ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse(
                    "INNER_POWER",
                    InnerPower: new TacticalInnerPowerResponse(
                        fact.Value.StateId,
                        fact.Value.BacklashOnUseElement))
                : null);

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<SlotBudgetSet> fact) => ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse(
                    "SLOT_BUDGETS",
                    SlotBudgets: [.. fact.Value.Values.Select(Map)])
                : null);

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<GenericSlotAllocation> fact) => ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse(
                    "UNIVERSAL_SLOTS",
                    UniversalSlots: Map(fact.Value))
                : null);

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<ImmutableArray<LegendaryBookCostSlot>> fact) =>
        ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse(
                    "LEGENDARY_COST_SLOTS",
                    SlotReferences: [.. fact.Value.Select(item =>
                        item.SlotReference)])
                : null);

    private static TacticalContextFactResponse Map(
        string identity,
        TacticalContextFact<ImmutableArray<LegendaryBookCostAssignment>> fact)
        => ContextFact(
            identity,
            fact,
            fact.IsAvailable
                ? new TacticalContextValueResponse(
                    "LEGENDARY_COST_ASSIGNMENTS",
                    Assignments: [.. fact.Value.Select(item =>
                        new TacticalLegendaryAssignmentResponse(
                            item.Slot.SlotReference,
                            item.SkillId,
                            item.Category,
                            item.Origin,
                            item.AssignmentEvidenceReference))])
                : null);

    private static TacticalContextFactResponse ContextFact<T>(
        string identity,
        TacticalContextFact<T> fact,
        TacticalContextValueResponse? value) => new(
            identity,
            fact.State,
            fact.Origin,
            fact.Availability,
            fact.ReasonIdentity,
            fact.EvidenceIdentities,
            value);

    private static TacticalCandidateDiscoveryResponse? Map(
        TacticalCandidateDiscoveryResult? value) => value is null
        ? null
        : new(
            value.SemanticFingerprint,
            value.LearnedSkillCount,
            value.SupportedRoleCount,
            value.ConsideredVerifiedRoleCount,
            value.AdmittedVerifiedRoleCount,
            value.UnsupportedCount,
            [.. value.Entries.Select(MapCandidate)],
            [.. value.AdmissionCounts.Select(item =>
                new TacticalCandidateCountResponse(item.State, item.Count))],
            [.. value.RejectionSummaries.Select(item =>
                new TacticalRejectionSummaryResponse(
                    item.ReasonIdentity,
                    item.Count,
                    item.ExampleConsiderationKeys))]);

    private static TacticalCandidateResponse MapCandidate(
        TacticalCandidateDiscoveryEntry item) => new(
            Candidate(item.Consideration.Identity),
            item.SkillId,
            item.Category,
            item.Direction,
            item.RequiresBreakthrough,
            item.IsCurrentlyEquipped,
            item.SupportState,
            item.AdmissionState,
            item.Consideration.Decision,
            Map(item.ObservedRawEffectId),
            Map(item.EffectiveCost),
            item.Role is null
                ? null
                : new TacticalCandidateRoleResponse(
                    Role(item.Role.Identity),
                    item.Role.Purpose,
                    item.Role.Timing,
                    item.Role.RawEffectId,
                    [.. item.Role.TransitionIdentities.Select(identity =>
                        identity.Code)],
                    item.Role.LimitationIdentity,
                    item.Role.EvidenceIdentities),
            [.. item.Gates.Select(gate => new TacticalCandidateGateResponse(
                gate.Kind,
                gate.State,
                gate.ReasonIdentity,
                gate.EvidenceIdentities))]);

    private static TacticalIntegerFactResponse Map(
        TacticalContextFact<int> value) => new(
            value.State,
            value.IsAvailable ? value.Value : null,
            value.ReasonIdentity,
            value.EvidenceIdentities);

    private static TacticalSearchResponse? Map(
        TacticalLoadoutSearchResult? value) => value is null
        ? null
        : new(
            value.SemanticFingerprint,
            value.IsComplete,
            value.IsOptimal,
            Map(value.Coverage),
            [.. value.CandidateDecisions.Select(item =>
                new TacticalSearchCandidateResponse(
                    Candidate(item.Identity),
                    item.Decision,
                    [.. item.Roles.Select(Role)],
                    [.. item.Requirements.Select(Map)],
                    item.ReasonIdentity,
                    MapEvidence(item.Evidence),
                    item.DominatedBy is null
                        ? null
                        : Candidate(item.DominatedBy)))],
            [.. value.PrunedCandidates.Select(item =>
                new TacticalPrunedCandidateResponse(
                    Candidate(item.Candidate),
                    item.Rule,
                    item.ReasonIdentity,
                    MapEvidence(item.Evidence),
                    item.Dominator is null
                        ? null
                        : Candidate(item.Dominator)))],
            [.. value.FeasibleResults.Select(item =>
                new TacticalFeasibleResultResponse(
                    item.StableKey,
                    [.. item.SelectedCandidates.Select(Candidate)]))]);

    private static TacticalSearchCoverageResponse Map(
        TacticalSearchCoverage value) => new(
            new TacticalSearchBoundsResponse(
                value.Bounds.MaximumOptions,
                value.Bounds.MaximumExploredCombinations,
                checked((int)value.Bounds.MaximumElapsed.TotalMilliseconds),
                value.Bounds.MaximumResults),
            value.CandidateUniverseCount,
            value.RoleSupportedCount,
            value.AdmittedCount,
            value.RejectedCount,
            value.UnsupportedCount,
            value.IrrelevantCount,
            value.DominatedCount,
            value.SearchedOptionCount,
            value.ExploredCombinationCount,
            value.FeasibleResultCount,
            value.RetainedResultCount,
            value.FirstTerminator,
            checked((long)value.Elapsed.TotalMilliseconds),
            value.Fingerprint,
            [.. value.Caches.Select(item => new TacticalCacheDiagnosticResponse(
                item.CacheIdentity,
                item.HitCount,
                item.MissCount))]);

    private static TacticalScoringResponse? Map(
        TacticalCombatScoringResult? value) => value is null
        ? null
        : new(
            value.SemanticFingerprint,
            value.ScoringVersion,
            value.Weights.Policy,
            value.PolicyLimitationIdentity,
            [.. value.RankedCandidates.Select(item =>
                new TacticalScoredLoadoutResponse(
                    item.Candidate.StableKey,
                    item.TotalScore,
                    [.. item.Components.Select(component =>
                        new TacticalScoreComponentResponse(
                            component.Kind,
                            component.State,
                            component.NormalizationIdentity,
                            component.BaseWeight,
                            component.AppliedWeight,
                            component.NormalizedValue,
                            component.Contribution,
                            [.. component.RawInputs.Select(input =>
                                new TacticalScoreInputResponse(
                                    input.Kind,
                                    input.Identity,
                                    input.State,
                                    Map(input.Value),
                                    input.ReasonIdentity,
                                    MapEvidence(input.Evidence)))],
                            MapEvidence(component.Evidence),
                            component.Limitations))],
                    [.. item.UnusedCapacity.Categories.Select(capacity =>
                        new TacticalUnusedCapacityResponse(
                            capacity.Category,
                            capacity.Remaining,
                            capacity.Capacity))]))]);

    private static TacticalSelectedLoadoutResponse? MapSelected(
        TacticalCompiledCombatPlan? value)
    {
        if (value is null)
        {
            return null;
        }

        var selected = value.SelectedLoadout;
        var loadout = selected.Candidate.Loadout;
        return new TacticalSelectedLoadoutResponse(
            value.SelectedLoadoutFingerprint,
            selected.Candidate.StableKey,
            selected.TotalScore,
            [.. selected.Candidate.SelectedCandidates.Select(Candidate)],
            [.. Enum.GetValues<SkillCategory>().Select(category =>
                new TacticalLoadoutCategoryResponse(
                    category,
                    loadout.Proposal.Skills.Get(category),
                    Map(loadout.SlotBudgets[category])))],
            Map(loadout.Proposal.GenericSlotAllocation));
    }

    private static TacticalPlanResponse? MapPlan(
        TacticalCompiledCombatPlan? compiled)
    {
        if (compiled is null)
        {
            return null;
        }

        var plan = compiled.Plan;
        return new TacticalPlanResponse(
            compiled.SemanticFingerprint,
            plan.GameDataVersion,
            plan.RuleVersion,
            compiled.FinishDisposition,
            [.. plan.Facts.Select(item => new TacticalStateFactResponse(
                Fact(item.Identity),
                item.State,
                Map(item.Value),
                item.ReasonIdentity,
                MapEvidence(item.Evidence),
                [.. item.Conflicts.Select(conflict =>
                    new TacticalConflictResponse(
                        Map(conflict.Value)!,
                        Map(conflict.Evidence))) ]))],
            [.. plan.Requirements.Select(item =>
                new TacticalRequirementDefinitionResponse(
                    item.Identity.Code,
                    Fact(item.Fact),
                    item.Operator,
                    Map(item.ExpectedValue)))],
            [.. plan.Transitions.Select(item =>
                new TacticalPlanTransitionResponse(
                    item.Identity.Code,
                    [.. item.Preconditions.Select(identity => identity.Code)],
                    [.. item.ResultingFacts.Select(Fact)],
                    item.Timing,
                    item.ExpectedPurposeIdentity,
                    item.LimitationIdentity,
                    MapEvidence(item.Evidence)))],
            [.. plan.Roles.Select(item => new TacticalPlanRoleResponse(
                Role(item.Identity),
                item.SkillId,
                item.Direction,
                item.EffectId,
                item.Timing,
                [.. item.Transitions.Select(identity => identity.Code)],
                [.. item.Requirements.Select(identity => identity.Code)],
                item.LimitationIdentity,
                MapEvidence(item.Evidence)))],
            [.. plan.Stages.Select(item => new TacticalPlanStageResponse(
                item.Stage,
                item.State,
                item.LimitationIdentity,
                [.. item.Steps.Select(step => new TacticalPlanStepResponse(
                    step.Identity.Code,
                    step.Order,
                    step.BranchKind,
                    [.. step.ObservedFacts.Select(Fact)],
                    [.. step.Requirements.Select(Map)],
                    [.. step.Transitions.Select(identity => identity.Code)],
                    step.SkillId,
                    step.ManualActionIdentity,
                    step.ExpectedPurposeIdentity,
                    step.LimitationIdentity,
                    [.. step.Branches.Select(branch =>
                        new TacticalPlanBranchResponse(
                            branch.ConditionIdentity,
                            branch.Outcome,
                            branch.TargetStep?.Code))],
                    MapEvidence(step.Evidence)))],
                MapEvidence(item.Evidence)))],
            [.. compiled.PreparationChecks.Select(item =>
                new TacticalPreparationCheckResponse(
                    item.Identity,
                    item.Kind,
                    item.ManualActionIdentity,
                    item.Category,
                    item.SkillId,
                    item.Direction))],
            MapEvidence(plan.SharedEvidence));
    }

    private static TacticalRequirementEvaluationResponse Map(
        TacticalRequirementEvaluation value) => new(
            value.Requirement.Code,
            value.Outcome,
            value.ReasonIdentity,
            MapEvidence(value.Evidence));

    private static TacticalFactValueResponse? Map(TacticalFactValue? value) =>
        value is null
            ? null
            : new TacticalFactValueResponse(value.Kind, value.CanonicalValue);

    private static TacticalEvidenceResponse Map(
        TacticalEvidenceReference value) => new(
            value.Source,
            value.EvidenceIdentity,
            value.GameDataVersion,
            value.RuleVersion,
            value.ScopeIdentity);

    private static IReadOnlyList<TacticalEvidenceResponse> MapEvidence(
        IEnumerable<TacticalEvidenceReference> values) =>
        [.. values.Select(Map)];

    private static TacticalSlotBudgetResponse Map(SlotBudget value) => new(
        value.Category,
        value.Used.IsAvailable,
        value.Used.IsAvailable ? value.Used.Value : null,
        value.Capacity,
        value.Used.UnavailableReason);

    private static GenericSlotPlanResponse Map(
        GenericSlotAllocation value) => new(
            value.TotalSlots,
            value.Attack,
            value.Agility,
            value.Defense,
            value.Assistance);

    private static TacticalRecommendationWorkCountsResponse Map(
        TacticalRecommendationWorkCounts value) => new(
            value.SnapshotReads,
            value.LegacyRecommendationBuilds,
            value.ComparisonBuilds,
            value.RuleResolutions,
            value.ContextProjections,
            value.CandidateDiscoveries,
            value.Searches,
            value.Scores,
            value.PlanCompilations);

    private static string Candidate(TacticalCandidateIdentity value) =>
        $"{value.SkillId}:{value.Direction.ToString().ToUpperInvariant()}";

    private static string Fact(TacticalFactIdentity value) =>
        $"{value.Kind.ToString().ToUpperInvariant()}:{value.Code}";

    private static string Role(TacticalRoleIdentity value) =>
        $"{value.Kind.ToString().ToUpperInvariant()}:{value.Code}";
}
