using TaiWu.Application.Localization;
using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using TaiWuAPI.Contracts.VillageWorkforce;
using TaiWuAPI.Localization;

namespace TaiWuAPI.Presentation;

public static class VillageWorkforceViewModelMapper
{
    public static VillageWorkforceViewModel Map(
        VillageWorkforceFinderResult result,
        TaiwuLanguage language,
        int targetOrdinal)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.HasAuthoritativeResult)
        {
            throw new ArgumentException(
                "An authoritative workforce result is required.",
                nameof(result));
        }

        var response = VillageWorkforceResponseMapper.Map(
            result,
            ApiLanguage(language));
        var candidates = response.Candidates.Select((candidate, index) =>
            MapCandidate(candidate, index + 1, language)).ToArray();
        return new VillageWorkforceViewModel(
            response.Status,
            response.Source!.CapturedAtUtc,
            response.Status == VillageWorkforceApiStatus.Partial,
            VillageWorkforceUiText.Get(
                language,
                VillageWorkforceUiTextKey.ObjectiveLabel),
            VillageWorkforceUiText.Get(
                language,
                VillageWorkforceUiTextKey.ObjectiveDescription),
            response.Objective!.RuleVersion,
            VillageWorkforceUiText.TargetLabel(language, targetOrdinal),
            response.Counts!,
            candidates.Single(candidate => candidate.IsCurrent),
            candidates,
            response.Limitations.Select(item => item.Message).ToArray());
    }

    public static VillageWorkforceComparisonViewModel MapComparison(
        VillageWorkforceFinderResult result,
        VillageWorkforceViewModel model,
        int firstCharacterId,
        int secondCharacterId,
        TaiwuLanguage language)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(model);
        var shortlist = result.Shortlist
            ?? throw new ArgumentException(
                "An authoritative workforce result is required.",
                nameof(result));
        var comparison = shortlist.Compare(
            new VillageWorkerIdentity(firstCharacterId),
            new VillageWorkerIdentity(secondCharacterId));
        var mapped = VillageWorkforceResponseMapper.MapComparison(
            comparison,
            ApiLanguage(language));
        var first = model.Candidates.Single(item =>
            item.CharacterId == firstCharacterId);
        var second = model.Candidates.Single(item =>
            item.CharacterId == secondCharacterId);
        var alternative = first.IsCurrent && second.State is
                VillageWorkforceApiEvaluationState.Ranked
                or VillageWorkforceApiEvaluationState.Tied
            ? second
            : second.IsCurrent && first.State is
                VillageWorkforceApiEvaluationState.Ranked
                or VillageWorkforceApiEvaluationState.Tied
                ? first
                : null;
        var checklist = alternative is null
            ? []
            : VillageWorkforceResponseMapper.MapManualPlan(
                    shortlist.CreateManualPlan(
                        new VillageWorkerIdentity(alternative.CharacterId)),
                    ApiLanguage(language))
                .Checklist.Select(item => item.Message).ToArray();
        return new VillageWorkforceComparisonViewModel(
            first,
            second,
            mapped.OutcomeLabel,
            mapped.FirstValue,
            mapped.SecondValue,
            VillageWorkforceUiText.Get(
                language,
                VillageWorkforceUiTextKey.QualificationPoints),
            checklist);
    }

    public static VillageWorkforceNoticeViewModel MapFailure(
        VillageWorkforceFinderStatus status,
        TaiwuLanguage language) => status switch
        {
            VillageWorkforceFinderStatus.SaveUnavailable => Notice(
                language,
                VillageWorkforceUiTextKey.SaveUnavailableTitle,
                VillageWorkforceUiTextKey.SaveUnavailableMessage,
                true),
            VillageWorkforceFinderStatus.UnsupportedSourceVersion
                or VillageWorkforceFinderStatus.UnsupportedRule => Notice(
                    language,
                    VillageWorkforceUiTextKey.UnsupportedSourceTitle,
                    VillageWorkforceUiTextKey.UnsupportedSourceMessage,
                    false),
            VillageWorkforceFinderStatus.ConflictingSources => Notice(
                language,
                VillageWorkforceUiTextKey.ConflictingSourcesTitle,
                VillageWorkforceUiTextKey.ConflictingSourcesMessage,
                true),
            VillageWorkforceFinderStatus.ChangedRevision => Notice(
                language,
                VillageWorkforceUiTextKey.ChangedRevisionTitle,
                VillageWorkforceUiTextKey.ChangedRevisionMessage,
                true),
            VillageWorkforceFinderStatus.TargetNotFound => Notice(
                language,
                VillageWorkforceUiTextKey.TargetMissingTitle,
                VillageWorkforceUiTextKey.TargetMissingMessage,
                false),
            _ => Notice(
                language,
                VillageWorkforceUiTextKey.ReadFailedTitle,
                VillageWorkforceUiTextKey.ReadFailedMessage,
                true)
        };

    public static VillageWorkforceNoticeViewModel MapDiscoveryFailure(
        VillageWorkforceSnapshotReadStatus status,
        TaiwuLanguage language) => status switch
        {
            VillageWorkforceSnapshotReadStatus.SaveUnavailable => Notice(
                language,
                VillageWorkforceUiTextKey.SaveUnavailableTitle,
                VillageWorkforceUiTextKey.SaveUnavailableMessage,
                true),
            VillageWorkforceSnapshotReadStatus.UnsupportedVersion => Notice(
                language,
                VillageWorkforceUiTextKey.UnsupportedSourceTitle,
                VillageWorkforceUiTextKey.UnsupportedSourceMessage,
                false),
            VillageWorkforceSnapshotReadStatus.ConflictingSources => Notice(
                language,
                VillageWorkforceUiTextKey.ConflictingSourcesTitle,
                VillageWorkforceUiTextKey.ConflictingSourcesMessage,
                true),
            VillageWorkforceSnapshotReadStatus.ChangedRevision => Notice(
                language,
                VillageWorkforceUiTextKey.ChangedRevisionTitle,
                VillageWorkforceUiTextKey.ChangedRevisionMessage,
                true),
            _ => Notice(
                language,
                VillageWorkforceUiTextKey.ReadFailedTitle,
                VillageWorkforceUiTextKey.ReadFailedMessage,
                true)
        };

    private static VillageWorkforceCandidateViewModel MapCandidate(
        VillageWorkforceCandidateResponse candidate,
        int ordinal,
        TaiwuLanguage language)
    {
        var requirements = candidate.Requirements.Select(item =>
            new VillageWorkforceRequirementViewModel(
                item.Order,
                item.Explanation,
                item.OutcomeLabel,
                item.Outcome == VillageWorkforceApiRequirementOutcome.Passed,
                item.Evidence.Select(evidence => MapProvenance(
                    evidence,
                    language)).ToArray(),
                item.Conflicts.Count)).ToArray();
        var components = candidate.Components.Select(item =>
            new VillageWorkforceComponentViewModel(
                item.Explanation,
                item.Contribution,
                VillageWorkforceUiText.Get(
                    language,
                    VillageWorkforceUiTextKey.QualificationPoints),
                item.Evidence.Select(evidence => MapProvenance(
                    evidence,
                    language)).ToArray())).ToArray();
        var stateLabel = StateLabel(candidate.EvaluationState, language);
        var decisive = requirements.FirstOrDefault(item => !item.Passed)
            ?.Explanation
            ?? stateLabel;
        return new VillageWorkforceCandidateViewModel(
            candidate.CharacterId,
            ordinal,
            VillageWorkforceUiText.WorkerLabel(
                language,
                ordinal,
                candidate.IsCurrent),
            candidate.IsCurrent,
            candidate.EvaluationState,
            stateLabel,
            StateCssClass(candidate.EvaluationState),
            candidate.CompetitionRank,
            candidate.Total,
            VillageWorkforceUiText.Get(
                language,
                VillageWorkforceUiTextKey.QualificationPoints),
            decisive,
            requirements.Count(item => item.Passed),
            requirements,
            components);
    }

    private static VillageWorkforceProvenanceViewModel MapProvenance(
        VillageWorkforceEvidenceResponse evidence,
        TaiwuLanguage language) => new(
            VillageWorkforceUiText.Source(language, evidence.Source),
            evidence.SourceVersion);

    private static string StateLabel(
        VillageWorkforceApiEvaluationState state,
        TaiwuLanguage language) => VillageWorkforceUiText.Get(
        language,
        state switch
        {
            VillageWorkforceApiEvaluationState.Ranked =>
                VillageWorkforceUiTextKey.Ranked,
            VillageWorkforceApiEvaluationState.Tied =>
                VillageWorkforceUiTextKey.Tied,
            VillageWorkforceApiEvaluationState.CurrentOnly =>
                VillageWorkforceUiTextKey.CurrentOnly,
            VillageWorkforceApiEvaluationState.Ineligible =>
                VillageWorkforceUiTextKey.Ineligible,
            VillageWorkforceApiEvaluationState.Incomplete =>
                VillageWorkforceUiTextKey.Incomplete,
            VillageWorkforceApiEvaluationState.Unsupported =>
                VillageWorkforceUiTextKey.Unsupported,
            VillageWorkforceApiEvaluationState.Conflicting =>
                VillageWorkforceUiTextKey.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        });

    private static string StateCssClass(
        VillageWorkforceApiEvaluationState state) => state switch
        {
            VillageWorkforceApiEvaluationState.Ranked
                or VillageWorkforceApiEvaluationState.Tied => "comparable",
            VillageWorkforceApiEvaluationState.Ineligible => "ineligible",
            _ => "needs-review"
        };

    private static VillageWorkforceApiLanguage ApiLanguage(
        TaiwuLanguage language) => language switch
        {
            TaiwuLanguage.English => VillageWorkforceApiLanguage.English,
            TaiwuLanguage.Chinese =>
                VillageWorkforceApiLanguage.TraditionalChinese,
            _ => throw new ArgumentOutOfRangeException(nameof(language))
        };

    private static VillageWorkforceNoticeViewModel Notice(
        TaiwuLanguage language,
        VillageWorkforceUiTextKey title,
        VillageWorkforceUiTextKey message,
        bool retry) => new(
            VillageWorkforceUiText.Get(language, title),
            VillageWorkforceUiText.Get(language, message),
            retry);
}
