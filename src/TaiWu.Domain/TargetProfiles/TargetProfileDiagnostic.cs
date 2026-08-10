using System.Collections.Immutable;

namespace TaiWu.Domain.TargetProfiles;

public sealed class TargetProfileDiagnostic
{
    public TargetProfileDiagnostic(
        string code,
        TargetProfileDiagnosticSeverity severity,
        TargetProfileFacetIdentity? facet,
        IEnumerable<string>? evidenceReferences = null)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                "Unknown target-profile diagnostic severity.");
        }

        var references = evidenceReferences is null
            ? []
            : evidenceReferences
                .Select(reference => TargetProfileText.Code(
                    reference,
                    nameof(evidenceReferences)))
                .ToImmutableArray();
        if (references.Distinct(StringComparer.Ordinal).Count()
            != references.Length)
        {
            throw new ArgumentException(
                "Diagnostic evidence references must be unique.",
                nameof(evidenceReferences));
        }

        Code = TargetProfileText.Code(code, nameof(code));
        Severity = severity;
        Facet = facet;
        EvidenceReferences = [.. references.Order(StringComparer.Ordinal)];
    }

    public string Code { get; }

    public TargetProfileDiagnosticSeverity Severity { get; }

    public TargetProfileFacetIdentity? Facet { get; }

    public ImmutableArray<string> EvidenceReferences { get; }

    internal string StableKey => TargetProfileText.Stable(
        ((int)Severity).ToString(
            System.Globalization.CultureInfo.InvariantCulture),
        Code,
        Facet?.StableKey ?? string.Empty,
        TargetProfileText.StableCollection(EvidenceReferences));
}
