using System.Globalization;
using TaiWu.Application.CompanionCandidates;
using TaiWu.Application.Localization;
using TaiWu.Domain.CompanionCandidates;
using TaiWu.Domain.CompanionRoles;
using TaiWuAPI.Contracts.CompanionCandidates;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

public static partial class CompanionFinderViewModelMapper
{
    public static IReadOnlyList<CompanionFinderRoleOptionViewModel> MapRoles(
        TaiwuLanguage language) =>
        [.. CompanionFinderResponseMapper.MapRoles(language).Roles.Select(role =>
            new CompanionFinderRoleOptionViewModel(
                role.Identity,
                role.RoleVersion,
                role.DisciplineDomain,
                role.RequiresDisciplineSelection,
                CompanionFinderUiText.RoleLabel(
                    language,
                    role.Identity,
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
        string? disciplineName,
        IReadOnlyList<CompanionDisciplineOptionViewModel>? disciplineOptions = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.HasAuthoritativeResult)
        {
            throw new ArgumentException(
                "An authoritative companion-finder result is required.",
                nameof(result));
        }

        var response = CompanionFinderResponseMapper.Map(result, language);
        var role = response.Role!;
        if (role.RequiresDisciplineSelection
            && string.IsNullOrWhiteSpace(disciplineName))
        {
            throw new ArgumentException(
                "A discipline label is required for this objective.",
                nameof(disciplineName));
        }

        var roleLabel = CompanionFinderUiText.RoleLabel(
            language,
            role.Identity,
            role.DisciplineDomain);
        var counts = response.Counts!;
        var candidates = response.Candidates.Select(candidate => MapCandidate(
            candidate,
            language,
            disciplineOptions ?? [])).ToArray();
        return new CompanionFinderViewModel(
            response.Status,
            role.RequiresDisciplineSelection
                ? disciplineName!.Trim()
                : roleLabel,
            roleLabel,
            role.RequiresDisciplineSelection,
            string.Equals(
                role.Identity,
                "COMPREHENSIVE_BASE_CAPABILITY",
                StringComparison.Ordinal),
            Text(
                language,
                role.RequiresDisciplineSelection
                    ? CompanionFinderUiTextKey.SavedBaseQualification
                    : string.Equals(
                        role.Identity,
                        "SUCCESSION_CANDIDATE_READINESS",
                        StringComparison.Ordinal)
                        ? CompanionFinderUiTextKey.SuccessionIndex
                        : CompanionFinderUiTextKey.BreadthIndex),
            role.Purpose,
            role.RequiresDisciplineSelection
                ? Text(language, CompanionFinderUiTextKey.ScoreLimitation)
                : role.ScoreLimitation,
            response.Source!.SnapshotCapturedAtUtc,
            response.Source.SnapshotReadStatus,
            MapEnrichment(
                response.Enrichment!.Status,
                response.Enrichment.CatalogueStatus,
                language),
            new CompanionFinderCountsViewModel(
                counts.Total,
                counts.Eligible,
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

    private static CompanionCandidateViewModel MapCandidate(
        CompanionCandidateResponse candidate,
        TaiwuLanguage language,
        IReadOnlyList<CompanionDisciplineOptionViewModel> disciplineOptions)
    {
        var evidence = AggregateEvidence(candidate.ScoreFacts);
        var strengths = candidate.Explanations
            .Where(value => value.Kind
                == CompanionRoleExplanationKind.StrongestContribution)
            .Select(value => value.Message)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var limitations = candidate.Explanations
            .Where(value => value.Kind
                != CompanionRoleExplanationKind.StrongestContribution
                && !string.Equals(
                    value.Identity,
                    "ROLE_SCORE_LIMITED_TO_APPROVED_COMPONENTS",
                    StringComparison.Ordinal))
            .Select(value => value.Message)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return new CompanionCandidateViewModel(
            candidate.CharacterId,
            candidate.DisplayName ?? Text(
                language,
                CompanionFinderUiTextKey.UnnamedCandidate),
            candidate.LocationName ?? Text(
                language,
                CompanionFinderUiTextKey.LocationUnavailable),
            CandidateSource(candidate.CandidateContextFacts, language),
            CurrentAge(candidate.CandidateContextFacts, language),
            Section(candidate.RankingState),
            candidate.RankingState,
            candidate.RankingStateLabel,
            candidate.EvaluationState,
            CompanionFinderApiText.EvaluationState(
                language,
                candidate.EvaluationState),
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
            MapCapabilitySummary(
                candidate.CapabilitySummary,
                language,
                disciplineOptions),
            strengths,
            limitations,
            [.. candidate.Gates.Select(gate =>
                new CompanionCandidateGateViewModel(
                    gate.Order,
                    gate.RequirementIdentity,
                    gate.Kind,
                    gate.Field,
                    CompanionFinderApiText.GateRequirement(
                        language,
                        gate.Kind,
                        gate.Field),
                    gate.Outcome,
                    gate.OutcomeLabel,
                    gate.ReasonIdentity,
                    gate.Explanation,
                    gate.Outcome == CompanionRoleGateOutcome.Passed))]);
    }

    private static CompanionFactEvidenceState AggregateEvidence(
        IReadOnlyList<CompanionRoleFactResponse> facts)
    {
        if (facts.Count == 0)
        {
            return CompanionFactEvidenceState.Missing;
        }

        return facts.Any(fact => fact.EvidenceState
                == CompanionFactEvidenceState.Conflicting)
            ? CompanionFactEvidenceState.Conflicting
            : facts.Any(fact => fact.EvidenceState
                == CompanionFactEvidenceState.Unsupported)
                ? CompanionFactEvidenceState.Unsupported
                : facts.Any(fact => fact.EvidenceState
                    == CompanionFactEvidenceState.Stale)
                    ? CompanionFactEvidenceState.Stale
                    : facts.Any(fact => fact.EvidenceState
                        == CompanionFactEvidenceState.Incomplete)
                        ? CompanionFactEvidenceState.Incomplete
                        : facts.Any(fact => fact.EvidenceState
                            == CompanionFactEvidenceState.Missing)
                            ? CompanionFactEvidenceState.Missing
                            : CompanionFactEvidenceState.Confirmed;
    }

    private static string CandidateSource(
        IReadOnlyList<CompanionRoleFactResponse> facts,
        TaiwuLanguage language)
    {
        var group = ContextBoolean(
            facts,
            CandidateProfileField.RosterMembership);
        var village = ContextBoolean(
            facts,
            CandidateProfileField.VillageWorkCandidateMembership);
        var key = (group, village) switch
        {
            (true, true) => CompanionFinderUiTextKey.GroupAndVillageCandidate,
            (true, false) => CompanionFinderUiTextKey.CurrentGroupCandidate,
            (false, true) => CompanionFinderUiTextKey.VillageWorkCandidate,
            _ => CompanionFinderUiTextKey.Unavailable
        };
        return Text(language, key);
    }

    private static string CurrentAge(
        IReadOnlyList<CompanionRoleFactResponse> facts,
        TaiwuLanguage language)
    {
        var fact = facts.SingleOrDefault(value => value.Field
            == CandidateProfileField.CurrentAge);
        return fact?.EvidenceState == CompanionFactEvidenceState.Confirmed
            && fact.Value?.Int16 is { } age
                ? age.ToString(CultureInfo.InvariantCulture)
                : Text(language, CompanionFinderUiTextKey.Unavailable);
    }

    private static bool? ContextBoolean(
        IReadOnlyList<CompanionRoleFactResponse> facts,
        CandidateProfileField field)
    {
        var fact = facts.SingleOrDefault(value => value.Field == field);
        return fact?.EvidenceState == CompanionFactEvidenceState.Confirmed
            ? fact.Value?.Boolean
            : null;
    }

    private static CompanionCapabilitySummaryViewModel MapCapabilitySummary(
        CompanionCapabilitySummaryResponse summary,
        TaiwuLanguage language,
        IReadOnlyList<CompanionDisciplineOptionViewModel> disciplineOptions) =>
        new(
            summary.State,
            summary.RuleVersion,
            summary.Formula,
            ScoreLabel(summary.BreadthIndex, summary.State, language),
            MapCapabilityCategory(
                summary.MainAttributes,
                language,
                disciplineOptions),
            MapCapabilityCategory(
                summary.MartialDisciplines,
                language,
                disciplineOptions),
            MapCapabilityCategory(
                summary.LifeSkillDisciplines,
                language,
                disciplineOptions));

    private static CompanionCapabilityCategoryViewModel MapCapabilityCategory(
        CompanionCapabilityCategoryResponse category,
        TaiwuLanguage language,
        IReadOnlyList<CompanionDisciplineOptionViewModel> disciplineOptions)
    {
        var topValues = category.State == CompanionCapabilitySummaryState.Complete
            ? category.Components
                .Where(component => component.Value.HasValue)
                .OrderByDescending(component => component.Value)
                .ThenBy(ComponentOrder)
                .Select(component => new
                {
                    Label = CapabilityComponentLabel(
                        component,
                        language,
                        disciplineOptions),
                    Value = component.Value!.Value
                })
                .Where(component => component.Label is not null)
                .Take(3)
                .Select(component => new CompanionCapabilityTopValueViewModel(
                    component.Label!,
                    component.Value))
                .ToArray()
            : [];
        return new CompanionCapabilityCategoryViewModel(
            category.Category,
            category.State,
            Text(language, category.Category switch
            {
                CompanionCapabilityCategory.MainAttributes =>
                    CompanionFinderUiTextKey.MainAttributeAverage,
                CompanionCapabilityCategory.MartialDisciplines =>
                    CompanionFinderUiTextKey.MartialAptitudeAverage,
                CompanionCapabilityCategory.LifeSkillDisciplines =>
                    CompanionFinderUiTextKey.LifeSkillAptitudeAverage,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(category),
                    category.Category,
                    "Unknown capability category.")
            }),
            ScoreLabel(category.Average, category.State, language),
            $"{category.ConfirmedCount.ToString(CultureInfo.InvariantCulture)}/"
                + category.ExpectedCount.ToString(CultureInfo.InvariantCulture),
            topValues);
    }

    private static string? CapabilityComponentLabel(
        CompanionCapabilityComponentResponse component,
        TaiwuLanguage language,
        IReadOnlyList<CompanionDisciplineOptionViewModel> disciplineOptions)
    {
        if (component.MainAttribute.HasValue)
        {
            return CompanionFinderUiText.MainAttributeLabel(
                language,
                component.MainAttribute.Value);
        }

        if (!component.DisciplineDomain.HasValue
            || !component.DisciplineType.HasValue)
        {
            return null;
        }

        return disciplineOptions.SingleOrDefault(option =>
            option.Domain == component.DisciplineDomain.Value
            && option.Type == component.DisciplineType.Value
            && option.NameAvailable)?.DisplayName;
    }

    private static int ComponentOrder(
        CompanionCapabilityComponentResponse component) =>
        component.MainAttribute.HasValue
            ? (int)component.MainAttribute.Value
            : component.DisciplineType ?? int.MaxValue;

    private static string ScoreLabel(
        decimal? score,
        CompanionCapabilitySummaryState state,
        TaiwuLanguage language) => score?.ToString(
        "0.##",
        CultureInfo.InvariantCulture) ?? EvidenceLabel(
        state switch
        {
            CompanionCapabilitySummaryState.Incomplete =>
                CompanionFactEvidenceState.Incomplete,
            CompanionCapabilitySummaryState.Unsupported =>
                CompanionFactEvidenceState.Unsupported,
            CompanionCapabilitySummaryState.Stale =>
                CompanionFactEvidenceState.Stale,
            CompanionCapabilitySummaryState.Conflicting =>
                CompanionFactEvidenceState.Conflicting,
            CompanionCapabilitySummaryState.Complete =>
                CompanionFactEvidenceState.Missing,
            _ => throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "Unknown capability summary state.")
        },
        language);

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

    private static string Text(
        TaiwuLanguage language,
        CompanionFinderUiTextKey key) =>
        CompanionFinderUiText.Get(language, key);
}
