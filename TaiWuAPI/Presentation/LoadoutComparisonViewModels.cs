using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.CombatThreats;
using TaiWu.Domain.LoadoutComparisons;

namespace TaiWuAPI.Presentation;

public sealed record LoadoutComparisonViewModel(
    string Reference,
    string SnapshotReference,
    IReadOnlyList<LoadoutComparisonColumnViewModel> Columns,
    IReadOnlyList<LoadoutComparisonCategoryViewModel> Categories,
    IReadOnlyList<LoadoutComparisonProvenanceViewModel> BaselineProvenance,
    string InformationOnlyNotice,
    IReadOnlyList<LoadoutComparisonUnsupportedViewModel>? UnsupportedMechanics
        = null)
{
    public IReadOnlyList<LoadoutComparisonUnsupportedViewModel>
        SafeUnsupportedMechanics => UnsupportedMechanics ?? [];
}

public sealed record LoadoutComparisonColumnViewModel(
    LoadoutComparisonColumnKind Kind,
    LoadoutComparisonColumnStatus Status,
    RecommendationPolicy? Policy,
    string? StyleReference,
    LoadoutComparisonGenericSlotsViewModel? GenericSlots,
    bool GenericSlotsChanged,
    int? ManualActionCount,
    string? ManualActionCountUnavailableReason,
    string? Diagnostic,
    LoadoutComparisonTacticalViewModel? Tactical = null);

public sealed record LoadoutComparisonGenericSlotsViewModel(
    int Total,
    int Attack,
    int Agility,
    int Defense,
    int Assistance);

public sealed record LoadoutComparisonCategoryViewModel(
    SkillCategory Category,
    string DisplayName,
    IReadOnlyList<LoadoutComparisonCapacityCellViewModel> Capacities,
    IReadOnlyList<LoadoutComparisonSkillRowViewModel> Skills);

public sealed record LoadoutComparisonCapacityCellViewModel(
    LoadoutComparisonColumnKind Column,
    int? Used,
    string? UsedUnavailableReason,
    int? Capacity,
    string? CapacityUnavailableReason,
    int? Remaining,
    string? RemainingUnavailableReason,
    int? CategoryContribution,
    string? CategoryContributionUnavailableReason,
    int? GenericContribution,
    string? GenericContributionUnavailableReason);

public sealed record LoadoutComparisonSkillRowViewModel(
    SkillCategory Category,
    int SkillId,
    string? Name,
    string? NameUnavailableReason,
    IReadOnlyList<LoadoutComparisonSkillCellViewModel> Cells);

public sealed record LoadoutComparisonSkillCellViewModel(
    LoadoutComparisonColumnKind Column,
    LoadoutComparisonMembership? Membership,
    string? MembershipUnavailableReason,
    PracticeDirection? CurrentDirection,
    string? CurrentDirectionUnavailableReason,
    int? EffectiveCost,
    string? EffectiveCostUnavailableReason,
    IReadOnlyList<LoadoutComparisonSkillActionViewModel> Actions)
{
    public bool HasDifference => Membership is
            LoadoutComparisonMembership.Added
            or LoadoutComparisonMembership.Removed
        || Membership is null
        || Actions.Count > 0;
}

public sealed record LoadoutComparisonSkillActionViewModel(
    LoadoutComparisonSkillActionKind Kind,
    PracticeDirection RequiredDirection,
    string Reason);

public sealed record LoadoutComparisonProvenanceViewModel(
    LoadoutComparisonBaselineField Field,
    SnapshotDataSource Source,
    DateTimeOffset CapturedAtUtc);

public sealed record LoadoutComparisonTacticalViewModel(
    RecommendationPolicy Policy,
    LoadoutComparisonRoleViewModel ActiveDefense,
    LoadoutComparisonRoleViewModel ActiveAgility,
    IReadOnlyList<LoadoutComparisonThreatViewModel> CoveredThreats,
    IReadOnlyList<LoadoutComparisonThreatViewModel> UnresolvedThreats,
    IReadOnlyList<LoadoutComparisonConditionSummaryViewModel> Conditions,
    IReadOnlyList<LoadoutComparisonCaveatSummaryViewModel> Caveats,
    IReadOnlyList<LoadoutComparisonScoreSummaryViewModel> Scores,
    IReadOnlyList<string> EvidenceReferences);

public sealed record LoadoutComparisonRoleViewModel(
    string? SkillName,
    string? UnavailableReason);

public sealed record LoadoutComparisonThreatViewModel(
    string Reference,
    string Code,
    string Title,
    TargetThreatSeverity Severity,
    IReadOnlyList<string> EvidenceReferences);

public sealed record LoadoutComparisonConditionSummaryViewModel(
    string SkillName,
    RecommendationConditionKind Kind,
    CombatRequirementCriticality Criticality,
    CombatRequirementStatus Status,
    string Evaluation,
    string EvidenceReference);

public sealed record LoadoutComparisonCaveatSummaryViewModel(
    RecommendationCaveatKind Kind,
    string Explanation,
    string? SkillName,
    IReadOnlyList<string> EvidenceReferences);

public sealed record LoadoutComparisonScoreSummaryViewModel(
    RecommendationScoreComponentKind Kind,
    int Weight,
    decimal? Score,
    string? ScoreUnavailableReason,
    string Explanation,
    string EvidenceReference);

public sealed record LoadoutComparisonUnsupportedViewModel(
    bool IsCritical,
    string Message,
    string EffectOnRecommendation,
    IReadOnlyList<string> EvidenceReferences);

public sealed class LoadoutComparisonFilterState
{
    public bool DifferencesOnly { get; private set; }

    public void ShowAll() => DifferencesOnly = false;

    public void ShowDifferences() => DifferencesOnly = true;

    public void SetDifferencesOnly(bool value) => DifferencesOnly = value;
}
