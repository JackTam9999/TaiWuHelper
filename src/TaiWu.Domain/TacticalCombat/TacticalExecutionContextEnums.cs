namespace TaiWu.Domain.TacticalCombat;

public enum TacticalContextFactState
{
    Available,
    Unknown,
    Unsupported,
    Conflicting
}

public enum TacticalContextOrigin
{
    SaveSnapshot,
    CurrentScreenObservation,
    ProposedPlan,
    InstalledConfiguration,
    VerifiedRule,
    ManualConfirmation,
    RuntimeUnavailable
}

public enum TacticalContextAvailability
{
    FixedForRequest,
    PreCombatConfigurable,
    ManuallyObservable,
    RuntimeUnavailable
}

public enum TacticalResolvedRuleKind
{
    Transition,
    SkillRole
}
