using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Presentation;

public enum ManualChecklistItemKind
{
    RemoveSkill,
    AddSkill,
    ChangeDirection,
    CompleteBreakthrough,
    AllocateGenericSlots,
    ConfirmWeapon,
    ConfirmResource
}

public sealed record ManualChecklistItemViewModel(
    string Reference,
    ManualChecklistItemKind Kind,
    string SubjectName,
    string Instruction,
    string Reason,
    string? ReasonReference,
    IReadOnlyList<string> EvidenceReferences);

public enum BattlePlanPhaseKind
{
    BeforeCombat,
    Opening,
    NormalExecution,
    TriggerBasedReaction,
    Switching
}

public sealed record BattlePlanPhaseViewModel(
    BattlePlanPhaseKind Phase,
    string Title,
    IReadOnlyList<BattlePlanItemViewModel> Items);

public sealed record BattlePlanItemViewModel(
    string Reference,
    string SkillName,
    string Instruction,
    int? SkillId,
    string? ReasonReference,
    IReadOnlyList<string> ThreatReferences,
    IReadOnlyList<string> EvidenceReferences);

public static class ManualSetupChecklistBuilder
{
    public static IReadOnlyList<ManualChecklistItemViewModel> Build(
        RecommendationStyleViewModel style)
    {
        ArgumentNullException.ThrowIfNull(style);
        if (!style.HasRecommendation || style.CandidateReference is null)
        {
            return [];
        }

        var changes = style.ManualChanges
            .Where(change => change.Kind != ManualLoadoutChangeKind.Retain)
            .OrderBy(change => ChangeOrder(change.Kind))
            .ThenBy(change => change.Category)
            .ThenBy(change => change.SkillId)
            .Select(MapChange);
        var genericAllocations = style.Categories
            .Where(category => category.GenericSlots > 0)
            .OrderBy(category => category.Category)
            .Select(category => new ManualChecklistItemViewModel(
                $"{style.CandidateReference}:checklist:generic:"
                + category.Category,
                ManualChecklistItemKind.AllocateGenericSlots,
                category.DisplayName,
                $"Allocate {category.GenericSlots} 萬用 slot(s) to "
                + $"{category.DisplayName}.",
                "The selected recommendation requires these generic slots "
                + "in this category.",
                ReasonReference: category.Reference,
                EvidenceReferences: Array.Empty<string>()));
        var requirements = style.Categories
            .SelectMany(category => category.Skills)
            .SelectMany(skill => skill.Conditions.Select(
                condition => (Skill: skill, Condition: condition)))
            .Where(value =>
                value.Condition.Kind is RecommendationConditionKind.Weapon
                    or RecommendationConditionKind.Resource)
            .OrderBy(value => value.Condition.Kind)
            .ThenBy(value => value.Skill.SkillId)
            .ThenBy(value => value.Condition.Reference, StringComparer.Ordinal)
            .Select(value => new ManualChecklistItemViewModel(
                $"{style.CandidateReference}:checklist:requirement:"
                + value.Condition.Reference,
                value.Condition.Kind == RecommendationConditionKind.Weapon
                    ? ManualChecklistItemKind.ConfirmWeapon
                    : ManualChecklistItemKind.ConfirmResource,
                SkillName(value.Skill),
                $"Confirm for {SkillName(value.Skill)}: "
                + value.Condition.Evaluation,
                value.Condition.Kind == RecommendationConditionKind.Weapon
                    ? "This skill has a weapon condition that must be "
                        + "checked manually."
                    : "This skill has a resource condition that must be "
                        + "checked manually.",
                value.Skill.Reasons.FirstOrDefault()?.Reference,
                [value.Condition.EvidenceReference]));

        return
        [
            .. changes,
            .. genericAllocations,
            .. requirements
        ];
    }

    private static ManualChecklistItemViewModel MapChange(
        ManualLoadoutChangeViewModel change)
    {
        return new ManualChecklistItemViewModel(
            $"{change.Reference}:checklist",
            change.Kind switch
            {
                ManualLoadoutChangeKind.Remove =>
                    ManualChecklistItemKind.RemoveSkill,
                ManualLoadoutChangeKind.Add =>
                    ManualChecklistItemKind.AddSkill,
                ManualLoadoutChangeKind.ChangeDirection =>
                    ManualChecklistItemKind.ChangeDirection,
                ManualLoadoutChangeKind.CompleteBreakthrough =>
                    ManualChecklistItemKind.CompleteBreakthrough,
                _ => throw new ArgumentOutOfRangeException(nameof(change))
            },
            change.SkillName,
            change.Kind switch
            {
                ManualLoadoutChangeKind.Remove =>
                    $"Remove {change.SkillName} manually.",
                ManualLoadoutChangeKind.Add =>
                    $"Add {change.SkillName} to {change.Category} manually.",
                ManualLoadoutChangeKind.ChangeDirection =>
                    $"Change {change.SkillName} to "
                    + $"{DirectionLabel(change.RequiredDirection)}.",
                ManualLoadoutChangeKind.CompleteBreakthrough =>
                    $"Complete {change.SkillName}'s breakthrough as "
                    + $"{DirectionLabel(change.RequiredDirection)} before "
                    + "combat.",
                _ => throw new ArgumentOutOfRangeException(nameof(change))
            },
            change.Reason.Summary,
            change.Reason.Reference,
            change.Reason.EvidenceReferences);
    }

