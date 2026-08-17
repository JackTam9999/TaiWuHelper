using System.Collections.Immutable;

namespace TaiWu.Domain.CompanionCandidates;

public sealed class CandidateConflictValue
{
    public CandidateConflictValue(
        CandidateFactValue value,
        CandidateFactProvenance provenance,
        IEnumerable<CandidateEvidenceReference> evidence)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        Evidence = CandidateProfileCollections.CopyEvidence(evidence, nameof(evidence));
    }

    public CandidateFactValue Value { get; }

    public CandidateFactProvenance Provenance { get; }

    public ImmutableArray<CandidateEvidenceReference> Evidence { get; }

    internal string StableKey => $"{Value.StableKey}|{Provenance.StableKey}|{string.Join(';', Evidence.Select(item => item.StableKey))}";
}

public sealed record CandidateConflictDecision
{
    public CandidateConflictDecision(
        CandidateConflictDecisionKind kind,
        string rationaleCode,
        CandidateFactProvenance? selectedProvenance = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown conflict decision.");
        }

        if ((kind == CandidateConflictDecisionKind.SelectedBySourcePrecedence)
            != (selectedProvenance is not null))
        {
            throw new ArgumentException(
                "Only a source-precedence decision has selected provenance.",
                nameof(selectedProvenance));
        }

        Kind = kind;
        RationaleCode = CandidateProfileText.Stable(rationaleCode, nameof(rationaleCode));
        SelectedProvenance = selectedProvenance;
    }

    public CandidateConflictDecisionKind Kind { get; }

    public string RationaleCode { get; }

    public CandidateFactProvenance? SelectedProvenance { get; }

    internal string StableKey => $"{CandidateProfileText.EnumKey(Kind)}|{RationaleCode}|{SelectedProvenance?.StableKey ?? "NONE"}";
}

internal static class CandidateProfileCollections
{
    public static ImmutableArray<CandidateEvidenceReference> CopyEvidence(
        IEnumerable<CandidateEvidenceReference> evidence,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(evidence, parameterName);
        var copied = evidence.ToImmutableArray();
        if (copied.Any(item => item is null))
        {
            throw new ArgumentException("Evidence cannot contain null entries.", parameterName);
        }

        var duplicate = copied
            .GroupBy(item => item.StableKey, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException("Evidence cannot contain duplicate entries.", parameterName);
        }

        return [.. copied.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
    }
}
