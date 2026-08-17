using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public sealed class CompanionRoleGateEvaluation
{
    internal CompanionRoleGateEvaluation(
        CompanionRoleHardRequirement requirement,
        CompanionRoleGateOutcome outcome,
        string reasonIdentity,
        IEnumerable<CandidateEvidenceReference> evidence)
    {
        Requirement = requirement;
        Outcome = outcome;
        ReasonIdentity = CompanionRoleText.Stable(reasonIdentity, nameof(reasonIdentity));
        Evidence = [.. evidence.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
    }

    public CompanionRoleHardRequirement Requirement { get; }

    public CompanionRoleGateOutcome Outcome { get; }

    public string ReasonIdentity { get; }

    public ImmutableArray<CandidateEvidenceReference> Evidence { get; }

    internal string StableKey => string.Join('|',
        Requirement.StableKey,
        CompanionRoleText.EnumKey(Outcome),
        ReasonIdentity,
        string.Join("||", Evidence.Select(item => item.StableKey)));
}

public sealed class CompanionRoleScoreComponent
{
    internal CompanionRoleScoreComponent(
        CompanionRoleScoreDimension dimension,
        CandidateProfileFieldIdentity field,
        short rawValue,
        decimal normalizedValue,
        decimal contribution,
        IEnumerable<CandidateEvidenceReference> evidence)
    {
        Dimension = dimension;
        Field = field;
        RawValue = rawValue;
        NormalizedValue = normalizedValue;
        Weight = dimension.Weight;
        Contribution = contribution;
        Evidence = [.. evidence.OrderBy(item => item.StableKey, StringComparer.Ordinal)];
    }

    public CompanionRoleScoreDimension Dimension { get; }

    public CandidateProfileFieldIdentity Field { get; }

    public short RawValue { get; }

    public decimal NormalizedValue { get; }

    public decimal Weight { get; }

    public decimal Contribution { get; }

    public ImmutableArray<CandidateEvidenceReference> Evidence { get; }

    internal string StableKey => string.Join('|',
        Dimension.StableKey,
        Field.StableKey,
        RawValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
        NormalizedValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Weight.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Contribution.ToString(System.Globalization.CultureInfo.InvariantCulture),
        string.Join("||", Evidence.Select(item => item.StableKey)));
}

public sealed class CompanionRoleEvaluation
{
    internal CompanionRoleEvaluation(
        CompanionRoleDefinition definition,
        CandidateProfile profile,
        CandidateDisciplineIdentity discipline,
        CompanionRoleEvaluationState state,
        IEnumerable<CompanionRoleGateEvaluation> gates,
        IEnumerable<CompanionRoleScoreComponent> components,
        decimal? totalScore,
        string outcomeIdentity)
    {
        Definition = definition;
        Profile = profile;
        Discipline = discipline;
        State = state;
        Gates = [.. gates];
        Components = [.. components];
        TotalScore = totalScore;
        OutcomeIdentity = CompanionRoleText.Stable(outcomeIdentity, nameof(outcomeIdentity));
        Fingerprint = CreateFingerprint();
    }

    public CompanionRoleDefinition Definition { get; }

    public CandidateProfile Profile { get; }

    public CandidateDisciplineIdentity Discipline { get; }

    public CompanionRoleEvaluationState State { get; }

    public ImmutableArray<CompanionRoleGateEvaluation> Gates { get; }

    public ImmutableArray<CompanionRoleScoreComponent> Components { get; }

    public decimal? TotalScore { get; }

    public string OutcomeIdentity { get; }

    public string Fingerprint { get; }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("COMPANION_ROLE_EVALUATION_V1\n")
            .Append(Definition.Fingerprint).Append('\n')
            .Append(Profile.Fingerprint).Append('\n')
            .Append(Discipline.StableKey).Append('\n')
            .Append(CompanionRoleText.EnumKey(State)).Append('|')
            .Append(TotalScore?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NONE")
            .Append('|').Append(OutcomeIdentity).Append('\n');
        foreach (var gate in Gates)
        {
            canonical.Append("GATE|").Append(gate.StableKey).Append('\n');
        }

        foreach (var component in Components)
        {
            canonical.Append("COMPONENT|").Append(component.StableKey).Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
