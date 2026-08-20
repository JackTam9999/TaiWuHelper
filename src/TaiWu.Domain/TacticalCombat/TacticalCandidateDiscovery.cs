using System.Collections.Immutable;
using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public static class TacticalCandidateDiscovery
{
    private const string SaveEvidence = "SAVE_SNAPSHOT";
    private const string RuleEvidence = "VERIFIED_TACTICAL_RULE_SET";
    private const string DiscoveryEvidence = "TACTICAL_CANDIDATE_DISCOVERY";

    public static TacticalCandidateDiscoveryResult Discover(
        PlayerCombatSnapshot player,
        TacticalExecutionContext context,
        TacticalCombatRuleResolution ruleResolution,
        TacticalCandidateDiscoveryLimits? limits = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(ruleResolution);
        cancellationToken.ThrowIfCancellationRequested();
        var configuredLimits = limits ?? TacticalCandidateDiscoveryLimits.Default;
        if (player.LearnedSkills.Length > configuredLimits.MaxLearnedSkills)
        {
            throw new ArgumentException(
                "The learned-skill snapshot exceeds the bounded discovery limit.",
                nameof(player));
        }

        if (!string.Equals(
                ruleResolution.RuleSetFingerprint,
                context.RuleSetFingerprint,
                StringComparison.Ordinal)
            || ruleResolution.Status != context.RuleResolutionStatus)
        {
            throw new ArgumentException(
                "Candidate discovery requires the context's exact rule resolution.",
                nameof(ruleResolution));
        }

        var equipped = Enum.GetValues<SkillCategory>()
            .SelectMany(category => player.EquippedSkills.Get(category))
            .ToHashSet();
        List<TacticalCandidateDiscoveryEntry> considerations = [];
        foreach (var skill in player.LearnedSkills.OrderBy(item => item.SkillId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var direction in Directions(skill))
            {
                cancellationToken.ThrowIfCancellationRequested();
                considerations.Add(Consider(
                    player,
                    context,
                    ruleResolution,
                    skill,
                    direction,
                    equipped.Contains(skill.SkillId)));
            }
        }

        return new TacticalCandidateDiscoveryResult(
            context.SemanticFingerprint,
            player.LearnedSkills.Length,
            ruleResolution.Roles.Length,
            considerations,
            configuredLimits);
    }

    private static TacticalCandidateDiscoveryEntry Consider(
        PlayerCombatSnapshot player,
        TacticalExecutionContext context,
        TacticalCombatRuleResolution resolution,
        CombatSkillSnapshot skill,
        DirectionOption direction,
        bool isCurrentlyEquipped)
    {
        List<TacticalCandidateGateResult> gates = [];
        gates.Add(Passed(
            TacticalCandidateGateKind.Ownership,
            "SKILL_OWNERSHIP_CONFIRMED",
            SaveEvidence));
        gates.Add(Mastery(skill));
        gates.Add(Direction(skill, direction));

        var rawEffect = RawEffect(skill, direction.Direction);
        var exactMatches = resolution.Roles.Where(match =>
                match.Rule.SkillId == skill.SkillId
                && match.Rule.Direction == direction.Direction).ToArray();
        if (exactMatches.Length > 1)
        {
            throw new InvalidOperationException(
                "One skill direction cannot resolve to multiple tactical roles in version 1.");
        }

        var match = exactMatches.SingleOrDefault();
        var role = match is null
            ? null
            : new TacticalCandidateRoleProjection(match.Rule);
        var support = Support(resolution, skill, direction, match);
        gates.Add(support.Gate);
        gates.Add(RawEffectGate(rawEffect, match));
        gates.Add(RuleEvidenceGate(resolution, match));
        gates.Add(ExecutionRequirementGate(context, match));
        gates.Add(BacklashGate(context, skill, match));

        var cost = EffectiveCost(player, context, skill);
        gates.Add(cost.Gate);
        gates.Add(CategoryCapacityGate(
            context,
            skill.Category,
            cost.Fact));
        gates.Add(UniversalSlotGate(context, skill.Category));
        gates.Add(isCurrentlyEquipped
            ? Passed(
                TacticalCandidateGateKind.CurrentRetention,
                "CURRENT_LOADOUT_RETENTION_CONFIRMED",
                SaveEvidence)
            : NotApplicable(
                TacticalCandidateGateKind.CurrentRetention,
                "SKILL_NOT_CURRENTLY_EQUIPPED",
                SaveEvidence));

        var admission = Admission(
            support.State,
            isCurrentlyEquipped,
            gates);
        var core = CoreConsideration(
            resolution,
            skill,
            direction.Direction,
            admission,
            match,
            gates);
        return new TacticalCandidateDiscoveryEntry(
            core,
            skill.Category,
            direction.RequiresBreakthrough,
            isCurrentlyEquipped,
            support.State,
            admission,
            rawEffect,
            cost.Fact,
            role,
            gates);
    }

    private static ImmutableArray<DirectionOption> Directions(
        CombatSkillSnapshot skill)
    {
        return
        [
            Option(skill, PracticeDirection.Direct),
            Option(skill, PracticeDirection.Reverse)
        ];
    }

    private static DirectionOption Option(
        CombatSkillSnapshot skill,
        PracticeDirection direction)
    {
        var active = skill.Direction.IsAvailable
            && skill.Direction.Value == direction;
        var completed = skill.BreakthroughDirections.IsAvailable
            && skill.BreakthroughDirections.Value.HasCompleted(direction);
        var breakthrough = skill.BreakthroughDirections.IsAvailable
            && skill.BreakthroughDirections.Value.Includes(direction);
        return new DirectionOption(
            direction,
            RequiresBreakthrough: !active && !completed && breakthrough,
            RequiresDirectionChange: !active && completed,
            IsAvailable: active || completed || breakthrough);
    }

    private static TacticalCandidateGateResult Mastery(
        CombatSkillSnapshot skill)
    {
        if (!skill.Mastered.IsAvailable)
        {
            return Unknown(
                TacticalCandidateGateKind.Mastery,
                "MASTERY_STATUS_UNKNOWN",
                SaveEvidence);
        }

        return Passed(
            TacticalCandidateGateKind.Mastery,
            skill.Mastered.Value
                ? "MASTERY_CONFIRMED"
                : "UNMASTERED_COST_CONFIRMED",
            SaveEvidence);
    }

    private static TacticalCandidateGateResult Direction(
        CombatSkillSnapshot skill,
        DirectionOption direction)
    {
        if (!direction.IsAvailable)
        {
            return !skill.Direction.IsAvailable
                && !skill.BreakthroughDirections.IsAvailable
                ? Unknown(
                    TacticalCandidateGateKind.Direction,
                    "PRACTICE_DIRECTION_UNKNOWN",
                    SaveEvidence)
                : Failed(
                    TacticalCandidateGateKind.Direction,
                    "PRACTICE_DIRECTION_NOT_AVAILABLE",
                    SaveEvidence);
        }

        if (direction.RequiresBreakthrough)
        {
            return Passed(
                TacticalCandidateGateKind.Direction,
                "IMMEDIATE_BREAKTHROUGH_DIRECTION_CONFIRMED",
                SaveEvidence);
        }

        return Passed(
            TacticalCandidateGateKind.Direction,
            direction.RequiresDirectionChange
                ? "COMPLETED_DIRECTION_CHANGE_CONFIRMED"
                : "ACTIVE_DIRECTION_CONFIRMED",
            SaveEvidence);
    }

    private static SupportResult Support(
        TacticalCombatRuleResolution resolution,
        CombatSkillSnapshot skill,
        DirectionOption direction,
        TacticalSkillRoleRuleMatch? match)
    {
        if (!resolution.IsResolved)
        {
            return new SupportResult(
                TacticalCandidateSupportState.UnsupportedGameDataVersion,
                Unsupported(
                    TacticalCandidateGateKind.TacticalRole,
                    "TACTICAL_RULE_VERSION_UNSUPPORTED",
                    RuleEvidence));
        }

        if (match is not null)
        {
            return new SupportResult(
                TacticalCandidateSupportState.VerifiedRole,
                Passed(
                    TacticalCandidateGateKind.TacticalRole,
                    "EXACT_TACTICAL_ROLE_VERIFIED",
                    RuleEvidence));
        }

        var wrongDirection = resolution.Roles.Any(item =>
            item.Rule.SkillId == skill.SkillId
            && item.Rule.Direction != direction.Direction);
        return new SupportResult(
            TacticalCandidateSupportState.UnsupportedEffect,
            Unsupported(
                TacticalCandidateGateKind.TacticalRole,
                wrongDirection
                    ? "TACTICAL_ROLE_WRONG_DIRECTION"
                    : "TACTICAL_EFFECT_UNSUPPORTED",
                RuleEvidence));
    }

    private static TacticalContextFact<int> RawEffect(
        CombatSkillSnapshot skill,
        PracticeDirection direction)
    {
        var effect = direction == PracticeDirection.Direct
            ? skill.DirectEffectId
            : skill.ReverseEffectId;
        return effect.IsAvailable
            ? TacticalContextFact<int>.Available(
                effect.Value,
                TacticalContextOrigin.SaveSnapshot,
                TacticalContextAvailability.FixedForRequest,
                "RAW_EFFECT_CAPTURED",
                SaveEvidence)
            : TacticalContextFact<int>.Unavailable(
                TacticalContextFactState.Unknown,
                TacticalContextOrigin.SaveSnapshot,
                TacticalContextAvailability.FixedForRequest,
                "RAW_EFFECT_UNKNOWN",
                SaveEvidence);
    }

    private static TacticalCandidateGateResult RawEffectGate(
        TacticalContextFact<int> observed,
        TacticalSkillRoleRuleMatch? match)
    {
        if (!observed.IsAvailable)
        {
            return observed.State == TacticalContextFactState.Unsupported
                ? Unsupported(
                    TacticalCandidateGateKind.RawEffect,
                    observed.ReasonIdentity,
                    observed.EvidenceIdentities)
                : Unknown(
                    TacticalCandidateGateKind.RawEffect,
                    observed.ReasonIdentity,
                    observed.EvidenceIdentities);
        }

        if (match is null)
        {
            return NotApplicable(
                TacticalCandidateGateKind.RawEffect,
                "NO_VERIFIED_EFFECT_TO_COMPARE",
                SaveEvidence);
        }

        return observed.Value == match.Rule.RawEffectId
            ? Passed(
                TacticalCandidateGateKind.RawEffect,
                "EXACT_RAW_EFFECT_VERIFIED",
                SaveEvidence,
                RuleEvidence)
            : Failed(
                TacticalCandidateGateKind.RawEffect,
                "RAW_EFFECT_ID_MISMATCH",
                SaveEvidence,
                RuleEvidence);
    }

    private static TacticalCandidateGateResult RuleEvidenceGate(
        TacticalCombatRuleResolution resolution,
        TacticalSkillRoleRuleMatch? match)
    {
        if (!resolution.IsResolved || match is null)
        {
            return NotApplicable(
                TacticalCandidateGateKind.RuleEvidence,
                "NO_RESOLVED_ROLE_EVIDENCE",
                RuleEvidence);
        }

        var evidence = match.UnmetEvidence
            .Select(item => item.Code)
            .Append(RuleEvidence)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return match.Applicability switch
        {
            TacticalRuleApplicability.Applicable => Passed(
                TacticalCandidateGateKind.RuleEvidence,
                "ROLE_EVIDENCE_APPLICABLE",
                RuleEvidence),
            TacticalRuleApplicability.Incomplete => Unknown(
                TacticalCandidateGateKind.RuleEvidence,
                "ROLE_EVIDENCE_INCOMPLETE",
                evidence),
            TacticalRuleApplicability.Conflicting => Conflicting(
                TacticalCandidateGateKind.RuleEvidence,
                "ROLE_EVIDENCE_CONFLICTING",
                evidence),
            TacticalRuleApplicability.Contrary => Failed(
                TacticalCandidateGateKind.RuleEvidence,
                "ROLE_EVIDENCE_CONTRARY",
                evidence),
            _ => throw new ArgumentOutOfRangeException(nameof(match))
        };
    }

    private static TacticalCandidateGateResult ExecutionRequirementGate(
        TacticalExecutionContext context,
        TacticalSkillRoleRuleMatch? match)
    {
        if (match is null)
        {
            return NotApplicable(
                TacticalCandidateGateKind.ExecutionRequirements,
                "NO_VERIFIED_ROLE_REQUIREMENTS",
                RuleEvidence);
        }

        if (match.Rule.SharedCounter is null)
        {
            return Unknown(
                TacticalCandidateGateKind.ExecutionRequirements,
                "EXECUTION_REQUIREMENTS_NOT_TYPED",
                RuleEvidence);
        }

        var requirements = match.Rule.SharedCounter.Requirements;
        if (requirements.IsEmpty)
        {
            return Passed(
                TacticalCandidateGateKind.ExecutionRequirements,
                "NO_ADDITIONAL_EXECUTION_REQUIREMENTS",
                RuleEvidence);
        }

        var results = requirements
            .Select(item => EvaluateRequirement(context.Proposed, item))
            .ToArray();
        var evidence = results.SelectMany(item => item.Evidence)
            .Append(RuleEvidence)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (results.Any(item => item.State == TacticalCandidateGateState.Failed))
        {
            return Failed(
                TacticalCandidateGateKind.ExecutionRequirements,
                "EXECUTION_REQUIREMENTS_UNSATISFIED",
                evidence);
        }

        if (results.Any(item => item.State == TacticalCandidateGateState.Unknown))
        {
            return Unknown(
                TacticalCandidateGateKind.ExecutionRequirements,
                "EXECUTION_REQUIREMENTS_UNKNOWN",
                evidence);
        }

        return Passed(
            TacticalCandidateGateKind.ExecutionRequirements,
            "EXECUTION_REQUIREMENTS_SATISFIED",
            evidence);
    }

    private static RequirementResult EvaluateRequirement(
        ProposedTacticalExecutionFacts facts,
        CombatRequirement requirement) => requirement switch
    {
        WeaponRequirement value => Membership(
            facts.EquippedWeaponTypeIds,
            item => item.Contains(value.WeaponTypeId),
            "REQUIREMENT:EQUIPPED_WEAPON"),
        WeaponUnlockRequirement value => Membership(
            facts.UnlockedWeaponTypeIds,
            item => item.Contains(value.WeaponTypeId),
            "REQUIREMENT:UNLOCKED_WEAPON"),
        RangeRequirement value => Number(
            facts.Distance,
            item => (!value.MinimumInclusive.HasValue
                    || item >= value.MinimumInclusive.Value)
                && (!value.MaximumInclusive.HasValue
                    || item <= value.MaximumInclusive.Value),
            "REQUIREMENT:DISTANCE"),
        ResourceRequirement value => Resource(facts.Resources, value),
        SkillActivationRequirement value => value.RequiredState switch
        {
            SkillActivationState.EquippedPassive => Membership(
                facts.EquippedSkillIds,
                item => item.Contains(value.SkillId),
                "REQUIREMENT:EQUIPPED_SKILL"),
            SkillActivationState.ActiveDefense => Number(
                facts.ActiveDefenseSkillId,
                item => item == value.SkillId,
                "REQUIREMENT:ACTIVE_DEFENSE"),
            SkillActivationState.ActiveAgility => Number(
                facts.ActiveAgilitySkillId,
                item => item == value.SkillId,
                "REQUIREMENT:ACTIVE_AGILITY"),
            _ => throw new ArgumentOutOfRangeException(nameof(requirement))
        },
        TrickRequirement => new RequirementResult(
            TacticalCandidateGateState.Unknown,
            ["REQUIREMENT:TRICK_COUNTS", DiscoveryEvidence]),
        _ => throw new ArgumentOutOfRangeException(nameof(requirement))
    };

    private static RequirementResult Membership(
        TacticalContextFact<ImmutableArray<int>> fact,
        Func<ImmutableArray<int>, bool> predicate,
        string evidence) => !fact.IsAvailable
        ? new RequirementResult(
            TacticalCandidateGateState.Unknown,
            fact.EvidenceIdentities.Append(evidence).ToArray())
        : new RequirementResult(
            predicate(fact.Value)
                ? TacticalCandidateGateState.Passed
                : TacticalCandidateGateState.Failed,
            fact.EvidenceIdentities.Append(evidence).ToArray());

    private static RequirementResult Number(
        TacticalContextFact<int> fact,
        Func<int, bool> predicate,
        string evidence) => !fact.IsAvailable
        ? new RequirementResult(
            TacticalCandidateGateState.Unknown,
            fact.EvidenceIdentities.Append(evidence).ToArray())
        : new RequirementResult(
            predicate(fact.Value)
                ? TacticalCandidateGateState.Passed
                : TacticalCandidateGateState.Failed,
            fact.EvidenceIdentities.Append(evidence).ToArray());

    private static RequirementResult Resource(
        TacticalContextFact<ImmutableArray<CombatResourceAmount>> fact,
        ResourceRequirement requirement)
    {
        if (!fact.IsAvailable)
        {
            return new RequirementResult(
                TacticalCandidateGateState.Unknown,
                fact.EvidenceIdentities.Append("REQUIREMENT:RESOURCE")
                    .ToArray());
        }

        var amount = fact.Value.SingleOrDefault(item =>
            item.Resource == requirement.Resource);
        if (amount is null || !amount.Amount.IsAvailable)
        {
            return new RequirementResult(
                TacticalCandidateGateState.Unknown,
                fact.EvidenceIdentities.Append("REQUIREMENT:RESOURCE")
                    .ToArray());
        }

        return new RequirementResult(
            amount.Amount.Value >= requirement.MinimumAmount
                ? TacticalCandidateGateState.Passed
                : TacticalCandidateGateState.Failed,
            fact.EvidenceIdentities.Append("REQUIREMENT:RESOURCE").ToArray());
    }

    private static TacticalCandidateGateResult BacklashGate(
        TacticalExecutionContext context,
        CombatSkillSnapshot skill,
        TacticalSkillRoleRuleMatch? match)
    {
        if (match is null || !IsActiveUse(match.Rule))
        {
            return NotApplicable(
                TacticalCandidateGateKind.InnerPowerBacklash,
                "BACKLASH_GATE_NOT_APPLICABLE",
                RuleEvidence);
        }

        if (!context.Proposed.InnerPower.IsAvailable)
        {
            return Unknown(
                TacticalCandidateGateKind.InnerPowerBacklash,
                "INNER_POWER_CONTEXT_UNKNOWN",
                context.Proposed.InnerPower.EvidenceIdentities);
        }

        if (!skill.Element.IsAvailable)
        {
            return Unknown(
                TacticalCandidateGateKind.InnerPowerBacklash,
                "SKILL_ELEMENT_UNKNOWN",
                SaveEvidence);
        }

        return context.Proposed.InnerPower.Value.BacklashOnUseElement
                == skill.Element.Value
            ? Failed(
                TacticalCandidateGateKind.InnerPowerBacklash,
                "INNER_POWER_BACKLASH_ON_USE",
                SaveEvidence)
            : Passed(
                TacticalCandidateGateKind.InnerPowerBacklash,
                "INNER_POWER_BACKLASH_CLEAR",
                SaveEvidence);
    }

    private static bool IsActiveUse(TacticalSkillRoleRule role)
    {
        if (role.SharedCounter is not null)
        {
            return role.SharedCounter.ActivationTiming is
                CombatCounterActivationTiming.ActiveAttack
                or CombatCounterActivationTiming.ActiveDefense
                or CombatCounterActivationTiming.ActiveAgility;
        }

        return role.Timing is TacticalTransitionTiming.DuringCast
            or TacticalTransitionTiming.AfterCast
            or TacticalTransitionTiming.AfterManualAction;
    }

    private static CostResult EffectiveCost(
        PlayerCombatSnapshot player,
        TacticalExecutionContext context,
        CombatSkillSnapshot skill)
    {
        CombatSkillCostBreakdown? breakdown = null;
        if (context.Proposed.LegendaryCostAssignments.IsAvailable)
        {
            var assignment = context.Proposed.LegendaryCostAssignments.Value
                .SingleOrDefault(item => item.SkillId == skill.SkillId);
            breakdown = assignment is null
                ? CombatSkillCostCalculator
                    .CalculateWithoutLegendaryAssignment(player, skill.SkillId)
                : CombatSkillCostCalculator.CalculateProposed(
                    player,
                    assignment);
        }
        else if (context.Current.LegendaryCostSlots.IsAvailable
            && context.Current.LegendaryCostSlots.Value.IsEmpty)
        {
            breakdown = CombatSkillCostCalculator
                .CalculateWithoutLegendaryAssignment(player, skill.SkillId);
        }

        if (breakdown is null || !breakdown.EffectiveCost.IsAvailable)
        {
            var fact = TacticalContextFact<int>.Unavailable(
                TacticalContextFactState.Unknown,
                TacticalContextOrigin.ProposedPlan,
                TacticalContextAvailability.PreCombatConfigurable,
                breakdown is null
                    ? "LEGENDARY_COST_ASSIGNMENT_UNKNOWN"
                    : "EFFECTIVE_COST_UNKNOWN",
                SaveEvidence);
            return new CostResult(
                fact,
                Unknown(
                    TacticalCandidateGateKind.EffectiveCost,
                    fact.ReasonIdentity,
                    fact.EvidenceIdentities));
        }

        var available = TacticalContextFact<int>.Available(
            breakdown.EffectiveCost.Value,
            TacticalContextOrigin.ProposedPlan,
            TacticalContextAvailability.PreCombatConfigurable,
            "EFFECTIVE_COST_VERIFIED",
            SaveEvidence);
        return new CostResult(
            available,
            Passed(
                TacticalCandidateGateKind.EffectiveCost,
                "EFFECTIVE_COST_VERIFIED",
                SaveEvidence));
    }

    private static TacticalCandidateGateResult CategoryCapacityGate(
        TacticalExecutionContext context,
        SkillCategory category,
        TacticalContextFact<int> effectiveCost)
    {
        if (!effectiveCost.IsAvailable)
        {
            return Unknown(
                TacticalCandidateGateKind.CategoryCapacity,
                "CATEGORY_CAPACITY_DEPENDS_ON_UNKNOWN_COST",
                effectiveCost.EvidenceIdentities);
        }

        if (!context.Proposed.SlotBudgets.IsAvailable)
        {
            return Unknown(
                TacticalCandidateGateKind.CategoryCapacity,
                "PROPOSED_CATEGORY_CAPACITY_UNKNOWN",
                context.Proposed.SlotBudgets.EvidenceIdentities);
        }

        var capacity = context.Proposed.SlotBudgets.Value[category].Capacity;
        return effectiveCost.Value <= capacity
            ? Passed(
                TacticalCandidateGateKind.CategoryCapacity,
                "CANDIDATE_FITS_CATEGORY_CAPACITY",
                context.Proposed.SlotBudgets.EvidenceIdentities)
            : Failed(
                TacticalCandidateGateKind.CategoryCapacity,
                "CANDIDATE_EXCEEDS_CATEGORY_CAPACITY",
                context.Proposed.SlotBudgets.EvidenceIdentities);
    }

    private static TacticalCandidateGateResult UniversalSlotGate(
        TacticalExecutionContext context,
        SkillCategory category)
    {
        if (category == SkillCategory.Neigong)
        {
            return NotApplicable(
                TacticalCandidateGateKind.UniversalSlots,
                "UNIVERSAL_SLOTS_NOT_APPLICABLE_TO_NEIGONG",
                DiscoveryEvidence);
        }

        return context.Proposed.UniversalSlotAllocation.IsAvailable
            ? Passed(
                TacticalCandidateGateKind.UniversalSlots,
                "UNIVERSAL_SLOT_ALLOCATION_VERIFIED",
                context.Proposed.UniversalSlotAllocation.EvidenceIdentities)
            : Unknown(
                TacticalCandidateGateKind.UniversalSlots,
                "PROPOSED_UNIVERSAL_SLOTS_UNKNOWN",
                context.Proposed.UniversalSlotAllocation.EvidenceIdentities);
    }

    private static TacticalCandidateAdmissionState Admission(
        TacticalCandidateSupportState support,
        bool isCurrentlyEquipped,
        IEnumerable<TacticalCandidateGateResult> gates)
    {
        if (support != TacticalCandidateSupportState.VerifiedRole)
        {
            return isCurrentlyEquipped
                ? TacticalCandidateAdmissionState.RetainedOnly
                : TacticalCandidateAdmissionState.Unsupported;
        }

        var values = gates.ToArray();
        if (values.Any(item => item.State == TacticalCandidateGateState.Failed))
        {
            return TacticalCandidateAdmissionState.Infeasible;
        }

        if (values.Any(item => item.State is
            TacticalCandidateGateState.Unknown
            or TacticalCandidateGateState.Conflicting))
        {
            return TacticalCandidateAdmissionState.UnknownContext;
        }

        if (values.Any(item => item.State
            == TacticalCandidateGateState.Unsupported))
        {
            return TacticalCandidateAdmissionState.Unsupported;
        }

        return TacticalCandidateAdmissionState.Admitted;
    }

    private static TacticalCandidateConsideration CoreConsideration(
        TacticalCombatRuleResolution resolution,
        CombatSkillSnapshot skill,
        PracticeDirection direction,
        TacticalCandidateAdmissionState admission,
        TacticalSkillRoleRuleMatch? match,
        IReadOnlyList<TacticalCandidateGateResult> gates)
    {
        var evidence = EvidenceFor(resolution, skill.SkillId, direction, match);
        var requirements = gates.Select(gate =>
            new TacticalRequirementEvaluation(
                new TacticalRequirementIdentity(
                    $"CANDIDATE_GATE_{TacticalCombatText.EnumKey(gate.Kind)}"),
                GateOutcome(gate.State),
                gate.ReasonIdentity,
                evidence));
        return new TacticalCandidateConsideration(
            new TacticalCandidateIdentity(skill.SkillId, direction),
            admission switch
            {
                TacticalCandidateAdmissionState.Admitted =>
                    TacticalCandidateDecision.Admitted,
                TacticalCandidateAdmissionState.Infeasible =>
                    TacticalCandidateDecision.Rejected,
                _ => TacticalCandidateDecision.Unsupported
            },
            match is null ? [] : [match.Rule.Identity],
            requirements,
            admission switch
            {
                TacticalCandidateAdmissionState.Admitted =>
                    "CANDIDATE_ADMITTED",
                TacticalCandidateAdmissionState.RetainedOnly =>
                    "CURRENT_RETENTION_ONLY",
                TacticalCandidateAdmissionState.Infeasible =>
                    "CANDIDATE_INFEASIBLE",
                TacticalCandidateAdmissionState.UnknownContext =>
                    "CANDIDATE_CONTEXT_UNKNOWN",
                TacticalCandidateAdmissionState.Unsupported =>
                    "CANDIDATE_UNSUPPORTED",
                _ => throw new ArgumentOutOfRangeException(nameof(admission))
            },
            evidence);
    }

    private static TacticalRequirementOutcome GateOutcome(
        TacticalCandidateGateState state) => state switch
    {
        TacticalCandidateGateState.Passed
            or TacticalCandidateGateState.NotApplicable =>
                TacticalRequirementOutcome.Satisfied,
        TacticalCandidateGateState.Failed =>
            TacticalRequirementOutcome.Unsatisfied,
        TacticalCandidateGateState.Unknown =>
            TacticalRequirementOutcome.Unknown,
        TacticalCandidateGateState.Conflicting =>
            TacticalRequirementOutcome.Conflicting,
        TacticalCandidateGateState.Unsupported =>
            TacticalRequirementOutcome.Unsupported,
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    private static ImmutableArray<TacticalEvidenceReference> EvidenceFor(
        TacticalCombatRuleResolution resolution,
        int skillId,
        PracticeDirection direction,
        TacticalSkillRoleRuleMatch? match)
    {
        var snapshot = new TacticalEvidenceReference(
            TacticalEvidenceSourceKind.SaveSnapshot,
            "LEARNED_SKILL_ATLAS",
            resolution.GameDataVersion,
            "TACTICAL_CANDIDATE_DISCOVERY@1.0.0",
            $"SKILL_{skillId}_{TacticalCombatText.EnumKey(direction)}");
        return
        [
            .. (match?.Rule.Evidence ?? []).Append(snapshot)
                .DistinctBy(item => item.StableKey, StringComparer.Ordinal)
                .OrderBy(item => item.StableKey, StringComparer.Ordinal)
        ];
    }

    private static TacticalCandidateGateResult Passed(
        TacticalCandidateGateKind kind,
        string reason,
        params string[] evidence) => new(
        kind,
        TacticalCandidateGateState.Passed,
        reason,
        evidence);

    private static TacticalCandidateGateResult Passed(
        TacticalCandidateGateKind kind,
        string reason,
        IEnumerable<string> evidence) => new(
        kind,
        TacticalCandidateGateState.Passed,
        reason,
        evidence);

    private static TacticalCandidateGateResult Failed(
        TacticalCandidateGateKind kind,
        string reason,
        params string[] evidence) => new(
        kind,
        TacticalCandidateGateState.Failed,
        reason,
        evidence);

    private static TacticalCandidateGateResult Failed(
        TacticalCandidateGateKind kind,
        string reason,
        IEnumerable<string> evidence) => new(
        kind,
        TacticalCandidateGateState.Failed,
        reason,
        evidence);

    private static TacticalCandidateGateResult Unknown(
        TacticalCandidateGateKind kind,
        string reason,
        params string[] evidence) => new(
        kind,
        TacticalCandidateGateState.Unknown,
        reason,
        evidence);

    private static TacticalCandidateGateResult Unknown(
        TacticalCandidateGateKind kind,
        string reason,
        IEnumerable<string> evidence) => new(
        kind,
        TacticalCandidateGateState.Unknown,
        reason,
        evidence);

    private static TacticalCandidateGateResult Unsupported(
        TacticalCandidateGateKind kind,
        string reason,
        params string[] evidence) => new(
        kind,
        TacticalCandidateGateState.Unsupported,
        reason,
        evidence);

    private static TacticalCandidateGateResult Conflicting(
        TacticalCandidateGateKind kind,
        string reason,
        IEnumerable<string> evidence) => new(
        kind,
        TacticalCandidateGateState.Conflicting,
        reason,
        evidence);

    private static TacticalCandidateGateResult Unsupported(
        TacticalCandidateGateKind kind,
        string reason,
        IEnumerable<string> evidence) => new(
        kind,
        TacticalCandidateGateState.Unsupported,
        reason,
        evidence);

    private static TacticalCandidateGateResult NotApplicable(
        TacticalCandidateGateKind kind,
        string reason,
        params string[] evidence) => new(
        kind,
        TacticalCandidateGateState.NotApplicable,
        reason,
        evidence);

    private sealed record DirectionOption(
        PracticeDirection Direction,
        bool RequiresBreakthrough,
        bool RequiresDirectionChange,
        bool IsAvailable);

    private sealed record SupportResult(
        TacticalCandidateSupportState State,
        TacticalCandidateGateResult Gate);

    private sealed record RequirementResult(
        TacticalCandidateGateState State,
        IReadOnlyList<string> Evidence);

    private sealed record CostResult(
        TacticalContextFact<int> Fact,
        TacticalCandidateGateResult Gate);
}
