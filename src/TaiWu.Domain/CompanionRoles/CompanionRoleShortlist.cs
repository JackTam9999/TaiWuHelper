using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public sealed class CompanionRoleExplanation
{
    internal CompanionRoleExplanation(
        CompanionRoleExplanationKind kind,
        string identity,
        IEnumerable<CompanionRoleScoreComponent> components,
        IEnumerable<CompanionRoleGateEvaluation> gates)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown explanation kind.");
        }

        Kind = kind;
        Identity = CompanionRoleText.Stable(identity, nameof(identity));
        ArgumentNullException.ThrowIfNull(components);
        var componentValues = components.ToImmutableArray();
        if (componentValues.Any(item => item is null)
            || componentValues.Distinct().Count() != componentValues.Length)
        {
            throw new ArgumentException("Explanation components must be unique and non-null.", nameof(components));
        }

        ArgumentNullException.ThrowIfNull(gates);
        var gateValues = gates.ToImmutableArray();
        if (gateValues.Any(item => item is null)
            || gateValues.Distinct().Count() != gateValues.Length)
        {
            throw new ArgumentException("Explanation gates must be unique and non-null.", nameof(gates));
        }

        if (componentValues.IsEmpty == gateValues.IsEmpty)
        {
            throw new ArgumentException(
                "An explanation must reference components or gates, but not both.",
                nameof(components));
        }

        Components = [.. componentValues.OrderBy(item => item.Dimension.Identity, StringComparer.Ordinal)];
        Gates = [.. gateValues.OrderBy(item => item.Requirement.Order)];
    }

    public CompanionRoleExplanationKind Kind { get; }

    public string Identity { get; }

    public ImmutableArray<CompanionRoleScoreComponent> Components { get; }

    public ImmutableArray<CompanionRoleGateEvaluation> Gates { get; }

    internal string StableKey => string.Join('|',
        CompanionRoleText.EnumKey(Kind),
        Identity,
        string.Join("||", Components.Select(item => item.StableKey)),
        string.Join("||", Gates.Select(item => item.StableKey)));
}

public sealed class CompanionRoleShortlistDiagnostic
{
    internal CompanionRoleShortlistDiagnostic(
        string identity,
        CompanionRoleShortlistDiagnosticSeverity severity)
    {
        Identity = CompanionRoleText.Stable(identity, nameof(identity));
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown shortlist diagnostic severity.");
        }

        Severity = severity;
    }

    public string Identity { get; }

    public CompanionRoleShortlistDiagnosticSeverity Severity { get; }

    internal string StableKey => $"{Identity}|{CompanionRoleText.EnumKey(Severity)}";
}

public sealed class CompanionRoleShortlistEntry
{
    internal CompanionRoleShortlistEntry(
        CompanionRoleCandidateRanking candidate,
        IEnumerable<CompanionRoleExplanation> explanations)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        ArgumentNullException.ThrowIfNull(explanations);
        var explanationValues = explanations.ToImmutableArray();
        if (explanationValues.Any(item => item is null)
            || explanationValues.GroupBy(item => item.StableKey, StringComparer.Ordinal)
                .Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Entry explanations must be unique and non-null.", nameof(explanations));
        }

        Explanations = [.. explanationValues
            .OrderBy(item => item.Kind)
            .ThenBy(item => item.Identity, StringComparer.Ordinal)];
        LocationEvidence = [.. candidate.Evaluation.Profile.Facts
            .Where(item => item.Identity.Field is CandidateProfileField.CurrentLocationArea
                or CandidateProfileField.CurrentLocationBlock)
            .OrderBy(item => item.Identity.StableKey, StringComparer.Ordinal)];
        AvailableLocationFacts = [.. LocationEvidence.Where(item =>
            IsCurrentConfirmedFact(item, candidate.Evaluation.Profile.SourceVersions))];
    }

    public CompanionRoleCandidateRanking Candidate { get; }

    public CompanionRoleEvaluation Evaluation => Candidate.Evaluation;

    public ImmutableArray<CompanionRoleExplanation> Explanations { get; }

    public ImmutableArray<CandidateProfileFact> LocationEvidence { get; }

    public ImmutableArray<CandidateProfileFact> AvailableLocationFacts { get; }

    public ImmutableArray<CandidateProfileDiagnostic> ProfileDiagnostics =>
        Evaluation.Profile.Diagnostics;

    internal string StableKey => string.Join('|',
        Candidate.StableKey,
        string.Join("||", Explanations.Select(item => item.StableKey)),
        string.Join("||", LocationEvidence.Select(item => item.StableKey)));

    private static bool IsCurrentConfirmedFact(
        CandidateProfileFact fact,
        CandidateProfileSourceVersions versions) =>
        fact.State == CandidateEvidenceState.Confirmed
        && fact.Provenance is { } provenance
        && provenance.SourceKind == CandidateEvidenceSourceKind.ConfiguredSave
        && string.Equals(
            provenance.RevisionIdentity,
            versions.SaveSha256,
            StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            provenance.SourceVersion,
            versions.ProfileMappingVersion,
            StringComparison.Ordinal);
}

