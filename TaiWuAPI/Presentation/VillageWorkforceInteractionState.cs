using TaiWu.Domain.VillageWorkforce;

namespace TaiWuAPI.Presentation;

public sealed class VillageWorkforceInteractionState
{
    public const int DefaultAlternativeLimit = 10;
    public const int PageSize = 25;

    private readonly List<int> _selectedCharacterIds = [];

    public WorkforceShortlistFilter Filter { get; private set; } =
        WorkforceShortlistFilter.Comparable;

    public string NameQuery { get; private set; } = string.Empty;

    public bool ShowAllComparable { get; private set; }

    public int PageIndex { get; private set; }

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
        ShowAllComparable = false;
        PageIndex = 0;
    }

    public void SetNameQuery(string? value)
    {
        NameQuery = value?.Trim() ?? string.Empty;
        PageIndex = 0;
    }

    public void ShowAllMatches()
    {
        ShowAllComparable = true;
        PageIndex = 0;
    }

    public void ShowTopAlternatives()
    {
        ShowAllComparable = false;
        PageIndex = 0;
    }

    public bool IsSelected(int characterId) =>
        _selectedCharacterIds.Contains(characterId);

    public bool IsSelectionDisabled(int characterId) =>
        ComparisonReady && !IsSelected(characterId);

    public void ToggleComparison(int characterId, int currentCharacterId)
    {
        if (_selectedCharacterIds.Remove(characterId))
        {
            return;
        }

        if (_selectedCharacterIds.Count == 0
            && characterId != currentCharacterId)
        {
            _selectedCharacterIds.Add(currentCharacterId);
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
        var matching = MatchingCandidates(model);
        if (UsesCompactSummary)
        {
            return matching
                .Take(DefaultAlternativeLimit + 1)
                .ToArray();
        }

        return matching
            .Skip(PageIndex * PageSize)
            .Take(PageSize)
            .ToArray();
    }

    public int MatchingCandidateCount(VillageWorkforceViewModel model) =>
        MatchingCandidates(model).Count;

    public bool HasMoreCompactCandidates(VillageWorkforceViewModel model) =>
        UsesCompactSummary
        && MatchingCandidateCount(model) > DefaultAlternativeLimit + 1;

    public int PageCount(VillageWorkforceViewModel model)
    {
        var count = MatchingCandidateCount(model);
        return Math.Max(1, (count + PageSize - 1) / PageSize);
    }

    public bool HasPreviousPage => !UsesCompactSummary && PageIndex > 0;

    public bool HasNextPage(VillageWorkforceViewModel model) =>
        !UsesCompactSummary && PageIndex + 1 < PageCount(model);

    public void PreviousPage()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
        }
    }

    public void NextPage(VillageWorkforceViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (HasNextPage(model))
        {
            PageIndex++;
        }
    }

    public void ClearComparison() => _selectedCharacterIds.Clear();

    public void Reset()
    {
        Filter = WorkforceShortlistFilter.Comparable;
        NameQuery = string.Empty;
        ShowAllComparable = false;
        PageIndex = 0;
        _selectedCharacterIds.Clear();
    }

    private bool UsesCompactSummary =>
        Filter == WorkforceShortlistFilter.Comparable
        && !ShowAllComparable
        && string.IsNullOrWhiteSpace(NameQuery);

    private IReadOnlyList<VillageWorkforceCandidateViewModel>
        MatchingCandidates(VillageWorkforceViewModel model)
    {
        var matching = model.Candidates
            .Where(candidate => candidate.IsCurrent
                && Filter == WorkforceShortlistFilter.Comparable
                || MatchesFilter(candidate))
            .Where(item => string.IsNullOrWhiteSpace(NameQuery)
                || item.Label.Contains(
                    NameQuery,
                    StringComparison.CurrentCultureIgnoreCase))
            .ToArray();
        if (Filter != WorkforceShortlistFilter.Comparable)
        {
            return matching;
        }

        return matching
            .OrderByDescending(candidate => candidate.IsCurrent)
            .ThenBy(candidate => candidate.DisplayOrdinal)
            .ToArray();
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
