namespace TaiWu.Domain.TargetPlaybooks;

public enum TargetResponsePriority
{
    Critical,
    High,
    Normal,
    Fallback
}

public enum TargetCounterPlaybookGapKind
{
    NoVerifiedOption,
    InaccessibleVerifiedOption,
    IncompleteEvidence
}

public enum TargetCounterPlaybookResolutionStatus
{
    Resolved,
    UnsupportedGameDataVersion,
    ArchetypeNotFound
}
