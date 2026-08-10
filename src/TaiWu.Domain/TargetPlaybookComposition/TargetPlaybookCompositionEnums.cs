namespace TaiWu.Domain.TargetPlaybookComposition;

public enum TargetPlaybookCompositionConflictKind
{
    ActiveRole,
    Requirement,
    Timing,
    Capacity
}

public enum TargetPlaybookAdjustmentAction
{
    Retained,
    Elevated,
    Reduced,
    Added,
    Replaced,
    Unresolved
}

public enum TargetPlaybookAdjustmentEvidenceKind
{
    ProfileFacet,
    Threat,
    Skill,
    Effect,
    Equipment,
    Observation,
    Gap,
    ArchetypeMatch
}

public enum TargetPlaybookAdjustmentEvidenceState
{
    Confirmed,
    Contrary,
    Incomplete
}

public enum TargetPlaybookResponseReferenceKind
{
    Goal,
    Option,
    Gap,
    Threat
}
