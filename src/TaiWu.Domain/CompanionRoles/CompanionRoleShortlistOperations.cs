using TaiWu.Domain.CompanionCandidates;

namespace TaiWu.Domain.CompanionRoles;

public static class CompanionRoleShortlistFactory
{
    public static CompanionRoleShortlist Create(CompanionRoleRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var entries = ranking.Candidates
            .Select(candidate => new CompanionRoleShortlistEntry(
                candidate,
                Explain(candidate)))
            .ToArray();
        var counts = new CompanionRoleShortlistCounts(
            Count(ranking, CompanionRoleCandidateRankingState.Ranked),
            Count(ranking, CompanionRoleCandidateRankingState.Tied),
            Count(ranking, CompanionRoleCandidateRankingState.Ineligible),
            Count(ranking, CompanionRoleCandidateRankingState.Incomplete),
            Count(ranking, CompanionRoleCandidateRankingState.Unsupported),
            Count(ranking, CompanionRoleCandidateRankingState.Conflicting));
        var diagnostics = new List<CompanionRoleShortlistDiagnostic>
        {
            new(
                "ROLE_SCORE_IS_ROLE_LOCAL",
                CompanionRoleShortlistDiagnosticSeverity.Information),
            new(
                "SHORTLIST_IS_INFORMATION_ONLY",
                CompanionRoleShortlistDiagnosticSeverity.Information)
        };
        if (ranking.Candidates.Any(item => !item.IsRanked))
        {
            diagnostics.Add(new CompanionRoleShortlistDiagnostic(
                "SHORTLIST_CONTAINS_UNRANKED_EVIDENCE",
                CompanionRoleShortlistDiagnosticSeverity.Warning));
        }

        return new CompanionRoleShortlist(ranking, entries, counts, diagnostics);
    }

    private static IEnumerable<CompanionRoleExplanation> Explain(
        CompanionRoleCandidateRanking candidate)
    {
        var evaluation = candidate.Evaluation;
        if (!candidate.IsRanked)
        {
            var nonPassingGates = evaluation.Gates
                .Where(item => item.Outcome != CompanionRoleGateOutcome.Passed)
                .ToArray();
            return [new CompanionRoleExplanation(
                CompanionRoleExplanationKind.Exclusion,
                evaluation.OutcomeIdentity,
                [],
                nonPassingGates)];
        }

        var maximumContribution = evaluation.Components.Max(item => item.Contribution);
        var strongest = evaluation.Components
            .Where(item => item.Contribution == maximumContribution)
            .ToArray();
        var explanations = new List<CompanionRoleExplanation>
        {
            new(
                CompanionRoleExplanationKind.StrongestContribution,
                "STRONGEST_APPROVED_SCORE_CONTRIBUTION",
                strongest,
                []),
            new(
                CompanionRoleExplanationKind.MaterialLimitation,
                "ROLE_SCORE_LIMITED_TO_APPROVED_COMPONENTS",
                evaluation.Components,
                [])
        };
        if (candidate.State == CompanionRoleCandidateRankingState.Tied)
        {
            explanations.Add(new CompanionRoleExplanation(
                CompanionRoleExplanationKind.ExactTie,
                "EXACT_ROLE_TOTAL_TIE",
                evaluation.Components,
                []));
        }

        return explanations;
    }

    private static int Count(
        CompanionRoleRanking ranking,
        CompanionRoleCandidateRankingState state) =>
        ranking.Candidates.Count(item => item.State == state);
}

public static class CompanionRoleShortlistFilterer
{
    public static CompanionRoleShortlistView CreateView(
        CompanionRoleShortlist shortlist,
        CompanionRoleShortlistFilter filter)
    {
        ArgumentNullException.ThrowIfNull(shortlist);
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown shortlist filter.");
        }

        var entries = shortlist.Entries.Where(item => filter switch
        {
            CompanionRoleShortlistFilter.All => true,
            CompanionRoleShortlistFilter.Ranked => item.Candidate.State is
                CompanionRoleCandidateRankingState.Ranked
                or CompanionRoleCandidateRankingState.Tied,
            CompanionRoleShortlistFilter.NeedsReview => item.Candidate.State is
                CompanionRoleCandidateRankingState.Incomplete
                or CompanionRoleCandidateRankingState.Unsupported
                or CompanionRoleCandidateRankingState.Conflicting,
            CompanionRoleShortlistFilter.Ineligible =>
                item.Candidate.State == CompanionRoleCandidateRankingState.Ineligible,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unknown shortlist filter.")
        });
        return new CompanionRoleShortlistView(shortlist, filter, entries);
    }
}

