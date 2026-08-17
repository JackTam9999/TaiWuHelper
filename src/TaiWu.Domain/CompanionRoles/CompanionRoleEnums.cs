namespace TaiWu.Domain.CompanionRoles;

public enum CompanionRoleRequirementKind
{
    CandidateUniverseEligible = 0,
    SourceVersionsSupported = 1,
    DisciplineSupported = 2,
    RequiredFactConfirmed = 3,
    FactProvenanceCompatible = 4
}

public enum CompanionRoleGateOutcome
{
    Passed = 0,
    Failed = 1,
    Incomplete = 2,
    Unsupported = 3,
    Conflicting = 4
}

public enum CompanionRoleEvaluationState
{
    Rankable = 0,
    Ineligible = 1,
    Incomplete = 2,
    Unsupported = 3,
    Conflicting = 4
}

public enum CompanionRoleScoreDirection
{
    HigherIsBetter = 0,
    LowerIsBetter = 1
}

public enum CompanionRoleNormalizationKind
{
    Identity = 0
}

public enum CompanionRoleMissingEvidenceBehavior
{
    EvaluationIncomplete = 0,
    EvaluationUnsupported = 1
}

public enum CompanionRoleTiePolicy
{
    ExactTotalRemainsTie = 0
}

public enum CompanionRoleMeritComparison
{
    FirstPreferred = 0,
    SecondPreferred = 1,
    ExactTie = 2,
    NotComparable = 3
}

public enum CompanionRoleDefinitionResolutionState
{
    Supported = 0,
    UnknownIdentity = 1,
    UnsupportedVersion = 2
}
