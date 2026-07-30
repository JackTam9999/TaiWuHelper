using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.CombatRecommendations;

public static class ManualCombatPlanBuilder
{
    private const int MaximumRoleAlternatives = 3;

    public static ManualCombatPlanResult Build(
        PlayerCombatSnapshot player,
        CombatRecommendationScoringResult scoring)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(scoring);

        if (scoring.RankedCandidates.IsEmpty)
        {
            return new ManualCombatPlanResult(
                plan: null,
                "No feasible scored candidate is available for a manual "
                + "combat plan.");
        }

        var selected = scoring.RankedCandidates[0];
        var defense = BuildRoleRecommendation(
            SkillCategory.Defense,
            CombatCounterActivationTiming.ActiveDefense,
            scoring.RankedCandidates);
        var agility = BuildRoleRecommendation(
            SkillCategory.Agility,
            CombatCounterActivationTiming.ActiveAgility,
            scoring.RankedCandidates);
        var plan = new ManualCombatPlan(
            selected,
            BuildLoadoutChanges(player, selected),
            defense,
            agility,
            BuildOpeningActions(selected),
            BuildSwitchingConditions(defense, agility));

        return new ManualCombatPlanResult(plan, diagnostic: null);
    }

    private static ManualLoadoutChange[] BuildLoadoutChanges(
        PlayerCombatSnapshot player,
        ScoredCombatLoadout selected)
    {
        List<ManualLoadoutChange> changes = [];
        var proposal = selected.Candidate.FeasibleLoadout.Proposal;
        var optionsBySkillId = selected.Candidate.SelectedOptions
            .ToDictionary(option => option.Candidate.SkillId);

        foreach (var category in Enum.GetValues<SkillCategory>())
        {
            var current = player.EquippedSkills.Get(category)
                .ToHashSet();
            var proposed = proposal.Skills.Get(category)
                .ToHashSet();

            foreach (var skillId in proposed.Except(current).Order())
            {
                changes.Add(
                    new ManualLoadoutChange(
                        ManualLoadoutChangeKind.Add,
                        category,
                        skillId,
                        requiredDirection: null,
                        OptionReason(
                            optionsBySkillId[skillId],
                            "ADD_RECOMMENDED_SKILL",
                            "Add the skill because it is part of the "
                            + "highest-ranked feasible loadout.")));
            }

            foreach (var skillId in current.Except(proposed).Order())
            {
                var component = selected.Get(
                    RecommendationScoreComponentKind.OpportunityCost);
                changes.Add(
                    new ManualLoadoutChange(
                        ManualLoadoutChangeKind.Remove,
                        category,
                        skillId,
                        requiredDirection: null,
                        ScoreReason(
                            component,
                            "REMOVE_FROM_RECOMMENDED_LOADOUT",
                            "Remove the skill manually because it is absent "
                            + "from the highest-ranked feasible loadout.")));
            }

            foreach (var skillId in current.Intersect(proposed).Order())
            {
                var component = selected.Get(
                    RecommendationScoreComponentKind
                        .CurrentLoadoutCompatibility);
                changes.Add(
                    new ManualLoadoutChange(
                        ManualLoadoutChangeKind.Retain,
                        category,
                        skillId,
                        requiredDirection: null,
                        ScoreReason(
                            component,
                            "RETAIN_CURRENT_SKILL",
                            "Retain the skill because the selected loadout "
                            + "preserves this current selection.")));
            }
        }

        foreach (var option in selected.Candidate.SelectedOptions
                     .OrderBy(value => value.Candidate.SkillId))
        {
            var validation = CombatSkillCandidateValidator.Validate(
                player,
                option.Candidate);
            if (!validation.RequiredDirectionChange.HasValue)
            {
                continue;
            }

            var category = validation.Skill!.Category;
            changes.Add(
                new ManualLoadoutChange(
                    ManualLoadoutChangeKind.ChangeDirection,
                    category,
                    option.Candidate.SkillId,
                    validation.RequiredDirectionChange,
                    OptionReason(
                        option,
                        "CHANGE_PRACTICE_DIRECTION",
                        "Change direction manually to activate the verified "
                        + $"{validation.RequiredDirectionChange.Value} "
                        + "effect used by this recommendation.")));
        }

        return
        [
            .. changes
                .OrderBy(change => change.Kind)
                .ThenBy(change => change.Category)
                .ThenBy(change => change.SkillId)
        ];
    }

    private static CombatRoleRecommendation BuildRoleRecommendation(
        SkillCategory category,
        CombatCounterActivationTiming timing,
        IEnumerable<ScoredCombatLoadout> rankedCandidates)
    {
        var choices = rankedCandidates
            .Select(candidate => CreateRoleChoice(candidate, timing))
            .Where(choice => choice is not null)
            .Cast<CombatRoleChoice>()
            .DistinctBy(choice => (
                choice.SkillId,
                choice.RequiredDirection))
            .ToArray();

        return new CombatRoleRecommendation(
            category,
            choices.FirstOrDefault(),
            choices.Skip(1).Take(MaximumRoleAlternatives));
    }

    private static CombatRoleChoice? CreateRoleChoice(
        ScoredCombatLoadout scored,
        CombatCounterActivationTiming timing)
    {
        var option = scored.Candidate.SelectedOptions.SingleOrDefault(
            value => value.ActivationTiming == timing);
        if (option is null)
        {
            return null;
        }

        return new CombatRoleChoice(
            option.Candidate.SkillId,
            option.Candidate.RequiredDirection,
            scored.TotalScore,
            OptionReason(
                option,
                timing == CombatCounterActivationTiming.ActiveDefense
                    ? "ACTIVE_DEFENSE_CHOICE"
                    : "ACTIVE_AGILITY_CHOICE",
                "Use this feasible active-role choice according to its "
                + "ranked candidate and verified counter evidence."));
    }

    private static BattlePlanInstruction[] BuildOpeningActions(
        ScoredCombatLoadout selected)
    {
        var ordered = selected.Candidate.SelectedOptions
            .Where(option => option.ActivationTiming.HasValue)
            .OrderBy(option => OpeningOrder(option.ActivationTiming!.Value))
            .ThenBy(option => option.Candidate.SkillId)
            .ToArray();

        return
        [
            .. ordered.Select((option, index) =>
                new BattlePlanInstruction(
                    index + 1,
                    IsPassive(option.ActivationTiming!.Value)
                        ? BattlePlanInstructionKind.ConfirmEquipped
                        : BattlePlanInstructionKind.ActivateSkill,
                    option.Candidate.SkillId,
                    alternativeSkillId: null,
                    OpeningCondition(option.ActivationTiming.Value),
                    OptionReason(
                        option,
                        "OPENING_COUNTER_ACTION",
                        "Follow this opening step to obtain the verified "
                        + "counter effect represented by the selected "
                        + "candidate.")))
        ];
    }

    private static BattlePlanInstruction[] BuildSwitchingConditions(
        CombatRoleRecommendation defense,
        CombatRoleRecommendation agility)
    {
        var pairs = new[] { defense, agility }
            .Where(role => role.Primary is not null)
            .SelectMany(role => role.Alternatives.Select(alternative => (
                Primary: role.Primary!,
                Alternative: alternative)));

        return
        [
            .. pairs.Select((pair, index) =>
                new BattlePlanInstruction(
                    index + 1,
                    BattlePlanInstructionKind.SwitchBeforeCombat,
                    pair.Primary.SkillId,
                    pair.Alternative.SkillId,
                    "Before combat or between attempts, choose the "
                    + "alternative if the primary skill's activation "
                    + "requirements cannot be satisfied.",
                    pair.Alternative.Reason))
        ];
    }

    private static RecommendationReason OptionReason(
        CombatLoadoutOption option,
        string code,
        string summary)
    {
        return new RecommendationReason(
            code,
            summary,
            [option.EvidenceReference],
            option.ThreatCodes);
    }

    private static RecommendationReason ScoreReason(
        RecommendationScoreComponent component,
        string code,
        string summary)
    {
        return new RecommendationReason(
            code,
            summary,
            [component.EvidenceReference],
            threatCodes: []);
    }

    private static bool IsPassive(
        CombatCounterActivationTiming timing) =>
        timing is CombatCounterActivationTiming.CombatStartPassive
            or CombatCounterActivationTiming.EquippedPassive;

    private static int OpeningOrder(
        CombatCounterActivationTiming timing) => timing switch
        {
            CombatCounterActivationTiming.CombatStartPassive => 0,
            CombatCounterActivationTiming.EquippedPassive => 1,
            CombatCounterActivationTiming.ActiveDefense => 2,
            CombatCounterActivationTiming.ActiveAgility => 3,
            CombatCounterActivationTiming.ActiveAttack => 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(timing),
                timing,
                "Unknown counter activation timing.")
        };

    private static string OpeningCondition(
        CombatCounterActivationTiming timing) => timing switch
        {
            CombatCounterActivationTiming.CombatStartPassive =>
                "Before combat begins, confirm this passive is equipped.",
            CombatCounterActivationTiming.EquippedPassive =>
                "Keep this passive equipped while its counter is needed.",
            CombatCounterActivationTiming.ActiveDefense =>
                "At the opening, select this as the active defense skill; "
                + "activate it only when its requirements are satisfied.",
            CombatCounterActivationTiming.ActiveAgility =>
                "At the opening, select this as the active agility skill; "
                + "activate it only when its requirements are satisfied.",
            CombatCounterActivationTiming.ActiveAttack =>
                "At the opening, use this attack only when its activation "
                + "requirements are satisfied.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(timing),
                timing,
                "Unknown counter activation timing.")
        };
}
