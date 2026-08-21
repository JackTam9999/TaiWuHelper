namespace TaiWu.Domain.TacticalCombat;

public enum TacticalCandidateAdmissionState
{
    Admitted,
    RetainedOnly,
    Infeasible,
    UnknownContext,
    Unsupported
}

public enum TacticalCandidateSupportState
{
    VerifiedRole,
    IrrelevantSkill,
    UnsupportedEffect,
    UnsupportedGameDataVersion
}

public enum TacticalCandidateGateKind
{
    Ownership,
    Mastery,
    Direction,
    RawEffect,
    TacticalRole,
    RuleEvidence,
    ExecutionRequirements,
    InnerPowerBacklash,
    EffectiveCost,
    CategoryCapacity,
    UniversalSlots,
    CurrentRetention
}

public enum TacticalCandidateGateState
{
    Passed,
    Failed,
    Unknown,
    Conflicting,
    Unsupported,
    NotApplicable
}
