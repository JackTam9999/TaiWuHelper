using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public sealed record TacticalInnerPowerContext
{
    public TacticalInnerPowerContext(
        int stateId,
        ElementAdjustmentSet maxPowerChanges,
        ElementAdjustmentSet requirementChanges,
        CombatSkillElement? backlashOnUseElement)
    {
        if (stateId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stateId));
        }

        StateId = stateId;
        MaxPowerChanges = maxPowerChanges
            ?? throw new ArgumentNullException(nameof(maxPowerChanges));
        RequirementChanges = requirementChanges
            ?? throw new ArgumentNullException(nameof(requirementChanges));
        if (backlashOnUseElement.HasValue
            && !Enum.IsDefined(backlashOnUseElement.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(backlashOnUseElement));
        }

        BacklashOnUseElement = backlashOnUseElement;
    }

    public int StateId { get; }

    public ElementAdjustmentSet MaxPowerChanges { get; }

    public ElementAdjustmentSet RequirementChanges { get; }

    public CombatSkillElement? BacklashOnUseElement { get; }

    internal string SemanticKey => string.Join('|',
        StateId.ToString(CultureInfo.InvariantCulture),
        Adjustments(MaxPowerChanges),
        Adjustments(RequirementChanges),
        BacklashOnUseElement?.ToString().ToUpperInvariant() ?? "NONE");

    private static string Adjustments(ElementAdjustmentSet value) =>
        string.Join(':',
            value.Metal,
            value.Wood,
            value.Water,
            value.Fire,
            value.Earth);
}

public sealed record TacticalResolvedRuleState
{
    public TacticalResolvedRuleState(
        TacticalResolvedRuleKind kind,
        string ruleIdentity,
        TacticalRuleApplicability applicability,
        IEnumerable<TacticalRuleEvidenceIdentity> unmetEvidence)
    {
        Kind = TacticalCombatText.Defined(kind, nameof(kind));
        RuleIdentity = TacticalCombatText.Code(
            ruleIdentity,
            nameof(ruleIdentity));
        Applicability = TacticalCombatText.Defined(
            applicability,
            nameof(applicability));
        UnmetEvidence = TacticalCombatText.CopyUnique(
            unmetEvidence,
            item => item.StableKey,
            "unmet rule evidence",
            nameof(unmetEvidence));
        if ((Applicability == TacticalRuleApplicability.Applicable)
            != UnmetEvidence.IsEmpty)
        {
            throw new ArgumentException(
                "Only an applicable rule can have no unmet evidence.",
                nameof(unmetEvidence));
        }
    }

    public TacticalResolvedRuleKind Kind { get; }

    public string RuleIdentity { get; }

    public TacticalRuleApplicability Applicability { get; }

    public ImmutableArray<TacticalRuleEvidenceIdentity> UnmetEvidence { get; }

    internal string StableKey =>
        $"{TacticalCombatText.EnumKey(Kind)}:{RuleIdentity}";

    internal string SemanticKey => string.Join('|',
        StableKey,
        TacticalCombatText.EnumKey(Applicability),
        string.Join("||", UnmetEvidence.Select(item => item.StableKey)));
}