public static class CompanionRoleComparisonBuilder
{
    public static CompanionRoleComparison Compare(
        CompanionRoleShortlist shortlist,
        int firstCharacterId,
        int secondCharacterId)
    {
        ArgumentNullException.ThrowIfNull(shortlist);
        if (firstCharacterId == secondCharacterId)
        {
            throw new ArgumentException("Comparison candidate identities must be different.", nameof(secondCharacterId));
        }

        var first = shortlist.Entries.SingleOrDefault(item =>
            item.Evaluation.Profile.Identity.CharacterId == firstCharacterId)
            ?? throw new ArgumentException("The first candidate is not in the shortlist.", nameof(firstCharacterId));
        var second = shortlist.Entries.SingleOrDefault(item =>
            item.Evaluation.Profile.Identity.CharacterId == secondCharacterId)
            ?? throw new ArgumentException("The second candidate is not in the shortlist.", nameof(secondCharacterId));
        var rows = shortlist.Definition.ScoreDimensions
            .Select(dimension => CreateRow(dimension, shortlist.Discipline, first, second))
            .ToArray();
        var outcome = AggregateOutcome(first, second, rows);
        return new CompanionRoleComparison(shortlist, first, second, rows, outcome);
    }

    private static CompanionRoleComparisonRow CreateRow(
        CompanionRoleScoreDimension dimension,
        CandidateDisciplineIdentity discipline,
        CompanionRoleShortlistEntry first,
        CompanionRoleShortlistEntry second)
    {
        var field = CandidateProfileFieldIdentity.ForRole(
            dimension.Field,
            discipline);
        var firstValue = dimension.Field
            == CandidateProfileField.CapabilityBreadthIndex
            ? ReadCapabilityValue(first.Evaluation.Profile, dimension)
            : ReadValue(first.Evaluation.Profile.FindFact(field));
        var secondValue = dimension.Field
            == CandidateProfileField.CapabilityBreadthIndex
            ? ReadCapabilityValue(second.Evaluation.Profile, dimension)
            : ReadValue(second.Evaluation.Profile.FindFact(field));
        var outcome = CompareRow(first, second, dimension, firstValue, secondValue);
        return new CompanionRoleComparisonRow(
            dimension,
            field,
            firstValue,
            secondValue,
            outcome);
    }

    private static CompanionRoleComparisonValue ReadCapabilityValue(
        CandidateProfile profile,
        CompanionRoleScoreDimension dimension)
    {
        var summary = CompanionCapabilitySummaryBuilder.Build(profile);
        var state = summary.State switch
        {
            CompanionCapabilitySummaryState.Complete =>
                CompanionRoleComparisonEvidenceState.Confirmed,
            CompanionCapabilitySummaryState.Incomplete =>
                CompanionRoleComparisonEvidenceState.Incomplete,
            CompanionCapabilitySummaryState.Unsupported =>
                CompanionRoleComparisonEvidenceState.Unsupported,
            CompanionCapabilitySummaryState.Stale =>
                CompanionRoleComparisonEvidenceState.Stale,
            CompanionCapabilitySummaryState.Conflicting =>
                CompanionRoleComparisonEvidenceState.Conflicting,
            _ => throw new InvalidOperationException(
                $"Unknown capability summary state '{summary.State}'.")
        };
        var scaledBreadth = summary.BreadthIndex.GetValueOrDefault() * 100m;
        if (state == CompanionRoleComparisonEvidenceState.Confirmed
            && (scaledBreadth < short.MinValue
                || scaledBreadth > short.MaxValue
                || scaledBreadth < dimension.NormalizationMinimum
                || scaledBreadth > dimension.NormalizationMaximum))
        {
            state = CompanionRoleComparisonEvidenceState.Conflicting;
        }

        return new CompanionRoleComparisonValue(
            state,
            state == CompanionRoleComparisonEvidenceState.Confirmed
                ? decimal.ToInt16(scaledBreadth)
                : null,
            fact: null);
    }

