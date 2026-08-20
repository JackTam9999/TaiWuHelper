namespace TaiWu.Domain.TacticalCombat;

public enum TacticalEvidenceState
{
    Available,
    Incomplete,
    Unsupported,
    Conflicting
}

public enum TacticalEvidenceSourceKind
{
    SaveSnapshot,
    InstalledConfiguration,
    ConfirmedObservation,
    VerifiedRule,
    PlayerConfirmation
}

public enum TacticalFactKind
{
    TargetSkillPhase,
    Mark,
    Resonance,
    Resource,
    TemporaryLockout,
    PlayerReadiness,
    ActiveRole,
    Distance,
    Equipment,
    Capacity,
    Other
}

public enum TacticalFactValueKind
{
    Boolean,
    Integer,
    Code
}

public enum TacticalRequirementOperator
{
    Present,
    Absent,
    Equal,
    AtLeast,
    AtMost
}

public enum TacticalRequirementOutcome
{
    Satisfied,
    Unsatisfied,
    Unknown,
    Unsupported,
    Conflicting
}

public enum TacticalTransitionTiming
{
    BeforeCombat,
    CombatStart,
    BeforeFirstUse,
    DuringCast,
    AfterCast,
    OnObservedState,
    AfterManualAction
}

public enum TacticalRoleKind
{
    Suppression,
    Interrupt,
    Mitigation,
    Recovery,
    DamageChannel,
    Finish,
    Fallback
}

public enum TacticalCandidateDecision
{
    Admitted,
    Rejected,
    Unsupported,
    Irrelevant,
    Dominated
}

public enum TacticalPlanStage
{
    Preparation = 1,
    Opening = 2,
    TargetStateResponse = 3,
    Recovery = 4,
    Finish = 5,
    Fallback = 6
}

public enum TacticalPlanStageState
{
    Supported,
    Omitted,
    Unsupported
}

public enum TacticalStepBranchKind
{
    Primary,
    Conditional,
    Fallback
}

public enum TacticalBranchOutcome
{
    Continue,
    Fallback,
    Unresolved,
    Stop
}

public enum TacticalSearchTerminator
{
    None,
    OptionLimit,
    ExplorationLimit,
    TimeLimit,
    ResultLimit,
    Cancelled
}
