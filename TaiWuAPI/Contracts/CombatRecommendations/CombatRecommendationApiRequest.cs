using System.ComponentModel.DataAnnotations;
using TaiWu.Application.CombatRecommendations;
using TaiWu.Application.CombatSkills;
using TaiWu.Application.Localization;
using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWuAPI.Contracts.CombatRecommendations;

public sealed class CombatRecommendationApiRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int? TargetCharacterId { get; init; }

    public RecommendationPolicy Objective { get; init; } =
        RecommendationPolicy.Balanced;

    public TaiwuLanguage Language { get; init; } =
        TaiwuLanguage.English;

    public CurrentScreenLoadoutRequest? CurrentScreenObservation
    {
        get;
        init;
    }

    public TargetObservationApiRequest? TargetObservation { get; init; }

    public TacticalPlanningApiRequest? TacticalPlanning { get; init; }
}

public sealed class TargetObservationApiRequest
{
    public TargetObservationContext Context { get; init; } =
        TargetObservationContext.Sparring;

    public DateTimeOffset ObservedAt { get; init; }

    [Required]
    [StringLength(
        SnapshotFieldSource.MaximumEvidenceReferenceLength,
        MinimumLength = 1)]
    public string EvidenceReference { get; init; } = string.Empty;

    public TargetLoadoutCoverageKind Coverage { get; init; } =
        TargetLoadoutCoverageKind.PartialLoadout;

    [Required]
    public IReadOnlyList<TargetObservedSkillApiRequest> SelectedSkills
    {
        get;
        init;
    } = [];

    public bool ConfirmPrecedenceWhenSaveTimeUnavailable { get; init; }

    internal TargetObservationRequest ToApplication()
    {
        if (ObservedAt == default)
        {
            throw new ArgumentException(
                "Target observation observedAt is required.",
                nameof(ObservedAt));
        }

        if (SelectedSkills is null)
        {
            throw new ArgumentException(
                "Target observation selectedSkills is required.",
                nameof(SelectedSkills));
        }

        return new TargetObservationRequest(
            Context,
            ObservedAt,
            EvidenceReference,
            Coverage,
            SelectedSkills.Select(skill => skill?.ToApplication()
                ?? throw new ArgumentException(
                    "Target observation skills cannot contain null.")),
            ConfirmPrecedenceWhenSaveTimeUnavailable);
    }
}

public sealed class TargetObservedSkillApiRequest
{
    [Required]
    [StringLength(CombatSkillSearchRequest.MaximumQueryLength,
        MinimumLength = 1)]
    public string VisibleName { get; init; } = string.Empty;

    public SkillCategory Category { get; init; }

    [Range(0, int.MaxValue)]
    public int? ConfirmedSkillId { get; init; }

    public PracticeDirection? Direction { get; init; }

    [Range(0, int.MaxValue)]
    public int? SlotIndex { get; init; }

    [Range(0, int.MaxValue)]
    public int? VisiblePowerPercent { get; init; }

    internal TargetObservedSkillRequest ToApplication() => new(
        VisibleName,
        Category,
        ConfirmedSkillId,
        Direction,
        SlotIndex,
        VisiblePowerPercent);
}

public sealed class CurrentScreenLoadoutRequest
{
    public DateTimeOffset ObservedAt { get; init; }

    [Required]
    public string EvidenceReference { get; init; } = string.Empty;

    [Required]
    public CombatLoadoutRequest EquippedSkills { get; init; } = new();

    [Required]
    public GenericSlotAllocationRequest GenericSlotAllocation
    {
        get;
        init;
    } = new();

    public DisplayedSlotBudgetSetRequest? DisplayedSlotBudgets
    {
        get;
        init;
    }

    internal PlayerLoadoutObservation ToDomain()
    {
        if (ObservedAt == default)
        {
            throw new ArgumentException(
                "Current-screen observedAt is required.",
                nameof(ObservedAt));
        }

        return new PlayerLoadoutObservation(
            ObservedAt,
            EvidenceReference,
            EquippedSkills.ToDomain(),
            GenericSlotAllocation.ToDomain(),
            DisplayedSlotBudgets?.ToDomain());
    }
}

public sealed class CombatLoadoutRequest
{
    public IReadOnlyList<int> NeigongSkillIds { get; init; } = [];

    public IReadOnlyList<int> AttackSkillIds { get; init; } = [];

    public IReadOnlyList<int> AgilitySkillIds { get; init; } = [];

    public IReadOnlyList<int> DefenseSkillIds { get; init; } = [];

    public IReadOnlyList<int> AssistanceSkillIds { get; init; } = [];

    internal CombatLoadoutSnapshot ToDomain()
    {
        return new CombatLoadoutSnapshot(
            NeigongSkillIds,
            AttackSkillIds,
            AgilitySkillIds,
            DefenseSkillIds,
            AssistanceSkillIds);
    }
}

public sealed class DisplayedSlotBudgetSetRequest
{
    [Required]
    public DisplayedSlotBudgetRequest? Neigong { get; init; }

    [Required]
    public DisplayedSlotBudgetRequest? Attack { get; init; }

    [Required]
    public DisplayedSlotBudgetRequest? Agility { get; init; }

    [Required]
    public DisplayedSlotBudgetRequest? Defense { get; init; }

    [Required]
    public DisplayedSlotBudgetRequest? Assistance { get; init; }

    internal SlotBudgetSet ToDomain()
    {
        return new SlotBudgetSet(
        [
            Required(Neigong, SkillCategory.Neigong),
            Required(Attack, SkillCategory.Attack),
            Required(Agility, SkillCategory.Agility),
            Required(Defense, SkillCategory.Defense),
            Required(Assistance, SkillCategory.Assistance)
        ]);
    }

    private static SlotBudget Required(
        DisplayedSlotBudgetRequest? request,
        SkillCategory category)
    {
        return request?.ToDomain(category)
            ?? throw new ArgumentException(
                $"Displayed {category} slot budget is required.");
    }
}

public sealed class DisplayedSlotBudgetRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int? Used { get; init; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? Capacity { get; init; }

    internal SlotBudget ToDomain(SkillCategory category)
    {
        if (Used is null || Capacity is null)
        {
            throw new ArgumentException(
                $"Displayed {category} used and capacity values are required.");
        }

        return new SlotBudget(category, Used.Value, Capacity.Value);
    }
}

public sealed class GenericSlotAllocationRequest
{
    public int TotalSlots { get; init; }

    public int Attack { get; init; }

    public int Agility { get; init; }

    public int Defense { get; init; }

    public int Assistance { get; init; }

    internal GenericSlotAllocation ToDomain()
    {
        return new GenericSlotAllocation(
            TotalSlots,
            Attack,
            Agility,
            Defense,
            Assistance);
    }
}
