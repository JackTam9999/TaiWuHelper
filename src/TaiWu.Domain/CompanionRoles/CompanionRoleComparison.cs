using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public sealed class CompanionRoleComparisonValue
{
    internal CompanionRoleComparisonValue(
        CompanionRoleComparisonEvidenceState state,
        short? value,
        CandidateProfileFact? fact)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown comparison evidence state.");
        }

        if ((state == CompanionRoleComparisonEvidenceState.Confirmed) != value.HasValue)
        {
            throw new ArgumentException("Only confirmed comparison evidence has a current numeric value.", nameof(value));
        }

        State = state;
        Value = value;
        Fact = fact;
    }

    public CompanionRoleComparisonEvidenceState State { get; }

    public short? Value { get; }

    public CandidateProfileFact? Fact { get; }

    public ImmutableArray<CandidateEvidenceReference> Evidence => Fact?.Evidence ?? [];

    public CandidateUnavailableReason? UnavailableReason => Fact?.UnavailableReason;

    internal string StableKey => string.Join('|',
        CompanionRoleText.EnumKey(State),
        Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NONE",
        Fact?.StableKey ?? "NO_FACT");
}

public sealed class CompanionRoleComparisonRow
{
    internal CompanionRoleComparisonRow(
        CompanionRoleScoreDimension dimension,
        CandidateProfileFieldIdentity field,
        CompanionRoleComparisonValue first,
        CompanionRoleComparisonValue second,
        CompanionRoleComparisonOutcome outcome)
    {
        Dimension = dimension ?? throw new ArgumentNullException(nameof(dimension));
        Field = field ?? throw new ArgumentNullException(nameof(field));
        First = first ?? throw new ArgumentNullException(nameof(first));
        Second = second ?? throw new ArgumentNullException(nameof(second));
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown comparison outcome.");
        }

        Outcome = outcome;
    }

    public CompanionRoleScoreDimension Dimension { get; }

    public CandidateProfileFieldIdentity Field { get; }

    public CompanionRoleComparisonValue First { get; }

    public CompanionRoleComparisonValue Second { get; }

    public CompanionRoleComparisonOutcome Outcome { get; }

    internal string StableKey => string.Join('|',
        Dimension.StableKey,
        Field.StableKey,
        First.StableKey,
        Second.StableKey,
        CompanionRoleText.EnumKey(Outcome));
}

public sealed class CompanionRoleComparison
{
    internal CompanionRoleComparison(
        CompanionRoleShortlist shortlist,
        CompanionRoleShortlistEntry first,
        CompanionRoleShortlistEntry second,
        IEnumerable<CompanionRoleComparisonRow> rows,
        CompanionRoleComparisonOutcome outcome)
    {
        Shortlist = shortlist ?? throw new ArgumentNullException(nameof(shortlist));
        First = first ?? throw new ArgumentNullException(nameof(first));
        Second = second ?? throw new ArgumentNullException(nameof(second));
        if (ReferenceEquals(first, second))
        {
            throw new ArgumentException("A candidate cannot be compared with itself.", nameof(second));
        }

        if (!shortlist.Entries.Contains(first) || !shortlist.Entries.Contains(second))
        {
            throw new ArgumentException("Both comparison candidates must belong to the same shortlist.", nameof(first));
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown comparison outcome.");
        }

        ArgumentNullException.ThrowIfNull(rows);
        var values = rows.ToImmutableArray();
        if (values.Any(item => item is null)
            || values.Length != shortlist.Definition.ScoreDimensions.Length
            || values.Where((item, index) => !ReferenceEquals(
                item.Dimension,
                shortlist.Definition.ScoreDimensions[index])).Any())
        {
            throw new ArgumentException(
                "Comparison rows must preserve every ordered score dimension exactly once.",
                nameof(rows));
        }

        Rows = values;
        Outcome = outcome;
        Fingerprint = CreateFingerprint();
    }

    public CompanionRoleShortlist Shortlist { get; }

    public CompanionRoleShortlistEntry First { get; }

    public CompanionRoleShortlistEntry Second { get; }

    public ImmutableArray<CompanionRoleComparisonRow> Rows { get; }

    public CompanionRoleComparisonOutcome Outcome { get; }

    public string Fingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("COMPANION_ROLE_COMPARISON_V1\n")
            .Append(Shortlist.Fingerprint).Append('\n')
            .Append(First.Candidate.StableKey).Append('\n')
            .Append(Second.Candidate.StableKey).Append('\n')
            .Append(CompanionRoleText.EnumKey(Outcome)).Append('\n');
        foreach (var row in Rows)
        {
            canonical.Append("ROW|").Append(row.StableKey).Append('\n');
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
