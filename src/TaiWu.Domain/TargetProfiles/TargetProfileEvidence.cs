namespace TaiWu.Domain.TargetProfiles;

public sealed record TargetProfileEvidence
{
    public TargetProfileEvidence(
        string reference,
        TargetProfileEvidenceSourceKind sourceKind,
        string sourceIdentity,
        TargetProfileVersion sourceVersion)
    {
        if (!Enum.IsDefined(sourceKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceKind),
                sourceKind,
                "Unknown target-profile evidence source kind.");
        }

        Reference = TargetProfileText.Code(reference, nameof(reference));
        SourceKind = sourceKind;
        SourceIdentity = TargetProfileText.Code(
            sourceIdentity,
            nameof(sourceIdentity));
        SourceVersion = sourceVersion
            ?? throw new ArgumentNullException(nameof(sourceVersion));
    }

    public string Reference { get; }

    public TargetProfileEvidenceSourceKind SourceKind { get; }

    public string SourceIdentity { get; }

    public TargetProfileVersion SourceVersion { get; }

    internal string StableKey => TargetProfileText.Stable(
        ((int)SourceKind).ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        SourceIdentity,
        SourceVersion.Value,
        Reference);
}
