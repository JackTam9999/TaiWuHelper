using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public static class CombatLoadoutGenerator
{
    public static CombatLoadoutGenerationResult Generate(
        CombatLoadoutGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<CombatLoadoutGenerationDiagnostic> diagnostics = [];
        var eligibleOptions = GetEligibleOptions(request, diagnostics);
        if (eligibleOptions.Length == 0)
        {
            diagnostics.Add(
                Diagnostic(
                    CombatLoadoutGenerationDiagnosticCode.NoEligibleOptions,
                    "No eligible loadout options remain after hard "
                    + "candidate filters."));
            return new CombatLoadoutGenerationResult(
                candidates: [],
                diagnostics,
                exploredCombinations: 0);
        }

        var requiredOptions = eligibleOptions
            .Where(option => IsRequiredCurrentNeigong(request.Player, option))
            .ToArray();
        var retentionOptions = eligibleOptions
            .Except(requiredOptions)
            .Where(IsPureRetentionOption)
            .OrderBy(option => EffectiveCost(request.Player, option))
            .ThenBy(option => option.Candidate.SkillId)
            .ToArray();
        var strategicOptions = eligibleOptions
            .Except(requiredOptions)
            .Except(retentionOptions)
            .ToArray();
        List<GeneratedCombatLoadout> generated = [];
        List<CombatLoadoutOption> selected = [.. requiredOptions];
        var explored = 0;
        var explorationTruncated = false;

        Explore(index: 0);

        if (explorationTruncated)
        {
            diagnostics.Add(
                Diagnostic(
                    CombatLoadoutGenerationDiagnosticCode
                        .ExplorationLimitReached,
                    $"Combination exploration stopped at the configured "
                    + $"limit of {request.MaxExploredCombinations}."));
        }

        var ordered = generated
            .GroupBy(candidate => candidate.StableKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderByDescending(
                candidate => candidate.CombatStartCounterCount)
            .ThenByDescending(candidate => candidate.HardCounterCount)
            .ThenByDescending(candidate => candidate.ThreatCodes.Length)
            .ThenByDescending(
                candidate => candidate.RetainedCurrentSkillCount)
            .ThenBy(candidate => candidate.SelectedOptions.Length)
            .ThenBy(candidate => candidate.StableKey, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Length > request.MaxResults)
        {
            diagnostics.Add(
                Diagnostic(
                    CombatLoadoutGenerationDiagnosticCode
                        .ResultLimitReached,
                    $"Feasible results were limited to "
                    + $"{request.MaxResults} of {ordered.Length}."));
        }

        return new CombatLoadoutGenerationResult(
            ordered.Take(request.MaxResults),
            diagnostics,
            explored);

        void Explore(int index)
        {
            if (explored >= request.MaxExploredCombinations)
            {
                explorationTruncated = true;
                return;
            }

            if (index == strategicOptions.Length)
            {
                EvaluateWithMaximumRetention();
                return;
            }

            selected.Add(strategicOptions[index]);
            Explore(index + 1);
            selected.RemoveAt(selected.Count - 1);
            Explore(index + 1);
        }

        void EvaluateWithMaximumRetention()
        {
            var strategicSelectionCount = selected.Count;
            GeneratedCombatLoadout? best = null;
            if (selected.Count > 0)
            {
                best = TryEvaluateSelected();
            }

            foreach (var option in retentionOptions)
            {
                if (explorationTruncated)
                {
                    break;
                }

                selected.Add(option);
                var retained = TryEvaluateSelected();
                if (retained is null)
                {
                    selected.RemoveAt(selected.Count - 1);
                }
                else
                {
                    best = retained;
                }
            }

            if (best is not null)
            {
                generated.Add(best);
            }

            selected.RemoveRange(
                strategicSelectionCount,
                selected.Count - strategicSelectionCount);
        }

        GeneratedCombatLoadout? TryEvaluateSelected()
        {
            if (explored >= request.MaxExploredCombinations)
            {
                explorationTruncated = true;
                return null;
            }

            explored++;
            return EvaluateCombination(request, selected, diagnostics);
        }
    }

    private static bool IsRequiredCurrentNeigong(
        PlayerCombatSnapshot player,
        CombatLoadoutOption option)
    {
        if (!option.IsCurrentlyEquipped)
        {
            return false;
        }

        var skill = player.LearnedSkills.Single(
            value => value.SkillId == option.Candidate.SkillId);
        return skill.Category == SkillCategory.Neigong;
    }

    private static bool IsPureRetentionOption(CombatLoadoutOption option)
    {
        return option.IsCurrentlyEquipped
            && option.ThreatCodes.IsEmpty
            && option.Requirements.IsEmpty
            && !option.CounterStrength.HasValue
            && !option.ActivationTiming.HasValue;
    }

    private static int EffectiveCost(
        PlayerCombatSnapshot player,
        CombatLoadoutOption option)
    {
        var cost = CombatSkillCostCalculator.Calculate(
            player,
            option.Candidate.SkillId).EffectiveCost;
        return cost.IsAvailable ? cost.Value : int.MaxValue;
    }

    private static CombatLoadoutOption[] GetEligibleOptions(
        CombatLoadoutGenerationRequest request,
        List<CombatLoadoutGenerationDiagnostic> diagnostics)
    {
        List<CombatLoadoutOption> eligible = [];
        foreach (var option in request.Options)
        {
            var validation = CombatSkillCandidateValidator.Validate(
                request.Player,
                option.Candidate);
            if (validation.IsAccepted
                && HasExpectedEffect(option, validation))
            {
                eligible.Add(option);
                continue;
            }

            var effectFailure = validation.IsAccepted
                ? $"Skill {option.Candidate.SkillId} does not "
                + $"match expected effect {option.ExpectedEffectId}."
                : null;
            diagnostics.Add(
                Diagnostic(
                    CombatLoadoutGenerationDiagnosticCode.OptionRejected,
                    effectFailure
                        ?? string.Join(
                            " ",
                            validation.Rejections.Select(
                                rejection => rejection.Reason)),
                    option.Candidate.SkillId));
            if (option.IsCurrentlyEquipped)
            {
                eligible.Add(
                    CombatLoadoutOption.RetainCurrentSkill(
                        option.Candidate.SkillId,
                        option.EvidenceReference));
            }
        }

        return [.. eligible
            .OrderByDescending(option => option.IsCombatStartCounter)
            .ThenByDescending(option => option.IsHardCounter)
            .ThenByDescending(option => option.ThreatCodes.Length)
            .ThenByDescending(option => option.IsCurrentlyEquipped)
            .ThenBy(option => option.Candidate.SkillId)];
    }

    private static bool HasExpectedEffect(
        CombatLoadoutOption option,
        CombatSkillCandidateValidationResult validation)
    {
        if (!option.ExpectedEffectId.HasValue)
        {
            return true;
        }

        var direction = option.Candidate.RequiredDirection!.Value;
        var effectId = direction == PracticeDirection.Direct
            ? validation.Skill!.DirectEffectId
            : validation.Skill!.ReverseEffectId;
        return effectId.IsAvailable
            && effectId.Value == option.ExpectedEffectId.Value;
    }

    private static GeneratedCombatLoadout? EvaluateCombination(
        CombatLoadoutGenerationRequest request,
        List<CombatLoadoutOption> selected,
        List<CombatLoadoutGenerationDiagnostic> diagnostics)
    {
        var activeAgility = GetSingleActiveSkill(
            selected,
            CombatCounterActivationTiming.ActiveAgility);
        var activeDefense = GetSingleActiveSkill(
            selected,
            CombatCounterActivationTiming.ActiveDefense);
        if (activeAgility.IsConflict || activeDefense.IsConflict)
        {
            diagnostics.Add(
                Diagnostic(
                    CombatLoadoutGenerationDiagnosticCode
                        .ActiveRoleConflict,
                    "A combination selected more than one active agility "
                    + "or active defense skill."));
            return null;
        }

        var loadout = CreateLoadout(request.Player, selected);
        var context = CreateRequirementContext(
            request.BaseRequirementContext,
            loadout,
            activeDefense.SkillId,
            activeAgility.SkillId);
        var proposal = new ProposedCombatLoadout(
            loadout,
            request.GenericSlotAllocation,
            selected.Select(option => option.Candidate),
            selected.SelectMany(option => option.Requirements),
            context);
        var validation = CombatLoadoutFeasibilityValidator.Validate(
            request.Player,
            proposal);
        if (!validation.IsFeasible)
        {
            diagnostics.Add(
                Diagnostic(
                    CombatLoadoutGenerationDiagnosticCode
                        .CombinationInfeasible,
                    string.Join(
                        " ",
                        validation.Failures.Select(
                            failure => failure.Reason)),
                    feasibilityFailures: validation.Failures));
            return null;
        }

        return new GeneratedCombatLoadout(
            validation.FeasibleLoadout!,
            selected,
            CreateStableKey(loadout));
    }

    private static CombatLoadoutSnapshot CreateLoadout(
        PlayerCombatSnapshot player,
        List<CombatLoadoutOption> selected)
    {
        var selectedIds = selected
            .Select(option => option.Candidate.SkillId)
            .ToHashSet();
        var skillsById = player.LearnedSkills.ToDictionary(
            skill => skill.SkillId);

        int[] CategorySkills(SkillCategory category)
        {
            var current = player.EquippedSkills.Get(category)
                .Where(selectedIds.Contains);
            var added = selectedIds
                .Where(skillId =>
                    skillsById[skillId].Category == category
                    && !player.EquippedSkills.Get(category)
                        .Contains(skillId))
                .Order();
            return [.. current, .. added];
        }

        return new CombatLoadoutSnapshot(
            CategorySkills(SkillCategory.Neigong),
            CategorySkills(SkillCategory.Attack),
            CategorySkills(SkillCategory.Agility),
            CategorySkills(SkillCategory.Defense),
            CategorySkills(SkillCategory.Assistance));
    }

    private static CombatRequirementContext CreateRequirementContext(
        CombatRequirementContext source,
        CombatLoadoutSnapshot loadout,
        int? activeDefenseSkillId,
        int? activeAgilitySkillId)
    {
        var equippedSkillIds = Enum
            .GetValues<SkillCategory>()
            .SelectMany(category => loadout.Get(category));
        return new CombatRequirementContext(
            source.EquippedWeaponTypeIds,
            source.TrickCounts.Select(value =>
                new CombatTrickCount(value.Key, value.Value)),
            source.Distance,
            source.Resources.Select(value =>
                new CombatResourceAmount(value.Key, value.Value)),
            source.UnlockedWeaponTypeIds,
            equippedSkillIds,
            activeDefenseSkillId,
            activeAgilitySkillId);
    }

    private static ActiveSkillSelection GetSingleActiveSkill(
        List<CombatLoadoutOption> selected,
        CombatCounterActivationTiming timing)
    {
        var skillIds = selected
            .Where(option => option.ActivationTiming == timing)
            .Select(option => option.Candidate.SkillId)
            .ToArray();
        return new ActiveSkillSelection(
            skillIds.Length == 1 ? skillIds[0] : null,
            skillIds.Length > 1);
    }

    private static string CreateStableKey(CombatLoadoutSnapshot loadout)
    {
        return string.Join(
            "|",
            Enum.GetValues<SkillCategory>().Select(
                category => $"{category}:"
                    + string.Join(",", loadout.Get(category))));
    }

    private static CombatLoadoutGenerationDiagnostic Diagnostic(
        CombatLoadoutGenerationDiagnosticCode code,
        string reason,
        int? skillId = null,
        IEnumerable<CombatLoadoutFeasibilityFailure>? feasibilityFailures =
            null)
    {
        return new CombatLoadoutGenerationDiagnostic(
            code,
            reason,
            skillId,
            feasibilityFailures);
    }

    private sealed record ActiveSkillSelection(
        int? SkillId,
        bool IsConflict);
}