public sealed class CurrentTacticalExecutionFacts
{
    internal CurrentTacticalExecutionFacts(
        TacticalContextFact<ImmutableArray<int>> equippedWeaponTypeIds,
        TacticalContextFact<ImmutableArray<int>> unlockedWeaponTypeIds,
        TacticalContextFact<ImmutableArray<int>> usableCombatStyleIds,
        TacticalContextFact<ImmutableArray<CombatTrickCount>> trickCounts,
        TacticalContextFact<int> distance,
        TacticalContextFact<int> stance,
        TacticalContextFact<int> breath,
        TacticalContextFact<ImmutableArray<CombatResourceAmount>> resources,
        TacticalContextFact<int> activeDefenseSkillId,
        TacticalContextFact<int> activeAgilitySkillId,
        TacticalContextFact<TacticalInnerPowerContext> innerPower,
        TacticalContextFact<SlotBudgetSet> slotBudgets,
        TacticalContextFact<GenericSlotAllocation> universalSlotAllocation,
        TacticalContextFact<ImmutableArray<LegendaryBookCostSlot>>
            legendaryCostSlots,
        TacticalContextFact<ImmutableArray<LegendaryBookCostAssignment>>
            legendaryCostAssignments,
        TacticalContextFact<ImmutableArray<int>> equippedSkillIds)
    {
        EquippedWeaponTypeIds = equippedWeaponTypeIds;
        UnlockedWeaponTypeIds = unlockedWeaponTypeIds;
        UsableCombatStyleIds = usableCombatStyleIds;
        TrickCounts = trickCounts;
        Distance = distance;
        Stance = stance;
        Breath = breath;
        Resources = resources;
        ActiveDefenseSkillId = activeDefenseSkillId;
        ActiveAgilitySkillId = activeAgilitySkillId;
        InnerPower = innerPower;
        SlotBudgets = slotBudgets;
        UniversalSlotAllocation = universalSlotAllocation;
        LegendaryCostSlots = legendaryCostSlots;
        LegendaryCostAssignments = legendaryCostAssignments;
        EquippedSkillIds = equippedSkillIds;
    }

    public TacticalContextFact<ImmutableArray<int>> EquippedWeaponTypeIds
    { get; }

    public TacticalContextFact<ImmutableArray<int>> UnlockedWeaponTypeIds
    { get; }

    public TacticalContextFact<ImmutableArray<int>> UsableCombatStyleIds
    { get; }

    public TacticalContextFact<ImmutableArray<CombatTrickCount>> TrickCounts
    { get; }

    public TacticalContextFact<int> Distance { get; }

    public TacticalContextFact<int> Stance { get; }

    public TacticalContextFact<int> Breath { get; }

    public TacticalContextFact<ImmutableArray<CombatResourceAmount>> Resources
    { get; }

    public TacticalContextFact<int> ActiveDefenseSkillId { get; }

    public TacticalContextFact<int> ActiveAgilitySkillId { get; }

    public TacticalContextFact<TacticalInnerPowerContext> InnerPower { get; }

    public TacticalContextFact<SlotBudgetSet> SlotBudgets { get; }

    public TacticalContextFact<GenericSlotAllocation> UniversalSlotAllocation
    { get; }

    public TacticalContextFact<ImmutableArray<LegendaryBookCostSlot>>
        LegendaryCostSlots
    { get; }

    public TacticalContextFact<ImmutableArray<LegendaryBookCostAssignment>>
        LegendaryCostAssignments
    { get; }

    public TacticalContextFact<ImmutableArray<int>> EquippedSkillIds { get; }

    internal string SemanticKey => TacticalExecutionContextKeys.Facts(
        EquippedWeaponTypeIds,
        UnlockedWeaponTypeIds,
        UsableCombatStyleIds,
        TrickCounts,
        Distance,
        Stance,
        Breath,
        Resources,
        ActiveDefenseSkillId,
        ActiveAgilitySkillId,
        InnerPower,
        SlotBudgets,
        UniversalSlotAllocation,
        LegendaryCostSlots,
        LegendaryCostAssignments,
        EquippedSkillIds);
}