    private static CompanionRoleComparisonValue ReadValue(CandidateProfileFact? fact)
    {
        if (fact is null)
        {
            return new CompanionRoleComparisonValue(
                CompanionRoleComparisonEvidenceState.Missing,
                value: null,
                fact: null);
        }

        var state = fact.State switch
        {
            CandidateEvidenceState.Confirmed => CompanionRoleComparisonEvidenceState.Confirmed,
            CandidateEvidenceState.Incomplete => CompanionRoleComparisonEvidenceState.Incomplete,
            CandidateEvidenceState.Unsupported => CompanionRoleComparisonEvidenceState.Unsupported,
            CandidateEvidenceState.Stale => CompanionRoleComparisonEvidenceState.Stale,
            CandidateEvidenceState.Conflicting => CompanionRoleComparisonEvidenceState.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact.State, "Unknown candidate evidence state.")
        };
        return new CompanionRoleComparisonValue(
            state,
            state == CompanionRoleComparisonEvidenceState.Confirmed
                ? fact.Value!.Int16Value
                : null,
            fact);
    }

    private static CompanionRoleComparisonOutcome CompareRow(
        CompanionRoleShortlistEntry first,
        CompanionRoleShortlistEntry second,
        CompanionRoleScoreDimension dimension,
        CompanionRoleComparisonValue firstValue,
        CompanionRoleComparisonValue secondValue)
    {
        if (first.Candidate.State == CompanionRoleCandidateRankingState.Conflicting
            || second.Candidate.State == CompanionRoleCandidateRankingState.Conflicting
            || firstValue.State == CompanionRoleComparisonEvidenceState.Conflicting
            || secondValue.State == CompanionRoleComparisonEvidenceState.Conflicting)
        {
            return CompanionRoleComparisonOutcome.Conflicting;
        }

        if (!first.Candidate.IsRanked
            || !second.Candidate.IsRanked
            || firstValue.State != CompanionRoleComparisonEvidenceState.Confirmed
            || secondValue.State != CompanionRoleComparisonEvidenceState.Confirmed)
        {
            return CompanionRoleComparisonOutcome.Unavailable;
        }

        var firstComponent = first.Evaluation.Components.SingleOrDefault(item =>
            ReferenceEquals(item.Dimension, dimension));
        var secondComponent = second.Evaluation.Components.SingleOrDefault(item =>
            ReferenceEquals(item.Dimension, dimension));
        if (firstComponent is null || secondComponent is null)
        {
            return CompanionRoleComparisonOutcome.Unavailable;
        }

        if (firstComponent.Contribution == secondComponent.Contribution)
        {
            return CompanionRoleComparisonOutcome.Equal;
        }

        return firstComponent.Contribution > secondComponent.Contribution
            ? CompanionRoleComparisonOutcome.FirstAdvantage
            : CompanionRoleComparisonOutcome.SecondAdvantage;
    }

    private static CompanionRoleComparisonOutcome AggregateOutcome(
        CompanionRoleShortlistEntry first,
        CompanionRoleShortlistEntry second,
        IReadOnlyList<CompanionRoleComparisonRow> rows)
    {
        if (first.Candidate.State == CompanionRoleCandidateRankingState.Conflicting
            || second.Candidate.State == CompanionRoleCandidateRankingState.Conflicting
            || rows.Any(item => item.Outcome == CompanionRoleComparisonOutcome.Conflicting))
        {
            return CompanionRoleComparisonOutcome.Conflicting;
        }

        if (!first.Candidate.IsRanked
            || !second.Candidate.IsRanked
            || rows.Any(item => item.Outcome == CompanionRoleComparisonOutcome.Unavailable))
        {
            return CompanionRoleComparisonOutcome.Unavailable;
        }

        var firstAdvantage = rows.Any(item => item.Outcome == CompanionRoleComparisonOutcome.FirstAdvantage);
        var secondAdvantage = rows.Any(item => item.Outcome == CompanionRoleComparisonOutcome.SecondAdvantage);
        if (firstAdvantage && secondAdvantage)
        {
            return CompanionRoleComparisonOutcome.Tradeoff;
        }

        if (firstAdvantage)
        {
            return CompanionRoleComparisonOutcome.FirstAdvantage;
        }

        return secondAdvantage
            ? CompanionRoleComparisonOutcome.SecondAdvantage
            : CompanionRoleComparisonOutcome.Equal;
    }
}
