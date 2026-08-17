using System.Collections.Immutable;

namespace TaiWu.Domain.CompanionCandidates;

public sealed class CandidateProfileDiagnostic
{
    public CandidateProfileDiagnostic(
        string code,
        CandidateProfileDiagnosticSeverity severity,
        string detail,
        CandidateProfileFieldIdentity? field,
        IEnumerable<CandidateEvidenceReference> evidence)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown diagnostic severity.");
        }

        Code = CandidateProfileText.Stable(code, nameof(code));
        Severity = severity;
        Detail = CandidateProfileText.Detail(detail, nameof(detail));
        Field = field;
        Evidence = CandidateProfileCollections.CopyEvidence(evidence, nameof(evidence));
    }

    public string Code { get; }

    public CandidateProfileDiagnosticSeverity Severity { get; }

    public string Detail { get; }

    public CandidateProfileFieldIdentity? Field { get; }

    public ImmutableArray<CandidateEvidenceReference> Evidence { get; }

    internal string StableKey => string.Join('|',
        Code,
        CandidateProfileText.EnumKey(Severity),
        Field?.StableKey ?? "NONE",
        string.Join("||", Evidence.Select(item => item.StableKey)));
}
