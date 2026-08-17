namespace TaiWu.Domain.CompanionCandidates;

public sealed record CandidateFactProvenance
{
    public CandidateFactProvenance(
        CandidateEvidenceSourceKind sourceKind,
        string sourceIdentity,
        string sourceVersion,
        string revisionIdentity)
    {
        if (!Enum.IsDefined(sourceKind))
        {
            throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Unknown candidate evidence source.");
        }

        SourceKind = sourceKind;
        SourceIdentity = CandidateProfileText.Stable(sourceIdentity, nameof(sourceIdentity));
        SourceVersion = CandidateProfileText.Stable(sourceVersion, nameof(sourceVersion));
        RevisionIdentity = CandidateProfileText.Stable(revisionIdentity, nameof(revisionIdentity));
    }

    public CandidateEvidenceSourceKind SourceKind { get; }

    public string SourceIdentity { get; }

    public string SourceVersion { get; }

    public string RevisionIdentity { get; }

    internal string StableKey => string.Join('|',
        CandidateProfileText.EnumKey(SourceKind),
        SourceIdentity,
        SourceVersion,
        RevisionIdentity);
}

public sealed record CandidateEvidenceReference
{
    public CandidateEvidenceReference(
        string reference,
        CandidateFactProvenance provenance)
    {
        Reference = CandidateProfileText.Stable(reference, nameof(reference));
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
    }

    public string Reference { get; }

    public CandidateFactProvenance Provenance { get; }

    internal string StableKey => $"{Reference}|{Provenance.StableKey}";
}