    private static int ChangeOrder(ManualLoadoutChangeKind kind) => kind switch
    {
        ManualLoadoutChangeKind.Remove => 0,
        ManualLoadoutChangeKind.CompleteBreakthrough => 1,
        ManualLoadoutChangeKind.Add => 2,
        ManualLoadoutChangeKind.ChangeDirection => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string DirectionLabel(
        PracticeDirection? direction) =>
        direction switch
        {
            PracticeDirection.Direct => "正練 (Direct)",
            PracticeDirection.Reverse => "逆練 (Reverse)",
            PracticeDirection.Neutral => "中性 (Neutral)",
            _ => "the required practice direction"
        };

    private static string SkillName(RecommendedSkillViewModel skill) =>
        skill.Name ?? "Unnamed skill";
}

public static class BattlePlanViewModelBuilder
{
    public static IReadOnlyList<BattlePlanPhaseViewModel> Build(
        RecommendationStyleViewModel style)
    {
        ArgumentNullException.ThrowIfNull(style);
        if (!style.HasRecommendation)
        {
            return [];
        }

        var skills = style.Categories
            .SelectMany(category => category.Skills)
            .ToArray();
        var skillsById = skills
            .GroupBy(skill => skill.SkillId)
            .ToDictionary(group => group.Key, group => group.First());
        var beforeCombat = style.OpeningActions
            .Where(step => step.Kind is
                BattlePlanInstructionKind.ConfirmEquipped
                or BattlePlanInstructionKind.SwitchBeforeCombat)
            .Select(step => MapStep(step, skillsById))
            .ToArray();
        var opening = style.OpeningActions
            .Where(step => step.Kind == BattlePlanInstructionKind.ActivateSkill)
            .Select(step => MapStep(step, skillsById))
            .ToArray();
        var normalExecution = skills
            .Where(skill => skill.Counter.ActivationTiming
                == CombatCounterActivationTiming.ActiveAttack)
            .Select(skill => MapSkillInstruction(
                skill,
                BattlePlanPhaseKind.NormalExecution))
            .ToArray();
        var reactions = skills
            .Where(skill => skill.Counter.ActivationTiming is
                CombatCounterActivationTiming.ActiveDefense
                or CombatCounterActivationTiming.ActiveAgility)
            .Select(skill => MapSkillInstruction(
                skill,
                BattlePlanPhaseKind.TriggerBasedReaction))
            .ToArray();
        var switching = style.SwitchingConditions
            .Select(step => MapStep(step, skillsById))
            .ToArray();

        return
        [
            new(
                BattlePlanPhaseKind.BeforeCombat,
                "Before combat",
                beforeCombat),
            new(
                BattlePlanPhaseKind.Opening,
                "Opening",
                opening),
            new(
                BattlePlanPhaseKind.NormalExecution,
                "Normal execution",
                normalExecution),
            new(
                BattlePlanPhaseKind.TriggerBasedReaction,
                "Trigger-based reactions",
                reactions),
            new(
                BattlePlanPhaseKind.Switching,
                "Switching conditions",
                switching)
        ];
    }

    private static BattlePlanItemViewModel MapStep(
        BattlePlanStepViewModel step,
        IReadOnlyDictionary<int, RecommendedSkillViewModel> skillsById)
    {
        skillsById.TryGetValue(step.SkillId, out var skill);
        return new BattlePlanItemViewModel(
            step.Reference,
            step.SkillName,
            StepInstruction(step, skill?.Counter.ActivationTiming),
            step.SkillId,
            step.Reason.Reference,
            step.Reason.ThreatReferences,
            step.Reason.EvidenceReferences);
    }

    private static string StepInstruction(
        BattlePlanStepViewModel step,
        CombatCounterActivationTiming? timing) =>
        step.Kind switch
        {
            BattlePlanInstructionKind.ConfirmEquipped
                when timing == CombatCounterActivationTiming.EquippedPassive =>
                $"Keep {step.SkillName} equipped while its counter is needed.",
            BattlePlanInstructionKind.ConfirmEquipped =>
                $"Before combat, confirm {step.SkillName} is equipped so its "
                + "passive can activate.",
            BattlePlanInstructionKind.ActivateSkill
                when timing == CombatCounterActivationTiming.ActiveDefense =>
                $"At the opening, select {step.SkillName} as the active "
                + "defense skill and activate it once its requirements are "
                + "satisfied.",
            BattlePlanInstructionKind.ActivateSkill
                when timing == CombatCounterActivationTiming.ActiveAgility =>
                $"At the opening, select {step.SkillName} as the active "
                + "agility skill and activate it once its requirements are "
                + "satisfied.",
            BattlePlanInstructionKind.ActivateSkill =>
                $"At the opening, use {step.SkillName} once its activation "
                + "requirements are satisfied.",
            BattlePlanInstructionKind.SwitchBeforeCombat =>
                "Before combat or between attempts, use "
                + $"{step.AlternativeSkillName ?? "the alternative skill"} "
                + $"instead of {step.SkillName} if {step.SkillName}'s "
                + "activation requirements cannot be satisfied.",
            _ => throw new ArgumentOutOfRangeException(nameof(step))
        };

    private static BattlePlanItemViewModel MapSkillInstruction(
        RecommendedSkillViewModel skill,
        BattlePlanPhaseKind phase)
    {
        var reason = skill.Reasons.FirstOrDefault();
        var timing = phase == BattlePlanPhaseKind.NormalExecution
            ? "Use"
            : "Activate";
        var skillName = skill.Name ?? "Unnamed skill";
        return new BattlePlanItemViewModel(
            $"{skill.Reference}:plan:{phase}",
            skillName,
            $"{timing} {skillName} when its "
            + "listed conditions and linked threat timing are present.",
            skill.SkillId,
            reason?.Reference,
            skill.ThreatReferences,
            reason?.EvidenceReferences
                ?? skill.Cost.EvidenceReferences);
    }
}
