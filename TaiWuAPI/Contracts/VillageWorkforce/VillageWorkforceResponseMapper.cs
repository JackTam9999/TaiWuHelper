using TaiWu.Application.VillageWorkforce;
using TaiWu.Domain.VillageWorkforce;
using TaiWuAPI.Localization;
using System.Globalization;

namespace TaiWuAPI.Contracts.VillageWorkforce;

internal static class VillageWorkforceResponseMapper
{
    public static VillageWorkforceDiscoveryResponse MapDiscovery(
        VillageWorkforceSnapshotReadResult read,
        VillageWorkforceApiLanguage language)
    {
        ArgumentNullException.ThrowIfNull(read);
        if (read.Snapshot is null)
        {
            var (status, identity) = MapReadFailure(read.Status);
            return new VillageWorkforceDiscoveryResponse(
                status,
                Failure(identity, language),
                [Objective(language)],
                []);
        }

        return new VillageWorkforceDiscoveryResponse(
            read.Status == VillageWorkforceSnapshotReadStatus.Partial
                ? VillageWorkforceApiStatus.Partial
                : VillageWorkforceApiStatus.Complete,
            Failure: null,
            [Objective(language)],
            read.Snapshot.Targets.Select(target => MapTarget(
                target,
                read.TargetDisplays.SingleOrDefault(display =>
                    display.Identity == target.Identity),
                language)).ToArray());
    }

    public static VillageWorkforceResultResponse Map(
        VillageWorkforceFinderResult result,
        VillageWorkforceApiLanguage language)
    {
        ArgumentNullException.ThrowIfNull(result);
        var status = MapStatus(result.Status);
        if (!result.HasAuthoritativeResult)
        {
            return new VillageWorkforceResultResponse(
                status,
                Failure(
                    result.FailureIdentity ?? "VILLAGE_WORKFORCE_FAILED",
                    language),
                Fingerprint: null,
                Source: null,
                Objective: null,
                Target: null,
                CurrentAssignment: null,
                Counts: null,
                Candidates: [],
                VisibleCandidateReferences: [],
                Limitations: [],
                Comparison: null,
                ManualPlan: null,
                Diagnostics: []);
        }

        var snapshot = result.Snapshot!;
        var rule = result.Rule!;
        var evaluationSet = result.EvaluationSet!;
        var shortlist = result.Shortlist!;
        var target = snapshot.Targets.Single(item =>
            item.Identity == evaluationSet.ResultIdentity.Target);
        var currentAssignment = snapshot.CurrentAssignments.Single(item =>
            item.Target == target.Identity);
        var ranks = shortlist.Comparable.ToDictionary(
            item => item.Evaluation.Worker,
            item => item.CompetitionRank);
        var candidates = shortlist.ApplyFilter(WorkforceShortlistFilter.All)
            .VisibleEvaluations
            .Select(evaluation => MapCandidate(
                evaluation,
                snapshot.Workers.Single(worker =>
                    worker.Identity == evaluation.Worker),
                evaluation.Worker == evaluationSet.CurrentWorker,
                ranks.GetValueOrDefault(evaluation.Worker),
                rule,
                result.WorkerDisplays.SingleOrDefault(display =>
                    display.Identity == evaluation.Worker),
                language))
            .ToArray();
        var visible = result.View!.VisibleEvaluations
            .Select(item => WorkerReference(item.Worker))
            .ToArray();
        var diagnostics = snapshot.Diagnostics
            .Select(item => MapDiagnostic(
                item,
                "snapshot",
                workerReference: null,
                language))
            .Concat(snapshot.Workers.SelectMany(worker =>
                worker.Diagnostics.Select(item => MapDiagnostic(
                    item,
                    "worker",
                    WorkerReference(worker.Identity),
                    language))))
            .ToArray();

        return new VillageWorkforceResultResponse(
            status,
            result.FailureIdentity is null
                ? null
                : Failure(result.FailureIdentity, language),
            result.Fingerprint,
            new VillageWorkforceSourceResponse(
                snapshot.CapturedAt,
                MapSnapshotStatus(result.SnapshotReadStatus!.Value),
                snapshot.SourceVersions.GameDataVersion,
                snapshot.SourceVersions.MappingVersion,
                snapshot.SourceVersions.CandidateUniverseVersion,
                snapshot.SourceVersions.FingerprintSchemaVersion),
            Objective(language, rule),
            MapTarget(
                target,
                result.TargetDisplays.SingleOrDefault(display =>
                    display.Identity == target.Identity),
                language),
            new VillageWorkforceCurrentAssignmentResponse(
                TargetReference(target.Identity),
                WorkerReference(currentAssignment.Worker),
                currentAssignment.Worker.CharacterId,
                WorkerLabel(
                    language,
                    currentAssignment.Worker,
                    result.WorkerDisplays.SingleOrDefault(display =>
                        display.Identity == currentAssignment.Worker))),
            new VillageWorkforceCountsResponse(
                shortlist.Counts.Total,
                shortlist.Counts.Comparable,
                shortlist.Counts.Ranked,
                shortlist.Counts.Tied,
                shortlist.Counts.CurrentOnly,
                shortlist.Counts.Ineligible,
                shortlist.Counts.Incomplete,
                shortlist.Counts.Unsupported,
                shortlist.Counts.Conflicting,
                visible.Length),
            candidates,
            visible,
            shortlist.Limitations.Select(item =>
                new VillageWorkforceLimitationResponse(
                    item.Identity,
                    VillageWorkforceApiText.Limitation(
                        language,
                        item.Identity))).ToArray(),
            result.Comparison is null
                ? null
                : MapComparison(result.Comparison, language),
            result.ManualPlan is null
                ? null
                : MapManualPlan(result.ManualPlan, language),
            diagnostics);
    }

