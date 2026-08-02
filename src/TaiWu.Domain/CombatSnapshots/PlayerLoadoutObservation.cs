using System.Collections.Immutable;

namespace TaiWu.Domain.CombatSnapshots;

public sealed record PlayerLoadoutObservation
{
    public PlayerLoadoutObservation(
        DateTimeOffset observedAt,
        string evidenceReference,
        CombatLoadoutSnapshot equippedSkills,
        GenericSlotAllocation genericSlotAllocation,
        SlotBudgetSet? displayedSlotBudgets = null,
        IEnumerable<LegendaryBookCostSlot>? legendaryBookCostSlots = null,
        IEnumerable<LegendaryBookCostAssignment>?
            legendaryBookCostAssignments = null)
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

        if ((legendaryBookCostSlots is null)
            != (legendaryBookCostAssignments is null))
        {
            throw new ArgumentException(
                "Legendary-book slots and assignments must be observed "
                + "together.");
        }

        if (legendaryBookCostSlots is not null)
        {
            LegendaryBookCostSlots = [.. legendaryBookCostSlots];
            LegendaryBookCostAssignments =
                [.. legendaryBookCostAssignments!];
            if (LegendaryBookCostSlots.Value.Any(slot => slot is null)
                || LegendaryBookCostAssignments.Value.Any(
                    assignment => assignment is null))
            {
                throw new ArgumentException(
                    "Observed legendary-book values cannot contain nulls.");
            }

            if (LegendaryBookCostAssignments.Value.Any(
                    assignment => assignment.Origin
                        != LegendaryBookAssignmentOrigin
                            .CurrentScreenObservation))
            {
                throw new ArgumentException(
                    "Observed legendary-book assignments must identify "
                    + "current-screen observation as their origin.",
                    nameof(legendaryBookCostAssignments));
            }
        }
    }

    public DateTimeOffset ObservedAtUtc { get; }

    public string EvidenceReference { get; }

    public CombatLoadoutSnapshot EquippedSkills { get; }

    public GenericSlotAllocation GenericSlotAllocation { get; }

    public SlotBudgetSet? DisplayedSlotBudgets { get; }

    public ImmutableArray<LegendaryBookCostSlot>?
        LegendaryBookCostSlots
    {
        get;
    }

    public ImmutableArray<LegendaryBookCostAssignment>?
        LegendaryBookCostAssignments
    {
        get;
    }
}
