namespace TaiWu.Domain.CombatSnapshots;

public sealed record CombatSkillSnapshot
{
    public CombatSkillSnapshot(
        int skillId,
        SnapshotValue<string> displayName,
        SkillCategory category,
        SnapshotValue<int> gridCost,
        SnapshotValue<bool> mastered,
        SnapshotValue<PracticeDirection> direction,
        SkillSlotContribution slotContribution,
        SnapshotValue<int> directEffectId,
        SnapshotValue<int> reverseEffectId,
        SnapshotValue<BreakthroughDirectionAvailability>?
            breakthroughDirections = null,
        SnapshotValue<CombatSkillElement>? element = null,
        SnapshotValue<bool>? hasConfiguredOuterDamage = null,
        SnapshotValue<bool>? hasConfiguredPoisonApplication = null)
    {
        if (skillId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skillId),
                skillId,
                "Skill ID cannot be negative.");
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "Unknown skill category.");
        }

        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(gridCost);
        ArgumentNullException.ThrowIfNull(mastered);
        ArgumentNullException.ThrowIfNull(direction);
        ArgumentNullException.ThrowIfNull(slotContribution);
        ArgumentNullException.ThrowIfNull(directEffectId);
        ArgumentNullException.ThrowIfNull(reverseEffectId);
        if (element is { IsAvailable: true }
            && !Enum.IsDefined(element.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(element),
                "Unknown combat-skill element.");
        }

        if (gridCost.IsAvailable && gridCost.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gridCost),
                "An available grid cost must be greater than zero.");
        }

        if (direction.IsAvailable
            && !Enum.IsDefined(direction.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(direction),
                "Unknown practice direction.");
        }

        ValidateEffectId(directEffectId, nameof(directEffectId));
        ValidateEffectId(reverseEffectId, nameof(reverseEffectId));

        SkillId = skillId;
        DisplayName = displayName;
        Category = category;
        GridCost = gridCost;
        Mastered = mastered;
        Direction = direction;
        SlotContribution = slotContribution;
        DirectEffectId = directEffectId;
        ReverseEffectId = reverseEffectId;
        BreakthroughDirections = breakthroughDirections
            ?? SnapshotValue<BreakthroughDirectionAvailability>.Unavailable(
                "Breakthrough-direction availability was not captured.");
        Element = element
            ?? SnapshotValue<CombatSkillElement>.Unavailable(
                "Combat-skill element was not captured.");
        HasConfiguredOuterDamage = hasConfiguredOuterDamage
            ?? SnapshotValue<bool>.Unavailable(
                "Configured outer-damage presence was not captured.");
        HasConfiguredPoisonApplication = hasConfiguredPoisonApplication
            ?? SnapshotValue<bool>.Unavailable(
                "Configured poison-application presence was not captured.");
    }

    public int SkillId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public SkillCategory Category { get; }

    public SnapshotValue<int> GridCost { get; }

    public SnapshotValue<bool> Mastered { get; }

    public SnapshotValue<PracticeDirection> Direction { get; }

    public SkillSlotContribution SlotContribution { get; }

    public SnapshotValue<int> DirectEffectId { get; }

    public SnapshotValue<int> ReverseEffectId { get; }

    public SnapshotValue<BreakthroughDirectionAvailability>
        BreakthroughDirections
    { get; }

    public SnapshotValue<CombatSkillElement> Element { get; }

    public SnapshotValue<bool> HasConfiguredOuterDamage { get; }

    public SnapshotValue<bool> HasConfiguredPoisonApplication { get; }

    private static void ValidateEffectId(
        SnapshotValue<int> effectId,
        string parameterName)
    {
        if (effectId.IsAvailable && effectId.Value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "An available effect ID cannot be negative.");
        }
    }
}