    public static bool TryParseLanguage(
        string? value,
        out VillageWorkforceApiLanguage language)
    {
        language = value switch
        {
            VillageWorkforceApiTokens.English =>
                VillageWorkforceApiLanguage.English,
            VillageWorkforceApiTokens.TraditionalChinese =>
                VillageWorkforceApiLanguage.TraditionalChinese,
            _ => (VillageWorkforceApiLanguage)(-1)
        };
        return Enum.IsDefined(language);
    }

    public static bool TryParseFilter(
        string? value,
        out WorkforceShortlistFilter filter)
    {
        filter = value switch
        {
            VillageWorkforceApiTokens.FilterAll =>
                WorkforceShortlistFilter.All,
            VillageWorkforceApiTokens.FilterComparable =>
                WorkforceShortlistFilter.Comparable,
            VillageWorkforceApiTokens.FilterNeedsReview =>
                WorkforceShortlistFilter.NeedsReview,
            VillageWorkforceApiTokens.FilterIneligible =>
                WorkforceShortlistFilter.Ineligible,
            _ => (WorkforceShortlistFilter)(-1)
        };
        return Enum.IsDefined(filter);
    }

    private static VillageWorkforceCandidateResponse MapCandidate(
        WorkforceEvaluation evaluation,
        VillageWorkerProfile profile,
        bool isCurrent,
        int competitionRank,
        WorkforceRuleDefinition rule,
        VillageWorkerDisplay? display,
        VillageWorkforceApiLanguage language)
    {
        var state = MapEvaluationState(evaluation.State);
        var definitions = rule.Requirements.ToDictionary(
            item => item.Requirement);
        return new VillageWorkforceCandidateResponse(
            WorkerReference(evaluation.Worker),
            evaluation.Worker.CharacterId,
            WorkerLabel(language, evaluation.Worker, display),
            WorkerLocation(language, display),
            isCurrent,
            MapWorkerState(evaluation.WorkerState),
            state,
            VillageWorkforceApiText.EvaluationState(language, state),
            evaluation.IsRankable ? competitionRank : null,
            evaluation.Result?.Value,
            evaluation.Result is null ? null : UnitToken(evaluation.Result.Unit),
            evaluation.Requirements.Select(item => MapRequirement(
                item,
                definitions[item.Requirement].Order,
                language)).ToArray(),
            evaluation.Components.Select(item =>
                new VillageWorkforceComponentResponse(
                    "REQUIRED_BASE_LIFE_SKILL_QUALIFICATION",
                    item.Identity.Discipline.Type,
                    item.RawValue,
                    item.NormalizedValue,
                    item.Weight,
                    item.Contribution,
                    UnitToken(item.Unit),
                    item.ExplanationIdentity,
                    VillageWorkforceApiText.Component(language),
                    item.Evidence.Select(MapEvidence).ToArray()))
                .ToArray(),
            profile.Diagnostics.Select(item => MapDiagnostic(
                item,
                "worker",
                WorkerReference(profile.Identity),
                language)).ToArray());
    }

