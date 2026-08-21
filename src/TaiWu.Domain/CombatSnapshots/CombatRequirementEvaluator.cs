namespace TaiWu.Domain.CombatSnapshots;

public static class CombatRequirementEvaluator
{
    public static CombatRequirementEvaluationResult Evaluate(
        IEnumerable<CombatRequirement> requirements,
        CombatRequirementContext context)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(context);

        var requirementValues = requirements.ToArray();
        if (requirementValues.Any(requirement => requirement is null))
        {
            throw new ArgumentException(
                "Combat requirements cannot contain null entries.",
                nameof(requirements));
        }

        return new CombatRequirementEvaluationResult(
            requirementValues.Select(
                requirement => EvaluateOne(requirement, context)));
    }

    private static CombatRequirementEvaluation EvaluateOne(
        CombatRequirement requirement,
        CombatRequirementContext context)
    {
        return requirement switch
        {
            WeaponRequirement value => EvaluateMembership(
                value,
                context.EquippedWeaponTypeIds.Contains(value.WeaponTypeId),
                $"Weapon type {value.WeaponTypeId} is equipped.",
                $"Weapon type {value.WeaponTypeId} is not equipped."),
            TrickRequirement value => EvaluateTrick(value, context),
            RangeRequirement value => EvaluateRange(value, context.Distance),
            ResourceRequirement value => EvaluateResource(value, context),
            WeaponUnlockRequirement value => EvaluateMembership(
                value,
                context.UnlockedWeaponTypeIds.Contains(value.WeaponTypeId),
                $"Weapon type {value.WeaponTypeId} is unlocked.",
                $"Weapon type {value.WeaponTypeId} is not unlocked."),
            SkillActivationRequirement value =>
                EvaluateSkillActivation(value, context),
            ManualConfirmationRequirement value => Unknown(
                value,
                $"Manual confirmation is required: {value.Code}."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement.GetType(),
                "Unknown combat requirement type.")
        };
    }

    private static CombatRequirementEvaluation EvaluateTrick(
        TrickRequirement requirement,
        CombatRequirementContext context)
    {
        context.TrickCounts.TryGetValue(
            requirement.TrickTypeId,
            out var actual);
        var satisfied = actual >= requirement.MinimumCount;
        return EvaluateMembership(
            requirement,
            satisfied,
            $"Trick type {requirement.TrickTypeId} has {actual} available; "
            + $"{requirement.MinimumCount} required.",
            $"Trick type {requirement.TrickTypeId} has {actual} available; "
            + $"{requirement.MinimumCount} required.");
    }

    private static CombatRequirementEvaluation EvaluateRange(
        RangeRequirement requirement,
        SnapshotValue<int> distance)
    {
        if (!distance.IsAvailable)
        {
            return Unknown(
                requirement,
                "Combat distance is unavailable: "
                + distance.UnavailableReason);
        }

        var minimumSatisfied =
            !requirement.MinimumInclusive.HasValue
            || distance.Value >= requirement.MinimumInclusive.Value;
        var maximumSatisfied =
            !requirement.MaximumInclusive.HasValue
            || distance.Value <= requirement.MaximumInclusive.Value;
        return EvaluateMembership(
            requirement,
            minimumSatisfied && maximumSatisfied,
            $"Combat distance {distance.Value} is within the required range.",
            $"Combat distance {distance.Value} is outside the required range.");
    }

    private static CombatRequirementEvaluation EvaluateResource(
        ResourceRequirement requirement,
        CombatRequirementContext context)
    {
        if (!context.Resources.TryGetValue(
                requirement.Resource,
                out var actual))
        {
            return Unknown(
                requirement,
                $"{requirement.Resource} was not reported.");
        }

        if (!actual.IsAvailable)
        {
            return Unknown(
                requirement,
                $"{requirement.Resource} is unavailable: "
                + actual.UnavailableReason);
        }

        return EvaluateMembership(
            requirement,
            actual.Value >= requirement.MinimumAmount,
            $"{requirement.Resource} has {actual.Value}; "
            + $"{requirement.MinimumAmount} required.",
            $"{requirement.Resource} has {actual.Value}; "
            + $"{requirement.MinimumAmount} required.");
    }

    private static CombatRequirementEvaluation EvaluateSkillActivation(
        SkillActivationRequirement requirement,
        CombatRequirementContext context)
    {
        var satisfied = requirement.RequiredState switch
        {
            SkillActivationState.EquippedPassive =>
                context.EquippedSkillIds.Contains(requirement.SkillId),
            SkillActivationState.ActiveDefense =>
                context.ActiveDefenseSkillId == requirement.SkillId,
            SkillActivationState.ActiveAgility =>
                context.ActiveAgilitySkillId == requirement.SkillId,
            _ => throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement.RequiredState,
                "Unknown skill activation state.")
        };

        return EvaluateMembership(
            requirement,
            satisfied,
            $"Skill {requirement.SkillId} satisfies "
            + $"{requirement.RequiredState}.",
            $"Skill {requirement.SkillId} does not satisfy "
            + $"{requirement.RequiredState}.");
    }

    private static CombatRequirementEvaluation EvaluateMembership(
        CombatRequirement requirement,
        bool satisfied,
        string satisfiedReason,
        string unsatisfiedReason)
    {
        return new CombatRequirementEvaluation(
            requirement,
            satisfied
                ? CombatRequirementStatus.Satisfied
                : CombatRequirementStatus.Unsatisfied,
            satisfied ? satisfiedReason : unsatisfiedReason);
    }

    private static CombatRequirementEvaluation Unknown(
        CombatRequirement requirement,
        string reason)
    {
        return new CombatRequirementEvaluation(
            requirement,
            CombatRequirementStatus.Unknown,
            reason);
    }
}
