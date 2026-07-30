using TaiWu.Domain.CombatCounters;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Presentation;

public enum ManualChecklistItemKind
{
    RemoveSkill,
    AddSkill,
    RetainSkill,
    ChangeDirection,
    AllocateGenericSlots,
    ConfirmWeapon,
    ConfirmResource
}

public sealed record ManualChecklistItemViewModel(
    string Reference,
    ManualChecklistItemKind Kind,
    string Instruction,
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
                $"Allocate {category.GenericSlots} 萬用 slot(s) to "
                + $"{category.DisplayName}.",
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
                $"Confirm for {SkillName(value.Skill)}: "
                + value.Condition.Evaluation,
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
                ManualLoadoutChangeKind.Retain =>
                    ManualChecklistItemKind.RetainSkill,
                ManualLoadoutChangeKind.ChangeDirection =>
                    ManualChecklistItemKind.ChangeDirection,
                _ => throw new ArgumentOutOfRangeException(nameof(change))
            },
            change.Kind switch
            {
                ManualLoadoutChangeKind.Remove =>
                    $"Remove skill {change.SkillId} manually.",
                ManualLoadoutChangeKind.Add =>
                    $"Add skill {change.SkillId} to {change.Category} manually.",
                ManualLoadoutChangeKind.Retain =>
                    $"Keep skill {change.SkillId} in {change.Category}.",
                ManualLoadoutChangeKind.ChangeDirection =>
                    $"Change skill {change.SkillId} to "
                    + $"{DirectionLabel(change.RequiredDirection)}.",
                _ => throw new ArgumentOutOfRangeException(nameof(change))
            },
            change.Reason.Reference,
            change.Reason.EvidenceReferences);
    }

    private static int ChangeOrder(ManualLoadoutChangeKind kind) => kind switch
    {
        ManualLoadoutChangeKind.Remove => 0,
        ManualLoadoutChangeKind.Add => 1,
        ManualLoadoutChangeKind.Retain => 2,
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
        skill.Name ?? $"skill {skill.SkillId}";
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

        var beforeCombat = style.OpeningActions
            .Where(step => step.Kind is
                BattlePlanInstructionKind.ConfirmEquipped
                or BattlePlanInstructionKind.SwitchBeforeCombat)
            .Select(MapStep)
            .ToArray();
        var opening = style.OpeningActions
            .Where(step => step.Kind == BattlePlanInstructionKind.ActivateSkill)
            .Select(MapStep)
            .ToArray();
        var skills = style.Categories
            .SelectMany(category => category.Skills)
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
            .Select(MapStep)
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
        BattlePlanStepViewModel step)
    {
        return new BattlePlanItemViewModel(
            step.Reference,
            step.Condition,
            step.SkillId,
            step.Reason.Reference,
            step.Reason.ThreatReferences,
            step.Reason.EvidenceReferences);
    }

    private static BattlePlanItemViewModel MapSkillInstruction(
        RecommendedSkillViewModel skill,
        BattlePlanPhaseKind phase)
    {
        var reason = skill.Reasons.FirstOrDefault();
        var timing = phase == BattlePlanPhaseKind.NormalExecution
            ? "Use"
            : "Activate";
        return new BattlePlanItemViewModel(
            $"{skill.Reference}:plan:{phase}",
            $"{timing} {skill.Name ?? $"skill {skill.SkillId}"} when its "
            + "listed conditions and linked threat timing are present.",
            skill.SkillId,
            reason?.Reference,
            skill.ThreatReferences,
            reason?.EvidenceReferences
                ?? skill.Cost.EvidenceReferences);
    }
}
