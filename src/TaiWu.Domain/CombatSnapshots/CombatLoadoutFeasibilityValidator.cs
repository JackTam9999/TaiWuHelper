namespace TaiWu.Domain.CombatSnapshots;

public static class CombatLoadoutFeasibilityValidator
{
    public static CombatLoadoutFeasibilityResult Validate(
        PlayerCombatSnapshot player,
        ProposedCombatLoadout proposal)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(proposal);

        List<CombatLoadoutFeasibilityFailure> failures = [];
        var selectedSkillIds = Enum
            .GetValues<SkillCategory>()
            .SelectMany(category => proposal.Skills.Get(category))
            .ToHashSet();
        var candidateValidations = proposal.SkillCandidates
            .Select(candidate =>
                CombatSkillCandidateValidator.Validate(player, candidate))
            .ToArray();

        ValidateCandidateCoverage(
            selectedSkillIds,
            candidateValidations,
            failures);
        ValidateRequirementContext(
            selectedSkillIds,
            proposal.RequirementContext,
            failures);
        ValidateGenericSlotTotal(player, proposal, failures);

        var requirementEvaluation = CombatRequirementEvaluator.Evaluate(
            proposal.Requirements,
            proposal.RequirementContext);
        foreach (var rejection in requirementEvaluation.Rejections)
        {
            failures.Add(
                Failure(
                    CombatLoadoutFeasibilityFailureCode
                        .RequirementRejected,
                    rejection.Reason));
        }

        SlotBudgetSet? slotBudgets = null;
        try
        {
            var proposedPlayer = CreateProposedPlayer(player, proposal);
            slotBudgets = CombatSlotBudgetCalculator.Calculate(
                proposedPlayer);
            foreach (var budget in slotBudgets.Values.Where(
                         budget => !budget.Used.IsAvailable))
            {
                failures.Add(
                    Failure(
                        CombatLoadoutFeasibilityFailureCode
                            .SlotUsageUnavailable,
                        $"{budget.Category} slot usage is unavailable: "
                        + budget.Used.UnavailableReason));
            }
        }
        catch (Exception exception)
            when (exception is ArgumentException or OverflowException)
        {
            failures.Add(
                Failure(
                    CombatLoadoutFeasibilityFailureCode.SlotBudgetInvalid,
                    exception.Message));
        }

        return new CombatLoadoutFeasibilityResult(
            proposal,
            candidateValidations,
            requirementEvaluation,
            slotBudgets,
            failures);
    }

    private static void ValidateCandidateCoverage(
        HashSet<int> selectedSkillIds,
        CombatSkillCandidateValidationResult[] candidateValidations,
        List<CombatLoadoutFeasibilityFailure> failures)
    {
        var candidateIds = candidateValidations
            .Select(value => value.Candidate.SkillId)
            .ToHashSet();
        foreach (var skillId in selectedSkillIds.Except(candidateIds))
        {
            failures.Add(
                Failure(
                    CombatLoadoutFeasibilityFailureCode.CandidateMissing,
                    $"Selected skill {skillId} has no candidate "
                    + "validation specification.",
                    skillId));
        }

        foreach (var validation in candidateValidations)
        {
            var skillId = validation.Candidate.SkillId;
            if (!selectedSkillIds.Contains(skillId))
            {
                failures.Add(
                    Failure(
                        CombatLoadoutFeasibilityFailureCode
                            .CandidateNotSelected,
                        $"Candidate skill {skillId} is not selected in the "
                        + "proposed loadout.",
                        skillId));
            }

            foreach (var rejection in validation.Rejections)
            {
                failures.Add(
                    Failure(
                        CombatLoadoutFeasibilityFailureCode
                            .CandidateRejected,
                        rejection.Reason,
                        skillId));
            }
        }
    }

    private static void ValidateRequirementContext(
        HashSet<int> selectedSkillIds,
        CombatRequirementContext context,
        List<CombatLoadoutFeasibilityFailure> failures)
    {
        if (!context.EquippedSkillIds.SetEquals(selectedSkillIds))
        {
            failures.Add(
                Failure(
                    CombatLoadoutFeasibilityFailureCode
                        .RequirementContextMismatch,
                    "Requirement-context equipped skills do not match the "
                    + "proposed loadout."));
        }
    }

    private static void ValidateGenericSlotTotal(
        PlayerCombatSnapshot player,
        ProposedCombatLoadout proposal,
        List<CombatLoadoutFeasibilityFailure> failures)
    {
        var learnedById = player.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        if (proposal.Skills.NeigongSkillIds.Any(
                skillId => !learnedById.ContainsKey(skillId)))
        {
            return;
        }

        var currentConfiguredTotal = player.EquippedSkills.NeigongSkillIds
            .Where(learnedById.ContainsKey)
            .Sum(skillId =>
                learnedById[skillId].SlotContribution.Generic);
        var persistentBonus = Math.Max(
            0,
            player.GenericSlotAllocation.TotalSlots
            - currentConfiguredTotal);
        var proposedConfiguredTotal = proposal.Skills.NeigongSkillIds.Sum(
            skillId => learnedById[skillId].SlotContribution.Generic);
        var expectedTotal = checked(
            persistentBonus + proposedConfiguredTotal);

        if (proposal.GenericSlotAllocation.TotalSlots != expectedTotal)
        {
            failures.Add(
                Failure(
                    CombatLoadoutFeasibilityFailureCode
                        .GenericSlotTotalMismatch,
                    $"Proposed generic-slot total "
                    + $"{proposal.GenericSlotAllocation.TotalSlots} does "
                    + $"not match the derived available total "
                    + $"{expectedTotal}."));
        }
    }

    private static PlayerCombatSnapshot CreateProposedPlayer(
        PlayerCombatSnapshot player,
        ProposedCombatLoadout proposal)
    {
        return new PlayerCombatSnapshot(
            player.CharacterId,
            player.DisplayName,
            player.LearnedSkills,
            proposal.Skills,
            player.Equipment,
            player.SlotBudgets,
            proposal.GenericSlotAllocation,
            player.LegendaryBookCostSlots,
            player.LegendaryBookCostAssignments);
    }

    private static CombatLoadoutFeasibilityFailure Failure(
        CombatLoadoutFeasibilityFailureCode code,
        string reason,
        int? skillId = null)
    {
        return new CombatLoadoutFeasibilityFailure(
            code,
            reason,
            skillId);
    }
}
