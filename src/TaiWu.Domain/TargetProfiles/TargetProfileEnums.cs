namespace TaiWu.Domain.TargetProfiles;

public enum TargetProfileDimension
{
    AttackFamily,
    Pressure,
    Resilience,
    Control,
    Tempo
}

public enum TargetProfileEvidenceState
{
    Confirmed,
    Incomplete,
    Unsupported,
    Conflicting
}

public enum TargetProfileEvidenceSourceKind
{
    SavedEquippedMembership,
    InstalledConfiguration,
    CurrentScreenObservation,
    SavedBaseCharacter,
    VerifiedRule,
    SyntheticFixture,
    SavedLoadoutSource
}

public enum TargetProfileFacetValueKind
{
    Presence,
    Measurements
}

public enum TargetProfileDiagnosticSeverity
{
    Information,
    Warning,
    Error
}
