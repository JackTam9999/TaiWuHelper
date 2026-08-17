using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public sealed class CompanionRoleCandidateRanking
{
    internal CompanionRoleCandidateRanking(
        CompanionRoleEvaluation evaluation,
        CompanionRoleCandidateRankingState state,
        int? competitionRank)
    {
        Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown candidate-ranking state.");
        }

        if ((evaluation.State == CompanionRoleEvaluationState.Rankable)
            != evaluation.TotalScore.HasValue)
        {
            throw new ArgumentException(
                "Only a rankable evaluation can carry a total score.",
                nameof(evaluation));
        }

        var expectedUnrankedState = evaluation.State switch
        {
            CompanionRoleEvaluationState.Ineligible => CompanionRoleCandidateRankingState.Ineligible,
            CompanionRoleEvaluationState.Incomplete => CompanionRoleCandidateRankingState.Incomplete,
            CompanionRoleEvaluationState.Unsupported => CompanionRoleCandidateRankingState.Unsupported,
            CompanionRoleEvaluationState.Conflicting => CompanionRoleCandidateRankingState.Conflicting,
            CompanionRoleEvaluationState.Rankable => (CompanionRoleCandidateRankingState?)null,
            _ => throw new ArgumentOutOfRangeException(
                nameof(evaluation),
                evaluation.State,
                "Unknown role-evaluation state.")
        };
        if (expectedUnrankedState is null)
        {
            if (state is not CompanionRoleCandidateRankingState.Ranked
                and not CompanionRoleCandidateRankingState.Tied
                || competitionRank is null or < 1)
            {
                throw new ArgumentException(
                    "A rankable evaluation requires a ranked or tied state and a positive competition rank.",
                    nameof(state));
            }
        }
        else if (state != expectedUnrankedState || competitionRank is not null)
        {
            throw new ArgumentException(
                "An unranked evaluation requires its exact typed state and cannot have a competition rank.",
                nameof(state));
        }

        State = state;
        CompetitionRank = competitionRank;
    }

    public CompanionRoleEvaluation Evaluation { get; }

    public CompanionRoleCandidateRankingState State { get; }

    public int? CompetitionRank { get; }

    public bool IsRanked => CompetitionRank.HasValue;

    internal string StableKey => string.Join('|',
        Evaluation.Fingerprint,
        CompanionRoleText.EnumKey(State),
        CompetitionRank?.ToString(CultureInfo.InvariantCulture) ?? "NONE");
}

public sealed class CompanionRoleRanking
{
    internal CompanionRoleRanking(
        CompanionRoleDefinition definition,
        CandidateDisciplineIdentity discipline,
        IEnumerable<CompanionRoleCandidateRanking> candidates)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Discipline = discipline ?? throw new ArgumentNullException(nameof(discipline));
        ArgumentNullException.ThrowIfNull(candidates);
        var values = candidates.ToImmutableArray();
        if (values.Any(item => item is null))
        {
            throw new ArgumentException("A role ranking cannot contain null candidates.", nameof(candidates));
        }

        if (values.GroupBy(item => item.Evaluation.Profile.Identity.CharacterId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException("A role ranking cannot contain a candidate more than once.", nameof(candidates));
        }

        if (values.Any(item =>
                !string.Equals(
                    item.Evaluation.Definition.Fingerprint,
                    definition.Fingerprint,
                    StringComparison.Ordinal)
                || item.Evaluation.Discipline != discipline))
        {
            throw new ArgumentException(
                "Every candidate ranking must use the exact role definition and discipline.",
                nameof(candidates));
        }

        SourceVersions = values.IsEmpty
            ? null
            : values[0].Evaluation.Profile.SourceVersions;
        if (values.Any(item => item.Evaluation.Profile.SourceVersions != SourceVersions))
        {
            throw new ArgumentException(
                "Comparable candidate evaluations must use one exact source-version set.",
                nameof(candidates));
        }

        ValidateCompetitionRanks(values);
        Candidates = [.. values
            .OrderBy(item => item.CompetitionRank ?? int.MaxValue)
            .ThenBy(item => item.IsRanked ? 0 : (int)item.State)
            .ThenBy(item => item.Evaluation.Profile.Identity.CharacterId)];
        RankedCandidates = [.. Candidates.Where(item => item.IsRanked)];
        UnrankedCandidates = [.. Candidates.Where(item => !item.IsRanked)];
        Fingerprint = CreateFingerprint();
    }

    public CompanionRoleDefinition Definition { get; }

    public CandidateDisciplineIdentity Discipline { get; }

    public CandidateProfileSourceVersions? SourceVersions { get; }

    public int CandidateCount => Candidates.Length;

    public ImmutableArray<CompanionRoleCandidateRanking> Candidates { get; }

    public ImmutableArray<CompanionRoleCandidateRanking> RankedCandidates { get; }

    public ImmutableArray<CompanionRoleCandidateRanking> UnrankedCandidates { get; }

    public string Fingerprint { get; }

    private static void ValidateCompetitionRanks(
        ImmutableArray<CompanionRoleCandidateRanking> candidates)
    {
        var rankable = candidates
            .Where(item => item.Evaluation.State == CompanionRoleEvaluationState.Rankable)
            .GroupBy(item => item.Evaluation.TotalScore!.Value)
            .OrderByDescending(group => group.Key)
            .ToArray();
        var expectedRank = 1;
        foreach (var group in rankable)
        {
            var expectedState = group.Count() == 1
                ? CompanionRoleCandidateRankingState.Ranked
                : CompanionRoleCandidateRankingState.Tied;
            if (group.Any(item =>
                    item.CompetitionRank != expectedRank
                    || item.State != expectedState))
            {
                throw new ArgumentException(
                    "Candidate states and competition ranks must match exact score groups.",
                    nameof(candidates));
            }

            expectedRank = checked(expectedRank + group.Count());
        }
    }

    private string CreateFingerprint()
    {
        var canonical = new StringBuilder()
            .Append("COMPANION_ROLE_RANKING_V1\n")
            .Append(Definition.Fingerprint).Append('\n')
            .Append(Discipline.StableKey).Append('\n')
            .Append(SourceVersions?.StableKey ?? "NO_CANDIDATE_SOURCE").Append('\n');
        foreach (var candidate in Candidates)
        {
            canonical.Append("CANDIDATE|").Append(candidate.StableKey).Append('\n');
        }

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }
}
