using System.Collections.Immutable;
using System.Globalization;
using TaiWu.Domain.CombatSnapshots;

namespace TaiWu.Domain.TacticalCombat;

public sealed class TacticalExecutionObservation
{
    public TacticalExecutionObservation(
        string evidenceReference,
        bool confirmsNewerThanSave,
        IEnumerable<int>? equippedWeaponTypeIds = null,
        IEnumerable<int>? unlockedWeaponTypeIds = null,
        IEnumerable<CombatTrickCount>? trickCounts = null,
        IEnumerable<int>? usableCombatStyleIds = null,
        int? distance = null,
        IEnumerable<CombatResourceAmount>? resources = null,
        int? activeDefenseSkillId = null,
        int? activeAgilitySkillId = null)
    {
        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "A tactical execution observation requires evidence.",
                nameof(evidenceReference));
        }

        EvidenceReference = evidenceReference.Trim();
        ConfirmsNewerThanSave = confirmsNewerThanSave;
        EquippedWeaponTypeIds = CopyIds(
            equippedWeaponTypeIds,
            nameof(equippedWeaponTypeIds));
        UnlockedWeaponTypeIds = CopyIds(
            unlockedWeaponTypeIds,
            nameof(unlockedWeaponTypeIds));
        UsableCombatStyleIds = CopyIds(
            usableCombatStyleIds,
            nameof(usableCombatStyleIds));
        TrickCounts = CopyTricks(trickCounts);
        Resources = CopyResources(resources);
        Distance = ValidateNonNegative(distance, nameof(distance));
        ActiveDefenseSkillId = ValidateNonNegative(
            activeDefenseSkillId,
            nameof(activeDefenseSkillId));
        ActiveAgilitySkillId = ValidateNonNegative(
            activeAgilitySkillId,
            nameof(activeAgilitySkillId));
        if (ActiveDefenseSkillId.HasValue
            && ActiveDefenseSkillId == ActiveAgilitySkillId)
        {
            throw new ArgumentException(
                "One observed skill cannot be active defense and agility.");
        }
    }

    public string EvidenceReference { get; }

    public bool ConfirmsNewerThanSave { get; }

    public ImmutableArray<int>? EquippedWeaponTypeIds { get; }

    public ImmutableArray<int>? UnlockedWeaponTypeIds { get; }

    public ImmutableArray<CombatTrickCount>? TrickCounts { get; }

    public ImmutableArray<int>? UsableCombatStyleIds { get; }

    public int? Distance { get; }

    public ImmutableArray<CombatResourceAmount>? Resources { get; }

    public int? ActiveDefenseSkillId { get; }

    public int? ActiveAgilitySkillId { get; }

    internal string SemanticKey => string.Join('|',
        EvidenceReference,
        ConfirmsNewerThanSave ? "CONFIRMED_NEWER" :
            "TIMESTAMP_REQUIRED",
        Ids(EquippedWeaponTypeIds),
        Ids(UnlockedWeaponTypeIds),
        Tricks(TrickCounts),
        Ids(UsableCombatStyleIds),
        Number(Distance),
        ResourceValues(Resources),
        Number(ActiveDefenseSkillId),
        Number(ActiveAgilitySkillId));

    private static ImmutableArray<int>? CopyIds(
        IEnumerable<int>? values,
        string parameterName)
    {
        if (values is null)
        {
            return null;
        }

        var copied = values.ToImmutableArray();
        if (copied.Any(item => item < 0)
            || copied.Distinct().Count() != copied.Length)
        {
            throw new ArgumentException(
                "Observed IDs must be non-negative and unique.",
                parameterName);
        }

        return [.. copied.Order()];
    }

    private static ImmutableArray<CombatTrickCount>? CopyTricks(
        IEnumerable<CombatTrickCount>? values)
    {
        if (values is null)
        {
            return null;
        }

        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.Select(item => item.TrickTypeId).Distinct().Count()
                != copied.Length)
        {
            throw new ArgumentException(
                "Observed trick counts must be non-null and unique by type.",
                nameof(values));
        }

        return [.. copied.OrderBy(item => item.TrickTypeId)];
    }

    private static ImmutableArray<CombatResourceAmount>? CopyResources(
        IEnumerable<CombatResourceAmount>? values)
    {
        if (values is null)
        {
            return null;
        }

        var copied = values.ToImmutableArray();
        if (copied.Any(item => item is null)
            || copied.Select(item => item.Resource).Distinct().Count()
                != copied.Length)
        {
            throw new ArgumentException(
                "Observed resources must be non-null and unique by kind.",
                nameof(values));
        }

        return [.. copied.OrderBy(item => item.Resource)];
    }

    private static int? ValidateNonNegative(int? value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }

    private static string Ids(ImmutableArray<int>? values) => values.HasValue
        ? string.Join(',', values.Value)
        : "UNOBSERVED";

    private static string Tricks(
        ImmutableArray<CombatTrickCount>? values) => values.HasValue
        ? string.Join(',', values.Value.Select(item =>
            $"{item.TrickTypeId}:{item.Count}"))
        : "UNOBSERVED";

    private static string ResourceValues(
        ImmutableArray<CombatResourceAmount>? values) => values.HasValue
        ? string.Join(',', values.Value.Select(item =>
            $"{item.Resource}:{(item.Amount.IsAvailable ? item.Amount.Value
                .ToString(CultureInfo.InvariantCulture) : "UNKNOWN")}"))
        : "UNOBSERVED";

    private static string Number(int? value) => value?.ToString(
        CultureInfo.InvariantCulture) ?? "UNOBSERVED";
}
