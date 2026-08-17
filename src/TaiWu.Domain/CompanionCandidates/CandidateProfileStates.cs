namespace TaiWu.Domain.CompanionCandidates;

public enum CandidateUniverseState
{
    Eligible = 0,
    Ineligible = 1,
    Incomplete = 2,
    Unsupported = 3,
    Conflicting = 4
}

public enum CandidateEvidenceState
{
    Confirmed = 0,
    Incomplete = 1,
    Unsupported = 2,
    Stale = 3,
    Conflicting = 4
}

public enum CandidateEvidenceSourceKind
{
    ConfiguredSave = 0,
    InstalledGameData = 1,
    HelperCatalog = 2,
    DerivedRule = 3
}

public enum CandidateFactValueKind
{
    Boolean = 0,
    Int16 = 1,
    Int32 = 2,
    Int32Set = 3
}

public enum CandidateDisciplineDomain
{
    Martial = 0,
    LifeSkill = 1,
    Capability = 2
}

public enum CandidateMainAttribute
{
    Strength = 0,
    Dexterity = 1,
    Concentration = 2,
    Vitality = 3,
    Energy = 4,
    Intelligence = 5
}

public enum CandidateProfileField
{
    RosterMembership = 0,
    DomainGroupMembership = 1,
    CharacterGroupMembership = 2,
    LivingState = 3,
    CurrentAge = 4,
    CurrentLocationArea = 5,
    CurrentLocationBlock = 6,
    FeatureIdentities = 7,
    BaseMartialQualification = 8,
    CurrentMartialQualification = 9,
    CurrentMartialAttainment = 10,
    LearnedMartialSkillIdentities = 11,
    EquippedMartialSkillIdentities = 12,
    BaseLifeSkillQualification = 13,
    CurrentLifeSkillQualification = 14,
    CurrentLifeSkillAttainment = 15,
    LearnedLifeSkillIdentities = 16,
    BaseMainAttribute = 17,
    CapabilityBreadthIndex = 18
}

public enum CandidateConflictDecisionKind
{
    Unresolved = 0,
    SelectedBySourcePrecedence = 1,
    RejectedAllCandidates = 2
}

public enum CandidateProfileDiagnosticSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2
}
