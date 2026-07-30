using System.ComponentModel.DataAnnotations;
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

    public CurrentScreenLoadoutRequest? CurrentScreenObservation
    {
        get;
        init;
    }
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
            GenericSlotAllocation.ToDomain());
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
