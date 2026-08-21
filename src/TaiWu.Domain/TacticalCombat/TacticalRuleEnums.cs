namespace TaiWu.Domain.TacticalCombat;

public enum TacticalRulePurpose
{
    DirectMagicMindPressure,
    DistractionMarkAccumulation,
    MindResonanceCountdown,
    MindResonanceCascade,
    DefeatMarkReset,
    CastSuppression,
    DirectPracticeSelfLock,
    DirectPracticeLockRecovery,
    MarkDurationReduction,
    ResonanceDurationReduction,
    HindranceMarkRemoval,
    EnemyAttackPowerReduction,
    ResetResourcePressure,
    ConditionalMarkTransfer,
    DamageChannelChoice,
    FinishWindowSupport,
    WeaponAttackParry,
    CastSpeedControl,
    HitChanceControl,
    MovementCounterattack,
    CriticalInjuryProtection,
    MindMarkConversion,
    DirectDamageReduction,
    CounterStancePressure,
    MindDefenseIncrease,
    CloseRangeAvoidance,
    MobilitySustain
}

public enum TacticalRuleEvidenceScope
{
    BroadRule,
    ExactTarget
}

public enum TacticalRuleEvidenceDisposition
{
    Confirmed,
    Contrary,
    Absent,
    Incomplete,
    Conflicting
}

public enum TacticalRuleApplicability
{
    Applicable,
    Contrary,
    Incomplete,
    Conflicting
}

public enum TacticalRuleSetResolutionStatus
{
    Resolved,
    UnsupportedGameDataVersion
}

public enum TacticalRoleUseKind
{
    EquippedPassive,
    ActiveAttack,
    ActiveDefense,
    ActiveAgility,
    PersistentState,
    SwitchOnlyBackup,
    OpeningUse
}