public sealed record CompanionRoleShortlistCounts
{
    internal CompanionRoleShortlistCounts(
        int ranked,
        int tied,
        int ineligible,
        int incomplete,
        int unsupported,
        int conflicting)
    {
        if (ranked < 0 || tied < 0 || ineligible < 0
            || incomplete < 0 || unsupported < 0 || conflicting < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ranked), "Shortlist counts cannot be negative.");
        }

        Ranked = ranked;
        Tied = tied;
        Ineligible = ineligible;
        Incomplete = incomplete;
        Unsupported = unsupported;
        Conflicting = conflicting;
        Total = checked(ranked + tied + ineligible + incomplete + unsupported + conflicting);
    }

    public int Ranked { get; }

    public int Tied { get; }

    public int Ineligible { get; }

    public int Incomplete { get; }

    public int Unsupported { get; }

    public int Conflicting { get; }

    public int Total { get; }

    internal string StableKey => string.Join('|',
        Ranked,
        Tied,
        Ineligible,
        Incomplete,
        Unsupported,
        Conflicting,
        Total);
}

public sealed class CompanionRoleShortlist
{
    internal CompanionRoleShortlist(
        CompanionRoleRanking ranking,
        IEnumerable<CompanionRoleShortlistEntry> entries,
        CompanionRoleShortlistCounts counts,
        IEnumerable<CompanionRoleShortlistDiagnostic> diagnostics)
    {
        Ranking = ranking ?? throw new ArgumentNullException(nameof(ranking));
        Counts = counts ?? throw new ArgumentNullException(nameof(counts));
        ArgumentNullException.ThrowIfNull(entries);
        var values = entries.ToImmutableArray();
        if (values.Any(item => item is null)
            || values.Length != ranking.Candidates.Length
            || values.Where((item, index) => !ReferenceEquals(
                item.Candidate,
                ranking.Candidates[index])).Any())
        {
            throw new ArgumentException(
                "Shortlist entries must preserve every canonical ranking candidate exactly once.",
                nameof(entries));
        }

        if (counts.Total != values.Length
            || Count(values, CompanionRoleCandidateRankingState.Ranked) != counts.Ranked
            || Count(values, CompanionRoleCandidateRankingState.Tied) != counts.Tied
            || Count(values, CompanionRoleCandidateRankingState.Ineligible) != counts.Ineligible
            || Count(values, CompanionRoleCandidateRankingState.Incomplete) != counts.Incomplete
            || Count(values, CompanionRoleCandidateRankingState.Unsupported) != counts.Unsupported
            || Count(values, CompanionRoleCandidateRankingState.Conflicting) != counts.Conflicting)
        {
            throw new ArgumentException("Shortlist counts do not match its entries.", nameof(counts));
        }

        ArgumentNullException.ThrowIfNull(diagnostics);
        var diagnosticValues = diagnostics.ToImmutableArray();
        if (diagnosticValues.Any(item => item is null)
            || diagnosticValues.GroupBy(item => item.StableKey, StringComparer.Ordinal)
                .Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Shortlist diagnostics must be unique and non-null.", nameof(diagnostics));
        }

        Entries = values;
        RankedEntries = [.. values.Where(item => item.Candidate.IsRanked)];
        ExcludedEntries = [.. values.Where(item => !item.Candidate.IsRanked)];
        Diagnostics = [.. diagnosticValues.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
        Fingerprint = CreateFingerprint();
    }

    public CompanionRoleRanking Ranking { get; }

    public CompanionRoleDefinition Definition => Ranking.Definition;

    public CandidateDisciplineIdentity Discipline => Ranking.Discipline;

    public CandidateProfileSourceVersions? SourceVersions => Ranking.SourceVersions;

    public CompanionRoleShortlistCounts Counts { get; }

    public ImmutableArray<CompanionRoleShortlistEntry> Entries { get; }

    public ImmutableArray<CompanionRoleShortlistEntry> RankedEntries { get; }

    public ImmutableArray<CompanionRoleShortlistEntry> ExcludedEntries { get; }

    public ImmutableArray<CompanionRoleShortlistDiagnostic> Diagnostics { get; }

    public string Fingerprint { get; }

    private static int Count(
        IEnumerable<CompanionRoleShortlistEntry> entries,
        CompanionRoleCandidateRankingState state) =>
        entries.Count(item => item.Candidate.State == state);

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("COMPANION_ROLE_SHORTLIST_V1\n")
            .Append(Ranking.Fingerprint).Append('\n')
            .Append(Counts.StableKey).Append('\n');
        foreach (var entry in Entries)
        {
            canonical.Append("ENTRY|").Append(entry.StableKey).Append('\n');
        }

        foreach (var diagnostic in Diagnostics)
        {
            canonical.Append("DIAGNOSTIC|").Append(diagnostic.StableKey).Append('\n');
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}

public sealed class CompanionRoleShortlistView
{
    internal CompanionRoleShortlistView(
        CompanionRoleShortlist source,
        CompanionRoleShortlistFilter filter,
        IEnumerable<CompanionRoleShortlistEntry> entries)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown shortlist filter.");
        }

        Filter = filter;
        Entries = [.. entries];
    }

    public CompanionRoleShortlist Source { get; }

    public CompanionRoleShortlistFilter Filter { get; }

    public ImmutableArray<CompanionRoleShortlistEntry> Entries { get; }

    public int VisibleCount => Entries.Length;

    public CompanionRoleShortlistCounts UnfilteredCounts => Source.Counts;
}
