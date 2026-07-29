namespace TaiWu.Domain.CombatSnapshots;

public sealed record PlayerLoadoutObservation
{
    public PlayerLoadoutObservation(
        DateTimeOffset observedAt,
        string evidenceReference,
        CombatLoadoutSnapshot equippedSkills,
        GenericSlotAllocation genericSlotAllocation,
        SlotBudgetSet? displayedSlotBudgets = null)
    {
        if (string.IsNullOrWhiteSpace(evidenceReference))
        {
            throw new ArgumentException(
                "A current-screen observation requires an evidence reference.",
                nameof(evidenceReference));
        }

        ObservedAtUtc = observedAt.ToUniversalTime();
        EvidenceReference = evidenceReference.Trim();
        EquippedSkills = equippedSkills
            ?? throw new ArgumentNullException(nameof(equippedSkills));
        GenericSlotAllocation = genericSlotAllocation
            ?? throw new ArgumentNullException(nameof(genericSlotAllocation));
        DisplayedSlotBudgets = displayedSlotBudgets;
    }

    public DateTimeOffset ObservedAtUtc { get; }

    public string EvidenceReference { get; }

    public CombatLoadoutSnapshot EquippedSkills { get; }

    public GenericSlotAllocation GenericSlotAllocation { get; }

    public SlotBudgetSet? DisplayedSlotBudgets { get; }
}