    private static VillageWorkforceRequirementResponse MapRequirement(
        WorkforceRequirementEvaluation requirement,
        int order,
        VillageWorkforceApiLanguage language)
    {
        var kind = MapRequirementKind(requirement.Requirement);
        var outcome = MapRequirementOutcome(requirement.Outcome);
        return new VillageWorkforceRequirementResponse(
            order,
            kind,
            outcome,
            VillageWorkforceApiText.RequirementOutcome(language, outcome),
            requirement.ReasonIdentity,
            VillageWorkforceApiText.Requirement(language, kind),
            requirement.Evidence.Select(MapEvidence).ToArray(),
            requirement.Conflicts.Select(MapConflict).ToArray());
    }

    private static VillageWorkforceEvidenceResponse MapEvidence(
        WorkforceEvidenceReference evidence) =>
        new(
            evidence.ReferenceIdentity,
            MapEvidenceSource(evidence.Provenance.SourceKind),
            evidence.Provenance.SourceVersion);

    private static VillageWorkforceConflictResponse MapConflict(
        WorkforceConflictValue conflict)
    {
        var kind = MapValueKind(conflict.Value.Kind);
        return new VillageWorkforceConflictResponse(
            kind,
            kind == VillageWorkforceApiValueKind.Boolean
                ? conflict.Value.BooleanValue
                : null,
            kind == VillageWorkforceApiValueKind.Int16
                ? conflict.Value.Int16Value
                : null,
            kind == VillageWorkforceApiValueKind.Int32
                ? conflict.Value.Int32Value
                : null,
            MapEvidenceSource(conflict.Provenance.SourceKind),
            conflict.Provenance.SourceVersion);
    }

    internal static VillageWorkforceComparisonResponse MapComparison(
        WorkforceComparison comparison,
        VillageWorkforceApiLanguage language)
    {
        var outcome = MapComparisonOutcome(comparison.Outcome);
        var firstUnit = comparison.First.Result?.Unit;
        var secondUnit = comparison.Second.Result?.Unit;
        return new VillageWorkforceComparisonResponse(
            WorkerReference(comparison.First.Worker),
            WorkerReference(comparison.Second.Worker),
            outcome,
            VillageWorkforceApiText.Comparison(language, outcome),
            comparison.First.Result?.Value,
            comparison.Second.Result?.Value,
            firstUnit.HasValue && firstUnit == secondUnit
                ? UnitToken(firstUnit.Value)
                : null);
    }

    internal static VillageWorkforceManualPlanResponse MapManualPlan(
        VillageWorkforceManualPlan plan,
        VillageWorkforceApiLanguage language) =>
        new(
            WorkerReference(plan.CurrentWorker),
            WorkerReference(plan.ProposedAssignment.Worker),
            plan.Checklist.Select(item =>
            {
                var kind = MapChecklistKind(item.Kind);
                return new VillageWorkforceChecklistItemResponse(
                    kind,
                    MapChecklistCategory(item.Category),
                    VillageWorkforceApiText.Checklist(language, kind));
            }).ToArray());