public sealed class ProposedTacticalExecutionFacts
{
    internal ProposedTacticalExecutionFacts(
        TacticalContextFact<ImmutableArray<int>> equippedWeaponTypeIds,
        TacticalContextFact<ImmutableArray<int>> unlockedWeaponTypeIds,
        TacticalContextFact<ImmutableArray<int>> usableCombatStyleIds,
        TacticalContextFact<ImmutableArray<CombatTrickCount>> trickCounts,
        TacticalContextFact<int> distance,
        TacticalContextFact<int> stance,
        TacticalContextFact<int> breath,
        TacticalContextFact<ImmutableArray<CombatResourceAmount>> resources,
        TacticalContextFact<int> activeDefenseSkillId,
        TacticalContextFact<int> activeAgilitySkillId,
        TacticalContextFact<TacticalInnerPowerContext> innerPower,
        TacticalContextFact<SlotBudgetSet> slotBudgets,
        TacticalContextFact<GenericSlotAllocation> universalSlotAllocation,
        TacticalContextFact<ImmutableArray<LegendaryBookCostSlot>>
            legendaryCostSlots,
        TacticalContextFact<ImmutableArray<LegendaryBookCostAssignment>>
            legendaryCostAssignments,
        TacticalContextFact<ImmutableArray<int>> equippedSkillIds)
    {
        EquippedWeaponTypeIds = equippedWeaponTypeIds;
        UnlockedWeaponTypeIds = unlockedWeaponTypeIds;
        UsableCombatStyleIds = usableCombatStyleIds;
        TrickCounts = trickCounts;
        Distance = distance;
        Stance = stance;
        Breath = breath;
        Resources = resources;
        ActiveDefenseSkillId = activeDefenseSkillId;
        ActiveAgilitySkillId = activeAgilitySkillId;
        InnerPower = innerPower;
        SlotBudgets = slotBudgets;
        UniversalSlotAllocation = universalSlotAllocation;
        LegendaryCostSlots = legendaryCostSlots;
        LegendaryCostAssignments = legendaryCostAssignments;
        EquippedSkillIds = equippedSkillIds;
    }

    public TacticalContextFact<ImmutableArray<int>> EquippedWeaponTypeIds
    { get; }

    public TacticalContextFact<ImmutableArray<int>> UnlockedWeaponTypeIds
    { get; }

    public TacticalContextFact<ImmutableArray<int>> UsableCombatStyleIds
    { get; }

    public TacticalContextFact<ImmutableArray<CombatTrickCount>> TrickCounts
    { get; }

    public TacticalContextFact<int> Distance { get; }

    public TacticalContextFact<int> Stance { get; }

    public TacticalContextFact<int> Breath { get; }

    public TacticalContextFact<ImmutableArray<CombatResourceAmount>> Resources
    { get; }

    public TacticalContextFact<int> ActiveDefenseSkillId { get; }

    public TacticalContextFact<int> ActiveAgilitySkillId { get; }

    public TacticalContextFact<TacticalInnerPowerContext> InnerPower { get; }

    public TacticalContextFact<SlotBudgetSet> SlotBudgets { get; }

    public TacticalContextFact<GenericSlotAllocation> UniversalSlotAllocation
    { get; }

    public TacticalContextFact<ImmutableArray<LegendaryBookCostSlot>>
        LegendaryCostSlots
    { get; }

    public TacticalContextFact<ImmutableArray<LegendaryBookCostAssignment>>
        LegendaryCostAssignments
    { get; }

    public TacticalContextFact<ImmutableArray<int>> EquippedSkillIds { get; }

    internal string SemanticKey => TacticalExecutionContextKeys.Facts(
        EquippedWeaponTypeIds,
        UnlockedWeaponTypeIds,
        UsableCombatStyleIds,
        TrickCounts,
        Distance,
        Stance,
        Breath,
        Resources,
        ActiveDefenseSkillId,
        ActiveAgilitySkillId,
        InnerPower,
        SlotBudgets,
        UniversalSlotAllocation,
        LegendaryCostSlots,
        LegendaryCostAssignments,
        EquippedSkillIds);
}

