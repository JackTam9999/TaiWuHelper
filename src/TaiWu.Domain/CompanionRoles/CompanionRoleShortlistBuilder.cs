using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public static class CompanionRoleShortlistBuilder
{
    public static CompanionRoleRanking EvaluateAndRank(
        CompanionRoleDefinition definition,
        CandidateDisciplineIdentity discipline,
        IEnumerable<CandidateProfile> profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(discipline);
        ArgumentNullException.ThrowIfNull(profiles);
        var candidates = profiles.ToArray();
        if (candidates.Any(item => item is null))
        {
            throw new ArgumentException("Candidate profiles cannot contain null entries.", nameof(profiles));
        }

        if (candidates.GroupBy(item => item.Identity.CharacterId)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException("A candidate identity cannot be evaluated more than once.", nameof(profiles));
        }

        if (candidates.Length > 0
            && candidates.Any(item => item.SourceVersions != candidates[0].SourceVersions))
        {
            throw new ArgumentException(
                "Comparable candidate profiles must use one exact source-version set.",
                nameof(profiles));
        }

        var evaluations = new List<CompanionRoleEvaluation>(candidates.Length);
        foreach (var profile in candidates.OrderBy(item => item.Identity.CharacterId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            evaluations.Add(CompanionRoleEvaluator.Evaluate(
                definition,
                profile,
                discipline));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var entries = new List<CompanionRoleCandidateRanking>(evaluations.Count);
        var competitionRank = 1;
        foreach (var scoreGroup in evaluations
                     .Where(item => item.State == CompanionRoleEvaluationState.Rankable)
                     .GroupBy(item => item.TotalScore!.Value)
                     .OrderByDescending(group => group.Key))
        {
            var group = scoreGroup
                .OrderBy(item => item.Profile.Identity.CharacterId)
                .ToArray();
            var state = group.Length == 1
                ? CompanionRoleCandidateRankingState.Ranked
                : CompanionRoleCandidateRankingState.Tied;
            entries.AddRange(group.Select(evaluation =>
                new CompanionRoleCandidateRanking(
                    evaluation,
                    state,
                    competitionRank)));
            competitionRank = checked(competitionRank + group.Length);
        }

        entries.AddRange(evaluations
            .Where(item => item.State != CompanionRoleEvaluationState.Rankable)
            .Select(evaluation => new CompanionRoleCandidateRanking(
                evaluation,
                MapUnrankedState(evaluation.State),
                competitionRank: null)));
        return new CompanionRoleRanking(definition, discipline, entries);
    }

    private static CompanionRoleCandidateRankingState MapUnrankedState(
        CompanionRoleEvaluationState state) =>
        state switch
        {
            CompanionRoleEvaluationState.Ineligible => CompanionRoleCandidateRankingState.Ineligible,
            CompanionRoleEvaluationState.Incomplete => CompanionRoleCandidateRankingState.Incomplete,
            CompanionRoleEvaluationState.Unsupported => CompanionRoleCandidateRankingState.Unsupported,
            CompanionRoleEvaluationState.Conflicting => CompanionRoleCandidateRankingState.Conflicting,
            CompanionRoleEvaluationState.Rankable => throw new ArgumentException(
                "A rankable evaluation cannot be mapped to an unranked state.",
                nameof(state)),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown role-evaluation state.")
        };
}
