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

public enum CompanionRoleCandidateRankingState
{
    Ranked = 0,
    Tied = 1,
    Ineligible = 2,
    Incomplete = 3,
    Unsupported = 4,
    Conflicting = 5
}

public enum CompanionRoleExplanationKind
{
    StrongestContribution = 0,
    MaterialLimitation = 1,
    ExactTie = 2,
    Exclusion = 3
}

public enum CompanionRoleShortlistDiagnosticSeverity
{
    Information = 0,
    Warning = 1
}

public enum CompanionRoleShortlistFilter
{
    All = 0,
    Ranked = 1,
    NeedsReview = 2,
    Ineligible = 3
}

public enum CompanionRoleComparisonEvidenceState
{
    Confirmed = 0,
    Missing = 1,
    Incomplete = 2,
    Unsupported = 3,
    Stale = 4,
    Conflicting = 5
}

public enum CompanionRoleComparisonOutcome
{
    FirstAdvantage = 0,
    SecondAdvantage = 1,
    Equal = 2,
    Unavailable = 3,
    Conflicting = 4,
    Tradeoff = 5
}

public enum CompanionRoleDefinitionResolutionState
{
    Supported = 0,
    UnknownIdentity = 1,
    UnsupportedVersion = 2
}
