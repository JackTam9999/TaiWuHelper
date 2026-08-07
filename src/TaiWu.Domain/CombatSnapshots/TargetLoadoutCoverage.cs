namespace TaiWu.Domain.CombatSnapshots;

public sealed record TargetLoadoutCoverage
{
    private TargetLoadoutCoverage(
        TargetLoadoutCoverageKind kind,
        TargetLoadoutCompletenessEvidence? completenessEvidence)
    {
        Kind = kind;
        CompletenessEvidence = completenessEvidence;
    }

    public static TargetLoadoutCoverage PartialLoadout { get; } = new(
        TargetLoadoutCoverageKind.PartialLoadout,
        completenessEvidence: null);

    public TargetLoadoutCoverageKind Kind { get; }

    public TargetLoadoutCompletenessEvidence? CompletenessEvidence { get; }

    public bool CanEstablishAbsence =>
        Kind == TargetLoadoutCoverageKind.CompleteCurrentLoadout;

    public static TargetLoadoutCoverage CompleteCurrentLoadout(
        TargetLoadoutCompletenessEvidence completenessEvidence)
    {
        ArgumentNullException.ThrowIfNull(completenessEvidence);

        return new TargetLoadoutCoverage(
            TargetLoadoutCoverageKind.CompleteCurrentLoadout,
            completenessEvidence);
    }
}
