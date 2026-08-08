using TaiWu.Domain.CombatRecommendations;
using TaiWu.Domain.CombatSnapshots;
using TaiWu.Domain.LoadoutComparisons;

namespace TaiWuAPI.Presentation;

public sealed record LoadoutComparisonViewModel(
    string Reference,
    string SnapshotReference,
    IReadOnlyList<LoadoutComparisonColumnViewModel> Columns,
    IReadOnlyList<LoadoutComparisonCategoryViewModel> Categories,
    IReadOnlyList<LoadoutComparisonProvenanceViewModel> BaselineProvenance,
    string InformationOnlyNotice);

public sealed record LoadoutComparisonColumnViewModel(
    LoadoutComparisonColumnKind Kind,
    LoadoutComparisonColumnStatus Status,
    RecommendationPolicy? Policy,
    string? StyleReference,
    LoadoutComparisonGenericSlotsViewModel? GenericSlots,
    bool GenericSlotsChanged,
    int? ManualActionCount,
    string? ManualActionCountUnavailableReason,
    string? Diagnostic);

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

public sealed class LoadoutComparisonFilterState
{
    public bool DifferencesOnly { get; private set; }

    public void ShowAll() => DifferencesOnly = false;

    public void ShowDifferences() => DifferencesOnly = true;

    public void SetDifferencesOnly(bool value) => DifferencesOnly = value;
}
