using TaiWu.Domain.VillageWorkforce;

namespace TaiWuAPI.Presentation;

public sealed class VillageWorkforceInteractionState
{
    private readonly List<int> _selectedCharacterIds = [];

    public WorkforceShortlistFilter Filter { get; private set; } =
        WorkforceShortlistFilter.All;

    public string NameQuery { get; private set; } = string.Empty;

    public IReadOnlyList<int> SelectedCharacterIds =>
        _selectedCharacterIds;

    public bool ComparisonReady => _selectedCharacterIds.Count == 2;

    public void SetFilter(WorkforceShortlistFilter filter)
    {
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(nameof(filter));
        }

        Filter = filter;
    }

    public void SetNameQuery(string? value) =>
        NameQuery = value?.Trim() ?? string.Empty;

    public bool IsSelected(int characterId) =>
        _selectedCharacterIds.Contains(characterId);

    public bool IsSelectionDisabled(int characterId) =>
        ComparisonReady && !IsSelected(characterId);

    public void ToggleComparison(int characterId)
    {
        if (_selectedCharacterIds.Remove(characterId))
        {
            return;
        }

        if (_selectedCharacterIds.Count < 2)
        {
            _selectedCharacterIds.Add(characterId);
        }
    }

    public IReadOnlyList<VillageWorkforceCandidateViewModel> VisibleCandidates(
        VillageWorkforceViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Candidates
            .Where(MatchesFilter)
            .Where(item => string.IsNullOrWhiteSpace(NameQuery)
                || item.Label.Contains(
                    NameQuery,
                    StringComparison.CurrentCultureIgnoreCase))
            .ToArray();
    }

    public void ClearComparison() => _selectedCharacterIds.Clear();

    public void Reset()
    {
        Filter = WorkforceShortlistFilter.All;
        NameQuery = string.Empty;
        _selectedCharacterIds.Clear();
    }

    private bool MatchesFilter(VillageWorkforceCandidateViewModel candidate) =>
        Filter switch
        {
            WorkforceShortlistFilter.All => true,
            WorkforceShortlistFilter.Comparable => candidate.State is
                Contracts.VillageWorkforce.VillageWorkforceApiEvaluationState.Ranked
                or Contracts.VillageWorkforce.VillageWorkforceApiEvaluationState.Tied,
            WorkforceShortlistFilter.NeedsReview => candidate.State is
                Contracts.VillageWorkforce.VillageWorkforceApiEvaluationState.Incomplete
                or Contracts.VillageWorkforce.VillageWorkforceApiEvaluationState.Unsupported
                or Contracts.VillageWorkforce.VillageWorkforceApiEvaluationState.Conflicting,
            WorkforceShortlistFilter.Ineligible => candidate.State ==
                Contracts.VillageWorkforce.VillageWorkforceApiEvaluationState.Ineligible,
            _ => throw new InvalidOperationException(
                $"Unknown workforce filter '{Filter}'.")
        };
}
