using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatRequirementContext
{
    public CombatRequirementContext(
        IEnumerable<int> equippedWeaponTypeIds,
        IEnumerable<CombatTrickCount> trickCounts,
        SnapshotValue<int> distance,
        IEnumerable<CombatResourceAmount> resources,
        IEnumerable<int> unlockedWeaponTypeIds,
        IEnumerable<int> equippedSkillIds,
        int? activeDefenseSkillId = null,
        int? activeAgilitySkillId = null)
    {
        ArgumentNullException.ThrowIfNull(equippedWeaponTypeIds);
        ArgumentNullException.ThrowIfNull(trickCounts);
        ArgumentNullException.ThrowIfNull(distance);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(unlockedWeaponTypeIds);
        ArgumentNullException.ThrowIfNull(equippedSkillIds);

        if (distance.IsAvailable && distance.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distance),
                "An available combat distance cannot be negative.");
        }

        EquippedWeaponTypeIds = CopyIds(
            equippedWeaponTypeIds,
            nameof(equippedWeaponTypeIds));
        UnlockedWeaponTypeIds = CopyIds(
            unlockedWeaponTypeIds,
            nameof(unlockedWeaponTypeIds));
        EquippedSkillIds = CopyIds(
            equippedSkillIds,
            nameof(equippedSkillIds));
        TrickCounts = CopyTrickCounts(trickCounts);
        Resources = CopyResources(resources);
        Distance = distance;
        ActiveDefenseSkillId = ValidateActiveSkill(
            activeDefenseSkillId,
            nameof(activeDefenseSkillId));
        ActiveAgilitySkillId = ValidateActiveSkill(
            activeAgilitySkillId,
            nameof(activeAgilitySkillId));

        if (ActiveDefenseSkillId.HasValue
            && ActiveAgilitySkillId == ActiveDefenseSkillId)
        {
            throw new ArgumentException(
                "One skill cannot be the active defense and agility skill.");
        }
    }

    public ImmutableHashSet<int> EquippedWeaponTypeIds { get; }

    public ImmutableDictionary<int, int> TrickCounts { get; }

    public SnapshotValue<int> Distance { get; }

    public ImmutableDictionary<CombatResourceKind, SnapshotValue<int>>
        Resources
    {
        get;
    }

    public ImmutableHashSet<int> UnlockedWeaponTypeIds { get; }

    public ImmutableHashSet<int> EquippedSkillIds { get; }

    public int? ActiveDefenseSkillId { get; }

    public int? ActiveAgilitySkillId { get; }

    private static ImmutableHashSet<int> CopyIds(
        IEnumerable<int> source,
        string parameterName)
    {
        var values = source.ToArray();
        if (values.Any(value => value < 0))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "IDs cannot be negative.");
        }

        if (values.Distinct().Count() != values.Length)
        {
            throw new ArgumentException(
                "IDs cannot be duplicated.",
                parameterName);
        }

        return [.. values];
    }

    private static ImmutableDictionary<int, int> CopyTrickCounts(
        IEnumerable<CombatTrickCount> source)
    {
        var values = source.ToArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Trick counts cannot contain null entries.",
                nameof(source));
        }

        return values.ToImmutableDictionary(
            value => value.TrickTypeId,
            value => value.Count);
    }

    private static ImmutableDictionary<
        CombatResourceKind,
        SnapshotValue<int>> CopyResources(
            IEnumerable<CombatResourceAmount> source)
    {
        var values = source.ToArray();
        if (values.Any(value => value is null))
        {
            throw new ArgumentException(
                "Resources cannot contain null entries.",
                nameof(source));
        }

        return values.ToImmutableDictionary(
            value => value.Resource,
            value => value.Amount);
    }

    private int? ValidateActiveSkill(int? skillId, string parameterName)
    {
        if (!skillId.HasValue)
        {
            return null;
        }

        if (skillId.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                skillId,
                "Active skill ID cannot be negative.");
        }

        if (!EquippedSkillIds.Contains(skillId.Value))
        {
            throw new ArgumentException(
                $"Active skill {skillId.Value} must also be equipped.",
                parameterName);
        }

        return skillId;
    }
}