public sealed class TacticalExecutionContext
{
    internal TacticalExecutionContext(
        string sourceRevisionFingerprint,
        string observationRevisionFingerprint,
        TacticalContextFact<string> gameDataVersion,
        string ruleSetFingerprint,
        TacticalRuleSetResolutionStatus ruleResolutionStatus,
        IEnumerable<TacticalResolvedRuleState> resolvedRules,
        CurrentTacticalExecutionFacts current,
        ProposedTacticalExecutionFacts proposed)
    {
        SourceRevisionFingerprint = Fingerprint(
            sourceRevisionFingerprint,
            nameof(sourceRevisionFingerprint));
        ObservationRevisionFingerprint = Fingerprint(
            observationRevisionFingerprint,
            nameof(observationRevisionFingerprint));
        GameDataVersion = gameDataVersion
            ?? throw new ArgumentNullException(nameof(gameDataVersion));
        RuleSetFingerprint = Fingerprint(
            ruleSetFingerprint,
            nameof(ruleSetFingerprint));
        RuleResolutionStatus = TacticalCombatText.Defined(
            ruleResolutionStatus,
            nameof(ruleResolutionStatus));
        ResolvedRules = TacticalCombatText.CopyUnique(
            resolvedRules,
            item => item.StableKey,
            "resolved tactical rule",
            nameof(resolvedRules));
        Current = current ?? throw new ArgumentNullException(nameof(current));
        Proposed = proposed ?? throw new ArgumentNullException(nameof(proposed));
        if (RuleResolutionStatus
                == TacticalRuleSetResolutionStatus.UnsupportedGameDataVersion
            && !ResolvedRules.IsEmpty)
        {
            throw new ArgumentException(
                "An unsupported rule version cannot expose resolved rules.",
                nameof(resolvedRules));
        }

        SemanticFingerprint = CreateFingerprint();
    }

    public string SourceRevisionFingerprint { get; }

    public string ObservationRevisionFingerprint { get; }

    public TacticalContextFact<string> GameDataVersion { get; }

    public string RuleSetFingerprint { get; }

    public TacticalRuleSetResolutionStatus RuleResolutionStatus { get; }

    public bool HasCompatibleRules =>
        RuleResolutionStatus == TacticalRuleSetResolutionStatus.Resolved;

    public ImmutableArray<TacticalResolvedRuleState> ResolvedRules { get; }

    public CurrentTacticalExecutionFacts Current { get; }

    public ProposedTacticalExecutionFacts Proposed { get; }

    public string SemanticFingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("TACTICAL_EXECUTION_CONTEXT_V2\n")
            .Append(SourceRevisionFingerprint).Append('\n')
            .Append(ObservationRevisionFingerprint).Append('\n')
            .Append(RuleSetFingerprint).Append('\n')
            .Append(TacticalCombatText.EnumKey(RuleResolutionStatus))
            .Append('\n')
            .Append("GAMEDATA|")
            .Append(GameDataVersion.SemanticKey(
                GameDataVersion.IsAvailable
                    ? GameDataVersion.Value
                    : "NONE"))
            .Append('\n')
            .Append("CURRENT|").Append(Current.SemanticKey).Append('\n')
            .Append("PROPOSED|").Append(Proposed.SemanticKey).Append('\n');
        foreach (var rule in ResolvedRules)
        {
            canonical.Append("RULE|").Append(rule.SemanticKey).Append('\n');
        }

        return TacticalCombatText.Fingerprint(canonical.ToString());
    }

    private static string Fingerprint(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length != 64
            || !value.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "A tactical context fingerprint must be 64 hexadecimal characters.",
                parameterName);
        }

        return value.ToUpperInvariant();
    }
}

