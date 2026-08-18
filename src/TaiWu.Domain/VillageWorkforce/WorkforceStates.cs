namespace TaiWu.Domain.VillageWorkforce;

public enum WorkforceObjectiveKind
{
    ShopManagerBaseLifeSkillQualification = 0
}

public enum WorkforceTargetKind
{
    ShopManagerSlot = 0
}

public enum WorkforceCandidateUniverseKind
{
    TaiwuWorkCandidates = 0
}

public enum WorkforceWorkerState
{
    Eligible = 0,
    CurrentOnly = 1,
    Ineligible = 2,
    Incomplete = 3,
    Unsupported = 4,
    Conflicting = 5
}

public enum WorkforceEvidenceState
{
    Confirmed = 0,
    Incomplete = 1,
    Unsupported = 2,
    Stale = 3,
    Conflicting = 4
}

public enum WorkforceEvidenceSourceKind
{
    ConfiguredSave = 0,
    InstalledGameData = 1,
    DerivedRule = 2
}

public enum WorkforceFactKind
{
    CandidateUniverseMembership = 0,
    CurrentAssignmentMembership = 1,
    BaseLifeSkillQualification = 2
}

public enum WorkforceFactValueKind
{
    Boolean = 0,
    Int16 = 1,
    Int32 = 2
}

public enum WorkforceAssignmentOrigin
{
    CurrentSave = 0,
    ProposedHelper = 1
}

public enum WorkforceRequirementKind
{
    SupportedSourceVersion = 0,
    SupportedShopTarget = 1,
    AlternativeWorkCandidate = 2,
    CharacterProfileAvailable = 3,
    QualificationProvenanceMatch = 4
}

public enum WorkforceRequirementOutcome
{
    Passed = 0,
    Failed = 1,
    Incomplete = 2,
    Unsupported = 3,
    Conflicting = 4
}

public enum WorkforceEvidenceRequirementKind
{
    SourceVersions = 0,
    SupportedTarget = 1,
    ConfirmedFact = 2,
    MatchingProvenance = 3
}

public enum WorkforceComponentKind
{
    RequiredBaseLifeSkillQualification = 0
}

public enum WorkforceNormalizationKind
{
    Identity = 0
}

public enum WorkforceScoreDirection
{
    HigherIsBetter = 0
}

public enum WorkforceUnit
{
    BaseQualificationPoint = 0
}

public enum WorkforceEvaluationState
{
    Ranked = 0,
    Tied = 1,
    CurrentOnly = 2,
    Ineligible = 3,
    Incomplete = 4,
    Unsupported = 5,
    Conflicting = 6
}

public enum WorkforceComparisonOutcome
{
    Higher = 0,
    Lower = 1,
    Equal = 2,
    Unavailable = 3,
    Conflicting = 4
}

public enum WorkforceDiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}

public enum WorkforceRuleResolutionStatus
{
    Resolved = 0,
    UnsupportedObjectiveVersion = 1,
    UnsupportedGameDataVersion = 2,
    UnsupportedMappingVersion = 3,
    UnsupportedCandidateUniverseVersion = 4,
    UnsupportedFingerprintSchemaVersion = 5,
    UnsupportedTargetKind = 6
}