    private static VillageWorkforceDiagnosticResponse MapDiagnostic(
        WorkforceDiagnostic diagnostic,
        string scope,
        string? workerReference,
        VillageWorkforceApiLanguage language) =>
        new(
            scope,
            diagnostic.Code,
            MapSeverity(diagnostic.Severity),
            VillageWorkforceApiText.Diagnostic(language),
            workerReference);

    private static VillageWorkforceObjectiveResponse Objective(
        VillageWorkforceApiLanguage language,
        WorkforceRuleDefinition? rule = null) =>
        new(
            "village-workforce-objective:shop-manager-base-life-skill-qualification:v1",
            VillageWorkforceApiTokens.Objective,
            VillageWorkforceApiTokens.ObjectiveVersion,
            rule?.Version.Value ?? VerifiedVillageWorkforceRules.RuleVersion,
            VillageWorkforceApiText.ObjectiveLabel(language),
            VillageWorkforceApiText.ObjectiveDescription(language),
            VillageWorkforceApiText.Unit(language));

    private static VillageWorkforceTargetResponse MapTarget(
        ShopManagerTarget target,
        VillageWorkforceTargetDisplay? display,
        VillageWorkforceApiLanguage language) =>
        new(
            TargetReference(target.Identity),
            target.Identity.Building.AreaId,
            target.Identity.Building.BlockId,
            target.Identity.Building.BuildingBlockIndex,
            target.Identity.ManagerSlotIndex,
            target.RequiredDiscipline.Type,
            VillageWorkforceApiVacancyState.NoExplicitVacancy,
            TargetLabel(target, display, language));

