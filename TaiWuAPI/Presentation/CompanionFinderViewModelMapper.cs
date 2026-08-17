using System.Globalization;
using TaiWu.Application.CombatSkills;
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
        string disciplineName,
        IReadOnlyList<CompanionDisciplineOptionViewModel>? disciplineOptions = null)
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
            language,
            disciplineOptions ?? [])).ToArray();
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

    public static CompanionFinderEnrichmentViewModel MapEnrichment(
        CompanionCandidateEnrichmentStatus status,
        CombatSkillCatalogueStatus catalogueStatus,
        TaiwuLanguage language)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown companion enrichment status.");
        }

        if (!Enum.IsDefined(catalogueStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(catalogueStatus),
                catalogueStatus,
                "Unknown combat-skill catalogue status.");
        }

        var (title, message) = (status, catalogueStatus) switch
        {
            (CompanionCandidateEnrichmentStatus.Complete,
                CombatSkillCatalogueStatus.Current) => (
                CompanionFinderUiTextKey.EnrichmentCurrentTitle,
                CompanionFinderUiTextKey.EnrichmentCurrentMessage),
            (CompanionCandidateEnrichmentStatus.Partial,
                CombatSkillCatalogueStatus.Current) => (
                CompanionFinderUiTextKey.CandidateEvidencePartialTitle,
                CompanionFinderUiTextKey.CandidateEvidencePartialMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueMissing,
                CombatSkillCatalogueStatus.MissingSources) => (
                    CompanionFinderUiTextKey.CatalogueSourcesMissingTitle,
                    CompanionFinderUiTextKey.CatalogueSourcesMissingMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueMissing,
                CombatSkillCatalogueStatus.Missing) => (
                CompanionFinderUiTextKey.CatalogueMissingTitle,
                CompanionFinderUiTextKey.CatalogueMissingMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueStale,
                CombatSkillCatalogueStatus.Stale) => (
                CompanionFinderUiTextKey.CatalogueStaleTitle,
                CompanionFinderUiTextKey.CatalogueStaleMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueRebuilding,
                CombatSkillCatalogueStatus.Rebuilding) => (
                CompanionFinderUiTextKey.CatalogueRebuildingTitle,
                CompanionFinderUiTextKey.CatalogueRebuildingMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueUnsupported,
                CombatSkillCatalogueStatus.UnsupportedVersion) => (
                CompanionFinderUiTextKey.CatalogueUnsupportedTitle,
                CompanionFinderUiTextKey.CatalogueUnsupportedMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.SourceReadFailed) => (
                    CompanionFinderUiTextKey.CatalogueSourceReadFailedTitle,
                    CompanionFinderUiTextKey.CatalogueSourceReadFailedMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.RepositoryFailed) => (
                    CompanionFinderUiTextKey.CatalogueRepositoryFailedTitle,
                    CompanionFinderUiTextKey.CatalogueRepositoryFailedMessage),
            (CompanionCandidateEnrichmentStatus.CatalogueFailed,
                CombatSkillCatalogueStatus.Corrupt) => (
                    CompanionFinderUiTextKey.CatalogueCorruptTitle,
                    CompanionFinderUiTextKey.CatalogueCorruptMessage),
            _ => throw new ArgumentException(
                "The enrichment and catalogue states are not a supported presentation combination.",
                nameof(status))
        };
        return new CompanionFinderEnrichmentViewModel(
            status,
            catalogueStatus,
            Text(language, title),
            Text(language, message),
            status != CompanionCandidateEnrichmentStatus.Complete);
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
                first.EvaluationStateLabel,
                second.EvaluationStateLabel),
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
        var capability = new CompanionCapabilityComparisonViewModel(
            Text(language, CompanionFinderUiTextKey.CapabilityOverview),
            Text(language, CompanionFinderUiTextKey.CapabilityLimitation),
            [
                new CompanionCapabilityComparisonFactViewModel(
                    Text(language, CompanionFinderUiTextKey.BreadthIndex),
                    first.CapabilitySummary.BreadthIndexLabel,
                    string.Empty,
                    second.CapabilitySummary.BreadthIndexLabel,
                    string.Empty),
                CapabilityFact(
                    first.CapabilitySummary.MainAttributes,
                    second.CapabilitySummary.MainAttributes,
                    language),
                CapabilityFact(
                    first.CapabilitySummary.MartialDisciplines,
                    second.CapabilitySummary.MartialDisciplines,
                    language),
                CapabilityFact(
                    first.CapabilitySummary.LifeSkillDisciplines,
                    second.CapabilitySummary.LifeSkillDisciplines,
                    language)
            ]);
        return new CompanionComparisonViewModel(
            first.DisplayName,
            second.DisplayName,
            CompanionFinderApiText.ComparisonOutcome(
                language,
                comparison.Outcome),
            capability,
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
        TaiwuLanguage language,
        IReadOnlyList<CompanionDisciplineOptionViewModel> disciplineOptions)
    {
        var scoreFact = candidate.ScoreFacts.SingleOrDefault();
        var evidence = scoreFact?.EvidenceState
            ?? CompanionFactEvidenceState.Missing;
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

    private static CompanionCapabilityComparisonFactViewModel CapabilityFact(
        CompanionCapabilityCategoryViewModel first,
        CompanionCapabilityCategoryViewModel second,
        TaiwuLanguage language) => new(
        first.Label,
        first.ScoreLabel,
        CapabilityDetail(first, language),
        second.ScoreLabel,
        CapabilityDetail(second, language));

    private static string CapabilityDetail(
        CompanionCapabilityCategoryViewModel category,
        TaiwuLanguage language)
    {
        var coverage = $"{Text(language, CompanionFinderUiTextKey.ConfirmedCoverage)}: "
            + category.CoverageLabel;
        if (category.TopValues.Count == 0)
        {
            return coverage;
        }

        return coverage + "; "
            + Text(language, CompanionFinderUiTextKey.TopValues)
            + ": "
            + string.Join(
                ", ",
                category.TopValues.Select(value =>
                    $"{value.Label} {value.Value.ToString(CultureInfo.InvariantCulture)}"));
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

    private static string GateSummary(
        CompanionCandidateViewModel candidate,
        TaiwuLanguage language) => candidate.Gates.Count == 0
        ? Text(language, CompanionFinderUiTextKey.Unavailable)
        : string.Join(
            "; ",
            candidate.Gates.Select(value =>
                $"{value.RequirementLabel} — {value.OutcomeLabel}: "
                + value.Explanation));

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
