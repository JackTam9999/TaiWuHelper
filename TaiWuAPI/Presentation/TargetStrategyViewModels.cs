using TaiWu.Application.CombatRecommendations;
using TaiWu.Domain.TargetArchetypes;
using TaiWu.Domain.TargetPlaybookComposition;
using TaiWu.Domain.TargetProfiles;

namespace TaiWuAPI.Presentation;

public enum TargetStrategyStatus
{
    Available,
    Partial,
    Unsupported,
    Conflicting,
    NoMatch
}

public enum TargetProfileGroupKind
{
    Context,
    Mechanics
}

public sealed record TargetStrategyViewModel(
    TargetStrategyStatus Status,
    string StatusLabel,
    string Summary,
    DateTimeOffset CapturedAtUtc,
    string RuleVersion,
    int EvidenceSourceCount,
    int MatchedArchetypeCount,
    IReadOnlyList<TargetProfileGroupViewModel> ProfileGroups,
    IReadOnlyList<TargetArchetypeSummaryViewModel> Archetypes,
    IReadOnlyList<TargetResponseGoalViewModel> Goals,
    IReadOnlyList<TargetCounterSummaryViewModel> Counters,
    IReadOnlyList<TargetStrategyGapViewModel> StandaloneGaps,
    IReadOnlyList<TargetAdjustmentExplanationViewModel> Adjustments,
    TargetStrategyFeasibilityViewModel Feasibility)
{
    public bool IsMultiMatch => MatchedArchetypeCount > 1;

    public bool HasPlaybook => Goals.Count > 0;
}

public sealed record TargetProfileGroupViewModel(
    TargetProfileGroupKind Kind,
    string Label,
    IReadOnlyList<TargetProfileFacetSummaryViewModel> Facets);

public sealed record TargetProfileFacetSummaryViewModel(
    string Reference,
    TargetProfileDimension Dimension,
    string DimensionLabel,
    string Title,
    TargetProfileEvidenceState State,
    string StateLabel,
    string? ValueSummary,
    int EvidenceSourceCount,
    string EvidenceSummary);

public sealed record TargetArchetypeSummaryViewModel(
    string Code,
    string Title,
    TargetArchetypeMatchState State,
    string StateLabel,
    string VersionSummary,
    int EvidenceSourceCount,
    string EvidenceSummary,
    IReadOnlyList<string> SupportingFacts,
    IReadOnlyList<string> MissingFacts,
    IReadOnlyList<string> ExcludingFacts,
    IReadOnlyList<string> ConflictingFacts);

public sealed record TargetResponseGoalViewModel(
    string Code,
    string Title,
    string PriorityLabel,
    string TimingLabel,
    bool IsEligible,
    IReadOnlyList<TargetStrategyThreatLinkViewModel> Threats,
    IReadOnlyList<TargetStrategyCounterLinkViewModel> Counters,
    IReadOnlyList<TargetStrategyGapViewModel> Gaps);

public sealed record TargetStrategyThreatLinkViewModel(
    string Reference,
    string Title);

public sealed record TargetStrategyCounterLinkViewModel(
    string Anchor,
    string Name);

public sealed record TargetCounterSummaryViewModel(
    string Code,
    string Anchor,
    int SkillId,
    string SkillName,
    string SkillDetailHref,
    string DirectionLabel,
    TargetPlaybookCounterAvailabilityState Availability,
    string AvailabilityLabel,
    string FeasibilityExplanation,
    IReadOnlyList<string> RequirementSummaries,
    TargetStrategyGapViewModel? Gap);

public sealed record TargetStrategyGapViewModel(
    string Code,
    string Message);

public sealed record TargetAdjustmentExplanationViewModel(
    TargetPlaybookAdjustmentAction Action,
    string ActionLabel,
    string Summary,
    string Reason,
    TargetAdjustmentReferenceViewModel? OriginalResponse,
    TargetAdjustmentReferenceViewModel? ResultResponse,
    IReadOnlyList<TargetAdjustmentEvidenceViewModel> Evidence);

public sealed record TargetAdjustmentReferenceViewModel(
    string Title,
    string? Href,
    string? ThreatReference = null);

public sealed record TargetAdjustmentEvidenceViewModel(
    TargetPlaybookAdjustmentEvidenceKind Kind,
    TargetPlaybookAdjustmentEvidenceState State,
    string StateLabel,
    string Title,
    string? Href,
    string? ThreatReference,
    int SourceCount);

public sealed record TargetStrategyFeasibilityViewModel(
    string Summary,
    bool CurrentLoadoutAlreadySatisfies,
    int FeasibleCounterCount,
    int UnavailableCounterCount);
