using System.Collections.Immutable;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public static class NeigongLoadoutOptimizer
{
    public static NeigongOptimizationResult? Optimize(
        PlayerCombatSnapshot player,
        IEnumerable<CombatLoadoutOption> selectedOptions)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(selectedOptions);
        var options = selectedOptions.ToArray();
        if (options.Any(option => option is null))
        {
            throw new ArgumentException(
                "Selected options cannot contain nulls.",
                nameof(selectedOptions));
        }

        var skillsById = player.LearnedSkills.ToDictionary(
            skill => skill.SkillId);
        var mandatoryIds = options
            .Select(option => option.Candidate.SkillId)
            .Where(skillId =>
                skillsById[skillId].Category == SkillCategory.Neigong)
            .ToHashSet();
        var used = CalculateOuterUsage(player, options, skillsById);
        if (used is null)
        {
            return null;
        }

        var currentIds = player.EquippedSkills.NeigongSkillIds.ToHashSet();
        var mandatory = CreateConfiguration(
            player,
            mandatoryIds.Select(skillId => skillsById[skillId]));
        if (mandatory is null
            || mandatory.Cost > CombatSlotBudgetCalculator
                .BaseNeigongCapacity)
        {
            return null;
        }

        var maximumUseful = MaximumUsefulContribution(
            player,
            used,
            skillsById);
        Dictionary<ConfigurationKey, Configuration> states =
            new()
            {
                [Key(mandatory, maximumUseful)] = mandatory
            };
        var optionalSkills = player.LearnedSkills
            .Where(skill =>
                skill.Category == SkillCategory.Neigong
                && !mandatoryIds.Contains(skill.SkillId)
                && (skill.Direction.IsAvailable
                    || currentIds.Contains(skill.SkillId)))
            .Select(skill => (Skill: skill, Cost: EffectiveCost(player, skill)))
            .Where(value => value.Cost.HasValue)
            .Select(value => (value.Skill, Cost: value.Cost!.Value))
            .OrderByDescending(value => currentIds.Contains(
                value.Skill.SkillId))
            .ThenBy(value => value.Skill.SkillId)
            .ToArray();

        foreach (var (skill, cost) in optionalSkills)
        {
            var additions = states.Values
                .Where(state => state.Cost + cost
                    <= CombatSlotBudgetCalculator.BaseNeigongCapacity)
                .Select(state => state.Add(skill, cost))
                .ToArray();
            foreach (var addition in additions)
            {
                var key = Key(addition, maximumUseful);
                if (!states.TryGetValue(key, out var existing)
                    || IsPreferred(addition, existing, currentIds))
                {
                    states[key] = addition;
                }
            }
        }

        return states.Values
            .Select(configuration => BuildResult(
                player,
                configuration,
                used,
                skillsById))
            .Where(result => result is not null)
            .Cast<NeigongOptimizationResult>()
            .OrderByDescending(result => result.NeigongSkillIds.Count(
                currentIds.Contains))
            .ThenBy(result => ChangeCount(result, currentIds))
            .ThenBy(result => result.NeigongSkillIds.Length)
            .ThenByDescending(result => result.RemainingOuterCapacity)
            .ThenBy(result => string.Join(",", result.NeigongSkillIds),
                StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static Dictionary<SkillCategory, int>? CalculateOuterUsage(
        PlayerCombatSnapshot player,
        CombatLoadoutOption[] options,
        IReadOnlyDictionary<int, CombatSkillSnapshot> skillsById)
    {
        Dictionary<SkillCategory, int> result = [];
        foreach (var category in OuterCategories())
        {
            var total = 0;
            foreach (var option in options.Where(option =>
                         skillsById[option.Candidate.SkillId].Category
                         == category))
            {
                var cost = CombatSkillCostCalculator.Calculate(
                    player,
                    option.Candidate.SkillId).EffectiveCost;
                if (!cost.IsAvailable)
                {
                    return null;
                }

                total = checked(total + cost.Value);
            }

            result[category] = total;
        }

        return result;
    }

    private static Configuration? CreateConfiguration(
        PlayerCombatSnapshot player,
        IEnumerable<CombatSkillSnapshot> skills)
    {
        var result = Configuration.Empty;
        foreach (var skill in skills.OrderBy(skill => skill.SkillId))
        {
            var cost = EffectiveCost(player, skill);
            if (!cost.HasValue)
            {
                return null;
            }

            result = result.Add(skill, cost.Value);
        }

        return result;
    }

    private static int? EffectiveCost(
        PlayerCombatSnapshot player,
        CombatSkillSnapshot skill)
    {
        var cost = CombatSkillCostCalculator.Calculate(
            player,
            skill.SkillId).EffectiveCost;
        return cost.IsAvailable ? cost.Value : null;
    }

    private static ContributionCaps MaximumUsefulContribution(
        PlayerCombatSnapshot player,
        IReadOnlyDictionary<SkillCategory, int> used,
        IReadOnlyDictionary<int, CombatSkillSnapshot> skillsById)
    {
        int Required(SkillCategory category) => Math.Max(
            0,
            used[category]
            - CombatSlotBudgetCalculator.BaseOuterCategoryCapacity
            - ObservedCapacityAdjustment(player, category, skillsById));
        return new ContributionCaps(
            Required(SkillCategory.Attack),
            Required(SkillCategory.Agility),
            Required(SkillCategory.Defense),
            Required(SkillCategory.Assistance),
            OuterCategories().Sum(Required));
    }

    private static NeigongOptimizationResult? BuildResult(
        PlayerCombatSnapshot player,
        Configuration configuration,
        IReadOnlyDictionary<SkillCategory, int> used,
        IReadOnlyDictionary<int, CombatSkillSnapshot> skillsById)
    {
        var persistentGenericBonus = Math.Max(
            0,
            player.GenericSlotAllocation.TotalSlots
            - player.EquippedSkills.NeigongSkillIds
                .Where(skillsById.ContainsKey)
                .Sum(skillId =>
                    skillsById[skillId].SlotContribution.Generic));
        var totalGeneric = checked(
            persistentGenericBonus + configuration.Generic);
        Dictionary<SkillCategory, int> minimum = [];
        foreach (var category in OuterCategories())
        {
            var specific = configuration.GetSpecific(category);
            var nonGenericCapacity = checked(
                CombatSlotBudgetCalculator.BaseOuterCategoryCapacity
                + specific
                + ObservedCapacityAdjustment(
                    player,
                    category,
                    skillsById));
            minimum[category] = Math.Max(
                0,
                used[category] - nonGenericCapacity);
        }

        if (minimum.Values.Sum() > totalGeneric)
        {
            return null;
        }

        var allocation = AllocateGenericSlots(
            totalGeneric,
            minimum,
            player.GenericSlotAllocation);
        var remaining = OuterCategories().Sum(category =>
            CombatSlotBudgetCalculator.BaseOuterCategoryCapacity
            + configuration.GetSpecific(category)
            + ObservedCapacityAdjustment(player, category, skillsById)
            + allocation.Get(category)
            - used[category]);
        return new NeigongOptimizationResult(
            configuration.SkillIds,
            allocation,
            configuration.Cost,
            remaining);
    }

    private static GenericSlotAllocation AllocateGenericSlots(
        int total,
        IReadOnlyDictionary<SkillCategory, int> minimum,
        GenericSlotAllocation current)
    {
        var allocated = minimum.ToDictionary(pair => pair.Key, pair => pair.Value);
        var remaining = total - allocated.Values.Sum();
        foreach (var category in OuterCategories()
                     .OrderByDescending(current.Get)
                     .ThenBy(category => category))
        {
            var desired = Math.Max(
                0,
                current.Get(category) - allocated[category]);
            var added = Math.Min(desired, remaining);
            allocated[category] += added;
            remaining -= added;
        }

        return new GenericSlotAllocation(
            total,
            allocated[SkillCategory.Attack],
            allocated[SkillCategory.Agility],
            allocated[SkillCategory.Defense],
            allocated[SkillCategory.Assistance]);
    }

    private static int ObservedCapacityAdjustment(
        PlayerCombatSnapshot player,
        SkillCategory category,
        IReadOnlyDictionary<int, CombatSkillSnapshot> skillsById)
    {
        var currentNeigong = player.EquippedSkills.NeigongSkillIds
            .Where(skillsById.ContainsKey)
            .Select(skillId => skillsById[skillId]);
        var configured = CombatSlotBudgetCalculator
            .CalculateConfiguredCapacity(
                category,
                currentNeigong,
                player.GenericSlotAllocation);
        return checked(player.SlotBudgets[category].Capacity - configured);
    }

    private static bool IsPreferred(
        Configuration candidate,
        Configuration existing,
        HashSet<int> currentIds)
    {
        var candidateRetained = candidate.SkillIds.Count(currentIds.Contains);
        var existingRetained = existing.SkillIds.Count(currentIds.Contains);
        if (candidateRetained != existingRetained)
        {
            return candidateRetained > existingRetained;
        }

        var candidateChanges = ChangeCount(candidate.SkillIds, currentIds);
        var existingChanges = ChangeCount(existing.SkillIds, currentIds);
        return candidateChanges != existingChanges
            ? candidateChanges < existingChanges
            : string.CompareOrdinal(
                string.Join(",", candidate.SkillIds),
                string.Join(",", existing.SkillIds)) < 0;
    }

    private static int ChangeCount(
        NeigongOptimizationResult result,
        HashSet<int> currentIds) =>
        ChangeCount(result.NeigongSkillIds, currentIds);

    private static int ChangeCount(
        IEnumerable<int> proposed,
        HashSet<int> currentIds)
    {
        var proposedIds = proposed.ToHashSet();
        return currentIds.Except(proposedIds).Count()
            + proposedIds.Except(currentIds).Count();
    }

    private static ConfigurationKey Key(
        Configuration value,
        ContributionCaps caps) => new(
            value.Cost,
            Math.Min(value.Attack, caps.Attack),
            Math.Min(value.Agility, caps.Agility),
            Math.Min(value.Defense, caps.Defense),
            Math.Min(value.Assistance, caps.Assistance),
            Math.Min(value.Generic, caps.Generic));

    private static IEnumerable<SkillCategory> OuterCategories()
    {
        yield return SkillCategory.Attack;
        yield return SkillCategory.Agility;
        yield return SkillCategory.Defense;
        yield return SkillCategory.Assistance;
    }

    private sealed record Configuration(
        ImmutableArray<int> SkillIds,
        int Cost,
        int Attack,
        int Agility,
        int Defense,
        int Assistance,
        int Generic)
    {
        public static Configuration Empty { get; } = new(
            [], 0, 0, 0, 0, 0, 0);

        public Configuration Add(CombatSkillSnapshot skill, int cost) => new(
            [.. SkillIds, skill.SkillId],
            checked(Cost + cost),
            checked(Attack + skill.SlotContribution.Attack),
            checked(Agility + skill.SlotContribution.Agility),
            checked(Defense + skill.SlotContribution.Defense),
            checked(Assistance + skill.SlotContribution.Assistance),
            checked(Generic + skill.SlotContribution.Generic));

        public int GetSpecific(SkillCategory category) => category switch
        {
            SkillCategory.Attack => Attack,
            SkillCategory.Agility => Agility,
            SkillCategory.Defense => Defense,
            SkillCategory.Assistance => Assistance,
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }

    private readonly record struct ConfigurationKey(
        int Cost,
        int Attack,
        int Agility,
        int Defense,
        int Assistance,
        int Generic);

    private readonly record struct ContributionCaps(
        int Attack,
        int Agility,
        int Defense,
        int Assistance,
        int Generic);
}

public sealed record NeigongOptimizationResult(
    ImmutableArray<int> NeigongSkillIds,
    GenericSlotAllocation GenericSlotAllocation,
    int UsedNeigongCapacity,
    int RemainingOuterCapacity);