    private static string TargetLabel(
        ShopManagerTarget target,
        VillageWorkforceTargetDisplay? display,
        VillageWorkforceApiLanguage language)
    {
        var building = language == VillageWorkforceApiLanguage.TraditionalChinese
            ? display?.TraditionalChineseBuildingName
            : display?.EnglishBuildingName;
        var location = language == VillageWorkforceApiLanguage.TraditionalChinese
            ? display?.TraditionalChineseLocation
            : display?.EnglishLocation;
        var discipline = language == VillageWorkforceApiLanguage.TraditionalChinese
            ? display?.TraditionalChineseDisciplineName
            : display?.EnglishDisciplineName;
        if (building is null)
        {
            return VillageWorkforceApiText.Target(
                language,
                target.Identity.Building.AreaId,
                target.Identity.Building.BlockId,
                target.Identity.Building.BuildingBlockIndex,
                target.Identity.ManagerSlotIndex,
                target.RequiredDiscipline.Type);
        }

        var position = language == VillageWorkforceApiLanguage.TraditionalChinese
            ? $"管理位置 {target.Identity.ManagerSlotIndex + 1}"
            : $"Manager position {target.Identity.ManagerSlotIndex + 1}";
        return string.Join(
            " · ",
            new[] { building, location, position, discipline }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string WorkerLabel(
        VillageWorkforceApiLanguage language,
        VillageWorkerIdentity worker,
        VillageWorkerDisplay? display) =>
        (language == VillageWorkforceApiLanguage.TraditionalChinese
            ? display?.TraditionalChineseName
            : display?.EnglishName)
        ?? VillageWorkforceApiText.Worker(language, worker.CharacterId);

    private static string? WorkerLocation(
        VillageWorkforceApiLanguage language,
        VillageWorkerDisplay? display) =>
        language == VillageWorkforceApiLanguage.TraditionalChinese
            ? display?.TraditionalChineseLocation
            : display?.EnglishLocation;

    private static VillageWorkforceFailureResponse Failure(
        string identity,
        VillageWorkforceApiLanguage language) =>
        new(identity, VillageWorkforceApiText.Failure(language, identity));

    private static (VillageWorkforceApiStatus Status, string Identity)
        MapReadFailure(VillageWorkforceSnapshotReadStatus status) =>
        status switch
        {
            VillageWorkforceSnapshotReadStatus.SaveUnavailable =>
                (VillageWorkforceApiStatus.SaveUnavailable,
                    "VILLAGE_WORKFORCE_SAVE_UNAVAILABLE"),
            VillageWorkforceSnapshotReadStatus.UnsupportedVersion =>
                (VillageWorkforceApiStatus.UnsupportedSourceVersion,
                    "VILLAGE_WORKFORCE_SOURCE_VERSION_UNSUPPORTED"),
            VillageWorkforceSnapshotReadStatus.ConflictingSources =>
                (VillageWorkforceApiStatus.ConflictingSources,
                    "VILLAGE_WORKFORCE_SOURCES_CONFLICTING"),
            VillageWorkforceSnapshotReadStatus.ChangedRevision =>
                (VillageWorkforceApiStatus.ChangedRevision,
                    "VILLAGE_WORKFORCE_SAVE_REVISION_CHANGED"),
            VillageWorkforceSnapshotReadStatus.ReadFailed =>
                (VillageWorkforceApiStatus.ReadFailed,
                    "VILLAGE_WORKFORCE_SNAPSHOT_READ_FAILED"),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static VillageWorkforceApiStatus MapStatus(
        VillageWorkforceFinderStatus status) => status switch
        {
            VillageWorkforceFinderStatus.Complete =>
                VillageWorkforceApiStatus.Complete,
            VillageWorkforceFinderStatus.Partial =>
                VillageWorkforceApiStatus.Partial,
            VillageWorkforceFinderStatus.InvalidRequest =>
                VillageWorkforceApiStatus.InvalidRequest,
            VillageWorkforceFinderStatus.SaveUnavailable =>
                VillageWorkforceApiStatus.SaveUnavailable,
            VillageWorkforceFinderStatus.UnsupportedSourceVersion =>
                VillageWorkforceApiStatus.UnsupportedSourceVersion,
            VillageWorkforceFinderStatus.ConflictingSources =>
                VillageWorkforceApiStatus.ConflictingSources,
            VillageWorkforceFinderStatus.ChangedRevision =>
                VillageWorkforceApiStatus.ChangedRevision,
            VillageWorkforceFinderStatus.ReadFailed =>
                VillageWorkforceApiStatus.ReadFailed,
            VillageWorkforceFinderStatus.TargetNotFound =>
                VillageWorkforceApiStatus.TargetNotFound,
            VillageWorkforceFinderStatus.UnsupportedRule =>
                VillageWorkforceApiStatus.UnsupportedRule,
            VillageWorkforceFinderStatus.InvalidComparison =>
                VillageWorkforceApiStatus.InvalidComparison,
            VillageWorkforceFinderStatus.InvalidProposal =>
                VillageWorkforceApiStatus.InvalidProposal,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static VillageWorkforceApiSnapshotStatus MapSnapshotStatus(
        VillageWorkforceSnapshotReadStatus status) => status switch
        {
            VillageWorkforceSnapshotReadStatus.Complete =>
                VillageWorkforceApiSnapshotStatus.Complete,
            VillageWorkforceSnapshotReadStatus.Partial =>
                VillageWorkforceApiSnapshotStatus.Partial,
            VillageWorkforceSnapshotReadStatus.SaveUnavailable =>
                VillageWorkforceApiSnapshotStatus.SaveUnavailable,
            VillageWorkforceSnapshotReadStatus.UnsupportedVersion =>
                VillageWorkforceApiSnapshotStatus.UnsupportedVersion,
            VillageWorkforceSnapshotReadStatus.ConflictingSources =>
                VillageWorkforceApiSnapshotStatus.ConflictingSources,
            VillageWorkforceSnapshotReadStatus.ChangedRevision =>
                VillageWorkforceApiSnapshotStatus.ChangedRevision,
            VillageWorkforceSnapshotReadStatus.ReadFailed =>
                VillageWorkforceApiSnapshotStatus.ReadFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    private static VillageWorkforceApiEvaluationState MapEvaluationState(
        WorkforceEvaluationState state) => state switch
        {
            WorkforceEvaluationState.Ranked =>
                VillageWorkforceApiEvaluationState.Ranked,
            WorkforceEvaluationState.Tied =>
                VillageWorkforceApiEvaluationState.Tied,
            WorkforceEvaluationState.CurrentOnly =>
                VillageWorkforceApiEvaluationState.CurrentOnly,
            WorkforceEvaluationState.Ineligible =>
                VillageWorkforceApiEvaluationState.Ineligible,
            WorkforceEvaluationState.Incomplete =>
                VillageWorkforceApiEvaluationState.Incomplete,
            WorkforceEvaluationState.Unsupported =>
                VillageWorkforceApiEvaluationState.Unsupported,
            WorkforceEvaluationState.Conflicting =>
                VillageWorkforceApiEvaluationState.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    private static VillageWorkforceApiWorkerState MapWorkerState(
        WorkforceWorkerState state) => state switch
        {
            WorkforceWorkerState.Eligible =>
                VillageWorkforceApiWorkerState.Eligible,
            WorkforceWorkerState.CurrentOnly =>
                VillageWorkforceApiWorkerState.CurrentOnly,
            WorkforceWorkerState.Ineligible =>
                VillageWorkforceApiWorkerState.Ineligible,
            WorkforceWorkerState.Incomplete =>
                VillageWorkforceApiWorkerState.Incomplete,
            WorkforceWorkerState.Unsupported =>
                VillageWorkforceApiWorkerState.Unsupported,
            WorkforceWorkerState.Conflicting =>
                VillageWorkforceApiWorkerState.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };

    private static VillageWorkforceApiRequirementKind MapRequirementKind(
        WorkforceRequirementKind kind) => kind switch
        {
            WorkforceRequirementKind.SupportedSourceVersion =>
                VillageWorkforceApiRequirementKind.SupportedSourceVersion,
            WorkforceRequirementKind.SupportedShopTarget =>
                VillageWorkforceApiRequirementKind.SupportedShopTarget,
            WorkforceRequirementKind.AlternativeWorkCandidate =>
                VillageWorkforceApiRequirementKind.AlternativeWorkCandidate,
            WorkforceRequirementKind.CharacterProfileAvailable =>
                VillageWorkforceApiRequirementKind.CharacterProfileAvailable,
            WorkforceRequirementKind.QualificationProvenanceMatch =>
                VillageWorkforceApiRequirementKind
                    .QualificationProvenanceMatch,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static VillageWorkforceApiRequirementOutcome MapRequirementOutcome(
        WorkforceRequirementOutcome outcome) => outcome switch
        {
            WorkforceRequirementOutcome.Passed =>
                VillageWorkforceApiRequirementOutcome.Passed,
            WorkforceRequirementOutcome.Failed =>
                VillageWorkforceApiRequirementOutcome.Failed,
            WorkforceRequirementOutcome.Incomplete =>
                VillageWorkforceApiRequirementOutcome.Incomplete,
            WorkforceRequirementOutcome.Unsupported =>
                VillageWorkforceApiRequirementOutcome.Unsupported,
            WorkforceRequirementOutcome.Conflicting =>
                VillageWorkforceApiRequirementOutcome.Conflicting,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };

    private static VillageWorkforceApiEvidenceSource MapEvidenceSource(
        WorkforceEvidenceSourceKind source) => source switch
        {
            WorkforceEvidenceSourceKind.ConfiguredSave =>
                VillageWorkforceApiEvidenceSource.ConfiguredSave,
            WorkforceEvidenceSourceKind.InstalledGameData =>
                VillageWorkforceApiEvidenceSource.InstalledGameData,
            WorkforceEvidenceSourceKind.DerivedRule =>
                VillageWorkforceApiEvidenceSource.DerivedRule,
            _ => throw new ArgumentOutOfRangeException(nameof(source))
        };

    private static VillageWorkforceApiValueKind MapValueKind(
        WorkforceFactValueKind kind) => kind switch
        {
            WorkforceFactValueKind.Boolean =>
                VillageWorkforceApiValueKind.Boolean,
            WorkforceFactValueKind.Int16 =>
                VillageWorkforceApiValueKind.Int16,
            WorkforceFactValueKind.Int32 =>
                VillageWorkforceApiValueKind.Int32,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static VillageWorkforceApiComparisonOutcome MapComparisonOutcome(
        WorkforceComparisonOutcome outcome) => outcome switch
        {
            WorkforceComparisonOutcome.Higher =>
                VillageWorkforceApiComparisonOutcome.Higher,
            WorkforceComparisonOutcome.Lower =>
                VillageWorkforceApiComparisonOutcome.Lower,
            WorkforceComparisonOutcome.Equal =>
                VillageWorkforceApiComparisonOutcome.Equal,
            WorkforceComparisonOutcome.Unavailable =>
                VillageWorkforceApiComparisonOutcome.Unavailable,
            WorkforceComparisonOutcome.Incompatible =>
                VillageWorkforceApiComparisonOutcome.Incompatible,
            WorkforceComparisonOutcome.NotComparable =>
                VillageWorkforceApiComparisonOutcome.NotComparable,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };

    private static VillageWorkforceApiChecklistItemKind MapChecklistKind(
        WorkforceChecklistItemKind kind) => kind switch
        {
            WorkforceChecklistItemKind.TargetIdentityMustMatch =>
                VillageWorkforceApiChecklistItemKind.TargetIdentityMustMatch,
            WorkforceChecklistItemKind
                .ReassignmentAvailabilityMustBeVerified =>
                VillageWorkforceApiChecklistItemKind
                    .ReassignmentAvailabilityMustBeVerified,
            WorkforceChecklistItemKind
                .QualificationAndEvidenceMustBeReviewed =>
                VillageWorkforceApiChecklistItemKind
                    .QualificationAndEvidenceMustBeReviewed,
            WorkforceChecklistItemKind.EfficiencyWasNotCalculated =>
                VillageWorkforceApiChecklistItemKind
                    .EfficiencyWasNotCalculated,
            WorkforceChecklistItemKind.NoActionWasSentToGame =>
                VillageWorkforceApiChecklistItemKind.NoActionWasSentToGame,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    private static VillageWorkforceApiChecklistCategory MapChecklistCategory(
        WorkforceChecklistCategory category) => category switch
        {
            WorkforceChecklistCategory.Prerequisite =>
                VillageWorkforceApiChecklistCategory.Prerequisite,
            WorkforceChecklistCategory.FactToVerify =>
                VillageWorkforceApiChecklistCategory.FactToVerify,
            WorkforceChecklistCategory.Caution =>
                VillageWorkforceApiChecklistCategory.Caution,
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };

    private static VillageWorkforceApiDiagnosticSeverity MapSeverity(
        WorkforceDiagnosticSeverity severity) => severity switch
        {
            WorkforceDiagnosticSeverity.Information =>
                VillageWorkforceApiDiagnosticSeverity.Information,
            WorkforceDiagnosticSeverity.Warning =>
                VillageWorkforceApiDiagnosticSeverity.Warning,
            WorkforceDiagnosticSeverity.Error =>
                VillageWorkforceApiDiagnosticSeverity.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(severity))
        };

    private static string UnitToken(WorkforceUnit unit) => unit switch
    {
        WorkforceUnit.BaseQualificationPoint => "BASE_QUALIFICATION_POINT",
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static string WorkerReference(VillageWorkerIdentity worker) =>
        $"village-worker:{worker.CharacterId.ToString(CultureInfo.InvariantCulture)}";

    private static string TargetReference(ShopManagerTargetIdentity target) =>
        string.Join(':',
            "village-workforce-target",
            target.Building.AreaId.ToString(CultureInfo.InvariantCulture),
            target.Building.BlockId.ToString(CultureInfo.InvariantCulture),
            target.Building.BuildingBlockIndex.ToString(
                CultureInfo.InvariantCulture),
            target.ManagerSlotIndex.ToString(CultureInfo.InvariantCulture));
}
