namespace TaiWu.Domain.CombatSnapshots;

public enum CombatLoadoutFeasibilityFailureCode
{
    CandidateMissing,
    CandidateNotSelected,
    CandidateRejected,
    RequirementContextMismatch,
    RequirementRejected,
    GenericSlotTotalMismatch,
    SlotBudgetInvalid,
    SlotUsageUnavailable
}
