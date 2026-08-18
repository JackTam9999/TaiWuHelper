using TaiWuAPI.Contracts.VillageWorkforce;

namespace TaiWuAPI.Presentation;

public sealed record VillageWorkforceViewModel(
    VillageWorkforceApiStatus Status,
    DateTimeOffset CapturedAtUtc,
    bool IsPartial,
    string ObjectiveLabel,
    string ObjectiveDescription,
    string RuleVersion,
    string TargetLabel,
    VillageWorkforceCountsResponse Counts,
    VillageWorkforceCandidateViewModel Current,
    IReadOnlyList<VillageWorkforceCandidateViewModel> Candidates,
    IReadOnlyList<string> Limitations);

public sealed record VillageWorkforceCandidateViewModel(
    int CharacterId,
    int DisplayOrdinal,
    string Label,
    string? LocationLabel,
    bool IsCurrent,
    VillageWorkforceApiEvaluationState State,
    string StateLabel,
    string StateCssClass,
    int? CompetitionRank,
    decimal? Total,
    string UnitLabel,
    string DecisiveEvidence,
    int PassedRequirements,
    IReadOnlyList<VillageWorkforceRequirementViewModel> Requirements,
    IReadOnlyList<VillageWorkforceComponentViewModel> Components,
    VillageWorkerCapabilitySummaryViewModel? CapabilitySummary = null);

public sealed record VillageWorkerCapabilityCategoryViewModel(
    string Label,
    string AverageLabel,
    string CoverageLabel);

public sealed record VillageWorkerCapabilitySummaryViewModel(
    VillageWorkerCapabilityCategoryViewModel MainAttributes,
    VillageWorkerCapabilityCategoryViewModel MartialDisciplines,
    VillageWorkerCapabilityCategoryViewModel LifeSkillDisciplines);

public sealed record VillageWorkforceRequirementViewModel(
    int Order,
    string Explanation,
    string Outcome,
    bool Passed,
    IReadOnlyList<VillageWorkforceProvenanceViewModel> Provenance,
    int ConflictCount);

public sealed record VillageWorkforceComponentViewModel(
    string Explanation,
    decimal Contribution,
    string UnitLabel,
    IReadOnlyList<VillageWorkforceProvenanceViewModel> Provenance);

public sealed record VillageWorkforceProvenanceViewModel(
    string Source,
    string Version);

public sealed record VillageWorkforceComparisonViewModel(
    VillageWorkforceCandidateViewModel First,
    VillageWorkforceCandidateViewModel Second,
    string Outcome,
    decimal? FirstValue,
    decimal? SecondValue,
    string UnitLabel,
    IReadOnlyList<string> ManualChecklist);

public sealed record VillageWorkforceNoticeViewModel(
    string Title,
    string Message,
    bool CanRetry);

public sealed record VillageWorkforceTargetOptionViewModel(
    int OptionIndex,
    string Label);

public sealed record VillageWorkforceTargetGroupViewModel(
    int GroupIndex,
    string Label,
    IReadOnlyList<VillageWorkforceTargetOptionViewModel> Positions);
