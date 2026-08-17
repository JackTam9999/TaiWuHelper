using TaiWu.Domain.CompanionRoles;

namespace TaiWuAPI.Presentation;

public sealed class CompanionFinderInteractionState
{
    private readonly List<int> _selectedCharacterIds = [];

    public CompanionRoleShortlistFilter Filter { get; private set; } =
        CompanionRoleShortlistFilter.All;

    public string NameQuery { get; private set; } = string.Empty;

    public IReadOnlyList<int> SelectedCharacterIds => _selectedCharacterIds;

    public bool ComparisonReady => _selectedCharacterIds.Count == 2;

    public void SetFilter(CompanionRoleShortlistFilter filter)
    {
        if (!Enum.IsDefined(filter))
        {
            throw new ArgumentOutOfRangeException(
                nameof(filter),
                filter,
                "Unknown shortlist filter.");
        }

        Filter = filter;
    }

    public void SetNameQuery(string? query)
    {
        NameQuery = query?.Trim() ?? string.Empty;
    }

    public IReadOnlyList<CompanionCandidateViewModel> VisibleCandidates(
        CompanionFinderViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return [.. model.Candidates.Where(candidate =>
            MatchesFilter(candidate)
            && (NameQuery.Length == 0
                || candidate.DisplayName.Contains(
                    NameQuery,
                    StringComparison.CurrentCultureIgnoreCase)))];
    }

    public bool IsSelected(int characterId) =>
        _selectedCharacterIds.Contains(characterId);

    public bool CanSelect(int characterId) =>
        IsSelected(characterId) || _selectedCharacterIds.Count < 2;

    public void ToggleComparison(
        CompanionFinderViewModel model,
        int characterId)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Candidates.All(value => value.CharacterId != characterId))
        {
            throw new ArgumentOutOfRangeException(
                nameof(characterId),
                characterId,
                "The comparison candidate is not in this result.");
        }

        if (_selectedCharacterIds.Remove(characterId))
        {
            return;
        }

        if (_selectedCharacterIds.Count >= 2)
        {
            throw new InvalidOperationException(
                "At most two candidates can be selected for comparison.");
        }

        _selectedCharacterIds.Add(characterId);
    }

    public IReadOnlyList<int> ClearComparison()
    {
        var previous = _selectedCharacterIds.ToArray();
        _selectedCharacterIds.Clear();
        return previous;
    }

    public void Reset()
    {
        Filter = CompanionRoleShortlistFilter.All;
        NameQuery = string.Empty;
        _selectedCharacterIds.Clear();
    }

    private bool MatchesFilter(CompanionCandidateViewModel candidate) =>
        Filter switch
        {
            CompanionRoleShortlistFilter.All => true,
            CompanionRoleShortlistFilter.Ranked =>
                candidate.Section == CompanionCandidateSection.Ranked,
            CompanionRoleShortlistFilter.NeedsReview =>
                candidate.Section == CompanionCandidateSection.NeedsReview,
            CompanionRoleShortlistFilter.Ineligible =>
                candidate.Section == CompanionCandidateSection.Ineligible,
            _ => throw new ArgumentOutOfRangeException(
                nameof(Filter),
                Filter,
                "Unknown shortlist filter.")
        };
}
