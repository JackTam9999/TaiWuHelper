using System.Collections.Immutable;

namespace TaiWu.Domain.CombatRecommendations;

public sealed record ManualCombatPlan
{
    internal ManualCombatPlan(
        ScoredCombatLoadout selectedRecommendation,
        IEnumerable<ManualLoadoutChange> loadoutChanges,
        CombatRoleRecommendation defense,
        CombatRoleRecommendation agility,
        IEnumerable<BattlePlanInstruction> openingActions,
        IEnumerable<BattlePlanInstruction> switchingConditions)
    {
        SelectedRecommendation = selectedRecommendation;
        LoadoutChanges = [.. loadoutChanges];
        Defense = defense;
        Agility = agility;
        OpeningActions = [.. openingActions];
        SwitchingConditions = [.. switchingConditions];
    }

    public ScoredCombatLoadout SelectedRecommendation { get; }

    public ImmutableArray<ManualLoadoutChange> LoadoutChanges { get; }

    public CombatRoleRecommendation Defense { get; }

    public CombatRoleRecommendation Agility { get; }

    public ImmutableArray<BattlePlanInstruction> OpeningActions { get; }

    public ImmutableArray<BattlePlanInstruction> SwitchingConditions { get; }
}
