namespace TaiWu.Domain.CombatSnapshots;

public sealed record InnerPowerStateSnapshot
{
    public InnerPowerStateSnapshot(
        int stateId,
        SnapshotValue<string> displayName,
        SnapshotValue<string> effectDescription,
        ElementAdjustmentSet maxPowerChanges,
        ElementAdjustmentSet requirementChanges,
        CombatSkillElement? backlashOnUseElement)
    {
        if (stateId < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stateId),
                stateId,
                "Inner-power state ID cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(effectDescription);
        ArgumentNullException.ThrowIfNull(maxPowerChanges);
        ArgumentNullException.ThrowIfNull(requirementChanges);
        if (backlashOnUseElement.HasValue
            && !Enum.IsDefined(backlashOnUseElement.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(backlashOnUseElement),
                backlashOnUseElement,
                "Unknown backlash element.");
        }

        StateId = stateId;
        DisplayName = displayName;
        EffectDescription = effectDescription;
        MaxPowerChanges = maxPowerChanges;
        RequirementChanges = requirementChanges;
        BacklashOnUseElement = backlashOnUseElement;
    }

    public int StateId { get; }

    public SnapshotValue<string> DisplayName { get; }

    public SnapshotValue<string> EffectDescription { get; }

    public ElementAdjustmentSet MaxPowerChanges { get; }

    public ElementAdjustmentSet RequirementChanges { get; }

    public CombatSkillElement? BacklashOnUseElement { get; }
}