internal static class TacticalExecutionContextKeys
{
    internal static string Facts(
        TacticalContextFact<ImmutableArray<int>> equippedWeaponTypeIds,
        TacticalContextFact<ImmutableArray<int>> unlockedWeaponTypeIds,
        TacticalContextFact<ImmutableArray<int>> usableCombatStyleIds,
        TacticalContextFact<ImmutableArray<CombatTrickCount>> trickCounts,
        TacticalContextFact<int> distance,
        TacticalContextFact<int> stance,
        TacticalContextFact<int> breath,
        TacticalContextFact<ImmutableArray<CombatResourceAmount>> resources,
        TacticalContextFact<int> activeDefenseSkillId,
        TacticalContextFact<int> activeAgilitySkillId,
        TacticalContextFact<TacticalInnerPowerContext> innerPower,
        TacticalContextFact<SlotBudgetSet> slotBudgets,
        TacticalContextFact<GenericSlotAllocation> universalSlotAllocation,
        TacticalContextFact<ImmutableArray<LegendaryBookCostSlot>>
            legendaryCostSlots,
        TacticalContextFact<ImmutableArray<LegendaryBookCostAssignment>>
            legendaryCostAssignments,
        TacticalContextFact<ImmutableArray<int>> equippedSkillIds) =>
        string.Join('\n',
            $"EQUIPPED_WEAPONS|{Set(equippedWeaponTypeIds)}",
            $"UNLOCKED_WEAPONS|{Set(unlockedWeaponTypeIds)}",
            $"COMBAT_STYLES|{Set(usableCombatStyleIds)}",
            $"TRICK_COUNTS|{Tricks(trickCounts)}",
            $"DISTANCE|{Number(distance)}",
            $"STANCE|{Number(stance)}",
            $"BREATH|{Number(breath)}",
            $"RESOURCES|{Resources(resources)}",
            $"ACTIVE_DEFENSE|{Number(activeDefenseSkillId)}",
            $"ACTIVE_AGILITY|{Number(activeAgilitySkillId)}",
            $"INNER_POWER|{InnerPower(innerPower)}",
            $"SLOT_BUDGETS|{Budgets(slotBudgets)}",
            $"UNIVERSAL_SLOTS|{Generic(universalSlotAllocation)}",
            $"LEGENDARY_SLOTS|{LegendarySlots(legendaryCostSlots)}",
            $"LEGENDARY_ASSIGNMENTS|{LegendaryAssignments(legendaryCostAssignments)}",
            $"EQUIPPED_SKILLS|{Set(equippedSkillIds)}");

    private static string Set(
        TacticalContextFact<ImmutableArray<int>> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? string.Join(',', fact.Value)
            : "NONE");

    private static string Number(TacticalContextFact<int> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? fact.Value.ToString(CultureInfo.InvariantCulture)
            : "NONE");

    private static string Tricks(
        TacticalContextFact<ImmutableArray<CombatTrickCount>> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? string.Join("||", fact.Value.Select(item =>
                $"{item.TrickTypeId}:{item.Count}"))
            : "NONE");

    private static string Resources(
        TacticalContextFact<ImmutableArray<CombatResourceAmount>> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? string.Join("||", fact.Value.Select(item => string.Join(':',
                item.Resource,
                item.Amount.IsAvailable
                    ? item.Amount.Value.ToString(CultureInfo.InvariantCulture)
                    : $"UNKNOWN:{item.Amount.UnavailableReason}")))
            : "NONE");

    private static string InnerPower(
        TacticalContextFact<TacticalInnerPowerContext> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? fact.Value.SemanticKey
            : "NONE");

    private static string Budgets(TacticalContextFact<SlotBudgetSet> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? string.Join("||", fact.Value.Values.Select(item => string.Join(':',
                item.Category,
                item.Used.IsAvailable
                    ? item.Used.Value.ToString(CultureInfo.InvariantCulture)
                    : $"UNKNOWN:{item.Used.UnavailableReason}",
                item.Capacity.ToString(CultureInfo.InvariantCulture))))
            : "NONE");

    private static string Generic(
        TacticalContextFact<GenericSlotAllocation> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? string.Join(':',
                fact.Value.TotalSlots,
                fact.Value.Attack,
                fact.Value.Agility,
                fact.Value.Defense,
                fact.Value.Assistance)
            : "NONE");

    private static string LegendarySlots(
        TacticalContextFact<ImmutableArray<LegendaryBookCostSlot>> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? string.Join("||", fact.Value.Select(item => string.Join(':',
                item.SlotReference,
                item.Rule.Effect,
                item.Rule.Source,
                item.Rule.EvidenceReference)))
            : "NONE");

    private static string LegendaryAssignments(
        TacticalContextFact<ImmutableArray<LegendaryBookCostAssignment>> fact) =>
        fact.SemanticKey(fact.IsAvailable
            ? string.Join("||", fact.Value.Select(item => string.Join(':',
                item.Slot.SlotReference,
                item.SkillId,
                item.Category,
                item.Origin,
                item.AssignmentEvidenceReference)))
            : "NONE");
}
