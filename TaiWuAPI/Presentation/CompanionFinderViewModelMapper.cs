using System.Globalization;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWuAPI.Contracts.CompanionCandidates;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

public static class CompanionFinderViewModelMapper
{
    public static IReadOnlyList<CompanionFinderRoleOptionViewModel> MapRoles(
        TaiwuLanguage language) =>
        [.. CompanionFinderResponseMapper.MapRoles(language).Roles.Select(role =>
            new CompanionFinderRoleOptionViewModel(
                role.Identity,
                role.RoleVersion,
                role.DisciplineDomain,
                CompanionFinderUiText.RoleLabel(
                    language,
                    role.DisciplineDomain),
                role.Purpose,
                role.ScoreLimitation))];

    public static IReadOnlyList<CompanionDisciplineOptionViewModel>
        MapDisciplines(
            CompanionDisciplineDisplayResult result,
            TaiwuLanguage language)
    {
        ArgumentNullException.ThrowIfNull(result);
        return [.. result.Disciplines.Select(value =>
        {
            var name = language switch
            {
                TaiwuLanguage.English => value.EnglishName,
                TaiwuLanguage.Chinese => value.TraditionalChineseName,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(language),
                    language,
                    "Unknown UI language.")
            };
            return new CompanionDisciplineOptionViewModel(
                value.Discipline.Domain,
                value.Discipline.Type,
                name ?? CompanionFinderUiText.Get(
                    language,
                    CompanionFinderUiTextKey.Unavailable),
                name is not null);
        })];
    }

    public static CompanionFinderViewModel Map(
        CompanionFinderResult result,
        TaiwuLanguage language,
        string disciplineName)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(disciplineName);
        if (!result.HasAuthoritativeResult)
        {
            throw new ArgumentException(
                "An authoritative companion-finder result is required.",
                nameof(result));
        }

        var response = CompanionFinderResponseMapper.Map(result, language);
        var counts = response.Counts!;
        var candidates = response.Candidates.Select(candidate => MapCandidate(
            candidate,
            language)).ToArray();
        return new CompanionFinderViewModel(
            response.Status,
            disciplineName.Trim(),
            CompanionFinderUiText.RoleLabel(
                language,
                response.Role!.DisciplineDomain),
            response.Role.Purpose,
            CompanionFinderUiText.Get(
                language,
                CompanionFinderUiTextKey.ScoreLimitation),
            response.Source!.SnapshotCapturedAtUtc,
            new CompanionFinderCountsViewModel(
                counts.Total,
                checked(counts.Ranked + counts.Tied),
                counts.Ranked,
                counts.Tied,
                checked(counts.Incomplete
                    + counts.Unsupported
                    + counts.Conflicting),
                counts.Ineligible,
                counts.Incomplete,
                counts.Unsupported,
                counts.Conflicting),
            candidates,
            response.Status == CompanionFinderStatus.Partial,
            response.Status == CompanionFinderStatus.Empty);
    }

    public static CompanionComparisonViewModel MapComparison(
        CompanionFinderResult result,
        CompanionFinderViewModel model,
        int firstCharacterId,
        int secondCharacterId,
        TaiwuLanguage language)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(model);
        if (!result.HasAuthoritativeResult)
        {
            throw new ArgumentException(
                "An authoritative companion-finder result is required.",
                nameof(result));
        }

        var comparison = CompanionRoleComparisonBuilder.Compare(
            result.Shortlist!,
            firstCharacterId,
            secondCharacterId);
        var first = model.Candidates.Single(value =>
            value.CharacterId == firstCharacterId);
        var second = model.Candidates.Single(value =>
            value.CharacterId == secondCharacterId);
        var facts = new List<CompanionComparisonFactViewModel>
        {
            new(
                Text(language, CompanionFinderUiTextKey.EvaluationState),
                first.RankingStateLabel,
                second.RankingStateLabel),
            new(
                Text(language, CompanionFinderUiTextKey.HardGates),
                GateSummary(first, language),
                GateSummary(second, language))
        };
        foreach (var row in comparison.Rows)
        {
            facts.Add(new CompanionComparisonFactViewModel(
                Text(
                    language,
                    CompanionFinderUiTextKey.SavedBaseQualification),
                ComparisonValue(row.First, language),
                ComparisonValue(row.Second, language)));
        }

        facts.Add(new CompanionComparisonFactViewModel(
            Text(language, CompanionFinderUiTextKey.Evidence),
            first.EvidenceLabel,
            second.EvidenceLabel));
        facts.Add(new CompanionComparisonFactViewModel(
            Text(language, CompanionFinderUiTextKey.RoleLocalScore),
            first.ScoreLabel,
            second.ScoreLabel));
        facts.Add(new CompanionComparisonFactViewModel(
            Text(language, CompanionFinderUiTextKey.CompetitionRank),
            first.RankLabel,
            second.RankLabel));
        return new CompanionComparisonViewModel(
            first.DisplayName,
            second.DisplayName,
            CompanionFinderApiText.ComparisonOutcome(
                language,
                comparison.Outcome),
            facts);
    }

    public static CompanionFinderNoticeViewModel MapFailure(
        CompanionFinderStatus status,
        TaiwuLanguage language)
    {
        var (title, message, retry) = status switch
        {
            CompanionFinderStatus.SaveUnavailable => (
                CompanionFinderUiTextKey.SaveUnavailableTitle,
                CompanionFinderUiTextKey.SaveUnavailableMessage,
                true),
            CompanionFinderStatus.UnsupportedSourceVersion
                or CompanionFinderStatus.UnsupportedRoleVersion => (
                CompanionFinderUiTextKey.UnsupportedSourceTitle,
                CompanionFinderUiTextKey.UnsupportedSourceMessage,
                false),
            CompanionFinderStatus.ChangedRevision => (
                CompanionFinderUiTextKey.ChangedRevisionTitle,
                CompanionFinderUiTextKey.ChangedRevisionMessage,
                true),
            _ => (
                CompanionFinderUiTextKey.ReadFailedTitle,
                CompanionFinderUiTextKey.ReadFailedMessage,
                true)
        };
        return new CompanionFinderNoticeViewModel(
            CompanionFinderNoticeStatus.Failure,
            Text(language, title),
            Text(language, message),
            retry);
    }

    private static CompanionCandidateViewModel MapCandidate(
        CompanionCandidateResponse candidate,
        TaiwuLanguage language)
    {
        var scoreFact = candidate.ScoreFacts.SingleOrDefault();
        var evidence = scoreFact?.EvidenceState
            ?? CompanionFactEvidenceState.Missing;
        var strengths = candidate.Explanations
            .Where(value => value.Kind
                == CompanionRoleExplanationKind.StrongestContribution)
            .Select(value => value.Message)
            .ToArray();
        var limitations = candidate.Explanations
            .Where(value => value.Kind
                != CompanionRoleExplanationKind.StrongestContribution)
            .Select(value => value.Message)
            .ToArray();
        return new CompanionCandidateViewModel(
            candidate.CharacterId,
            candidate.DisplayName ?? Text(
                language,
                CompanionFinderUiTextKey.UnnamedCandidate),
            candidate.LocationName ?? Text(
                language,
                CompanionFinderUiTextKey.LocationUnavailable),
            Section(candidate.RankingState),
            candidate.RankingState,
            candidate.RankingStateLabel,
            candidate.CompetitionRank,
            RankLabel(
                candidate.RankingState,
                candidate.CompetitionRank,
                language),
            candidate.TotalScore,
            candidate.TotalScore?.ToString(
                "0.##",
                CultureInfo.InvariantCulture)
                ?? Text(language, CompanionFinderUiTextKey.Unavailable),
            EvidenceLabel(evidence, language),
            strengths,
            limitations,
            [.. candidate.Gates.Select(gate =>
                new CompanionCandidateGateViewModel(
                    gate.OutcomeLabel,
                    gate.Explanation,
                    gate.Outcome == CompanionRoleGateOutcome.Passed))]);
    }

    private static CompanionCandidateSection Section(
        CompanionRoleCandidateRankingState state) => state switch
        {
            CompanionRoleCandidateRankingState.Ranked
                or CompanionRoleCandidateRankingState.Tied =>
                CompanionCandidateSection.Ranked,
            CompanionRoleCandidateRankingState.Incomplete
                or CompanionRoleCandidateRankingState.Unsupported
                or CompanionRoleCandidateRankingState.Conflicting =>
                CompanionCandidateSection.NeedsReview,
            CompanionRoleCandidateRankingState.Ineligible =>
                CompanionCandidateSection.Ineligible,
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown candidate ranking state.")
        };

    private static string EvidenceLabel(
        CompanionFactEvidenceState state,
        TaiwuLanguage language) => Text(
        language,
        state switch
        {
            CompanionFactEvidenceState.Confirmed =>
                CompanionFinderUiTextKey.ConfirmedEvidence,
            CompanionFactEvidenceState.Missing =>
                CompanionFinderUiTextKey.MissingEvidence,
            CompanionFactEvidenceState.Incomplete =>
                CompanionFinderUiTextKey.IncompleteEvidence,
            CompanionFactEvidenceState.Unsupported =>
                CompanionFinderUiTextKey.UnsupportedEvidence,
            CompanionFactEvidenceState.Stale =>
                CompanionFinderUiTextKey.StaleEvidence,
            CompanionFactEvidenceState.Conflicting =>
                CompanionFinderUiTextKey.ConflictingEvidence,
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown candidate evidence state.")
        });

    private static string RankLabel(
        CompanionRoleCandidateRankingState state,
        int? rank,
        TaiwuLanguage language)
    {
        if (!rank.HasValue)
        {
            return Text(language, CompanionFinderUiTextKey.NotRanked);
        }

        return language switch
        {
            TaiwuLanguage.English when state
                == CompanionRoleCandidateRankingState.Tied =>
                $"Tied at rank {rank.Value}",
            TaiwuLanguage.English => $"Rank {rank.Value}",
            TaiwuLanguage.Chinese when state
                == CompanionRoleCandidateRankingState.Tied =>
                $"並列第 {rank.Value} 名",
            TaiwuLanguage.Chinese => $"第 {rank.Value} 名",
            _ => throw new ArgumentOutOfRangeException(
                nameof(language),
                language,
                "Unknown UI language.")
        };
    }

    private static string GateSummary(
        CompanionCandidateViewModel candidate,
        TaiwuLanguage language) => candidate.Gates.Count == 0
        ? Text(language, CompanionFinderUiTextKey.Unavailable)
        : string.Join(
            "; ",
            candidate.Gates.Select(value =>
                $"{value.Outcome}: {value.Explanation}"));

    private static string ComparisonValue(
        CompanionRoleComparisonValue value,
        TaiwuLanguage language) => value.Value?.ToString(
        CultureInfo.InvariantCulture) ?? Text(
        language,
        value.State switch
        {
            CompanionRoleComparisonEvidenceState.Missing =>
                CompanionFinderUiTextKey.MissingEvidence,
            CompanionRoleComparisonEvidenceState.Incomplete =>
                CompanionFinderUiTextKey.IncompleteEvidence,
            CompanionRoleComparisonEvidenceState.Unsupported =>
                CompanionFinderUiTextKey.UnsupportedEvidence,
            CompanionRoleComparisonEvidenceState.Stale =>
                CompanionFinderUiTextKey.StaleEvidence,
            CompanionRoleComparisonEvidenceState.Conflicting =>
                CompanionFinderUiTextKey.ConflictingEvidence,
            CompanionRoleComparisonEvidenceState.Confirmed =>
                CompanionFinderUiTextKey.Unavailable,
            _ => throw new ArgumentOutOfRangeException(
                nameof(value),
                value.State,
                "Unknown comparison evidence state.")
        });

    private static string Text(
        TaiwuLanguage language,
        CompanionFinderUiTextKey key) =>
        CompanionFinderUiText.Get(language, key);
}
